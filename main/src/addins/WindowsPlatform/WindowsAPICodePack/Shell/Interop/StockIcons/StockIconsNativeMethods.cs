//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Shell
{
    internal static class StockIconsNativeMethods
    {
        #region StockIcon declarations

        /// <summary>
        /// Specifies options for the appearance of the 
        /// stock icon.
        /// </summary>
        [Flags]
        internal enum StockIconOptions
        {
            /// <summary>
            /// Retrieve the small version of the icon, as specified by  
            /// SM_CXICON and SM_CYICON system metrics.
            /// </summary>
            Large = 0x000000000,

            /// <summary>
            /// Retrieve the small version of the icon, as specified by  
            /// SM_CXSMICON and SM_CYSMICON system metrics.
            /// </summary>
            Small = 0x000000001,

            /// <summary>
            /// Retrieve the shell-sized icons (instead of the 
            /// size specified by the system metrics). 
            /// </summary>
            ShellSize = 0x000000004,

            /// <summary>
            /// Specified that the hIcon member of the SHSTOCKICONINFO 
            /// structure receives a handle to the specified icon.
            /// </summary>
            Handle = 0x000000100,

            /// <summary>
            /// Specifies that the iSysImageImage member of the SHSTOCKICONINFO 
            /// structure receives the index of the specified 
            /// icon in the system imagelist.
            /// </summary>
            SystemIndex = 0x000004000,

            /// <summary>
            /// Adds the link overlay to the icon.
            /// </summary>
            LinkOverlay = 0x000008000,

            ///<summary>
            /// Adds the system highlight color to the icon.
            /// </summary>
            Selected = 0x000010000
        }

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct StockIconInfo
        {
            internal UInt32 StuctureSize;
            internal IntPtr Handle;
            internal Int32 ImageIndex;
            internal Int32 Identifier;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string Path;
        }

        [PreserveSig]
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode,
        ExactSpelling = true, SetLastError = false)]
        internal static extern HResult SHGetStockIconInfo(
            StockIconIdentifier identifier,
            StockIconOptions flags,
            ref StockIconInfo info);

        #endregion
    }
}
