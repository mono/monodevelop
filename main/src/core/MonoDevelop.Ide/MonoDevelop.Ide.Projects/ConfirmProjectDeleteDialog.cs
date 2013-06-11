// 
// ConfirmProjectDeleteDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gtk;


namespace MonoDevelop.Ide.Projects
{
	public partial class ConfirmProjectDeleteDialog : Gtk.Dialog
	{
		TreeStore store;
		IWorkspaceFileObject item;
		Dictionary<FilePath, TreeIter> paths = new Dictionary<FilePath, TreeIter> ();
		
		enum ChildInfo {
			None = 0,
			AllSelected = 1,
			SomeSelected = 2,
			HasProjectFiles = 4
		}
		
		static IList<string> knownExtensions = new string [] {
			".pidb", ".userprefs", ".usertasks"
		};
		static IList<string> knownSubdirs = new string [] {
			"bin"
		};
		
		public ConfirmProjectDeleteDialog (IWorkspaceFileObject item)
		{
			this.Build ();
			this.item = item;
			
			store = new TreeStore (typeof(bool), typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string));
			fileList.Model = store;
			
			TreeViewColumn col = new TreeViewColumn ();
			
			CellRendererToggle crt = new CellRendererToggle ();
			crt.Toggled += CrtToggled;
			col.PackStart (crt, false);
			col.AddAttribute (crt, "active", 0);
			
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "pixbuf", 1);
			
			CellRendererText cre = new CellRendererText ();
			col.PackStart (cre, true);
			col.AddAttribute (cre, "text", 2);
			col.AddAttribute (cre, "foreground", 4);
			
			fileList.AppendColumn (col);
			store.SetSortColumnId (2, SortType.Ascending);
			
			labelProjectDir.Text = item.BaseDirectory.FullPath;
			
			HashSet<string> itemFiles = new HashSet<string> ();
			HashSet<string> knownPaths = new HashSet<string> ();
			
			foreach (FilePath file in item.GetItemFiles (true)) {
				itemFiles.Add (file.FullPath);
				knownPaths.Add (file.FullPath + "~");
			}
			
			foreach (string ext in knownExtensions)
				knownPaths.Add (item.FileName.ChangeExtension (ext));

			FillDirRec (TreeIter.Zero, item, itemFiles, knownPaths, item.BaseDirectory, false);
			
			if (item.BaseDirectory != item.ItemDirectory) {
				// If the project has a custom base directory, make sure the project files
				// from the item directory are shown in the list
				foreach (FilePath f in item.GetItemFiles (false)) {
					if (!f.IsChildPathOf (item.BaseDirectory)) {
						Gdk.Pixbuf pix = DesktopService.GetPixbufForFile (f, IconSize.Menu);
						paths [f] = store.AppendValues (true, pix, f.FileName, f.ToString ());
					}
				}
			}

			if (item is SolutionItem) {
				var sol = ((SolutionItem)item).ParentSolution;
				var bdir = item.BaseDirectory;
				if (sol.GetItemFiles (false).Any (f => f.IsChildPathOf (bdir)) || sol.GetAllSolutionItems<SolutionEntityItem> ().Any (it => it != item && it.GetItemFiles (true).Any (f => f.IsChildPathOf (bdir)))) {
					radioDeleteAll.Sensitive = false;
					labelProjectDir.Text = GettextCatalog.GetString ("Project directory can't be deleted since it contains files from other projects or solutions");
				}
			}
			
			if (item.BaseDirectory.FileName == item.Name && radioDeleteAll.Sensitive) {
				radioDeleteAll.Active = true;
				fileList.Sensitive = false;
			}
			else {
				radioDeleteSel.Active = true;
				Focus = radioDeleteSel;
			}
		}
		
		public List<FilePath> GetFilesToDelete ()
		{
			List<FilePath> files = new List<FilePath> ();
			
			if (radioDeleteAll.Active) {
				files.Add (item.BaseDirectory);
				if (item.BaseDirectory != item.ItemDirectory) {
					foreach (FilePath f in item.GetItemFiles (false)) {
						if (!f.IsChildPathOf (item.BaseDirectory))
							files.Add (f);
					}
				}
				return files;
			}
			
			foreach (Gtk.TreeIter it in paths.Values) {
				if ((bool) store.GetValue (it, 0))
					files.Add ((string) store.GetValue (it, 3));
			}
			
			// If a directory is selected, remove all files of that directory,
			// since the dir will be deleted as a whole
			
			List<FilePath> cleaned = new List<FilePath> (files);
			foreach (FilePath path in files) {
				if (Directory.Exists (path)) {
					for (int n=0; n<cleaned.Count; n++) {
						if (cleaned [n].IsChildPathOf (path)) {
							cleaned.RemoveAt (n);
							n--;
						}
					}
				}
			}
			return cleaned;
		}
		
		ChildInfo FillDirRec (Gtk.TreeIter iter, IWorkspaceFileObject item, HashSet<string> itemFiles, HashSet<string> knownPaths, FilePath dir, bool forceSet)
		{
			ChildInfo cinfo = ChildInfo.AllSelected;
			bool hasChildren = false;
			
			foreach (string sd in knownSubdirs) {
				if (dir == item.BaseDirectory.Combine (sd)) {
					forceSet = true;
					break;
				}
			}
			
			TreeIter dit;
			if (!iter.Equals (TreeIter.Zero)) {
				dit = store.AppendValues (iter, false, DesktopService.GetPixbufForFile (dir, IconSize.Menu), dir.FileName.ToString (), dir.ToString ());
				fileList.ExpandRow (store.GetPath (iter), false);
			}
			else
				dit = store.AppendValues (false, DesktopService.GetPixbufForFile (dir, IconSize.Menu), dir.FileName.ToString (), dir.ToString ());
			
			paths [dir] = dit;
			
			foreach (string file in Directory.GetFiles (dir)) {
				string path = System.IO.Path.GetFileName (file);
				Gdk.Pixbuf pix = DesktopService.GetPixbufForFile (file, IconSize.Menu);
				bool active = itemFiles.Contains (file);
				string color = null;
				if (!active) {
					pix = ImageService.MakeTransparent (pix, 0.5);
					color = "dimgrey";
				} else
					cinfo |= ChildInfo.HasProjectFiles;
				
				active = active || forceSet || knownPaths.Contains (file);
				if (!active)
					cinfo &= ~ChildInfo.AllSelected;
				else
					cinfo |= ChildInfo.SomeSelected;

				paths [file] = store.AppendValues (dit, active, pix, path, file, color);
				if (!hasChildren) {
					hasChildren = true;
					fileList.ExpandRow (store.GetPath (dit), false);
				}
			}
			foreach (string cdir in Directory.GetDirectories (dir)) {
				hasChildren = true;
				ChildInfo ci = FillDirRec (dit, item, itemFiles, knownPaths, cdir, forceSet);
				if ((ci & ChildInfo.AllSelected) == 0)
					cinfo &= ~ChildInfo.AllSelected;
				cinfo |= ci & (ChildInfo.SomeSelected | ChildInfo.HasProjectFiles);
			}
			if ((cinfo & ChildInfo.AllSelected) != 0 && hasChildren)
				store.SetValue (dit, 0, true);
			if ((cinfo & ChildInfo.HasProjectFiles) == 0) {
				Gdk.Pixbuf pix = DesktopService.GetPixbufForFile (dir, IconSize.Menu);
				pix = ImageService.MakeTransparent (pix, 0.5);
				store.SetValue (dit, 1, pix);
				store.SetValue (dit, 4, "dimgrey");
			}
			if ((cinfo & ChildInfo.SomeSelected) != 0 && (cinfo & ChildInfo.AllSelected) == 0) {
				fileList.ExpandRow (store.GetPath (dit), false);
			} else {
				fileList.CollapseRow (store.GetPath (dit));
			}
			return cinfo;
		}

		void CrtToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;
			
			bool currentVal = !(bool) store.GetValue (iter, 0);
			string path = (string) store.GetValue (iter, 3);
			
			store.SetValue (iter, 0, currentVal);
			
			if (Directory.Exists (path))
				SelectWholeDirectory (path, currentVal);
			
			UpdateDirectoryToggle (System.IO.Path.GetDirectoryName (path));
		}
		
		void SelectWholeDirectory (string path, bool sel)
		{
			FilePath basePath = path;
			foreach (Gtk.TreeIter it in paths.Values) {
				FilePath cpath = (string) store.GetValue (it, 3);
				if (cpath.IsChildPathOf (basePath))
					store.SetValue (it, 0, sel);
			}
		}
		
		void UpdateDirectoryToggle (string path)
		{
			bool allChildrenSet = true;
			FilePath basePath = path;
			Gtk.TreeIter itDir = Gtk.TreeIter.Zero;
			foreach (Gtk.TreeIter it in paths.Values) {
				FilePath cpath = (string) store.GetValue (it, 3);
				if (cpath == basePath)
					itDir = it;
				else if (cpath.IsChildPathOf (basePath) && !(bool)store.GetValue (it, 0))
					allChildrenSet = false;
			}
			
			if (!itDir.Equals (TreeIter.Zero)) {
				if (allChildrenSet != (bool) store.GetValue (itDir, 0)) {
					store.SetValue (itDir, 0, allChildrenSet);
					UpdateDirectoryToggle (basePath.ParentDirectory);
				}
			}
		}

		protected virtual void OnRadioDeleteAllToggled (object sender, System.EventArgs e)
		{
			fileList.Sensitive = radioDeleteSel.Active;
		}
	}
}
