// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Gui.Components;
using MonoDevelop.Services;
using MonoDevelop.Gui.Widgets;

using Gtk;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
{
	public class CompileFileProjectOptions : AbstractOptionPanel
	{
		class CompileFileOptionsWidget : GladeWidgetExtract 
		{
			// Gtk Controls
			[Glade.Widget] Label includeLabel;
			[Glade.Widget] Gtk.TreeView includeTreeView;
			public ListStore store;
			
			Project project;

			public CompileFileOptionsWidget (IProperties CustomizationObject) : 
				base ("Base.glade", "CompileFileOptionsPanel")
			{
				this.project = (Project)((IProperties)CustomizationObject).GetProperty("Project");	
				
				includeLabel.UseUnderline = true;
				store = new ListStore (typeof(bool), typeof(string));
				store.SetSortColumnId (1, SortType.Ascending);
				includeTreeView.Selection.Mode = SelectionMode.None;
				includeTreeView.Model = store;
				CellRendererToggle rendererToggle = new CellRendererToggle ();
				rendererToggle.Activatable = true;
				rendererToggle.Toggled += new ToggledHandler (ItemToggled);
				includeTreeView.AppendColumn ("Choosen", rendererToggle, "active", 0);
				includeTreeView.AppendColumn ("Name", new CellRendererText (), "text", 1);
				
				foreach (ProjectFile info in project.ProjectFiles) {
					if (info.BuildAction == BuildAction.Nothing || info.BuildAction == BuildAction.Compile) {
						string name = Runtime.FileUtilityService.AbsoluteToRelativePath(
							project.BaseDirectory, info.Name).Substring(2);
						store.AppendValues (info.BuildAction == BuildAction.Compile ? true : false, name);
					}
				}
			}			
			
			private void ItemToggled (object o, ToggledArgs args)
			{
 				const int column = 0;
 				Gtk.TreeIter iter;
				
				if (store.GetIterFromString (out iter, args.Path)) {
 					bool val = (bool) store.GetValue(iter, column);
 					store.SetValue(iter, column, !val);
 				}
			}

			public bool Store ()
			{	
				bool success = true;
				TreeIter current;	
				store.GetIterFirst (out current);

				for (int i = 0; i < store.IterNChildren (); i++) {
					if (i != 0)
						store.IterNext(ref current);
					string name = Runtime.FileUtilityService.RelativeToAbsolutePath(
						project.BaseDirectory, "." + System.IO.Path.DirectorySeparatorChar + store.GetValue(current, 1));
					int j = 0;
					while (j < project.ProjectFiles.Count && project.ProjectFiles[j].Name != name) {
						++j;
					}
					if (j < project.ProjectFiles.Count) {
						project.ProjectFiles[j].BuildAction = (bool) store.GetValue(current, 0) ? BuildAction.Compile : BuildAction.Nothing;
					} else {
						Runtime.MessageService.ShowError (String.Format (GettextCatalog.GetString ("File {0} not found in {1}."), name, project.Name));
						success = false;
					}
				}
				return success;
			}
		}
		
		CompileFileOptionsWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new  CompileFileOptionsWidget ((IProperties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
 			return widget.Store ();
 		}
	}
}
