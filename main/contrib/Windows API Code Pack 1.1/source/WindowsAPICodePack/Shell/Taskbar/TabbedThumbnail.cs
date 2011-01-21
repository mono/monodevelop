//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.Resources;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Represents a tabbed thumbnail on the taskbar for a given window or a control.
    /// </summary>
    public class TabbedThumbnail : IDisposable
    {
        #region Internal members

        // Control properties
        internal IntPtr WindowHandle { get; set; }
        internal IntPtr ParentWindowHandle { get; set; }

        // WPF properties
        internal UIElement WindowsControl { get; set; }
        internal Window WindowsControlParentWindow { get; set; }

        private TaskbarWindow _taskbarWindow;
        internal TaskbarWindow TaskbarWindow
        {
            get { return _taskbarWindow; }
            set
            {
                _taskbarWindow = value;

                // If we have a TaskbarWindow assigned, set it's icon
                if (_taskbarWindow != null && _taskbarWindow.TabbedThumbnailProxyWindow != null)
                {
                    _taskbarWindow.TabbedThumbnailProxyWindow.Icon = Icon;
                }
            }
        }

        private bool _addedToTaskbar;
        internal bool AddedToTaskbar
        {
            get { return _addedToTaskbar; }
            set
            {
                _addedToTaskbar = value;

                // The user has updated the clipping region, so invalidate our existing preview
                if (ClippingRectangle != null)
                {
                    TaskbarWindowManager.InvalidatePreview(this.TaskbarWindow);
                }
            }
        }

        internal bool RemovedFromTaskbar { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new TabbedThumbnail with the given window handle of the parent and
        /// a child control/window's handle (e.g. TabPage or Panel)
        /// </summary>
        /// <param name="parentWindowHandle">Window handle of the parent window. 
        /// This window has to be a top-level window and the handle cannot be null or IntPtr.Zero</param>
        /// <param name="windowHandle">Window handle of the child control or window for which a tabbed 
        /// thumbnail needs to be displayed</param>
        public TabbedThumbnail(IntPtr parentWindowHandle, IntPtr windowHandle)
        {
            if (parentWindowHandle == IntPtr.Zero)
            {
                throw new ArgumentException(LocalizedMessages.TabbedThumbnailZeroParentHandle, "parentWindowHandle");
            }
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException(LocalizedMessages.TabbedThumbnailZeroChildHandle, "windowHandle");
            }

            WindowHandle = windowHandle;
            ParentWindowHandle = parentWindowHandle;
        }

        /// <summary>
        /// Creates a new TabbedThumbnail with the given window handle of the parent and
        /// a child control (e.g. TabPage or Panel)
        /// </summary>
        /// <param name="parentWindowHandle">Window handle of the parent window. 
        /// This window has to be a top-level window and the handle cannot be null or IntPtr.Zero</param>
        /// <param name="control">Child control for which a tabbed thumbnail needs to be displayed</param>
        /// <remarks>This method can also be called when using a WindowsFormHost control in a WPF application.
        ///  Call this method with the main WPF Window's handle, and windowsFormHost.Child control.</remarks>
        public TabbedThumbnail(IntPtr parentWindowHandle, Control control)
        {
            if (parentWindowHandle == IntPtr.Zero)
            {
                throw new ArgumentException(LocalizedMessages.TabbedThumbnailZeroParentHandle, "parentWindowHandle");
            }
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            WindowHandle = control.Handle;
            ParentWindowHandle = parentWindowHandle;
        }

        /// <summary>
        /// Creates a new TabbedThumbnail with the given window handle of the parent and
        /// a WPF child Window. For WindowsFormHost control, use TabbedThumbnail(IntPtr, Control) overload and pass
        /// the WindowsFormHost.Child as the second parameter.
        /// </summary>
        /// <param name="parentWindow">Parent window for the UIElement control. 
        /// This window has to be a top-level window and the handle cannot be null</param>
        /// <param name="windowsControl">WPF Control (UIElement) for which a tabbed thumbnail needs to be displayed</param>
        /// <param name="peekOffset">Offset point used for displaying the peek bitmap. This setting is
        /// recomended for hidden WPF controls as it is difficult to calculate their offset.</param>
        public TabbedThumbnail(Window parentWindow, UIElement windowsControl, Vector peekOffset)
        {
            if (windowsControl == null)
            {
                throw new ArgumentNullException("windowsControl");
            }
            if (parentWindow == null)
            {
                throw new ArgumentNullException("parentWindow");
            }

            WindowHandle = IntPtr.Zero;

            WindowsControl = windowsControl;
            WindowsControlParentWindow = parentWindow;
            ParentWindowHandle = (new WindowInteropHelper(parentWindow)).Handle;
            PeekOffset = peekOffset;
        }

        #endregion

        #region Public Properties

        private string _title = string.Empty;
        /// <summary>
        /// Title for the window shown as the taskbar thumbnail.
        /// </summary>
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    if (TitleChanged != null) { TitleChanged(this, EventArgs.Empty); }
                }
            }
        }

        private string _tooltip = string.Empty;
        /// <summary>
        /// Tooltip to be shown for this thumbnail on the taskbar. 
        /// By default this is full title of the window shown on the taskbar.
        /// </summary>
        public string Tooltip
        {
            get { return _tooltip; }
            set
            {
                if (_tooltip != value)
                {
                    _tooltip = value;
                    if (TooltipChanged != null) { TooltipChanged(this, EventArgs.Empty); }
                }
            }
        }

        /// <summary>
        /// Sets the window icon for this thumbnail preview
        /// </summary>
        /// <param name="icon">System.Drawing.Icon for the window/control associated with this preview</param>
        public void SetWindowIcon(Icon icon)
        {
            Icon = icon;

            // If we have a TaskbarWindow assigned, set its icon
            if (TaskbarWindow != null && TaskbarWindow.TabbedThumbnailProxyWindow != null)
            {
                TaskbarWindow.TabbedThumbnailProxyWindow.Icon = Icon;
            }
        }

        /// <summary>
        /// Sets the window icon for this thumbnail preview
        /// </summary>
        /// <param name="iconHandle">Icon handle (hIcon) for the window/control associated with this preview</param>
        /// <remarks>This method will not release the icon handle. It is the caller's responsibility to release the icon handle.</remarks>
        public void SetWindowIcon(IntPtr iconHandle)
        {
            Icon = iconHandle != IntPtr.Zero ? System.Drawing.Icon.FromHandle(iconHandle) : null;

            if (TaskbarWindow != null && TaskbarWindow.TabbedThumbnailProxyWindow != null)
            {
                TaskbarWindow.TabbedThumbnailProxyWindow.Icon = Icon;
            }
        }

        private Rectangle? _clippingRectangle;
        /// <summary>
        /// Specifies that only a portion of the window's client area
        /// should be used in the window's thumbnail.
        /// <para>A value of null will clear the clipping area and use the default thumbnail.</para>
        /// </summary>
        public Rectangle? ClippingRectangle
        {
            get { return _clippingRectangle; }
            set
            {
                _clippingRectangle = value;

                // The user has updated the clipping region, so invalidate our existing preview
                TaskbarWindowManager.InvalidatePreview(this.TaskbarWindow);
            }
        }

        internal IntPtr CurrentHBitmap { get; set; }

        internal Icon Icon { get; private set; }

        /// <summary>
        /// Override the thumbnail and peek bitmap. 
        /// By providing this bitmap manually, Thumbnail Window manager will provide the 
        /// Desktop Window Manager (DWM) this bitmap instead of rendering one automatically.
        /// Use this property to update the bitmap whenever the control is updated and the user
        /// needs to be shown a new thumbnail on the taskbar preview (or aero peek).
        /// </summary>
        /// <param name="bitmap">The image to use.</param>
        /// <remarks>
        /// If the bitmap doesn't have the right dimensions, the DWM may scale it or not 
        /// render certain areas as appropriate - it is the user's responsibility
        /// to render a bitmap with the proper dimensions.
        /// </remarks>
        public void SetImage(Bitmap bitmap)
        {
            if (bitmap != null)
            {
                SetImage(bitmap.GetHbitmap());
            }
            else
            {
                SetImage(IntPtr.Zero);
            }
        }

        /// <summary>
        /// Override the thumbnail and peek bitmap. 
        /// By providing this bitmap manually, Thumbnail Window manager will provide the 
        /// Desktop Window Manager (DWM) this bitmap instead of rendering one automatically.
        /// Use this property to update the bitmap whenever the control is updated and the user
        /// needs to be shown a new thumbnail on the taskbar preview (or aero peek).
        /// </summary>
        /// <param name="bitmapSource">The image to use.</param>
        /// <remarks>
        /// If the bitmap doesn't have the right dimensions, the DWM may scale it or not 
        /// render certain areas as appropriate - it is the user's responsibility
        /// to render a bitmap with the proper dimensions.
        /// </remarks>
        public void SetImage(BitmapSource bitmapSource)
        {
            if (bitmapSource == null)
            {
                SetImage(IntPtr.Zero);
                return;
            }

            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using (MemoryStream memoryStream = new MemoryStream())
            {
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                using (Bitmap bmp = new Bitmap(memoryStream))
                {
                    SetImage(bmp.GetHbitmap());
                }
            }
        }

        /// <summary>
        /// Override the thumbnail and peek bitmap. 
        /// By providing this bitmap manually, Thumbnail Window manager will provide the 
        /// Desktop Window Manager (DWM) this bitmap instead of rendering one automatically.
        /// Use this property to update the bitmap whenever the control is updated and the user
        /// needs to be shown a new thumbnail on the taskbar preview (or aero peek).
        /// </summary>
        /// <param name="hBitmap">A bitmap handle for the image to use.
        /// <para>When the TabbedThumbnail is finalized, this class will delete the provided hBitmap.</para></param>
        /// <remarks>
        /// If the bitmap doesn't have the right dimensions, the DWM may scale it or not 
        /// render certain areas as appropriate - it is the user's responsibility
        /// to render a bitmap with the proper dimensions.
        /// </remarks>
        internal void SetImage(IntPtr hBitmap)
        {
            // Before we set a new bitmap, dispose the old one
            if (CurrentHBitmap != IntPtr.Zero)
            {
                ShellNativeMethods.DeleteObject(CurrentHBitmap);
            }

            // Set the new bitmap
            CurrentHBitmap = hBitmap;

            // Let DWM know to invalidate its cached thumbnail/preview and ask us for a new one            
            TaskbarWindowManager.InvalidatePreview(TaskbarWindow);
        }

        /// <summary>
        /// Specifies whether a standard window frame will be displayed
        /// around the bitmap.  If the bitmap represents a top-level window,
        /// you would probably set this flag to <b>true</b>.  If the bitmap
        /// represents a child window (or a frameless window), you would
        /// probably set this flag to <b>false</b>.
        /// </summary>
        public bool DisplayFrameAroundBitmap { get; set; }

        /// <summary>
        /// Invalidate any existing thumbnail preview. Calling this method
        /// will force DWM to request a new bitmap next time user previews the thumbnails
        /// or requests Aero peek preview.
        /// </summary>
        public void InvalidatePreview()
        {
            // clear current image and invalidate
            SetImage(IntPtr.Zero);
        }

        /// <summary>
        /// Gets or sets the offset used for displaying the peek bitmap. This setting is
        /// recomended for hidden WPF controls as it is difficult to calculate their offset.
        /// </summary>
        public Vector? PeekOffset { get; set; }

        #endregion



        #region Events

        /// <summary>
        /// This event is raised when the Title property changes.
        /// </summary>
        public event EventHandler TitleChanged;

        /// <summary>
        /// This event is raised when the Tooltip property changes.
        /// </summary>
        public event EventHandler TooltipChanged;

        /// <summary>
        /// The event that occurs when a tab is closed on the taskbar thumbnail preview.
        /// </summary>
        public event EventHandler<TabbedThumbnailClosedEventArgs> TabbedThumbnailClosed;

        /// <summary>
        /// The event that occurs when a tab is maximized via the taskbar thumbnail preview (context menu).
        /// </summary>
        public event EventHandler<TabbedThumbnailEventArgs> TabbedThumbnailMaximized;

        /// <summary>
        /// The event that occurs when a tab is minimized via the taskbar thumbnail preview (context menu).
        /// </summary>
        public event EventHandler<TabbedThumbnailEventArgs> TabbedThumbnailMinimized;

        /// <summary>
        /// The event that occurs when a tab is activated (clicked) on the taskbar thumbnail preview.
        /// </summary>
        public event EventHandler<TabbedThumbnailEventArgs> TabbedThumbnailActivated;

        /// <summary>
        /// The event that occurs when a thumbnail or peek bitmap is requested by the user.
        /// </summary>
        public event EventHandler<TabbedThumbnailBitmapRequestedEventArgs> TabbedThumbnailBitmapRequested;


        internal void OnTabbedThumbnailMaximized()
        {
            if (TabbedThumbnailMaximized != null)
            {
                TabbedThumbnailMaximized(this, GetTabbedThumbnailEventArgs());
            }
            else
            {
                // No one is listening to these events.
                // Forward the message to the main window
                CoreNativeMethods.SendMessage(ParentWindowHandle, WindowMessage.SystemCommand, new IntPtr(TabbedThumbnailNativeMethods.ScMaximize), IntPtr.Zero);
            }
        }

        internal void OnTabbedThumbnailMinimized()
        {
            if (TabbedThumbnailMinimized != null)
            {
                TabbedThumbnailMinimized(this, GetTabbedThumbnailEventArgs());
            }
            else
            {
                // No one is listening to these events.
                // Forward the message to the main window
                CoreNativeMethods.SendMessage(ParentWindowHandle, WindowMessage.SystemCommand, new IntPtr(TabbedThumbnailNativeMethods.ScMinimize), IntPtr.Zero);
            }

        }

        /// <summary>
        /// Returns true if the thumbnail was removed from the taskbar; false if it was not.
        /// </summary>
        /// <returns>Returns true if the thumbnail was removed from the taskbar; false if it was not.</returns>
        internal bool OnTabbedThumbnailClosed()
        {
            var closedHandler = TabbedThumbnailClosed;
            if (closedHandler != null)
            {
                var closingEvent = GetTabbedThumbnailClosingEventArgs();

                closedHandler(this, closingEvent);

                if (closingEvent.Cancel) { return false; }                
            }
            else
            {
                // No one is listening to these events. Forward the message to the main window
                CoreNativeMethods.SendMessage(ParentWindowHandle, WindowMessage.NCDestroy, IntPtr.Zero, IntPtr.Zero);
            }

            // Remove it from the internal list as well as the taskbar
            TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(this);
            return true;
        }

        internal void OnTabbedThumbnailActivated()
        {
            if (TabbedThumbnailActivated != null)
            {
                TabbedThumbnailActivated(this, GetTabbedThumbnailEventArgs());
            }
            else
            {
                // No one is listening to these events.
                // Forward the message to the main window
                CoreNativeMethods.SendMessage(ParentWindowHandle, WindowMessage.ActivateApplication, new IntPtr(1), new IntPtr(Thread.CurrentThread.GetHashCode()));
            }
        }

        internal void OnTabbedThumbnailBitmapRequested()
        {
            if (TabbedThumbnailBitmapRequested != null)
            {
                TabbedThumbnailBitmapRequestedEventArgs eventArgs = null;

                if (this.WindowHandle != IntPtr.Zero)
                {
                    eventArgs = new TabbedThumbnailBitmapRequestedEventArgs(this.WindowHandle);
                }
                else if (this.WindowsControl != null)
                {
                    eventArgs = new TabbedThumbnailBitmapRequestedEventArgs(this.WindowsControl);
                }

                TabbedThumbnailBitmapRequested(this, eventArgs);
            }
        }

        private TabbedThumbnailClosedEventArgs GetTabbedThumbnailClosingEventArgs()
        {
            TabbedThumbnailClosedEventArgs eventArgs = null;

            if (this.WindowHandle != IntPtr.Zero)
            {
                eventArgs = new TabbedThumbnailClosedEventArgs(this.WindowHandle);
            }
            else if (this.WindowsControl != null)
            {
                eventArgs = new TabbedThumbnailClosedEventArgs(this.WindowsControl);
            }

            return eventArgs;
        }

        private TabbedThumbnailEventArgs GetTabbedThumbnailEventArgs()
        {
            TabbedThumbnailEventArgs eventArgs = null;

            if (this.WindowHandle != IntPtr.Zero)
            {
                eventArgs = new TabbedThumbnailEventArgs(this.WindowHandle);
            }
            else if (this.WindowsControl != null)
            {
                eventArgs = new TabbedThumbnailEventArgs(this.WindowsControl);
            }

            return eventArgs;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// 
        /// </summary>
        ~TabbedThumbnail()
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

        /// <summary>
        /// Release the native objects.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _taskbarWindow = null;

                if (Icon != null) { Icon.Dispose(); }
                Icon = null;

                _title = null;
                _tooltip = null;
                WindowsControl = null;
            }

            if (CurrentHBitmap != IntPtr.Zero)
            {
                ShellNativeMethods.DeleteObject(CurrentHBitmap);
                CurrentHBitmap = IntPtr.Zero;
            }
        }

        #endregion
    }
}
