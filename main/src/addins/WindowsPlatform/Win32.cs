using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Platform
{
    class Win32
    {
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
        public const uint SHGFI_SMALLICON = 0x1; // 'Small icon
        public const uint SHGFI_ICONLOCATION = 0x1000;
        public const uint SHGFI_TYPENAME = 0x400;

        [DllImport ("shell32.dll")]
        public static extern IntPtr SHGetFileInfo (string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
    }

    [StructLayout (LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public uint dwAttributes;
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };
}

