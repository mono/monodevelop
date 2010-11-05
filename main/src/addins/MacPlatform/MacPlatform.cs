//
// MacPlatform.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Ide.Gui;
using OSXIntegration.Framework;
using MonoDevelop.Ide; 
using System.Linq;
using MonoDevelop.Core.Execution;
using MonoDevelop.Platform.Mac;
using MonoDevelop.Core;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace MonoDevelop.Platform
{
	public class MacPlatform : PlatformService
	{
		static bool setupFail, initedApp, initedGlobal;
		
		static Dictionary<string, string> mimemap;

		static MacPlatform ()
		{
			//make sure the menu app name is correct even when running Mono 2.6 preview, or not running from the .app
			Carbon.SetProcessName ("MonoDevelop");
			
			MonoMac.AppKit.NSApplication.Init ();
			
			GlobalSetup ();
			mimemap = new Dictionary<string, string> ();
			LoadMimeMap ();
			
			CheckGtkVersion (2, 17, 9);
		}
		
		//Mac GTK+ is unstable, even between micro releases
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
			FileInfo file = new FileInfo (uri);
			
			if (mimemap.ContainsKey (file.Extension))
				return mimemap [file.Extension];

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
		
		private static void LoadMimeMap ()
		{
			LoggingService.Trace ("Mac Platform Service", "Loading MIME map");
			
			// All recent Macs should have this file; if not we'll just die silently
			if (!File.Exists ("/etc/apache2/mime.types")) {
				MonoDevelop.Core.LoggingService.LogError ("Apache mime database is missing");
				return;
			}
			
			try {
				StreamReader reader = new StreamReader (File.OpenRead ("/etc/apache2/mime.types"));
				Regex mime = new Regex ("([a-zA-Z]+/[a-zA-z0-9+-_.]+)\t+([a-zA-Z]+)", RegexOptions.Compiled);
				string line;
				while ((line = reader.ReadLine ()) != null) {
					Match m = mime.Match (line);
					if (m.Success)
						mimemap ["." + m.Groups [2].Captures [0].Value] = m.Groups [1].Captures [0].Value; 
				}
			} catch (Exception ex){
				MonoDevelop.Core.LoggingService.LogError ("Could not load Apache mime database", ex);
			}
			
			LoggingService.Trace ("Mac Platform Service", "Loaded MIME map");
		}
		
		HashSet<object> ignoreCommands = new HashSet<object> () {
			CommandManager.ToCommandId (HelpCommands.About),
			CommandManager.ToCommandId (EditCommands.DefaultPolicies),
			CommandManager.ToCommandId (EditCommands.MonodevelopPreferences),
			CommandManager.ToCommandId (FileCommands.Exit),
		};
		
		public override bool SetGlobalMenu (CommandManager commandManager, string commandMenuAddinPath)
		{
			if (setupFail)
				return false;
			
			try {
				InitApp (commandManager);
				CommandEntrySet ces = commandManager.CreateCommandEntrySet (commandMenuAddinPath);
				OSXIntegration.OSXMenu.Recreate (commandManager, ces, ignoreCommands);
			} catch (Exception ex) {
				try {
					OSXIntegration.OSXMenu.Destroy (true);
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
			
			OSXIntegration.OSXMenu.AddCommandIDMappings (new Dictionary<object, CarbonCommandID> ()
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
			commandManager.GetCommand (EditCommands.DefaultPolicies).Text = GettextCatalog.GetString ("Default Policies...");
			commandManager.GetCommand (HelpCommands.About).Text = GettextCatalog.GetString ("About MonoDevelop");
			
			initedApp = true;
			OSXIntegration.OSXMenu.SetAppQuitCommand (CommandManager.ToCommandId (FileCommands.Exit));
			OSXIntegration.OSXMenu.AddAppMenuItems (
				commandManager,
			    CommandManager.ToCommandId (HelpCommands.About),
				CommandManager.ToCommandId (Command.Separator),
				CommandManager.ToCommandId (EditCommands.DefaultPolicies),
				CommandManager.ToCommandId (EditCommands.MonodevelopPreferences));
			
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
					if (!IdeApp.Exit ())
						e.UserCancelled = true;
					e.Handled = true;
				};
				
				ApplicationEvents.Reopen += delegate (object sender, ApplicationEventArgs e) {
					if (IdeApp.Workbench != null && IdeApp.Workbench.RootWindow != null) {
						IdeApp.Workbench.RootWindow.Deiconify ();
						IdeApp.Workbench.RootWindow.Visible = true;
						e.Handled = true;
					}
				};
				
				ApplicationEvents.OpenDocuments += delegate (object sender, ApplicationDocumentEventArgs e) {
					//OpenFiles may pump the mainloop, but can't do that from an AppleEvent, so use a brief timeout
					GLib.Timeout.Add (10, delegate {
						IdeApp.OpenFiles (e.Documents.Select (doc => new FileOpenInformation (doc.Key, doc.Value, 1, true)));
						return false;
					});
					e.Handled = true;
				};
				
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Could not install app event handlers", ex);
				setupFail = true;
			}
		}
		
		[GLib.ConnectBefore]
		static void HandleDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			args.RetVal = true;
			IdeApp.Workbench.RootWindow.Visible = false;
		}
		
		protected override Gdk.Pixbuf OnGetPixbufForFile (string filename, Gtk.IconSize size)
		{
			NSImage icon = null;
			
			//FIXME: handle names of files that haven't been saved yet
			if (!Path.IsPathRooted (filename))
				icon = NSWorkspace.SharedWorkspace.IconForFile (filename);
			
			if (icon != null) {
				int w, h;
				if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out w, out h))
					w = h = 22;
				var rect = new System.Drawing.RectangleF (0, 0, w, h);
				var ctx = NSGraphicsContext.CurrentContext;
				var rep = icon.BestRepresentation (rect, ctx, new NSDictionary ()) as NSBitmapImageRep;
				if (rep != null) {
					var tiff = rep.TiffRepresentation;
					byte[] arr = new byte[tiff.Length];
					System.Runtime.InteropServices.Marshal.Copy (tiff.Bytes, arr, 0, arr.Length);
					return new Gdk.Pixbuf (arr, rep.PixelsWide, rep.PixelsHigh);
				}
			}
			return base.OnGetPixbufForFile (filename, size);
		}
		
		public override IProcessAsyncOperation StartConsoleProcess (string command, string arguments, string workingDirectory,
		                                                            IDictionary<string, string> environmentVariables,
		                                                            string title, bool pauseWhenFinished)
		{
			return new ExternalConsoleProcess (command, arguments, workingDirectory, environmentVariables,
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
		
		public override string GetUpdaterUrl ()
		{
			return "http://go-mono.com/macupdate/update";
		}
		
		public override IEnumerable<string> GetUpdaterEnviromentFlags ()
		{
			var sdkDir = "/Developer/Platforms/iPhoneSimulator.platform/Developer/SDKs";
			if (Directory.Exists (sdkDir)) {
				foreach (var dir in Directory.GetDirectories (sdkDir, "iPhoneSimulator*")) {
					var name = Path.GetFileNameWithoutExtension (dir);
					int len = "iPhoneSimulator".Length;
					if (name != null && name.Length > len) 
						yield return "iphsdk" + name.Substring (len);
				}
			}
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
	}
}
