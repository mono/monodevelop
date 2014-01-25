//
// WindowsPlatform.cs
//
// Author:
//   Jonathan Pobst <monkey@jpobst.com>
//   Lluis Sanchez Gual <lluis@novell.com>
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2007-2011 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Win32;
using CustomControls.OS;
using MonoDevelop.Ide.Desktop;
using System.Diagnostics;
using MonoDevelop.Core.Execution;
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Platform
{
	public class WindowsPlatform : PlatformService
	{
		public override string DefaultMonospaceFont {
			get {
				// Vista has the very beautiful Consolas
				if (Environment.OSVersion.Version.Major >= 6)
					return "Consolas 10";
					
				return "Courier New 10";
			}
		}

		public override string Name {
			get { return "Windows"; }
		}
		
		public override object GetFileAttributes (string fileName)
		{
			return null;
		}
		
		public override void SetFileAttributes (string fileName, object attributes)
		{
		}
		
		protected override string OnGetMimeTypeForUri (string uri)
		{
			string ext = Path.GetExtension (uri).ToLower ();
			
			RegistryKey typeKey = Registry.ClassesRoot.OpenSubKey (ext, false);
			if (typeKey == null)
				return base.OnGetMimeTypeForUri (uri);
			try {
				string type = (string) typeKey.GetValue ("Content Type");
				if (type == null) {
					string ptype = (string) typeKey.GetValue ("PerceivedType");
					if (ptype == "text")
						type = "text/plain";
				}
				return type ?? base.OnGetMimeTypeForUri (uri);
			}
			finally {
				typeKey.Close ();
			}
		}
		
		protected override Gdk.Pixbuf OnGetPixbufForType (string type, Gtk.IconSize size)
		{
			return base.OnGetPixbufForType (type, size);
		}
		
		Dictionary<string, Gdk.Pixbuf> icons = new Dictionary<string, Gdk.Pixbuf> ();

		// http://msdn.microsoft.com/en-us/library/windows/desktop/bb762179(v=vs.85).aspx
		// FIXME: You should call this function from a background thread. Failure to do so could cause the UI to stop responding.
		protected override Gdk.Pixbuf OnGetPixbufForFile (string filename, Gtk.IconSize size)
		{
			SHFILEINFO shinfo = new SHFILEINFO();
			Win32.SHGetFileInfoW (filename, Win32.FILE_ATTRIBUTES_NORMAL, ref shinfo, (uint)Marshal.SizeOf (shinfo),  Win32.SHGFI_USEFILEATTRIBUTES | Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON | Win32.SHGFI_ICONLOCATION | Win32.SHGFI_TYPENAME);
			if (shinfo.iIcon == 0) {
				Win32.DestroyIcon (shinfo.hIcon);
				return null;
			}
			string key = shinfo.iIcon + " - " + shinfo.szDisplayName;
			Gdk.Pixbuf pix;
			if (!icons.TryGetValue (key, out pix)) {
				var icon = Icon.FromHandle (shinfo.hIcon);
				pix = CreateFromResource (icon.ToBitmap ());
				icons[key] = pix;
			}
			Win32.DestroyIcon (shinfo.hIcon);
			return pix;
		}

		protected override string OnGetMimeTypeDescription (string mimeType)
		{
			if (mimeType == "text/plain")
				return "Text document";
			if (mimeType == "application/xml")
				return "XML document";
			else
				return mimeType;
		}
		
		public Gdk.Pixbuf CreateFromResource (Bitmap bitmap)
		{
			MemoryStream ms = new MemoryStream ();
			bitmap.Save (ms, ImageFormat.Png);
			ms.Position = 0;
			return new Gdk.Pixbuf (ms);
		}

		// Note: we can't reuse RectangleF because the layout is different...
		[StructLayout (LayoutKind.Sequential)]
		struct Rect {
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public int X { get { return Left; } }
			public int Y { get { return Top; } }
			public int Width { get { return Right - Left; } }
			public int Height { get { return Bottom - Top; } }
		}

		const int MonitorInfoFlagsPrimary = 0x01;

		[StructLayout (LayoutKind.Sequential)]
		unsafe struct MonitorInfo {
			public int Size;
			public Rect Frame;         // Monitor
			public Rect VisibleFrame;  // Work
			public int Flags;
			public fixed byte Device[32];
		}

		[UnmanagedFunctionPointer (CallingConvention.Winapi)]
		delegate int EnumMonitorsCallback (IntPtr hmonitor, IntPtr hdc, IntPtr prect, IntPtr user_data);

		[DllImport (Win32.USER32)]
		extern static int EnumDisplayMonitors (IntPtr hdc, IntPtr clip, EnumMonitorsCallback callback, IntPtr user_data);

		[DllImport (Win32.USER32)]
		extern static int GetMonitorInfoA (IntPtr hmonitor, ref MonitorInfo info);

		public override Gdk.Rectangle GetUsableMonitorGeometry (Gdk.Screen screen, int monitor_id)
		{
			Gdk.Rectangle geometry = screen.GetMonitorGeometry (monitor_id);
			List<MonitorInfo> screens = new List<MonitorInfo> ();

			EnumDisplayMonitors (IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hmonitor, IntPtr hdc, IntPtr prect, IntPtr user_data) {
				var info = new MonitorInfo ();

				unsafe {
					info.Size = sizeof (MonitorInfo);
				}

				GetMonitorInfoA (hmonitor, ref info);

				// In order to keep the order the same as Gtk, we need to put the primary monitor at the beginning.
				if ((info.Flags & MonitorInfoFlagsPrimary) != 0)
					screens.Insert (0, info);
				else
					screens.Add (info);

				return 1;
			}, IntPtr.Zero);

			MonitorInfo monitor = screens[monitor_id];
			Rect visible = monitor.VisibleFrame;
			Rect frame = monitor.Frame;

			// Rebase the VisibleFrame off of Gtk's idea of this monitor's geometry (since they use different coordinate systems)
			int x = geometry.X + (visible.Left - frame.Left);
			int width = visible.Width;

			int y = geometry.Y + (visible.Top - frame.Top);
			int height = visible.Height;

			return new Gdk.Rectangle (x, y, width, height);
		}

		static ProcessStartInfo CreateConsoleStartInfo (
			string command, string arguments, string workingDirectory,
			IDictionary<string, string> environmentVariables,
			string title, bool pauseWhenFinished)
		{
			var sb = new StringBuilder ();
			if (command != null) {
				sb.Append ("/C \"");
				if (title != null)
					sb.Append ("title " + title + " && ");
				sb.Append ("\"" + command + "\" " + arguments);
				if (pauseWhenFinished)
					sb.Append (" & pause");
				sb.Append ("\"");
			} else if (title != null) {
				sb.Append ("/K \"title " + title + "\"");
			}
			var psi = new ProcessStartInfo ("cmd.exe", sb.ToString ()) {
				CreateNoWindow = false,
				WorkingDirectory = workingDirectory,
				UseShellExecute = false,
			};
			if (environmentVariables != null)
				foreach (var env in environmentVariables)
					psi.EnvironmentVariables [env.Key] = env.Value;
			return psi;
		}

		public override IProcessAsyncOperation StartConsoleProcess (
			string command, string arguments, string workingDirectory,
			IDictionary<string, string> environmentVariables,
			string title, bool pauseWhenFinished)
		{
			var proc = new ProcessWrapper {
				StartInfo = CreateConsoleStartInfo (
					command, arguments, workingDirectory, environmentVariables, title, pauseWhenFinished
				)
			};
			proc.Start ();
			return proc;
		}

		public override bool CanOpenTerminal {
			get { return true; }
		}

		public override void OpenTerminal (FilePath directory, IDictionary<string, string> environmentVariables, string title)
		{
			Process.Start (CreateConsoleStartInfo (null, null, directory, environmentVariables, title, false));
		}

		protected override RecentFiles CreateRecentFilesProvider ()
		{
			return new WindowsRecentFiles ();
		}
	}
}
