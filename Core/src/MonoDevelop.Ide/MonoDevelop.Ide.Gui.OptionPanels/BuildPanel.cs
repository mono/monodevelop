// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;

using MonoDevelop.Core.Properties;
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
			BeforeCompileAction action = (BeforeCompileAction) Runtime.Properties.GetProperty(
				"SharpDevelop.Services.DefaultParserService.BeforeCompileAction", 
				BeforeCompileAction.SaveAllFiles);
			saveChangesRadioButton.Active = action.Equals(BeforeCompileAction.SaveAllFiles);
			promptChangesRadioButton.Active = action.Equals(BeforeCompileAction.PromptForSave);
			noSaveRadioButton.Active = action.Equals(BeforeCompileAction.Nothing);
			showTaskListCheckBox.Active = (bool)Runtime.Properties.GetProperty(
				"SharpDevelop.ShowTaskListAfterBuild", true);
			showOutputCheckBox.Active = (bool)Runtime.Properties.GetProperty(
				"SharpDevelop.ShowOutputWindowAtBuild", true);
		}
		
		public void Store ()
		{
			// set properties
			if (saveChangesRadioButton.Active) {
				Runtime.Properties.SetProperty("SharpDevelop.Services.DefaultParserService.BeforeCompileAction", 
						BeforeCompileAction.SaveAllFiles);
			} else if (promptChangesRadioButton.Active) {
				Runtime.Properties.SetProperty("SharpDevelop.Services.DefaultParserService.BeforeCompileAction", 
						BeforeCompileAction.PromptForSave);
			} else if (noSaveRadioButton.Active) {
				Runtime.Properties.SetProperty("SharpDevelop.Services.DefaultParserService.BeforeCompileAction", 
						BeforeCompileAction.Nothing);
			}
			
			Runtime.Properties.SetProperty("SharpDevelop.ShowTaskListAfterBuild", showTaskListCheckBox.Active);
			Runtime.Properties.SetProperty("SharpDevelop.ShowOutputWindowAtBuild", showOutputCheckBox.Active);
		}
	}
}
