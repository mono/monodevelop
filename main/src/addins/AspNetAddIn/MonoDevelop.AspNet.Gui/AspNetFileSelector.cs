// 
// AspNetFileSelector.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui;

namespace MonoDevelop.AspNet.Gui
{
	
	
	public partial class AspNetFileSelector : Gtk.Dialog
	{
		List<string> filters;
		string defaultFilterName;
		string defaultFilterPattern;
		AspNetAppProject project;
		TreeStore dirStore = new TreeStore (typeof (object), typeof (Boolean));
		ListStore fileStore = new ListStore (typeof (ProjectFile));
		
		Gdk.Pixbuf openFolder;
		Gdk.Pixbuf closedFolder;
		
		public AspNetFileSelector (AspNetAppProject project)
			: this (project, GettextCatalog.GetString ("All files"), "*")
		{
		}
		
		public AspNetFileSelector (AspNetAppProject project, string defaultFilterName, string defaultFilterPattern)
		{
			this.project = project;
			this.defaultFilterName = defaultFilterName;
			this.defaultFilterPattern = defaultFilterPattern ?? "*";
			
			this.Build();
			
			openFolder = MonoDevelop.Core.Gui.Services.Resources.GetIcon (MonoDevelop.Core.Gui.Stock.OpenFolder);
			closedFolder = MonoDevelop.Core.Gui.Services.Resources.GetIcon (MonoDevelop.Core.Gui.Stock.ClosedFolder); 
			
			TreeViewColumn projectCol = new TreeViewColumn ();
			projectCol.Title = GettextCatalog.GetString ("Project Folders");
			CellRendererPixbuf pixRenderer = new CellRendererPixbuf ();
			CellRendererText txtRenderer = new CellRendererText ();
			projectCol.PackStart (pixRenderer, false);
			projectCol.PackStart (txtRenderer, true);
			projectCol.SetCellDataFunc (pixRenderer, new TreeCellDataFunc (PixDataFunc));
			projectCol.SetCellDataFunc (txtRenderer, new TreeCellDataFunc (TxtDataFunc));
			projectTree.Model = dirStore;
			projectTree.TestExpandRow += HandleTestExpandRow;;
			projectTree.AppendColumn (projectCol);
			TreeIter projectIter = dirStore.AppendValues (project);
			InitDirs (projectIter, project.BaseDirectory);
			
			TreeViewColumn fileCol = new TreeViewColumn ();
			CellRendererPixbuf filePixRenderer = new CellRendererPixbuf ();
			fileCol.Title = GettextCatalog.GetString ("Files");
			fileCol.PackStart (filePixRenderer, false);
			fileCol.PackStart (txtRenderer, true);
			fileCol.SetCellDataFunc (filePixRenderer, new TreeCellDataFunc (PixFileDataFunc));
			fileCol.SetCellDataFunc (txtRenderer, new TreeCellDataFunc (TxtFileDataFunc));
			fileList.Model = fileStore;
			fileList.AppendColumn (fileCol);
			
			UpdateFileList (null, null);
			
			projectTree.Selection.Changed += UpdateFileList;
			fileList.Selection.Changed += UpdateSensitivity;
		}

		void HandleTestExpandRow (object o, TestExpandRowArgs args)
		{
			if ((bool)dirStore.GetValue (args.Iter, 1) == false) {
				ProjectFile pf = (ProjectFile) dirStore.GetValue (args.Iter, 0);
				InitDirs (args.Iter, pf.FilePath);
			}
			args.RetVal = !dirStore.IterHasChild (args.Iter);
		}
		
		void InitDirs (TreeIter parent, string path)
		{
			foreach (ProjectFile pf in project.Files.GetFilesInPath (path))
				if (pf.Subtype == Subtype.Directory)
					dirStore.AppendValues (parent, pf, false);
			dirStore.SetValue (parent, 1, true);
		}
		
		void PixDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			CellRendererPixbuf pixRenderer = (CellRendererPixbuf) cell;
			object obj = tree_model.GetValue (iter, 0);
			string icon = null;
			if (obj is Project) {
				pixRenderer.PixbufExpanderOpen = null;
				pixRenderer.PixbufExpanderClosed = null;
				pixRenderer.IconName
					= MonoDevelop.Projects.Gui.Services.Icons.GetImageForProjectType (((Project)obj).ProjectType);
				return;
			}
			
			ProjectFile pf = (ProjectFile)obj;
			System.Diagnostics.Debug.Assert (pf.Subtype == Subtype.Directory);
			
			pixRenderer.IconName = null;
			pixRenderer.PixbufExpanderOpen = openFolder;
			pixRenderer.PixbufExpanderClosed = closedFolder; 
		}
		
		void TxtDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			CellRendererText txtRenderer = (CellRendererText) cell;
			object obj = tree_model.GetValue (iter, 0);
			if (obj is Project) {
				txtRenderer.Text = ((Project)obj).Name;
				return;
			}
			
			ProjectFile pf = (ProjectFile)obj;
			System.Diagnostics.Debug.Assert (pf.Subtype == Subtype.Directory);
			int lastSlash = pf.Name.LastIndexOf (System.IO.Path.DirectorySeparatorChar);
			txtRenderer.Text = lastSlash < 0? pf.Name : pf.Name.Substring (lastSlash); 
		}
		
		void PixFileDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			CellRendererPixbuf pixRenderer = (CellRendererPixbuf) cell;
			ProjectFile pf = (ProjectFile)tree_model.GetValue (iter, 0);
			Gdk.Pixbuf oldBuf = pixRenderer.Pixbuf;
			string icName = MonoDevelop.Projects.Gui.Services.Icons.GetImageForFile (pf.FilePath);
			if (icName != MonoDevelop.Core.Gui.Stock.MiscFiles || !System.IO.File.Exists (pf.FilePath))
				pixRenderer.Pixbuf = MonoDevelop.Core.Gui.Services.Resources.GetIcon (icName, Gtk.IconSize.Menu);
			else
				pixRenderer.Pixbuf = MonoDevelop.Ide.Gui.IdeApp.Services.PlatformService.GetPixbufForFile (pf.FilePath, Gtk.IconSize.Menu);
			if (oldBuf != null)
				oldBuf.Dispose ();
		}
		
		void TxtFileDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			CellRendererText txtRenderer = (CellRendererText) cell;
			ProjectFile pf = (ProjectFile)tree_model.GetValue (iter, 0);
			txtRenderer.Text = System.IO.Path.GetFileName (pf.FilePath);
		}
		
		public void AddFileFilter (string name, string pattern)
		{
			if (filters == null) {
				filters = new List<string> ();
				if (defaultFilterPattern != null) {
					filters.Add (defaultFilterPattern);
					fileTypeCombo.AppendText (defaultFilterName);
				}
				typeBox.Visible = false;
				typeBox.ShowAll ();
			}
			
			filters.Add (name);
			fileTypeCombo.AppendText (pattern);
		}
		
		public ProjectFile SelectedFile { get; private set; }
		
		string GetSelectedDirectory ()
		{
			TreeIter iter;
			if (!projectTree.Selection.GetSelected (out iter))
				return project.BaseDirectory;
			object o = dirStore.GetValue (iter, 0);
			if (o is Project)
				return project.BaseDirectory;
			ProjectFile pf = (ProjectFile)o;
			System.Diagnostics.Debug.Assert (pf.Subtype == Subtype.Directory);
			return pf.FilePath;
		}
		
		void UpdateFileList (object sender, EventArgs args)
		{
			fileStore.Clear ();
			
			string pattern = defaultFilterPattern;
			if (filters != null) {
				pattern = filters[fileTypeCombo.Active];
			}
			pattern = System.Text.RegularExpressions.Regex.Escape (pattern);
			pattern = pattern.Replace ("\\*",".*");
			pattern = pattern.Replace ("\\?",".");
			pattern = pattern.Replace ("\\|","$|^");
			pattern = "^" + pattern + "$";
			System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex (pattern);
			
			string dir = GetSelectedDirectory ();
			foreach (ProjectFile pf in project.Files.GetFilesInPath (dir))
				if (pf.Subtype != Subtype.Directory && regex.IsMatch (System.IO.Path.GetFileName (pf.Name)))
					fileStore.AppendValues (pf);
			
			UpdateSensitivity (null, null);
		}
		
		void UpdateSensitivity (object sender, EventArgs args)
		{
			TreeIter iter;
			bool selected = fileList.Selection.GetSelected (out iter);
			buttonOk.Sensitive = selected;
			SelectedFile = selected? (ProjectFile) fileStore.GetValue (iter, 0) : null;
		}
		
		protected override void OnDestroyed ()
		{
			if (openFolder != null) {
				openFolder.Dispose ();
				closedFolder.Dispose ();
				openFolder = closedFolder = null;
			}
			base.OnDestroyed ();
		}

	}
}
