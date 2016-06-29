// 
// IncludeNewFilesDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Projects
{
	partial class IncludeNewFilesDialog : Gtk.Dialog
	{
		TreeStore store = new TreeStore (typeof (Xwt.Drawing.Image), typeof (Xwt.Drawing.Image), typeof (string),
			typeof (string), typeof (bool));

		FilePath baseDirectory;
		
		static class Columns {
			public static int IconOpened = 0;
			public static int IconClosed = 1;
			public static int Text       = 2;
			public static int FileName   = 3;
			public static int IsToggled  = 4;
		}
		
		public IncludeNewFilesDialog (string title, FilePath baseDirectory)
		{
			this.Build ();
			this.Title = title;
			this.baseDirectory = baseDirectory;
			
			treeviewFiles.Model = store;
			treeviewFiles.SearchColumn = -1; // disable the interactive search

			treeviewFiles.HeadersVisible = false; // Headers are untranslated because they're hidden as default
			
			TreeViewColumn textColumn = new TreeViewColumn ();
			
			CellRendererToggle toggleRender = new CellRendererToggle ();
			toggleRender.Toggled += ToggleRenderToggled;
			textColumn.PackStart (toggleRender, false);
			textColumn.AddAttribute (toggleRender, "active", Columns.IsToggled);
			
			textColumn.Title = "Name";
			var pixbufRenderer = new CellRendererImage ();
			textColumn.PackStart (pixbufRenderer, false);
			textColumn.AddAttribute (pixbufRenderer, "image", Columns.IconOpened);
			textColumn.AddAttribute (pixbufRenderer, "image-expander-open", Columns.IconOpened);
			textColumn.AddAttribute (pixbufRenderer, "image-expander-closed", Columns.IconClosed);
			
			CellRendererText textRenderer = new CellRendererText ();
			textColumn.PackStart (textRenderer, false);
			textColumn.AddAttribute (textRenderer, "text", Columns.Text);
			treeviewFiles.AppendColumn (textColumn);
			buttonExcludeAll.Clicked += ButtonExcludeAllClicked;
			buttonIncludeAll.Clicked += ButtonIncludeAllClicked;
			buttonOk.Clicked += ButtonOkClicked;
		}
		
		public List<FilePath> SelectedFiles { get; set; }
		public List<FilePath> IgnoredFiles { get; set; }
		
		void ButtonOkClicked (object sender, EventArgs e)
		{
			SelectedFiles = new List<FilePath> ();
			IgnoredFiles = new List<FilePath> ();
			
			TraverseSubtree (TreeIter.Zero, delegate (TreeIter currentIter) {
				bool isToggled = (bool) store.GetValue (currentIter, Columns.IsToggled);
				string fileName = (string) store.GetValue (currentIter, Columns.FileName);
				if (fileName != null) {
					if (isToggled) {
						SelectedFiles.Add (fileName);
					} else {
						IgnoredFiles.Add (fileName);
					}
				}
			});
		}
		
		void ButtonIncludeAllClicked (object sender, EventArgs e)
		{
			SetSubtreeIsToggled (TreeIter.Zero, true);
		}
		
		void ButtonExcludeAllClicked (object sender, EventArgs e)
		{
			SetSubtreeIsToggled (TreeIter.Zero, false);
		}
		
		void ToggleRenderToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString(out iter, args.Path)) {
				bool isToggled = !(bool)store.GetValue (iter, Columns.IsToggled);
				store.SetValue (iter, Columns.IsToggled, isToggled);
				if (store.IterHasChild (iter)) 
					SetSubtreeIsToggled (iter, isToggled);
			}
		}
		
		void TraverseSubtree (TreeIter iter, Action<TreeIter> action)
		{
			TreeIter newIter;
			if (!iter.Equals (TreeIter.Zero)) {
				if (!store.IterChildren (out newIter, iter))
					return;
			} else {
				if (!store.GetIterFirst (out newIter))
					return;
			}
			do {
				action (newIter);
				if (store.IterHasChild (newIter))
					TraverseSubtree (newIter, action);
			} while (store.IterNext (ref newIter));
		}
		
		void SetSubtreeIsToggled (TreeIter iter, bool isToggled)
		{
			TraverseSubtree (iter, delegate (TreeIter newIter) {
				store.SetValue (newIter, Columns.IsToggled, isToggled);
			});
		}
		
		public void AddFiles (IEnumerable<string> newFiles)
		{
			foreach (string fileName in newFiles) {
				AddFile (fileName);
			}
			treeviewFiles.ExpandAll ();
		}
		
		TreeIter SearchPath (string path)
		{
			TreeIter iter;
			if (store.IterChildren (out iter) && SearchSibling (ref iter, path))
				return iter;
			return store.AppendValues (GetFolderValues (path));
		}
		
		
		TreeIter SearchPath (TreeIter parent, string path)
		{
			TreeIter iter;
			if (store.IterChildren (out iter, parent) && SearchSibling (ref iter, path))
				return iter;
			return store.AppendValues (parent, GetFolderValues (path));
		}

		bool SearchSibling (ref TreeIter iter, string name)
		{
			do {
				string val = (string)store.GetValue (iter, Columns.Text);
				if (name == val)
					return true;
			} while (store.IterNext (ref iter));
			return false;
		}
		
		object[] GetFolderValues (string name)
		{
			return new object[] { ImageService.GetIcon (MonoDevelop.Ide.Gui.Stock.OpenFolder, IconSize.Menu),
				ImageService.GetIcon (MonoDevelop.Ide.Gui.Stock.ClosedFolder, IconSize.Menu), name, null, false };
		}

		TreeIter GetPath (string fullPath)
		{
			if (string.IsNullOrEmpty (fullPath))
				return TreeIter.Zero;
			string[] paths = fullPath.Split (System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
			var iter = SearchPath (paths[0]);
			for (int i = 1; i < paths.Length; i++)
				iter = SearchPath (iter, paths[i]);
			return iter;
		}
		
		void AddFile (FilePath filePath)
		{
			var relativePath = filePath.ToRelative (baseDirectory);
			TreeIter iter = GetPath (relativePath.ParentDirectory);
			object[] values = new object[] {
				//FIXME: look these pixbufs up lazily in the renderer
				DesktopService.GetIconForFile (filePath, IconSize.Menu),
				null,
				filePath.FileName,
				filePath.ToString (),
				false
			};
			if (iter.Equals (TreeIter.Zero)) {
				store.AppendValues (values);
				return;
			}
			store.AppendValues (iter, values);
		}
	}
}
