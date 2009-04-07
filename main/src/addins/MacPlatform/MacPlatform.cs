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
using IgeMacIntegration;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Platform
{
	public class MacPlatform : PlatformService
	{
		OSXIntegration.OSXMenu globalMenu;
		bool igeInited, igeExists;
		bool menuFail;
		
		static Dictionary<string, string> mimemap;

		static MacPlatform () {
			mimemap = new Dictionary<string, string> ();
			LoadMimeMap ();
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
			get { return "Monaco 12"; }
		}
		
		public override string Name {
			get { return "OSX"; }
		}
		
		private static void LoadMimeMap () {
			// All recent Macs should have this file; if not we'll just die silently
			try {
				StreamReader reader = new StreamReader (File.OpenRead ("/etc/apache2/mime.types"));
				Regex mime = new Regex ("([a-zA-Z]+/[a-zA-z0-9+-_.]+)\t+([a-zA-Z]+)");
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
		
		[System.Runtime.InteropServices.DllImport("libigemacintegration.dylib")]
		static extern void ige_mac_menu_set_global_key_handler_enabled (bool enabled);
		
		bool IgeExists ()
		{
			if (igeInited)
				return igeExists;
			igeInited = true;
			
			try {
				//disabled, as the IGE menu integration can't handle our menus
				ige_mac_menu_set_global_key_handler_enabled (false);
			}
			catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Could not load libigemacintegration. Main Menu integration disabled.", ex);
				return false;
			}
			
			IgeSetup ();
			igeExists = true;
			return true;
		}
		
		public override bool SetGlobalMenu (CommandManager commandManager, string commandMenuAddinPath)
		{
			IgeExists ();
			
			//FIXME: disabled, as it doesn't fully work yet
			return false;
			
			if (menuFail)
				return false;
			
			if (globalMenu == null) {
				try {
					CommandEntrySet ces = commandManager.CreateCommandEntrySet (commandMenuAddinPath);
					globalMenu = new OSXIntegration.OSXMenu (commandManager, ces);
					globalMenu.InstallMenu ();
				} catch (Exception ex) {
					MonoDevelop.Core.LoggingService.LogError ("Could not install global menu", ex);
					menuFail = true;
					return false;
				}
			} else {
				//FIXME: update existing menus
				MonoDevelop.Core.LoggingService.LogError ("Updating global menu not supported yet");
			}
			
			return true;
		}
		
		//add quit, preferences, about to the app menu group
		void IgeSetup ()
		{
			IgeMacMenu.QuitMenuItem = new CommandMenuItem (FileCommands.Exit, IdeApp.CommandService);
			
			IgeMacMenuGroup aboutGroup = IgeMacMenu.AddAppMenuGroup ();
			object cmdId = HelpCommands.About;
			aboutGroup.AddMenuItem (new CommandMenuItem (cmdId, IdeApp.CommandService), IdeApp.CommandService.GetCommand (cmdId).Text.Replace ("_", ""));
			
			IgeMacMenuGroup prefsGroup = IgeMacMenu.AddAppMenuGroup ();
			cmdId = EditCommands.MonodevelopPreferences;
			prefsGroup.AddMenuItem (new CommandMenuItem (cmdId, IdeApp.CommandService), IdeApp.CommandService.GetCommand (cmdId).Text.Replace ("_", ""));
			cmdId = EditCommands.DefaultPolicies;
			prefsGroup.AddMenuItem (new CommandMenuItem (cmdId, IdeApp.CommandService), IdeApp.CommandService.GetCommand (cmdId).Text.Replace ("_", ""));	
			IgeMacDock.Default.QuitActivate += delegate {
				IdeApp.Exit ();
			};
			
			IgeMacDock.Default.Clicked += delegate {
				IdeApp.Workbench.RootWindow.Deiconify ();
				IdeApp.Workbench.RootWindow.Visible = true;
			};
			
			IdeApp.Workbench.RootWindow.DeleteEvent += HandleDeleteEvent;
		}
		
		[GLib.ConnectBefore]
		void HandleDeleteEvent(object o, Gtk.DeleteEventArgs args)
		{
			args.RetVal = true;
			IdeApp.Workbench.RootWindow.Visible = false;
		}
	}
}
