//  ProjectReferencePanel.cs
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
			store.SetSortFunc (0, CompareNodes);
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
			
			store.SetSortColumnId (0, SortType.Ascending);
			ShowAll ();
			
			BorderWidth = 6;
		}

		public void SetProject (DotNetProject configureProject)
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
				selectDialog.AddReference (new ProjectReference (project));
				
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
		
		int CompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			string s1 = (string) store.GetValue (a, 0);
			string s2 = (string) store.GetValue (b, 0);
			if (s1 == string.Empty) return 1;
			if (s2 == string.Empty) return -1;
			return String.Compare (s1, s2, true);
		}
		
		void PopulateListView (DotNetProject configureProject)
		{
			Solution openSolution = configureProject.ParentSolution;
			
			if (openSolution == null) {
				return;
			}
			
			bool circDeps = false;
			foreach (Project projectEntry in openSolution.GetAllSolutionItems<Project>()) {

				if (projectEntry == configureProject) {
					continue;
				}

				DotNetProject netProject = projectEntry as DotNetProject;
				if (netProject != null) {
					if (ProjectReferencesProject (netProject, configureProject.Name)) {
						circDeps = true;
						continue;
					}
				    if (!configureProject.TargetFramework.IsCompatibleWithFramework (netProject.TargetFramework.Id))
						continue;
				}
				
				Gdk.Pixbuf icon = PixbufService.GetPixbuf (projectEntry.StockIcon, IconSize.Menu);
				store.AppendValues (projectEntry.Name, projectEntry.BaseDirectory, projectEntry, false, icon, true);
			}
			
			if (circDeps)
				store.AppendValues ("", "<span foreground='dimgrey'>" + GettextCatalog.GetString ("(Projects referencing '{0}' are not shown,\nsince cyclic dependencies are not allowed)", configureProject.Name) + "</span>", null, false, null, false);
		}
		
		bool ProjectReferencesProject (DotNetProject project, string targetProject)
		{
			foreach (ProjectReference pr in project.References) {
				if (pr.Reference == targetProject)
					return true;
				
				DotNetProject pref = project.ParentSolution.FindProjectByName (pr.Reference) as DotNetProject;
				if (pref != null && ProjectReferencesProject (pref, targetProject))
					return true;
			}
			return false;
		}
	}
}

