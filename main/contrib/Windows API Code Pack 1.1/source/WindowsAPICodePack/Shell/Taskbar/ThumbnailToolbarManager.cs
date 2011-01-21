//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Shell.Resources;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Thumbnail toolbar manager class for adding a thumbnail toolbar with a specified set of buttons 
    /// to the thumbnail image of a window in a taskbar button flyout.
    /// </summary>
    public class ThumbnailToolBarManager
    {
        internal ThumbnailToolBarManager()
        {
            // Hide the public constructor so users can't create an instance of this class.
        }

        /// <summary>
        /// Adds thumbnail toolbar for the specified window.
        /// </summary>
        /// <param name="windowHandle">Window handle for which the thumbnail toolbar buttons need to be added</param>
        /// <param name="buttons">Thumbnail buttons for the window's thumbnail toolbar</param>
        /// <exception cref="System.ArgumentException">If the number of buttons exceed the maximum allowed capacity (7).</exception>
        /// <exception cref="System.ArgumentException">If the Window Handle passed in invalid</exception>
        /// <remarks>After a toolbar has been added to a thumbnail, buttons can be altered only through various 
        /// properties on the <see cref="T:Microsoft.WindowsAPICodePack.Taskbar.ThumbnailToolBarButton"/>. While individual buttons cannot be added or removed, 
        /// they can be shown and hidden through <see cref="P:Microsoft.WindowsAPICodePack.Taskbar.ThumbnailToolBarButton.Visible"/> as needed. 
        /// The toolbar itself cannot be removed without re-creating the window itself.
        /// </remarks>
        public void AddButtons(IntPtr windowHandle, params ThumbnailToolBarButton[] buttons)
        {
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailManagerInvalidHandle, "windowHandle");
            }
            VerifyButtons(buttons);

            // Add the buttons to our window manager, which will also create a proxy window
            TaskbarWindowManager.AddThumbnailButtons(windowHandle, buttons);
        }

        /// <summary>
        /// Adds thumbnail toolbar for the specified WPF Control.
        /// </summary>
        /// <param name="control">WPF Control for which the thumbnail toolbar buttons need to be added</param>
        /// <param name="buttons">Thumbnail buttons for the window's thumbnail toolbar</param>
        /// <exception cref="System.ArgumentException">If the number of buttons exceed the maximum allowed capacity (7).</exception>
        /// <exception cref="System.ArgumentNullException">If the control passed in null</exception>
        /// <remarks>After a toolbar has been added to a thumbnail, buttons can be altered only through various 
        /// properties on the ThumbnailToolBarButton. While individual buttons cannot be added or removed, 
        /// they can be shown and hidden through ThumbnailToolBarButton.Visible as needed. 
        /// The toolbar itself cannot be removed without re-creating the window itself.
        /// </remarks>
        public void AddButtons(UIElement control, params ThumbnailToolBarButton[] buttons)
        {
            if (control == null) { throw new ArgumentNullException("control"); }
            VerifyButtons(buttons);

            // Add the buttons to our window manager, which will also create a proxy window
            TaskbarWindowManager.AddThumbnailButtons(control, buttons);
        }

        private static void VerifyButtons(params ThumbnailToolBarButton[] buttons)
        {
            if (buttons != null && buttons.Length == 0)
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailToolbarManagerNullEmptyArray, "buttons");
            }
            if (buttons.Length > 7)
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailToolbarManagerMaxButtons, "buttons");
            }
        }
    }
}
