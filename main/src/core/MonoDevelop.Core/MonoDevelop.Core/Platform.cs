// 
// Platform.cs
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
using System.IO;
using System.Text;

namespace MonoDevelop.Core
{
	public static class Platform
	{
		public readonly static bool IsWindows;
		public readonly static bool IsMac;
		public readonly static bool IsLinux;

		public static Version OSVersion { get; private set; }

		static Platform ()
		{
			IsWindows = Path.DirectorySeparatorChar == '\\';
			IsMac = !IsWindows && IsRunningOnMac ();
			IsLinux = !IsMac && !IsWindows;
			OSVersion = Environment.OSVersion.Version;

			// needed to make sure various p/invokes work
			if (Platform.IsWindows) {
				InitWindowsNativeLibs ();
			} else if (Platform.IsMac) {
				InitMacFoundation ();
			}
		}

		public static void Initialize ()
		{
			//no-op, triggers static ctor
		}

		[DllImport ("libc")]
		static extern int uname (IntPtr buf);

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
		extern static IntPtr dlopen (string name, int mode);

		static void InitMacFoundation ()
		{
			dlopen ("/System/Library/Frameworks/Foundation.framework/Foundation", 0x1);
			OSVersion = new Version (Gestalt ("sys1"), Gestalt ("sys2"), Gestalt ("sys3"));
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetDllDirectory (string lpPathName);

		[System.Runtime.InteropServices.DllImport ("/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon")]
		static extern int Gestalt (int selector, out int result);

		//TODO: there are other gestalt selectors that return info we might want to display
		//mac API for obtaining info about the system
		static int Gestalt (string selector)
		{
			System.Diagnostics.Debug.Assert (selector != null && selector.Length == 4);
			int cc = selector[3] | (selector[2] << 8) | (selector[1] << 16) | (selector[0] << 24);
			int result;
			int ret = Gestalt (cc, out result);
			if (ret != 0) {
				LoggingService.LogError ("Error reading gestalt for selector '{0}': {1}", selector, ret);
				return 0;
			}
			return result;
		}

		static void InitWindowsNativeLibs ()
		{
			string location = null;
			using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Xamarin\GtkSharp\InstallFolder")) {
				if (key != null) {
					location = key.GetValue (null) as string;
				}
			}
			if (location == null || !File.Exists (Path.Combine (location, "bin", "libgtk-win32-2.0-0.dll"))) {
				LoggingService.LogError ("Did not find registered GTK# installation");
				return;
			}
			var path = Path.Combine (location, @"bin");
			try {
				if (SetDllDirectory (path)) {
					return;
				}
			} catch (EntryPointNotFoundException) {
			}
			LoggingService.LogError ("Unable to set GTK# dll directory");
		}
	}
}