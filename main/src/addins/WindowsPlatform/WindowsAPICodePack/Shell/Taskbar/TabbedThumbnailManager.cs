//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using MS.WindowsAPICodePack.Internal;
using Microsoft.WindowsAPICodePack.Shell.Interop;
using Microsoft.WindowsAPICodePack.Shell.Resources;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    /// <summary>
    /// Represents the main class for adding and removing tabbed thumbnails on the Taskbar
    /// for child windows and controls.
    /// </summary>
    public class TabbedThumbnailManager
    {
        /// <summary>
        /// Internal dictionary to keep track of the user's window handle and its 
        /// corresponding thumbnail preview objects.
        /// </summary>
        private Dictionary<IntPtr, TabbedThumbnail> _tabbedThumbnailCache;
        private Dictionary<UIElement, TabbedThumbnail> _tabbedThumbnailCacheWPF; // list for WPF controls

        /// <summary>
        /// Internal constructor that creates a new dictionary for keeping track of the window handles
        /// and their corresponding thumbnail preview objects.
        /// </summary>
        internal TabbedThumbnailManager()
        {
            _tabbedThumbnailCache = new Dictionary<IntPtr, TabbedThumbnail>();
            _tabbedThumbnailCacheWPF = new Dictionary<UIElement, TabbedThumbnail>();
        }

        /// <summary>
        /// Adds a new tabbed thumbnail to the taskbar.
        /// </summary>
        /// <param name="preview">Thumbnail preview for a specific window handle or control. The preview
        /// object can be initialized with specific properties for the title, bitmap, and tooltip.</param>
        /// <exception cref="System.ArgumentException">If the tabbed thumbnail has already been added</exception>
        public void AddThumbnailPreview(TabbedThumbnail preview)
        {
            if (preview == null) { throw new ArgumentNullException("preview"); }

            // UI Element has a windowHandle of zero.
            if (preview.WindowHandle == IntPtr.Zero)
            {
                if (_tabbedThumbnailCacheWPF.ContainsKey(preview.WindowsControl))
                {
                    throw new ArgumentException(LocalizedMessages.ThumbnailManagerPreviewAdded, "preview");
                }
                _tabbedThumbnailCacheWPF.Add(preview.WindowsControl, preview);
            }
            else
            {
                // Regular control with a valid handle
                if (_tabbedThumbnailCache.ContainsKey(preview.WindowHandle))
                {
                    throw new ArgumentException(LocalizedMessages.ThumbnailManagerPreviewAdded, "preview");
                }
                _tabbedThumbnailCache.Add(preview.WindowHandle, preview);
            }

            TaskbarWindowManager.AddTabbedThumbnail(preview);

            preview.InvalidatePreview(); // Note: Why this here?
        }

        /// <summary>
        /// Gets the TabbedThumbnail object associated with the given window handle
        /// </summary>
        /// <param name="windowHandle">Window handle for the control/window</param>
        /// <returns>TabbedThumbnail associated with the given window handle</returns>
        public TabbedThumbnail GetThumbnailPreview(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailManagerInvalidHandle, "windowHandle");
            }

            TabbedThumbnail thumbnail;
            return _tabbedThumbnailCache.TryGetValue(windowHandle, out thumbnail) ? thumbnail : null;
        }

        /// <summary>
        /// Gets the TabbedThumbnail object associated with the given control
        /// </summary>
        /// <param name="control">Specific control for which the preview object is requested</param>
        /// <returns>TabbedThumbnail associated with the given control</returns>
        public TabbedThumbnail GetThumbnailPreview(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            return GetThumbnailPreview(control.Handle);
        }

        /// <summary>
        /// Gets the TabbedThumbnail object associated with the given WPF Window
        /// </summary>
        /// <param name="windowsControl">WPF Control (UIElement) for which the preview object is requested</param>
        /// <returns>TabbedThumbnail associated with the given WPF Window</returns>
        public TabbedThumbnail GetThumbnailPreview(UIElement windowsControl)
        {
            if (windowsControl == null)
            {
                throw new ArgumentNullException("windowsControl");
            }

            TabbedThumbnail thumbnail;
            return _tabbedThumbnailCacheWPF.TryGetValue(windowsControl, out thumbnail) ? thumbnail : null;
        }

        /// <summary>
        /// Remove the tabbed thumbnail from the taskbar.
        /// </summary>
        /// <param name="preview">TabbedThumbnail associated with the control/window that 
        /// is to be removed from the taskbar</param>
        public void RemoveThumbnailPreview(TabbedThumbnail preview)
        {
            if (preview == null)
            {
                throw new ArgumentNullException("preview");
            }

            if (_tabbedThumbnailCache.ContainsKey(preview.WindowHandle))
            {
                RemoveThumbnailPreview(preview.WindowHandle);
            }
            else if (_tabbedThumbnailCacheWPF.ContainsKey(preview.WindowsControl))
            {
                RemoveThumbnailPreview(preview.WindowsControl);
            }
        }

        /// <summary>
        /// Remove the tabbed thumbnail from the taskbar.
        /// </summary>
        /// <param name="windowHandle">TabbedThumbnail associated with the window handle that 
        /// is to be removed from the taskbar</param>
        public void RemoveThumbnailPreview(IntPtr windowHandle)
        {
            if (!_tabbedThumbnailCache.ContainsKey(windowHandle))
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailManagerControlNotAdded, "windowHandle");
            }

            TaskbarWindowManager.UnregisterTab(_tabbedThumbnailCache[windowHandle].TaskbarWindow);

            _tabbedThumbnailCache.Remove(windowHandle);

            TaskbarWindow taskbarWindow = TaskbarWindowManager.GetTaskbarWindow(windowHandle, TaskbarProxyWindowType.TabbedThumbnail);

            if (taskbarWindow != null)
            {
                if (TaskbarWindowManager._taskbarWindowList.Contains(taskbarWindow))
                    TaskbarWindowManager._taskbarWindowList.Remove(taskbarWindow);
                taskbarWindow.Dispose();
                taskbarWindow = null;
            }
        }

        /// <summary>
        /// Remove the tabbed thumbnail from the taskbar.
        /// </summary>
        /// <param name="control">TabbedThumbnail associated with the control that 
        /// is to be removed from the taskbar</param>
        public void RemoveThumbnailPreview(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            IntPtr handle = control.Handle;

            RemoveThumbnailPreview(handle);
        }

        /// <summary>
        /// Remove the tabbed thumbnail from the taskbar.
        /// </summary>
        /// <param name="windowsControl">TabbedThumbnail associated with the WPF Control (UIElement) that 
        /// is to be removed from the taskbar</param>
        public void RemoveThumbnailPreview(UIElement windowsControl)
        {
            if (windowsControl == null) { throw new ArgumentNullException("windowsControl"); }

            if (!_tabbedThumbnailCacheWPF.ContainsKey(windowsControl))
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailManagerControlNotAdded, "windowsControl");
            }

            TaskbarWindowManager.UnregisterTab(_tabbedThumbnailCacheWPF[windowsControl].TaskbarWindow);

            _tabbedThumbnailCacheWPF.Remove(windowsControl);

            TaskbarWindow taskbarWindow = TaskbarWindowManager.GetTaskbarWindow(windowsControl, TaskbarProxyWindowType.TabbedThumbnail);

            if (taskbarWindow != null)
            {
                if (TaskbarWindowManager._taskbarWindowList.Contains(taskbarWindow))
                {
                    TaskbarWindowManager._taskbarWindowList.Remove(taskbarWindow);
                }
                taskbarWindow.Dispose();
                taskbarWindow = null;
            }
        }

        /// <summary>
        /// Sets the given tabbed thumbnail preview object as being active on the taskbar tabbed thumbnails list.
        /// Call this method to keep the application and the taskbar in sync as to which window/control
        /// is currently active (or selected, in the case of tabbed application).
        /// </summary>
        /// <param name="preview">TabbedThumbnail for the specific control/indow that is currently active in the application</param>
        /// <exception cref="System.ArgumentException">If the control/window is not yet added to the tabbed thumbnails list</exception>
        public void SetActiveTab(TabbedThumbnail preview)
        {
            if (preview == null) { throw new ArgumentNullException("preview"); }

            if (preview.WindowHandle != IntPtr.Zero)
            {
                if (!_tabbedThumbnailCache.ContainsKey(preview.WindowHandle))
                {
                    throw new ArgumentException(LocalizedMessages.ThumbnailManagerPreviewNotAdded, "preview");
                }
                TaskbarWindowManager.SetActiveTab(_tabbedThumbnailCache[preview.WindowHandle].TaskbarWindow);
            }
            else if (preview.WindowsControl != null)
            {
                if (!_tabbedThumbnailCacheWPF.ContainsKey(preview.WindowsControl))
                {
                    throw new ArgumentException(LocalizedMessages.ThumbnailManagerPreviewNotAdded, "preview");
                }
                TaskbarWindowManager.SetActiveTab(_tabbedThumbnailCacheWPF[preview.WindowsControl].TaskbarWindow);
            }
        }

        /// <summary>
        /// Sets the given window handle as being active on the taskbar tabbed thumbnails list.
        /// Call this method to keep the application and the taskbar in sync as to which window/control
        /// is currently active (or selected, in the case of tabbed application).
        /// </summary>
        /// <param name="windowHandle">Window handle for the control/window that is currently active in the application</param>
        /// <exception cref="System.ArgumentException">If the control/window is not yet added to the tabbed thumbnails list</exception>
        public void SetActiveTab(IntPtr windowHandle)
        {
            if (!_tabbedThumbnailCache.ContainsKey(windowHandle))
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailManagerPreviewNotAdded, "windowHandle");
            }
            TaskbarWindowManager.SetActiveTab(_tabbedThumbnailCache[windowHandle].TaskbarWindow);
        }

        /// <summary>
        /// Sets the given Control/Form window as being active on the taskbar tabbed thumbnails list.
        /// Call this method to keep the application and the taskbar in sync as to which window/control
        /// is currently active (or selected, in the case of tabbed application).
        /// </summary>
        /// <param name="control">Control/Form that is currently active in the application</param>
        /// <exception cref="System.ArgumentException">If the control/window is not yet added to the tabbed thumbnails list</exception>
        public void SetActiveTab(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }
            SetActiveTab(control.Handle);
        }

        /// <summary>
        /// Sets the given WPF window as being active on the taskbar tabbed thumbnails list.
        /// Call this method to keep the application and the taskbar in sync as to which window/control
        /// is currently active (or selected, in the case of tabbed application).
        /// </summary>
        /// <param name="windowsControl">WPF control that is currently active in the application</param>
        /// <exception cref="System.ArgumentException">If the control/window is not yet added to the tabbed thumbnails list</exception>
        public void SetActiveTab(UIElement windowsControl)
        {
            if (windowsControl == null)
            {
                throw new ArgumentNullException("windowsControl");
            }

            if (!_tabbedThumbnailCacheWPF.ContainsKey(windowsControl))
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailManagerPreviewNotAdded, "windowsControl");
            }
            TaskbarWindowManager.SetActiveTab(_tabbedThumbnailCacheWPF[windowsControl].TaskbarWindow);

        }

        /// <summary>
        /// Determines whether the given preview has been added to the taskbar's tabbed thumbnail list.
        /// </summary>
        /// <param name="preview">The preview to locate on the taskbar's tabbed thumbnail list</param>
        /// <returns>true if the tab is already added on the taskbar; otherwise, false.</returns>
        public bool IsThumbnailPreviewAdded(TabbedThumbnail preview)
        {
            if (preview == null)
            {
                throw new ArgumentNullException("preview");
            }

            if (preview.WindowHandle != IntPtr.Zero && _tabbedThumbnailCache.ContainsKey(preview.WindowHandle))
            {
                return true;
            }
            else if (preview.WindowsControl != null && _tabbedThumbnailCacheWPF.ContainsKey(preview.WindowsControl))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given window has been added to the taskbar's tabbed thumbnail list.
        /// </summary>
        /// <param name="windowHandle">The window to locate on the taskbar's tabbed thumbnail list</param>
        /// <returns>true if the tab is already added on the taskbar; otherwise, false.</returns>
        public bool IsThumbnailPreviewAdded(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException(LocalizedMessages.ThumbnailManagerInvalidHandle, "windowHandle");
            }

            return _tabbedThumbnailCache.ContainsKey(windowHandle);            
        }

        /// <summary>
        /// Determines whether the given control has been added to the taskbar's tabbed thumbnail list.
        /// </summary>
        /// <param name="control">The preview to locate on the taskbar's tabbed thumbnail list</param>
        /// <returns>true if the tab is already added on the taskbar; otherwise, false.</returns>
        public bool IsThumbnailPreviewAdded(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            return _tabbedThumbnailCache.ContainsKey(control.Handle);
        }

        /// <summary>
        /// Determines whether the given control has been added to the taskbar's tabbed thumbnail list.
        /// </summary>
        /// <param name="control">The preview to locate on the taskbar's tabbed thumbnail list</param>
        /// <returns>true if the tab is already added on the taskbar; otherwise, false.</returns>
        public bool IsThumbnailPreviewAdded(UIElement control)
        {
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            return _tabbedThumbnailCacheWPF.ContainsKey(control);
        }

        /// <summary>
        /// Invalidates all the tabbed thumbnails. This will force the Desktop Window Manager
        /// to not use the cached thumbnail or preview or aero peek and request a new one next time.
        /// </summary>
        /// <remarks>This method should not be called frequently. 
        /// Doing so can lead to poor performance as new bitmaps are created and retrieved.</remarks>
        public void InvalidateThumbnails()
        {
            // Invalidate all the previews currently in our cache.
            // This will ensure we get updated bitmaps next time

            foreach (TabbedThumbnail thumbnail in _tabbedThumbnailCache.Values)
            {
                TaskbarWindowManager.InvalidatePreview(thumbnail.TaskbarWindow);
                thumbnail.SetImage(IntPtr.Zero); // TODO: Investigate this, and why it needs to be called.
            }

            foreach (TabbedThumbnail thumbnail in _tabbedThumbnailCacheWPF.Values)
            {
                TaskbarWindowManager.InvalidatePreview(thumbnail.TaskbarWindow);
                thumbnail.SetImage(IntPtr.Zero);
            }
        }

        /// <summary>
        /// Clear a clip that is already in place and return to the default display of the thumbnail.
        /// </summary>
        /// <param name="windowHandle">The handle to a window represented in the taskbar. This has to be a top-level window.</param>
        public static void ClearThumbnailClip(IntPtr windowHandle)
        {
            TaskbarList.Instance.SetThumbnailClip(windowHandle, IntPtr.Zero);
        }

        /// <summary>
        /// Selects a portion of a window's client area to display as that window's thumbnail in the taskbar.
        /// </summary>
        /// <param name="windowHandle">The handle to a window represented in the taskbar. This has to be a top-level window.</param>
        /// <param name="clippingRectangle">Rectangle structure that specifies a selection within the window's client area,
        /// relative to the upper-left corner of that client area.
        /// <para>If this parameter is null, the clipping area will be cleared and the default display of the thumbnail will be used instead.</para></param>
        public void SetThumbnailClip(IntPtr windowHandle, Rectangle? clippingRectangle)
        {
            if (clippingRectangle == null)
            {
                ClearThumbnailClip(windowHandle);
                return;
            }

            NativeRect rect = new NativeRect();
            rect.Left = clippingRectangle.Value.Left;
            rect.Top = clippingRectangle.Value.Top;
            rect.Right = clippingRectangle.Value.Right;
            rect.Bottom = clippingRectangle.Value.Bottom;

            IntPtr rectPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(rect));
            try
            {
                Marshal.StructureToPtr(rect, rectPtr, true);
                TaskbarList.Instance.SetThumbnailClip(windowHandle, rectPtr);
            }
            finally
            {
                Marshal.FreeCoTaskMem(rectPtr);
            }
        }

        /// <summary>
        /// Moves an existing thumbnail to a new position in the application's group.
        /// </summary>
        /// <param name="previewToChange">Preview for the window whose order is being changed. 
        /// This value is required, must already be added via AddThumbnailPreview method, and cannot be null.</param>
        /// <param name="insertBeforePreview">The preview of the tab window whose thumbnail that previewToChange is inserted to the left of. 
        /// This preview must already be added via AddThumbnailPreview. If this value is null, the previewToChange tab is added to the end of the list.
        /// </param>
        public static void SetTabOrder(TabbedThumbnail previewToChange, TabbedThumbnail insertBeforePreview)
        {
            if (previewToChange == null)
            {
                throw new ArgumentNullException("previewToChange");
            }

            IntPtr handleToReorder = previewToChange.TaskbarWindow.WindowToTellTaskbarAbout;

            if (insertBeforePreview == null)
            {
                TaskbarList.Instance.SetTabOrder(handleToReorder, IntPtr.Zero);
            }
            else
            {
                IntPtr handleBefore = insertBeforePreview.TaskbarWindow.WindowToTellTaskbarAbout;
                TaskbarList.Instance.SetTabOrder(handleToReorder, handleBefore);
            }
        }
    }
}
