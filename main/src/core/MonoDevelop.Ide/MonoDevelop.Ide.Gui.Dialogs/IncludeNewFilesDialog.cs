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
using MonoDevelop.Core.Gui;
using System.IO;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class IncludeNewFilesDialog : Gtk.Dialog
	{
		TreeStore store;
		Project project;
		
		static class Columns {
			public static int IconOpened = 0;
			public static int IconClosed = 1;
			public static int Text       = 2;
			public static int FileName   = 3;
			public static int IsToggled  = 4;
		}
		
		public IncludeNewFilesDialog (Project project)
		{
			this.Build ();
			this.Title = GettextCatalog.GetString ("Found new files in {0}", project.Name);
			this.project = project;
			store = new TreeStore (typeof (Gdk.Pixbuf), // Image
			                       typeof (Gdk.Pixbuf), // Image - closed
			                       typeof (string), // Text
			                       typeof (string), // FileName
			                       typeof (bool)    // IsToggled
			                      );
			treeviewFiles.Model = store;
			
			treeviewFiles.HeadersVisible = false; // Headers are untranslated because they're hidden as default
			
			TreeViewColumn textColumn = new TreeViewColumn ();
			
			CellRendererToggle toggleRender = new CellRendererToggle ();
			toggleRender.Toggled += ToggleRenderToggled;
			textColumn.PackStart (toggleRender, false);
			textColumn.AddAttribute (toggleRender, "active", Columns.IsToggled);
			
			textColumn.Title = "Name";
			CellRendererPixbuf pixbufRenderer = new CellRendererPixbuf ();
			textColumn.PackStart (pixbufRenderer, false);
			textColumn.AddAttribute (pixbufRenderer, "pixbuf", Columns.IconOpened);
			textColumn.AddAttribute (pixbufRenderer, "pixbuf-expander-open", Columns.IconOpened);
			textColumn.AddAttribute (pixbufRenderer, "pixbuf-expander-closed", Columns.IconClosed);
			
			CellRendererText textRenderer = new CellRendererText ();
			textColumn.PackStart (textRenderer, false);
			textColumn.AddAttribute (textRenderer, "text", Columns.Text);
			treeviewFiles.AppendColumn (textColumn);
			buttonExcludeAll.Clicked += ButtonExcludeAllClicked;
			buttonIncludeAll.Clicked += ButtonIncludeAllClicked;
			buttonOk.Clicked += ButtonOkClicked;
		}
		
		void ButtonOkClicked (object sender, EventArgs e)
		{
			TraverseSubtree (TreeIter.Zero, delegate (TreeIter currentIter) {
				bool isToggled = (bool) store.GetValue (currentIter, Columns.IsToggled);
				string fileName = (string) store.GetValue (currentIter, Columns.FileName);
				if (isToggled) {
					project.AddFile (fileName);
				} else {
					ProjectFile projectFile = project.AddFile (fileName, BuildAction.None);
					if (projectFile != null)
						projectFile.Visible = false;
				}
			});
			IdeApp.ProjectOperations.Save (project);
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
			if (store.IterIsValid (iter)) {
				store.IterNthChild (out newIter, iter, 0);
			} else {
				store.IterNthChild (out newIter, 0);
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
		
		TreeIter SearchPath (TreeIter parent, string path)
		{
			TreeIter iter;
			if (store.IterChildren (out iter, parent)) {
				do {
					string name = (string)store.GetValue (iter, Columns.Text);
					if (name == path)
						return iter;
				} while (store.IterNext (ref iter));
			}
			return store.AppendValues (parent, ImageService.GetPixbuf (MonoDevelop.Core.Gui.Stock.OpenFolder, IconSize.Menu), ImageService.GetPixbuf (MonoDevelop.Core.Gui.Stock.ClosedFolder, IconSize.Menu), path, null, false);
		}
		
		TreeIter GetPath (string fullPath)
		{
			if (string.IsNullOrEmpty (fullPath))
				return TreeIter.Zero;
			string[] paths = fullPath.Split (System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
			TreeIter iter;
			if (!store.GetIterFirst (out iter)) 
				return TreeIter.Zero;
			for (int i = 0; i < paths.Length; i++) {
				iter = SearchPath (iter, paths[i]);
			}
			return iter;
		}
		
		void AddFile (string fileName)
		{
			string relativePath = FileService.AbsoluteToRelativePath (project.BaseDirectory, fileName);
			TreeIter iter = GetPath (System.IO.Path.GetDirectoryName (relativePath));
			object[] values = new object[] {
				IdeApp.Services.PlatformService.GetPixbufForFile (fileName, IconSize.Menu),
				null,
				System.IO.Path.GetFileName (fileName),
				fileName,
				false
			};
			if (!store.IterIsValid (iter)) {
				store.AppendValues (values);
				return;
			}
			store.AppendValues (iter, values);
		}
	}
}
