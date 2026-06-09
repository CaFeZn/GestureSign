using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Log;
using ManagedWinapi.Hooks;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestureSign.Daemon.Input
{
    internal class InputProvider : IDisposable
    {
        private bool disposedValue = false; // To detect redundant calls
        private MessageWindow _messageWindow;
        private CustomNamedPipeServer _deviceStateServer;
        private int _stateUpdating;
        private readonly SynchronizationContext _synchronizationContext;

        public LowLevelMouseHook LowLevelMouseHook;
        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public InputProvider()
        {
            _synchronizationContext = SynchronizationContext.Current;
            _messageWindow = new MessageWindow();
            _messageWindow.PointsIntercepted += MessageWindow_PointsIntercepted;

            AppConfig.ConfigChanged += AppConfig_ConfigChanged;
            ApplicationManager.OnLoadApplicationsCompleted += ApplicationManager_OnLoadApplicationsCompleted;
            LowLevelMouseHook = new LowLevelMouseHook();
            ScheduleMouseHookUpdate(1000);

            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(OnSessionSwitch);
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(OnPowerModeChanged);

            _deviceStateServer = new CustomNamedPipeServer(Common.Constants.Daemon + "DeviceState", IpcCommands.SynDeviceState,
                () => HidDevice.EnumerateDevices());
            TryLogRawInputDevices();
        }

        private void AppConfig_ConfigChanged(object sender, EventArgs e)
        {
            ScheduleMouseHookUpdate();
            UpdateDeviceState();
        }

        private void ApplicationManager_OnLoadApplicationsCompleted(object sender, EventArgs e)
        {
            ScheduleMouseHookUpdate();
        }

        private void MessageWindow_PointsIntercepted(object sender, RawPointsDataMessageEventArgs e)
        {
            if (e.RawData.Count == 0)
                return;
            PointsIntercepted?.Invoke(this, e);
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                UpdateDeviceState();
            }
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            // We need to handle sleeping(and other related events)
            // This is so we never lose the lock on the touchpad hardware.
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.SessionUnlock:
                    UpdateDeviceState();
                    break;
                default:
                    break;
            }
        }

        private void UpdateDeviceState()
        {
            if (0 == Interlocked.Exchange(ref _stateUpdating, 1))
            {
                Task.Delay(600).ContinueWith(t => PostDeviceStateUpdate());
            }
        }

        private void PostDeviceStateUpdate()
        {
            if (_synchronizationContext != null)
            {
                _synchronizationContext.Post(state => UpdateDeviceStateOnContext(), null);
                return;
            }

            UpdateDeviceStateOnContext();
        }

        private void UpdateDeviceStateOnContext()
        {
            try
            {
                TryLogRawInputDevices();
                _messageWindow?.UpdateRegistration();
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _stateUpdating, 0);
            }
        }

        private static void TryLogRawInputDevices()
        {
            try
            {
                HidDevice.EnumerateDevices();
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
            }
        }

        private void ScheduleMouseHookUpdate(int delayMilliseconds = 0)
        {
            if (delayMilliseconds > 0)
            {
                Task.Delay(delayMilliseconds).ContinueWith(t => PostMouseHookUpdate());
                return;
            }

            PostMouseHookUpdate();
        }

        private void PostMouseHookUpdate()
        {
            if (_synchronizationContext != null)
            {
                _synchronizationContext.Post(state => UpdateMouseHook(), null);
                return;
            }

            UpdateMouseHook();
        }

        private void UpdateMouseHook()
        {
            if (ShouldUseMouseHook())
                LowLevelMouseHook.StartHook();
            else
                LowLevelMouseHook.Unhook();
        }

        private static bool ShouldUseMouseHook()
        {
            return AppConfig.DrawingButton != MouseActions.None || HasConditionedStandaloneWheelAction();
        }

        private static bool HasConditionedStandaloneWheelAction()
        {
            return ApplicationManager.Instance.Applications
                .Where(app => !(app is IgnoredApp) && app.Actions != null)
                .SelectMany(app => app.Actions)
                .Any(action =>
                    action != null &&
                    (action.MouseHotkey == MouseActions.WheelForward || action.MouseHotkey == MouseActions.WheelBackward) &&
                    (action.IgnoredDevices & Devices.Mouse) == 0 &&
                    !string.IsNullOrWhiteSpace(action.Condition) &&
                    action.Commands != null &&
                    action.Commands.Any(command => command != null && command.IsEnabled));
        }

        internal void ReleaseCurrentTouchSource()
        {
            _messageWindow?.ReleaseCurrentTouchSource();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AppConfig.ConfigChanged -= AppConfig_ConfigChanged;
                    ApplicationManager.OnLoadApplicationsCompleted -= ApplicationManager_OnLoadApplicationsCompleted;
                    _messageWindow?.Dispose();
                }

                SystemEvents.SessionSwitch -= OnSessionSwitch;
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                LowLevelMouseHook?.Unhook();
                _deviceStateServer.Dispose();
                disposedValue = true;
            }
        }

        ~InputProvider()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
