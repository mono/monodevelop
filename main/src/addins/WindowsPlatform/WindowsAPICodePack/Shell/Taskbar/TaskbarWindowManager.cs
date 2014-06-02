// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.Resources;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Taskbar
{
    internal static class TaskbarWindowManager
    {
        internal static List<TaskbarWindow> _taskbarWindowList = new List<TaskbarWindow>();

        private static bool _buttonsAdded;

        internal static void AddThumbnailButtons(IntPtr userWindowHandle, params ThumbnailToolBarButton[] buttons)
        {
            // Try to get an existing taskbar window for this user windowhandle            
            TaskbarWindow taskbarWindow = GetTaskbarWindow(userWindowHandle, TaskbarProxyWindowType.ThumbnailToolbar);
            TaskbarWindow temp = null;
            try
            {
                AddThumbnailButtons(
                    taskbarWindow ?? (temp = new TaskbarWindow(userWindowHandle, buttons)),
                    taskbarWindow == null,
                    buttons);
            }
            catch
            {
                if (temp != null) { temp.Dispose(); }
                throw;
            }
        }

        internal static void AddThumbnailButtons(System.Windows.UIElement control, params ThumbnailToolBarButton[] buttons)
        {
            // Try to get an existing taskbar window for this user uielement            
            TaskbarWindow taskbarWindow = GetTaskbarWindow(control, TaskbarProxyWindowType.ThumbnailToolbar);
            TaskbarWindow temp = null;
            try
            {
                AddThumbnailButtons(
                    taskbarWindow ?? (temp = new TaskbarWindow(control, buttons)),
                    taskbarWindow == null,
                    buttons);
            }
            catch
            {
                if (temp != null) { temp.Dispose(); }
                throw;
            }
        }

        private static void AddThumbnailButtons(TaskbarWindow taskbarWindow, bool add, params ThumbnailToolBarButton[] buttons)
        {
            if (add)
            {
                _taskbarWindowList.Add(taskbarWindow);
            }
            else if (taskbarWindow.ThumbnailButtons == null)
            {
                taskbarWindow.ThumbnailButtons = buttons;
            }
            else
            {
                // We already have buttons assigned
                throw new InvalidOperationException(LocalizedMessages.TaskbarWindowManagerButtonsAlreadyAdded);
            }
        }

        internal static void AddTabbedThumbnail(TabbedThumbnail preview)
        {
            // Create a TOP-LEVEL proxy window for the user's source window/control
            TaskbarWindow taskbarWindow = null;

            // get the TaskbarWindow for UIElement/WindowHandle respectfully.
            if (preview.WindowHandle == IntPtr.Zero)
            {
                taskbarWindow = GetTaskbarWindow(preview.WindowsControl, TaskbarProxyWindowType.TabbedThumbnail);
            }
            else
            {
                taskbarWindow = GetTaskbarWindow(preview.WindowHandle, TaskbarProxyWindowType.TabbedThumbnail);
            }

            //create taskbar, or set its TabbedThumbnail
            if (taskbarWindow == null)
            {
                taskbarWindow = new TaskbarWindow(preview);
                _taskbarWindowList.Add(taskbarWindow);
            }
            else if (taskbarWindow.TabbedThumbnail == null)
            {
                taskbarWindow.TabbedThumbnail = preview;
            }

            // Listen for Title changes
            preview.TitleChanged += new EventHandler(thumbnailPreview_TitleChanged);
            preview.TooltipChanged += new EventHandler(thumbnailPreview_TooltipChanged);

            // Get/Set properties for proxy window
            IntPtr windowHandle = taskbarWindow.WindowToTellTaskbarAbout;

            // Register this new tab and set it as being active.
            TaskbarList.Instance.RegisterTab(windowHandle, preview.ParentWindowHandle);
            TaskbarList.Instance.SetTabOrder(windowHandle, IntPtr.Zero);
            TaskbarList.Instance.SetTabActive(windowHandle, preview.ParentWindowHandle, 0);

            // We need to make sure we can set these properties even when running with admin 
            TabbedThumbnailNativeMethods.ChangeWindowMessageFilter(
                TabbedThumbnailNativeMethods.WmDwmSendIconicThumbnail,
                TabbedThumbnailNativeMethods.MsgfltAdd);

            TabbedThumbnailNativeMethods.ChangeWindowMessageFilter(
                TabbedThumbnailNativeMethods.WmDwmSendIconicLivePreviewBitmap,
                TabbedThumbnailNativeMethods.MsgfltAdd);

            // BUG: There should be somewhere to disable CustomWindowPreview. I didn't find it.
            TabbedThumbnailNativeMethods.EnableCustomWindowPreview(windowHandle, true);

            // Make sure we use the initial title set by the user
            // Trigger a "fake" title changed event, so the title is set on the taskbar thumbnail.
            // Empty/null title will be ignored.
            thumbnailPreview_TitleChanged(preview, EventArgs.Empty);
            thumbnailPreview_TooltipChanged(preview, EventArgs.Empty);

            // Indicate to the preview that we've added it on the taskbar
            preview.AddedToTaskbar = true;
        }

        internal static TaskbarWindow GetTaskbarWindow(System.Windows.UIElement windowsControl, TaskbarProxyWindowType taskbarProxyWindowType)
        {
            if (windowsControl == null) { throw new ArgumentNullException("windowsControl"); }

            TaskbarWindow toReturn = _taskbarWindowList.FirstOrDefault(window =>
            {
                return (window.TabbedThumbnail != null && window.TabbedThumbnail.WindowsControl == windowsControl) ||
                    (window.ThumbnailToolbarProxyWindow != null &&
                     window.ThumbnailToolbarProxyWindow.WindowsControl == windowsControl);
            });

            if (toReturn != null)
            {
                if (taskbarProxyWindowType == TaskbarProxyWindowType.ThumbnailToolbar)
                {
                    toReturn.EnableThumbnailToolbars = true;
                }
                else if (taskbarProxyWindowType == TaskbarProxyWindowType.TabbedThumbnail)
                {
                    toReturn.EnableTabbedThumbnails = true;
                }
            }

            return toReturn;
        }

        internal static TaskbarWindow GetTaskbarWindow(IntPtr userWindowHandle, TaskbarProxyWindowType taskbarProxyWindowType)
        {
            if (userWindowHandle == IntPtr.Zero)
            {
                throw new ArgumentException(LocalizedMessages.CommonFileDialogInvalidHandle, "userWindowHandle");
            }

            TaskbarWindow toReturn = _taskbarWindowList.FirstOrDefault(window => window.UserWindowHandle == userWindowHandle);

            // If its not in the list, return null so it can be added.            
            if (toReturn != null)
            {
                if (taskbarProxyWindowType == TaskbarProxyWindowType.ThumbnailToolbar)
                {
                    toReturn.EnableThumbnailToolbars = true;
                }
                else if (taskbarProxyWindowType == TaskbarProxyWindowType.TabbedThumbnail)
                {
                    toReturn.EnableTabbedThumbnails = true;
                }
            }

            return toReturn;
        }

        #region Message dispatch methods
        private static void DispatchTaskbarButtonMessages(ref System.Windows.Forms.Message m, TaskbarWindow taskbarWindow)
        {
            if (m.Msg == (int)TaskbarNativeMethods.WmTaskbarButtonCreated)
            {
                AddButtons(taskbarWindow);
            }
            else
            {
                if (!_buttonsAdded)
                {
                    AddButtons(taskbarWindow);
                }

                if (m.Msg == TaskbarNativeMethods.WmCommand &&
                    CoreNativeMethods.GetHiWord(m.WParam.ToInt64(), 16) == ThumbButton.Clicked)
                {
                    int buttonId = CoreNativeMethods.GetLoWord(m.WParam.ToInt64());

                    var buttonsFound =
                        from b in taskbarWindow.ThumbnailButtons
                        where b.Id == buttonId
                        select b;

                    foreach (ThumbnailToolBarButton button in buttonsFound)
                    {
                        button.FireClick(taskbarWindow);
                    }
                }
            }
        }

        private static bool DispatchActivateMessage(ref System.Windows.Forms.Message m, TaskbarWindow taskbarWindow)
        {
            if (m.Msg == (int)WindowMessage.Activate)
            {
                // Raise the event
                taskbarWindow.TabbedThumbnail.OnTabbedThumbnailActivated();
                SetActiveTab(taskbarWindow);
                return true;
            }
            return false;
        }

        private static bool DispatchSendIconThumbnailMessage(ref System.Windows.Forms.Message m, TaskbarWindow taskbarWindow)
        {
            if (m.Msg == (int)TaskbarNativeMethods.WmDwmSendIconThumbnail)
            {
                int width = (int)((long)m.LParam >> 16);
                int height = (int)(((long)m.LParam) & (0xFFFF));
                Size requestedSize = new Size(width, height);

                // Fire an event to let the user update their bitmap
                taskbarWindow.TabbedThumbnail.OnTabbedThumbnailBitmapRequested();

                IntPtr hBitmap = IntPtr.Zero;

                // Default size for the thumbnail
                Size realWindowSize = new Size(200, 200);

                // Get the size of teh control or UIElement
                if (taskbarWindow.TabbedThumbnail.WindowHandle != IntPtr.Zero)
                {
                    TabbedThumbnailNativeMethods.GetClientSize(taskbarWindow.TabbedThumbnail.WindowHandle, out realWindowSize);
                }
                else if (taskbarWindow.TabbedThumbnail.WindowsControl != null)
                {
                    realWindowSize = new Size(
                        Convert.ToInt32(taskbarWindow.TabbedThumbnail.WindowsControl.RenderSize.Width),
                        Convert.ToInt32(taskbarWindow.TabbedThumbnail.WindowsControl.RenderSize.Height));
                }

                if (realWindowSize.Height == -1 && realWindowSize.Width == -1)
                {
                    realWindowSize.Width = realWindowSize.Height = 199;
                }

                // capture the bitmap for the given control
                // If the user has already specified us a bitmap to use, use that.
                if (taskbarWindow.TabbedThumbnail.ClippingRectangle != null &&
                    taskbarWindow.TabbedThumbnail.ClippingRectangle.Value != Rectangle.Empty)
                {
                    if (taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero)
                    {
                        hBitmap = GrabBitmap(taskbarWindow, realWindowSize);
                    }
                    else
                    {
                        hBitmap = taskbarWindow.TabbedThumbnail.CurrentHBitmap;
                    }

                    // Clip the bitmap we just got.
                    Bitmap bmp = Bitmap.FromHbitmap(hBitmap);

                    Rectangle clippingRectangle = taskbarWindow.TabbedThumbnail.ClippingRectangle.Value;

                    // If our clipping rect is out of bounds, update it
                    if (clippingRectangle.Height > requestedSize.Height)
                    {
                        clippingRectangle.Height = requestedSize.Height;
                    }
                    if (clippingRectangle.Width > requestedSize.Width)
                    {
                        clippingRectangle.Width = requestedSize.Width;
                    }

                    // NOTE: Is this a memory leak?
                    bmp = bmp.Clone(clippingRectangle, bmp.PixelFormat);

                    // Make sure we dispose the bitmap before assigning, otherwise we'll have a memory leak
                    if (hBitmap != IntPtr.Zero && taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero)
                    {
                        ShellNativeMethods.DeleteObject(hBitmap);
                    }
                    hBitmap = bmp.GetHbitmap();
                    bmp.Dispose();
                }
                else
                {
                    // Else, user didn't want any clipping, if they haven't provided us a bitmap,
                    // use the screencapture utility and capture it.

                    hBitmap = taskbarWindow.TabbedThumbnail.CurrentHBitmap;

                    // If no bitmap, capture one using the utility
                    if (hBitmap == IntPtr.Zero)
                    {
                        hBitmap = GrabBitmap(taskbarWindow, realWindowSize);
                    }
                }

                // Only set the thumbnail if it's not null. 
                // If it's null (either we didn't get the bitmap or size was 0),
                // let DWM handle it
                if (hBitmap != IntPtr.Zero)
                {
                    Bitmap temp = TabbedThumbnailScreenCapture.ResizeImageWithAspect(
                        hBitmap, requestedSize.Width, requestedSize.Height, true);

                    if (taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero)
                    {
                        ShellNativeMethods.DeleteObject(hBitmap);
                    }

                    hBitmap = temp.GetHbitmap();
                    TabbedThumbnailNativeMethods.SetIconicThumbnail(taskbarWindow.WindowToTellTaskbarAbout, hBitmap);
                    temp.Dispose();
                }

                // If the bitmap we have is not coming from the user (i.e. we created it here),
                // then make sure we delete it as we don't need it now.
                if (taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero)
                {
                    ShellNativeMethods.DeleteObject(hBitmap);
                }

                return true;
            }
            return false;
        }

        private static bool DispatchLivePreviewBitmapMessage(ref System.Windows.Forms.Message m, TaskbarWindow taskbarWindow)
        {
            if (m.Msg == (int)TaskbarNativeMethods.WmDwmSendIconicLivePreviewBitmap)
            {
                // Try to get the width/height
                int width = (int)(((long)m.LParam) >> 16);
                int height = (int)(((long)m.LParam) & (0xFFFF));

                // Default size for the thumbnail
                Size realWindowSize = new Size(200, 200);

                if (taskbarWindow.TabbedThumbnail.WindowHandle != IntPtr.Zero)
                {
                    TabbedThumbnailNativeMethods.GetClientSize(taskbarWindow.TabbedThumbnail.WindowHandle, out realWindowSize);
                }
                else if (taskbarWindow.TabbedThumbnail.WindowsControl != null)
                {
                    realWindowSize = new Size(
                        Convert.ToInt32(taskbarWindow.TabbedThumbnail.WindowsControl.RenderSize.Width),
                        Convert.ToInt32(taskbarWindow.TabbedThumbnail.WindowsControl.RenderSize.Height));
                }

                // If we don't have a valid height/width, use the original window's size
                if (width <= 0)
                {
                    width = realWindowSize.Width;
                }
                if (height <= 0)
                {
                    height = realWindowSize.Height;
                }

                // Fire an event to let the user update their bitmap
                // Raise the event
                taskbarWindow.TabbedThumbnail.OnTabbedThumbnailBitmapRequested();

                // capture the bitmap for the given control
                // If the user has already specified us a bitmap to use, use that.
                IntPtr hBitmap = taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero ? GrabBitmap(taskbarWindow, realWindowSize) : taskbarWindow.TabbedThumbnail.CurrentHBitmap;

                // If we have a valid parent window handle,
                // calculate the offset so we can place the "peek" bitmap
                // correctly on the app window
                if (taskbarWindow.TabbedThumbnail.ParentWindowHandle != IntPtr.Zero && taskbarWindow.TabbedThumbnail.WindowHandle != IntPtr.Zero)
                {
                    System.Drawing.Point offset = new System.Drawing.Point();

                    // if we don't have a offset specified already by the user...
                    if (!taskbarWindow.TabbedThumbnail.PeekOffset.HasValue)
                    {
                        offset = WindowUtilities.GetParentOffsetOfChild(taskbarWindow.TabbedThumbnail.WindowHandle, taskbarWindow.TabbedThumbnail.ParentWindowHandle);
                    }
                    else
                    {
                        offset = new System.Drawing.Point(Convert.ToInt32(taskbarWindow.TabbedThumbnail.PeekOffset.Value.X),
                            Convert.ToInt32(taskbarWindow.TabbedThumbnail.PeekOffset.Value.Y));
                    }

                    // Only set the peek bitmap if it's not null. 
                    // If it's null (either we didn't get the bitmap or size was 0),
                    // let DWM handle it
                    if (hBitmap != IntPtr.Zero)
                    {
                        if (offset.X >= 0 && offset.Y >= 0)
                        {
                            TabbedThumbnailNativeMethods.SetPeekBitmap(
                                taskbarWindow.WindowToTellTaskbarAbout,
                                hBitmap, offset,
                                taskbarWindow.TabbedThumbnail.DisplayFrameAroundBitmap);
                        }
                    }

                    // If the bitmap we have is not coming from the user (i.e. we created it here),
                    // then make sure we delete it as we don't need it now.
                    if (taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero)
                    {
                        ShellNativeMethods.DeleteObject(hBitmap);
                    }

                    return true;
                }
                // Else, we don't have a valid window handle from the user. This is mostly likely because
                // we have a WPF UIElement control. If that's the case, use a different screen capture method
                // and also couple of ways to try to calculate the control's offset w.r.t it's parent.
                else if (taskbarWindow.TabbedThumbnail.ParentWindowHandle != IntPtr.Zero &&
                    taskbarWindow.TabbedThumbnail.WindowsControl != null)
                {
                    System.Windows.Point offset;

                    if (!taskbarWindow.TabbedThumbnail.PeekOffset.HasValue)
                    {
                        // Calculate the offset for a WPF UIElement control
                        // For hidden controls, we can't seem to perform the transform.
                        GeneralTransform objGeneralTransform = taskbarWindow.TabbedThumbnail.WindowsControl.TransformToVisual(taskbarWindow.TabbedThumbnail.WindowsControlParentWindow);
                        offset = objGeneralTransform.Transform(new System.Windows.Point(0, 0));
                    }
                    else
                    {
                        offset = new System.Windows.Point(taskbarWindow.TabbedThumbnail.PeekOffset.Value.X, taskbarWindow.TabbedThumbnail.PeekOffset.Value.Y);
                    }

                    // Only set the peek bitmap if it's not null. 
                    // If it's null (either we didn't get the bitmap or size was 0),
                    // let DWM handle it
                    if (hBitmap != IntPtr.Zero)
                    {
                        if (offset.X >= 0 && offset.Y >= 0)
                        {
                            TabbedThumbnailNativeMethods.SetPeekBitmap(
                                taskbarWindow.WindowToTellTaskbarAbout,
                                hBitmap, new System.Drawing.Point((int)offset.X, (int)offset.Y),
                                taskbarWindow.TabbedThumbnail.DisplayFrameAroundBitmap);
                        }
                        else
                        {
                            TabbedThumbnailNativeMethods.SetPeekBitmap(
                                taskbarWindow.WindowToTellTaskbarAbout,
                                hBitmap,
                                taskbarWindow.TabbedThumbnail.DisplayFrameAroundBitmap);
                        }
                    }

                    // If the bitmap we have is not coming from the user (i.e. we created it here),
                    // then make sure we delete it as we don't need it now.
                    if (taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero)
                    {
                        ShellNativeMethods.DeleteObject(hBitmap);
                    }

                    return true;
                }
                else
                {
                    // Else (no parent specified), just set the bitmap. It would take over the entire 
                    // application window (would work only if you are a MDI app)

                    // Only set the peek bitmap if it's not null. 
                    // If it's null (either we didn't get the bitmap or size was 0),
                    // let DWM handle it
                    if (hBitmap != null)
                    {
                        TabbedThumbnailNativeMethods.SetPeekBitmap(taskbarWindow.WindowToTellTaskbarAbout, hBitmap, taskbarWindow.TabbedThumbnail.DisplayFrameAroundBitmap);
                    }

                    // If the bitmap we have is not coming from the user (i.e. we created it here),
                    // then make sure we delete it as we don't need it now.
                    if (taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero)
                    {
                        ShellNativeMethods.DeleteObject(hBitmap);
                    }

                    return true;
                }
            }
            return false;
        }

        private static bool DispatchDestroyMessage(ref System.Windows.Forms.Message m, TaskbarWindow taskbarWindow)
        {
            if (m.Msg == (int)WindowMessage.Destroy)
            {
                TaskbarList.Instance.UnregisterTab(taskbarWindow.WindowToTellTaskbarAbout);

                taskbarWindow.TabbedThumbnail.RemovedFromTaskbar = true;

                return true;
            }
            return false;
        }

        private static bool DispatchNCDestroyMessage(ref System.Windows.Forms.Message m, TaskbarWindow taskbarWindow)
        {
            if (m.Msg == (int)WindowMessage.NCDestroy)
            {
                // Raise the event
                taskbarWindow.TabbedThumbnail.OnTabbedThumbnailClosed();
                
                // Remove the taskbar window from our internal list
                if (_taskbarWindowList.Contains(taskbarWindow))
                {
                    _taskbarWindowList.Remove(taskbarWindow);
                }

                taskbarWindow.Dispose();

                return true;
            }
            return false;
        }

        private static bool DispatchSystemCommandMessage(ref System.Windows.Forms.Message m, TaskbarWindow taskbarWindow)
        {
            if (m.Msg == (int)WindowMessage.SystemCommand)
            {
                if (((int)m.WParam) == TabbedThumbnailNativeMethods.ScClose)
                {
                    // Raise the event
                    if (taskbarWindow.TabbedThumbnail.OnTabbedThumbnailClosed())
                    {
                        // Remove the taskbar window from our internal list
                        if (_taskbarWindowList.Contains(taskbarWindow))
                        {
                            _taskbarWindowList.Remove(taskbarWindow);
                        }

                        taskbarWindow.Dispose();
                        taskbarWindow = null;
                    }
                }
                else if (((int)m.WParam) == TabbedThumbnailNativeMethods.ScMaximize)
                {
                    // Raise the event
                    taskbarWindow.TabbedThumbnail.OnTabbedThumbnailMaximized();
                }
                else if (((int)m.WParam) == TabbedThumbnailNativeMethods.ScMinimize)
                {
                    // Raise the event
                    taskbarWindow.TabbedThumbnail.OnTabbedThumbnailMinimized();
                }

                return true;
            }
            return false;
        }

        #endregion

        /// <summary>
        /// Dispatches a window message so that the appropriate events
        /// can be invoked. This is used for the Taskbar's thumbnail toolbar feature.
        /// </summary>
        /// <param name="m">The window message, typically obtained
        /// from a Windows Forms or WPF window procedure.</param>
        /// <param name="taskbarWindow">Taskbar window for which we are intercepting the messages</param>
        /// <returns>Returns true if this method handles the window message</returns>           
        internal static bool DispatchMessage(ref System.Windows.Forms.Message m, TaskbarWindow taskbarWindow)
        {
            if (taskbarWindow.EnableThumbnailToolbars)
            {
                DispatchTaskbarButtonMessages(ref m, taskbarWindow);
            }

            // If we are removed from the taskbar, ignore all the messages
            if (taskbarWindow.EnableTabbedThumbnails)
            {
                if (taskbarWindow.TabbedThumbnail == null ||
                    taskbarWindow.TabbedThumbnail.RemovedFromTaskbar)
                {
                    return false;
                }

                if (DispatchActivateMessage(ref m, taskbarWindow))
                {
                    return true;
                }

                if (DispatchSendIconThumbnailMessage(ref m, taskbarWindow))
                {
                    return true;
                }

                if (DispatchLivePreviewBitmapMessage(ref m, taskbarWindow))
                {
                    return true;
                }

                if (DispatchDestroyMessage(ref m, taskbarWindow))
                {
                    return true;
                }

                if (DispatchNCDestroyMessage(ref m, taskbarWindow))
                {
                    return true;
                }

                if (DispatchSystemCommandMessage(ref m, taskbarWindow))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Helper function to capture a bitmap for a given window handle or incase of WPF app,
        /// an UIElement.
        /// </summary>
        /// <param name="taskbarWindow">The proxy window for which a bitmap needs to be created</param>
        /// <param name="requestedSize">Size for the requested bitmap image</param>
        /// <returns>Bitmap captured from the window handle or UIElement. Null if the window is hidden or it's size is zero.</returns>
        private static IntPtr GrabBitmap(TaskbarWindow taskbarWindow, System.Drawing.Size requestedSize)
        {
            IntPtr hBitmap = IntPtr.Zero;

            if (taskbarWindow.TabbedThumbnail.WindowHandle != IntPtr.Zero)
            { //TabbedThumbnail is linked to WinformsControl
                if (taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero)
                {
                    using (Bitmap bmp = TabbedThumbnailScreenCapture.GrabWindowBitmap(
                        taskbarWindow.TabbedThumbnail.WindowHandle, requestedSize))
                    {

                        hBitmap = bmp.GetHbitmap();
                    }
                }
                else
                {
                    using (Image img = Image.FromHbitmap(taskbarWindow.TabbedThumbnail.CurrentHBitmap))
                    {
                        using (Bitmap bmp = new Bitmap(img, requestedSize))
                        {
                            hBitmap = bmp != null ? bmp.GetHbitmap() : IntPtr.Zero;
                        }
                    }
                }
            }
            else if (taskbarWindow.TabbedThumbnail.WindowsControl != null)
            { //TabbedThumbnail is linked to a WPF UIElement
                if (taskbarWindow.TabbedThumbnail.CurrentHBitmap == IntPtr.Zero)
                {
                    Bitmap bmp = TabbedThumbnailScreenCapture.GrabWindowBitmap(
                        taskbarWindow.TabbedThumbnail.WindowsControl,
                        96, 96, requestedSize.Width, requestedSize.Height);

                    if (bmp != null)
                    {
                        hBitmap = bmp.GetHbitmap();
                        bmp.Dispose();
                    }
                }
                else
                {
                    using (Image img = Image.FromHbitmap(taskbarWindow.TabbedThumbnail.CurrentHBitmap))
                    {
                        using (Bitmap bmp = new Bitmap(img, requestedSize))
                        {

                            hBitmap = bmp != null ? bmp.GetHbitmap() : IntPtr.Zero;
                        }
                    }
                }
            }

            return hBitmap;
        }

        internal static void SetActiveTab(TaskbarWindow taskbarWindow)
        {
            if (taskbarWindow != null)
            {
                TaskbarList.Instance.SetTabActive(
                    taskbarWindow.WindowToTellTaskbarAbout,
                    taskbarWindow.TabbedThumbnail.ParentWindowHandle, 0);
            }
        }

        internal static void UnregisterTab(TaskbarWindow taskbarWindow)
        {
            if (taskbarWindow != null)
            {
                TaskbarList.Instance.UnregisterTab(taskbarWindow.WindowToTellTaskbarAbout);
            }
        }

        internal static void InvalidatePreview(TaskbarWindow taskbarWindow)
        {
            if (taskbarWindow != null)
            {
                TabbedThumbnailNativeMethods.DwmInvalidateIconicBitmaps(
                    taskbarWindow.WindowToTellTaskbarAbout);
            }
        }

        private static void AddButtons(TaskbarWindow taskbarWindow)
        {
            // Add the buttons
            // Get the array of thumbnail buttons in native format
            ThumbButton[] nativeButtons = (from thumbButton in taskbarWindow.ThumbnailButtons
                                           select thumbButton.Win32ThumbButton).ToArray();

            // Add the buttons on the taskbar
            HResult hr = TaskbarList.Instance.ThumbBarAddButtons(taskbarWindow.WindowToTellTaskbarAbout, (uint)taskbarWindow.ThumbnailButtons.Length, nativeButtons);

            if (!CoreErrorHelper.Succeeded(hr))
            {
                throw new ShellException(hr);
            }

            _buttonsAdded = true;

            foreach (ThumbnailToolBarButton button in taskbarWindow.ThumbnailButtons)
            {
                button.AddedToTaskbar = _buttonsAdded;
            }
        }
        
        #region Event handlers

        private static void thumbnailPreview_TooltipChanged(object sender, EventArgs e)
        {
            TabbedThumbnail preview = sender as TabbedThumbnail;

            TaskbarWindow taskbarWindow = null;

            if (preview.WindowHandle == IntPtr.Zero)
            {
                taskbarWindow = GetTaskbarWindow(preview.WindowsControl, TaskbarProxyWindowType.TabbedThumbnail);
            }
            else
            {
                taskbarWindow = GetTaskbarWindow(preview.WindowHandle, TaskbarProxyWindowType.TabbedThumbnail);
            }

            // Update the proxy window for the tabbed thumbnail            
            if (taskbarWindow != null)
            {
                TaskbarList.Instance.SetThumbnailTooltip(taskbarWindow.WindowToTellTaskbarAbout, preview.Tooltip);
            }
        }

        private static void thumbnailPreview_TitleChanged(object sender, EventArgs e)
        {
            TabbedThumbnail preview = sender as TabbedThumbnail;

            TaskbarWindow taskbarWindow = null;

            if (preview.WindowHandle == IntPtr.Zero)
            {
                taskbarWindow = GetTaskbarWindow(preview.WindowsControl, TaskbarProxyWindowType.TabbedThumbnail);
            }
            else
            {
                taskbarWindow = GetTaskbarWindow(preview.WindowHandle, TaskbarProxyWindowType.TabbedThumbnail);
            }

            // Update the proxy window for the tabbed thumbnail
            if (taskbarWindow != null)
            {
                taskbarWindow.SetTitle(preview.Title);
            }
        }

        #endregion
    }
}
