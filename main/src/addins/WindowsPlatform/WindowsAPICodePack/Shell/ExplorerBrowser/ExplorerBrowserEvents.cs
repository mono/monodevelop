//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.Controls
{
   
    /// <summary>
    /// Event argument for The NavigationPending event
    /// </summary>
    public class NavigationPendingEventArgs : EventArgs
    {
        /// <summary>
        /// The location being navigated to
        /// </summary>
        public ShellObject PendingLocation { get; set; }

        /// <summary>
        /// Set to 'True' to cancel the navigation.
        /// </summary>
        public bool Cancel { get; set; }

    }

    /// <summary>
    /// Event argument for The NavigationComplete event
    /// </summary>
    public class NavigationCompleteEventArgs : EventArgs
    {
        /// <summary>
        /// The new location of the explorer browser
        /// </summary>
        public ShellObject NewLocation { get; set; }
    }

    /// <summary>
    /// Event argument for the NavigatinoFailed event
    /// </summary>
    public class NavigationFailedEventArgs : EventArgs
    {
        /// <summary>
        /// The location the the browser would have navigated to.
        /// </summary>
        public ShellObject FailedLocation { get; set; }
    }
}