using System;
using System.Collections.Generic;
using System.Drawing;

namespace GestureSign.Common.Input
{
    public interface IPointCapture
    {
        event PointsCapturedEventHandler AfterPointsCaptured;
        event PointsCapturedEventHandler BeforePointsCaptured;
        event PointsCapturedEventHandler CaptureStarted;
        event EventHandler CaptureEnded;
        event PointsCapturedEventHandler CaptureCanceled;
        event RecognitionEventHandler GestureRecognized;
        event PointsCapturedEventHandler PointCaptured;
        List<CapturedContact> InputContacts { get; }
        List<Point>[] InputPoints { get; }
        List<int> InputContactIdentifiers { get; }
        List<int> CapturePressedVirtualKeys { get; }
        bool TemporarilyDisableCapture { get; set; }
        Devices SourceDevice { get; }
        CaptureState State { get; set; }
        CaptureMode Mode { get; set; }
    }
}
