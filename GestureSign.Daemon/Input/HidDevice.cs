using GestureSign.Common.Input;
using GestureSign.Common.Log;
using GestureSign.Daemon.Native;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GestureSign.Daemon.Input
{
    public abstract class HidDevice : IDevice, IDisposable
    {
        private bool disposedValue;
        protected SafeUnmanagedMemoryHandle _hPreparsedData;
        protected IntPtr _pRawData;
        protected int _dwCount;
        protected int _dwSizHid;
        protected Point _physicalMax;

        protected static bool _isAxisCorresponds;
        protected static bool _xAxisDirection;
        protected static bool _yAxisDirection;


        public abstract Devices DeviceType { get; }

        protected HidDevice(IntPtr rawInputBuffer, ref RAWINPUT raw)
        {
            _hPreparsedData = GetPreparsedData(raw.header.hDevice);
            _pRawData = GetRawDataPtr(rawInputBuffer, ref raw);
            _dwCount = raw.hid.dwCount;
            _dwSizHid = raw.hid.dwSizHid;
        }

        protected static SafeUnmanagedMemoryHandle GetPreparsedData(IntPtr hDevice)
        {
            uint pcbSize = 0;
            if (!TryGetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref pcbSize, "query preparsed-data size") || pcbSize == 0)
            {
                throw new InvalidOperationException($"Raw input device preparsed data is unavailable. hDevice=0x{hDevice.ToInt64():X}, size={pcbSize}");
            }

            IntPtr pPreparsedData = Marshal.AllocHGlobal((int)pcbSize);
            if (!TryGetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_PREPARSEDDATA, pPreparsedData, ref pcbSize, "read preparsed data"))
            {
                Marshal.FreeHGlobal(pPreparsedData);
                throw new InvalidOperationException($"Failed to read raw input device preparsed data. hDevice=0x{hDevice.ToInt64():X}, size={pcbSize}");
            }

            return new SafeUnmanagedMemoryHandle(pPreparsedData);
        }

        protected static IntPtr GetRawDataPtr(IntPtr rawInputBuffer, ref RAWINPUT raw)
        {
            return new IntPtr(rawInputBuffer.ToInt64() + (raw.header.dwSize - raw.hid.dwSizHid * raw.hid.dwCount));
        }

        protected static ushort[] GetButtonList(IntPtr pPreparsedData, IntPtr pRawData, short nodeIndex, int rawDateSize)
        {
            int usageLength = 0;
            HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, null, ref usageLength, pPreparsedData, pRawData, rawDateSize);
            var usageList = new ushort[usageLength];
            HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, usageList, ref usageLength, pPreparsedData, pRawData, rawDateSize);
            return usageList;
        }

        protected virtual Point GetCoordinate(short linkCollection, Screen currentScr, IntPtr pRawDataPacket)
        {
            int physicalX = 0;
            int physicalY = 0;

            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, linkCollection, NativeMethods.XCoordinateId, ref physicalX, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);
            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, linkCollection, NativeMethods.YCoordinateId, ref physicalY, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);

            int x, y;
            if (_isAxisCorresponds)
            {
                x = physicalX * currentScr.Bounds.Width / _physicalMax.X;
                y = physicalY * currentScr.Bounds.Height / _physicalMax.Y;
            }
            else
            {
                x = physicalY * currentScr.Bounds.Width / _physicalMax.Y;
                y = physicalX * currentScr.Bounds.Height / _physicalMax.X;
            }
            x = _xAxisDirection ? x : currentScr.Bounds.Width - x;
            y = _yAxisDirection ? y : currentScr.Bounds.Height - y;

            return new Point(x + currentScr.Bounds.X, y + currentScr.Bounds.Y);
        }

        public virtual Point GetCoordinate(short linkCollection, Screen currentScr)
        {
            return GetCoordinate(linkCollection, currentScr, _pRawData);
        }

        public virtual int GetContactCount()
        {
            int contactCount;
            if (!TryGetContactCount(out contactCount))
            {
                throw new ApplicationException(Common.Localization.LocalizationProvider.Instance.GetTextValue("Messages.ContactCountError"));
            }
            return contactCount;
        }

        public virtual bool TryGetContactCount(out int contactCount)
        {
            contactCount = 0;
            int status = HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.DigitizerUsagePage, 0, NativeMethods.ContactCountId,
                ref contactCount, _hPreparsedData.DangerousGetHandle(), _pRawData, _dwSizHid);

            if (status != HidNativeApi.HIDP_STATUS_SUCCESS)
                return false;

            contactCount = Math.Max(0, contactCount);
            return true;
        }

        public virtual HidNativeApi.HIDP_LINK_COLLECTION_NODE[] GetLinkCollectionNodes()
        {
            int linkCount = 0;
            HidNativeApi.HidP_GetLinkCollectionNodes(null, ref linkCount, _hPreparsedData.DangerousGetHandle());
            HidNativeApi.HIDP_LINK_COLLECTION_NODE[] lcn = new HidNativeApi.HIDP_LINK_COLLECTION_NODE[linkCount];
            HidNativeApi.HidP_GetLinkCollectionNodes(lcn, ref linkCount, _hPreparsedData.DangerousGetHandle());
            return lcn;
        }

        public virtual int GetContactId(short nodeIndex, IntPtr pRawDataPacket)
        {
            int contactIdentifier;
            return TryGetContactId(nodeIndex, pRawDataPacket, out contactIdentifier)
                ? contactIdentifier
                : nodeIndex;
        }

        protected virtual bool TryGetContactId(short nodeIndex, IntPtr pRawDataPacket, out int contactIdentifier)
        {
            contactIdentifier = 0;
            return HidNativeApi.HIDP_STATUS_SUCCESS == HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, NativeMethods.ContactIdentifierId, ref contactIdentifier, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);
        }

        public virtual int InferContactCount(short numberOfChildren)
        {
            return Math.Max(0, _dwCount) * Math.Max(0, (int)numberOfChildren);
        }

        public static void GetCurrentScreenOrientation()
        {
            switch (SystemInformation.ScreenOrientation)
            {
                case ScreenOrientation.Angle0:
                    _xAxisDirection = _yAxisDirection = true;
                    _isAxisCorresponds = true;
                    break;
                case ScreenOrientation.Angle90:
                    _isAxisCorresponds = false;
                    _xAxisDirection = false;
                    _yAxisDirection = true;
                    break;
                case ScreenOrientation.Angle180:
                    _xAxisDirection = _yAxisDirection = false;
                    _isAxisCorresponds = true;
                    break;
                case ScreenOrientation.Angle270:
                    _isAxisCorresponds = false;
                    _xAxisDirection = true;
                    _yAxisDirection = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Devices EnumerateDevices()
        {
            Devices foundDevices = Devices.None;
            uint deviceCount = 0;
            int dwSize = Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));

            uint countResult = NativeMethods.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize);
            if (countResult == uint.MaxValue)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"GetRawInputDeviceList failed while querying device count. itemSize={dwSize}");
            }

            if (countResult == 0)
            {
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                using (new SafeUnmanagedMemoryHandle(pRawInputDeviceList))
                {
                    uint listResult = NativeMethods.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);
                    if (listResult == uint.MaxValue)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"GetRawInputDeviceList failed while reading device list. itemSize={dwSize}, deviceCount={deviceCount}");
                    }

                    for (int i = 0; i < listResult; i++)
                    {
                        uint pSize = 0;

                        RAWINPUTDEVICELIST rid = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(
                            IntPtr.Add(pRawInputDeviceList, dwSize * i),
                            typeof(RAWINPUTDEVICELIST));

                        if (!TryGetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICEINFO, IntPtr.Zero, ref pSize, "query enumerated device info size"))
                            continue;

                        if (pSize <= 0)
                            continue;

                        IntPtr pInfo = Marshal.AllocHGlobal((int)pSize);
                        using (new SafeUnmanagedMemoryHandle(pInfo))
                        {
                            InitializeRawDeviceInfoBuffer(pInfo);
                            if (!TryGetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICEINFO, pInfo, ref pSize, "read enumerated device info"))
                                continue;

                            var info = (RID_DEVICE_INFO)Marshal.PtrToStructure(pInfo, typeof(RID_DEVICE_INFO));
                            if (info.dwType != NativeMethods.RIM_TYPEHID || info.hid.usUsagePage != NativeMethods.DigitizerUsagePage)
                                continue;

                            switch (info.hid.usUsage)
                            {
                                case NativeMethods.TouchPadUsage:
                                    foundDevices |= Devices.TouchPad;
                                    break;
                                case NativeMethods.TouchScreenUsage:
                                    foundDevices |= Devices.TouchScreen;
                                    break;
                                case NativeMethods.PenUsage:
                                    foundDevices |= Devices.Pen;
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }
                }
                return foundDevices;
            }
            else
            {
                throw new InvalidOperationException($"GetRawInputDeviceList returned unexpected query result. result={countResult}, deviceCount={deviceCount}, itemSize={dwSize}");
            }
        }

        private static bool TryGetRawInputDeviceInfo(IntPtr hDevice, uint command, IntPtr data, ref uint size, string operation)
        {
            uint result = NativeMethods.GetRawInputDeviceInfo(hDevice, command, data, ref size);
            if (result != uint.MaxValue)
                return true;

            Logging.LogException(new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"GetRawInputDeviceInfo failed during {operation}. hDevice=0x{hDevice.ToInt64():X}, command=0x{command:X}, size={size}"));
            return false;
        }

        public static void InitializeRawDeviceInfoBuffer(IntPtr pInfo)
        {
            var deviceInfo = new RID_DEVICE_INFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(RID_DEVICE_INFO))
            };
            Marshal.StructureToPtr(deviceInfo, pInfo, false);
        }

        public bool TryGetPhysicalMax(int collectionCount)
        {
            short valueCapsLength = (short)(collectionCount > 0 ? collectionCount : 1);
            HidNativeApi.HidP_Value_Caps[] hvc = new HidNativeApi.HidP_Value_Caps[valueCapsLength];

            if (HidNativeApi.HIDP_STATUS_SUCCESS != HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, NativeMethods.GenericDesktopPage, 0, NativeMethods.XCoordinateId, hvc, ref valueCapsLength, _hPreparsedData.DangerousGetHandle()) ||
                valueCapsLength <= 0)
            {
                _physicalMax = Point.Empty;
                return false;
            }
            _physicalMax.X = hvc[0].PhysicalMax != 0 ? hvc[0].PhysicalMax : hvc[0].LogicalMax;

            valueCapsLength = (short)(collectionCount > 0 ? collectionCount : 1);
            hvc = new HidNativeApi.HidP_Value_Caps[valueCapsLength];
            if (HidNativeApi.HIDP_STATUS_SUCCESS != HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, NativeMethods.GenericDesktopPage, 0, NativeMethods.YCoordinateId, hvc, ref valueCapsLength, _hPreparsedData.DangerousGetHandle()) ||
                valueCapsLength <= 0)
            {
                _physicalMax = Point.Empty;
                return false;
            }
            _physicalMax.Y = hvc[0].PhysicalMax != 0 ? hvc[0].PhysicalMax : hvc[0].LogicalMax;

            if (_physicalMax.X <= 0 || _physicalMax.Y <= 0)
            {
                _physicalMax = Point.Empty;
                return false;
            }

            return true;
        }

        public void GetPhysicalMax(int collectionCount)
        {
            if (!TryGetPhysicalMax(collectionCount))
                throw new ApplicationException("Invalid HID coordinate range.");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                _hPreparsedData.Dispose();
                disposedValue = true;
            }
        }

        ~HidDevice()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
