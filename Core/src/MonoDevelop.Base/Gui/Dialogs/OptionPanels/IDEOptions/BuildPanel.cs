// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;
using System.Collections;

using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Properties;
using MonoDevelop.Gui.Components;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Services;

using Gtk;
using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
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
		
		public class BuildPanelWidget :  GladeWidgetExtract 
		{
			//
			// Gtk controls
			//

			[Glade.Widget] public Gtk.RadioButton saveChangesRadioButton;
			[Glade.Widget] public Gtk.RadioButton promptChangesRadioButton; 
			[Glade.Widget] public Gtk.RadioButton noSaveRadioButton;
			[Glade.Widget] public Gtk.CheckButton showTaskListCheckBox;
			[Glade.Widget] public Gtk.CheckButton showOutputCheckBox;
			[Glade.Widget] public Gtk.Label buildAndRunOptionsLabel;   

			public  BuildPanelWidget () : base ("Base.glade", "BuildPanel")
			{
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
}
