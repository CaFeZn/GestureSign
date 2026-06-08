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

        private SystemWindow ResolveTargetWindow()
        {
            var foregroundWindow = SystemWindow.ForegroundWindow;
            if (foregroundWindow != null &&
                foregroundWindow.HWnd != IntPtr.Zero &&
                !ApplicationManager.IsShellUiWindow(foregroundWindow))
            {
                if (_targetWindow == null || _targetWindow.HWnd != foregroundWindow.HWnd)
                    _targetWindow = foregroundWindow;

                return _targetWindow;
            }

            if ((_targetWindow == null || _targetWindow.HWnd == IntPtr.Zero) &&
                _pointLocation != null &&
                _pointLocation.Count != 0)
            {
                _targetWindow = ApplicationManager.Instance.GetWindowFromPoint(_pointLocation[0]);
            }

            return _targetWindow;
        }

        #endregion
    }
}
