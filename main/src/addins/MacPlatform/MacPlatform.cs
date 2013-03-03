//
// MacPlatformService.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2007-2011 Novell, Inc (http://www.novell.com)
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
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using MonoMac.AppKit;
using MonoMac.Foundation;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide; 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.MacInterop;
using MonoDevelop.Components.MainToolbar;

namespace MonoDevelop.MacIntegration
{
	public class MacPlatformService : PlatformService
	{
		const string monoDownloadUrl = "http://www.go-mono.com/mono-downloads/download.html";

		TimerCounter timer = InstrumentationService.CreateTimerCounter ("Mac Platform Initialization", "Platform Service");
		TimerCounter mimeTimer = InstrumentationService.CreateTimerCounter ("Mac Mime Database", "Platform Service");

		static bool initedGlobal;
		bool setupFail, initedApp;
		
		Lazy<Dictionary<string, string>> mimemap;
		
		//this is a BCD value of the form "xxyz", where x = major, y = minor, z = bugfix
		//eg. 0x1071 = 10.7.1
		int systemVersion;

		public MacPlatformService ()
		{
			if (initedGlobal)
				throw new Exception ("Only one MacPlatformService instance allowed");
			initedGlobal = true;

			timer.BeginTiming ();
			
			systemVersion = Carbon.Gestalt ("sysv");
			
			mimemap = new Lazy<Dictionary<string, string>> (LoadMimeMapAsync);

			//make sure the menu app name is correct even when running Mono 2.6 preview, or not running from the .app
			Carbon.SetProcessName (BrandingService.ApplicationName);
			
			Cocoa.InitMonoMac ();

			CheckGtkVersion (2, 24, 14);

			timer.Trace ("Installing App Event Handlers");
			GlobalSetup ();
			
			timer.EndTiming ();
		}

		static void CheckGtkVersion (uint major, uint minor, uint micro)
		{
			// to require exact version, also check
			//: || Gtk.Global.CheckVersion (major, minor, micro + 1) == null
			//
			if (Gtk.Global.CheckVersion (major, minor, micro) != null) {
				
				LoggingService.LogFatalError (
					"GTK+ version is incompatible with required version {0}.{1}.{2}.",
					major, minor, micro
				);
				
				var downloadButton = new AlertButton ("Download Mono Framework", null);
				if (downloadButton == MessageService.GenericAlert (
					Stock.Error,
					GettextCatalog.GetString ("Some dependencies need to be updated"),
					GettextCatalog.GetString (
						"{0} requires a newer version of GTK+, which is included with the Mono Framework. Please " +
						"download and install the latest stable Mono Framework package and restart {0}.",
						BrandingService.ApplicationName
					),
					new AlertButton ("Quit", null), downloadButton))
				{
					OpenUrl (monoDownloadUrl);
				}
				
				Environment.Exit (1);
			}
		}

		protected override string OnGetMimeTypeForUri (string uri)
		{
			var ext = Path.GetExtension (uri);
			string mime = null;
			if (ext != null && mimemap.Value.TryGetValue (ext, out mime))
				return mime;
			return null;
		}

		public override void ShowUrl (string url)
		{
			OpenUrl (url);
		}
		
		internal static void OpenUrl (string url)
		{
			Gtk.Application.Invoke (delegate {
				NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl (url));
			});
		}
		
		public override void OpenFile (string filename)
		{
			Gtk.Application.Invoke (delegate {
				NSWorkspace.SharedWorkspace.OpenFile (filename);
			});
		}

		public override string DefaultMonospaceFont {
			get { return "Menlo 12"; }
		}
		
		public override string Name {
			get { return "OSX"; }
		}
		
		Dictionary<string, string> LoadMimeMapAsync ()
		{
			var map = new Dictionary<string, string> ();
			// All recent Macs should have this file; if not we'll just die silently
			if (!File.Exists ("/etc/apache2/mime.types")) {
				LoggingService.LogError ("Apache mime database is missing");
				return map;
			}
			
			mimeTimer.BeginTiming ();
			try {
				using (var file = File.OpenRead ("/etc/apache2/mime.types")) {
					using (var reader = new StreamReader (file)) {
						var mime = new Regex ("([a-zA-Z]+/[a-zA-z0-9+-_.]+)\t+([a-zA-Z]+)", RegexOptions.Compiled);
						string line;
						while ((line = reader.ReadLine ()) != null) {
							Match m = mime.Match (line);
							if (m.Success)
								map ["." + m.Groups [2].Captures [0].Value] = m.Groups [1].Captures [0].Value; 
						}
					}
				}
			} catch (Exception ex){
				LoggingService.LogError ("Could not load Apache mime database", ex);
			}
			mimeTimer.EndTiming ();
			return map;
		}
		
		public override bool SetGlobalMenu (CommandManager commandManager, string commandMenuAddinPath, string appMenuAddinPath)
		{
			if (setupFail)
				return false;
			
			try {
				InitApp (commandManager);
				CommandEntrySet ces = commandManager.CreateCommandEntrySet (commandMenuAddinPath);
				MacMainMenu.Recreate (commandManager, ces);

				CommandEntrySet aes = commandManager.CreateCommandEntrySet (appMenuAddinPath);
				MacMainMenu.SetAppMenuItems (commandManager, aes);
			} catch (Exception ex) {
				try {
					MacMainMenu.Destroy (true);
				} catch {}
				LoggingService.LogError ("Could not install global menu", ex);
				setupFail = true;
				return false;
			}
			
			return true;
		}
		
		void InitApp (CommandManager commandManager)
		{
			if (initedApp)
				return;

			MacMainMenu.SetAppQuitCommand (CommandManager.ToCommandId (FileCommands.Exit));
			
			MacMainMenu.AddCommandIDMappings (new Dictionary<object, CarbonCommandID> ()
			{
				{ CommandManager.ToCommandId (EditCommands.Copy), CarbonCommandID.Copy },
				{ CommandManager.ToCommandId (EditCommands.Cut), CarbonCommandID.Cut },
				{ CommandManager.ToCommandId (EditCommands.MonodevelopPreferences), CarbonCommandID.Preferences }, 
				{ CommandManager.ToCommandId (EditCommands.Redo), CarbonCommandID.Redo },
				{ CommandManager.ToCommandId (EditCommands.Undo), CarbonCommandID.Undo },
				{ CommandManager.ToCommandId (EditCommands.SelectAll), CarbonCommandID.SelectAll },
				{ CommandManager.ToCommandId (FileCommands.NewFile), CarbonCommandID.New },
				{ CommandManager.ToCommandId (FileCommands.OpenFile), CarbonCommandID.Open },
				{ CommandManager.ToCommandId (FileCommands.Save), CarbonCommandID.Save },
				{ CommandManager.ToCommandId (FileCommands.SaveAs), CarbonCommandID.SaveAs },
				{ CommandManager.ToCommandId (FileCommands.CloseFile), CarbonCommandID.Close },
				{ CommandManager.ToCommandId (FileCommands.Exit), CarbonCommandID.Quit },
				{ CommandManager.ToCommandId (FileCommands.ReloadFile), CarbonCommandID.Revert },
				{ CommandManager.ToCommandId (HelpCommands.About), CarbonCommandID.About },
				{ CommandManager.ToCommandId (HelpCommands.Help), CarbonCommandID.AppHelp },
				{ CommandManager.ToCommandId (MacIntegrationCommands.HideWindow), CarbonCommandID.Hide },
				{ CommandManager.ToCommandId (MacIntegrationCommands.HideOthers), CarbonCommandID.HideOthers },
			});
			
			//mac-ify these command names
			commandManager.GetCommand (EditCommands.MonodevelopPreferences).Text = GettextCatalog.GetString ("Preferences...");
			commandManager.GetCommand (EditCommands.DefaultPolicies).Text = GettextCatalog.GetString ("Custom Policies...");
			commandManager.GetCommand (HelpCommands.About).Text = GettextCatalog.GetString ("About {0}", BrandingService.ApplicationName);
			commandManager.GetCommand (MacIntegrationCommands.HideWindow).Text = GettextCatalog.GetString ("Hide {0}", BrandingService.ApplicationName);
			commandManager.GetCommand (ToolCommands.AddinManager).Text = GettextCatalog.GetString ("Add-in Manager...");
			
			initedApp = true;
			
			IdeApp.Workbench.RootWindow.DeleteEvent += HandleDeleteEvent;

			if (MacSystemInformation.OsVersion >= MacSystemInformation.Lion) {
				IdeApp.Workbench.RootWindow.Realized += (sender, args) => {
					var win = GtkQuartz.GetWindow ((Gtk.Window) sender);
					win.CollectionBehavior |= NSWindowCollectionBehavior.FullScreenPrimary;
				};
			}
		}

		void GlobalSetup ()
		{
			//FIXME: should we remove these when finalizing?
			try {
				ApplicationEvents.Quit += delegate (object sender, ApplicationQuitEventArgs e)
				{
					// We can only attempt to quit safely if all windows are GTK windows and not modal
					if (GtkQuartz.GetToplevels ().All (t => t.Value != null && (!t.Value.Visible || !t.Value.Modal))) {
						e.UserCancelled = !IdeApp.Exit ();
						e.Handled = true;
						return;
					}

					// When a modal dialog is running, things are much harder. We can't just shut down MD behind the
					// dialog, and aborting the dialog may not be appropriate.
					//
					// There's NSTerminateLater but I'm not sure how to access it from carbon, maybe
					// we need to swizzle methods into the app's NSApplicationDelegate.
					// Also, it stops the main CFRunLoop and enters a special runloop mode, not sure how that would
					// interact with GTK+.

					// For now, just bounce
					NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
					// and abort the quit.
					e.UserCancelled = true;
					e.Handled = true;
				};
				
				ApplicationEvents.Reopen += delegate (object sender, ApplicationEventArgs e) {
					if (IdeApp.Workbench != null && IdeApp.Workbench.RootWindow != null) {
						IdeApp.Workbench.RootWindow.Deiconify ();

						// This is a workaround to a GTK+ bug. The HasTopLevelFocus flag is not properly
						// set when the main window is restored. The workaround is to hide and re-show it.
						// Since this happens before the next mainloop cycle, the window isn't actually affected.
						IdeApp.Workbench.RootWindow.Hide ();
						IdeApp.Workbench.RootWindow.Show ();

						IdeApp.Workbench.RootWindow.Present ();
						e.Handled = true;
					}
				};
				
				ApplicationEvents.OpenDocuments += delegate (object sender, ApplicationDocumentEventArgs e) {
					//OpenFiles may pump the mainloop, but can't do that from an AppleEvent, so use a brief timeout
					GLib.Timeout.Add (10, delegate {
						IdeApp.OpenFiles (e.Documents.Select (doc =>
							new FileOpenInformation (doc.Key, doc.Value, 1, OpenDocumentOptions.Default)));
						return false;
					});
					e.Handled = true;
				};
				
				//if not running inside an app bundle, assume usual MD build layout and load the app icon
				FilePath exePath = System.Reflection.Assembly.GetExecutingAssembly ().Location;
				string iconFile = null;

				iconFile = BrandingService.GetString ("ApplicationIcon");
				if (iconFile != null) {
					iconFile = BrandingService.GetFile (iconFile);
				}
				else if (!exePath.ToString ().Contains ("MonoDevelop.app")) {
						var mdSrcMain = exePath.ParentDirectory.ParentDirectory.ParentDirectory;
						iconFile = mdSrcMain.Combine ("theme-icons", "Mac", "monodevelop.icns");
				} else {
					//HACK: override the app image
					//NSApplication doesn't seem to pick up the image correctly, probably due to the
					//getting confused about the bundle root because of the launch script
					var bundleContents = exePath.ParentDirectory.ParentDirectory.ParentDirectory
						.ParentDirectory.ParentDirectory;
					iconFile = bundleContents.Combine ("Resources", "monodevelop.icns");
				}
				if (File.Exists (iconFile)) {
					NSApplication.SharedApplication.ApplicationIconImage = new NSImage (iconFile);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Could not install app event handlers", ex);
				setupFail = true;
			}
		}
		
		[GLib.ConnectBefore]
		static void HandleDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			args.RetVal = true;
			MacHideOthersHandler.RunMenuCommand (CarbonCommandID.Hide);
		}

		public static Gdk.Pixbuf GetPixbufFromNSImageRep (NSImageRep rep, int width, int height)
		{
			var rect = new RectangleF (0, 0, width, height);
			var bitmap = rep as NSBitmapImageRep;
			
			if (bitmap == null) {
				using (var cgi = rep.AsCGImage (ref rect, null, null))
					bitmap = new NSBitmapImageRep (cgi);
			}
			
			try {
				byte[] data;
				using (var tiff = bitmap.TiffRepresentation) {
					data = new byte[tiff.Length];
					System.Runtime.InteropServices.Marshal.Copy (tiff.Bytes, data, 0, data.Length);
				}
				
				int pw = bitmap.PixelsWide, ph = bitmap.PixelsHigh;
				var pixbuf = new Gdk.Pixbuf (data, pw, ph);
				
				// if one dimension matches, and the other is same or smaller, use as-is
				if ((pw == width && ph <= height) || (ph == height && pw <= width))
					return pixbuf;
				
				// otherwise scale proportionally such that the largest dimension matches the desired size
				if (pw == ph) {
					pw = width;
					ph = height;
				} else if (pw > ph) {
					ph = (int) (width * ((float) ph / pw));
					pw = width;
				} else {
					pw = (int) (height * ((float) pw / ph));
					ph = height;
				}
				
				var scaled = pixbuf.ScaleSimple (pw, ph, Gdk.InterpType.Bilinear);
				pixbuf.Dispose ();
				
				return scaled;
			} finally {
				if (bitmap != rep)
					bitmap.Dispose ();
			}
		}
		
		protected override Gdk.Pixbuf OnGetPixbufForFile (string filename, Gtk.IconSize size)
		{
			//this only works on MacOS 10.6.0 and greater
			if (systemVersion < 0x1060)
				return base.OnGetPixbufForFile (filename, size);
			
			NSImage icon = null;
			
			if (Path.IsPathRooted (filename) && File.Exists (filename)) {
				icon = NSWorkspace.SharedWorkspace.IconForFile (filename);
			} else {
				string extension = Path.GetExtension (filename);
				if (!string.IsNullOrEmpty (extension))
					icon = NSWorkspace.SharedWorkspace.IconForFileType (extension);
			}
			
			if (icon == null) {
				return base.OnGetPixbufForFile (filename, size);
			}
			
			int w, h;
			if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out w, out h)) {
				w = h = 22;
			}
			
			var rect = new System.Drawing.RectangleF (0, 0, w, h);
			
			using (var rep = icon.BestRepresentation (rect, null, null)) {
				if (rep == null)
					return base.OnGetPixbufForFile (filename, size);
				
				return GetPixbufFromNSImageRep (rep, w, h);
			}
		}
		
		public override IProcessAsyncOperation StartConsoleProcess (string command, string arguments, string workingDirectory,
		                                                            IDictionary<string, string> environmentVariables,
		                                                            string title, bool pauseWhenFinished)
		{
			return new MacExternalConsoleProcess (command, arguments, workingDirectory, environmentVariables,
			                                   title, pauseWhenFinished);
		}
		
		public override bool CanOpenTerminal {
			get {
				return true;
			}
		}
		
		public override void OpenInTerminal (FilePath directory)
		{
			AppleScript.Run (string.Format (
@"tell application ""Terminal""
activate
do script with command ""cd {0}""
end tell", directory.ToString ().Replace ("\"", "\\\"")));
		}
		
		public override IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			//FIXME: we should disambiguate dupliacte apps in different locations and display both
			//for now, just filter out the duplicates
			var checkUniqueName = new HashSet<string> ();
			var checkUniquePath = new HashSet<string> ();
			
			//FIXME: bundle path is wrong because of how MD is built into an app
			//var thisPath = NSBundle.MainBundle.BundleUrl.Path;
			//checkUniquePath.Add (thisPath);
			
			checkUniqueName.Add ("MonoDevelop");
			checkUniqueName.Add (BrandingService.ApplicationName);
			
			string def = CoreFoundation.GetApplicationUrl (filename, CoreFoundation.LSRolesMask.All);
			
			var apps = new List<DesktopApplication> ();
			
			foreach (var app in CoreFoundation.GetApplicationUrls (filename, CoreFoundation.LSRolesMask.All)) {
				if (string.IsNullOrEmpty (app) || !checkUniquePath.Add (app))
					continue;
				var name = NSFileManager.DefaultManager.DisplayName (app);
				if (checkUniqueName.Add (name))
					apps.Add (new MacDesktopApplication (app, name, def != null && def == app));
			}
			
			apps.Sort ((DesktopApplication a, DesktopApplication b) => {
				int r = a.IsDefault.CompareTo (b.IsDefault);
				if (r != 0)
					return -r;
				return a.DisplayName.CompareTo (b.DisplayName);
			});
			
			return apps;
		}
		
		class MacDesktopApplication : DesktopApplication
		{
			public MacDesktopApplication (string app, string name, bool isDefault) : base (app, name, isDefault)
			{
			}
			
			public override void Launch (params string[] files)
			{
				foreach (var file in files)
					NSWorkspace.SharedWorkspace.OpenFile (file, Id);
			}
		}
		
		public override Gdk.Rectangle GetUsableMonitorGeometry (Gdk.Screen screen, int monitor_id)
		{
			Gdk.Rectangle ygeometry = screen.GetMonitorGeometry (monitor_id);
			Gdk.Rectangle xgeometry = screen.GetMonitorGeometry (0);
			NSScreen monitor = NSScreen.Screens[monitor_id];
			RectangleF visible = monitor.VisibleFrame;
			RectangleF frame = monitor.Frame;
			
			// Note: Frame and VisibleFrame rectangles are relative to monitor 0, but we need absolute
			// coordinates.
			visible.X += xgeometry.X;
			frame.X += xgeometry.X;
			
			// VisibleFrame.Y is the height of the Dock if it is at the bottom of the screen, so in order
			// to get the menu height, we just figure out the difference between the visibleFrame height
			// and the actual frame height, then subtract the Dock height.
			//
			// We need to swap the Y offset with the menu height because our callers expect the Y offset
			// to be from the top of the screen, not from the bottom of the screen.
			float x, y, width, height;
			
			if (visible.Height <= frame.Height) {
				float dockHeight = visible.Y - frame.Y;
				float menubarHeight = (frame.Height - visible.Height) - dockHeight;
				
				height = frame.Height - menubarHeight - dockHeight;
				y = ygeometry.Y + menubarHeight;
			} else {
				height = frame.Height;
				y = ygeometry.Y;
			}
			
			// Takes care of the possibility of the Dock being positioned on the left or right edge of the screen.
			width = Math.Min (visible.Width, frame.Width);
			x = Math.Max (visible.X, frame.X);
			
			return new Gdk.Rectangle ((int) x, (int) y, (int) width, (int) height);
		}
		
		public override void GrabDesktopFocus (Gtk.Window window)
		{
			window.Present ();
			NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
		}

		static Cairo.Color ConvertColor (NSColor color)
		{
			float r, g, b, a;
			if (color.ColorSpaceName == NSColorSpace.DeviceWhite) {
				a = 1.0f;
				r = g = b = color.WhiteComponent;
			} else {
				color.GetRgba (out r, out g, out b, out a);
			}
			return new Cairo.Color (r, g, b, a);
		}

		static int GetTitleBarHeight ()
		{
			var frame = new RectangleF (0, 0, 100, 100);
			var rect = NSWindow.ContentRectFor (frame, NSWindowStyle.Titled);
			return (int)(frame.Height - rect.Height);
		}


		static NSImage LoadImage (string resource)
		{
			byte[] buffer;
			using (var stream = typeof (MacPlatformService).Assembly.GetManifestResourceStream (resource)) {
				buffer = new byte [stream.Length];
				stream.Read (buffer, 0, (int)stream.Length);
			}

			// Workaround: loading from file name.
			var tmp = Path.GetTempFileName ();
			File.WriteAllBytes (tmp, buffer);
			var img = new NSImage (tmp);
			File.Delete (tmp);
			return img;
		}

		internal override void SetMainWindowDecorations (Gtk.Window window)
		{
			NSWindow w = GtkQuartz.GetWindow (window);
			w.IsOpaque = false;
			
			var resource = "maintoolbarbg.png";
			NSImage img = LoadImage (resource);
			w.BackgroundColor = NSColor.FromPatternImage (img);
			w.StyleMask |= NSWindowStyle.TexturedBackground;
		}

		internal override void RemoveWindowShadow (Gtk.Window window)
		{
			if (window == null)
				throw new ArgumentNullException ("window");
			NSWindow w = GtkQuartz.GetWindow (window);
			w.HasShadow = false;
		}

		internal override MainToolbar CreateMainToolbar (Gtk.Window window)
		{
			NSApplication.Init ();
			
			NSWindow w = GtkQuartz.GetWindow (window);
			w.IsOpaque = false;
			
			var resource = "maintoolbarbg.png";
			NSImage img = LoadImage (resource);
			var c = NSColor.FromPatternImage (img);
			w.BackgroundColor = c;
			w.StyleMask |= NSWindowStyle.TexturedBackground;

			var result = new MainToolbar () {
				Background = MonoDevelop.Components.CairoExtensions.LoadImage (typeof (MacPlatformService).Assembly, resource),
				TitleBarHeight = GetTitleBarHeight ()
			};
			return result;
		}

		protected override RecentFiles CreateRecentFilesProvider ()
		{
			return new FdoRecentFiles (UserProfile.Current.LocalConfigDir.Combine ("RecentlyUsed.xml"));
		}

		public override bool GetIsFullscreen (Gtk.Window window)
		{
			if (MacSystemInformation.OsVersion < MacSystemInformation.Lion) {
				return base.GetIsFullscreen (window);
			}

			NSWindow nswin = GtkQuartz.GetWindow (window);
			return (nswin.StyleMask & NSWindowStyle.FullScreenWindow) != 0;
		}

		public override void SetIsFullscreen (Gtk.Window window, bool isFullscreen)
		{
			if (MacSystemInformation.OsVersion < MacSystemInformation.Lion) {
				base.SetIsFullscreen (window, isFullscreen);
				return;
			}

			NSWindow nswin = GtkQuartz.GetWindow (window);
			if (isFullscreen != ((nswin.StyleMask & NSWindowStyle.FullScreenWindow) != 0)) {
				//HACK: workaround for MonoMac not allowing null as argument
				MonoMac.ObjCRuntime.Messaging.void_objc_msgSend_IntPtr (
					nswin.Handle,
					MonoMac.ObjCRuntime.Selector.GetHandle ("toggleFullScreen:"),
					IntPtr.Zero);
			}
		}
	}
}
