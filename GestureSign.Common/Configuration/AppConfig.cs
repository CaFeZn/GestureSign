using GestureSign.Common.Input;
using GestureSign.Common.Log;
using ManagedWinapi.Hooks;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GestureSign.Common.Configuration
{
    public class AppConfig
    {
        private static bool _loadFlag = true;
        private static Dictionary<string, object> _settingCache = new Dictionary<string, object>(16);
        private static readonly object ConfigLock = new object();
        static System.Configuration.Configuration _config;
        static Timer Timer;
        public static event EventHandler ConfigChanged;
        private const DeviceStates PenActivationButtons = DeviceStates.RightClickButton | DeviceStates.Invert;
        private const DeviceStates PenDrawingModes = DeviceStates.Tip | DeviceStates.InRange;
        private const DeviceStates SupportedPenGestureButtonStates = PenActivationButtons | PenDrawingModes;

        private static ExeConfigurationFileMap ExeMap;
        private static string _applicationDataPath;
        private static string _localApplicationDataPath;
        private const int ErrorInsufficientBuffer = 122;

        private static System.Configuration.Configuration Config
        {
            get
            {
                lock (ConfigLock)
                {
                    if (_config == null || _loadFlag)
                    {
                        try
                        {
                            FileManager.WaitFile(ConfigPath);
                            _config = ConfigurationManager.OpenMappedExeConfiguration(ExeMap, ConfigurationUserLevel.None);
                            _settingCache.Clear();
                            _loadFlag = false;
                        }
                        catch (Exception e)
                        {
                            Logging.LogAndNotice(new Exceptions.FileWriteException(e));
                        }
                    }
                    return _config;
                }
            }
        }

        public static string ApplicationDataPath
        {
            get
            {
                if (!Directory.Exists(_applicationDataPath))
                {
                    try
                    {
                        Directory.CreateDirectory(_applicationDataPath);
                    }
                    catch (Exception e)
                    {
                        Logging.LogAndNotice(new Exceptions.FileWriteException(e));
                    }
                }

                return _applicationDataPath;
            }
            private set => _applicationDataPath = value;
        }

        public static string LocalApplicationDataPath
        {
            get
            {
                if (!Directory.Exists(_localApplicationDataPath))
                {
                    try
                    {
                        Directory.CreateDirectory(_localApplicationDataPath);
                    }
                    catch (Exception e)
                    {
                        Logging.LogAndNotice(new Exceptions.FileWriteException(e));
                    }
                }

                return _localApplicationDataPath;
            }
            private set => _localApplicationDataPath = value;
        }

        public static string BackupPath { private set; get; }

        public static string ConfigPath { private set; get; }

        public static string CurrentFolderPath { private set; get; }

        #region Setting Parameters

        public static System.Drawing.Color VisualFeedbackColor
        {
            get
            {
                return (System.Drawing.Color)GetValue("VisualFeedbackColor", System.Drawing.Color.DeepSkyBlue);
            }
            set
            {
                SetValue("VisualFeedbackColor", value);
            }
        }

        public static int VisualFeedbackWidth
        {
            get
            {
                return (int)GetValue("VisualFeedbackWidth", 9);
            }
            set
            {
                SetValue("VisualFeedbackWidth", value);
            }
        }
        public static int MinimumPointDistance
        {
            get
            {
                int value = (int)GetValue("MinimumPointDistance", 20);
                return value >= 3 ? value : 3;
            }
            set
            {
                SetValue("MinimumPointDistance", value >= 3 ? value : 3);
            }
        }

        public static double Opacity
        {
            get
            {
                double value = (double)GetValue("Opacity", 0.35);
                if (value < 0d) return 0d;
                if (value > 1d) return 1d;
                return value;
            }
            set
            {
                if (value < 0d) value = 0d;
                if (value > 1d) value = 1d;
                SetValue("Opacity", value);
            }
        }

        public static bool IsOrderByLocation
        {
            get
            {
                return (bool)GetValue("IsOrderByLocation", true);
            }
            set
            {
                SetValue("IsOrderByLocation", value);
            }
        }

        public static bool UiAccess { get; set; }
        public static bool ShowTrayIcon
        {
            get
            {
                return (bool)GetValue("ShowTrayIcon", true);
            }
            set
            {
                SetValue("ShowTrayIcon", value);
            }
        }

        public static string CultureName
        {
            get
            {
                return (string)GetValue("CultureName", "");
            }
            set
            {
                SetValue("CultureName", value);
            }
        }

        public static bool SendErrorReport
        {
            get
            {
                return (bool)GetValue("SendErrorReport", true);
            }
            set
            {
                SetValue("SendErrorReport", value);
            }
        }

        public static DateTime LastErrorTime
        {
            get
            {
                return GetValue("LastErrorTime", DateTime.MinValue);
            }
            set
            {
                SetValue("LastErrorTime", value);
            }
        }

        public static int InitialTimeout
        {
            get
            {
                return (int)GetValue(nameof(InitialTimeout), 0);
            }
            set
            {
                SetValue(nameof(InitialTimeout), value);
            }
        }

        public static int GestureTimeout
        {
            get
            {
                return (int)GetValue(nameof(GestureTimeout), 0);
            }
            set
            {
                SetValue(nameof(GestureTimeout), value);
            }
        }

        public static int CompositeGestureTimeout
        {
            get
            {
                return (int)GetValue(nameof(CompositeGestureTimeout), 800);
            }
            set
            {
                SetValue(nameof(CompositeGestureTimeout), value);
            }
        }

        public static int ContinuousGestureDistance
        {
            get
            {
                int value = (int)GetValue(nameof(ContinuousGestureDistance), 20);
                return value >= 5 ? value : 5;
            }
            set
            {
                SetValue(nameof(ContinuousGestureDistance), value >= 5 ? value : 5);
            }
        }

        public static MouseActions DrawingButton
        {
            get
            {
                return (MouseActions)GetValue(nameof(DrawingButton), 0);
            }
            set
            {
                SetValue(nameof(DrawingButton), (int)value);
            }
        }

        public static bool RegisterTouchPad
        {
            get
            {
                return (bool)GetValue(nameof(RegisterTouchPad), true);
            }
            set
            {
                SetValue(nameof(RegisterTouchPad), value);
            }
        }

        public static bool RegisterTouchScreen
        {
            get
            {
                return GetValue(nameof(RegisterTouchScreen), true);
            }
            set
            {
                SetValue(nameof(RegisterTouchScreen), value);
            }
        }

        public static bool IgnoreFullScreen
        {
            get
            {
                return GetValue(nameof(IgnoreFullScreen), false);
            }
            set
            {
                SetValue(nameof(IgnoreFullScreen), value);
            }
        }

        public static bool WhitelistedApplicationsOnly
        {
            get
            {
                return GetValue(nameof(WhitelistedApplicationsOnly), false);
            }
            set
            {
                SetValue(nameof(WhitelistedApplicationsOnly), value);
            }
        }

        public static bool IgnoreTouchInputWhenUsingPen
        {
            get
            {
                return GetValue(nameof(IgnoreTouchInputWhenUsingPen), true);
            }
            set
            {
                SetValue(nameof(IgnoreTouchInputWhenUsingPen), value);
            }
        }

        public static bool PlaySoundOnUnrecognizedGesture
        {
            get
            {
                return GetValue(nameof(PlaySoundOnUnrecognizedGesture), false);
            }
            set
            {
                SetValue(nameof(PlaySoundOnUnrecognizedGesture), value);
            }
        }

        public static string UnrecognizedGestureSoundPath
        {
            get
            {
                return GetValue(nameof(UnrecognizedGestureSoundPath), string.Empty);
            }
            set
            {
                SetValue(nameof(UnrecognizedGestureSoundPath), value ?? string.Empty);
            }
        }

        public static DeviceStates PenGestureButton
        {
            get
            {
                return NormalizePenGestureButton((DeviceStates)GetValue(nameof(PenGestureButton), 0));
            }
            set
            {
                SetValue(nameof(PenGestureButton), (int)NormalizePenGestureButton(value));
            }
        }

        private static DeviceStates NormalizePenGestureButton(DeviceStates value)
        {
            value &= SupportedPenGestureButtonStates;

            if (value == DeviceStates.None)
                return DeviceStates.None;

            bool hasActivationButton = (value & PenActivationButtons) != 0;
            bool hasDrawingMode = (value & PenDrawingModes) != 0;

            return hasActivationButton && hasDrawingMode ? value : DeviceStates.None;
        }

        public static bool RunAsAdmin
        {
            get
            {
                return GetValue(nameof(RunAsAdmin), false);
            }
            set
            {
                SetValue(nameof(RunAsAdmin), value);
            }
        }

        #endregion

        static AppConfig()
        {
#if uiAccess
            UiAccess = VersionHelper.IsWindows8OrGreater();
#endif
            CurrentFolderPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
#if Portable
            ApplicationDataPath = Path.Combine(CurrentFolderPath, "AppData");
            LocalApplicationDataPath = ApplicationDataPath;

            ConfigPath = Path.Combine(ApplicationDataPath, Constants.ConfigFileName);
            BackupPath = Path.Combine(LocalApplicationDataPath, "Backup");
#elif ConvertedDesktopApp
            string packageLocalStatePath = GetPackageLocalStatePath();
            if (packageLocalStatePath != null)
            {
                ApplicationDataPath = packageLocalStatePath;
                LocalApplicationDataPath = packageLocalStatePath;
            }
            else
            {
                ApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign");
                LocalApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GestureSign");
            }

            ConfigPath = Path.Combine(ApplicationDataPath, Constants.ConfigFileName);
            BackupPath = Path.Combine(LocalApplicationDataPath, "Backup");
#else
            ApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign");
            LocalApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GestureSign");

            ConfigPath = Path.Combine(ApplicationDataPath, Constants.ConfigFileName);
            BackupPath = LocalApplicationDataPath + "\\Backup";

#endif
            ExeMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = ConfigPath,
                RoamingUserConfigFilename = ConfigPath,
            };
            Timer = new Timer(SaveFile, null, Timeout.Infinite, Timeout.Infinite);
        }

        private static string GetPackageLocalStatePath()
        {
            string packageFamilyName = GetCurrentPackageFamilyName();
            if (string.IsNullOrWhiteSpace(packageFamilyName))
                return null;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages",
                packageFamilyName,
                "LocalState",
                "GestureSign");
        }

        private static string GetCurrentPackageFamilyName()
        {
            int length = 0;
            int result = GetCurrentPackageFamilyName(ref length, null);
            if (result != ErrorInsufficientBuffer || length <= 0)
                return null;

            StringBuilder packageFamilyName = new StringBuilder(length);
            result = GetCurrentPackageFamilyName(ref length, packageFamilyName);
            return result == 0 ? packageFamilyName.ToString().TrimEnd('\0') : null;
        }

        public static void Reload()
        {
            lock (ConfigLock)
            {
                _loadFlag = true;
                _settingCache.Clear();
            }
            if (ConfigChanged != null)
                ConfigChanged(new object(), EventArgs.Empty);
        }


        private static void Save()
        {
            Timer.Change(100, Timeout.Infinite);
        }

        public static void Flush()
        {
            Timer.Change(Timeout.Infinite, Timeout.Infinite);
            SaveFile(null);
        }

        private static void SaveFile(object state)
        {
            try
            {
                lock (ConfigLock)
                {
                    FileManager.WaitFile(ConfigPath);
                    // Save the configuration file.
                    var config = Config;
                    if (config == null)
                        return;
                    config.AppSettings.SectionInformation.ForceSave = true;
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
            catch (ConfigurationErrorsException e)
            {
                Reload();
                Logging.LogAndNotice(new Exceptions.FileWriteException(e));
            }
            catch (Exception e)
            {
                Logging.LogAndNotice(e);
            }
            // Force a reload of the changed section.    
            ConfigurationManager.RefreshSection("appSettings");
            ConfigChanged?.Invoke(new object(), EventArgs.Empty);
        }

        private static T GetValue<T>(string key, T defaultValue, Func<string, T> converter)
        {
            lock (ConfigLock)
            {
                var config = Config;
                if (config == null)
                    return defaultValue;
                var setting = config.AppSettings.Settings[key];
                if (setting != null)
                {
                    try
                    {
                        return converter(setting.Value);
                    }
                    catch
                    {
                        config.AppSettings.Settings.Remove(key);
                        return defaultValue;
                    }
                }
                return defaultValue;
            }
        }

        private static T GetCacheValue<T>(string key, T defaultValue, Func<string, T> converter)
        {
            lock (ConfigLock)
            {
                object output;
                if (_settingCache.TryGetValue(key, out output))
                {
                    return (T)output;
                }
                else
                {
                    var value = GetValue(key, defaultValue, converter);
                    _settingCache.Add(key, value);
                    return value;
                }
            }
        }

        private static int GetValue(string key, int defaultValue)
        {
            return GetCacheValue(key, defaultValue, s => int.Parse(s));
        }

        private static double GetValue(string key, double defaultValue)
        {
            return GetCacheValue(key, defaultValue, s => double.Parse(s));
        }

        private static bool GetValue(string key, bool defaultValue)
        {
            return GetCacheValue(key, defaultValue, s => bool.Parse(s));
        }

        private static string GetValue(string key, string defaultValue)
        {
            return GetValue(key, defaultValue, s => s);
        }

        private static DateTime GetValue(string key, DateTime defaultValue)
        {
            string setting = GetValue(key, string.Empty);
            if (!string.IsNullOrEmpty(setting))
            {
                try
                {
                    return DateTime.Parse(setting);
                }
                catch
                {
                    lock (ConfigLock)
                    {
                        Config?.AppSettings.Settings.Remove(key);
                    }
                    return defaultValue;
                }
            }
            else return defaultValue;
        }

        private static System.Drawing.Color GetValue(string key, System.Drawing.Color defaultValue)
        {
            string setting = GetValue(key, string.Empty);
            if (!string.IsNullOrEmpty(setting))
            {
                try
                {
                    return GetCacheValue(key, defaultValue, System.Drawing.ColorTranslator.FromHtml);
                }
                catch
                {
                    lock (ConfigLock)
                    {
                        Config?.AppSettings.Settings.Remove(key);
                    }
                    return defaultValue;
                }
            }
            else
            {
                System.Drawing.Color color;
                if (GetWindowGlassColor(out color))
                    return color;
                return defaultValue;
            }
        }

        private static void SetValue<T>(string key, T value)
        {
            lock (ConfigLock)
            {
                var config = Config;
                if (config == null)
                    return;
                _settingCache.Clear();

                if (config.AppSettings.Settings[key] != null)
                {
                    config.AppSettings.Settings[key].Value = value.ToString();
                }
                else
                {
                    config.AppSettings.Settings.Add(key, value.ToString());
                }
            }
            Save();
        }

        private static void SetValue(string key, System.Drawing.Color value)
        {
            SetValue(key, System.Drawing.ColorTranslator.ToHtml(value));
        }

        private static void SetValue(string key, DateTime value)
        {
            SetValue(key, value.ToString(CultureInfo.InvariantCulture));
        }

        private static bool GetWindowGlassColor(out System.Drawing.Color windowGlassColor)
        {
            windowGlassColor = System.Drawing.Color.Empty;
            try
            {
                if (VersionHelper.IsWindowsVistaOrGreater())
                {
                    using (RegistryKey dwm = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                    {
                        if (dwm == null)
                            return false;
                        var colorizationColor = dwm.GetValue("ColorizationColor");
                        if (colorizationColor == null)
                            return false;

                        windowGlassColor = System.Drawing.Color.FromArgb((int)colorizationColor | -16777216);
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetCurrentPackageFamilyName(ref int packageFamilyNameLength, StringBuilder packageFamilyName);
    }
}
