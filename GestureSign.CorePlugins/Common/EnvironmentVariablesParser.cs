using GestureSign.Common.Plugins;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ManagedWinapi.Windows;

namespace GestureSign.CorePlugins.Common
{
    public class EnvironmentVariablesParser
    {
        PointInfo _pointInfo;
        public EnvironmentVariablesParser(PointInfo pointInfo)
        {
            _pointInfo = pointInfo;
        }

        public string ExpandEnvironmentVariables(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return command;

            command = Environment.ExpandEnvironmentVariables(command);

            if (command.Contains("%GS_Clipboard%"))
            {
                string clipboardString = string.Empty;
                _pointInfo.Invoke(() =>
                {
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData != null && iData.GetDataPresent(DataFormats.Text))
                    {
                        clipboardString = (string)iData.GetData(DataFormats.Text);
                    }
                });
                if (!string.IsNullOrEmpty(clipboardString))
                    command = command.Replace("%GS_Clipboard%", clipboardString);
            }

            SystemWindow window = null;
            try
            {
                window = _pointInfo.Window;
            }
            catch
            {
            }

            string className = GetWindowValue(window, w => w.ClassName);
            string title = GetWindowValue(window, w => w.Title);
            int? processId = GetWindowProcessId(window);

            if (command.Contains("%GS_ClassName%") && !string.IsNullOrEmpty(className))
            {
                command = command.Replace("%GS_ClassName%", className);
            }
            if (command.Contains("%GS_Title%") && !string.IsNullOrEmpty(title))
            {
                command = command.Replace("%GS_Title%", title);
            }
            if (command.Contains("%GS_PID%") && processId.HasValue)
            {
                command = command.Replace("%GS_PID%", processId.Value.ToString());
            }

            Point? startPoint = GetStartPoint();
            Point? endPoint = GetEndPoint();

            if (startPoint.HasValue)
            {
                command = command.Replace("%GS_StartPoint_X%", startPoint.Value.X.ToString())
                    .Replace("%GS_StartPoint_Y%", startPoint.Value.Y.ToString());
            }

            if (endPoint.HasValue)
            {
                command = command.Replace("%GS_EndPoint_X%", endPoint.Value.X.ToString())
                    .Replace("%GS_EndPoint_Y%", endPoint.Value.Y.ToString());
            }

            return command.Replace("%GS_WindowHandle%", _pointInfo.WindowHandle.ToString());
        }

        private Point? GetStartPoint()
        {
            try
            {
                if (_pointInfo.PointLocation == null || _pointInfo.PointLocation.Count == 0)
                    return null;

                return _pointInfo.PointLocation.First();
            }
            catch
            {
                return null;
            }
        }

        private Point? GetEndPoint()
        {
            try
            {
                if (_pointInfo.Points == null ||
                    _pointInfo.Points.Count == 0 ||
                    _pointInfo.Points[0] == null ||
                    _pointInfo.Points[0].Count == 0)
                    return null;

                return _pointInfo.Points[0].Last();
            }
            catch
            {
                return null;
            }
        }

        private static string GetWindowValue(SystemWindow window, Func<SystemWindow, string> valueSelector)
        {
            try
            {
                if (window == null || window.HWnd == IntPtr.Zero)
                    return string.Empty;

                return valueSelector(window);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int? GetWindowProcessId(SystemWindow window)
        {
            try
            {
                if (window == null || window.HWnd == IntPtr.Zero)
                    return null;

                return window.ProcessId;
            }
            catch
            {
                return null;
            }
        }
    }
}
