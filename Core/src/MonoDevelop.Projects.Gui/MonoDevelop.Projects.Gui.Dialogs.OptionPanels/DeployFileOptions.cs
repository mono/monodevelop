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
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;

using Gtk;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal class DeployFileProjectOptions : AbstractOptionPanel
	{

		class DeployFileOptionsWidget : GladeWidgetExtract 
		{
		        // Gtk Controls
 			[Glade.Widget] RadioButton projectFileRadioButton;
 			[Glade.Widget] RadioButton compiledAssemblyRadioButton;
 			[Glade.Widget] RadioButton scriptFileRadioButton;
 			[Glade.Widget] Gnome.FileEntry selectScriptButton;
 			[Glade.Widget] Gnome.FileEntry selectTargetButton;
			//[Glade.Widget] VBox deployScriptBox;
			[Glade.Widget] VBox deployTargetBox;
			[Glade.Widget] Gtk.TreeView includeTreeView;
			public ListStore store;

			// Services
			Project project;

			public DeployFileOptionsWidget (IProperties CustomizationObject) : 
				base ("Base.glade", "DeployFileOptionsPanel")
			{
				this.project = (Project)((IProperties)CustomizationObject).GetProperty("Project");

  				projectFileRadioButton.Clicked += new EventHandler(RadioButtonClicked);
  				compiledAssemblyRadioButton.Clicked += new EventHandler(RadioButtonClicked);
  				scriptFileRadioButton.Clicked += new EventHandler(RadioButtonClicked);

				store = new ListStore (typeof(bool), typeof(string));
				includeTreeView.Selection.Mode = SelectionMode.None;
				includeTreeView.Model = store;
				CellRendererToggle rendererToggle = new CellRendererToggle ();
				rendererToggle.Activatable = true;
				rendererToggle.Toggled += new ToggledHandler (ItemToggled);
				includeTreeView.AppendColumn ("Choosen", rendererToggle, "active", 0);
				includeTreeView.AppendColumn ("Name", new CellRendererText (), "text", 1);
				
				foreach (ProjectFile info in project.ProjectFiles) {
					if (info.BuildAction != BuildAction.Exclude) {
						string name = Runtime.FileService.AbsoluteToRelativePath(project.BaseDirectory, info.Name);
						store.AppendValues (project.DeployInformation.IsFileExcluded(info.Name) ? true : false, name);
					}
				}

				selectTargetButton.Filename = project.DeployInformation.DeployTarget;
				selectScriptButton.Filename = project.DeployInformation.DeployScript;
			
				projectFileRadioButton.Active = project.DeployInformation.DeploymentStrategy == DeploymentStrategy.File;
				compiledAssemblyRadioButton.Active = project.DeployInformation.DeploymentStrategy == DeploymentStrategy.Assembly;
				scriptFileRadioButton.Active = project.DeployInformation.DeploymentStrategy == DeploymentStrategy.Script;
				
				RadioButtonClicked(null, null);
			}

			void RadioButtonClicked(object sender, EventArgs e)
			{
 				deployTargetBox.Sensitive = compiledAssemblyRadioButton.Active || projectFileRadioButton.Active;
  				selectScriptButton.Sensitive = scriptFileRadioButton.Active;
			}
			
			private void ItemToggled (object o, ToggledArgs args)
			{
 				const int column = 0;
 				Gtk.TreeIter iter;
				
				if (store.GetIterFromString(out iter, args.Path)) {
 					bool val = (bool) store.GetValue(iter, column);
 					store.SetValue(iter, column, !val);
 				}
			}
			
			public bool Store () 
			{
				if (selectTargetButton.Filename.Length > 0) {
					if (!Runtime.FileService.IsValidFileName(selectTargetButton.Filename)) {
						Services.MessageService.ShowError (null, GettextCatalog.GetString ("Invalid deploy target specified"), (Gtk.Window) Toplevel, true);
						return false;
					}
				}
				
				if (selectScriptButton.Filename.Length > 0) {
					if (!Runtime.FileService.IsValidFileName(selectScriptButton.Filename)) {
						Services.MessageService.ShowError (null, GettextCatalog.GetString ("Invalid deploy script specified"), (Gtk.Window) Toplevel, true);
						return false;				
					}
				}
				
				if (!System.IO.File.Exists(selectScriptButton.Filename)) {
					Services.MessageService.ShowError (null, GettextCatalog.GetString ("Deploy script doesn't exist"), (Gtk.Window) Toplevel, true);
					return false;
 				}
			
 			project.DeployInformation.DeployTarget = selectTargetButton.Filename;
 			project.DeployInformation.DeployScript = selectScriptButton.Filename;
			
 			if (projectFileRadioButton.Active) {
 				project.DeployInformation.DeploymentStrategy = DeploymentStrategy.File;
 			} else if (compiledAssemblyRadioButton.Active) {
 				project.DeployInformation.DeploymentStrategy = DeploymentStrategy.Assembly;
 			} else {
 				project.DeployInformation.DeploymentStrategy = DeploymentStrategy.Script;
 			}

			TreeIter first;	
			store.GetIterFirst(out first);
			TreeIter current = first;
 			project.DeployInformation.ClearExcludedFiles();
			for (int i = 0; i < store.IterNChildren() ; ++i) {
				if ( (bool) store.GetValue(current, 0)){
					project.DeployInformation.AddExcludedFile(Runtime.FileService.RelativeToAbsolutePath(
											  project.BaseDirectory, 
											  (string) store.GetValue(current, 1)));
							}
				store.IterNext(ref current);
			}
			return true;
			}
		}
		
		DeployFileOptionsWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new  DeployFileOptionsWidget ((IProperties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
			bool success = widget.Store();
 			return success;
		}
	}
}
