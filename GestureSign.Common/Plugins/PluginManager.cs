using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
            ExecuteAction(executableActions, pointCapture.Mode, pointCapture.SourceDevice, e.ContactIdentifiers, e.FirstCapturedPoints, e.Points);
        }

        #endregion

        #region Public Methods

        public void ExecuteAction(List<IAction> executableActions, CaptureMode mode, Devices devices, List<int> contactIdentifiers, List<Point> firstCapturedPoints, List<List<Point>> points, List<int> conditionContactIdentifiers = null, List<List<Point>> conditionPoints = null)
        {
            // Exit if we're teaching
            if (mode == CaptureMode.Training)
                return;
            var target = ApplicationManager.Instance.CaptureWindow;
            var pointsForCondition = conditionPoints ?? points;
            var contactIdentifiersForCondition = conditionContactIdentifiers ?? contactIdentifiers;
            var pointInfo = new PointInfo(firstCapturedPoints, points, target, _mainContext);
            var action = new Action<object>(o =>
            {
                foreach (IAction executableAction in executableActions)
                {
                    // Exit if there is no action configured
                    if (executableAction == null || (executableAction.IgnoredDevices & devices) != 0 ||
                    executableAction.Commands == null || !Compute(executableAction.Condition, pointsForCondition, contactIdentifiersForCondition))
                        continue;

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
            target?.WaitForIdle(200);

            if (activateWindow)
                ActivateWindow(target);

            // Load action settings into plugin
            pluginInfo.Plugin.Deserialize(command.CommandSettings);

            // Execute plugin process
            bool success = pluginInfo.Plugin.Gestured(pointInfo);
            if (success && recordCommand)
                StoreLastCommand(command, pluginInfo, repeatActivateWindow);

            return success;
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

        private static bool ShouldActivateWindow(IAction executableAction, IPlugin plugin)
        {
            return executableAction.ActivateWindow == null && plugin.ActivateWindowDefault ||
                executableAction.ActivateWindow.GetValueOrDefault();
        }

        private static void ActivateWindow(SystemWindow target)
        {
            if (target != null && target.HWnd.ToInt64() != SystemWindow.ForegroundWindow?.HWnd.ToInt64())
                SystemWindow.ForegroundWindow = target;
        }

        private static bool IsNonRepeatable(IPluginInfo pluginInfo)
        {
            return pluginInfo.Plugin is INonRepeatablePlugin;
        }

        private bool Compute(string condition, List<List<Point>> pointList, List<int> contactIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;

            string expression = GetExpression(condition, pointList, contactIdentifiers);
            try
            {
                DataTable dataTable = new DataTable();
                var result = dataTable.Compute(expression, null);
                return result is DBNull || Convert.ToBoolean(result);
            }
            catch (EvaluateException)
            {
                return false;
            }
        }

        private string GetExpression(string condition, List<List<Point>> pointList, List<int> contactIdentifiers)
        {
            for (int i = 1; i <= pointList.Count; i++)
            {
                int startX = pointList[i - 1].FirstOrDefault().X;
                int startY = pointList[i - 1].FirstOrDefault().Y;
                int endX = pointList[i - 1].LastOrDefault().X;
                int endY = pointList[i - 1].LastOrDefault().Y;

                if (condition.Contains('%'))
                {
                    int left = (int)System.Windows.SystemParameters.VirtualScreenLeft;
                    int top = (int)System.Windows.SystemParameters.VirtualScreenTop;
                    int width = (int)System.Windows.SystemParameters.VirtualScreenWidth;
                    int height = (int)System.Windows.SystemParameters.VirtualScreenHeight;
                    condition = ReplaceVariables(condition, i, "start_X%", (startX - left) * 100 / width);
                    condition = ReplaceVariables(condition, i, "start_Y%", (startY - top) * 100 / height);
                    condition = ReplaceVariables(condition, i, "end_X%", (endX - left) * 100 / width);
                    condition = ReplaceVariables(condition, i, "end_Y%", (endY - top) * 100 / height);
                }

                condition = ReplaceVariables(condition, i, "start_X", startX);
                condition = ReplaceVariables(condition, i, "start_Y", startY);
                condition = ReplaceVariables(condition, i, "end_X", endX);
                condition = ReplaceVariables(condition, i, "end_Y", endY);

                condition = ReplaceVariables(condition, i, "ID", contactIdentifiers[i - 1]);
            }
            return condition;
        }

        private string ReplaceVariables(string str, int id, string key, int value)
        {
            string variable = $"finger_{id}_{key}";
            return str.Replace(variable, value.ToString());
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
