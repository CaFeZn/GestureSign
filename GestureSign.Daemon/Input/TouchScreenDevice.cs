using GestureSign.Common.Input;
using GestureSign.Daemon.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GestureSign.Daemon.Input
{
    public class TouchScreenDevice : HidDevice
    {
        public override Devices DeviceType => Devices.TouchScreen;

        public TouchScreenDevice(IntPtr rawInputBuffer, ref RAWINPUT raw) : base(rawInputBuffer, ref raw)
        {
        }

        public bool TryGetFirstTipPoint(short numberOfChildren, Screen currentScr, out Point point)
        {
            point = Point.Empty;
            if (_physicalMax.X <= 0 || _physicalMax.Y <= 0)
                return false;

            for (int dwIndex = 0; dwIndex < _dwCount; dwIndex++)
            {
                IntPtr pRawDataPacket = new IntPtr(_pRawData.ToInt64() + dwIndex * _dwSizHid);
                for (short nodeIndex = 1; nodeIndex <= numberOfChildren; nodeIndex++)
                {
                    ushort[] usageList = GetButtonList(_hPreparsedData.DangerousGetHandle(), pRawDataPacket, nodeIndex, _dwSizHid);
                    if (Array.IndexOf(usageList, NativeMethods.TipId) < 0)
                        continue;

                    point = GetCoordinate(nodeIndex, currentScr, pRawDataPacket);
                    return true;
                }
            }
            return false;
        }

        public Screen GetScreenFromPhysicalSize(Screen[] screens)
        {
            if (_physicalMax.X <= 0 || _physicalMax.Y <= 0)
                return null;

            double physicalWidth = _isAxisCorresponds ? _physicalMax.X : _physicalMax.Y;
            double physicalHeight = _isAxisCorresponds ? _physicalMax.Y : _physicalMax.X;
            double physicalRatio = physicalWidth / physicalHeight;

            Screen bestScreen = null;
            double bestScore = double.MaxValue;
            double secondScore = double.MaxValue;

            foreach (Screen screen in screens)
            {
                double screenRatio = (double)screen.Bounds.Width / screen.Bounds.Height;
                double score = Math.Abs(screenRatio - physicalRatio) / physicalRatio;

                if (score < bestScore)
                {
                    secondScore = bestScore;
                    bestScore = score;
                    bestScreen = screen;
                }
                else if (score < secondScore)
                {
                    secondScore = score;
                }
            }

            if (bestScreen != null && bestScore <= 0.02 && secondScore - bestScore >= 0.01)
                return bestScreen;

            return null;
        }

        public void GetRawDatas(short numberOfChildren, Screen currentScr, ref int requiringContactCount, ref List<RawData> _outputTouchs)
        {
            for (int dwIndex = 0; dwIndex < _dwCount; dwIndex++)
            {
                IntPtr pRawDataPacket = new IntPtr(_pRawData.ToInt64() + dwIndex * _dwSizHid);
                for (short nodeIndex = 1; nodeIndex <= numberOfChildren; nodeIndex++)
                {
                    int contactIdentifier = GetContactId(nodeIndex, pRawDataPacket);
                    Point point = GetCoordinate(nodeIndex, currentScr, pRawDataPacket);

                    ushort[] usageList = GetButtonList(_hPreparsedData.DangerousGetHandle(), pRawDataPacket, nodeIndex, _dwSizHid);
                    bool tip = Array.IndexOf(usageList, NativeMethods.TipId) >= 0;

                    _outputTouchs.Add(new RawData(tip ? DeviceStates.Tip : DeviceStates.None, contactIdentifier, point));

                    if (--requiringContactCount == 0) break;
                }
                if (requiringContactCount == 0) break;
            }
        }
    }
}
