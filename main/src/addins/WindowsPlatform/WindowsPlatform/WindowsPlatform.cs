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

		public enum AssociationFlags {
			None = 0x00000000,
			InitNoRemapClsid = 0x00000001,
			InitByExeName = 0x00000002,
			OpenByExeName = 0x00000002,
			InitDefaultToStar = 0x00000004,
			InitDefaultToFolder = 0x00000008,
			NoUserSettings = 0x00000010,
			NoTruncate = 0x00000020,
			Verify = 0x00000040,
			RemapRunDll = 0x00000080,
			NoFixups = 0x00000100,
			IgnoreBaseClass = 0x00000200,
			InitIgnoreUnknown = 0x00000400,
			InitFixedProgid = 0x00000800,
			IsProtocol = 0x00001000
		}

		public enum AssociationString {
			Command = 1,
			Executable,
			FriendlyDocName,
			FriendlyAppName,
			NoOpen,
			ShellNewValue,
			DdeCommand,
			DdeIfExec,
			DdeApplication,
			DdeTopic,
			InfoTip,
			QuickTip,
			Tileinfo,
			ContentType,
			DefaultIcon,
			ShellExtension,
			DropTrget,
			DelegateExecute,
			SupportedUriProtocols,
			MaxString
		}

		public static string QueryAssociationString (string assoc, AssociationString str, AssociationFlags flags, string extra = null)
		{
			if (assoc == null)
				throw new ArgumentNullException("assoc");

			flags |= AssociationFlags.NoTruncate;

			const uint E_POINTER = 0x80004003;

			var builder = new StringBuilder (512);
			int size = builder.Length;

			var result = AssocQueryStringW (flags, str, assoc, extra, builder, ref size);

			if (result == unchecked((int)E_POINTER)) {
				builder.Length = size;
				result = AssocQueryStringW (flags, str, assoc, extra, builder, ref size);
			}
			Marshal.ThrowExceptionForHR (result);
			return builder.ToString ();
		}

		[DllImport ("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern int AssocQueryStringW (AssociationFlags flags, AssociationString str, string assoc,
						    string extra, StringBuilder outBuffer, ref int outBufferSize);

		static string GetDefaultApp (string extension)
		{
			string appDefault = null;
			using (RegistryKey RegKey = Registry.ClassesRoot.OpenSubKey (extension)) {
				if (RegKey != null)
					appDefault = (string)RegKey.GetValue ("", null);
			}
			return appDefault;
		}

		public override IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			string extension = Path.GetExtension (filename);
			if (string.IsNullOrEmpty (extension))
				yield break;
			string defaultApp = GetDefaultApp (extension);

			foreach (var app in GetAppsByProgID (extension, defaultApp))
				yield return app;

			foreach (var app in GetAppsByExeName (extension, defaultApp))
				yield return app;
		}

		const string assocBaseKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\";

		static IEnumerable<DesktopApplication> GetAppsByProgID (string extension, string defaultApp)
		{
			var progIDs = new HashSet<string> ();

			using (RegistryKey RegKey = Registry.CurrentUser.OpenSubKey (assocBaseKey + extension + @"\OpenWithProgids")) {
				if (RegKey != null) {
					foreach (string progID in RegKey.GetValueNames ()) {
						if (progIDs.Add (progID)) {
							var app = WindowsAppFromProgID (progID, defaultApp);
							if (app != null)
								yield return app;
						}
					}
				}
			}

			using (RegistryKey RegKey = Registry.ClassesRoot.OpenSubKey (extension + @"\OpenWithProgids")) {
				if (RegKey != null) {
					foreach (string progID in RegKey.GetValueNames ()) {
						if (progIDs.Add (progID)) {
							var app = WindowsAppFromProgID (progID, defaultApp);
							if (app != null)
								yield return app;
						}
					}
				}
			}
		}

		static IEnumerable<DesktopApplication> GetAppsByExeName (string extension, string defaultApp)
		{
			var exeNames = new HashSet<string> ();

			using (RegistryKey RegKey = Registry.CurrentUser.OpenSubKey (assocBaseKey + extension + @"\OpenWithList")) {
				if (RegKey != null) {
					string MRUList = (string)RegKey.GetValue ("MRUList");
					if(MRUList != null){
						foreach (char c in MRUList.ToString()) {
							string exeName = RegKey.GetValue (c.ToString ()).ToString ();
							if (exeNames.Add (exeName)) {
								var app = WindowsAppFromExeName (exeName, defaultApp);
								if (app != null)
									yield return app;
							}
						}
					}
				}
			}

			using (RegistryKey RegKey = Registry.ClassesRoot.OpenSubKey (extension + @"\OpenWithList")) {
				if (RegKey != null) {
					foreach (string exeName in RegKey.GetSubKeyNames ()) {
						if (exeNames.Add (exeName)) {
							var app = WindowsAppFromExeName (exeName, defaultApp);
							if (app != null)
								yield return app;
						}
					}
				}
			}
		}



		static WindowsDesktopApplication WindowsAppFromProgID (string progID, string defaultApp)
		{
			try {
				string displayName = QueryAssociationString (progID, AssociationString.FriendlyAppName, AssociationFlags.None, "open");
				string exePath = QueryAssociationString (progID, AssociationString.Executable, AssociationFlags.None, "open");
				return new WindowsDesktopApplication (progID, displayName, exePath, progID.Equals (defaultApp));
			} catch (Exception ex) {
				LoggingService.LogError (string.Format ("Failed to read info for ProgID '{0}'", progID), ex);
				return null;
			}
		}

		static WindowsDesktopApplication WindowsAppFromExeName (string exeName, string defaultApp)
		{
			try {
				string displayName = QueryAssociationString (exeName, AssociationString.FriendlyAppName, AssociationFlags.OpenByExeName, "open");
				string exePath = QueryAssociationString (exeName, AssociationString.Executable, AssociationFlags.OpenByExeName, "open");
				return new WindowsDesktopApplication (exeName, displayName, exePath, exeName.Equals (defaultApp));
			} catch (Exception ex) {
				LoggingService.LogError (string.Format ("Failed to read info for ExeName '{0}'", exeName), ex);
				return null;
			}
		}

		class WindowsDesktopApplication : DesktopApplication
		{
			public WindowsDesktopApplication (string id, string displayName, string exePath, bool isDefault) : base (id, displayName, isDefault)
			{
				this.ExePath = exePath;
			}

			public string ExePath  { get; private set; }

			public override void Launch (params string[] files)
			{
				var pab = new ProcessArgumentBuilder ();
				foreach (string file in files) {
					pab.AddQuoted (file);
					Process.Start (ExePath, pab.ToString ());
				}
			}
		}
	}
}
