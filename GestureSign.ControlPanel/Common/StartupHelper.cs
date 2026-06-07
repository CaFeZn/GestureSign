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

        private static bool AreSamePath(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            try
            {
                string normalizedLeft = Path.GetFullPath(left.Trim('"')).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string normalizedRight = Path.GetFullPath(right.Trim('"')).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception exception)
            {
                GestureSign.Common.Log.Logging.LogException(exception);
                return false;
            }
        }

        private static string GetLnkTargetPath(string filepath)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(filepath);
            return shortcut.TargetPath;
        }

        private static string GetLnkWorkingDirectory(string filepath)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(filepath);
            return shortcut.WorkingDirectory;
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
            shortCut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            // Application.ProductName + Application.ProductVersion;
            //shortCut.IconLocation = Application.ResourceAssembly.Location;// Application.ExecutablePath;
            //shortCut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;// Application.ResourceAssembly.;
            shortCut.Save();
        }

        private static bool IsStartupShortcutCurrent(string startupLnkPath)
        {
            return File.Exists(startupLnkPath) &&
                   AreSamePath(GetLnkTargetPath(startupLnkPath), DaemonPath) &&
                   AreSamePath(GetLnkWorkingDirectory(startupLnkPath), DaemonDirectory);
        }

        private static bool RunSchtasks(string arguments, bool runAsAdministrator)
        {
            using (Process schtasks = new Process())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("schtasks.exe", arguments)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = runAsAdministrator,
                };

                if (runAsAdministrator)
                    startInfo.Verb = "runas";

                schtasks.StartInfo = startInfo;
                schtasks.Start();
                schtasks.WaitForExit();

                if (schtasks.ExitCode == 0)
                    return true;

                GestureSign.Common.Log.Logging.LogMessage($"schtasks.exe {arguments} exited with code {schtasks.ExitCode}.");
                return false;
            }
        }

        private static string QueryStartupTaskXml()
        {
            using (Process schtasks = new Process())
            {
                schtasks.StartInfo = new ProcessStartInfo("schtasks.exe", $" /query /tn \"{StartupTaskName}\" /xml")
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                schtasks.Start();
                string output = schtasks.StandardOutput.ReadToEnd();
                string error = schtasks.StandardError.ReadToEnd();
                schtasks.WaitForExit();

                if (schtasks.ExitCode == 0)
                    return output;

                if (!string.IsNullOrWhiteSpace(error))
                    GestureSign.Common.Log.Logging.LogMessage(error);

                GestureSign.Common.Log.Logging.LogMessage($"schtasks.exe /query exited with code {schtasks.ExitCode}.");
                return null;
            }
        }

        private static bool IsStartupTaskCurrent()
        {
            try
            {
                string taskXml = QueryStartupTaskXml();
                if (string.IsNullOrWhiteSpace(taskXml))
                    return false;

                XmlDocument taskDocument = new XmlDocument();
                taskDocument.LoadXml(taskXml);

                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(taskDocument.NameTable);
                namespaceManager.AddNamespace("task", TaskSchedulerNamespace);

                string enabled = taskDocument.SelectSingleNode("/task:Task/task:Settings/task:Enabled", namespaceManager)?.InnerText;
                string command = taskDocument.SelectSingleNode("/task:Task/task:Actions/task:Exec/task:Command", namespaceManager)?.InnerText;
                string workingDirectory = taskDocument.SelectSingleNode("/task:Task/task:Actions/task:Exec/task:WorkingDirectory", namespaceManager)?.InnerText;

                bool isEnabled;
                return bool.TryParse(enabled, out isEnabled) &&
                       isEnabled &&
                       AreSamePath(command, DaemonPath) &&
                       (string.IsNullOrWhiteSpace(workingDirectory) || AreSamePath(workingDirectory, DaemonDirectory));
            }
            catch (Exception exception)
            {
                GestureSign.Common.Log.Logging.LogException(exception);
                return false;
            }
        }

        private static bool AddStartupTask(string filePath)
        {
            string xmlFilePath = Path.Combine(AppConfig.LocalApplicationDataPath, "StartGestureSignTask.xml");

            try
            {
                string escapedFilePath = SecurityElement.Escape(filePath);
                string escapedWorkingDirectory = SecurityElement.Escape(Path.GetDirectoryName(filePath));
                string taskXml = Properties.Resources.StartGestureSignTask
                    .Replace("\"GestureSignFilePath\"", escapedFilePath)
                    .Replace("GestureSignFilePath", escapedFilePath)
                    .Replace("GestureSignWorkingDirectory", escapedWorkingDirectory);
                if (!taskXml.Contains("<WorkingDirectory>"))
                {
                    taskXml = taskXml.Replace("</Exec>", $"  <WorkingDirectory>{escapedWorkingDirectory}</WorkingDirectory>{Environment.NewLine}    </Exec>");
                }
                File.WriteAllText(xmlFilePath, taskXml, System.Text.Encoding.Unicode);

                string arguments = string.Format(" /create /tn \"{0}\" /f /xml \"{1}\"", StartupTaskName, xmlFilePath);
                return RunSchtasks(arguments, true) && IsStartupTaskCurrent();
            }
            catch (Exception exception)
            {
                GestureSign.Common.Log.Logging.LogAndNotice(exception);
                return false;
            }
            finally
            {
                if (File.Exists(xmlFilePath))
                    File.Delete(xmlFilePath);
            }
        }

        private static bool DelStartupTask()
        {
            try
            {
                string arguments = string.Format(" /delete /tn \"{0}\" /f", StartupTaskName);
                RunSchtasks(arguments, true);
                return !IsStartupTaskCurrent();
            }
            catch (Exception exception)
            {
                GestureSign.Common.Log.Logging.LogAndNotice(exception);
                return false;
            }
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
                if (IsStartupShortcutCurrent(startupLnkPath))
                {
                    return true;
                }

                if (File.Exists(startupLnkPath))
                {
                    CreateLnk(startupLnkPath, DaemonPath);
                    return IsStartupShortcutCurrent(startupLnkPath);
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool EnableNormalStartup()
        {
            try
            {
                CreateLnk(StartupLnkPath, DaemonPath);
                return IsStartupShortcutCurrent(StartupLnkPath);
            }
            catch (Exception exception)
            {
                GestureSign.Common.Log.Logging.LogAndNotice(exception);
                return false;
            }
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

        public static bool GetHighPrivilegeStartupStatus()
        {
            return IsStartupTaskCurrent();
        }

        public static bool DisableHighPrivilegeStartup()
        {
            return DelStartupTask();
        }
    }
}
