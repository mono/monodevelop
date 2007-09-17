// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
	// FIXME 
	// - internationalize 
	//   SetupFromXml(Path.Combine(PropertyService.DataDirectory, 
	//                           @"resources\panels\GeneralProjectOptions.xfrm"));
	// - Name entry can't be empty. It crashes with empty values.

	public class GeneralProjectOptions : AbstractOptionPanel {

		class GeneralProjectOptionsWidget : GladeWidgetExtract {

			// Gtk Controls
			[Glade.Widget] Label nameLabel;
			[Glade.Widget] Label descriptionLabel;
			[Glade.Widget] Entry projectNameEntry;
			[Glade.Widget] Entry projectDefaultNamespaceEntry;
			[Glade.Widget] TextView projectDescriptionTextView;
			[Glade.Widget] CheckButton newFilesOnLoadCheckButton;
 			[Glade.Widget] CheckButton autoInsertNewFilesCheckButton;
 			[Glade.Widget] CheckButton enableViewStateCheckButton;

			Project project;

			public GeneralProjectOptionsWidget (Properties CustomizationObject) : base ("Base.glade", "GeneralProjectOptionsPanel")
			{
				this.project = ((Properties)CustomizationObject).Get<Project> ("Project");
				
				nameLabel.UseUnderline = true;
				
				descriptionLabel.UseUnderline = true;

				projectNameEntry.Text = project.Name;
				projectDefaultNamespaceEntry.Text = project.DefaultNamespace;
				projectDescriptionTextView.Buffer.Text = project.Description;
				enableViewStateCheckButton.Active = project.EnableViewState;
				
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
				AutoLoadCheckBoxCheckedChangeEvent(null, null);
			}			

			void AutoLoadCheckBoxCheckedChangeEvent(object sender, EventArgs e)
			{
				autoInsertNewFilesCheckButton.Sensitive = newFilesOnLoadCheckButton.Active;
				if (newFilesOnLoadCheckButton.Active == false) {
					autoInsertNewFilesCheckButton.Active = false;
				}
			}

			public void  Store (Properties CustomizationObject)
			{
				project.Name                 = projectNameEntry.Text;
				project.DefaultNamespace     = projectDefaultNamespaceEntry.Text;
				project.Description          = projectDescriptionTextView.Buffer.Text;
				project.EnableViewState      = enableViewStateCheckButton.Active;
				
				if (newFilesOnLoadCheckButton.Active) {
					project.NewFileSearch = autoInsertNewFilesCheckButton.Active ?  NewFileSearch.OnLoadAutoInsert : NewFileSearch.OnLoad;
				} else {
					project.NewFileSearch = NewFileSearch.None;
				}
			}
		}
		
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
}

