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
		private Gtk.ListStore selectedPackagesListStore = new Gtk.ListStore (typeof(string), typeof(string));
		private CProject project;
		private ProjectPackageCollection selectedPackages = new ProjectPackageCollection ();
		private ProjectPackageCollection projectPackages;
		
		public EditPackagesDialog(CProject project)
		{
			this.Build();
			
			this.project = project;
			
			selectedPackages.AddRange (project.Packages);
			
			Gtk.CellRendererText textRenderer = new Gtk.CellRendererText ();
			
			Gtk.CellRendererPixbuf pixbufRenderer = new Gtk.CellRendererPixbuf ();
			pixbufRenderer.StockId = "md-package";
			
			// <!-- Normal packages -->
			
			Gtk.CellRendererToggle normalPackageToggleRenderer = new Gtk.CellRendererToggle ();
			normalPackageToggleRenderer.Activatable = true;
			normalPackageToggleRenderer.Toggled += OnNormalPackageToggled;
			normalPackageToggleRenderer.Xalign = 0;
			
			Gtk.TreeViewColumn normalPackageColumn = new Gtk.TreeViewColumn ();
			normalPackageColumn.Title = "Package";
			normalPackageColumn.PackStart (pixbufRenderer, false);
			normalPackageColumn.PackStart (textRenderer, true);
			normalPackageColumn.AddAttribute (textRenderer, "text", 1);
			
			normalPackageTreeView.Model = normalPackageListStore;
			normalPackageTreeView.HeadersVisible = true;
			normalPackageTreeView.AppendColumn ("", normalPackageToggleRenderer, "active", 0);
			normalPackageTreeView.AppendColumn (normalPackageColumn);
			normalPackageTreeView.AppendColumn ("Version", textRenderer, "text", 2);
			
			// <!-- Project packages -->
			
			Gtk.CellRendererToggle projectPackageToggleRenderer = new Gtk.CellRendererToggle ();
			projectPackageToggleRenderer.Activatable = true;
			projectPackageToggleRenderer.Toggled += OnProjectPackageToggled;
			projectPackageToggleRenderer.Xalign = 0;
			
			Gtk.TreeViewColumn projectPackageColumn = new Gtk.TreeViewColumn ();
			projectPackageColumn.Title = "Package";
			projectPackageColumn.PackStart (pixbufRenderer, false);
			projectPackageColumn.PackStart (textRenderer, true);
			projectPackageColumn.AddAttribute (textRenderer, "text", 1);
			
			projectPackageTreeView.Model = projectPackageListStore;
			projectPackageTreeView.HeadersVisible = true;
			projectPackageTreeView.AppendColumn ("", projectPackageToggleRenderer, "active", 0);
			projectPackageTreeView.AppendColumn (projectPackageColumn);
			projectPackageTreeView.AppendColumn ("Version", textRenderer, "text", 2);
			
			
			// <!-- Selected packages -->
			
			Gtk.TreeViewColumn selectedPackageColumn = new Gtk.TreeViewColumn ();
			selectedPackageColumn.Title = "Package";
			selectedPackageColumn.PackStart (pixbufRenderer, false);
			selectedPackageColumn.PackStart (textRenderer, true);
			selectedPackageColumn.AddAttribute (textRenderer, "text", 0);
			
			selectedPackagesTreeView.Model = selectedPackagesListStore;
			selectedPackagesTreeView.HeadersVisible = true;
			selectedPackagesTreeView.AppendColumn (selectedPackageColumn);
			selectedPackagesTreeView.AppendColumn ("Version", textRenderer, "text", 1);
			
			// Fill up the project tree view
			projectPackages = ProjectPackages (project);
			
			foreach (ProjectPackage p in projectPackages) {
				if (p.Name == project.Name) continue;
				string version = GetPackageVersion (p.File);
				bool inProject = IsInProject (p.File);
				projectPackageListStore.AppendValues (inProject, p.Name, version);
				
				if (inProject)
					selectedPackagesListStore.AppendValues (p.Name, version);
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
						normalPackageListStore.AppendValues (inProject, name, version);
						
						if (inProject)
							selectedPackagesListStore.AppendValues (name, version);
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
		
		private string[] ScanDirs ()
		{
			List<string> dirs = new List<string> ();
			string pkg_var = Environment.GetEnvironmentVariable ("PKG_CONFIG_PATH");
			string[] pkg_paths;
			
			dirs.Add ("/usr/lib/pkgconfig");
			dirs.Add ("/usr/share/pkgconfig");
			dirs.Add ("/usr/local/lib/pkgconfig");
			dirs.Add ("/usr/local/share/pkgconfig");
			
			if (pkg_var == null) return dirs.ToArray ();
			
			pkg_paths = pkg_var.Split (':');
			
			foreach (string dir in pkg_paths) {
				if (!dirs.Contains (dir) && !string.IsNullOrEmpty (dir)) {
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
			
			selectedPackagesTreeView.Selection.GetSelected (out iter);
			
			if (!selectedPackagesListStore.IterIsValid (iter)) return;
			
			string package = (string)selectedPackagesListStore.GetValue (iter, 0);
			bool isProject = false;
			
			foreach (ProjectPackage p in selectedPackages) {
				if (p.Name == package) {
					isProject = p.IsProject;
					selectedPackages.Remove (p);
					break;
				}
			}
			
			selectedPackagesListStore.Remove (ref iter);
			
			if (!isProject) {
				Gtk.TreeIter search_iter;
				bool has_elem = normalPackageListStore.GetIterFirst (out search_iter);
					
				if (has_elem) {
					while (true) {
						string current = (string)normalPackageListStore.GetValue (search_iter, 1);
						
						if (current.Equals (package)) {
							normalPackageListStore.SetValue (search_iter, 0, false);
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
						string current = (string)projectPackageListStore.GetValue (search_iter, 1);
						
						if (current.Equals (package)) {
							projectPackageListStore.SetValue (search_iter, 0, false);
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
				old = (bool)normalPackageListStore.GetValue (iter, 0);
				normalPackageListStore.SetValue (iter, 0, !old);
			}
			
			name = (string)normalPackageListStore.GetValue (iter, 1);
			version = (string)normalPackageListStore.GetValue(iter, 2);
			
			if (old == false) {
				selectedPackagesListStore.AppendValues (name, version);
				selectedPackages.Add (new ProjectPackage (name));
			} else {
				Gtk.TreeIter search_iter;
				bool has_elem = selectedPackagesListStore.GetIterFirst (out search_iter);
				
				if (has_elem)
				{
					while (true) {
						string current = (string)selectedPackagesListStore.GetValue (search_iter, 0);
						
						if (current.Equals (name)) {
							selectedPackagesListStore.Remove (ref search_iter);
							foreach (ProjectPackage p in selectedPackages) {
								if (p.Name == name) {
									selectedPackages.Remove (p);
									break;
								}
							}
							
							break;
						}
						
						if (!selectedPackagesListStore.IterNext (ref search_iter))
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
				old = (bool)projectPackageListStore.GetValue (iter, 0);
				projectPackageListStore.SetValue (iter, 0, !old);
			}
			
			name = (string)projectPackageListStore.GetValue (iter, 1);
			version = (string)projectPackageListStore.GetValue(iter, 2);
			
			if (old == false) {
				selectedPackagesListStore.AppendValues (name, version);
				
				foreach (ProjectPackage p in projectPackages) {
					if (p.Name == name) {
						selectedPackages.Add (p);
						break;
					}
				}
			} else {
				Gtk.TreeIter search_iter;
				bool has_elem = selectedPackagesListStore.GetIterFirst (out search_iter);
				
				if (has_elem)
				{
					while (true) {
						string current = (string)selectedPackagesListStore.GetValue (search_iter, 0);
						
						if (current.Equals (name)) {
							selectedPackagesListStore.Remove (ref search_iter);
							foreach (ProjectPackage p in selectedPackages) {
								if (p.Name == name) {
									selectedPackages.Remove (p);
									break;
								}
							}
							
							break;
						}
						
						if (!selectedPackagesListStore.IterNext (ref search_iter))
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
				if (line.StartsWith ("Cflags:", true, null)) {
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
