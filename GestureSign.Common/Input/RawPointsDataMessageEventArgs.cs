using System;
using System.Collections.Generic;

namespace GestureSign.Common.Input
{
    public class RawPointsDataMessageEventArgs : EventArgs
    {
        #region Constructors

        public RawPointsDataMessageEventArgs(List<RawData> rawData, Devices device, IntPtr deviceHandle = default)
        {
            this.RawData = rawData;
            SourceDevice = device;
            DeviceHandle = deviceHandle;
        }


        #endregion

        #region Public Properties

        public List<RawData> RawData { get; set; }
        public Devices SourceDevice { get; set; }
        public IntPtr DeviceHandle { get; set; }

        #endregion
    }
}
