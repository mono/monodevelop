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

namespace MonoDevelop.MacIntegration
{
	class MacPlatformService : PlatformService
	{
		static TimerCounter timer = InstrumentationService.CreateTimerCounter ("Mac Platform Initialization", "Platform Service");
		static TimerCounter mimeTimer = InstrumentationService.CreateTimerCounter ("Mac Mime Database", "Platform Service");
		
		static bool setupFail, initedApp, initedGlobal;
		
		static Dictionary<string, string> mimemap;
		
		//this is a BCD value of the form "xxyz", where x = major, y = minor, z = bugfix
		//eg. 0x1071 = 10.7.1
		static int systemVersion;

		static MacPlatformService ()
		{
			timer.BeginTiming ();
			
			systemVersion = Carbon.Gestalt ("sysv");
			
			LoadMimeMapAsync ();
			
			CheckGtkVersion (2, 24, 0);
			
			//make sure the menu app name is correct even when running Mono 2.6 preview, or not running from the .app
			Carbon.SetProcessName (BrandingService.ApplicationName);
			
			MonoDevelop.MacInterop.Cocoa.InitMonoMac ();
			
			timer.Trace ("Installing App Event Handlers");
			GlobalSetup ();
			
			timer.EndTiming ();
		}
		
		//Mac GTK+ behaviour isn't completely stable even between micro releases
		static void CheckGtkVersion (uint major, uint minor, uint micro)
		{
			string url = "http://www.go-mono.com/mono-downloads/download.html";
			
			// to require exact version, also check : || Gtk.Global.CheckVersion (major, minor, micro + 1) == null
			if (Gtk.Global.CheckVersion (major, minor, micro) != null) {
				
				MonoDevelop.Core.LoggingService.LogFatalError ("GTK+ version is incompatible with required version {0}.{1}.{2}.", major, minor, micro);
				
				AlertButton downloadButton = new AlertButton ("Download...", null);
				if (downloadButton == MessageService.GenericAlert (
					Stock.Error,
					"Incompatible Mono Framework Version",
					"MonoDevelop requires a newer version of the Mono Framework.",
					new AlertButton ("Cancel", null), downloadButton))
				{
					OpenUrl (url);
				}
				
				Environment.Exit (1);
			}
		}

		protected override string OnGetMimeTypeForUri (string uri)
		{
			var ext = System.IO.Path.GetExtension (uri);
			if (mimemap != null && mimemap.ContainsKey (ext))
				return mimemap [ext];
			return null;
		}

		public override void ShowUrl (string url)
		{
			OpenUrl (url);
		}
		
		internal static void OpenUrl (string url)
		{
			NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl (url));
		}
		
		public override void OpenFile (string filename)
		{
			NSWorkspace.SharedWorkspace.OpenFile (filename);
		}

		public override string DefaultMonospaceFont {
			get { return "Monaco 12"; }
		}
		
		public override string Name {
			get { return "OSX"; }
		}
		
		private static void LoadMimeMapAsync ()
		{
			// All recent Macs should have this file; if not we'll just die silently
			if (!File.Exists ("/etc/apache2/mime.types")) {
				MonoDevelop.Core.LoggingService.LogError ("Apache mime database is missing");
				return;
			}
			
			System.Threading.ThreadPool.QueueUserWorkItem (delegate {
				mimeTimer.BeginTiming ();
				try {
					var map = new Dictionary<string, string> ();
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
					mimemap = map;
				} catch (Exception ex){
					MonoDevelop.Core.LoggingService.LogError ("Could not load Apache mime database", ex);
				}
				mimeTimer.EndTiming ();
			});
		}
		
		HashSet<object> ignoreCommands = new HashSet<object> () {
			CommandManager.ToCommandId (HelpCommands.About),
			CommandManager.ToCommandId (EditCommands.DefaultPolicies),
			CommandManager.ToCommandId (EditCommands.MonodevelopPreferences),
			CommandManager.ToCommandId (ToolCommands.AddinManager),
			CommandManager.ToCommandId (FileCommands.Exit),
		};
		
		public override bool SetGlobalMenu (CommandManager commandManager, string commandMenuAddinPath)
		{
			if (setupFail)
				return false;
			
			try {
				InitApp (commandManager);
				CommandEntrySet ces = commandManager.CreateCommandEntrySet (commandMenuAddinPath);
				MacMainMenu.Recreate (commandManager, ces, ignoreCommands);
			} catch (Exception ex) {
				try {
					MacMainMenu.Destroy (true);
				} catch {}
				MonoDevelop.Core.LoggingService.LogError ("Could not install global menu", ex);
				setupFail = true;
				return false;
			}
			
			return true;
		}
		
		static void InitApp (CommandManager commandManager)
		{
			if (initedApp)
				return;
			
			MacMainMenu.AddCommandIDMappings (new Dictionary<object, CarbonCommandID> ()
			{
				{ CommandManager.ToCommandId (EditCommands.Copy), CarbonCommandID.Copy },
				{ CommandManager.ToCommandId (EditCommands.Cut), CarbonCommandID.Cut },
				//FIXME: for some reason mapping this causes two menu items to be created
				// { EditCommands.MonodevelopPreferences, CarbonCommandID.Preferences }, 
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
			});
			
			//mac-ify these command names
			commandManager.GetCommand (EditCommands.MonodevelopPreferences).Text = GettextCatalog.GetString ("Preferences...");
			commandManager.GetCommand (EditCommands.DefaultPolicies).Text = GettextCatalog.GetString ("Custom Policies...");
			commandManager.GetCommand (HelpCommands.About).Text = string.Format (GettextCatalog.GetString ("About {0}"), BrandingService.ApplicationName);
			commandManager.GetCommand (ToolCommands.AddinManager).Text = GettextCatalog.GetString ("Add-in Manager...");
			
			initedApp = true;
			MacMainMenu.SetAppQuitCommand (CommandManager.ToCommandId (FileCommands.Exit));
			MacMainMenu.AddAppMenuItems (
				commandManager,
			    CommandManager.ToCommandId (HelpCommands.About),
				CommandManager.ToCommandId (MonoDevelop.Ide.Updater.UpdateCommands.CheckForUpdates),
				CommandManager.ToCommandId (Command.Separator),
				CommandManager.ToCommandId (EditCommands.MonodevelopPreferences),
				CommandManager.ToCommandId (EditCommands.DefaultPolicies),
				CommandManager.ToCommandId (ToolCommands.AddinManager));
			
			IdeApp.Workbench.RootWindow.DeleteEvent += HandleDeleteEvent;
		}
		
		static void GlobalSetup ()
		{
			if (initedGlobal || setupFail)
				return;
			initedGlobal = true;
			
			//FIXME: should we remove these when finalizing?
			try {
				ApplicationEvents.Quit += delegate (object sender, ApplicationQuitEventArgs e) {
					//FIXME: can we avoid replying to the message until the app quits?
					//There's NSTerminateLate but I'm not sure how to access it from carbon, maybe
					//we need to swizzle methods into the app's NSApplicationDelegate.
					//Also, it stops the main CFRunLoop, hopefully GTK dialogs use a child runloop.
					//For now, just bounce.
					var topDialog = MessageService.GetDefaultModalParent () as Gtk.Dialog;
					if (topDialog != null && topDialog.Modal) {
						NSApplication.SharedApplication.RequestUserAttention (
							NSRequestUserAttentionType.CriticalRequest);
					}
					//FIXME: delay this until all existing modal dialogs were closed
					if (!IdeApp.Exit ())
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
				if (!exePath.ToString ().Contains ("MonoDevelop.app")) {
					var mdSrcMain = exePath.ParentDirectory.ParentDirectory.ParentDirectory;
					var icons = mdSrcMain.Combine ("theme-icons", "Mac", "monodevelop.icns");
					if (File.Exists (icons))
						NSApplication.SharedApplication.ApplicationIconImage = new NSImage (icons);
				}
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Could not install app event handlers", ex);
				setupFail = true;
			}
		}
		
		[GLib.ConnectBefore]
		static void HandleDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			args.RetVal = true;
			IdeApp.Workbench.RootWindow.Hide ();
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
			
			var arep = icon.BestRepresentation (rect, null, null);
			if (arep == null) {
				return base.OnGetPixbufForFile (filename, size);
			}
			
			var rep = arep as NSBitmapImageRep;
			if (rep == null) {
				using (var cgi = arep.AsCGImage (rect, null, null))
					rep = new NSBitmapImageRep (cgi);
				arep.Dispose ();
			}
			
			try {
				byte[] arr;
				using (var tiff = rep.TiffRepresentation) {
					arr = new byte[tiff.Length];
					System.Runtime.InteropServices.Marshal.Copy (tiff.Bytes, arr, 0, arr.Length);
				}
				int pw = rep.PixelsWide, ph = rep.PixelsHigh;
				var px = new Gdk.Pixbuf (arr, pw, ph);
				
				//if one dimension matches, and the other is same or smaller, use as-is
				if ((pw == w && ph <= h) || (ph == h && pw <= w))
					return px;
				
				//else scale proportionally such that the largest dimension matches the desired size
				if (pw == ph) {
					pw = w;
					ph = h;
				} else if (pw > ph) {
					ph = (int) (w * ((float) ph / pw));
					pw = w;
				} else {
					pw = (int) (h * ((float) pw / ph));
					ph = h;
				}
				
				var scaled = px.ScaleSimple (pw, ph, Gdk.InterpType.Bilinear);
				px.Dispose ();
				return scaled;
			} finally {
				if (rep != null)
					rep.Dispose ();
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
			Gdk.Rectangle geometry = screen.GetMonitorGeometry (0);
			NSScreen monitor = NSScreen.Screens[monitor_id];
			RectangleF visible = monitor.VisibleFrame;
			RectangleF frame = monitor.Frame;
			
			// Note: Frame and VisibleFrame rectangles are relative to monitor 0, but we need absolute
			// coordinates.
			visible.X += geometry.X;
			visible.Y += geometry.Y;
			frame.X += geometry.X;
			frame.Y += geometry.Y;
			
			// VisibleFrame.Y is the height of the Dock if it is at the bottom of the screen, so in order
			// to get the menu height, we just figure out the difference between the visibleFrame height
			// and the actual frame height, then subtract the Dock height.
			//
			// We need to swap the Y offset with the menu height because our callers expect the Y offset
			// to be from the top of the screen, not from the bottom of the screen.
			float x, y, width, height;
			
			if (visible.Height <= frame.Height) {
				float dockHeight = visible.Y;
				float menubarHeight = (frame.Height - visible.Height) - dockHeight;
				
				height = frame.Height - menubarHeight - dockHeight;
				y = menubarHeight;
			} else {
				height = frame.Height;
				y = frame.Y;
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
	}
}
