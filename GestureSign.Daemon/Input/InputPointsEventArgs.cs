using System;
using System.Collections.Generic;
using System.Linq;
using GestureSign.Common.Input;

namespace GestureSign.Daemon.Input
{
    public class InputPointsEventArgs : EventArgs
    {
        #region Constructors

        public InputPointsEventArgs(List<InputPoint> inputPointList, Devices pointSource, IntPtr deviceHandle = default)
        {
            InputPointList = inputPointList;
            PointSource = pointSource;
            DeviceHandle = deviceHandle;
        }

        public InputPointsEventArgs(List<RawData> rawDataList, Devices pointSource, IntPtr deviceHandle = default)
        {
            InputPointList = rawDataList?.Select(rd => new InputPoint(rd.ContactIdentifier, rd.RawPoints)).ToList();
            PointSource = pointSource;
            DeviceHandle = deviceHandle;
        }

        #endregion

        #region Public Properties

        public List<InputPoint> InputPointList { get; set; }

        public bool Handled { get; set; }

        public Devices PointSource { get; set; }

        public IntPtr DeviceHandle { get; set; }

        #endregion
    }
}
