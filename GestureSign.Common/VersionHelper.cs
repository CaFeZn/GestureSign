using System;
using System.Runtime.InteropServices;

namespace GestureSign.Common
{
    public class VersionHelper
    {
        private const int StatusSuccess = 0;
        private static Version _osVersion;

        public static Version OsVersion
        {
            get
            {
                if (_osVersion == null)
                {
                    _osVersion = GetOsVersion();
                }
                return _osVersion;
            }
        }

        public static bool IsWindowsVistaOrGreater()
        {
            return OsVersion.Major >= 6;
        }

        public static bool IsWindows8OrGreater()
        {
            return OsVersion >= new Version(6, 2);
        }

        public static bool IsWindows8Point1OrGreater()
        {
            return OsVersion >= new Version(6, 3);
        }

        public static bool IsWindows10OrGreater()
        {
            return OsVersion >= new Version(10, 0);
        }

        public static bool IsWindows11OrGreater()
        {
            return OsVersion >= new Version(10, 0, 22000);
        }

        private static Version GetOsVersion()
        {
            try
            {
                var versionInfo = new OSVERSIONINFOEX();
                versionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

                if (RtlGetVersion(ref versionInfo) == StatusSuccess)
                {
                    return new Version(
                        versionInfo.dwMajorVersion,
                        versionInfo.dwMinorVersion,
                        versionInfo.dwBuildNumber);
                }
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }

            return Environment.OSVersion.Version;
        }

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }
    }
}
