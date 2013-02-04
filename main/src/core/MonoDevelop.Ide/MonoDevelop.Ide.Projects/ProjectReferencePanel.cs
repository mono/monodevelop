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
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Projects {
	
	internal class ProjectReferencePanel : VBox, IReferencePanel
	{
		SelectReferenceDialog selectDialog;

		ListStore store;
		TreeView  treeView;
		StringMatcher stringMatcher;
		DotNetProject configureProject;
		HashSet<string> selection = new HashSet<string> ();
		
		const int ColName = 0;
		const int ColPath = 1;
		const int ColProject = 2;
		const int ColSelected = 3;
		const int ColPixbuf = 4;
		const int ColVisible = 5;
		const int ColColor = 6;
		
		
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
			firstColumn.AddAttribute (tog_render, "active", ColSelected);
			firstColumn.AddAttribute (tog_render, "visible", ColVisible);

			secondColumn.Title = GettextCatalog.GetString ("Project");
			Gtk.CellRendererPixbuf pix_render = new Gtk.CellRendererPixbuf ();
			secondColumn.PackStart (pix_render, false);
			secondColumn.AddAttribute (pix_render, "pixbuf", ColPixbuf);
			
			CellRendererText text_render = new CellRendererText ();
			secondColumn.PackStart (text_render, true);
			secondColumn.AddAttribute (text_render, "markup", ColName);
			secondColumn.AddAttribute (text_render, "foreground", ColColor);
			
			treeView.AppendColumn (firstColumn);
			treeView.AppendColumn (secondColumn);
			treeView.AppendColumn (GettextCatalog.GetString ("Directory"), new CellRendererText (), "markup", ColPath);
			
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
			selection.Clear ();
			store.Clear ();
			this.configureProject = configureProject;
			PopulateListView ();
			Show ();
		}
		
		public void SetFilter (string filter)
		{
			if (!string.IsNullOrEmpty (filter))
				stringMatcher = StringMatcher.GetMatcher (filter, false);
			else
				stringMatcher = null;
			PopulateListView ();
		}
		
		public void AddReference(object sender, Gtk.ToggledArgs e)
		{
			Gtk.TreeIter iter;
			store.GetIterFromString (out iter, e.Path);
			Project project = (Project) store.GetValue (iter, 2);
			
			if ((bool)store.GetValue (iter, ColSelected) == false) {
				store.SetValue (iter, ColSelected, true);
				selectDialog.AddReference (new ProjectReference (project));
				
			} else {
				store.SetValue (iter, ColSelected, false);
				selectDialog.RemoveReference(ReferenceType.Project, project.Name);
			}
		}
		
		public void SignalRefChange (ProjectReference pref, bool newstate)
		{
			if (pref.ReferenceType != ReferenceType.Project)
				return;
			
			if (newstate)
				selection.Add (pref.Reference);
			else
				selection.Remove (pref.Reference);
			
			string refLoc = pref.Reference;
			Gtk.TreeIter looping_iter;
			if (!store.GetIterFirst (out looping_iter))
				return;

			do {
				Project project = (Project) store.GetValue (looping_iter, ColProject);
				if (project == null)
					return;
				if (project.Name == refLoc) {
					store.SetValue (looping_iter, ColSelected, newstate);
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
		
		void PopulateListView ()
		{
			store.Clear ();
			
			Solution openSolution = configureProject.ParentSolution;
			if (openSolution == null)
				return;
			
			Dictionary<DotNetProject,bool> references = new Dictionary<DotNetProject, bool> ();
			
			foreach (Project projectEntry in openSolution.GetAllSolutionItems<Project>()) {

				if (projectEntry == configureProject)
					continue;

				string txt;
				int matchRank = 0;
				
				if (stringMatcher != null) {
					if (!stringMatcher.CalcMatchRank (projectEntry.Name, out matchRank))
						continue;
					int[] match = stringMatcher.GetMatch (projectEntry.Name);
					txt = PackageReferencePanel.GetMatchMarkup (treeView, projectEntry.Name, match, 0);
				} else {
					txt = GLib.Markup.EscapeText (projectEntry.Name);
				}
	
				bool selected = selection.Contains (projectEntry.Name);
				bool allowSelecting = true;
				DotNetProject netProject = projectEntry as DotNetProject;
				if (netProject != null) {
					if (ProjectReferencesProject (references, null, netProject, configureProject.Name)) {
						txt += " " + GLib.Markup.EscapeText (GettextCatalog.GetString ("(Cyclic dependencies not allowed)"));
						allowSelecting = false;
					}
				    else if (!configureProject.TargetFramework.CanReferenceAssembliesTargetingFramework (netProject.TargetFramework)) {
						txt += " " + GLib.Markup.EscapeText (GettextCatalog.GetString ("(Incompatible target framework: {0})", netProject.TargetFramework.Id));
						allowSelecting = false;
					}
				}
				
				Gdk.Pixbuf icon = ImageService.GetPixbuf (projectEntry.StockIcon, IconSize.Menu);
				if (!allowSelecting) {
					// Don't show unselectable projects if there is a filter
					if (stringMatcher != null)
						continue;
					icon = ImageService.MakeTransparent (icon, 0.5);
				}
				Gtk.TreeIter it = store.AppendValues (txt, projectEntry.BaseDirectory.ToString (), projectEntry, selected, icon, allowSelecting);
				if (!allowSelecting)
					store.SetValue (it, ColColor, "dimgrey");
			}
		}
		
		internal static bool ProjectReferencesProject (Dictionary<DotNetProject,bool> references, HashSet<string> parentDeps,
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

