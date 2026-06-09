using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Plugins;
using GestureSign.Daemon.Filtration;
using GestureSign.Daemon.Surface;
using GestureSign.PointPatterns;
using ManagedWinapi.Hooks;
using ManagedWinapi.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace GestureSign.Daemon.Input
{
    public class PointCapture : ILoadable, IPointCapture, IDisposable
    {
        #region Private Variables

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002; // Don't call back for events on installer's process
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;

        // Create new Touch hook control to capture global input from Touch, and create an event translator to get formal events
        private readonly PointEventTranslator _pointEventTranslator;
        private readonly InputProvider _inputProvider;
        private readonly PointerInputTargetWindow _pointerInputTargetWindow;
        private readonly List<IPointPattern> _pointPatternCache = new List<IPointPattern>();
        private readonly System.Threading.Timer _blockTouchDelayTimer;
        private SurfaceForm _surfaceForm;

        private System.Threading.Timer _initialTimeoutTimer;
        private System.Threading.Timer _gestureTimeoutTimer;
        SynchronizationContext _currentContext;

        private Dictionary<int, List<Point>> _pointsCaptured;
        private List<int> _orderedContactIdentifiers;
        // Create variable to hold the only allowed instance of this class
        static readonly PointCapture _Instance = new PointCapture();

        private CaptureMode _mode = CaptureMode.Normal;
        private volatile CaptureState _state;

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        readonly WinEventDelegate _winEventDele;
        private readonly IntPtr _hWinEventHook;
        private GCHandle _winEventGch;

        private bool disposedValue = false; // To detect redundant calls

        private int? _blockTouchInputThreshold;
        private Point _touchPadStartPoint;
        private IntPtr _ignoredTouchPadDeviceHandle;
        private string _unrecognizedGestureSoundPath;
        private SoundPlayer _unrecognizedGestureSoundPlayer;

        #endregion

        #region PInvoke 

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        #endregion

        #region Public Instance Properties

        public Devices SourceDevice { get { return _pointEventTranslator.SourceDevice; } }

        public LowLevelMouseHook MouseHook
        {
            get { return _inputProvider.LowLevelMouseHook; }
        }

        public bool TemporarilyDisableCapture { get; set; }

        public List<CapturedContact> InputContacts
        {
            get
            {
                if (_pointsCaptured == null)
                    return new List<CapturedContact>();

                if (_orderedContactIdentifiers == null || _orderedContactIdentifiers.Count == 0)
                {
                    return _pointsCaptured
                        .Select(capturedPoint => new CapturedContact(capturedPoint.Key, capturedPoint.Value))
                        .ToList();
                }

                return _orderedContactIdentifiers
                    .Where(contactIdentifier => _pointsCaptured.ContainsKey(contactIdentifier))
                    .Select(contactIdentifier => new CapturedContact(contactIdentifier, _pointsCaptured[contactIdentifier]))
                    .ToList();
            }
        }

        public List<Point>[] InputPoints
        {
            get
            {
                return InputContacts.Select(contact => contact.Points).ToArray();
            }
        }

        public List<int> InputContactIdentifiers
        {
            get
            {
                return InputContacts.Select(contact => contact.ContactIdentifier).ToList();
            }
        }

        public List<int> CapturePressedVirtualKeys { get; private set; } = new List<int>();

        public CaptureState State
        {
            get { return _state; }
            set { _state = value; }
        }

        public CaptureMode Mode
        {
            get { return _mode; }
            set
            {
                if (value == _mode) return;
                _mode = value;
                OnModeChanged(new ModeChangedEventArgs(value));
            }
        }

        #endregion

        #region Custom Events

        public event ApplicationChangedEventHandler ForegroundApplicationsChanged;
        // Create an event to notify subscribers that CaptureState has been changed
        public event ModeChangedEventHandler ModeChanged;

        protected virtual void OnModeChanged(ModeChangedEventArgs e)
        {
            if (ModeChanged != null) ModeChanged(this, e);
        }

        // Create event to notify subscribers that the capture process has started
        public event PointsCapturedEventHandler CaptureStarted;

        protected virtual void OnCaptureStarted(PointsCapturedEventArgs e)
        {
            if (CaptureStarted != null) CaptureStarted(this, e);
        }

        // Create event to notify subscribers that a point set has been captured
        public event PointsCapturedEventHandler AfterPointsCaptured;
        public event PointsCapturedEventHandler BeforePointsCaptured;
        public event RecognitionEventHandler GestureRecognized;
        //public event RecognitionEventHandler GestureNotRecognized;

        protected virtual void OnAfterPointsCaptured(PointsCapturedEventArgs e)
        {
            if (AfterPointsCaptured != null) AfterPointsCaptured(this, e);
        }

        protected virtual void OnBeforePointsCaptured(PointsCapturedEventArgs e)
        {
            if (BeforePointsCaptured != null) BeforePointsCaptured(this, e);
        }

        protected virtual void OnGestureRecognized(RecognitionEventArgs e)
        {
            if (GestureRecognized != null) GestureRecognized(this, e);
        }

        //protected virtual void OnGestureNotRecognized(RecognitionEventArgs e)
        //{
        //    if (GestureNotRecognized != null) GestureNotRecognized(this, e);
        //}

        // Create event to notify subscribers that a single point has been captured
        public event PointsCapturedEventHandler PointCaptured;

        protected virtual void OnPointCaptured(PointsCapturedEventArgs e)
        {
            if (PointCaptured != null) PointCaptured(this, e);
        }

        // Create event to notify subscribers that the capture process has ended
        public event EventHandler CaptureEnded;

        protected virtual void OnCaptureEnded()
        {
            if (CaptureEnded != null) CaptureEnded(this, new EventArgs());
        }

        // Create event to notify subscribers that the capture has been canceled
        public event PointsCapturedEventHandler CaptureCanceled;

        protected virtual void OnCaptureCanceled(PointsCapturedEventArgs e)
        {
            if (CaptureCanceled != null) CaptureCanceled(this, e);
        }

        #endregion

        #region Public Properties

        public static PointCapture Instance
        {
            get { return _Instance; }
        }

        #endregion

        #region Constructors

        protected PointCapture()
        {
            _surfaceForm = new SurfaceForm();

            CaptureStarted += (o, e) => { if (Mode != CaptureMode.UserDisabled) _surfaceForm.StartDrawing(e.FirstCapturedPoints); };
            CaptureEnded += (o, e) => { _surfaceForm.EndDrawing(); };
            CaptureCanceled += (o, e) => { _surfaceForm.EndDrawing(); };
            PointCaptured += (o, e) =>
            {
                if (Mode != CaptureMode.UserDisabled && State == CaptureState.Capturing)
                {
                    _surfaceForm.DrawPoints(e.Points);
                }
            };

            _inputProvider = new InputProvider();
            _pointEventTranslator = new PointEventTranslator(_inputProvider);
            _pointEventTranslator.PointDown += (PointEventTranslator_PointDown);
            _pointEventTranslator.PointUp += (PointEventTranslator_PointUp);
            _pointEventTranslator.PointMove += (PointEventTranslator_PointMove);

            _currentContext = SynchronizationContext.Current;

            _winEventDele = WinEventProc;
            _winEventGch = GCHandle.Alloc(_winEventDele);
            _hWinEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, _winEventDele, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

            if (AppConfig.UiAccess)
            {
                _pointerInputTargetWindow = new PointerInputTargetWindow();
                ModeChanged += (o, e) =>
                {
                    if (e.Mode == CaptureMode.UserDisabled)
                        _pointerInputTargetWindow.BlockTouchInputThreshold = 0;
                };
                _blockTouchDelayTimer = new System.Threading.Timer(UpdateBlockTouchInputThresholdCallback, null, Timeout.Infinite, Timeout.Infinite);
                ForegroundApplicationsChanged += PointCapture_ForegroundApplicationsChanged;
            }

            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        #endregion

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _initialTimeoutTimer?.Dispose();
                    _gestureTimeoutTimer?.Dispose();
                    _blockTouchDelayTimer?.Dispose();
                    _pointerInputTargetWindow?.Dispose();
                    _inputProvider?.Dispose();
                    _surfaceForm?.Dispose();
                    _unrecognizedGestureSoundPlayer?.Dispose();
                }
                _surfaceForm = null;
                _unrecognizedGestureSoundPlayer = null;

                SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                if (_hWinEventHook != IntPtr.Zero)
                    UnhookWinEvent(_hWinEventHook);
                if (_winEventGch.IsAllocated)
                {
                    _winEventGch.Free();
                }

                disposedValue = true;
            }
        }

        ~PointCapture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region System Events

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_SYSTEM_FOREGROUND || eventType == EVENT_SYSTEM_MINIMIZEEND)
            {
                if (State != CaptureState.Ready || Mode != CaptureMode.Normal || hwnd.Equals(IntPtr.Zero))
                    return;
                var systemWindow = new SystemWindow(hwnd);
                if (!systemWindow.Visible)
                    return;
                var apps = ApplicationManager.Instance.GetApplicationFromWindow(systemWindow);
                ForegroundApplicationsChanged?.Invoke(this, new ApplicationChangedEventArgs(apps));
            }
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.RemoteConnect:
                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.SessionUnlock:
                    if (State == CaptureState.Disabled)
                        State = CaptureState.Ready;
                    break;
                case SessionSwitchReason.SessionLock:
                    State = CaptureState.Disabled;
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Events

        private void PointCapture_ForegroundApplicationsChanged(object sender, ApplicationChangedEventArgs appsChanged)
        {
            if (appsChanged.Applications != null)
            {
                var userAppList = appsChanged.Applications.Where(application => application is UserApp).ToList();
                if (userAppList.Count == 0)
                {
                    UpdateBlockTouchInputThreshold(0);
                    return;
                }
                UpdateBlockTouchInputThreshold(userAppList.Cast<UserApp>().Max(app => app.BlockTouchInputThreshold));
            }
        }

        protected void PointEventTranslator_PointDown(object sender, InputPointsEventArgs e)
        {
            if (ShouldIgnoreTouchPadInput(e) || ShouldBlockSingleFingerTouchStart(e))
                return;

            if (State == CaptureState.Ready || State == CaptureState.Capturing || State == CaptureState.CapturingInvalid)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                var stateBeforePointDown = State;
                var timeout = AppConfig.InitialTimeout;
                if (AppConfig.GestureTimeout <= 0 && timeout > 0 && ShouldStartInitialTimeout(e.PointSource, stateBeforePointDown))
                {
                    if (_initialTimeoutTimer == null)
                    {
                        _initialTimeoutTimer = new System.Threading.Timer(InitialTimeoutCallback, null, Timeout.Infinite, Timeout.Infinite);
                    }
                    _initialTimeoutTimer.Change(timeout, Timeout.Infinite);
                }

                // Try to begin capture process, if capture started then don't notify other applications of a Point event, otherwise do
                if (!TryBeginCapture(e.InputPointList))
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                }
                else e.Handled = Mode != CaptureMode.UserDisabled;
            }
        }

        protected void PointEventTranslator_PointMove(object sender, InputPointsEventArgs e)
        {
            if (ShouldIgnoreTouchPadInput(e))
                return;

            // Only add point if we're capturing
            if (State == CaptureState.Capturing || State == CaptureState.CapturingInvalid)
            {
                AddPoint(e.InputPointList);
            }
            UpdateBlockTouchInputThreshold();
        }

        protected void PointEventTranslator_PointUp(object sender, InputPointsEventArgs e)
        {
            if (e.PointSource == Devices.TouchPad && (_ignoredTouchPadDeviceHandle == IntPtr.Zero || _ignoredTouchPadDeviceHandle == e.DeviceHandle))
                _ignoredTouchPadDeviceHandle = IntPtr.Zero;

            if (State == CaptureState.Capturing || State == CaptureState.CapturingInvalid && (SourceDevice & Devices.TouchDevice) != 0)
            {
                e.Handled = Mode != CaptureMode.UserDisabled;

                EndCapture();

                if (TemporarilyDisableCapture && Mode == CaptureMode.UserDisabled)
                {
                    TemporarilyDisableCapture = false;
                    ToggleUserDisablePointCapture();
                }
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }
            else if (State == CaptureState.CapturingInvalid && SourceDevice == Devices.Mouse)
            {
                if (Mode != CaptureMode.UserDisabled)
                {
                    State = CaptureState.Disabled;

                    var observeExceptionsTask = new Action<Task>(t =>
                    {
                        State = CaptureState.Ready;
                        Console.WriteLine($"{t.Exception.InnerException.GetType().Name}: {t.Exception.InnerException.Message}");
                    });

                    var drawingButton = _pointEventTranslator.CurrentDrawingButton;
                    var clickAsync = Task.Factory.StartNew(delegate
                    {
                        ClickMouseButton(drawingButton);
                        State = CaptureState.Ready;
                    }).ContinueWith(observeExceptionsTask, TaskContinuationOptions.OnlyOnFaulted);

                    e.Handled = true;
                }
                else
                {
                    State = CaptureState.Ready;
                }
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }
            else if (State == CaptureState.TriggerFired)
            {
                State = CaptureState.Ready;
                e.Handled = Mode != CaptureMode.UserDisabled;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }

            UpdateBlockTouchInputThreshold();
            if (_initialTimeoutTimer != null)
                _initialTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
            if (_gestureTimeoutTimer != null)
                _gestureTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #endregion

        #region Private Methods

        private void UpdateBlockTouchInputThreshold(int? threshold = null)
        {
            if (!AppConfig.UiAccess) return;

            if (threshold != null)
            {
                _blockTouchInputThreshold = threshold;
                PostToCurrentContext(() =>
                {
                    if (_pointerInputTargetWindow != null)
                        _pointerInputTargetWindow.BlockTouchInputThreshold = _blockTouchInputThreshold.GetValueOrDefault();
                    _blockTouchInputThreshold = null;
                });
                return;
            }

            if (_blockTouchInputThreshold != null)
                _blockTouchDelayTimer.Change(100, Timeout.Infinite);
        }

        private void UpdateBlockTouchInputThresholdCallback(object o)
        {
            if (!_blockTouchInputThreshold.HasValue) return;

            PostToCurrentContext(() =>
            {
                _pointerInputTargetWindow.BlockTouchInputThreshold = _blockTouchInputThreshold.GetValueOrDefault();
                _blockTouchInputThreshold = null;
            });
        }

        private void InitialTimeoutCallback(object o)
        {
            PostToCurrentContext(() =>
            {
                if (State != CaptureState.CapturingInvalid) return;

                try
                {
                    if (SourceDevice == Devices.TouchScreen)
                    {
                        if (_pointerInputTargetWindow != null && _pointerInputTargetWindow.BlockTouchInputThreshold > 1)
                            _pointerInputTargetWindow.TemporarilyDisable();
                    }
                    else if (SourceDevice == Devices.Mouse)
                    {
                        PressMouseButton(_pointEventTranslator.CurrentDrawingButton);
                    }
                    else if (SourceDevice == Devices.TouchPad)
                    {
                        _ignoredTouchPadDeviceHandle = _pointEventTranslator.SourceDeviceHandle;
                    }
                    CancelCaptureByInitialTimeout();
                    if ((SourceDevice & Devices.TouchDevice) != 0)
                    {
                        _pointEventTranslator.ResetTouchDeviceSource();
                        _inputProvider.ReleaseCurrentTouchSource();
                    }
                }
                catch
                {
                    State = CaptureState.Ready;
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                }
            });
        }

        private void GestureTimeoutCallback(object o)
        {
            PostToCurrentContext(() =>
            {
                if (State != CaptureState.Capturing &&
                    State != CaptureState.CapturingInvalid &&
                    State != CaptureState.TriggerFired) return;

                try
                {
                    if (SourceDevice == Devices.TouchScreen)
                    {
                        if (_pointerInputTargetWindow != null && _pointerInputTargetWindow.BlockTouchInputThreshold > 1)
                            _pointerInputTargetWindow.TemporarilyDisable();
                    }
                    else if (SourceDevice == Devices.Mouse)
                    {
                        PressMouseButton(_pointEventTranslator.CurrentDrawingButton);
                    }
                    else if (SourceDevice == Devices.TouchPad)
                    {
                        _ignoredTouchPadDeviceHandle = _pointEventTranslator.SourceDeviceHandle;
                    }
                    CancelCaptureByGestureTimeout();
                    if ((SourceDevice & Devices.TouchDevice) != 0)
                    {
                        _pointEventTranslator.ResetTouchDeviceSource();
                        _inputProvider.ReleaseCurrentTouchSource();
                    }
                }
                catch
                {
                    State = CaptureState.Ready;
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                }
            });
        }

        private bool ShouldIgnoreTouchPadInput(InputPointsEventArgs e)
        {
            return e.PointSource == Devices.TouchPad &&
                _ignoredTouchPadDeviceHandle != IntPtr.Zero &&
                _ignoredTouchPadDeviceHandle == e.DeviceHandle;
        }

        private bool ShouldBlockSingleFingerTouchStart(InputPointsEventArgs e)
        {
            if ((e.PointSource & Devices.TouchDevice) == 0 ||
                Mode == CaptureMode.Training ||
                State != CaptureState.Ready ||
                e.InputPointList == null ||
                e.InputPointList.Count != 1)
                return false;

            var inputPoint = e.InputPointList[0];
            switch (e.PointSource)
            {
                case Devices.TouchPad:
                    return !HasMatchingConditionedSingleFingerTouchAction(Devices.TouchPad, System.Windows.Forms.Cursor.Position, inputPoint);
                case Devices.TouchScreen:
                    return HasOptedInSingleFingerTouchScreenAction(inputPoint.Point) &&
                        !HasExecutableOptedInSingleFingerTouchScreenAction(inputPoint.Point, inputPoint);
                default:
                    return false;
            }
        }

        private bool HasSingleFingerTouchAction(Devices sourceDevice, Point captureStartPoint)
        {
            var applications = ApplicationManager.Instance.GetApplicationFromCapturePoint(captureStartPoint, out _)?.ToList() ?? new List<IApplication>();
            var actions = GetSingleFingerTouchActionsInApplications(applications, sourceDevice);
            if (actions.Count != 0)
                return true;

            return !applications.Any(app => app is GlobalApp) &&
                ApplicationManager.Instance.ShouldUseGlobalFallback(applications) &&
                GetSingleFingerTouchActionsInApplications(new[] { ApplicationManager.Instance.GetGlobalApplication() }, sourceDevice).Count != 0;
        }

        private bool HasOptedInSingleFingerTouchScreenAction(Point captureStartPoint)
        {
            var applications = ApplicationManager.Instance.GetApplicationFromCapturePoint(captureStartPoint, out _)?.ToList() ?? new List<IApplication>();

            if (GetConditionedSingleFingerTouchActionsInApplications(applications, Devices.TouchScreen).Count != 0)
                return true;

            if (applications.Any(app => app is UserApp) &&
                GetSingleFingerTouchActionsInApplications(applications, Devices.TouchScreen).Count != 0)
            {
                return true;
            }

            return !applications.Any(app => app is GlobalApp) &&
                ApplicationManager.Instance.ShouldUseGlobalFallback(applications) &&
                GetConditionedSingleFingerTouchActionsInApplications(new[] { ApplicationManager.Instance.GetGlobalApplication() }, Devices.TouchScreen).Count != 0;
        }

        private bool HasConditionedSingleFingerTouchAction(Devices sourceDevice, Point captureStartPoint)
        {
            var applications = ApplicationManager.Instance.GetApplicationFromCapturePoint(captureStartPoint, out _)?.ToList() ?? new List<IApplication>();
            var actions = GetConditionedSingleFingerTouchActionsInApplications(applications, sourceDevice);
            if (actions.Count != 0)
                return true;

            return !applications.Any(app => app is GlobalApp) &&
                ApplicationManager.Instance.ShouldUseGlobalFallback(applications) &&
                GetConditionedSingleFingerTouchActionsInApplications(new[] { ApplicationManager.Instance.GetGlobalApplication() }, sourceDevice).Count != 0;
        }

        private bool HasMatchingConditionedSingleFingerTouchAction(Devices sourceDevice, Point captureStartPoint, InputPoint point)
        {
            var applications = ApplicationManager.Instance.GetApplicationFromCapturePoint(captureStartPoint, out var targetWindow)?.ToList() ?? new List<IApplication>();
            var conditionPoints = new List<List<Point>>(new[] { new List<Point>(new[] { point.Point }) });
            var contactIdentifiers = new List<int>(new[] { point.ContactIdentifier });

            if (HasMatchingConditionedSingleFingerTouchAction(
                GetConditionedSingleFingerTouchActionsInApplications(applications, sourceDevice),
                conditionPoints,
                contactIdentifiers,
                targetWindow))
                return true;

            return !applications.Any(app => app is GlobalApp) &&
                ApplicationManager.Instance.ShouldUseGlobalFallback(applications) &&
                HasMatchingConditionedSingleFingerTouchAction(
                GetConditionedSingleFingerTouchActionsInApplications(new[] { ApplicationManager.Instance.GetGlobalApplication() }, sourceDevice),
                conditionPoints,
                contactIdentifiers,
                targetWindow);
        }

        private bool HasExecutableSingleFingerTouchAction(Devices sourceDevice, Point captureStartPoint, InputPoint point)
        {
            var applications = ApplicationManager.Instance.GetApplicationFromCapturePoint(captureStartPoint, out var targetWindow)?.ToList() ?? new List<IApplication>();
            var conditionPoints = new List<List<Point>>(new[] { new List<Point>(new[] { point.Point }) });
            var contactIdentifiers = new List<int>(new[] { point.ContactIdentifier });

            if (HasExecutableSingleFingerTouchAction(
                GetSingleFingerTouchActionsInApplications(applications, sourceDevice),
                conditionPoints,
                contactIdentifiers,
                targetWindow))
                return true;

            return !applications.Any(app => app is GlobalApp) &&
                ApplicationManager.Instance.ShouldUseGlobalFallback(applications) &&
                HasExecutableSingleFingerTouchAction(
                    GetSingleFingerTouchActionsInApplications(new[] { ApplicationManager.Instance.GetGlobalApplication() }, sourceDevice),
                    conditionPoints,
                    contactIdentifiers,
                    targetWindow);
        }

        private bool HasExecutableOptedInSingleFingerTouchScreenAction(Point captureStartPoint, InputPoint point)
        {
            var applications = ApplicationManager.Instance.GetApplicationFromCapturePoint(captureStartPoint, out var targetWindow)?.ToList() ?? new List<IApplication>();
            var conditionPoints = new List<List<Point>>(new[] { new List<Point>(new[] { point.Point }) });
            var contactIdentifiers = new List<int>(new[] { point.ContactIdentifier });

            if (HasExecutableSingleFingerTouchAction(
                GetConditionedSingleFingerTouchActionsInApplications(applications, Devices.TouchScreen),
                conditionPoints,
                contactIdentifiers,
                targetWindow))
            {
                return true;
            }

            if (applications.Any(app => app is UserApp) &&
                HasExecutableSingleFingerTouchAction(
                    GetSingleFingerTouchActionsInApplications(applications, Devices.TouchScreen),
                    conditionPoints,
                    contactIdentifiers,
                    targetWindow))
            {
                return true;
            }

            return !applications.Any(app => app is GlobalApp) &&
                ApplicationManager.Instance.ShouldUseGlobalFallback(applications) &&
                HasExecutableSingleFingerTouchAction(
                    GetConditionedSingleFingerTouchActionsInApplications(new[] { ApplicationManager.Instance.GetGlobalApplication() }, Devices.TouchScreen),
                    conditionPoints,
                    contactIdentifiers,
                    targetWindow);
        }

        private bool HasMatchingConditionedSingleFingerTouchAction(IEnumerable<IAction> actions, List<List<Point>> conditionPoints, List<int> contactIdentifiers, SystemWindow targetWindow)
        {
            return actions.Any(action =>
                HasEnabledCommands(action) &&
                PluginManager.Instance.EvaluateCondition(action.Condition, conditionPoints, contactIdentifiers, targetWindow));
        }

        private bool HasExecutableSingleFingerTouchAction(IEnumerable<IAction> actions, List<List<Point>> conditionPoints, List<int> contactIdentifiers, SystemWindow targetWindow)
        {
            return actions.Any(action =>
                HasEnabledCommands(action) &&
                PluginManager.Instance.EvaluateCondition(action.Condition, conditionPoints, contactIdentifiers, targetWindow));
        }

        private static List<IAction> GetSingleFingerTouchActionsInApplications(IEnumerable<IApplication> applications, Devices sourceDevice)
        {
            return applications == null
                ? new List<IAction>()
                : applications.Where(app => !(app is IgnoredApp) && app.Actions != null)
                    .SelectMany(app => app.Actions)
                    .Where(action => IsSingleFingerTouchAction(action, sourceDevice) && HasEnabledCommands(action))
                    .ToList();
        }

        private static List<IAction> GetConditionedSingleFingerTouchActionsInApplications(IEnumerable<IApplication> applications, Devices sourceDevice)
        {
            return applications == null
                ? new List<IAction>()
                : applications.Where(app => !(app is IgnoredApp) && app.Actions != null)
                    .SelectMany(app => app.Actions)
                    .Where(action => IsConditionedSingleFingerTouchAction(action, sourceDevice) && HasEnabledCommands(action))
                    .ToList();
        }

        private static bool IsConditionedSingleFingerTouchAction(IAction action, Devices sourceDevice)
        {
            if (action == null ||
                (action.IgnoredDevices & sourceDevice) != 0 ||
                string.IsNullOrWhiteSpace(action.Condition))
                return false;

            return IsSingleFingerTouchAction(action, sourceDevice);
        }

        private static bool IsSingleFingerTouchAction(IAction action, Devices sourceDevice)
        {
            if (action == null || (action.IgnoredDevices & sourceDevice) != 0)
                return false;

            return action.ContinuousGesture != null
                ? action.ContinuousGesture.ContactCount == 1
                : IsSingleFingerGesture(action.GestureName);
        }

        private static bool IsSingleFingerGesture(string gestureName)
        {
            if (string.IsNullOrWhiteSpace(gestureName))
                return false;

            var gesture = GestureManager.Instance.Gestures.FirstOrDefault(g =>
                g != null && string.Equals(g.Name, gestureName, StringComparison.CurrentCulture));
            var firstPointPattern = gesture?.PointPatterns?.FirstOrDefault();
            return firstPointPattern?.Points != null && firstPointPattern.Points.Length == 1;
        }

        private static bool HasEnabledCommands(IAction action)
        {
            return action?.Commands != null && action.Commands.Any(command => command != null && command.IsEnabled);
        }

        private static bool ShouldStartInitialTimeout(Devices pointSource, CaptureState state)
        {
            return pointSource != Devices.TouchPad || state == CaptureState.Ready;
        }

        private void PostToCurrentContext(System.Action action)
        {
            if (_currentContext != null)
            {
                _currentContext.Post((state) => action(), null);
            }
            else
            {
                action();
            }
        }

        private static void ClickMouseButton(MouseActions button)
        {
            InputSimulator simulator = new InputSimulator();
            switch (button)
            {
                case MouseActions.Left:
                    simulator.Mouse.LeftButtonClick();
                    break;
                case MouseActions.Middle:
                    simulator.Mouse.MiddleButtonClick();
                    break;
                case MouseActions.Right:
                    simulator.Mouse.RightButtonClick();
                    break;
                case MouseActions.XButton1:
                    simulator.Mouse.XButtonClick(1);
                    break;
                case MouseActions.XButton2:
                    simulator.Mouse.XButtonClick(2);
                    break;
            }
        }

        private static void PressMouseButton(MouseActions button)
        {
            InputSimulator simulator = new InputSimulator();
            switch (button)
            {
                case MouseActions.Left:
                    simulator.Mouse.LeftButtonDown();
                    break;
                case MouseActions.Middle:
                    simulator.Mouse.MiddleButtonDown();
                    break;
                case MouseActions.Right:
                    simulator.Mouse.RightButtonDown();
                    break;
                case MouseActions.XButton1:
                    simulator.Mouse.XButtonDown(1);
                    break;
                case MouseActions.XButton2:
                    simulator.Mouse.XButtonDown(2);
                    break;
            }
        }

        private void CancelCaptureByInitialTimeout()
        {
            if (_pointsCaptured != null && _pointsCaptured.Count != 0)
            {
                var inputContacts = InputContacts;
                var points = inputContacts.Select(contact => new List<Point>(contact.Points)).ToList();
                var firstPoints = SourceDevice == Devices.TouchPad
                    ? new List<Point>() { _touchPadStartPoint }
                    : points.Select(p => p.FirstOrDefault()).ToList();
                OnCaptureCanceled(new PointsCapturedEventArgs(points, firstPoints));
                _pointsCaptured.Clear();
                _orderedContactIdentifiers = null;
            }

            CapturePressedVirtualKeys = new List<int>();
            State = CaptureState.Ready;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
        }

        private void CancelCaptureByGestureTimeout()
        {
            if (_pointsCaptured != null && _pointsCaptured.Count != 0)
            {
                var inputContacts = InputContacts;
                var points = inputContacts.Select(contact => new List<Point>(contact.Points)).ToList();
                var firstPoints = SourceDevice == Devices.TouchPad
                    ? new List<Point>() { _touchPadStartPoint }
                    : points.Select(p => p.FirstOrDefault()).ToList();
                OnCaptureCanceled(new PointsCapturedEventArgs(points, firstPoints));
                _pointsCaptured.Clear();
                _orderedContactIdentifiers = null;
            }
            else
            {
                OnCaptureEnded();
            }

            CapturePressedVirtualKeys = new List<int>();
            State = CaptureState.Ready;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
        }

        private bool TryBeginCapture(List<InputPoint> firstPoint)
        {
            CapturePressedVirtualKeys = CapturePressedVirtualKeysSnapshot();

            // Create capture args so we can notify subscribers that capture has started and allow them to cancel if they want.
            PointsCapturedEventArgs captureStartedArgs;
            if (SourceDevice == Devices.TouchPad)
            {
                _touchPadStartPoint = System.Windows.Forms.Cursor.Position;
                captureStartedArgs = new PointsCapturedEventArgs(firstPoint.Select(p => new List<Point>() { p.Point }).ToList(), new List<Point>() { _touchPadStartPoint });
            }
            else
            {
                captureStartedArgs = new PointsCapturedEventArgs(firstPoint.Select(p => p.Point).ToList());
            }
            OnCaptureStarted(captureStartedArgs);

            UpdateBlockTouchInputThreshold(Mode == CaptureMode.Normal ? captureStartedArgs.BlockTouchInputThreshold : 0);

            if (captureStartedArgs.Cancel)
            {
                if ((SourceDevice & Devices.TouchDevice) != 0)
                {
                    _pointEventTranslator.ResetTouchDeviceSource();
                    _inputProvider.ReleaseCurrentTouchSource();
                }
                CapturePressedVirtualKeys = new List<int>();
                return false;
            }

            State = CaptureState.CapturingInvalid;

            // Clear old gesture from point list so we can start adding the new captures points to the list 
            _pointsCaptured = new Dictionary<int, List<Point>>(firstPoint.Count);
            _orderedContactIdentifiers = new List<int>(firstPoint.Count);
            if (AppConfig.IsOrderByLocation)
            {
                foreach (var rawData in firstPoint.OrderBy(p => p.Point.X))
                {
                    if (!_pointsCaptured.ContainsKey(rawData.ContactIdentifier))
                    {
                        _pointsCaptured.Add(rawData.ContactIdentifier, new List<Point>(30));
                        _orderedContactIdentifiers.Add(rawData.ContactIdentifier);
                    }
                }
            }
            else
            {
                foreach (var rawData in firstPoint.OrderBy(p => p.ContactIdentifier))
                {
                    if (!_pointsCaptured.ContainsKey(rawData.ContactIdentifier))
                    {
                        _pointsCaptured.Add(rawData.ContactIdentifier, new List<Point>(30));
                        _orderedContactIdentifiers.Add(rawData.ContactIdentifier);
                    }
                }
            }
            AddPoint(firstPoint);
            StartGestureTimeout();
            return true;
        }

        private void StartGestureTimeout()
        {
            var gestureTimeout = AppConfig.GestureTimeout;
            if (gestureTimeout <= 0)
                return;

            if (_gestureTimeoutTimer == null)
            {
                _gestureTimeoutTimer = new System.Threading.Timer(GestureTimeoutCallback, null, Timeout.Infinite, Timeout.Infinite);
            }
            _gestureTimeoutTimer.Change(gestureTimeout, Timeout.Infinite);
        }

        private void EndCapture()
        {
            if (_gestureTimeoutTimer != null)
                _gestureTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);

            var inputContacts = InputContacts;
            var orderedPoints = inputContacts.Select(contact => new List<Point>(contact.Points)).ToList();

            // Create points capture event args, to be used to send off to event subscribers or to simulate original Point event
            PointsCapturedEventArgs pointsInformation = SourceDevice == Devices.TouchPad ?
                new PointsCapturedEventArgs(orderedPoints, new List<Point>() { _touchPadStartPoint }) :
                new PointsCapturedEventArgs(orderedPoints, orderedPoints.Select(p => p.FirstOrDefault()).ToList());

            // Notify subscribers that capture has ended （draw end）
            OnCaptureEnded();
            State = CaptureState.Ready;

            // Notify PointsCaptured event subscribers that points have been captured.
            //CaptureWindow GetGestureName
            OnBeforePointsCaptured(pointsInformation);

            if (pointsInformation.Cancel)
            {
                if ((SourceDevice & Devices.TouchDevice) != 0)
                {
                    _pointEventTranslator.ResetTouchDeviceSource();
                    _inputProvider.ReleaseCurrentTouchSource();
                }
                _pointsCaptured.Clear();
                _orderedContactIdentifiers = null;
                CapturePressedVirtualKeys = new List<int>();
                return;
            }

            if (ShouldRecordTrainingGesture())
            {
                _pointPatternCache.Clear();
                _pointPatternCache.Add(new PointPattern(orderedPoints));

                if (!NamedPipe.SendMessageAsync(IpcCommands.GotGesture, Constants.ControlPanel, _pointPatternCache.Select(p => p.Points).ToArray(), false).Result)
                    Mode = CaptureMode.Normal;
            }

            // Fire recognized event if we found a gesture match, otherwise throw not recognized event
            if (GestureManager.Instance.GestureName != null)
            {
                List<Point> capturedPoints = SourceDevice == Devices.TouchPad ? new List<Point>() { _touchPadStartPoint } : pointsInformation.FirstCapturedPoints;
                OnGestureRecognized(new RecognitionEventArgs(
                    GestureManager.Instance.GestureName,
                    pointsInformation.Points,
                    capturedPoints,
                    inputContacts.Select(contact => contact.ContactIdentifier).ToList(),
                    new List<int>(CapturePressedVirtualKeys)));
            }
            else if (ShouldPlayUnrecognizedGestureSound())
            {
                PlayUnrecognizedGestureSound();
            }

            OnAfterPointsCaptured(pointsInformation);

            if ((SourceDevice & Devices.TouchDevice) != 0)
            {
                _pointEventTranslator.ResetTouchDeviceSource();
                _inputProvider.ReleaseCurrentTouchSource();
            }

            _pointsCaptured.Clear();
            _orderedContactIdentifiers = null;
            CapturePressedVirtualKeys = new List<int>();
        }

        private bool ShouldRecordTrainingGesture()
        {
            if (Mode != CaptureMode.Training)
                return false;

            if (!IsSinglePointTap())
                return true;

            return SourceDevice == Devices.TouchScreen;
        }

        private bool IsSinglePointTap()
        {
            return InputContacts.Count == 1 && InputContacts[0].Points.Count == 1;
        }

        private static List<int> CapturePressedVirtualKeysSnapshot()
        {
            var pressedKeys = new List<int>(16);
            for (int virtualKey = 1; virtualKey <= 0xFE; virtualKey++)
            {
                if ((GetAsyncKeyState(virtualKey) & 0x8000) != 0)
                    pressedKeys.Add(virtualKey);
            }
            return pressedKeys;
        }

        private bool ShouldPlayUnrecognizedGestureSound()
        {
            return Mode == CaptureMode.Normal &&
                AppConfig.PlaySoundOnUnrecognizedGesture &&
                !GestureManager.Instance.IsWaitingForCompositeGesture;
        }

        private void PlayUnrecognizedGestureSound()
        {
            string customSoundPath = AppConfig.UnrecognizedGestureSoundPath;
            try
            {
                var soundPlayer = GetUnrecognizedGestureSoundPlayer(customSoundPath);
                if (soundPlayer != null)
                {
                    soundPlayer.Play();
                    return;
                }
            }
            catch
            {
            }

            try
            {
                SystemSounds.Beep.Play();
            }
            catch
            {
            }
        }

        private SoundPlayer GetUnrecognizedGestureSoundPlayer(string customSoundPath)
        {
            if (string.IsNullOrWhiteSpace(customSoundPath) || !File.Exists(customSoundPath))
                return null;

            if (_unrecognizedGestureSoundPlayer == null ||
                !string.Equals(_unrecognizedGestureSoundPath, customSoundPath, StringComparison.OrdinalIgnoreCase))
            {
                _unrecognizedGestureSoundPlayer?.Dispose();
                _unrecognizedGestureSoundPath = customSoundPath;
                _unrecognizedGestureSoundPlayer = new SoundPlayer(customSoundPath);
                _unrecognizedGestureSoundPlayer.Load();
            }

            return _unrecognizedGestureSoundPlayer;
        }

        //private void CancelCapture(int num)
        //{
        //    // Notify subscribers that gesture capture has been canceled
        //    OnCaptureCanceled(new PointsCapturedEventArgs(new List<List<Point>>(_pointsCaptured.Values)));
        //}

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void AddPoint(List<InputPoint> point)
        {
            bool getNewPoint = false;
            int threshold = AppConfig.MinimumPointDistance;
            foreach (var p in point)
            {
                // Don't accept point if it's within specified distance of last point unless it's the first point
                if (_pointsCaptured.TryGetValue(p.ContactIdentifier, out List<Point> stroke))
                {
                    if (stroke.Count != 0)
                    {
                        if (PointPatternMath.GetDistance(stroke.Last(), p.Point) < threshold)
                            continue;

                        if (State == CaptureState.CapturingInvalid)
                            State = CaptureState.Capturing;
                    }

                    getNewPoint = true;
                    // Add point to captured points list
                    stroke.Add(p.Point);
                }
            }
            if (getNewPoint)
            {
                var currentPointsByContact = point
                    .GroupBy(p => p.ContactIdentifier)
                    .ToDictionary(group => group.Key, group => group.Last().Point);
                var inputContacts = InputContacts;
                var latestPoints = inputContacts
                    .Select(capturedPoint => currentPointsByContact.TryGetValue(capturedPoint.ContactIdentifier, out var currentPoint)
                        ? currentPoint
                        : capturedPoint.Points.Last())
                    .ToList();

                // Notify subscribers that point has been captured
                OnPointCaptured(new PointsCapturedEventArgs(
                    inputContacts.Select(contact => new List<Point>(contact.Points)).ToList(),
                    latestPoints));
            }
        }



        #endregion

        #region Public Methods

        public void Load()
        {
            // Shortcut method to control singleton instantiation
            _currentContext = SynchronizationContext.Current ?? _currentContext;
        }

        public void ToggleUserDisablePointCapture()
        {
            // Toggle User selected Gesture Disabling
            // Added UserDisabled to CaptureState enum since Ready and Disabled can't be used
            // due to the existing logic of Enabling/Disabling for UI/menu popup/etc.
            // The reason I had to set state to Ready if !UserDisabled was due to the sequence of the tray events.
            // I originally had to set to Disable since if you're in the popup it's disabled, however, the popup onclose
            // fires before the menu item's code, so it was back to Ready before this block was executed.  Although, it probably 
            // makes more sense to set it to Ready in the event this is called from another location.
            Mode = Mode == CaptureMode.UserDisabled ? CaptureMode.Normal : CaptureMode.UserDisabled;
        }

        #endregion
    }
}
