using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GestureSign.Common.Input
{
    public class RecognitionEventArgs : EventArgs
    {
        #region Constructors

        public RecognitionEventArgs(List<List<Point>> points, List<Point> capturePoints, List<int> contactIdentifiers)
        {
            this.Points = points;
            this.FirstCapturedPoints = capturePoints;
            ContactIdentifiers = contactIdentifiers;
        }

        public RecognitionEventArgs(List<List<Point>> points, List<Point> capturePoints, List<int> contactIdentifiers, List<int> pressedVirtualKeys)
            : this(points, capturePoints, contactIdentifiers)
        {
            PressedVirtualKeys = pressedVirtualKeys;
        }

        public RecognitionEventArgs(string gestureName, List<List<Point>> points, List<Point> capturePoints, List<int> contactIdentifiers)
            : this(points, capturePoints, contactIdentifiers)
        {
            this.GestureName = gestureName;
        }

        public RecognitionEventArgs(string gestureName, List<List<Point>> points, List<Point> capturePoints, List<int> contactIdentifiers, List<int> pressedVirtualKeys)
            : this(gestureName, points, capturePoints, contactIdentifiers)
        {
            PressedVirtualKeys = pressedVirtualKeys;
        }

        #endregion

        #region Public Instance Properties

        public string GestureName { get; set; }
        public List<List<Point>> Points { get; set; }
        public List<Point> FirstCapturedPoints { get; set; }
        public List<int> ContactIdentifiers { get; set; }
        public List<int> PressedVirtualKeys { get; set; }

        #endregion
    }
}
