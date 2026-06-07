using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using GestureSign.ControlPanel.Localization;
using ManagedWinapi.Windows;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Management.Deployment;

namespace GestureSign.ControlPanel
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private const double MouseWheelDeltaForOneLine = 120.0;
        private const double MouseWheelPixelsPerLine = 16.0;
        private const double ScrollOffsetTolerance = 0.001;
        private static readonly DependencyProperty PendingMouseWheelLinesProperty =
            DependencyProperty.RegisterAttached("PendingMouseWheelLines", typeof(double), typeof(App), new PropertyMetadata(0.0));

        Mutex mutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Logging.LoggedExceptionOccurred += (o, ex) => ShowException(ex);
            Logging.OpenLogFile();
            LoadLanguageData();

            bool createdNew;
            mutex = new Mutex(true, Constants.ControlPanel, out createdNew);
            if (createdNew)
            {
                if (AppConfig.UiAccess && VersionHelper.IsWindows10OrGreater())
                    if (TryLaunchStoreVersion())
                        return;

                GestureManager.Instance.Load(null);
                GestureSign.Common.Plugins.PluginManager.Instance.Load(null);
                ApplicationManager.Instance.Load(null);

                NamedPipe.Instance.RunNamedPipeServer(Constants.ControlPanel, new MessageProcessor());

                ApplicationManager.ApplicationSaved += (o, ea) => NamedPipe.SendMessageAsync(IpcCommands.LoadApplications, Constants.Daemon);
                GestureManager.GestureSaved += (o, ea) => NamedPipe.SendMessageAsync(IpcCommands.LoadGestures, Constants.Daemon);
                AppConfig.ConfigChanged += (o, ea) =>
                {
                    NamedPipe.SendMessageAsync(IpcCommands.LoadConfiguration, Constants.Daemon);
                };
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                ShowControlPanel();
                // use Dispatcher to resolve exception 0xc0020001
                Current.Dispatcher.InvokeAsync(() => Current.Shutdown(), DispatcherPriority.ApplicationIdle);
            }
        }

        private void LoadLanguageData()
        {
            if (!LocalizationProvider.Instance.LoadFromFile("ControlPanel"))
            {
                LocalizationProvider.Instance.LoadFromResource(ControlPanel.Properties.Resources.en);
            }

            Current.Resources["DefaultFlowDirection"] = LocalizationProviderEx.FlowDirection;
            var font = LocalizationProviderEx.Font;
            var headerFontFamily = LocalizationProviderEx.HeaderFontFamily;
            if (font != null)
                Current.Resources["DefaultFont"] =
                    Current.Resources["ContentFontFamily"] =
                    Current.Resources["ToggleSwitchFontFamily"] =
                    Current.Resources["ToggleSwitchHeaderFontFamily"] =
                    Current.Resources["ToggleSwitchFontFamily.Win10"] =
                    Current.Resources["ToggleSwitchHeaderFontFamily.Win10"] = font;
            if (headerFontFamily != null)
                Current.Resources["HeaderFontFamily"] = headerFontFamily;
        }

        private bool ShowControlPanel()
        {
            Process current = Process.GetCurrentProcess();
            var controlPanelProcesses = Process.GetProcessesByName(current.ProcessName);

            if (controlPanelProcesses.Length > 1)
            {
                foreach (Process process in controlPanelProcesses)
                {
                    if (process.Id != current.Id)
                    {
                        var window = new SystemWindow(process.MainWindowHandle);

                        if (window.WindowState == System.Windows.Forms.FormWindowState.Minimized)
                        {
                            window.RestoreWindow();
                        }
                        SystemWindow.ForegroundWindow = window;
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        private bool TryLaunchStoreVersion()
        {
            using (var currentUser = WindowsIdentity.GetCurrent())
            {
                if (currentUser.User != null)
                {
                    var sid = currentUser.User.ToString();
                    PackageManager packageManager = new PackageManager();
                    var storeVersion = packageManager.FindPackagesForUserWithPackageTypes(sid, "41908Transpy.GestureSign", "CN=AF41F066-0041-4D13-9D95-9DAB66112B0A", PackageTypes.Main).FirstOrDefault();
                    if (storeVersion != null)
                    {
                        using (Process explorer = new Process
                        {
                            StartInfo =
                                    {
                                        FileName = "explorer.exe", Arguments = @"shell:AppsFolder\" + "41908Transpy.GestureSign_f441wk0cxr8zc!GestureSign"
                                    }
                        })
                        {
                            explorer.Start();
                        }
                        Current.Shutdown();
                        return true;
                    }
                }
            }
            return false;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mutex != null)
            {
                NamedPipe.Instance.Dispose();
                mutex.Dispose();
            }
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logging.LogMessage("AppDomain.CurrentDomain.UnhandledException");
                Logging.LogException((Exception)e.ExceptionObject);
                ShowException((Exception)e.ExceptionObject);
            };

            DispatcherUnhandledException += (s, e) =>
            {
                Logging.LogMessage("Application.Current.DispatcherUnhandledException");
                Logging.LogException(e.Exception);
                ShowException(e.Exception);
                e.Handled = true;
                Environment.Exit(0);
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Logging.LogMessage("TaskScheduler.UnobservedTaskException");
                Logging.LogException(e.Exception);
                ShowException(e.Exception);
                e.SetObserved();
            };
        }

        private void ShowException(Exception exception)
        {
            string message = null;
            if (exception is GestureSign.Common.Exceptions.FileWriteException)
            {
                message += Environment.NewLine + Environment.NewLine + LocalizationProvider.Instance.GetTextValue("Messages.FileWriteException");
            }

            while (exception.InnerException != null)
                exception = exception.InnerException;

            MessageBox.Show(exception.Message + message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            SetupExceptionHandling();
            AppContext.SetSwitch("Switch.System.Windows.DoNotScaleForDpiChanges", false);
            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);
            EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(ScrollViewer_PreviewMouseWheel));
            base.OnStartup(e);
        }

        private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null || e.Delta == 0 || HasNestedScrollableScrollViewer(scrollViewer, e.OriginalSource as DependencyObject, e.Delta))
                return;

            if (!CanScrollVertically(scrollViewer, e.Delta))
            {
                scrollViewer.SetValue(PendingMouseWheelLinesProperty, 0.0);
                e.Handled = true;
                return;
            }

            int wheelScrollLines = SystemParameters.WheelScrollLines;
            if (wheelScrollLines == 0)
            {
                e.Handled = true;
                return;
            }

            e.Handled = true;

            if (wheelScrollLines < 0 || wheelScrollLines == int.MaxValue)
            {
                ScrollByPage(scrollViewer, e.Delta);
                return;
            }

            double lineDelta = e.Delta / MouseWheelDeltaForOneLine * wheelScrollLines;
            if (UsesPixelScrolling(scrollViewer))
            {
                ScrollToVerticalOffset(scrollViewer, scrollViewer.VerticalOffset - lineDelta * MouseWheelPixelsPerLine);
                return;
            }

            double pendingLines = (double)scrollViewer.GetValue(PendingMouseWheelLinesProperty) + lineDelta;
            int wholeLines = pendingLines > 0 ? (int)Math.Floor(pendingLines) : (int)Math.Ceiling(pendingLines);
            scrollViewer.SetValue(PendingMouseWheelLinesProperty, pendingLines - wholeLines);
            ScrollByLines(scrollViewer, wholeLines);
        }

        private static void ScrollByPage(ScrollViewer scrollViewer, int delta)
        {
            if (UsesPixelScrolling(scrollViewer))
            {
                ScrollToVerticalOffset(scrollViewer, scrollViewer.VerticalOffset - delta / MouseWheelDeltaForOneLine * scrollViewer.ViewportHeight);
                return;
            }

            double pendingPages = (double)scrollViewer.GetValue(PendingMouseWheelLinesProperty) + delta / MouseWheelDeltaForOneLine;
            int wholePages = pendingPages > 0 ? (int)Math.Floor(pendingPages) : (int)Math.Ceiling(pendingPages);
            scrollViewer.SetValue(PendingMouseWheelLinesProperty, pendingPages - wholePages);

            for (int i = 0; i < Math.Abs(wholePages); i++)
            {
                if (wholePages > 0)
                    scrollViewer.PageUp();
                else
                    scrollViewer.PageDown();
            }
        }

        private static void ScrollByLines(ScrollViewer scrollViewer, int lineCount)
        {
            for (int i = 0; i < Math.Abs(lineCount); i++)
            {
                if (lineCount > 0)
                    scrollViewer.LineUp();
                else
                    scrollViewer.LineDown();
            }
        }

        private static void ScrollToVerticalOffset(ScrollViewer scrollViewer, double offset)
        {
            scrollViewer.ScrollToVerticalOffset(Math.Max(0.0, Math.Min(scrollViewer.ScrollableHeight, offset)));
        }

        private static bool UsesPixelScrolling(ScrollViewer scrollViewer)
        {
            if (!scrollViewer.CanContentScroll)
                return true;

            var itemsControl = FindAncestor<ItemsControl>(scrollViewer);
            return itemsControl != null && VirtualizingPanel.GetScrollUnit(itemsControl) == ScrollUnit.Pixel;
        }

        private static bool CanScrollVertically(ScrollViewer scrollViewer, int delta)
        {
            if (scrollViewer.ScrollableHeight <= ScrollOffsetTolerance)
                return false;

            return delta > 0
                ? scrollViewer.VerticalOffset > ScrollOffsetTolerance
                : scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight - ScrollOffsetTolerance;
        }

        private static bool HasNestedScrollableScrollViewer(ScrollViewer scrollViewer, DependencyObject originalSource, int delta)
        {
            DependencyObject current = originalSource;
            while (current != null && !ReferenceEquals(current, scrollViewer))
            {
                var nestedScrollViewer = current as ScrollViewer;
                if (nestedScrollViewer != null && CanScrollVertically(nestedScrollViewer, delta))
                    return true;

                current = GetParent(current);
            }
            return false;
        }

        private static T FindAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            DependencyObject current = GetParent(element);
            while (current != null)
            {
                var typedCurrent = current as T;
                if (typedCurrent != null)
                    return typedCurrent;

                current = GetParent(current);
            }
            return null;
        }

        private static DependencyObject GetParent(DependencyObject element)
        {
            if (element == null)
                return null;

            if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
            {
                DependencyObject visualParent = VisualTreeHelper.GetParent(element);
                if (visualParent != null)
                    return visualParent;
            }

            return LogicalTreeHelper.GetParent(element);
        }
    }
}
