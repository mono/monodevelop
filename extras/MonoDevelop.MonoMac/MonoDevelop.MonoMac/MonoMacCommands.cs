// 
// MonoMacCommands.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.MonoMac.Gui;
using MonoDevelop.Components;
using MonoDevelop.MacDev;
using System.Linq;

namespace MonoDevelop.MonoMac
{
	public enum MonoMacCommands
	{
		CreateMacInstaller
	}
	
	class CreateMacInstallerHandler : CommandHandler
	{
		const string PROP_KEY = "MonoMacPackagingSettings";
		
		static MonoMacProject GetSelectedMonoMacProject ()
		{
			return IdeApp.ProjectOperations.CurrentSelectedProject as MonoMacProject;
		}
		
		protected override void Update (CommandInfo info)
		{
			var proj = GetSelectedMonoMacProject ();
			info.Visible = proj != null;
			info.Enabled = info.Visible && proj.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) != null;
		}
		
		protected override void Run ()
		{
			var proj = GetSelectedMonoMacProject ();
			
			var settings = proj.UserProperties.GetValue<MonoMacPackagingSettings> (PROP_KEY)
				?? MonoMacPackagingSettings.GetAppStoreDefault ();
			
			MonoMacPackagingSettingsDialog dlg = null;
			try {
				dlg = new MonoMacPackagingSettingsDialog ();
				dlg.LoadSettings (settings);
				if (MessageService.RunCustomDialog (dlg) != (int)ResponseType.Ok)
					return;
				dlg.SaveSettings (settings);
			} finally {
				if (dlg != null)
					dlg.Destroy ();
			}
			
			var configSel = IdeApp.Workspace.ActiveConfiguration;
			var cfg = (MonoMacProjectConfiguration) proj.GetConfiguration (configSel);
			
			var ext = settings.CreatePackage? ".pkg" : ".app";
			
			var fileDlg = new SelectFileDialog () {
				Title = settings.CreatePackage?
					GettextCatalog.GetString ("Save Installer Package") :
					GettextCatalog.GetString ("Save Application Bundle"),
				InitialFileName = cfg.AppName + ext,
				Action = FileChooserAction.Save
			};
			fileDlg.DefaultFilter = fileDlg.AddFilter ("", "*" + ext);
			
			if (!fileDlg.Run ())
				return;
			
			proj.UserProperties.SetValue (PROP_KEY, settings);
			
			var target = fileDlg.SelectedFile;
			if (!string.Equals (target.Extension, ext, StringComparison.OrdinalIgnoreCase))
				target.ChangeExtension (ext);
			
			MonoMacPackaging.Package (proj, configSel, settings, target);
		}
	}
}

