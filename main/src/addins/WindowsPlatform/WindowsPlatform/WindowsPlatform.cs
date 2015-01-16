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
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Win32;
using CustomControls.OS;
using MonoDevelop.Ide.Desktop;
using System.Diagnostics;
using MonoDevelop.Core.Execution;
using System.Text;
using MonoDevelop.Core;
using Microsoft.WindowsAPICodePack.Taskbar;
using MonoDevelop.Ide;

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

		public override void Initialize ()
		{
			// Only initialize elements for Win7+.
			if (TaskbarManager.IsPlatformSupported) {
				TaskbarManager.Instance.ApplicationId = BrandingService.ProfileDirectoryName;
			}
		}

		public override void SetGlobalProgressBar (double progress)
		{
			if (!TaskbarManager.IsPlatformSupported)
				return;

			IntPtr handle = GdkWin32.HgdiobjGet (IdeApp.Workbench.RootWindow.GdkWindow);
			if (progress >= 1.0) {
				TaskbarManager.Instance.SetProgressState (TaskbarProgressBarState.NoProgress, handle);
			} else {
				TaskbarManager.Instance.SetProgressState (TaskbarProgressBarState.Normal, handle);
				TaskbarManager.Instance.SetProgressValue ((int)(progress * 100f), 100, handle);
			}
		}

		public override void ShowGlobalProgressBarIndeterminate ()
		{
			if (!TaskbarManager.IsPlatformSupported)
				return;
				
			IntPtr handle = GdkWin32.HgdiobjGet (IdeApp.Workbench.RootWindow.GdkWindow);
			TaskbarManager.Instance.SetProgressState (TaskbarProgressBarState.Indeterminate, handle);
		}

		public override void ShowGlobalProgressBarError ()
		{
			if (!TaskbarManager.IsPlatformSupported)
				return;

			IntPtr handle = GdkWin32.HgdiobjGet (IdeApp.Workbench.RootWindow.GdkWindow);
			TaskbarManager.Instance.SetProgressState (TaskbarProgressBarState.Error, handle);
			TaskbarManager.Instance.SetProgressValue (1, 1, handle);

			// Added a timeout to removing the red progress bar. This is to fix the dependency on a status bar update
			// that won't happen until the status bar receives another update.
			GLib.Timeout.Add (500, delegate {
				TaskbarManager.Instance.SetProgressState (TaskbarProgressBarState.NoProgress, handle);
				return false;
			});
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
			string ext = Path.GetExtension (uri);
			if (ext == null)
				return null;

			ext = ext.ToLower ();
			
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
				typeKey.Dispose ();
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
			using (var ms = new MemoryStream ()) {
				bitmap.Save (ms, ImageFormat.Png);
				ms.Position = 0;
				return new Gdk.Pixbuf (ms);
			}
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
		struct MonitorInfo {
			public int Size;
			public Rect Frame;         // Monitor
			public Rect VisibleFrame;  // Work
			public int Flags;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)]
			public string Device;
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

				info.Size = Marshal.SizeOf (info);

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

		public static string QueryAssociationString (string assoc, AssociationString str, AssociationFlags flags, string extra = null)
		{
			if (assoc == null)
				throw new ArgumentNullException("assoc");

			flags |= AssociationFlags.NoTruncate;

			const uint E_POINTER = 0x80004003;

			var builder = new StringBuilder (512);
			int size = builder.Length;

			var result = Win32.AssocQueryStringW (flags, str, assoc, extra, builder, ref size);

			if (result == unchecked((int)E_POINTER)) {
				builder.Length = size;
				result = Win32.AssocQueryStringW (flags, str, assoc, extra, builder, ref size);
			}
			Marshal.ThrowExceptionForHR (result);
			return builder.ToString ();
		}

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

			foreach (var app in GetAppsForExtension (extension, defaultApp))
				yield return app;
		}

		enum AppOpenWithRegistryType {
			FromValue,
			FromMRUList,
			FromSubkey,
		}
		const string assocBaseKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\";
		static IEnumerable<DesktopApplication> GetAppFromRegistry (RegistryKey key, string defaultApp, HashSet<string> uniqueAppsSet, AssociationFlags flags, AppOpenWithRegistryType type)
		{
			if (key != null) {
				var apps = new string[0];
				if (type == AppOpenWithRegistryType.FromValue)
					apps = key.GetValueNames ();
				else if (type == AppOpenWithRegistryType.FromSubkey)
					apps = key.GetSubKeyNames ();
				else if (type == AppOpenWithRegistryType.FromMRUList) {
					string list = (string)key.GetValue ("MRUList");
					apps = list.Select (c => (string)key.GetValue (c.ToString ())).ToArray ();
				}

				foreach (string appName in apps) {
					var app = WindowsAppFromName (appName, defaultApp, flags);
					if (app != null && uniqueAppsSet.Add (app.ExePath))
						yield return app;
				}
			}
		}

		static IEnumerable<DesktopApplication> GetAppsForExtension (string extension, string defaultApp)
		{
			var uniqueAppsSet = new HashSet<string> ();
			// Query Explorer OpenWithProgids.
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey (assocBaseKey + extension + @"\OpenWithProgids"))
				foreach (var app in GetAppFromRegistry (key, defaultApp, uniqueAppsSet, AssociationFlags.None, AppOpenWithRegistryType.FromValue))
					yield return app;

			// Query extension OpenWithProgids.
			using (RegistryKey key = Registry.ClassesRoot.OpenSubKey (extension + @"\OpenWithProgids"))
				foreach (var app in GetAppFromRegistry (key, defaultApp, uniqueAppsSet, AssociationFlags.None, AppOpenWithRegistryType.FromValue))
					yield return app;

			// Query Explorer OpenWithList.
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey (assocBaseKey + extension + @"\OpenWithList"))
				foreach (var app in GetAppFromRegistry (key, defaultApp, uniqueAppsSet, AssociationFlags.OpenByExeName, AppOpenWithRegistryType.FromMRUList))
					yield return app;

			// Query extension OpenWithList.
			using (RegistryKey key = Registry.ClassesRoot.OpenSubKey (extension + @"\OpenWithList"))
				foreach (var app in GetAppFromRegistry (key, defaultApp, uniqueAppsSet, AssociationFlags.OpenByExeName, AppOpenWithRegistryType.FromSubkey))
					yield return app;
		}

		static WindowsDesktopApplication WindowsAppFromName (string appName, string defaultApp, AssociationFlags flags)
		{
			try {
				string displayName = QueryAssociationString (appName, AssociationString.FriendlyAppName, flags, "open");
				string exePath = QueryAssociationString (appName, AssociationString.Executable, flags, "open");
				if (System.Reflection.Assembly.GetEntryAssembly ().Location != exePath)
					return new WindowsDesktopApplication (appName, displayName, exePath, appName.Equals (defaultApp));
			} catch (Exception ex) {
				LoggingService.LogError (string.Format ("Failed to read info for {0} '{1}'", flags == AssociationFlags.None ? "ProgId" : "ExeName", appName), ex);
			}
			return null;
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
				foreach (string file in files)
					Process.Start (ExePath, ProcessArgumentBuilder.Quote (file));
			}
		}
	}
}
