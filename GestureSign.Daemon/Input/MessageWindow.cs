using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Common.Log;
using GestureSign.Daemon.Native;
using Microsoft.Win32;

namespace GestureSign.Daemon.Input
{
    public class MessageWindow : NativeWindow, IDisposable
    {
        private Screen _currentScr;
        private const int SourceDeviceStaleTimeout = 1000;
        private const int InvalidTouchSourceStaleTimeout = 250;

        private static readonly HandleRef HwndMessage = new HandleRef(null, new IntPtr(-3));

        private List<RawData> _outputTouchs = new List<RawData>(1);
        private List<RawData> _lastSourceDeviceOutput = new List<RawData>(1);
        private int _requiringContactCount;
        private Dictionary<IntPtr, ushort> _validDevices = new Dictionary<IntPtr, ushort>();
        private Dictionary<IntPtr, bool> _touchSuppressingPenDevices = new Dictionary<IntPtr, bool>();
        private Dictionary<IntPtr, Screen> _touchScreenDeviceScreens = new Dictionary<IntPtr, Screen>();
        private readonly SynchronizationContext _messageContext;

        private Devices _sourceDevice;
        private IntPtr _sourceDeviceHandle;
        private int _lastSourceDeviceInputTick;
        private List<ushort> _registeredDeviceList = new List<ushort>(1);
        private int? _penLastActivity;
        private bool _ignoreTouchInputWhenUsingPen;
        private DeviceStates _penGestureButton;
        private DeviceStates _penGestureSetting;
        private bool _disposed;
        private int _displaySettingsRefreshQueued;
        private int _displaySettingsRefreshPending;
        private static readonly object SourceArbitrationLogLock = new object();
        private static readonly HashSet<string> SourceArbitrationLogKeys = new HashSet<string>();

        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public MessageWindow()
        {
            _messageContext = SynchronizationContext.Current;
            CreateWindow();
            UpdateRegistration();
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        ~MessageWindow()
        {
            Dispose(false);
        }

        public bool CreateWindow()
        {
            if (Handle == IntPtr.Zero)
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                CreateHandle(new CreateParams
                {
                    Style = 0,
                    ExStyle = WS_EX_NOACTIVATE,
                    ClassStyle = 0,
                    Caption = "GSMessageWindow",
                    Parent = (IntPtr)HwndMessage
                });
            }
            return Handle != IntPtr.Zero;
        }

        public void DestroyWindow()
        {
            DestroyWindow(true, IntPtr.Zero);
        }

        public override void DestroyHandle()
        {
            DestroyWindow(false, IntPtr.Zero);
            base.DestroyHandle();
        }

        protected override void OnHandleChange()
        {
            try
            {
                UpdateRegistration();
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
                try
                {
                    _validDevices.Clear();
                    _touchSuppressingPenDevices.Clear();
                    _touchScreenDeviceScreens.Clear();
                    ResetSourceDevice(true);
                }
                catch (Exception resetException)
                {
                    Logging.LogException(resetException);
                }
            }
            base.OnHandleChange();
        }

        private bool GetInvokeRequired(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            int pid;
            var hwndThread = NativeMethods.GetWindowThreadProcessId(new HandleRef(this, hWnd), out pid);
            var currentThread = NativeMethods.GetCurrentThreadId();
            return (hwndThread != currentThread);
        }

        private void DestroyWindow(bool destroyHwnd, IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                hWnd = Handle;
            }

            if (GetInvokeRequired(hWnd))
            {
                NativeMethods.PostMessage(new HandleRef(this, hWnd), NativeMethods.WmClose, 0, 0);
                return;
            }

            lock (this)
            {
                if (destroyHwnd)
                {
                    base.DestroyHandle();
                }
            }
        }

        public void UpdateRegistration()
        {
            if (_disposed || Handle == IntPtr.Zero)
                return;

            ResetSourceDevice(true);
            _ignoreTouchInputWhenUsingPen = AppConfig.IgnoreTouchInputWhenUsingPen;
            var penSetting = AppConfig.PenGestureButton;
            _penGestureSetting = penSetting;
            _penGestureButton = penSetting & (DeviceStates.Invert | DeviceStates.RightClickButton);

            _validDevices.Clear();
            _touchSuppressingPenDevices.Clear();
            _touchScreenDeviceScreens.Clear();

            UpdateRegisterState(AppConfig.RegisterTouchScreen, NativeMethods.TouchScreenUsage);
            UpdateRegisterState(_ignoreTouchInputWhenUsingPen || (penSetting & (DeviceStates.InRange | DeviceStates.Tip)) != 0, NativeMethods.PenUsage);
            UpdateRegisterState(AppConfig.RegisterTouchPad, NativeMethods.TouchPadUsage);
        }

        public void ReleaseCurrentTouchSource()
        {
            if ((_sourceDevice & Devices.TouchDevice) == 0)
                return;

            ResetSourceDevice(true);
        }

        private void UpdateRegisterState(bool register, ushort usage)
        {
            try
            {
                if (register)
                {
                    RegisterDevice(usage);
                }
                else
                {
                    UnregisterDevice(usage);
                }
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
            }
        }

        private void RegisterDevice(ushort usage)
        {
            UnregisterDevice(usage);

            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];

            rid[0].usUsagePage = NativeMethods.DigitizerUsagePage;
            rid[0].usUsage = usage;
            rid[0].dwFlags = NativeMethods.RIDEV_INPUTSINK | NativeMethods.RIDEV_DEVNOTIFY;
            rid[0].hwndTarget = Handle;

            if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to register raw input device usage 0x{usage:X2}.");
            }
            _registeredDeviceList.Add(usage);
        }

        private void UnregisterDevice(ushort usage)
        {
            if (_registeredDeviceList.Contains(usage))
            {
                RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];

                rid[0].usUsagePage = NativeMethods.DigitizerUsagePage;
                rid[0].usUsage = usage;
                rid[0].dwFlags = NativeMethods.RIDEV_REMOVE;
                rid[0].hwndTarget = IntPtr.Zero;

                if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to unregister raw input device usage 0x{usage:X2}.");
                }
                _registeredDeviceList.Remove(usage);
            }
        }

        private bool ValidateDevice(IntPtr hDevice, out ushort usage)
        {
            usage = 0;
            _touchSuppressingPenDevices.Remove(hDevice);
            uint pcbSize = 0;
            if (!TryGetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, IntPtr.Zero, ref pcbSize, "query device info size"))
                return false;

            if (pcbSize <= 0)
                return false;

            IntPtr pInfo = Marshal.AllocHGlobal((int)pcbSize);
            using (new SafeUnmanagedMemoryHandle(pInfo))
            {
                HidDevice.InitializeRawDeviceInfoBuffer(pInfo);
                if (!TryGetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, pInfo, ref pcbSize, "read device info"))
                    return false;

                var info = (RID_DEVICE_INFO)Marshal.PtrToStructure(pInfo, typeof(RID_DEVICE_INFO));
                if (info.dwType != NativeMethods.RIM_TYPEHID || info.hid.usUsagePage != NativeMethods.DigitizerUsagePage)
                    return true;

                switch (info.hid.usUsage)
                {
                    case NativeMethods.TouchPadUsage:
                    case NativeMethods.TouchScreenUsage:
                    case NativeMethods.PenUsage:
                        break;
                    default:
                        LogValidatedDevice(hDevice, info.hid.usUsagePage, info.hid.usUsage, null, "ignored unsupported digitizer usage");
                        return true;
                }

                if (!TryGetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize, "query device name size"))
                    return false;

                if (pcbSize <= 0)
                    return false;

                IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
                using (new SafeUnmanagedMemoryHandle(pData))
                {
                    if (!TryGetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICENAME, pData, ref pcbSize, "read device name"))
                        return false;

                    string deviceName = Marshal.PtrToStringAnsi(pData);
                    bool allowTouchSuppression = ShouldAllowPenTouchSuppression(info.hid.usUsage, deviceName);

                    if (ShouldIgnoreValidatedDevice(info.hid.usUsage, deviceName))
                    {
                        LogValidatedDevice(hDevice, info.hid.usUsagePage, info.hid.usUsage, deviceName, "ignored device path/name filter");
                        return true;
                    }

                    if (info.hid.usUsage == NativeMethods.PenUsage)
                        _touchSuppressingPenDevices[hDevice] = allowTouchSuppression;

                    LogValidatedDevice(
                        hDevice,
                        info.hid.usUsagePage,
                        info.hid.usUsage,
                        deviceName,
                        info.hid.usUsage == NativeMethods.PenUsage && !allowTouchSuppression
                            ? "accepted with touch-suppression disabled for this pen path"
                            : "accepted");
                    usage = info.hid.usUsage;
                    return true;
                }
            }
        }

        private static void LogValidatedDevice(IntPtr hDevice, ushort usagePage, ushort usage, string deviceName, string result)
        {
            Logging.LogMessage(
                $"Raw digitizer device validation: result={result}, hDevice=0x{hDevice.ToInt64():X}, usagePage=0x{usagePage:X2}, usage={GetUsageName(usage)} (0x{usage:X2}), deviceName={FormatDeviceName(deviceName)}");
        }

        private static bool ShouldIgnoreValidatedDevice(ushort usage, string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return true;

            // Some Win11 tablet and third-party drivers expose standard HID touchpad,
            // touchscreen, or pen collections through a ROOT/VIRTUAL_DIGITIZER
            // device path. Allow those standard usages to continue into gesture capture
            // instead of rejecting them purely by path. Pen-triggered touch suppression
            // keeps its own narrower trust boundary below.
            if (usage == NativeMethods.TouchPadUsage ||
                usage == NativeMethods.TouchScreenUsage ||
                usage == NativeMethods.PenUsage)
                return false;

            return deviceName.IndexOf("VIRTUAL_DIGITIZER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   deviceName.IndexOf("ROOT", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldAllowPenTouchSuppression(ushort usage, string deviceName)
        {
            if (usage != NativeMethods.PenUsage || string.IsNullOrEmpty(deviceName))
                return false;

            return deviceName.IndexOf("VIRTUAL_DIGITIZER", StringComparison.OrdinalIgnoreCase) < 0 &&
                   deviceName.IndexOf("ROOT", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private bool CanSuppressTouchForPenDevice(IntPtr hDevice)
        {
            return _touchSuppressingPenDevices.TryGetValue(hDevice, out bool canSuppress) && canSuppress;
        }

        private static string GetUsageName(ushort usage)
        {
            switch (usage)
            {
                case NativeMethods.TouchPadUsage:
                    return "TouchPad";
                case NativeMethods.TouchScreenUsage:
                    return "TouchScreen";
                case NativeMethods.PenUsage:
                    return "Pen";
                default:
                    return "Unknown";
            }
        }

        private static string FormatDeviceName(string deviceName)
        {
            return string.IsNullOrWhiteSpace(deviceName) ? "<empty>" : deviceName;
        }

        private static bool ShouldSuppressTouchForPenState(DeviceStates state)
        {
            return (state & (DeviceStates.Tip | DeviceStates.RightClickButton | DeviceStates.Invert | DeviceStates.Eraser)) != 0;
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                case NativeMethods.WM_INPUT:
                    {
                        try
                        {
                            ProcessInputCommand(message.LParam);
                        }
                        catch (Exception ex)
                        {
                            Logging.LogException(ex);
                            ResetSourceDevice(true);
                        }
                        break;
                    }
                case NativeMethods.WM_INPUT_DEVICE_CHANGE:
                    {
                        _validDevices.Clear();
                        _touchSuppressingPenDevices.Clear();
                        _touchScreenDeviceScreens.Clear();
                        ResetSourceDevice(true);
                        break;
                    }
            }
            base.WndProc(ref message);
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            if (_disposed)
                return;

            if (Interlocked.Exchange(ref _displaySettingsRefreshQueued, 1) != 0)
            {
                Interlocked.Exchange(ref _displaySettingsRefreshPending, 1);
                return;
            }

            if (_messageContext == null)
            {
                RefreshAfterDisplaySettingsChanged();
                return;
            }

            try
            {
                _messageContext.Post(_ => RefreshAfterDisplaySettingsChanged(), null);
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
                RefreshAfterDisplaySettingsChanged();
            }
        }

        private void RefreshAfterDisplaySettingsChanged()
        {
            try
            {
                if (_disposed)
                    return;

                UpdateRegistration();
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
                try
                {
                    _validDevices.Clear();
                    _touchScreenDeviceScreens.Clear();
                    ResetSourceDevice(true);
                }
                catch (Exception resetException)
                {
                    Logging.LogException(resetException);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _displaySettingsRefreshQueued, 0);
                if (Interlocked.Exchange(ref _displaySettingsRefreshPending, 0) != 0)
                    SystemEvents_DisplaySettingsChanged(this, EventArgs.Empty);
            }
        }

        private void CheckLastError()
        {
            int errCode = Marshal.GetLastWin32Error();
            if (errCode != 0)
            {
                throw new Win32Exception(errCode);
            }
        }

        private void ResetSourceDevice()
        {
            ResetSourceDevice(false);
        }

        private void ResetSourceDevice(bool notifyRelease)
        {
            _currentScr = null;
            if (notifyRelease)
                ReleaseSourceDevice();
            _sourceDevice = Devices.None;
            _sourceDeviceHandle = IntPtr.Zero;
            _requiringContactCount = 0;
            _outputTouchs = new List<RawData>(1);
            _lastSourceDeviceOutput = new List<RawData>(1);
            _lastSourceDeviceInputTick = 0;
        }

        private void ReleaseSourceDevice()
        {
            if (_sourceDevice == Devices.None || PointsIntercepted == null)
                return;

            var sourceOutput = _lastSourceDeviceOutput.Count != 0 ? _lastSourceDeviceOutput : _outputTouchs;
            if (sourceOutput.Count == 0 || sourceOutput.TrueForAll(rd => rd.State == DeviceStates.None))
                return;

            var releaseOutput = sourceOutput
                .Select(rd => new RawData(DeviceStates.None, rd.ContactIdentifier, rd.RawPoints))
                .ToList();
            PointsIntercepted(this, new RawPointsDataMessageEventArgs(releaseOutput, _sourceDevice, _sourceDeviceHandle));
        }

        private bool TryAcceptSourceDevice(Devices sourceDevice, IntPtr sourceDeviceHandle)
        {
            if (_sourceDevice == Devices.None)
            {
                _sourceDevice = sourceDevice;
                _sourceDeviceHandle = sourceDeviceHandle;
                _lastSourceDeviceInputTick = Environment.TickCount;
                return true;
            }

            if (_sourceDevice == sourceDevice && _sourceDeviceHandle == sourceDeviceHandle)
            {
                // Keep ownership alive only when a full output frame completes; partial raw packets
                // should still age out so another touch source can recover from a stuck stream.
                return true;
            }

            var captureState = Input.PointCapture.Instance.State;
            int staleTimeout = captureState == Common.Input.CaptureState.CapturingInvalid
                ? InvalidTouchSourceStaleTimeout
                : SourceDeviceStaleTimeout;
            int elapsedSinceLastFrame = _lastSourceDeviceInputTick == 0
                ? int.MaxValue
                : unchecked(Environment.TickCount - _lastSourceDeviceInputTick);
            bool sourceTimedOut = _lastSourceDeviceInputTick != 0 && elapsedSinceLastFrame > staleTimeout;
            bool touchSourceStillActive = (_sourceDevice & Devices.TouchDevice) != 0 &&
                (captureState == Common.Input.CaptureState.Capturing ||
                 captureState == Common.Input.CaptureState.CapturingInvalid ||
                 captureState == Common.Input.CaptureState.TriggerFired);
            if (touchSourceStillActive && !sourceTimedOut)
            {
                LogSourceArbitration("blocked-active",
                    _sourceDevice,
                    _sourceDeviceHandle,
                    sourceDevice,
                    sourceDeviceHandle,
                    captureState,
                    staleTimeout,
                    elapsedSinceLastFrame);
                return false;
            }

            if (sourceTimedOut)
            {
                LogSourceArbitration("takeover-timeout",
                    _sourceDevice,
                    _sourceDeviceHandle,
                    sourceDevice,
                    sourceDeviceHandle,
                    captureState,
                    staleTimeout,
                    elapsedSinceLastFrame);
                ResetSourceDevice(true);
                _sourceDevice = sourceDevice;
                _sourceDeviceHandle = sourceDeviceHandle;
                _lastSourceDeviceInputTick = Environment.TickCount;
                return true;
            }

            return false;
        }

        private static void LogSourceArbitration(string result, Devices currentSource, IntPtr currentHandle, Devices nextSource, IntPtr nextHandle, CaptureState captureState, int staleTimeout, int elapsedSinceLastFrame)
        {
            string key = $"{result}|{currentSource}|0x{currentHandle.ToInt64():X}|{nextSource}|0x{nextHandle.ToInt64():X}|{captureState}|{staleTimeout}";
            lock (SourceArbitrationLogLock)
            {
                if (!SourceArbitrationLogKeys.Add(key))
                    return;
            }

            Logging.LogMessage(
                $"Touch source arbitration: result={result}, currentSource={currentSource}, currentHandle=0x{currentHandle.ToInt64():X}, nextSource={nextSource}, nextHandle=0x{nextHandle.ToInt64():X}, captureState={captureState}, staleTimeoutMs={staleTimeout}, elapsedSinceLastFrameMs={elapsedSinceLastFrame}");
        }

        private bool IsCurrentScreenValid()
        {
            if (!IsUsableScreen(_currentScr))
                return false;

            try
            {
                Screen[] screens;
                if (!TryGetScreens(out screens))
                    return false;

                foreach (Screen screen in screens)
                {
                    if (!IsUsableScreen(screen))
                        continue;

                    if (string.Equals(screen.DeviceName, _currentScr.DeviceName, StringComparison.OrdinalIgnoreCase) &&
                        screen.Bounds.Equals(_currentScr.Bounds))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
            }

            return false;
        }

        private bool TrySetCurrentScreenFromCursor(bool updateOrientation)
        {
            Screen screen = GetFallbackScreen();
            if (!IsUsableScreen(screen))
            {
                ResetSourceDevice(true);
                return false;
            }

            _currentScr = screen;
            if (updateOrientation)
                HidDevice.GetCurrentScreenOrientation();

            return true;
        }

        private static bool IsUsableScreen(Screen screen)
        {
            return screen != null && screen.Bounds.Width > 0 && screen.Bounds.Height > 0;
        }

        private static bool TryGetScreens(out Screen[] screens)
        {
            try
            {
                screens = Screen.AllScreens?.Where(IsUsableScreen).ToArray() ?? new Screen[0];
                return screens.Length > 0;
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
                screens = new Screen[0];
                return false;
            }
        }

        #region ProcessInput

        /// <summary>
        /// Processes WM_INPUT messages to retrieve information about any
        /// touch events that occur.
        /// </summary>
        /// <param name="LParam">The WM_INPUT message to process.</param>
        private void ProcessInputCommand(IntPtr LParam)
        {
            uint dwSize = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));

            // First call to GetRawInputData sets the value of dwSize
            // dwSize can then be used to allocate the appropriate amount of memore,
            // storing the pointer in "buffer".
            uint sizeResult = NativeMethods.GetRawInputData(LParam, NativeMethods.RID_INPUT, IntPtr.Zero, ref dwSize, headerSize);
            if (sizeResult == uint.MaxValue)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"GetRawInputData failed while querying input size. lParam=0x{LParam.ToInt64():X}");
            }

            if (dwSize == 0)
            {
                throw new InvalidOperationException($"GetRawInputData returned an empty input size. lParam=0x{LParam.ToInt64():X}");
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                // Check that buffer points to something, and if so,
                // call GetRawInputData again to fill the allocated memory
                // with information about the input
                if (buffer == IntPtr.Zero)
                {
                    throw new OutOfMemoryException($"Failed to allocate raw input buffer. size={dwSize}");
                }

                uint expectedSize = dwSize;
                uint readSize = NativeMethods.GetRawInputData(LParam, NativeMethods.RID_INPUT, buffer, ref dwSize, headerSize);
                if (readSize == uint.MaxValue)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"GetRawInputData failed while reading input. lParam=0x{LParam.ToInt64():X}, expectedSize={expectedSize}, reportedSize={dwSize}");
                }

                if (readSize != expectedSize)
                {
                    throw new InvalidOperationException($"GetRawInputData returned an unexpected size. lParam=0x{LParam.ToInt64():X}, expectedSize={expectedSize}, readSize={readSize}, reportedSize={dwSize}");
                }

                RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));

                ushort usage;
                if (!_validDevices.TryGetValue(raw.header.hDevice, out usage))
                {
                    if (ValidateDevice(raw.header.hDevice, out usage))
                        _validDevices.Add(raw.header.hDevice, usage);
                }

                if (usage == 0)
                    return;
                try
                {
                    if (usage == NativeMethods.PenUsage)
                    {
                        switch (_sourceDevice)
                        {
                            case Devices.TouchScreen:
                            case Devices.None:
                            case Devices.Pen:
                                break;
                            default:
                                return;
                        }

                        using (PenDevice penDevice = new PenDevice(buffer, ref raw))
                        {
                            DeviceStates state = penDevice.GetPenState();
                            if (_ignoreTouchInputWhenUsingPen && CanSuppressTouchForPenDevice(raw.header.hDevice))
                                _penLastActivity = ShouldSuppressTouchForPenState(state) ? Environment.TickCount : (int?)null;
                            else
                                _penLastActivity = null;

                            bool drawByTip = (_penGestureSetting & DeviceStates.Tip) != 0;
                            bool drawByHover = (_penGestureSetting & DeviceStates.InRange) != 0;
                            bool tipActive = (state & (DeviceStates.Eraser | DeviceStates.Tip)) != 0;
                            bool hoverActive = (state & DeviceStates.InRange) != 0;
                            bool activationPressed = (state & _penGestureButton) != 0;
                            bool hasActivationGate = _penGestureButton != 0;
                            bool hasDrawingMode = drawByTip || drawByHover;
                            bool isDrawingState = hasActivationGate
                                ? activationPressed && ((drawByTip && tipActive) || (drawByHover && hoverActive))
                                : (drawByTip && tipActive) || (drawByHover && hoverActive);

                            if (!hasDrawingMode)
                                return;

                            if (_sourceDevice == Devices.None || _sourceDevice == Devices.TouchScreen)
                            {
                                if (isDrawingState)
                                {
                                    if (!TrySetCurrentScreenFromCursor(true))
                                        return;
                                    if (!TryAcceptSourceDevice(Devices.Pen, raw.header.hDevice))
                                        return;
                                }
                                else
                                    return;
                            }
                            else if (_sourceDevice == Devices.Pen)
                            {
                                if (!isDrawingState)
                                {
                                    state = DeviceStates.None;
                                }
                            }
                            if (!IsCurrentScreenValid() && !TrySetCurrentScreenFromCursor(true))
                                return;
                            if (!penDevice.TryGetPhysicalMax(1))
                            {
                                ResetSourceDevice(true);
                                return;
                            }
                            Point point = penDevice.GetCoordinate(0, _currentScr);
                            _outputTouchs = new List<RawData>(1) { new RawData(state, 0, point) };
                        }
                    }
                    else if (usage == NativeMethods.TouchScreenUsage)
                    {
                        if (_penLastActivity != null && Environment.TickCount - _penLastActivity < 100)
                            return;
                        if (!TryAcceptSourceDevice(Devices.TouchScreen, raw.header.hDevice))
                            return;

                        using (TouchScreenDevice touchScreen = new TouchScreenDevice(buffer, ref raw))
                        {
                            HidNativeApi.HIDP_LINK_COLLECTION_NODE[] linkCollection = touchScreen.GetLinkCollectionNodes();
                            if (linkCollection.Length == 0)
                            {
                                ResetSourceDevice(true);
                                return;
                            }
                            if (!touchScreen.TryGetPhysicalMax(linkCollection.Length))
                            {
                                ResetSourceDevice(true);
                                return;
                            }

                            int contactCount;
                            int inferredContactCount = touchScreen.InferContactCount(linkCollection[0].NumberOfChildren);
                            if (!touchScreen.TryGetContactCount(out contactCount) ||
                                contactCount <= 0 ||
                                contactCount > inferredContactCount ||
                                contactCount < inferredContactCount)
                            {
                                // Some Win11 touchscreen drivers report ContactCount as the number of
                                // active touches while still placing active contacts in later logical
                                // slots. Parse the whole logical report width so those later contacts
                                // are not skipped just because earlier slots are inactive.
                                contactCount = inferredContactCount;
                            }

                            if (contactCount != 0)
                            {
                                _requiringContactCount = contactCount;
                                _outputTouchs = new List<RawData>(contactCount);
                            }
                            if (_requiringContactCount == 0)
                            {
                                ResetSourceDevice(true);
                                return;
                            }

                            if (_currentScr == null || !IsCurrentScreenValid())
                            {
                                TouchScreenDevice.GetCurrentScreenOrientation();
                                _currentScr = ResolveTouchScreen(raw.header.hDevice, touchScreen, linkCollection[0].NumberOfChildren);
                                if (!IsUsableScreen(_currentScr))
                                {
                                    ResetSourceDevice(true);
                                    return;
                                }
                            }

                            touchScreen.GetRawDatas(linkCollection[0].NumberOfChildren, _currentScr, ref _requiringContactCount, ref _outputTouchs);
                        }
                    }
                    else if (usage == NativeMethods.TouchPadUsage)
                    {
                        if (!TryAcceptSourceDevice(Devices.TouchPad, raw.header.hDevice))
                            return;
                        if (_currentScr == null || !IsCurrentScreenValid())
                        {
                            if (!TrySetCurrentScreenFromCursor(false))
                                return;
                        }

                        using (TouchPadDevice touchPad = new TouchPadDevice(buffer, ref raw))
                        {
                            HidNativeApi.HIDP_LINK_COLLECTION_NODE[] linkCollection = touchPad.GetLinkCollectionNodes();
                            if (linkCollection.Length == 0)
                            {
                                ResetSourceDevice(true);
                                return;
                            }
                            if (!touchPad.TryGetPhysicalMax(linkCollection.Length))
                            {
                                ResetSourceDevice(true);
                                return;
                            }

                            int contactCount;
                            int inferredContactCount = touchPad.InferContactCount(linkCollection[0].NumberOfChildren);
                            if (!touchPad.TryGetContactCount(out contactCount) ||
                                contactCount <= 0 ||
                                contactCount > inferredContactCount ||
                                contactCount < inferredContactCount)
                            {
                                // Some precision-touchpad drivers expose the logical touch slots in the
                                // HID report but report only the currently active count. Parse the full
                                // logical report width so active contacts in later slots are not skipped.
                                contactCount = inferredContactCount;
                            }

                            if (contactCount != 0)
                            {
                                _requiringContactCount = contactCount;
                                _outputTouchs = new List<RawData>(contactCount);
                            }
                            if (_requiringContactCount == 0)
                            {
                                ResetSourceDevice(true);
                                return;
                            }

                            touchPad.GetRawDatas(linkCollection[0].NumberOfChildren, _currentScr, ref _requiringContactCount, ref _outputTouchs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogException(ex);
                    ResetSourceDevice(true);
                    return;
                }

                if (_requiringContactCount == 0 && PointsIntercepted != null)
                {
                    PointsIntercepted(this, new RawPointsDataMessageEventArgs(_outputTouchs, _sourceDevice, _sourceDeviceHandle));
                    _lastSourceDeviceOutput = _outputTouchs;
                    _lastSourceDeviceInputTick = Environment.TickCount;
                    if (_outputTouchs.TrueForAll(rd => rd.State == DeviceStates.None))
                    {
                        ResetSourceDevice();
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool TryGetRawInputDeviceInfo(IntPtr hDevice, uint command, IntPtr data, ref uint size, string operation)
        {
            uint result = NativeMethods.GetRawInputDeviceInfo(hDevice, command, data, ref size);
            if (result != uint.MaxValue)
                return true;

            Logging.LogException(new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"GetRawInputDeviceInfo failed during {operation}. hDevice=0x{hDevice.ToInt64():X}, command=0x{command:X}, size={size}"));
            return false;
        }

        private Screen ResolveTouchScreen(IntPtr hDevice, TouchScreenDevice touchScreen, short numberOfChildren)
        {
            Screen[] screens;
            if (!TryGetScreens(out screens))
                return null;

            Screen screen;
            if (TryGetCachedTouchScreen(hDevice, out screen))
            {
                if (screens.Length == 1)
                    return screen;

                var foregroundMatchedScreen = GetTouchScreenFromForegroundTouchPoint(touchScreen, numberOfChildren, screens);
                if (foregroundMatchedScreen != null &&
                    (!string.Equals(foregroundMatchedScreen.DeviceName, screen.DeviceName, StringComparison.OrdinalIgnoreCase) ||
                     !foregroundMatchedScreen.Bounds.Equals(screen.Bounds)))
                {
                    _touchScreenDeviceScreens[hDevice] = foregroundMatchedScreen;
                    return foregroundMatchedScreen;
                }

                var physicalSizeMatchedScreen = touchScreen.GetScreenFromPhysicalSize(screens);
                if (physicalSizeMatchedScreen != null &&
                    (!string.Equals(physicalSizeMatchedScreen.DeviceName, screen.DeviceName, StringComparison.OrdinalIgnoreCase) ||
                     !physicalSizeMatchedScreen.Bounds.Equals(screen.Bounds)))
                {
                    _touchScreenDeviceScreens[hDevice] = physicalSizeMatchedScreen;
                    return physicalSizeMatchedScreen;
                }

                return screen;
            }

            if (screens.Length == 1)
            {
                _touchScreenDeviceScreens[hDevice] = screens[0];
                return screens[0];
            }

            screen = GetTouchScreenFromForegroundTouchPoint(touchScreen, numberOfChildren, screens);
            if (screen != null)
            {
                _touchScreenDeviceScreens[hDevice] = screen;
                return screen;
            }

            screen = touchScreen.GetScreenFromPhysicalSize(screens);
            if (screen != null)
            {
                _touchScreenDeviceScreens[hDevice] = screen;
                return screen;
            }

            // For multi-screen touch setups, a guessed fallback screen can mis-map the
            // whole stroke onto the wrong monitor. Skip this unreliable frame instead.
            return null;
        }

        private bool TryGetCachedTouchScreen(IntPtr hDevice, out Screen screen)
        {
            if (!_touchScreenDeviceScreens.TryGetValue(hDevice, out screen))
                return false;

            Screen[] screens;
            if (!TryGetScreens(out screens))
            {
                screen = null;
                return false;
            }

            foreach (Screen currentScreen in screens)
            {
                if (currentScreen.DeviceName == screen.DeviceName && currentScreen.Bounds.Equals(screen.Bounds))
                {
                    screen = currentScreen;
                    return true;
                }
            }

            _touchScreenDeviceScreens.Remove(hDevice);
            screen = null;
            return false;
        }

        private Screen GetTouchScreenFromForegroundTouchPoint(TouchScreenDevice touchScreen, short numberOfChildren, Screen[] screens)
        {
            Rectangle foregroundBounds;
            if (!TryGetForegroundWindowBounds(out foregroundBounds))
                return null;

            Screen matchedScreen = null;
            foreach (Screen screen in screens)
            {
                Point touchPoint;
                if (!touchScreen.TryGetFirstTipPoint(numberOfChildren, screen, out touchPoint) || !foregroundBounds.Contains(touchPoint))
                    continue;

                if (matchedScreen != null)
                    return null;

                matchedScreen = screen;
            }
            return matchedScreen;
        }

        private bool TryGetForegroundWindowBounds(out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
                return false;

            NativeMethods.RECT windowRect;
            if (!NativeMethods.GetWindowRect(foregroundWindow, out windowRect) ||
                windowRect.Right <= windowRect.Left ||
                windowRect.Bottom <= windowRect.Top)
            {
                return false;
            }

            bounds = Rectangle.FromLTRB(windowRect.Left, windowRect.Top, windowRect.Right, windowRect.Bottom);
            return true;
        }

        private Screen GetFallbackScreen()
        {
            try
            {
                Screen cursorScreen = Screen.FromPoint(Cursor.Position);
                if (IsUsableScreen(cursorScreen))
                    return cursorScreen;
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
            }

            try
            {
                IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
                if (foregroundWindow != IntPtr.Zero)
                {
                    Screen foregroundScreen = Screen.FromHandle(foregroundWindow);
                    if (IsUsableScreen(foregroundScreen))
                        return foregroundScreen;
                }
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
            }

            Screen[] screens;
            return TryGetScreens(out screens) ? screens[0] : null;
        }


        #endregion ProcessInput

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

            DestroyWindow();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
