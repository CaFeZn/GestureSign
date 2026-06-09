using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GestureSign.Common.Applications;
using GestureSign.Common.Input;
using GestureSign.Common.Log;
using ManagedWinapi.Windows;

namespace GestureSign.Common.Plugins
{
    public class PluginManager : IPluginManager
    {
        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        static readonly PluginManager _Instance = new PluginManager();
        List<IPluginInfo> _Plugins = new List<IPluginInfo>();
        private Task _lastActionTask;
        private readonly object _lastCommandLock = new object();
        private RepeatableCommand _lastCommand;
        private SynchronizationContext _mainContext;
        private static readonly Regex FingerVariablePattern = new Regex(@"(?<!\w)finger_\d+_(?:(?:start_X|start_Y|end_X|end_Y)%?|ID)(?![%\w])", RegexOptions.Compiled);
        private static readonly Regex KeyVariablePattern = new Regex(@"(?<!\w)key_([A-Za-z0-9]+(?:_[A-Za-z0-9]+)*)_down(?!\w)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion

        #region Public Properties

        public IPluginInfo[] Plugins { get { return _Plugins.ToArray(); } }

        public static PluginManager Instance
        {
            get { return _Instance; }
        }

        #endregion

        #region Constructors

        protected PluginManager()
        {

        }

        #endregion

        #region Events

        protected void PointCapture_GestureRecognized(object sender, RecognitionEventArgs e)
        {
            var pointCapture = (IPointCapture)sender;
            // Get action to be executed
            var executableActions = ApplicationManager.Instance.GetRecognizedDefinedAction(e.GestureName)?.ToList();
            if (executableActions == null) return;
            ExecuteAction(executableActions, pointCapture.Mode, pointCapture.SourceDevice, e.ContactIdentifiers, e.FirstCapturedPoints, e.Points, pressedVirtualKeys: e.PressedVirtualKeys);
        }

        #endregion

        #region Public Methods

        public void ExecuteAction(List<IAction> executableActions, CaptureMode mode, Devices devices, List<int> contactIdentifiers, List<Point> firstCapturedPoints, List<List<Point>> points, List<int> conditionContactIdentifiers = null, List<List<Point>> conditionPoints = null, List<int> pressedVirtualKeys = null)
        {
            // Exit if we're teaching
            if (mode == CaptureMode.Training)
                return;
            var target = ApplicationManager.Instance.CaptureWindow;
            var pointsForCondition = conditionPoints ?? points;
            var contactIdentifiersForCondition = conditionContactIdentifiers ?? contactIdentifiers;
            var resolvedActions = GetExecutableActions(executableActions, mode, devices, contactIdentifiersForCondition, pointsForCondition, pressedVirtualKeys);
            if (resolvedActions.Count == 0)
                return;
            var pointInfo = new PointInfo(firstCapturedPoints, points, target, _mainContext);
            var action = new Action<object>(o =>
            {
                foreach (IAction executableAction in resolvedActions)
                {
                    var commandList = executableAction.Commands.Where(command => command != null && command.IsEnabled).ToList();
                    foreach (var command in commandList)
                    {
                        if (mode == CaptureMode.UserDisabled && !"GestureSign.CorePlugins.ToggleDisableGestures".Equals(command.PluginClass))
                            continue;

                        // Locate the plugin associated with this action
                        IPluginInfo pluginInfo = FindPluginByClassAndFilename(command.PluginClass, command.PluginFilename);

                        // Exit if there is no plugin available for action
                        if (pluginInfo == null)
                            continue;

                        bool activateWindow = ShouldActivateWindow(executableAction, pluginInfo.Plugin);
                        bool activateThisCommand = commandList.IndexOf(command) == 0 && activateWindow;
                        ExecuteCommand(command, pluginInfo, pointInfo, target, activateThisCommand, true, activateWindow);
                    }
                }
            });

            var observeExceptions = new Action<Task>(t =>
            {
                Logging.LogException(t.Exception.InnerException);
            });

            if (_lastActionTask == null)
            {
                _lastActionTask = Task.Factory.StartNew(action, null);
                _lastActionTask.ContinueWith(observeExceptions, TaskContinuationOptions.OnlyOnFaulted);
            }
            else
            {
                _lastActionTask = _lastActionTask.ContinueWith(action);
                _lastActionTask.ContinueWith(observeExceptions, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public bool LoadPlugins(IHostControl host)
        {
            // Default return value to failure
            bool bFailed = true;

            // Clear any existing plugins
            _Plugins = new List<IPluginInfo>();
            //_Plugins.Clear();
            string directoryPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (directoryPath == null) return true;

            // Load core plugins.
            string corePluginsPath = Path.Combine(directoryPath, "GestureSign.CorePlugins.dll");
            if (File.Exists(corePluginsPath))
            {
                _Plugins.AddRange(LoadPluginsFromAssembly(corePluginsPath, host));
                bFailed = false;
            }

            var extraPluginsPath = Path.Combine(directoryPath, "Plugins");
            if (Directory.Exists(extraPluginsPath))
            {
                // Load extra plugins.
                foreach (string sFilePath in Directory.GetFiles(extraPluginsPath, "*.dll"))
                {
                    _Plugins.AddRange(LoadPluginsFromAssembly(sFilePath, host));
                    bFailed = false;
                }
            }


            return bFailed;
        }

        public IPluginInfo FindPluginByClassAndFilename(string PluginClass, string PluginFilename)
        {
            // Get reference to plugin using PluginClass and PluginFilename
            return _Plugins.FirstOrDefault(p => p.Class == PluginClass && p.Filename == PluginFilename);
        }

        public bool PluginExists(string PluginClass, string PluginFilename)
        {
            return _Plugins.Exists(p => p.Class == PluginClass && p.Filename == PluginFilename);
        }

        public bool RepeatLastCommand(PointInfo actionPoint)
        {
            RepeatableCommand command;
            lock (_lastCommandLock)
            {
                command = _lastCommand?.Clone();
            }

            if (command == null)
                return false;

            IPluginInfo pluginInfo = FindPluginByClassAndFilename(command.PluginClass, command.PluginFilename);
            if (pluginInfo == null || IsNonRepeatable(pluginInfo))
                return false;

            var target = actionPoint?.Window;
            return ExecuteCommand(command.ToCommand(), pluginInfo, actionPoint, target, command.ActivateWindow, false, false);
        }

        public bool HasExecutableAction(string gestureName, CaptureMode mode, Devices devices, List<int> contactIdentifiers, List<List<Point>> points, List<int> pressedVirtualKeys = null)
        {
            if (string.IsNullOrWhiteSpace(gestureName))
                return false;

            var executableActions = ApplicationManager.Instance.GetRecognizedDefinedAction(gestureName)?.ToList();
            if (executableActions == null || executableActions.Count == 0)
                return false;

            return GetExecutableActions(executableActions, mode, devices, contactIdentifiers, points, pressedVirtualKeys).Count != 0;
        }

        public List<IAction> GetExecutableActions(IEnumerable<IAction> candidateActions, CaptureMode mode, Devices devices, List<int> contactIdentifiers, List<List<Point>> points, List<int> pressedVirtualKeys = null)
        {
            var executableActions = candidateActions?.ToList();
            if (executableActions == null || executableActions.Count == 0)
                return new List<IAction>();

            var target = ApplicationManager.Instance.CaptureWindow;
            var pointsForCondition = points ?? new List<List<Point>>();
            var contactIdentifiersForCondition = contactIdentifiers ?? new List<int>();
            var result = new List<IAction>();

            foreach (var executableAction in executableActions)
            {
                if (executableAction == null ||
                    (executableAction.IgnoredDevices & devices) != 0 ||
                    IsUnsafeSingleFingerTouchPadAction(executableAction, devices, pointsForCondition) ||
                    executableAction.Commands == null ||
                    !Compute(executableAction.Condition, pointsForCondition, contactIdentifiersForCondition, target, pressedVirtualKeys))
                {
                    continue;
                }

                foreach (var command in executableAction.Commands.Where(command => command != null && command.IsEnabled))
                {
                    if (mode == CaptureMode.UserDisabled &&
                        !"GestureSign.CorePlugins.ToggleDisableGestures".Equals(command.PluginClass))
                    {
                        continue;
                    }

                    if (FindPluginByClassAndFilename(command.PluginClass, command.PluginFilename) != null)
                    {
                        result.Add(executableAction);
                        break;
                    }
                }
            }

            if (result.Any(action => !string.IsNullOrWhiteSpace(action.Condition)))
            {
                result = result
                    .Where(action => !string.IsNullOrWhiteSpace(action.Condition))
                    .ToList();
            }

            return result;
        }

        #endregion

        #region Private Methods

        private List<IPluginInfo> LoadPluginsFromAssembly(string assemblyLocation, IHostControl hostControl)
        {
            List<IPluginInfo> retPlugins = new List<IPluginInfo>();

            //To avoid exception System.NotSupportedException
            byte[] file = File.ReadAllBytes(assemblyLocation);
            Assembly aPlugin = Assembly.Load(file);

            Localization.LocalizationProvider.Instance.AddAssembly(aPlugin.FullName);

            Type[] tPluginTypes = aPlugin.GetTypes();

            foreach (Type tPluginType in tPluginTypes)
                if (tPluginType.GetInterface("IPlugin") != null)
                {
                    IPlugin plugin = Activator.CreateInstance(tPluginType) as IPlugin;

                    // If we have a new instance of a plugin, initialize it and add it to return list
                    if (plugin != null)
                    {
                        plugin.HostControl = hostControl;
                        plugin.Initialize();
                        retPlugins.Add(new PluginInfo(plugin, tPluginType.FullName, Path.GetFileName(assemblyLocation)));
                    }
                }

            return retPlugins;
        }

        private bool ExecuteCommand(ICommand command, IPluginInfo pluginInfo, PointInfo pointInfo, SystemWindow target, bool activateWindow, bool recordCommand, bool repeatActivateWindow)
        {
            var effectiveTarget = ResolveCommandTarget(pointInfo, target);
            pointInfo?.SetTargetWindow(effectiveTarget);
            effectiveTarget?.WaitForIdle(200);

            if (activateWindow)
                ActivateWindow(effectiveTarget);

            // Load action settings into plugin
            pluginInfo.Plugin.Deserialize(command.CommandSettings);

            // Execute plugin process
            bool success = pluginInfo.Plugin.Gestured(pointInfo);
            if (success && recordCommand)
                StoreLastCommand(command, pluginInfo, repeatActivateWindow);

            return success;
        }

        private static SystemWindow ResolveCommandTarget(PointInfo pointInfo, SystemWindow target)
        {
            if (target != null &&
                target.HWnd != IntPtr.Zero &&
                !ApplicationManager.IsShellUiWindow(target))
            {
                return target;
            }

            var resolvedWindow = pointInfo?.Window;
            return resolvedWindow != null &&
                resolvedWindow.HWnd != IntPtr.Zero &&
                !ApplicationManager.IsShellUiWindow(resolvedWindow)
                ? resolvedWindow
                : target;
        }

        private void StoreLastCommand(ICommand command, IPluginInfo pluginInfo, bool activateWindow)
        {
            if (IsNonRepeatable(pluginInfo))
                return;

            lock (_lastCommandLock)
            {
                _lastCommand = new RepeatableCommand(command, activateWindow);
            }
        }

        public bool EvaluateCondition(string condition, List<List<Point>> pointList, List<int> contactIdentifiers, SystemWindow targetWindow)
        {
            return Compute(condition, pointList, contactIdentifiers, targetWindow, null);
        }

        private static bool ShouldActivateWindow(IAction executableAction, IPlugin plugin)
        {
            return executableAction.ActivateWindow == null && plugin.ActivateWindowDefault ||
                executableAction.ActivateWindow.GetValueOrDefault();
        }

        private static void ActivateWindow(SystemWindow target)
        {
            if (target != null &&
                target.HWnd != IntPtr.Zero &&
                !ApplicationManager.IsShellUiWindow(target) &&
                target.HWnd.ToInt64() != SystemWindow.ForegroundWindow?.HWnd.ToInt64())
            {
                SystemWindow.ForegroundWindow = target;
            }
        }

        private static bool IsNonRepeatable(IPluginInfo pluginInfo)
        {
            return pluginInfo.Plugin is INonRepeatablePlugin;
        }

        private static bool IsUnsafeSingleFingerTouchPadAction(IAction action, Devices devices, List<List<Point>> pointList)
        {
            if ((devices & Devices.TouchPad) == 0)
                return false;

            int contactCount = action.ContinuousGesture != null
                ? action.ContinuousGesture.ContactCount
                : pointList?.Count ?? 0;

            return contactCount == 1 && string.IsNullOrWhiteSpace(action.Condition);
        }

        private bool Compute(string condition, List<List<Point>> pointList, List<int> contactIdentifiers, SystemWindow targetWindow, List<int> pressedVirtualKeys)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            pointList = pointList ?? new List<List<Point>>();
            contactIdentifiers = contactIdentifiers ?? new List<int>();

            string expression = GetExpression(condition, pointList, contactIdentifiers, targetWindow, pressedVirtualKeys);
            if (FingerVariablePattern.IsMatch(expression))
                return false;

            try
            {
                DataTable dataTable = new DataTable();
                var result = dataTable.Compute(expression, null);
                return result is DBNull || Convert.ToBoolean(result);
            }
            catch (Exception ex) when (ex is EvaluateException ||
                ex is InvalidExpressionException ||
                ex is SyntaxErrorException ||
                ex is FormatException ||
                ex is InvalidCastException)
            {
                return false;
            }
        }

        private string GetExpression(string condition, List<List<Point>> pointList, List<int> contactIdentifiers, SystemWindow targetWindow, List<int> pressedVirtualKeys)
        {
            bool hasPercentVariables = condition.Contains("%");
            Rectangle virtualScreenBounds = Rectangle.Empty;
            bool hasVirtualScreenBounds = !hasPercentVariables || TryGetVirtualScreenBounds(out virtualScreenBounds);
            int count = Math.Min(pointList.Count, contactIdentifiers.Count);
            for (int i = 1; i <= count; i++)
            {
                if (pointList[i - 1] == null || pointList[i - 1].Count == 0)
                    continue;

                int startX = pointList[i - 1].FirstOrDefault().X;
                int startY = pointList[i - 1].FirstOrDefault().Y;
                int endX = pointList[i - 1].LastOrDefault().X;
                int endY = pointList[i - 1].LastOrDefault().Y;

                if (hasPercentVariables && hasVirtualScreenBounds)
                {
                    condition = ReplaceVariables(condition, i, "start_X%", (startX - virtualScreenBounds.Left) * 100 / virtualScreenBounds.Width);
                    condition = ReplaceVariables(condition, i, "start_Y%", (startY - virtualScreenBounds.Top) * 100 / virtualScreenBounds.Height);
                    condition = ReplaceVariables(condition, i, "end_X%", (endX - virtualScreenBounds.Left) * 100 / virtualScreenBounds.Width);
                    condition = ReplaceVariables(condition, i, "end_Y%", (endY - virtualScreenBounds.Top) * 100 / virtualScreenBounds.Height);
                }

                condition = ReplaceVariables(condition, i, "start_X", startX);
                condition = ReplaceVariables(condition, i, "start_Y", startY);
                condition = ReplaceVariables(condition, i, "end_X", endX);
                condition = ReplaceVariables(condition, i, "end_Y", endY);

                condition = ReplaceVariables(condition, i, "ID", contactIdentifiers[i - 1]);
            }
            condition = ReplaceWindowVariables(condition, targetWindow);
            condition = ReplaceKeyVariables(condition, pressedVirtualKeys);

            return condition;
        }

        private string ReplaceVariables(string str, int id, string key, int value)
        {
            string variable = $"finger_{id}_{key}";
            string suffixPattern = key.EndsWith("%", StringComparison.Ordinal) ? @"(?!\w)" : @"(?![%\w])";
            string pattern = $@"(?<!\w){Regex.Escape(variable)}{suffixPattern}";
            return Regex.Replace(str, pattern, value.ToString());
        }

        private static bool TryGetVirtualScreenBounds(out Rectangle bounds)
        {
            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            if (width <= 0 || height <= 0)
            {
                bounds = Rectangle.Empty;
                return false;
            }

            bounds = new Rectangle(left, top, width, height);
            return true;
        }

        private string ReplaceWindowVariables(string condition, SystemWindow targetWindow)
        {
            bool isMaximized = IsMaximized(targetWindow);
            bool isMinimized = IsMinimized(targetWindow);
            bool isFullscreen = IsFullScreen(targetWindow);

            condition = ReplaceToken(condition, "window_is_maximized", ToExpressionBoolean(isMaximized));
            condition = ReplaceToken(condition, "window_is_minimized", ToExpressionBoolean(isMinimized));
            condition = ReplaceToken(condition, "window_is_fullscreen", ToExpressionBoolean(isFullscreen));

            return condition;
        }

        private string ReplaceKeyVariables(string condition, List<int> pressedVirtualKeys)
        {
            condition = ReplaceToken(condition, "key_is_shift_down", ToExpressionBoolean(IsAnyKeyDown(pressedVirtualKeys, VK_LSHIFT, VK_RSHIFT)));
            condition = ReplaceToken(condition, "key_is_ctrl_down", ToExpressionBoolean(IsAnyKeyDown(pressedVirtualKeys, VK_LCONTROL, VK_RCONTROL)));
            condition = ReplaceToken(condition, "key_is_alt_down", ToExpressionBoolean(IsAnyKeyDown(pressedVirtualKeys, VK_LMENU, VK_RMENU)));
            condition = ReplaceToken(condition, "key_is_win_down", ToExpressionBoolean(IsAnyKeyDown(pressedVirtualKeys, VK_LWIN, VK_RWIN)));

            return KeyVariablePattern.Replace(condition, match =>
            {
                int virtualKey;
                return TryGetVirtualKey(match.Groups[1].Value, out virtualKey)
                    ? ToExpressionBoolean(IsAnyKeyDown(pressedVirtualKeys, virtualKey))
                    : match.Value;
            });
        }

        private static string ReplaceToken(string condition, string token, string value)
        {
            string pattern = $@"(?<!\w){Regex.Escape(token)}(?!\w)";
            return Regex.Replace(condition, pattern, value);
        }

        private static string ToExpressionBoolean(bool value)
        {
            return value ? "(1=1)" : "(1=0)";
        }

        private static bool IsMaximized(SystemWindow targetWindow)
        {
            return targetWindow != null && targetWindow.HWnd != IntPtr.Zero &&
                IsZoomed(targetWindow.HWnd);
        }

        private static bool IsMinimized(SystemWindow targetWindow)
        {
            return targetWindow != null && targetWindow.HWnd != IntPtr.Zero &&
                IsIconic(targetWindow.HWnd);
        }

        private static bool IsFullScreen(SystemWindow targetWindow)
        {
            try
            {
                if (targetWindow == null || targetWindow.HWnd == IntPtr.Zero)
                    return false;

                if (IsMaximized(targetWindow) || IsMinimized(targetWindow))
                    return false;

                RECT windowRect;
                if (!GetWindowRect(targetWindow.HWnd, out windowRect) ||
                    windowRect.Width <= 0 || windowRect.Height <= 0)
                    return false;

                IntPtr monitor = MonitorFromWindow(targetWindow.HWnd, MONITOR_DEFAULTTONEAREST);
                if (monitor == IntPtr.Zero)
                    return false;

                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                if (!GetMonitorInfo(monitor, ref monitorInfo))
                    return false;

                RECT monitorRect = monitorInfo.rcMonitor;
                return windowRect.Left <= monitorRect.Left &&
                    windowRect.Top <= monitorRect.Top &&
                    windowRect.Right >= monitorRect.Right &&
                    windowRect.Bottom >= monitorRect.Bottom;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private static bool IsAnyKeyDown(List<int> pressedVirtualKeys, params int[] virtualKeys)
        {
            if (pressedVirtualKeys != null)
                return virtualKeys.Any(key => pressedVirtualKeys.Contains(key));

            return virtualKeys.Any(key => (GetAsyncKeyState(key) & 0x8000) != 0);
        }

        private static bool TryGetVirtualKey(string keyName, out int virtualKey)
        {
            virtualKey = 0;
            if (string.IsNullOrWhiteSpace(keyName))
                return false;

            Keys key;
            string normalizedKeyName = keyName.Replace("_", string.Empty).Replace(" ", string.Empty);
            if (!Enum.TryParse(normalizedKeyName, true, out key))
                return false;

            virtualKey = (int)key;
            return virtualKey > 0 && virtualKey <= 0xFE;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private class RepeatableCommand
        {
            public RepeatableCommand(ICommand command, bool activateWindow)
            {
                CommandSettings = command.CommandSettings;
                Name = command.Name;
                PluginClass = command.PluginClass;
                PluginFilename = command.PluginFilename;
                ActivateWindow = activateWindow;
            }

            private RepeatableCommand()
            {
            }

            public string CommandSettings { get; set; }
            public string Name { get; set; }
            public string PluginClass { get; set; }
            public string PluginFilename { get; set; }
            public bool ActivateWindow { get; set; }

            public RepeatableCommand Clone()
            {
                return new RepeatableCommand
                {
                    CommandSettings = CommandSettings,
                    Name = Name,
                    PluginClass = PluginClass,
                    PluginFilename = PluginFilename,
                    ActivateWindow = ActivateWindow
                };
            }

            public ICommand ToCommand()
            {
                return new Command
                {
                    CommandSettings = CommandSettings,
                    IsEnabled = true,
                    Name = Name,
                    PluginClass = PluginClass,
                    PluginFilename = PluginFilename
                };
            }
        }

        #endregion

        #region ILoadable Methods

        public void Load(IHostControl host, SynchronizationContext syncContext = null)
        {
            _mainContext = syncContext;
            // Create empty list of plugins, then load as many as possible from plugin directory
            LoadPlugins(host);

            if (host == null) return;
            host.PointCapture.GestureRecognized += PointCapture_GestureRecognized;
        }

        #endregion
    }
}
