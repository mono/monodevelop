using System;
using System.Windows;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Event args for when close is selected on a tabbed thumbnail proxy window.
    /// </summary>
    public class TabbedThumbnailClosedEventArgs : TabbedThumbnailEventArgs
    {
        /// <summary>
        /// Creates a Event Args for a specific tabbed thumbnail event.
        /// </summary>
        /// <param name="windowHandle">Window handle for the control/window related to the event</param>        
        public TabbedThumbnailClosedEventArgs(IntPtr windowHandle) : base(windowHandle) { }

        /// <summary>
        /// Creates a Event Args for a specific tabbed thumbnail event.
        /// </summary>
        /// <param name="windowsControl">WPF Control (UIElement) related to the event</param>        
        public TabbedThumbnailClosedEventArgs(UIElement windowsControl) : base(windowsControl) { }

        /// <summary>
        /// If set to true, the proxy window will not be removed from the taskbar.
        /// </summary>
        public bool Cancel { get; set; }

    }
}
