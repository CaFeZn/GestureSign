using System;
using System.IO;
using System.Linq;
using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using GestureSign.Common.Plugins;
using ManagedWinapi.Windows;

namespace GestureSign.CorePlugins
{
    public class AddIgnoredApplication : IPlugin
    {
        private IHostControl _hostControl = null;

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.AddIgnoredApplication.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.AddIgnoredApplication.Description"); }
        }

        public object GUI
        {
            get { return null; }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public string Category
        {
            get { return "GestureSign"; }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon => IconSource.GestureSign;

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo actionPoint)
        {
            try
            {
                var targetWindow = GetTargetWindow(actionPoint);
                if (targetWindow == null || targetWindow.HWnd == IntPtr.Zero)
                    return false;

                string className, title, fileName;
                var window = ApplicationManager.GetWindowInfo(targetWindow, out className, out title, out fileName);
                if (string.IsNullOrWhiteSpace(fileName) || IsGestureSignProcess(fileName))
                    return false;

                var applicationManager = ApplicationManager.Instance;
                var existingApp = applicationManager.FindMatchApplications<IgnoredApp>(MatchUsing.ExecutableFilename, fileName).FirstOrDefault();
                if (existingApp != null)
                {
                    var ignoredApp = existingApp as IgnoredApp;
                    if (ignoredApp != null && !ignoredApp.IsEnabled)
                    {
                        ignoredApp.IsEnabled = true;
                        applicationManager.SaveApplications();
                    }
                    return true;
                }

                var name = GetApplicationName(window, title, fileName);
                applicationManager.AddApplication(new IgnoredApp(name, MatchUsing.ExecutableFilename, fileName, false, true));
                applicationManager.SaveApplications();

                return true;
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                return false;
            }
        }

        public bool Deserialize(string serializedData)
        {
            return true;
        }

        public string Serialize()
        {
            return "";
        }

        public IHostControl HostControl
        {
            get { return _hostControl; }
            set { _hostControl = value; }
        }

        private static SystemWindow GetTargetWindow(PointInfo actionPoint)
        {
            var targetWindow = actionPoint?.Window;
            if (targetWindow != null && !ApplicationManager.IsShellUiWindow(targetWindow))
                return targetWindow;

            var foregroundWindow = SystemWindow.ForegroundWindow;
            return foregroundWindow != null && !ApplicationManager.IsShellUiWindow(foregroundWindow)
                ? foregroundWindow
                : targetWindow;
        }

        private static bool IsGestureSignProcess(string fileName)
        {
            return Constants.ControlPanelFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                Constants.DaemonFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetApplicationName(SystemWindow window, string title, string fileName)
        {
            try
            {
                var fileDescription = window.Process.MainModule.FileVersionInfo.FileDescription;
                if (!string.IsNullOrWhiteSpace(fileDescription))
                    return fileDescription;
            }
            catch
            {
                // Fall back to stable window info when process metadata is unavailable.
            }

            if (!string.IsNullOrWhiteSpace(title))
                return title;

            return Path.GetFileNameWithoutExtension(fileName);
        }
    }
}
