using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using IWshRuntimeLibrary;
using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using File = System.IO.File;

namespace GestureSign.ControlPanel.Common
{
    static class StartupHelper
    {
        private const string StartupTaskName = "StartGestureSign";
        private const string TaskSchedulerNamespace = "http://schemas.microsoft.com/windows/2004/02/mit/task";

        private static string DaemonPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GestureSign.Common.Constants.DaemonFileName);

        private static string DaemonDirectory => Path.GetDirectoryName(DaemonPath);

        private static string StartupLnkPath => Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + GestureSign.Common.Constants.ProductName + ".lnk";

        public static bool IsRunAsAdmin => AppConfig.RunAsAdmin;

        private static string GetLnkTargetPath(string filepath)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(filepath);
            return shortcut.TargetPath;
        }

        private static void CreateLnk(string lnkPath, string targetPath)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortCut = (IWshShortcut)shell.CreateShortcut(lnkPath);
            shortCut.TargetPath = targetPath;
            //Application.ResourceAssembly.Location;// System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            shortCut.WindowStyle = 7;
            shortCut.Arguments = "";
            shortCut.Description = Application.ResourceAssembly.GetName().Version.ToString();
            // Application.ProductName + Application.ProductVersion;
            //shortCut.IconLocation = Application.ResourceAssembly.Location;// Application.ExecutablePath;
            //shortCut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;// Application.ResourceAssembly.;
            shortCut.Save();
        }

        private static bool AddStartupTask(string filePath)
        {
            try
            {
                string taskXml = Properties.Resources.StartGestureSignTask
                    .Replace("\"GestureSignFilePath\"", filePath)
                    .Replace("GestureSignFilePath", filePath);
                string xmlFilePath = Path.Combine(AppConfig.LocalApplicationDataPath, "StartGestureSignTask.xml");
                File.WriteAllText(xmlFilePath, taskXml, System.Text.Encoding.Unicode);

                using (Process schtasks = new Process())
                {
                    string arguments = string.Format(" /create /tn StartGestureSign /f /xml \"{0}\"", xmlFilePath);
                    schtasks.StartInfo = new ProcessStartInfo("schtasks.exe", arguments)
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true,
                        Verb = "runas",
                    };
                    schtasks.Start();
                    schtasks.WaitForExit();
                    if (schtasks.ExitCode != 0)
                    {
                        return false;
                    }
                }
                if (File.Exists(xmlFilePath))
                    File.Delete(xmlFilePath);
            }
            catch (Exception exception)
            {
                GestureSign.Common.Log.Logging.LogAndNotice(exception);
                return false;
            }

            return true;
        }

        private static bool DelStartupTask()
        {
            try
            {
                using (Process schtasks = new Process())
                {
                    schtasks.StartInfo = new ProcessStartInfo("schtasks.exe", " /delete /tn StartGestureSign /f")
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true,
                        Verb = "runas",
                    };
                    schtasks.Start();
                    schtasks.WaitForExit();
                }
            }
            catch (Exception exception)
            {
                GestureSign.Common.Log.Logging.LogAndNotice(exception);
                return false;
            }

            return true;
        }

        public static async Task<bool> CheckStoreAppStartupStatus()
        {
            var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("GestureSignTask");
            switch (startupTask.State)
            {
                case Windows.ApplicationModel.StartupTaskState.Disabled:
                    return false;
                case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                    return false;
                case Windows.ApplicationModel.StartupTaskState.Enabled:
                    return true;
                default:
                    return false;
            }
        }

        public static async Task<bool> EnableStoreAppStartup()
        {
            var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("GestureSignTask");
            if (startupTask.State != Windows.ApplicationModel.StartupTaskState.Enabled)
            {
                var state = await startupTask.RequestEnableAsync();
                if (state == Windows.ApplicationModel.StartupTaskState.DisabledByUser)
                {
                    MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Options.Messages.TaskUserDisabled"), LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        public static async Task<bool> DisableStoreAppStartup()
        {
            var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("GestureSignTask");
            if (startupTask.State == Windows.ApplicationModel.StartupTaskState.Enabled)
            {
                startupTask.Disable();
            }
            return true;
        }

        public static bool GetStartupStatus()
        {
            try
            {
                string startupLnkPath = StartupLnkPath;
                if (File.Exists(startupLnkPath))
                {
                    var targetPath = GetLnkTargetPath(startupLnkPath);
                    var daemonPath = DaemonPath;
                    if (!File.Exists(targetPath) || !daemonPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        CreateLnk(startupLnkPath, daemonPath);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool EnableNormalStartup()
        {
            CreateLnk(StartupLnkPath, DaemonPath);
            return true;
        }

        public static bool DisableNormalStartup()
        {
            if (File.Exists(StartupLnkPath))
            {
                try
                {
                    File.Delete(StartupLnkPath);
                }
                catch (Exception exception)
                {
                    GestureSign.Common.Log.Logging.LogAndNotice(exception);
                    return false;
                }
            }
            return true;
        }

        public static bool EnableHighPrivilegeStartup()
        {
            return AddStartupTask(DaemonPath);
        }

        public static bool DisableHighPrivilegeStartup()
        {
            return DelStartupTask();
        }
    }
}
