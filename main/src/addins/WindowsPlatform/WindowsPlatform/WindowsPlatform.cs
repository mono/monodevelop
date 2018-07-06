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
using Microsoft.WindowsAPICodePack.InternetExplorer;
using Microsoft.WindowsAPICodePack.Taskbar;
using MonoDevelop.Ide;
using MonoDevelop.Components.Windows;
using WindowsPlatform.MainToolbar;
using MonoDevelop.Components.Commands;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

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

		#region Toolbar implementation
		Components.Commands.CommandManager commandManager;
		string commandMenuAddinPath;
		string appMenuAddinPath;
		public override bool SetGlobalMenu (Components.Commands.CommandManager commandManager, string commandMenuAddinPath, string appMenuAddinPath)
		{
			// Only store this information. Release it when creating the main toolbar.
			this.commandManager = commandManager;
			this.commandMenuAddinPath = commandMenuAddinPath;
			this.appMenuAddinPath = appMenuAddinPath;

			return true;
		}

		const int WM_SYSCHAR = 0x0106;
        internal override void AttachMainToolbar (Gtk.VBox parent, Components.MainToolbar.IMainToolbarView toolbar)
		{
			titleBar = new TitleBar ();
			var topMenu = new WPFTitlebar (titleBar);

			//commandManager.IncompleteKeyPressed += (sender, e) => {
			//	if (e.Key == Gdk.Key.Alt_L) {
			//		Keyboard.Focus(titleBar.DockTitle.Children[0]);
			//	}
			//};
			parent.PackStart (topMenu, false, true, 0);
			SetupMenu ();

			parent.PackStart ((WPFToolbar)toolbar, false, true, 0);
		}

		void SetupMenu ()
		{
			// TODO: Use this?
			CommandEntrySet appCes = commandManager.CreateCommandEntrySet (appMenuAddinPath);

			CommandEntrySet ces = commandManager.CreateCommandEntrySet (commandMenuAddinPath);
			var mainMenu = new Menu {
				IsMainMenu = true,
				FocusVisualStyle = null,
			};
			foreach (CommandEntrySet ce in ces)
			{
				var item = new TitleMenuItem (commandManager, ce, menu: mainMenu);
				mainMenu.Items.Add(item);
			}

			titleBar.DockTitle.Children.Add (mainMenu);
			DockPanel.SetDock (mainMenu, Dock.Left);

			commandManager = null;
			commandMenuAddinPath = appMenuAddinPath = null;
		}

		TitleBar titleBar;
		internal override Components.MainToolbar.IMainToolbarView CreateMainToolbar (Gtk.Window window)
		{
			return new WPFToolbar {
				HeightRequest = 40,
			};
		}
		#endregion

		public override bool GetIsFullscreen (Components.Window window)
		{
			//the Fullscreen functionality is broken in GTK on Win7+
			//TODO: implement a workaround.
			return false;
		}

		public override void SetIsFullscreen (Components.Window window, bool isFullscreen)
		{
			//no-op as we have not yet implemented this
		}

		internal static Xwt.Toolkit WPFToolkit;

		public override void Initialize ()
		{
			// Only initialize elements for Win7+.
			if (TaskbarManager.IsPlatformSupported) {
				TaskbarManager.Instance.ApplicationId = BrandingService.ApplicationName;
			}
			// Set InternetExplorer emulation mode
			InternetExplorer.EmulationMode = IEEmulationMode.IE11;
		}

		public override Xwt.Toolkit LoadNativeToolkit ()
		{
			var path = Path.GetDirectoryName (GetType ().Assembly.Location);
			System.Reflection.Assembly.LoadFrom (Path.Combine (path, "Xwt.WPF.dll"));
			WPFToolkit = Xwt.Toolkit.Load (Xwt.ToolkitType.Wpf);

			WPFToolkit.RegisterBackend<Xwt.Backends.IDialogBackend, ThemedWpfDialogBackend> ();
			WPFToolkit.RegisterBackend<Xwt.Backends.IWindowBackend, ThemedWpfWindowBackend> ();

			return WPFToolkit;
		}

		internal override void SetMainWindowDecorations (Gtk.Window window)
		{
			Uri uri = new Uri ("pack://application:,,,/WindowsPlatform;component/Styles.xaml");
			Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });

			// Only initialize elements for Win7+.
			if (TaskbarManager.IsPlatformSupported) {
				TaskbarManager.Instance.SetApplicationIdForSpecificWindow (GdkWin32.HgdiobjGet (window.GdkWindow), BrandingService.ApplicationName);
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

		public override Xwt.Rectangle GetUsableMonitorGeometry (int screenNumber, int monitor_id)
		{
			var screen = Gdk.Display.Default.GetScreen (screenNumber);
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

			return new Xwt.Rectangle (x, y, width, height);
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
					sb.Append ("title ").Append (title).Append (" && ");
				sb.Append ("\"").Append (command).Append ("\" ").Append (arguments);
				if (pauseWhenFinished)
					sb.Append (" & pause");
				sb.Append ("\"");
			} else if (title != null) {
				sb.Append ("/K \"title ").Append (title).Append ("\"");
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

		public override ProcessAsyncOperation StartConsoleProcess (
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
			return proc.ProcessAsyncOperation;
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
			const uint E_NO_ASSOCIATION = 0x80070483;

			var builder = new StringBuilder (512);
			int size = builder.Length;

			var result = Win32.AssocQueryStringW (flags, str, assoc, extra, builder, ref size);

			if (result == unchecked((int)E_POINTER)) {
				builder.Length = size;
				result = Win32.AssocQueryStringW (flags, str, assoc, extra, builder, ref size);
			}

			if (result == unchecked((int)E_NO_ASSOCIATION)) {
				return null;
			}

			Marshal.ThrowExceptionForHR (result);
			return builder.ToString ();
		}

		public override IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			string extension = Path.GetExtension (filename);
			if (string.IsNullOrEmpty (extension))
				return new DesktopApplication[0];

			return GetAppsForExtension (extension);
		}

		static IEnumerable<RegistryKey> GetOpenWithProgidsKeys (string extension)
		{
			yield return Registry.CurrentUser.OpenSubKey (@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" + extension);
			yield return Registry.CurrentUser.OpenSubKey (@"Software\Classes\" + extension);
			yield return Registry.LocalMachine.OpenSubKey (@"Software\Classes\" + extension);
			yield return Registry.ClassesRoot.OpenSubKey (extension);
		}

		static IEnumerable<DesktopApplication> GetAppsForExtension (string extension)
		{
			var apps = new Dictionary<string,WindowsDesktopApplication> ();

			WindowsDesktopApplication defaultApp = null;

			//first check for the user's preferred app for this file type and use it as the default
			using (var key = Registry.CurrentUser.OpenSubKey (@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" + extension + @"\UserChoice")) {
				var progid = key?.GetValue ("ProgId") as string;
				if (progid != null)
					apps[progid] = defaultApp = WindowsAppFromName (progid, true, AssociationFlags.None);
			}

			//look in all the locatiosn where progids can be registered as handler for files
			//starting with local user and falling back to system
			foreach (var key in GetOpenWithProgidsKeys (extension)) {
				if (key == null)
					continue;
				using (key) {
					//if we didn't find a default app yet, check for one
					if (defaultApp == null) {
						var defaultProgid = key.GetValue ("") as string;
						if (defaultProgid != null)
							apps[defaultProgid] = defaultApp = WindowsAppFromName (defaultProgid, true, AssociationFlags.None);
					}
					using (var sk = key.OpenSubKey ("OpenWithProgids")) {
						if (sk == null)
							continue;
						foreach (var progid in sk.GetValueNames ()) {
							if (!apps.ContainsKey (progid))
								apps[progid] = WindowsAppFromName (progid, false, AssociationFlags.None);
						}
					}
				}
			}

			//return non-duplicate executables, giving precedence to the default
			var exePaths = new HashSet<string> ();

			if (defaultApp != null) {
				yield return defaultApp;
				exePaths.Add (defaultApp.ExePath);
			}

			foreach (var a in apps.Values) {
				if (a != null && exePaths.Add (a.ExePath)) {
					yield return a;
				}
			}
		}

		static WindowsDesktopApplication WindowsAppFromName (string appName, bool isDefault, AssociationFlags flags)
		{
			try {
				string displayName = QueryAssociationString (appName, AssociationString.FriendlyAppName, flags, "open");
				string exePath = QueryAssociationString (appName, AssociationString.Executable, flags, "open");
				//ignore apps with missing information, it's not worth logging
				if (exePath == null || displayName == null)
					return null;
				if (System.Reflection.Assembly.GetEntryAssembly ().Location != exePath)
					return new WindowsDesktopApplication (appName, displayName, exePath, isDefault);
			} catch (Exception ex) {
				LoggingService.LogError (string.Format ("Failed to read info for {0} '{1}'", flags == AssociationFlags.None ? "ProgId" : "ExeName", appName), ex);
			}
			return null;
		}

		#region OpenFolder

		[DllImport ("shell32.dll")]
		static extern int SHOpenFolderAndSelectItems (
			IntPtr pidlFolder,
			uint cidl,
			[In, MarshalAs (UnmanagedType.LPArray)] IntPtr[] apidl,
			uint dwFlags);

		[DllImport ("shell32.dll")]
		static extern IntPtr ILCreateFromPath ([MarshalAs (UnmanagedType.LPTStr)] string pszPath);

		[DllImport ("shell32.dll")]
		static extern void ILFree (IntPtr pidl);

		public override void OpenFolder (FilePath folderPath, FilePath[] selectFiles)
		{
			if (selectFiles.Length == 0) {
				Process.Start (folderPath);
			} else {
				var dir = ILCreateFromPath (folderPath);
				var files = selectFiles.Select ((f) => ILCreateFromPath (f)).ToArray ();
				try {
					SHOpenFolderAndSelectItems (dir, (uint)files.Length, files, 0);
				} finally {
					ILFree (dir);
					foreach (var file in files)
						ILFree (file);
				}
			}
		}

		#endregion

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

		static void ApplyTheme (System.Windows.Window window)
		{
			var color = System.Windows.Media.Color.FromArgb (
				(byte)(MonoDevelop.Ide.Gui.Styles.BackgroundColor.Alpha * 255.0),
				(byte)(MonoDevelop.Ide.Gui.Styles.BackgroundColor.Red * 255.0),
				(byte)(MonoDevelop.Ide.Gui.Styles.BackgroundColor.Green * 255.0),
				(byte)(MonoDevelop.Ide.Gui.Styles.BackgroundColor.Blue * 255.0));
			window.Background = new System.Windows.Media.SolidColorBrush (color);
		}

		public class ThemedWpfWindowBackend : Xwt.WPFBackend.WindowBackend
		{
			public override void Initialize ()
			{
				base.Initialize ();
				ApplyTheme (Window);
			}
		}

		public class ThemedWpfDialogBackend : Xwt.WPFBackend.DialogBackend
		{
			public override void Initialize ()
			{
				base.Initialize ();
				ApplyTheme (Window);
			}
		}
	}
}
