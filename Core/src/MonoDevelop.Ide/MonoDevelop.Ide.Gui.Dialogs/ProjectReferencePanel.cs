// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;

using Gtk;

namespace MonoDevelop.Ide.Gui.Dialogs {
	
	internal class ProjectReferencePanel : VBox, IReferencePanel
	{
		SelectReferenceDialog selectDialog;

		ListStore store;
		TreeView  treeView;
		
		public ProjectReferencePanel (SelectReferenceDialog selectDialog) : base (false, 6)
		{
			this.selectDialog = selectDialog;
			
			store = new ListStore (typeof (string), typeof (string), typeof(Project), typeof(bool), typeof(Gdk.Pixbuf), typeof(bool));
			store.SetSortColumnId (0, SortType.Ascending);
			treeView = new TreeView (store);
			
			TreeViewColumn firstColumn = new TreeViewColumn ();
			TreeViewColumn secondColumn = new TreeViewColumn ();
			
			CellRendererToggle tog_render = new CellRendererToggle ();
			tog_render.Xalign = 0;
			tog_render.Toggled += new Gtk.ToggledHandler (AddReference);
			firstColumn.PackStart (tog_render, false);
			firstColumn.AddAttribute (tog_render, "active", 3);
			firstColumn.AddAttribute (tog_render, "visible", 5);

			secondColumn.Title = GettextCatalog.GetString ("Project");
			Gtk.CellRendererPixbuf pix_render = new Gtk.CellRendererPixbuf ();
			secondColumn.PackStart (pix_render, false);
			secondColumn.AddAttribute (pix_render, "pixbuf", 4);
			
			CellRendererText text_render = new CellRendererText ();
			secondColumn.PackStart (text_render, true);
			secondColumn.AddAttribute (text_render, "text", 0);
			secondColumn.AddAttribute (text_render, "visible", 5);
			
			treeView.AppendColumn (firstColumn);
			treeView.AppendColumn (secondColumn);
			treeView.AppendColumn (GettextCatalog.GetString ("Directory"), new CellRendererText (), "markup", 1);
			
			ScrolledWindow sc = new ScrolledWindow ();
			sc.ShadowType = Gtk.ShadowType.In;
			sc.Add (treeView);
			PackStart (sc, true, true, 0);
			
			ShowAll ();
			
			BorderWidth = 6;
		}

		public void SetProject (Project configureProject)
		{
			store.Clear ();
			PopulateListView (configureProject);
		}
		
		public void AddReference(object sender, Gtk.ToggledArgs e)
		{
			Gtk.TreeIter iter;
			store.GetIterFromString (out iter, e.Path);
			Project project = (Project) store.GetValue (iter, 2);
			
			if ((bool)store.GetValue (iter, 3) == false) {
				store.SetValue (iter, 3, true);
				selectDialog.AddReference (ReferenceType.Project, project.Name);
				
			} else {
				store.SetValue (iter, 3, false);
				selectDialog.RemoveReference(ReferenceType.Project, project.Name);
			}
		}
		
		public void SignalRefChange (string refLoc, bool newstate)
		{
			Gtk.TreeIter looping_iter;
			if (!store.GetIterFirst (out looping_iter)) {
				return;
			}

			do {
				Project project = (Project) store.GetValue (looping_iter, 2);
				if (project == null)
					return;
				if (project.Name == refLoc) {
					store.SetValue (looping_iter, 3, newstate);
					return;
				}
			} while (store.IterNext (ref looping_iter));
		}
		
		void PopulateListView (Project configureProject)
		{
			Combine openCombine = IdeApp.ProjectOperations.CurrentOpenCombine;
			
			if (openCombine == null) {
				return;
			}
			
			bool circDeps = false;
			foreach (Project projectEntry in openCombine.GetAllProjects()) {

				if (projectEntry == configureProject) {
					continue;
				}
				if (ProjectReferencesProject (projectEntry, configureProject.Name)) {
					circDeps = true;
					continue;
				}

				string iconName = Services.Icons.GetImageForProjectType (projectEntry.ProjectType);
				Gdk.Pixbuf icon = treeView.RenderIcon (iconName, Gtk.IconSize.Menu, "");
				store.AppendValues (projectEntry.Name, projectEntry.BaseDirectory, projectEntry, false, icon, true);
			}
			
			if (circDeps)
				store.AppendValues ("" + (char)200, "<span foreground='dimgrey'>" + GettextCatalog.GetString ("(Projects referencing '{0}' are not shown,\nsince cyclic dependencies are not allowed)", configureProject.Name) + "</span>", null, false, null, false);
		}
		
		bool ProjectReferencesProject (Project project, string targetProject)
		{
			foreach (ProjectReference pr in project.ProjectReferences) {
				if (pr.Reference == targetProject)
					return true;
				
				Project pref = project.RootCombine.FindProject (pr.Reference);
				if (pref != null && ProjectReferencesProject (pref, targetProject))
					return true;
			}
			return false;
		}
	}
}

