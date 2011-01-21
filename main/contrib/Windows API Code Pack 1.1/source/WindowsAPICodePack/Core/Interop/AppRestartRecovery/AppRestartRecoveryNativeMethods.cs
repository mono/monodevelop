//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    internal static class AppRestartRecoveryNativeMethods
    {
        #region Application Restart and Recovery Definitions

        internal delegate UInt32 InternalRecoveryCallback(IntPtr state);

        private static InternalRecoveryCallback internalCallback = new InternalRecoveryCallback(InternalRecoveryHandler);
        internal static InternalRecoveryCallback InternalCallback { get { return internalCallback; } }

        private static UInt32 InternalRecoveryHandler(IntPtr parameter)
        {
            bool cancelled = false;
            ApplicationRecoveryInProgress(out cancelled);

            GCHandle handle = GCHandle.FromIntPtr(parameter);
            RecoveryData data = handle.Target as RecoveryData;
            data.Invoke();
            handle.Free();

            return (0);
        }



        [DllImport("kernel32.dll")]
        internal static extern void ApplicationRecoveryFinished(
           [MarshalAs(UnmanagedType.Bool)] bool success);

        [DllImport("kernel32.dll")]
        [PreserveSig]
        internal static extern HResult ApplicationRecoveryInProgress(
            [Out, MarshalAs(UnmanagedType.Bool)] out bool canceled);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [PreserveSig]
        internal static extern HResult RegisterApplicationRecoveryCallback(
            InternalRecoveryCallback callback, IntPtr param,
            uint pingInterval,
            uint flags); // Unused.

        [DllImport("kernel32.dll")]
        [PreserveSig]
        internal static extern HResult RegisterApplicationRestart(
            [MarshalAs(UnmanagedType.BStr)] string commandLineArgs,
            RestartRestrictions flags);

        [DllImport("kernel32.dll")]
        [PreserveSig]
        internal static extern HResult UnregisterApplicationRecoveryCallback();

        [DllImport("kernel32.dll")]
        [PreserveSig]
        internal static extern HResult UnregisterApplicationRestart();

        #endregion
    }
}
