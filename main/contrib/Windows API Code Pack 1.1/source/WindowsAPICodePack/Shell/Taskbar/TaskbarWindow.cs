// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Shell.Resources;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    internal class TaskbarWindow : IDisposable
    {
        internal TabbedThumbnailProxyWindow TabbedThumbnailProxyWindow { get; set; }

        internal ThumbnailToolbarProxyWindow ThumbnailToolbarProxyWindow { get; set; }

        internal bool EnableTabbedThumbnails { get; set; }

        internal bool EnableThumbnailToolbars { get; set; }

        internal IntPtr UserWindowHandle { get; set; }

        internal UIElement WindowsControl { get; set; }

        private TabbedThumbnail _tabbedThumbnailPreview;
        internal TabbedThumbnail TabbedThumbnail
        {
            get { return _tabbedThumbnailPreview; }
            set
            {
                if (_tabbedThumbnailPreview != null)
                {
                    throw new InvalidOperationException(LocalizedMessages.TaskbarWindowValueSet);
                }

                TabbedThumbnailProxyWindow = new TabbedThumbnailProxyWindow(value);
                _tabbedThumbnailPreview = value;
                _tabbedThumbnailPreview.TaskbarWindow = this;
            }
        }

        private ThumbnailToolBarButton[] _thumbnailButtons;
        internal ThumbnailToolBarButton[] ThumbnailButtons
        {
            get { return _thumbnailButtons; }
            set
            {
                _thumbnailButtons = value;
                UpdateHandles();
            }
        }

        private void UpdateHandles()
        {
            foreach (ThumbnailToolBarButton button in _thumbnailButtons)
            {
                button.WindowHandle = WindowToTellTaskbarAbout;
                button.AddedToTaskbar = false;
            }
        }


        // TODO: Verify the logic of this property. There are situations where this will throw InvalidOperationException when it shouldn't.
        internal IntPtr WindowToTellTaskbarAbout
        {
            get
            {
                if (EnableThumbnailToolbars && !EnableTabbedThumbnails && ThumbnailToolbarProxyWindow != null)
                {
                    return ThumbnailToolbarProxyWindow.WindowToTellTaskbarAbout;
                }
                else if (!EnableThumbnailToolbars && EnableTabbedThumbnails && TabbedThumbnailProxyWindow != null)
                {
                    return TabbedThumbnailProxyWindow.WindowToTellTaskbarAbout;
                }
                // Bug: What should happen when TabedThumbnailProxyWindow IS null, but it is enabled?
                // This occurs during the TabbedThumbnailProxyWindow constructor at line 31.   
                else if (EnableTabbedThumbnails && EnableThumbnailToolbars && TabbedThumbnailProxyWindow != null)
                {
                    return TabbedThumbnailProxyWindow.WindowToTellTaskbarAbout;
                }

                throw new InvalidOperationException();
            }
        }

        internal void SetTitle(string title)
        {
            if (TabbedThumbnailProxyWindow == null)
            {
                throw new InvalidOperationException(LocalizedMessages.TasbarWindowProxyWindowSet);                
            }
            TabbedThumbnailProxyWindow.Text = title;
        }

        internal TaskbarWindow(IntPtr userWindowHandle, params ThumbnailToolBarButton[] buttons)
        {
            if (userWindowHandle == IntPtr.Zero)
            {
                throw new ArgumentException(LocalizedMessages.CommonFileDialogInvalidHandle, "userWindowHandle");
            }

            if (buttons == null || buttons.Length == 0)
            {
                throw new ArgumentException(LocalizedMessages.TaskbarWindowEmptyButtonArray, "buttons");
            }

            // Create our proxy window
            ThumbnailToolbarProxyWindow = new ThumbnailToolbarProxyWindow(userWindowHandle, buttons);
            ThumbnailToolbarProxyWindow.TaskbarWindow = this;

            // Set our current state
            EnableThumbnailToolbars = true;
            EnableTabbedThumbnails = false;

            //
            this.ThumbnailButtons = buttons;
            UserWindowHandle = userWindowHandle;
            WindowsControl = null;
        }

        internal TaskbarWindow(System.Windows.UIElement windowsControl, params ThumbnailToolBarButton[] buttons)
        {
            if (windowsControl == null)
            {
                throw new ArgumentNullException("windowsControl");
            }

            if (buttons == null || buttons.Length == 0)
            {
                throw new ArgumentException(LocalizedMessages.TaskbarWindowEmptyButtonArray, "buttons");
            }

            // Create our proxy window
            ThumbnailToolbarProxyWindow = new ThumbnailToolbarProxyWindow(windowsControl, buttons);
            ThumbnailToolbarProxyWindow.TaskbarWindow = this;

            // Set our current state
            EnableThumbnailToolbars = true;
            EnableTabbedThumbnails = false;

            this.ThumbnailButtons = buttons;
            UserWindowHandle = IntPtr.Zero;
            WindowsControl = windowsControl;
        }

        internal TaskbarWindow(TabbedThumbnail preview)
        {
            if (preview == null) { throw new ArgumentNullException("preview"); }

            // Create our proxy window
            // Bug: This is only called in this constructor.  Which will cause the property 
            // to fail if TaskbarWindow is initialized from a different constructor.
            TabbedThumbnailProxyWindow = new TabbedThumbnailProxyWindow(preview);

            // set our current state
            EnableThumbnailToolbars = false;
            EnableTabbedThumbnails = true;

            // copy values
            UserWindowHandle = preview.WindowHandle;
            WindowsControl = preview.WindowsControl;
            TabbedThumbnail = preview;
        }

        #region IDisposable Members

        /// <summary>
        /// 
        /// </summary>
        ~TaskbarWindow()
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
                if (_tabbedThumbnailPreview != null)
                {
                    _tabbedThumbnailPreview.Dispose();
                }
                _tabbedThumbnailPreview = null;

                if (ThumbnailToolbarProxyWindow != null)
                {
                    ThumbnailToolbarProxyWindow.Dispose();
                }
                ThumbnailToolbarProxyWindow = null;

                if (TabbedThumbnailProxyWindow != null)
                {
                    TabbedThumbnailProxyWindow.Dispose();
                }
                TabbedThumbnailProxyWindow = null;

                // Don't dispose the thumbnail buttons as they might be used in another window.
                // Setting them to null will indicate we don't need use anymore.
                _thumbnailButtons = null;
            }
        }

        #endregion
    }
}
