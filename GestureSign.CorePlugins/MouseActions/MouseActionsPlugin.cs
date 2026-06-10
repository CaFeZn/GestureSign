using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins.MouseActions
{
    public class MouseActionsPlugin : IPlugin
    {
        #region Private Variables

        private MouseActionsUI _gui = null;
        private MouseActionsSettings _settings = null;
        private readonly object _heldButtonsLock = new object();
        private readonly HashSet<MouseActions> _heldButtons = new HashSet<MouseActions>();
        private bool _captureReleaseHandlersAttached;

        #endregion

        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Name"); }
        }

        public string Description
        {
            get { return GetDescription(); }
        }

        public object GUI
        {
            get { return _gui ?? (_gui = CreateGUI()); }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public MouseActionsUI TypedGUI
        {
            get { return (MouseActionsUI)GUI; }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Category"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon => IconSource.Mouse;

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo actionPoint)
        {
            if (_settings == null)
                return false;

            InputSimulator simulator = new InputSimulator();
            try
            {
                int buttonId = (_settings.MouseAction & MouseActions.XButton1) != 0 ? 1 : 2;
                var referencePoint = GetReferencePoint(_settings.ActionLocation, actionPoint);

                if (_settings.MouseAction.GetButtons() != 0)
                {
                    if (_settings.ActionLocation != ClickPositions.Current)
                        Cursor.Position = referencePoint;
                }

                switch (_settings.MouseAction)
                {
                    case MouseActions.HorizontalScroll:
                        simulator.Mouse.HorizontalScroll(_settings.ScrollAmount).Sleep(30);
                        return true;
                    case MouseActions.VerticalScroll:
                        simulator.Mouse.VerticalScroll(_settings.ScrollAmount).Sleep(30);
                        return true;
                    case MouseActions.MoveMouseTo:
                        Cursor.Position = _settings.MovePoint;
                        return true;
                    case MouseActions.MoveMouseBy:
                        referencePoint.Offset(_settings.MovePoint);
                        Cursor.Position = referencePoint;
                        break;
                    case MouseActions.XButton1Click:
                    case MouseActions.XButton2Click:
                        simulator.Mouse.XButtonClick(buttonId).Sleep(30);
                        break;
                    case MouseActions.XButton1DoubleClick:
                    case MouseActions.XButton2DoubleClick:
                        simulator.Mouse.XButtonDoubleClick(buttonId).Sleep(30);
                        break;
                    case MouseActions.XButton1Down:
                    case MouseActions.XButton2Down:
                        simulator.Mouse.XButtonDown(buttonId).Sleep(30);
                        TrackHeldButtonIfCapturing(_settings.MouseAction.GetButtons(), actionPoint);
                        break;
                    case MouseActions.XButton1Up:
                    case MouseActions.XButton2Up:
                        simulator.Mouse.XButtonUp(buttonId).Sleep(30);
                        ForgetHeldButton(_settings.MouseAction.GetButtons());
                        break;
                    default:
                        {
                            MethodInfo clickMethod = typeof(IMouseSimulator).GetMethod(_settings.MouseAction.ToString());
                            clickMethod.Invoke(simulator.Mouse, null);
                            Thread.Sleep(30);
                            TrackHeldButtonChangeIfNeeded(_settings.MouseAction, actionPoint);
                            break;
                        }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool Deserialize(string serializedData)
        {
            if (serializedData.Contains("ClickPosition"))
            {
                LegacyMouseActionsSettings legacySettings;
                bool flag = PluginHelper.DeserializeSettings(serializedData, out legacySettings);
                _settings = new MouseActionsSettings()
                {
                    MouseAction = legacySettings.MouseAction.ToNewMouseActions(),
                    ActionLocation = legacySettings.ClickPosition.ToClickPositions(),
                    MovePoint = legacySettings.MovePoint,
                    ScrollAmount = legacySettings.ScrollAmount
                };
                return flag;
            }
            return PluginHelper.DeserializeSettings(serializedData, out _settings);
        }

        public string Serialize()
        {
            if (_gui != null)
                _settings = _gui.Settings;

            if (_settings == null)
                _settings = new MouseActionsSettings();

            return PluginHelper.SerializeSettings(_settings);
        }

        #endregion

        #region Private Methods

        private Point GetReferencePoint(ClickPositions position, PointInfo actionPoint)
        {
            Point referencePoint;
            switch (position)
            {
                case ClickPositions.LastUp:
                    referencePoint = actionPoint.Points.Last().Last();
                    break;
                case ClickPositions.LastDown:
                    referencePoint = actionPoint.Points.Last().First();
                    break;
                case ClickPositions.FirstUp:
                    referencePoint = actionPoint.Points.First().Last();
                    break;
                case ClickPositions.FirstDown:
                    referencePoint = actionPoint.Points.First().First();
                    break;
                case ClickPositions.Custom:
                    return _settings.MovePoint;
                default:
                    referencePoint = Cursor.Position;
                    break;
            }
            return referencePoint;

        }

        private MouseActionsUI CreateGUI()
        {
            MouseActionsUI newGUI = new MouseActionsUI();

            newGUI.Loaded += (o, e) =>
            {
                TypedGUI.Settings = _settings;
            };

            return newGUI;
        }

        private void TrackHeldButtonChangeIfNeeded(MouseActions action, PointInfo actionPoint)
        {
            switch (action.GetActions())
            {
                case MouseActions.Down:
                    TrackHeldButtonIfCapturing(action.GetButtons(), actionPoint);
                    break;
                case MouseActions.Up:
                    ForgetHeldButton(action.GetButtons());
                    break;
            }
        }

        private void TrackHeldButtonIfCapturing(MouseActions button, PointInfo actionPoint)
        {
            if (button == MouseActions.None ||
                !WasTriggeredDuringActiveCapture(actionPoint))
                return;

            lock (_heldButtonsLock)
            {
                _heldButtons.Add(button);
            }

            AttachCaptureReleaseHandlers();
        }

        private void ForgetHeldButton(MouseActions button)
        {
            lock (_heldButtonsLock)
            {
                _heldButtons.Remove(button);
            }
        }

        private void AttachCaptureReleaseHandlers()
        {
            if (_captureReleaseHandlersAttached || HostControl?.PointCapture == null)
                return;

            HostControl.PointCapture.CaptureEnded += PointCapture_CaptureEnded;
            HostControl.PointCapture.CaptureCanceled += PointCapture_CaptureCanceled;
            _captureReleaseHandlersAttached = true;
        }

        private static bool WasTriggeredDuringActiveCapture(PointInfo actionPoint)
        {
            if (actionPoint == null)
                return false;

            switch (actionPoint.CaptureStateAtTrigger)
            {
                case GestureSign.Common.Input.CaptureState.Capturing:
                case GestureSign.Common.Input.CaptureState.CapturingInvalid:
                case GestureSign.Common.Input.CaptureState.TriggerFired:
                    return true;
            }

            return false;
        }

        private void PointCapture_CaptureEnded(object sender, EventArgs e)
        {
            ReleaseHeldButtons();
        }

        private void PointCapture_CaptureCanceled(object sender, GestureSign.Common.Input.PointsCapturedEventArgs e)
        {
            ReleaseHeldButtons();
        }

        private void ReleaseHeldButtons()
        {
            MouseActions[] buttons;
            lock (_heldButtonsLock)
            {
                if (_heldButtons.Count == 0)
                    return;

                buttons = _heldButtons.ToArray();
                _heldButtons.Clear();
            }

            InputSimulator simulator = new InputSimulator();
            foreach (var button in buttons)
            {
                ReleaseButton(simulator, button);
            }
        }

        private static void ReleaseButton(InputSimulator simulator, MouseActions button)
        {
            switch (button)
            {
                case MouseActions.LeftButton:
                    simulator.Mouse.LeftButtonUp();
                    break;
                case MouseActions.MiddleButton:
                    simulator.Mouse.MiddleButtonUp();
                    break;
                case MouseActions.RightButton:
                    simulator.Mouse.RightButtonUp();
                    break;
                case MouseActions.XButton1:
                    simulator.Mouse.XButtonUp(1);
                    break;
                case MouseActions.XButton2:
                    simulator.Mouse.XButtonUp(2);
                    break;
            }
        }

        private string GetDescription()
        {
            switch (_settings.MouseAction)
            {
                case MouseActions.HorizontalScroll:
                    return
                        String.Format(
                            LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.HorizontalScroll"),
                            (_settings.ScrollAmount >= 0
                                ? LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Right")
                                : LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Left")),
                            Math.Abs(_settings.ScrollAmount));
                case MouseActions.VerticalScroll:
                    return
                        String.Format(
                            LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.VerticalScroll"),
                            (_settings.ScrollAmount >= 0
                                ? LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Up")
                                : LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Down")),
                            Math.Abs(_settings.ScrollAmount));
                case MouseActions.MoveMouseBy:
                    return LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.MoveMouseBy") + _settings.MovePoint;
                case MouseActions.MoveMouseTo:
                    return LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.MoveMouseTo") + _settings.MovePoint;
            }

            string button, action, location;
            MouseActionDescription.ButtonDescription.TryGetValue(_settings.MouseAction.GetButtons(), out button);
            MouseActionDescription.DescriptionDict.TryGetValue(_settings.MouseAction.GetActions(), out action);
            ClickPositionDescription.DescriptionDict.TryGetValue(_settings.ActionLocation, out location);
            return string.Format("{0} {1} {2}", location, action, button);
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
