// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using System.Windows.Forms;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    internal sealed class TabbedThumbnailProxyWindow : Form, IDisposable
    {
        
        internal TabbedThumbnailProxyWindow(TabbedThumbnail preview)
        {
            TabbedThumbnail = preview;
            Size = new System.Drawing.Size(1, 1);

            if (!string.IsNullOrEmpty(preview.Title))
            {
                Text = preview.Title;
            }

            if (preview.WindowsControl != null)
            {                
                WindowsControl = preview.WindowsControl;            
            }
        }

        internal TabbedThumbnail TabbedThumbnail { get; private set; }

        internal UIElement WindowsControl { get; private set; }

        internal IntPtr WindowToTellTaskbarAbout { get { return this.Handle; } }

        protected override void WndProc(ref Message m)
        {
            bool handled = false;

            if (this.TabbedThumbnail != null)
            {
                handled = TaskbarWindowManager.DispatchMessage(ref m, this.TabbedThumbnail.TaskbarWindow);
            }

            // If it's a WM_Destroy message, then also forward it to the base class (our native window)
            if ((m.Msg == (int)WindowMessage.Destroy) ||
               (m.Msg == (int)WindowMessage.NCDestroy) ||
               ((m.Msg == (int)WindowMessage.SystemCommand) && (((int)m.WParam) == TabbedThumbnailNativeMethods.ScClose)))
            {
                base.WndProc(ref m);
            }
            else if (!handled) { base.WndProc(ref m); }
        }

        #region IDisposable Members

        /// <summary>
        /// 
        /// </summary>
        ~TabbedThumbnailProxyWindow()
        {
            Dispose(false);
        }

        /// <summary>
        /// Release the native objects.
        /// </summary>
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (TabbedThumbnail != null) { TabbedThumbnail.Dispose(); }

                TabbedThumbnail = null;
                
                WindowsControl = null;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}