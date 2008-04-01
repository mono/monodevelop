//  GeneralProjectOptions.cs
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
using System.ComponentModel;

using MonoDevelop.Projects;
using MonoDevelop.Core;

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui.Dialogs;
using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal class GeneralProjectOptions : AbstractOptionPanel
	{
		GeneralProjectOptionsWidget widget;

		public override void LoadPanelContents()
		{
			Add (widget = new GeneralProjectOptionsWidget ((Properties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ((Properties) CustomizationObject);
 			return true;
		}
	}

	partial class GeneralProjectOptionsWidget : Gtk.Bin
	{
		Project project;

		public GeneralProjectOptionsWidget (Properties CustomizationObject)
		{
			Build ();
			
			this.project = ((Properties)CustomizationObject).Get<Project> ("Project");
			
			nameLabel.UseUnderline = true;
			
			descriptionLabel.UseUnderline = true;

			projectNameEntry.Text = project.Name;
			projectDefaultNamespaceEntry.Text = project.DefaultNamespace;
			parentDirectoryNamespaceCheckButton.Active = project.UseParentDirectoryAsNamespace;
			projectDescriptionTextView.Buffer.Text = project.Description;
			
			switch (project.NewFileSearch) 
			{
			case NewFileSearch.None:
				newFilesOnLoadCheckButton.Active = false; 
				autoInsertNewFilesCheckButton.Active = false;
				break;
			case NewFileSearch.OnLoad:
				newFilesOnLoadCheckButton.Active = true; 
				autoInsertNewFilesCheckButton.Active = false;
				break;
			default:
				newFilesOnLoadCheckButton.Active = true; 
				autoInsertNewFilesCheckButton.Active = true;
				break;
			}
			
			newFilesOnLoadCheckButton.Clicked += new EventHandler(AutoLoadCheckBoxCheckedChangeEvent);
			parentDirectoryNamespaceCheckButton.Clicked += new EventHandler(ParentDirectoryNamespaceCheckButtonChangeEvent);
			AutoLoadCheckBoxCheckedChangeEvent(null, null);
			ParentDirectoryNamespaceCheckButtonChangeEvent(null, null);
		}			

		void AutoLoadCheckBoxCheckedChangeEvent(object sender, EventArgs e)
		{
			autoInsertNewFilesCheckButton.Sensitive = newFilesOnLoadCheckButton.Active;
			if (newFilesOnLoadCheckButton.Active == false) {
				autoInsertNewFilesCheckButton.Active = false;
			}
		}
		
		void ParentDirectoryNamespaceCheckButtonChangeEvent(object sender, EventArgs e)
		{
			projectDefaultNamespaceEntry.Sensitive = !parentDirectoryNamespaceCheckButton.Active;
		}

		public void  Store (Properties CustomizationObject)
		{
			project.Name                          = projectNameEntry.Text;
			project.DefaultNamespace              = projectDefaultNamespaceEntry.Text;
			project.UseParentDirectoryAsNamespace = parentDirectoryNamespaceCheckButton.Active;
			project.Description                   = projectDescriptionTextView.Buffer.Text;
			
			if (newFilesOnLoadCheckButton.Active) {
				project.NewFileSearch = autoInsertNewFilesCheckButton.Active ?  NewFileSearch.OnLoadAutoInsert : NewFileSearch.OnLoad;
			} else {
				project.NewFileSearch = NewFileSearch.None;
			}
		}
	}

}

