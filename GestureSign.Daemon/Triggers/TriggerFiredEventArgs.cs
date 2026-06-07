using GestureSign.Common.Applications;
using GestureSign.Common.Input;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GestureSign.Daemon.Triggers
{
    public class TriggerFiredEventArgs : EventArgs
    {
        public TriggerFiredEventArgs(List<IAction> firedActions, Point firedPoint, Devices sourceDevice = Devices.None)
        {
            FiredActions = firedActions;
            FiredPoint = firedPoint;
            SourceDevice = sourceDevice;
        }

        public List<IAction> FiredActions { get; }
        public Point FiredPoint { get; }
        public Devices SourceDevice { get; }
    }
}
