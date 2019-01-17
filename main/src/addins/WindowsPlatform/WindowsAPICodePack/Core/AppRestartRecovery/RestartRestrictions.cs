//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// Specifies the conditions when Windows Error Reporting
    /// should not restart an application that has registered
    /// for automatic restart.
    /// </summary>
    [Flags]
    public enum RestartRestrictions
    {
        /// <summary>
        /// Always restart the application.
        /// </summary>
        None = 0,
        /// <summary>
        /// Do not restart when the application has crashed.
        /// </summary>
        NotOnCrash = 1,
        /// <summary>
        /// Do not restart when the application is hung.
        /// </summary>
        NotOnHang = 2,
        /// <summary>
        /// Do not restart when the application is terminated
        /// due to a system update.
        /// </summary>
        NotOnPatch = 4,
        /// <summary>
        /// Do not restart when the application is terminated 
        /// because of a system reboot.
        /// </summary>
        NotOnReboot = 8
    }
}
