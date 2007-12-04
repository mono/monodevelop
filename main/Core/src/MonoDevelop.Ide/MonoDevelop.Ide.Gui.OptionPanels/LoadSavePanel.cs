//  LoadSavePanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Collections;

using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;

using Gtk;
using MonoDevelop.Components;

#pragma warning disable 612

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	
	internal class LoadSavePanel : AbstractOptionPanel
	{
		//FIXME: Add mneumonics for Window, Macintosh and Unix Radio Buttons. 
		//       Remove mneumonic from terminator label.

		LoadSavePanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new LoadSavePanelWidget ());
		}
		
		public override bool StorePanelContents()
		{			
			bool succes = widget.Store();
			return succes;
		}
	}

	partial class LoadSavePanelWidget : Gtk.Bin
	{
		public LoadSavePanelWidget ()
		{
			Build ();
			
			//
			// load the internationalized strings.
			//
			folderEntry.Path = PropertyService.Get(
				"MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", 
				System.IO.Path.Combine(System.Environment.GetEnvironmentVariable ("HOME"),
						"Projects")).ToString();
			//
			// setup the properties
			//
			loadUserDataCheckButton.Active = PropertyService.Get (
				"SharpDevelop.LoadDocumentProperties", true);
			createBackupCopyCheckButton.Active = PropertyService.Get (
				"SharpDevelop.CreateBackupCopy", false);
			loadPrevProjectCheckButton.Active = (bool) PropertyService.Get(
				"SharpDevelop.LoadPrevProjectOnStartup", false);
		}
		
		public bool Store () 
		{
			PropertyService.Set("SharpDevelop.LoadPrevProjectOnStartup", loadPrevProjectCheckButton.Active);
			PropertyService.Set ("SharpDevelop.LoadDocumentProperties",  loadUserDataCheckButton.Active);
			PropertyService.Set ("SharpDevelop.CreateBackupCopy",        createBackupCopyCheckButton.Active);
			
			// check for correct settings
			string projectPath = folderEntry.Path;
			if (projectPath.Length > 0) {
				if (!FileService.IsValidFileName(projectPath)) {
					Services.MessageService.ShowError("Invalid project path specified");
					return false;
				}
			}
			PropertyService.Set("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", projectPath);
			
			return true;
		}
	}
}

#pragma warning restore 612
