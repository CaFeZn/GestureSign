using System.Collections.Generic;
using System.Drawing;

namespace GestureSign.Common.Input
{
    public class CapturedContact
    {
        public CapturedContact(int contactIdentifier, List<Point> points)
        {
            ContactIdentifier = contactIdentifier;
            Points = points ?? new List<Point>();
        }

        public int ContactIdentifier { get; }
        public List<Point> Points { get; }
    }
}
