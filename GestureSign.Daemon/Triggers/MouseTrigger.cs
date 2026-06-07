using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using ManagedWinapi.Hooks;
using System.Collections.Generic;
using System.Linq;

namespace GestureSign.Daemon.Triggers
{
    class MouseTrigger : Trigger
    {
        public MouseTrigger()
        {
            PointCapture.Instance.MouseHook.MouseDown += MouseHook_MouseDown;
            PointCapture.Instance.MouseHook.MouseUp += MouseHook_MouseUp;
            PointCapture.Instance.MouseHook.MouseWheel += MouseHook_MouseWheel;
        }

        private void MouseHook_MouseWheel(LowLevelMouseMessage e, ref bool handled)
        {
            MouseActions wheelAction = e.MouseData > 0 ? MouseActions.WheelForward : e.MouseData < 0 ? MouseActions.WheelBackward : MouseActions.None;
            if (wheelAction == MouseActions.None)
                return;

            if (PointCapture.Instance.SourceDevice == Devices.Mouse &&
                (PointCapture.Instance.State == CaptureState.CapturingInvalid || PointCapture.Instance.State == CaptureState.TriggerFired))
            {
                var actions = ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.MouseHotkey == wheelAction);
                FireMouseWheelActions(actions, e.Point, ref handled, true);
                return;
            }

            if (PointCapture.Instance.State != CaptureState.Ready || PointCapture.Instance.Mode != CaptureMode.Normal)
                return;

            // Standalone wheel triggers must stay conditioned so ordinary scrolling is not captured globally.
            var wheelApps = ApplicationManager.Instance.GetApplicationFromPoint(e.Point).ToList();
            if (ShouldSkipStandaloneWheel(wheelApps))
                return;

            var wheelActions = ApplicationManager.Instance.GetDefinedAction(
                    wheelApps,
                    a => a.MouseHotkey == wheelAction &&
                        (a.IgnoredDevices & Devices.Mouse) == 0 &&
                        !string.IsNullOrWhiteSpace(a.Condition),
                    ApplicationManager.Instance.ShouldUseGlobalFallback(wheelApps))
                .Where(a => a.Commands != null && a.Commands.Any(command => command != null && command.IsEnabled))
                .ToList();

            FireMouseWheelActions(wheelActions, e.Point, ref handled, false);
        }

        private static bool ShouldSkipStandaloneWheel(IEnumerable<IApplication> applications)
        {
            return applications.Any(app => app is IgnoredApp ignoredApp && ignoredApp.IsEnabled) ||
                AppConfig.WhitelistedApplicationsOnly && !applications.Any(app => app is UserApp);
        }

        private void FireMouseWheelActions(List<IAction> actions, System.Drawing.Point point, ref bool handled, bool updateCaptureState)
        {
            if (actions == null || actions.Count == 0)
                return;

            OnTriggerFired(new TriggerFiredEventArgs(actions, point, Devices.Mouse));
            if (updateCaptureState)
                PointCapture.Instance.State = CaptureState.TriggerFired;
            handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
        }

        private void MouseHook_MouseDown(LowLevelMouseMessage evt, ref bool handled)
        {
            if (PointCapture.Instance.SourceDevice == Devices.Mouse)
                if (PointCapture.Instance.State == CaptureState.CapturingInvalid || PointCapture.Instance.State == CaptureState.TriggerFired)
                {
                    var actions = ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.MouseHotkey == (MouseActions)evt.Button);
                    if (actions != null && actions.Count != 0)
                    {
                        handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                    }
                }
        }

        private void MouseHook_MouseUp(LowLevelMouseMessage e, ref bool handled)
        {
            if (PointCapture.Instance.SourceDevice == Devices.Mouse)
                if (PointCapture.Instance.State == CaptureState.CapturingInvalid || PointCapture.Instance.State == CaptureState.TriggerFired)
                {
                    var actions = ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.MouseHotkey == (MouseActions)e.Button);
                    if (actions != null && actions.Count != 0)
                    {
                        OnTriggerFired(new TriggerFiredEventArgs(actions, e.Point));
                        PointCapture.Instance.State = CaptureState.TriggerFired;
                        handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                    }
                }
        }
    }
}
