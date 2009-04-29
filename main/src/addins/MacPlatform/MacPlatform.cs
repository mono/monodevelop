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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using OSXIntegration.Framework;

namespace MonoDevelop.Platform
{
	public class MacPlatform : PlatformService
	{
		static bool setupFail, initedApp, initedGlobal;
		
		static Dictionary<string, string> mimemap;

		static MacPlatform ()
		{
			GlobalSetup ();
			mimemap = new Dictionary<string, string> ();
			LoadMimeMap ();
			
			CheckGtkVersion (2, 14, 7);
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
					Process.Start (url);
				}
				
				Environment.Exit (1);
			}
		}

		public override DesktopApplication GetDefaultApplication (string mimetype) {
			return new DesktopApplication ();
		}
		
		public override DesktopApplication [] GetAllApplications (string mimetype) {
			return new DesktopApplication [] {new DesktopApplication ()};
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
			Process.Start (url);
		}

		public override string DefaultMonospaceFont {
			get { return "Osaka 14"; } //for some reason Pango needs "Osaka Regular-Mono 14" to be named this way
		}
		
		public override string Name {
			get { return "OSX"; }
		}
		
		private static void LoadMimeMap ()
		{
			// All recent Macs should have this file; if not we'll just die silently
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
		}
		
		HashSet<object> ignoreCommands = new HashSet<object> () {
			HelpCommands.About,
			EditCommands.DefaultPolicies,
			EditCommands.MonodevelopPreferences,
			FileCommands.Exit,
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
				{ EditCommands.Copy, CarbonCommandID.Copy },
				{ EditCommands.Cut, CarbonCommandID.Cut },
				//FIXME: for some reason mapping this causes two menu items to be created
		//		{ EditCommands.MonodevelopPreferences, CarbonCommandID.Preferences }, 
				{ EditCommands.Redo, CarbonCommandID.Redo },
				{ EditCommands.Undo, CarbonCommandID.Undo },
				{ EditCommands.SelectAll, CarbonCommandID.SelectAll },
				{ FileCommands.NewFile, CarbonCommandID.New },
				{ FileCommands.OpenFile, CarbonCommandID.Open },
				{ FileCommands.Save, CarbonCommandID.Save },
				{ FileCommands.SaveAs, CarbonCommandID.SaveAs },
				{ FileCommands.CloseFile, CarbonCommandID.Close },
				{ FileCommands.Exit, CarbonCommandID.Quit },
				{ FileCommands.ReloadFile, CarbonCommandID.Revert },
				{ HelpCommands.About, CarbonCommandID.About },
				{ HelpCommands.Help, CarbonCommandID.AppHelp },
			});
			
			initedApp = true;
			OSXIntegration.OSXMenu.SetAppQuitCommand (FileCommands.Exit);
			OSXIntegration.OSXMenu.AddAppMenuItems (commandManager, HelpCommands.About, Command.Separator,
			                                        EditCommands.DefaultPolicies, EditCommands.MonodevelopPreferences);
			
			IdeApp.Workbench.RootWindow.DeleteEvent += HandleDeleteEvent;
		}
		
		static void GlobalSetup ()
		{
			if (initedGlobal || setupFail)
				return;
			initedGlobal = true;
			
			try {
				//FIXME: remove these when finalizing
				Carbon.InstallApplicationEventHandler (HandleAppReopen, CarbonEventApple.ReopenApplication);
				Carbon.InstallApplicationEventHandler (HandleAppQuit, CarbonEventApple.QuitApplication);
				Carbon.InstallApplicationEventHandler (HandleAppDocOpen, CarbonEventApple.OpenDocuments); //kAEOpenDocuments
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Could not install app event handlers", ex);
				setupFail = true;
			}
		}
		
		static CarbonEventHandlerStatus HandleAppReopen (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			if (IdeApp.Workbench == null || IdeApp.Workbench.RootWindow == null)
				return CarbonEventHandlerStatus.NotHandled;
			
			IdeApp.Workbench.RootWindow.Deiconify ();
			IdeApp.Workbench.RootWindow.Visible = true;
			return CarbonEventHandlerStatus.Handled;
		}
		
		static CarbonEventHandlerStatus HandleAppQuit (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			IdeApp.Exit ();
			return CarbonEventHandlerStatus.Handled;
		}
		
		static CarbonEventHandlerStatus HandleAppDocOpen (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			try {
				AEDesc list = Carbon.GetEventParameter<AEDesc> (eventRef, CarbonEventParameterName.DirectObject, CarbonEventParameterType.AEList);
				long count = Carbon.AECountItems (ref list);
				List<string> files = new List<string> ();
				for (int i = 1; i <= count; i++) {
					FSRef fsRef = Carbon.AEGetNthPtr<FSRef> (ref list, i, CarbonEventParameterType.FSRef);
					string file = Carbon.FSRefToPath (ref fsRef);
					if (!String.IsNullOrEmpty (file))
						files.Add (file);
				}
				IdeApp.OpenFiles (files);
				Carbon.CheckReturn (Carbon.AEDisposeDesc (ref list));
			} catch (Exception ex) {
				System.Console.WriteLine (ex);
			}
			return CarbonEventHandlerStatus.Handled;
		}
		
		[GLib.ConnectBefore]
		static void HandleDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			args.RetVal = true;
			IdeApp.Workbench.RootWindow.Visible = false;
		}
	}
}
