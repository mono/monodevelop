//  BuildPanel.cs
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

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal class BuildPanel : AbstractOptionPanel
	{

		BuildPanelWidget widget;

		public override void LoadPanelContents()
		{
			Add (widget = new  BuildPanelWidget ());
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ();
			return true;
		}
	}
	
	internal partial class BuildPanelWidget :  Gtk.Bin 
	{
		public BuildPanelWidget ()
		{
			Build ();
			
		        //
		        // getting internationalized strings
		        //
			// FIXME i8n the following line
			//
			// reading properties
			//
			BeforeCompileAction action = (BeforeCompileAction) PropertyService.Get(
				"SharpDevelop.Services.DefaultParserService.BeforeCompileAction", 
				BeforeCompileAction.SaveAllFiles);
			saveChangesRadioButton.Active = action.Equals(BeforeCompileAction.SaveAllFiles);
			promptChangesRadioButton.Active = action.Equals(BeforeCompileAction.PromptForSave);
			noSaveRadioButton.Active = action.Equals(BeforeCompileAction.Nothing);
			showTaskListCheckBox.Active = (bool)PropertyService.Get(
				"SharpDevelop.ShowTaskListAfterBuild", true);
			showOutputCheckBox.Active = (bool)PropertyService.Get(
				"SharpDevelop.ShowOutputWindowAtBuild", true);
		}
		
		public void Store ()
		{
			// set properties
			if (saveChangesRadioButton.Active) {
				PropertyService.Set("SharpDevelop.Services.DefaultParserService.BeforeCompileAction", 
						BeforeCompileAction.SaveAllFiles);
			} else if (promptChangesRadioButton.Active) {
				PropertyService.Set("SharpDevelop.Services.DefaultParserService.BeforeCompileAction", 
						BeforeCompileAction.PromptForSave);
			} else if (noSaveRadioButton.Active) {
				PropertyService.Set("SharpDevelop.Services.DefaultParserService.BeforeCompileAction", 
						BeforeCompileAction.Nothing);
			}
			
			PropertyService.Set("SharpDevelop.ShowTaskListAfterBuild", showTaskListCheckBox.Active);
			PropertyService.Set("SharpDevelop.ShowOutputWindowAtBuild", showOutputCheckBox.Active);
		}
	}
}
