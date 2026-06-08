using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Common.Plugins;
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
                var actions = GetExecutableMouseActions(
                    ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.MouseHotkey == wheelAction),
                    e.Point);
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
                .ToList();

            wheelActions = GetExecutableMouseActions(wheelActions, e.Point);

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
                    var actions = GetExecutableMouseActions(
                        ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.MouseHotkey == (MouseActions)evt.Button),
                        evt.Point);
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
                    var actions = GetExecutableMouseActions(
                        ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.MouseHotkey == (MouseActions)e.Button),
                        e.Point);
                    if (actions != null && actions.Count != 0)
                    {
                        OnTriggerFired(new TriggerFiredEventArgs(actions, e.Point));
                        PointCapture.Instance.State = CaptureState.TriggerFired;
                        handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                    }
                }
        }

        private static List<IAction> GetExecutableMouseActions(IEnumerable<IAction> actions, System.Drawing.Point triggerPoint)
        {
            var inputPoints = PointCapture.Instance.InputPoints;
            var inputContactIdentifiers = PointCapture.Instance.InputContactIdentifiers;
            var conditionPoints = new List<List<System.Drawing.Point>>(inputPoints.Length);
            var conditionContactIdentifiers = new List<int>(inputPoints.Length);

            for (int i = 0; i < inputPoints.Length && i < inputContactIdentifiers.Count; i++)
            {
                if (inputPoints[i].Count == 0)
                    continue;

                conditionPoints.Add(new List<System.Drawing.Point>(inputPoints[i]));
                conditionContactIdentifiers.Add(inputContactIdentifiers[i]);
            }

            if (conditionPoints.Count == 0)
            {
                conditionPoints.Add(new List<System.Drawing.Point>(new[] { triggerPoint }));
                conditionContactIdentifiers.Add(1);
            }

            var targetWindow = ApplicationManager.Instance.CaptureWindow;
            return actions?
                .Where(action => action != null &&
                    (action.IgnoredDevices & Devices.Mouse) == 0 &&
                    HasEnabledCommands(action) &&
                    PluginManager.Instance.EvaluateCondition(action.Condition, conditionPoints, conditionContactIdentifiers, targetWindow))
                .ToList() ?? new List<IAction>();
        }

        private static bool HasEnabledCommands(IAction action)
        {
            return action?.Commands != null && action.Commands.Any(command => command != null && command.IsEnabled);
        }
    }
}
