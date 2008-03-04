//  CompileFileProjectOptions.cs
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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;

using Gtk;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal class CompileFileProjectOptions : AbstractOptionPanel
	{
		CompileFileOptionsWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new  CompileFileOptionsWidget ((Properties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
 			return widget.Store ();
 		}
	}

	partial class CompileFileOptionsWidget : Gtk.Bin 
	{
		public ListStore store;
		
		Project project;

		public CompileFileOptionsWidget (Properties CustomizationObject)
		{
			Build ();
			
			this.project = ((Properties)CustomizationObject).Get<Project> ("Project");	
			
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
					string name = FileService.NormalizeRelativePath (
							FileService.AbsoluteToRelativePath(
								project.BaseDirectory, info.Name));
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
				string name = FileService.RelativeToAbsolutePath(
					project.BaseDirectory, "." + System.IO.Path.DirectorySeparatorChar + store.GetValue(current, 1));
				int j = 0;
				while (j < project.ProjectFiles.Count && project.ProjectFiles[j].Name != name) {
					++j;
				}
				if (j < project.ProjectFiles.Count) {
					project.ProjectFiles[j].BuildAction = (bool) store.GetValue(current, 0) ? BuildAction.Compile : BuildAction.Nothing;
				} else {
					MessageService.ShowError (GettextCatalog.GetString ("File {0} not found in {1}.", name, project.Name));
					success = false;
				}
			}
			return success;
		}
	}
	

}
