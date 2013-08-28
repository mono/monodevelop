//
// GdkWin32.cs
//
// Author:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2011 Novell, Inc (http://www.novell.com)
// Copyright (C) 2012-2013 Xamarin Inc. (https://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using MonoDevelop.Core;

namespace MonoDevelop.Platform
{
	public static class GdkWin32
	{
		static readonly uint GotGdkEventsMessage = RegisterWindowMessage ("GDK_WIN32_GOT_EVENTS");

		[DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_drawable_get_handle (IntPtr drawable);

		[DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_hdc_get (IntPtr drawable, IntPtr gc, int usage);

		[DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gdk_win32_hdc_release (IntPtr drawable, IntPtr gc, int usage);

		[DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_set_modal_dialog_libgtk_only (IntPtr window);

		[DllImport ("User32.dll", SetLastError=true, CharSet=CharSet.Auto)]
		static extern uint RegisterWindowMessage (string lpString);

		public static IntPtr HgdiobjGet (Gdk.Drawable drawable)
		{
			return gdk_win32_drawable_get_handle (drawable.Handle);
		}
		
		public static IntPtr HdcGet (Gdk.Drawable drawable, Gdk.GC gc, Gdk.GCValuesMask usage)
		{
			return gdk_win32_hdc_get (drawable.Handle, gc.Handle, (int) usage);
		}
		
		public static void HdcRelease (Gdk.Drawable drawable, Gdk.GC gc, Gdk.GCValuesMask usage)
		{
			gdk_win32_hdc_release (drawable.Handle, gc.Handle, (int) usage);
		}

		public static bool RunModalWin32Dialog (CommonFileDialog dialog, Gtk.Window parent)
		{
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();

			IntPtr ph = HgdiobjGet (parent.GdkWindow);

			IntPtr hdlg = IntPtr.Zero;
			dialog.DialogOpening += delegate {
				try {
					hdlg = GetDialogHandle (dialog);
					SetGtkDialogHook (hdlg);
				} catch (Exception ex) {
					LoggingService.LogError ("Failed to hook win32 dialog messages", ex);
				}
			};

			bool result;
			try {
				result = dialog.ShowDialog (ph) == CommonFileDialogResult.Ok;
			} finally {
				if (hdlg != IntPtr.Zero)
					ClearGtkDialogHook (hdlg);
			}
			return result;
		}

		internal class SpecialForm : Form
		{
			public virtual DialogResult ShowMagicDialog ()
			{
				return ShowDialog ();
			}

			protected override void WndProc (ref Message m)
			{
				if (m.Msg == GotGdkEventsMessage)
					while (Gtk.Application.EventsPending ())
						Gtk.Application.RunIteration ();
				base.WndProc (ref m);
			}
		}

		internal static bool RunModalWin32Form (SpecialForm form, Gtk.Window parent)
		{
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();

			IntPtr ph = HgdiobjGet (parent.GdkWindow);

			IntPtr hdlg = IntPtr.Zero;
			form.Shown += delegate {
				try {
					hdlg = form.Handle;
					SetGtkDialogHook (hdlg, false);
				} catch (Exception ex) {
					LoggingService.LogError ("Failed to hook win32 dialog messages", ex);
				}
			};

			bool result;
			try {
				result = form.ShowMagicDialog () == DialogResult.OK;
			} finally {
				if (hdlg != IntPtr.Zero)
					ClearGtkDialogHook (hdlg);
			}
			return result;
		}

		// logic based on run_mainloop_hook in gtkprintoperation-win32.c
		static IntPtr GtkWindowProc (IntPtr hdlg, uint uiMsg, IntPtr wParam, IntPtr lParam)
		{
			if (uiMsg == GotGdkEventsMessage) {
				while (Gtk.Application.EventsPending ())
					Gtk.Application.RunIteration ();
				return IntPtr.Zero;
			}
			return CallWindowProc (dialogWndProc, hdlg, uiMsg, wParam, lParam);
		}

		static IntPtr GetDialogHandle (CommonFileDialog dialog)
		{
			var f = typeof (CommonFileDialog).GetField ("nativeDialog", BindingFlags.NonPublic | BindingFlags.Instance);
			var obj = f.GetValue (dialog);
			var ow = (IOleWindow) obj;
			IntPtr handle;
			var hr = ow.GetWindow (out handle);
			if (hr != 0)
				throw Marshal.GetExceptionForHR (hr);
			return handle;
		}

		static void SetGtkDialogHook (IntPtr hdlg, bool overrideWndProc = true)
		{
			if (dialogHookSet)
				throw new InvalidOperationException ("There is already an active hook");
			gdk_win32_set_modal_dialog_libgtk_only (hdlg);
			dialogHookSet = true;
			if (overrideWndProc) {
				dialogWndProc = GetWindowLongPtr (hdlg, DWLP_DLGPROC);
				SetWindowLongPtr (hdlg, DWLP_DLGPROC, Marshal.GetFunctionPointerForDelegate (GtkWindowProcDelegate));
			}
		}

		static void ClearGtkDialogHook (IntPtr hdlg)
		{
			gdk_win32_set_modal_dialog_libgtk_only (IntPtr.Zero);
			if (dialogWndProc != IntPtr.Zero) {
				SetWindowLongPtr (hdlg, DWLP_DLGPROC, dialogWndProc);
				dialogWndProc = IntPtr.Zero;
			}
			dialogHookSet = false;
		}

		static IntPtr dialogWndProc;
		static bool dialogHookSet;

		static readonly WindowProc GtkWindowProcDelegate = GtkWindowProc;
		static readonly int DWLP_DLGPROC = IntPtr.Size; // DWLP_MSGRESULT + sizeof(LRESULT);

		[DllImport("user32.dll")]
		static extern IntPtr CallWindowProc (IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		static IntPtr SetWindowLongPtr (IntPtr hWnd, int nIndex, IntPtr dwNewLong)
		{
			if (IntPtr.Size == 4)
				return SetWindowLongPtr32 (hWnd, nIndex, dwNewLong);
			return SetWindowLongPtr64 (hWnd, nIndex, dwNewLong);
		}

		[DllImport ("user32.dll", EntryPoint="SetWindowLongPtr")]
		static extern IntPtr SetWindowLongPtr64 (IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll", EntryPoint="SetWindowLong")]
		static extern IntPtr SetWindowLongPtr32 (IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		static IntPtr GetWindowLongPtr (IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 4)
				return GetWindowLongPtr32 (hWnd, nIndex);
			return GetWindowLongPtr64 (hWnd, nIndex);
		}

		[DllImport ("user32.dll", EntryPoint="GetWindowLongPtr")]
		static extern IntPtr GetWindowLongPtr64 (IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint="GetWindowLong")]
		static extern IntPtr GetWindowLongPtr32 (IntPtr hWnd, int nIndex);

		delegate IntPtr WindowProc (IntPtr hdlg, uint uiMsg, IntPtr wParam, IntPtr lParam);
	}

	
	[ComImport]
	[Guid("00000114-0000-0000-C000-000000000046")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	interface IOleWindow
	{
		/// <summary>
		/// Returns the window handle to one of the windows participating in in-place activation
		/// (frame, document, parent, or in-place object window).
		/// </summary>
		/// <param name="phwnd">Pointer to where to return the window handle.</param>
		int GetWindow (out IntPtr phwnd) ;

		/// <summary>
		/// Determines whether context-sensitive help mode should be entered during an
		/// in-place activation session.
		/// </summary>
		/// <param name="fEnterMode"><c>true</c> if help mode should be entered;
		/// <c>false</c> if it should be exited.</param>
		void ContextSensitiveHelp ([In, MarshalAs(UnmanagedType.Bool)] bool fEnterMode) ;
	}

	[ComImport]
	[Guid ( "b4db1657-70d7-485e-8e3e-6fcb5a5c1802" )]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	interface IModalWindow
	{
		int Show ([In] IntPtr parent);
	}
	
	public class GtkWin32Proxy : IWin32Window
	{
		public GtkWin32Proxy (Gtk.Window gtkWindow)
		{
			Handle = GdkWin32.HgdiobjGet (gtkWindow.RootWindow);
		}
		
		public IntPtr Handle { get; private set; }
	}
}