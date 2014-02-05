// 
// SelectProjectsDialog.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using ICSharpCode.PackageManagement;

namespace MonoDevelop.PackageManagement
{
	public partial class SelectProjectsDialog : Gtk.Dialog
	{
		SelectProjectsViewModel viewModel;
		ListStore projectsStore;
		const int SelectedCheckBoxColumn = 0;
		const int SelectedProjectNameColumn = 1;
		const int SelectedProjectColumn = 2;
		
		public SelectProjectsDialog (SelectProjectsViewModel viewModel)
		{
			this.Build ();
			this.viewModel = viewModel;
			InitializeTreeView ();
			AddProjectsToTreeView ();
		}
		
		void InitializeTreeView ()
		{
			projectsStore = new ListStore (typeof (bool), typeof (string), typeof (IPackageManagementSelectedProject));
			projectsTreeView.Model = projectsStore;
			projectsTreeView.AppendColumn (CreateTreeViewColumn ());
			projectsTreeView.Selection.Changed += ProjectsTreeViewSelectionChanged;
		}
		
		TreeViewColumn CreateTreeViewColumn ()
		{
			var column = new TreeViewColumn ();
			
			var checkBoxRenderer = new CellRendererToggle ();
			checkBoxRenderer.Toggled += SelectedProjectCheckBoxToggled;
			column.PackStart (checkBoxRenderer, false);
			column.AddAttribute (checkBoxRenderer, "active", SelectedCheckBoxColumn);
			
			var textRenderer = new CellRendererText ();
			column.PackStart (textRenderer, true);
			column.AddAttribute (textRenderer, "markup", SelectedProjectNameColumn);
			
			return column;
		}
		
		void ProjectsTreeViewSelectionChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (projectsTreeView.Selection.GetSelected (out iter)) {
				var project = projectsStore.GetValue (iter, SelectedProjectColumn) as IPackageManagementSelectedProject;
				if (!project.IsEnabled) {
					projectsTreeView.Selection.UnselectIter (iter);
				}
			}
		}
		
		void SelectedProjectCheckBoxToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			projectsStore.GetIterFromString (out iter, args.Path);
			var project = projectsStore.GetValue (iter, SelectedProjectColumn) as IPackageManagementSelectedProject;
			if (project.IsEnabled) {
				project.IsSelected = !project.IsSelected;
				projectsStore.SetValue (iter, SelectedCheckBoxColumn, project.IsSelected);
			}
		}
		
		void AddProjectsToTreeView ()
		{
			foreach (IPackageManagementSelectedProject project in viewModel.Projects) {
				AddProjectToTreeView (project);
			}
		}
		
		void AddProjectToTreeView (IPackageManagementSelectedProject project)
		{
			projectsStore.AppendValues (project.IsSelected, GetProjectNameMarkup (project), project);
		}
		
		string GetProjectNameMarkup (IPackageManagementSelectedProject project)
		{
			if (project.IsEnabled) {
				return project.Name;
			}
			
			string format = "<span foreground='lightgrey'>{0}</span>";
			return MarkupString.Format (format, project.Name);
		}
	}
}

