//
// EditPackagesDialog.cs: Allows you to add and remove pkg-config packages to the project
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
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
using System.IO;
using System.Collections.Generic;

using Mono.Addins;

using MonoDevelop.Projects;

namespace CBinding
{
	public partial class EditPackagesDialog : Gtk.Dialog
	{
		private Gtk.ListStore normalPackageListStore = new Gtk.ListStore (typeof(bool), typeof(string), typeof(string));
		private Gtk.ListStore projectPackageListStore = new Gtk.ListStore (typeof(bool), typeof(string), typeof(string));
		private Gtk.ListStore selectedPackageListStore = new Gtk.ListStore (typeof(string), typeof(string));
		private CProject project;
		private ProjectPackageCollection selectedPackages = new ProjectPackageCollection ();
		private ProjectPackageCollection projectPackages;
		
		
		// Column IDs
		const int NormalPackageToggleID = 0;
		const int NormalPackageNameID = 1;
		const int NormalPackageVersionID = 2;
		
		const int ProjectPackageToggleID = 0;
		const int ProjectPackageNameID = 1;
		const int ProjectPackageVersionID = 2;
		
		const int SelectedPackageNameID = 0;
		const int SelectedPackageVersionID = 1;
		
		public EditPackagesDialog(CProject project)
		{
			this.Build();
			
			this.project = project;
			
			selectedPackages.AddRange (project.Packages);
			
			Gtk.CellRendererText textRenderer = new Gtk.CellRendererText ();
			
			Gtk.CellRendererPixbuf pixbufRenderer = new Gtk.CellRendererPixbuf ();
			pixbufRenderer.StockId = "md-package";
			
			normalPackageListStore.DefaultSortFunc = NormalPackageCompareNodes;
			projectPackageListStore.DefaultSortFunc = ProjectPackageCompareNodes;
			selectedPackageListStore.DefaultSortFunc = SelectedPackageCompareNodes;
			
			normalPackageListStore.SetSortColumnId (NormalPackageNameID, Gtk.SortType.Ascending);
			projectPackageListStore.SetSortColumnId (ProjectPackageNameID, Gtk.SortType.Ascending);
			selectedPackageListStore.SetSortColumnId (SelectedPackageNameID, Gtk.SortType.Ascending);
			
			normalPackageTreeView.SearchColumn = NormalPackageNameID;
			projectPackageTreeView.SearchColumn = ProjectPackageNameID;
			selectedPackageTreeView.SearchColumn = SelectedPackageNameID;
			
			// <!-- Normal packages -->
			
			Gtk.CellRendererToggle normalPackageToggleRenderer = new Gtk.CellRendererToggle ();
			normalPackageToggleRenderer.Activatable = true;
			normalPackageToggleRenderer.Toggled += OnNormalPackageToggled;
			normalPackageToggleRenderer.Xalign = 0;
			
			Gtk.TreeViewColumn normalPackageColumn = new Gtk.TreeViewColumn ();
			normalPackageColumn.Title = "Package";
			normalPackageColumn.PackStart (pixbufRenderer, false);
			normalPackageColumn.PackStart (textRenderer, true);
			normalPackageColumn.AddAttribute (textRenderer, "text", NormalPackageNameID);
			
			normalPackageTreeView.Model = normalPackageListStore;
			normalPackageTreeView.HeadersVisible = true;
			normalPackageTreeView.AppendColumn ("", normalPackageToggleRenderer, "active", NormalPackageToggleID);
			normalPackageTreeView.AppendColumn (normalPackageColumn);
			normalPackageTreeView.AppendColumn ("Version", textRenderer, "text", NormalPackageVersionID);
			
			// <!-- Project packages -->
			
			Gtk.CellRendererToggle projectPackageToggleRenderer = new Gtk.CellRendererToggle ();
			projectPackageToggleRenderer.Activatable = true;
			projectPackageToggleRenderer.Toggled += OnProjectPackageToggled;
			projectPackageToggleRenderer.Xalign = 0;
			
			Gtk.TreeViewColumn projectPackageColumn = new Gtk.TreeViewColumn ();
			projectPackageColumn.Title = "Package";
			projectPackageColumn.PackStart (pixbufRenderer, false);
			projectPackageColumn.PackStart (textRenderer, true);
			projectPackageColumn.AddAttribute (textRenderer, "text", ProjectPackageNameID);
			
			projectPackageTreeView.Model = projectPackageListStore;
			projectPackageTreeView.HeadersVisible = true;
			projectPackageTreeView.AppendColumn ("", projectPackageToggleRenderer, "active", ProjectPackageToggleID);
			projectPackageTreeView.AppendColumn (projectPackageColumn);
			projectPackageTreeView.AppendColumn ("Version", textRenderer, "text", ProjectPackageVersionID);
			
			
			// <!-- Selected packages -->
			
			Gtk.TreeViewColumn selectedPackageColumn = new Gtk.TreeViewColumn ();
			selectedPackageColumn.Title = "Package";
			selectedPackageColumn.PackStart (pixbufRenderer, false);
			selectedPackageColumn.PackStart (textRenderer, true);
			selectedPackageColumn.AddAttribute (textRenderer, "text", SelectedPackageNameID);
			
			selectedPackageTreeView.Model = selectedPackageListStore;
			selectedPackageTreeView.HeadersVisible = true;
			selectedPackageTreeView.AppendColumn (selectedPackageColumn);
			selectedPackageTreeView.AppendColumn ("Version", textRenderer, "text", SelectedPackageVersionID);
			
			// Fill up the project tree view
			projectPackages = ProjectPackages (project);
			
			foreach (ProjectPackage p in projectPackages) {
				if (p.Name == project.Name) continue;
				string version = GetPackageVersion (p.File);
				bool inProject = IsInProject (p.File);

				if (!IsPackageInStore (projectPackageListStore, p.Name, version, ProjectPackageNameID, ProjectPackageVersionID)) {
				    projectPackageListStore.AppendValues (inProject, p.Name, version);
				
					if (inProject)
						selectedPackageListStore.AppendValues (p.Name, version);
				}
			}
			
			// Fill up the normal tree view
			foreach (string dir in ScanDirs ()) {
				if (Directory.Exists (dir)) {	
					DirectoryInfo di = new DirectoryInfo (dir);
					FileInfo[] availablePackages = di.GetFiles ("*.pc");
					
					foreach (FileInfo f in availablePackages) {
						if (!IsValidPackage (f.FullName)) { 
							continue;
						}
						string name = f.Name.Substring (0, f.Name.LastIndexOf ('.'));
						string version = GetPackageVersion (f.FullName);
						bool inProject = IsInProject (name);
						
						if (!IsPackageInStore (normalPackageListStore, name, version, NormalPackageNameID, NormalPackageVersionID)) {
							normalPackageListStore.AppendValues (inProject, name, version);
						
							if (inProject)
								selectedPackageListStore.AppendValues (name, version);
						}
					}
				}
			}
		}
		
		private ProjectPackageCollection ProjectPackages (Project project)
		{
			ProjectPackageCollection packages = new ProjectPackageCollection ();
			
			foreach (CombineEntry c in project.ParentCombine.Entries) {
				if (c is CProject) {
					CProject cproj = (CProject)c;
					CProjectConfiguration conf = (CProjectConfiguration)cproj.ActiveConfiguration;
					if (conf.CompileTarget != CBinding.CompileTarget.Bin) {
						cproj.WriteMDPkgPackage ();
						packages.Add (new ProjectPackage (cproj));
					}
				}
			}
			
			return packages;
		}
		
		private bool IsPackageInStore (Gtk.ListStore store, string pname, string pversion, int pname_col, int pversion_col)
		{
			Gtk.TreeIter search_iter;
			bool has_elem = store.GetIterFirst (out search_iter);
				
			if (has_elem) {
				while (true) {
					string name = (string)store.GetValue (search_iter, pname_col);
					string version = (string)store.GetValue (search_iter, pversion_col);
					
					if (name == pname && version == pversion)
						return true;
						
					if (!store.IterNext (ref search_iter))
						break;
				}
			}
			
			return false;
		}
		
		private string[] ScanDirs ()
		{
			List<string> dirs = new List<string> ();
			string pkg_var = Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH");
			string[] pkg_paths;
			
			dirs.Add ("/usr/lib/pkgconfig");
			dirs.Add ("/usr/lib64/pkgconfig");
			dirs.Add ("/usr/share/pkgconfig");
			dirs.Add ("/usr/local/lib/pkgconfig");
			dirs.Add ("/usr/local/share/pkgconfig");
			
			if (pkg_var == null) return dirs.ToArray ();
			
			pkg_paths = pkg_var.Split (':');
			
			foreach (string dir in pkg_paths) {
				if (string.IsNullOrEmpty (dir))
					continue;
				string dirPath = System.IO.Path.GetFullPath (dir);
				if (!dirs.Contains (dirPath) && !string.IsNullOrEmpty (dir)) {
					dirs.Add (dir);
				}
			}
			
			return dirs.ToArray ();
		}
		
		private void OnOkButtonClick (object sender, EventArgs e)
		{
			// Use this instead of clear, since clear seems to not update the packages tree
			while (project.Packages.Count > 0) {
				project.Packages.RemoveAt (0);
			}

			project.Packages.AddRange (selectedPackages);
			
			Destroy ();
		}
		
		private void OnCancelButtonClick (object sender, EventArgs e)
		{
			Destroy ();
		}
		
		private void OnRemoveButtonClick (object sender, EventArgs e)
		{
			Gtk.TreeIter iter;
			
			selectedPackageTreeView.Selection.GetSelected (out iter);
			
			if (!selectedPackageListStore.IterIsValid (iter)) return;
			
			string package = (string)selectedPackageListStore.GetValue (iter, SelectedPackageNameID);
			bool isProject = false;
			
			foreach (ProjectPackage p in selectedPackages) {
				if (p.Name == package) {
					isProject = p.IsProject;
					selectedPackages.Remove (p);
					break;
				}
			}
			
			selectedPackageListStore.Remove (ref iter);
			
			if (!isProject) {
				Gtk.TreeIter search_iter;
				bool has_elem = normalPackageListStore.GetIterFirst (out search_iter);
					
				if (has_elem) {
					while (true) {
						string current = (string)normalPackageListStore.GetValue (search_iter, NormalPackageNameID);
						
						if (current.Equals (package)) {
							normalPackageListStore.SetValue (search_iter, NormalPackageToggleID, false);
							break;
						}
						
						if (!normalPackageListStore.IterNext (ref search_iter))
							break;
					}
				}
			} else {
				Gtk.TreeIter search_iter;
				bool has_elem = projectPackageListStore.GetIterFirst (out search_iter);
					
				if (has_elem) {
					while (true) {
						string current = (string)projectPackageListStore.GetValue (search_iter, ProjectPackageNameID);
						
						if (current.Equals (package)) {
							projectPackageListStore.SetValue (search_iter, ProjectPackageToggleID, false);
							break;
						}
						
						if (!projectPackageListStore.IterNext (ref search_iter))
							break;
					}
				}
			}
		}
		
		private void OnNormalPackageToggled (object sender, Gtk.ToggledArgs args)
		{
			Gtk.TreeIter iter;
			bool old = true;
			string name;
			string version;

			if (normalPackageListStore.GetIter (out iter, new Gtk.TreePath (args.Path))) {
				old = (bool)normalPackageListStore.GetValue (iter, NormalPackageToggleID);
				normalPackageListStore.SetValue (iter, NormalPackageToggleID, !old);
			}
			
			name = (string)normalPackageListStore.GetValue (iter, NormalPackageNameID);
			version = (string)normalPackageListStore.GetValue(iter, NormalPackageVersionID);
			
			if (old == false) {
				selectedPackageListStore.AppendValues (name, version);
				selectedPackages.Add (new ProjectPackage (name));
			} else {
				Gtk.TreeIter search_iter;
				bool has_elem = selectedPackageListStore.GetIterFirst (out search_iter);
				
				if (has_elem) {
					while (true) {
						string current = (string)selectedPackageListStore.GetValue (search_iter, SelectedPackageNameID);
						
						if (current.Equals (name)) {
							selectedPackageListStore.Remove (ref search_iter);
							foreach (ProjectPackage p in selectedPackages) {
								if (p.Name == name) {
									selectedPackages.Remove (p);
									break;
								}
							}
							
							break;
						}
						
						if (!selectedPackageListStore.IterNext (ref search_iter))
							break;
					}
				}
			}
		}
		
		private void OnProjectPackageToggled (object sender, Gtk.ToggledArgs args)
		{
			Gtk.TreeIter iter;
			bool old = true;
			string name;
			string version;

			if (projectPackageListStore.GetIter (out iter, new Gtk.TreePath (args.Path))) {
				old = (bool)projectPackageListStore.GetValue (iter, ProjectPackageToggleID);
				projectPackageListStore.SetValue (iter, ProjectPackageToggleID, !old);
			}
			
			name = (string)projectPackageListStore.GetValue (iter, ProjectPackageNameID);
			version = (string)projectPackageListStore.GetValue(iter, ProjectPackageVersionID);
			
			if (old == false) {
				selectedPackageListStore.AppendValues (name, version);
				
				foreach (ProjectPackage p in projectPackages) {
					if (p.Name == name) {
						selectedPackages.Add (p);
						break;
					}
				}
			} else {
				Gtk.TreeIter search_iter;
				bool has_elem = selectedPackageListStore.GetIterFirst (out search_iter);
				
				if (has_elem)
				{
					while (true) {
						string current = (string)selectedPackageListStore.GetValue (search_iter, SelectedPackageNameID);
						
						if (current.Equals (name)) {
							selectedPackageListStore.Remove (ref search_iter);
							foreach (ProjectPackage p in selectedPackages) {
								if (p.Name == name) {
									selectedPackages.Remove (p);
									break;
								}
							}
							
							break;
						}
						
						if (!selectedPackageListStore.IterNext (ref search_iter))
							break;
					}
				}
			}
		}
		
		private string GetPackageVersion (string package)
		{
			StreamReader reader = new StreamReader (package);
			
			string line;
			string version = string.Empty;
			
			while ((line = reader.ReadLine ()) != null) {
				if (line.StartsWith ("Version:", true, null)) {
					version = line.Split(':')[1].TrimStart ();
				}
			}
			
			reader.Close ();
			
			return version;
		}
		
		private bool IsValidPackage (string package)
		{
			bool valid = false;
			StreamReader reader = new StreamReader (package);
			
			string line;
			
			while ((line = reader.ReadLine ()) != null) {
				if (line.StartsWith ("Libs:", true, null) && line.Contains (" -l")) {
					valid = true;
					break;
				}
			}
			reader.Close ();
			
			return valid;
		}
		
		private bool IsInProject (string package)
		{
			bool exists = false;
			
			foreach (ProjectPackage p in project.Packages) {
				if (package.Equals (p.File)) {
					exists = true;
					break;
				}
			}
			
			return exists;
		}
		
		int NormalPackageCompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			string name1 = (string)model.GetValue (a, NormalPackageNameID);
			string name2 = (string)model.GetValue (b, NormalPackageNameID);
			return string.Compare (name1, name2, true);
		}
		
		int ProjectPackageCompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			string name1 = (string)model.GetValue (a, ProjectPackageNameID);
			string name2 = (string)model.GetValue (b, ProjectPackageNameID);
			return string.Compare (name1, name2, true);
		}
		
		int SelectedPackageCompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			string name1 = (string)model.GetValue (a, SelectedPackageNameID);
			string name2 = (string)model.GetValue (b, SelectedPackageNameID);
			return string.Compare (name1, name2, true);
		}

		protected virtual void OnSelectedPackagesTreeViewCursorChanged (object sender, System.EventArgs e)
		{
			removeButton.Sensitive = true;
		}

		protected virtual void OnRemoveButtonClicked (object sender, System.EventArgs e)
		{
			removeButton.Sensitive = false;
		}
	}
}
