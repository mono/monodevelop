//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell.Resources;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    internal class ThumbnailToolbarProxyWindow : NativeWindow, IDisposable
    {
        private ThumbnailToolBarButton[] _thumbnailButtons;
        private IntPtr _internalWindowHandle;

        internal System.Windows.UIElement WindowsControl { get; set; }

        internal IntPtr WindowToTellTaskbarAbout
        {
            get
            {
                return _internalWindowHandle != IntPtr.Zero ? _internalWindowHandle : this.Handle;
            }
        }

        internal TaskbarWindow TaskbarWindow { get; set; }

        internal ThumbnailToolbarProxyWindow(IntPtr windowHandle, ThumbnailToolBarButton[] buttons)
        {
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException(LocalizedMessages.CommonFileDialogInvalidHandle, "windowHandle");
            }
            if (buttons != null && buttons.Length == 0)
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailToolbarManagerNullEmptyArray, "buttons");
            }

            _internalWindowHandle = windowHandle;
            _thumbnailButtons = buttons;

            // Set the window handle on the buttons (for future updates)
            Array.ForEach(_thumbnailButtons, new Action<ThumbnailToolBarButton>(UpdateHandle));

            // Assign the window handle (coming from the user) to this native window
            // so we can intercept the window messages sent from the taskbar to this window.
            this.AssignHandle(windowHandle);
        }

        internal ThumbnailToolbarProxyWindow(System.Windows.UIElement windowsControl, ThumbnailToolBarButton[] buttons)
        {
            if (windowsControl == null) { throw new ArgumentNullException("windowsControl"); }
            if (buttons != null && buttons.Length == 0)
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailToolbarManagerNullEmptyArray, "buttons");
            }

            _internalWindowHandle = IntPtr.Zero;
            WindowsControl = windowsControl;
            _thumbnailButtons = buttons;

            // Set the window handle on the buttons (for future updates)
            Array.ForEach(_thumbnailButtons, new Action<ThumbnailToolBarButton>(UpdateHandle));
        }

        private void UpdateHandle(ThumbnailToolBarButton button)
        {
            button.WindowHandle = _internalWindowHandle;
            button.AddedToTaskbar = false;
        }

        protected override void WndProc(ref Message m)
        {
            bool handled = false;

            handled = TaskbarWindowManager.DispatchMessage(ref m, this.TaskbarWindow);

            // If it's a WM_Destroy message, then also forward it to the base class (our native window)
            if ((m.Msg == (int)WindowMessage.Destroy) ||
               (m.Msg == (int)WindowMessage.NCDestroy) ||
               ((m.Msg == (int)WindowMessage.SystemCommand) && (((int)m.WParam) == TabbedThumbnailNativeMethods.ScClose)))
            {
                base.WndProc(ref m);
            }
            else if (!handled)
            {
                base.WndProc(ref m);
            }
        }

        #region IDisposable Members

        /// <summary>
        /// 
        /// </summary>
        ~ThumbnailToolbarProxyWindow()
        {
            Dispose(false);
        }

        /// <summary>
        /// Release the native objects.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources

                // Don't dispose the thumbnail buttons
                // as they might be used in another window.
                // Setting them to null will indicate we don't need use anymore.
                _thumbnailButtons = null;
            }
        }

        #endregion

    }
}
