// 
// GtkWorkarounds.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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

using System;
using System.Runtime.InteropServices;

namespace Stetic.Editor
{
	//from Mono.TextEditor/GtkWorkarounds.cs
	public static class GtkWorkarounds
	{
		public static bool TriggersContextMenu (Gdk.EventButton evt)
		{
			return evt.Type == Gdk.EventType.ButtonPress && IsContextMenuButton (evt);
		}
		
		public static bool IsContextMenuButton (Gdk.EventButton evt)
		{
			if (evt.Button == 3 &&
					(evt.State & (Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button2Mask)) == 0)
				return true;
			
			if (Platform.IsMac) {
				if (evt.Button == 1 &&
					(evt.State & Gdk.ModifierType.ControlMask) != 0 &&
					(evt.State & (Gdk.ModifierType.Button2Mask | Gdk.ModifierType.Button3Mask)) == 0)
				return true;
			}
			
			return false;
		}
	}
	
	//from Mono.TextEditor/Platform.cs
	public static class Platform
	{
		static Platform ()
		{
 			IsWindows = System.IO.Path.DirectorySeparatorChar == '\\';
 			IsMac = !IsWindows && IsRunningOnMac();
			IsX11 = !IsMac && System.Environment.OSVersion.Platform == PlatformID.Unix;
		}
		
		static Gdk.Keymap keymap = Gdk.Keymap.Default;
		
		public static bool IsMac { get; private set; }
		public static bool IsX11 { get; private set; }
		public static bool IsWindows { get; private set; }
		
		//From Managed.Windows.Forms/XplatUI
		static bool IsRunningOnMac ()
		{
			IntPtr buf = IntPtr.Zero;
			try {
				buf = Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = Marshal.PtrToStringAnsi (buf);
					if (os == "Darwin")
						return true;
				}
			} catch {
			} finally {
				if (buf != IntPtr.Zero)
					Marshal.FreeHGlobal (buf);
			}
		
			return false;
		}
		
		[DllImport ("libc")]
		static extern int uname (IntPtr buf);
	}
}