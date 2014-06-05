// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Event args for various Tabbed Thumbnail related events
    /// </summary>
    public class TabbedThumbnailEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a Event Args for a specific tabbed thumbnail event.
        /// </summary>
        /// <param name="windowHandle">Window handle for the control/window related to the event</param>        
        public TabbedThumbnailEventArgs(IntPtr windowHandle)
        {
            WindowHandle = windowHandle;
            WindowsControl = null;
        }

        /// <summary>
        /// Creates a Event Args for a specific tabbed thumbnail event.
        /// </summary>
        /// <param name="windowsControl">WPF Control (UIElement) related to the event</param>        
        public TabbedThumbnailEventArgs(UIElement windowsControl)
        {
            WindowHandle = IntPtr.Zero;
            WindowsControl = windowsControl;
        }

        /// <summary>
        /// Gets the Window handle for the specific control/window that is related to this event.
        /// </summary>
        /// <remarks>For WPF Controls (UIElement) the WindowHandle will be IntPtr.Zero. 
        /// Check the WindowsControl property to get the specific control associated with this event.</remarks>
        public IntPtr WindowHandle { get; private set; }

        /// <summary>
        /// Gets the WPF Control (UIElement) that is related to this event. This property may be null
        /// for non-WPF applications.
        /// </summary>
        public UIElement WindowsControl { get; private set; }
    }
}
