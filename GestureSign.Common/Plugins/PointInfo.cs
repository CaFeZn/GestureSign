using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using GestureSign.Common.Applications;
using ManagedWinapi.Windows;

namespace GestureSign.Common.Plugins
{
    public class PointInfo
    {
        #region Private Variables

        private List<Point> _pointLocation;
        private SystemWindow _targetWindow;
        private SynchronizationContext _syncContext;

        #endregion

        #region Constructors

        public PointInfo(List<Point> pointLocation, List<List<Point>> points, SystemWindow target, SynchronizationContext syncContext)
        {
            _pointLocation = pointLocation;
            Points = points;
            _targetWindow = target;
            _syncContext = syncContext;
        }

        #endregion

        #region Public Properties

        public List<Point> PointLocation
        {
            get { return _pointLocation; }
            set
            {
                _pointLocation = value;
            }
        }

        public IntPtr WindowHandle => ResolveTargetWindow()?.HWnd ?? IntPtr.Zero;

        public SystemWindow Window
        {
            get
            {
                return ResolveTargetWindow();
            }
        }

        public List<List<Point>> Points { get; set; }

        #endregion

        #region Public Methods

        public void Invoke(System.Action action)
        {
            _syncContext.Send((o) => action.Invoke(), null);
        }

        public void SetTargetWindow(SystemWindow targetWindow)
        {
            if (targetWindow == null ||
                targetWindow.HWnd == IntPtr.Zero ||
                ApplicationManager.IsShellUiWindow(targetWindow))
                return;

            _targetWindow = targetWindow;
        }

        private SystemWindow ResolveTargetWindow()
        {
            if (_targetWindow != null &&
                _targetWindow.HWnd != IntPtr.Zero &&
                !ApplicationManager.IsShellUiWindow(_targetWindow))
            {
                return _targetWindow;
            }

            var foregroundWindow = SystemWindow.ForegroundWindow;
            if (foregroundWindow != null &&
                foregroundWindow.HWnd != IntPtr.Zero &&
                !ApplicationManager.IsShellUiWindow(foregroundWindow))
            {
                _targetWindow = foregroundWindow;
                return _targetWindow;
            }

            if (_pointLocation != null && _pointLocation.Count != 0)
            {
                _targetWindow = ApplicationManager.Instance.GetWindowFromPoint(_pointLocation[0]);
            }

            return _targetWindow;
        }

        #endregion
    }
}
