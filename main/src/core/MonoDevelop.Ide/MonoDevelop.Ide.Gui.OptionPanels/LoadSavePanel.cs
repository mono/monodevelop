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
using System.Collections.Generic;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using Gtk;
using MonoDevelop.Components;

#pragma warning disable 612

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	
	internal class LoadSavePanel : OptionsPanel
	{
		//FIXME: Add mneumonics for Window, Macintosh and Unix Radio Buttons. 
		//       Remove mneumonic from terminator label.

		LoadSavePanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return widget = new LoadSavePanelWidget ();
		}
		
		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges ();
		}

		
		public override void ApplyChanges ()
		{
			widget.Store();
		}
	}

	partial class LoadSavePanelWidget : Gtk.Bin
	{
		List<string> formats = new List<string> ();
		
		public LoadSavePanelWidget ()
		{
			Build ();
			
			folderEntry.Path = IdeApp.ProjectOperations.ProjectsDefaultPath;
			
			loadUserDataCheckButton.Active = IdeApp.Preferences.LoadDocumentUserProperties;
			createBackupCopyCheckButton.Active = IdeApp.Preferences.CreateFileBackupCopies;
			loadPrevProjectCheckButton.Active = IdeApp.Preferences.LoadPrevSolutionOnStartup;

			string defaultFormat = IdeApp.Preferences.DefaultProjectFileFormat;
			
			Solution sol = new Solution ();
			FileFormat[] fs = IdeApp.Services.ProjectService.FileFormats.GetFileFormatsForObject (sol);
			foreach (FileFormat f in fs) {
				comboFileFormats.AppendText (f.Name);
				formats.Add (f.Id);
				if (f.Id == defaultFormat)
					comboFileFormats.Active = formats.Count - 1;
			}
		}
		
		public bool ValidateChanges ()
		{
			// check for correct settings
			string projectPath = folderEntry.Path;
			if (projectPath.Length > 0) {
				if (!FileService.IsValidPath(projectPath)) {
					MessageService.ShowError ("Invalid project path specified");
					return false;
				}
			}
			return true;
		}
		
		public void Store () 
		{
			IdeApp.Preferences.LoadPrevSolutionOnStartup = loadPrevProjectCheckButton.Active;
			IdeApp.Preferences.LoadDocumentUserProperties = loadUserDataCheckButton.Active;
			IdeApp.Preferences.CreateFileBackupCopies = createBackupCopyCheckButton.Active;
			IdeApp.Preferences.DefaultProjectFileFormat = formats [comboFileFormats.Active];
			IdeApp.ProjectOperations.ProjectsDefaultPath = folderEntry.Path;
		}
	}
}

#pragma warning restore 612
