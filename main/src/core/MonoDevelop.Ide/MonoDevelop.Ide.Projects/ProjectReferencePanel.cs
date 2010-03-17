// 
// ProjectReferencePanel.cs
//  
// Author:
//       Todd Berman <tberman@sevenl.net>
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

using Gtk;

namespace MonoDevelop.Ide.Projects {
	
	internal class ProjectReferencePanel : VBox, IReferencePanel
	{
		SelectReferenceDialog selectDialog;

		ListStore store;
		TreeView  treeView;
		
		public ProjectReferencePanel (SelectReferenceDialog selectDialog) : base (false, 6)
		{
			this.selectDialog = selectDialog;
			
			store = new ListStore (typeof (string), typeof (string), typeof(Project), typeof(bool), typeof(Gdk.Pixbuf), typeof(bool), typeof(string));
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
			secondColumn.AddAttribute (text_render, "foreground", 6);
			
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
			
			Dictionary<DotNetProject,bool> references = new Dictionary<DotNetProject, bool> ();
			
			foreach (Project projectEntry in openSolution.GetAllSolutionItems<Project>()) {

				if (projectEntry == configureProject)
					continue;

				string txt = projectEntry.Name;
				bool allowSelecting = true;
				DotNetProject netProject = projectEntry as DotNetProject;
				if (netProject != null) {
					if (ProjectReferencesProject (references, null, netProject, configureProject.Name)) {
						txt += " " + GettextCatalog.GetString ("(Cyclic dependencies not allowed)");
						allowSelecting = false;
					}
				    else if (!configureProject.TargetFramework.IsCompatibleWithFramework (netProject.TargetFramework.Id)) {
						txt += " " + GettextCatalog.GetString ("(Incompatible target framework: v{0})", netProject.TargetFramework.Id);
						allowSelecting = false;
					}
				}
				
				Gdk.Pixbuf icon = ImageService.GetPixbuf (projectEntry.StockIcon, IconSize.Menu);
				if (!allowSelecting)
					icon = ImageService.MakeTransparent (icon, 0.5);
				Gtk.TreeIter it = store.AppendValues (txt, projectEntry.BaseDirectory.ToString (), projectEntry, false, icon, allowSelecting);
				if (!allowSelecting)
					store.SetValue (it, 6, "dimgrey");
			}
		}
		
		bool ProjectReferencesProject (Dictionary<DotNetProject,bool> references, HashSet<string> parentDeps,
		                               DotNetProject project, string targetProject)
		{
			bool res;
			if (references.TryGetValue (project, out res))
				return res;
			foreach (ProjectReference pr in project.References) {
				if (pr.Reference == targetProject) {
					references [project] = true;
					return true;
				}
				
				DotNetProject pref = project.ParentSolution.FindProjectByName (pr.Reference) as DotNetProject;
				if (pref != null) {
					if (parentDeps == null) {
						parentDeps = new HashSet<string> ();
					} else if (parentDeps.Contains (pref.Name)) {
						LoggingService.LogWarning ("Cyclic dependency detected between projects '{0}' and '{1}'", project.Name, pref.Name);
						references [project] = true;
						return true;
					}
					parentDeps.Add (pref.Name);
					bool referencesTarget = ProjectReferencesProject (references, parentDeps, pref, targetProject);
					parentDeps.Remove (pref.Name);
					
					if (referencesTarget) {
						references [project] = true;
						return true;
					}
				}
			}
			references [project] = false;
			return false;
		}
	}
}

