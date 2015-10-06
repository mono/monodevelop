//
// GtkWin32Interop.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#if WIN32
using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace MonoDevelop.Components.Windows
{
	public static class GtkWin32Interop
	{
		internal const string LIBGDK = "libgdk-win32-2.0-0.dll";
		internal const string USER32 = "user32.dll";

		public enum ExtendedWindowStyles
		{
			// Hides it from alt-tab.
			WS_EX_TOOLWINDOW = 0x80,
		}

		public enum WindowStyles
		{
			// The window has a sizing border.
			WS_SIZEBOX = 0x00040000,
		}

		public enum GWLParameter
		{
			// Sets a new extended style.
			GWL_EXSTYLE = -20,

			// Sets a new application handle instance.
			GWL_HINSTANCE = -6,

			// Sets a new window handle as the parent.
			GWL_HWNDPARENT = -8,

			// Sets a new identifier of the window.
			GWL_ID = -12,

			// Sets a new window style.
			GWL_STYLE = -16,

			// Sets a new user data associated with the window. Initially zero.
			GWL_USERDATA = -21,

			// Sets a new address of the window procedure.
			GWL_WNDPROC = -4,
		}

		[DllImport (LIBGDK, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_window_get_impl_hwnd (IntPtr window);

		public static IntPtr HWndGet (Gdk.Window window)
		{
			return gdk_win32_window_get_impl_hwnd (window.Handle);
		}

		[DllImport (USER32, EntryPoint="SetWindowLongPtr")]
		static extern IntPtr SetWindowLongPtr64 (IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport(USER32, EntryPoint="SetWindowLong")]
		static extern IntPtr SetWindowLongPtr32 (IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		public static IntPtr SetWindowLongPtr (IntPtr hWnd, int nIndex, IntPtr dwNewLong)
		{
			if (IntPtr.Size == 4)
				return SetWindowLongPtr32 (hWnd, nIndex, dwNewLong);
			return SetWindowLongPtr64 (hWnd, nIndex, dwNewLong);
		}

		[DllImport (USER32, EntryPoint="GetWindowLongPtr")]
		static extern IntPtr GetWindowLongPtr64 (IntPtr hWnd, int nIndex);

		[DllImport(USER32, EntryPoint="GetWindowLong")]
		static extern IntPtr GetWindowLongPtr32 (IntPtr hWnd, int nIndex);

		public static IntPtr GetWindowLongPtr (IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 4)
				return GetWindowLongPtr32 (hWnd, nIndex);
			return GetWindowLongPtr64 (hWnd, nIndex);
		}

		internal static Gdk.EventKey ConvertKeyEvent (ModifierKeys mod, Key key)
		{
			var state = Gdk.ModifierType.None;
			if ((mod & ModifierKeys.Control) != 0)
				state |= Gdk.ModifierType.ControlMask;
			if ((mod & ModifierKeys.Shift) != 0)
				state |= Gdk.ModifierType.ShiftMask;
			if ((mod & ModifierKeys.Windows) != 0)
				state |= Gdk.ModifierType.MetaMask;
			if ((mod & ModifierKeys.Alt) != 0)
				state |= Gdk.ModifierType.Mod1Mask;

			return GtkUtil.CreateKeyEventFromKeyCode ((ushort)KeyInterop.VirtualKeyFromKey (key), state, Gdk.EventType.KeyPress, Ide.IdeApp.Workbench.RootWindow.GdkWindow);
		}
	}
}
#endif
