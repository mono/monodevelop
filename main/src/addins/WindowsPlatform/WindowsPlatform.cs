using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Win32;
using CustomControls.OS;
using CustomControls.Controls;
using System.Windows.Forms;
using MonoDevelop.Ide.Desktop;
using System.Diagnostics;
using MonoDevelop.Core.Execution;

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
		
		protected override Gdk.Pixbuf OnGetPixbufForFile (string filename, Gtk.IconSize size)
		{
			SHFILEINFO shinfo = new SHFILEINFO();
			Win32.SHGetFileInfo (filename, 0, ref shinfo, (uint) Marshal.SizeOf (shinfo), Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON | Win32.SHGFI_ICONLOCATION | Win32.SHGFI_TYPENAME);
			if (shinfo.iIcon == IntPtr.Zero)
				return null;
			string key = shinfo.iIcon.ToString () + " - " + shinfo.szDisplayName;
			Gdk.Pixbuf pix;
			if (!icons.TryGetValue (key, out pix)) {
				System.Drawing.Icon icon = System.Drawing.Icon.FromHandle (shinfo.hIcon);
				pix = CreateFromResource (icon.ToBitmap ());
				icons[key] = pix;
			}
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

		[DllImport ("User32.dll")]
		extern static int EnumDisplayMonitors (IntPtr hdc, IntPtr clip, EnumMonitorsCallback callback, IntPtr user_data);

		[DllImport ("User32.dll")]
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

		public override IProcessAsyncOperation StartConsoleProcess (string command, string arguments, string workingDirectory,
		                                                            IDictionary<string, string> environmentVariables, 
		                                                            string title, bool pauseWhenFinished)
		{
			string args = "/C \"title " + title + " && \"" + command + "\" " + arguments;
			if (pauseWhenFinished)
			    args += " & pause\"";
			else
			    args += "\"";
			
			var psi = new ProcessStartInfo ("cmd.exe", args) {
				CreateNoWindow = false,
				WorkingDirectory = workingDirectory,
				UseShellExecute = false,
			};
			foreach (var env in environmentVariables)
				psi.EnvironmentVariables [env.Key] = env.Value;
			
			ProcessWrapper proc = new ProcessWrapper ();
			proc.StartInfo = psi;
			proc.Start ();
			return proc;
        }
		
		protected override RecentFiles CreateRecentFilesProvider ()
		{
			return new MonoDevelop.Platform.WindowsRecentFiles ();
		}
	}
	
	public static class GdkWin32
	{
		[System.Runtime.InteropServices.DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_drawable_get_handle (IntPtr drawable);

		[System.Runtime.InteropServices.DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gdk_win32_hdc_get (IntPtr drawable, IntPtr gc, int usage);

		[System.Runtime.InteropServices.DllImport ("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gdk_win32_hdc_release (IntPtr drawable, IntPtr gc, int usage);
		
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
