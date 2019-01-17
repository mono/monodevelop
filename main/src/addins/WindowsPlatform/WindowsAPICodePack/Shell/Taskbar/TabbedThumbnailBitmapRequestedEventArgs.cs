// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Event args for the TabbedThumbnailBitmapRequested event. The event allows applications to
    /// provide a bitmap for the tabbed thumbnail's preview and peek. The application should also
    /// set the Handled property if a custom bitmap is provided.
    /// </summary>
    public class TabbedThumbnailBitmapRequestedEventArgs : TabbedThumbnailEventArgs
    {
        /// <summary>
        /// Creates a Event Args for a TabbedThumbnailBitmapRequested event.
        /// </summary>
        /// <param name="windowHandle">Window handle for the control/window related to the event</param>
        public TabbedThumbnailBitmapRequestedEventArgs(IntPtr windowHandle)
            : base(windowHandle)
        {
        }

        /// <summary>
        /// Creates a Event Args for a TabbedThumbnailBitmapRequested event.
        /// </summary>
        /// <param name="windowsControl">WPF Control (UIElement) related to the event</param>
        public TabbedThumbnailBitmapRequestedEventArgs(UIElement windowsControl)
            : base(windowsControl)
        {
        }


        /// <summary>
        /// Gets or sets a value indicating whether the TabbedThumbnailBitmapRequested event was handled.
        /// Set this property if the SetImage method is called with a custom bitmap for the thumbnail/peek.
        /// </summary>
        public bool Handled { get; set; }
    }
}
