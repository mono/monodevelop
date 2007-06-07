//
// OpenFileInSolutionDialog.cs
//
// Author:
//   Zach Lute (zach.lute@gmail.com)
//   Jacob Ilsø Christensen
//   Lluis Sanchez
//
// Copyright (C) 2007 Zach Lute
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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Gdk;
using Gtk;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class OpenFileInSolutionDialog : Gtk.Dialog
	{
		public static readonly int COL_ICON = 0;
		public static readonly int COL_FILE = 1;
		public static readonly int COL_PATH = 2;
		public static readonly int COL_FILEPATH = 3;
		public static readonly int COL_LINE = 4;
		public static readonly int COL_COLUMN = 5;
		
		static OpenFileInSolutionDialog dlg;
		public static OpenFileInSolutionDialog Instance {
			get {
				if(dlg == null)
					dlg = new OpenFileInSolutionDialog (true);
				return dlg;
			}
		}
		
		private Dictionary<string, Pixbuf> icons;
		
		ListStore list;
		TreeModelSort model;
		
		object matchLock;
		string matchString;
		
		Thread searchThread;
		bool searchFiles;
		bool updating;
		bool userSelecting;
		
		ArrayList searchResult = new ArrayList ();
		
		string filename;
		int fileLine;
		int fileCol;
		
		public string Filename {
			get { return filename; }
			set { filename = value; }
		}
		
		public int FileLine {
			get { return fileLine; }
		}
		
		public int FileColumn {
			get { return fileCol; }
		}
		
		public bool SearchFiles {
			get {
				return searchFiles; 
			}
			set {
				if (searchFiles != value) {
					searchFiles = value;
					UpdateList();
				}
			}
		}
		
		public OpenFileInSolutionDialog (bool searchFiles)
		{	
			this.Build ();
			icons = new Dictionary<string, Pixbuf> ();
			SetupTreeView ();
		
			matchLock = new System.Object ();
			matchString = "";
			
			matchEntry.GrabFocus ();
			
			this.searchFiles = searchFiles;
			UpdateList ();
		}
		
	    public static void Show (bool searchFiles)
		{
			OpenFileInSolutionDialog od = OpenFileInSolutionDialog.Instance;
			od.SearchFiles = searchFiles;
			int response = od.Run();
			od.Hide();
			if (response == (int)Gtk.ResponseType.Ok)
				IdeApp.Workbench.OpenDocument (od.Filename, od.FileLine, od.FileColumn, true);	
	    }
		
		private void SetupTreeView ()
		{
			Type[] types = new Type[] {
				typeof (Pixbuf),
				typeof (string),
				typeof (string),
				typeof (string),
				typeof (int),
				typeof (int)
			};
				
			list = new ListStore (types);
			
			model = new TreeModelSort (list);
			tree.Model = model;
			
			TreeViewColumn typeFileColumn = new TreeViewColumn ();
			typeFileColumn.Resizable = true;
			typeFileColumn.Title = GettextCatalog.GetString ("Name");			
			CellRendererPixbuf crPix = new CellRendererPixbuf ();
			typeFileColumn.PackStart (crPix, false);
			typeFileColumn.AddAttribute (crPix, "pixbuf", COL_ICON);			
			CellRendererText crText = new CellRendererText ();
			typeFileColumn.PackStart (crText, true);
			typeFileColumn.AddAttribute (crText, "text", COL_FILE);						
			tree.AppendColumn (typeFileColumn);
			
			TreeViewColumn pathColumn = new TreeViewColumn ();
			pathColumn.Resizable = true;
			pathColumn.Title = GettextCatalog.GetString ("Full name");
			crText = new CellRendererText ();
			pathColumn.PackStart (crText, true);
			pathColumn.AddAttribute (crText, "text", COL_PATH);			
			tree.AppendColumn (pathColumn);
			
			model.SetSortColumnId (COL_FILE, SortType.Ascending);
			model.ChangeSortColumn ();
		}
		
		protected void HandleShown (object sender, System.EventArgs e)
		{
			// Perform the search over in case things have changed.
			PerformSearch ();
			
			// Highlight the text so they can quickly type over it.
			matchEntry.SelectRegion (0, matchEntry.Text.Length);
			matchEntry.GrabFocus ();
		}

		protected virtual void HandleOpen (object sender, EventArgs e)
		{
			OpenFile ();
		}
		
		protected virtual void HandleRowActivate (object o, 
		                                          RowActivatedArgs args)
		{
			OpenFile ();
		}
		
		protected virtual void HandleEntryActivate (object o, 
		                                            EventArgs args)
		{
			OpenFile ();
		}
		
		private void OpenFile ()
		{
			TreeModel model;
			TreeIter iter;
			
			TreeSelection sel = tree.Selection;
			if (sel.GetSelected (out model, out iter)) {
				object o = model.GetValue (iter, COL_FILEPATH);
				Filename = o as string;
				fileLine = (int) model.GetValue (iter, COL_LINE);
				fileCol = (int) model.GetValue (iter, COL_COLUMN);
				this.Respond (ResponseType.Ok);
			} else {
				Filename = "";
				this.Respond (ResponseType.Cancel);
			}
		}

		protected virtual void HandleEntryChanged (object sender, 
		                                           System.EventArgs e)
		{
			// Find the matching files and display them in the tree.
			PerformSearch ();
		}
		
		private void PerformSearch ()
		{
			if (list == null)
				return;
			
			userSelecting = false;
			
			string toMatch = matchEntry.Text.ToLower ();
				
			lock (matchLock) {
				matchString = toMatch;
			}
			
			if (searchThread != null) {
				searchThread.Abort ();
				searchThread = null;
			}
			
			lock (searchResult) {
				// Clean the results list
				searchResult.Clear ();
			}

			// Queuing this seems to prevent things getting
			// added from queued events after the clear.
			Services.DispatchService.GuiDispatch (list.Clear);
			
			ThreadStart start = new ThreadStart (SearchThread);
			searchThread = new Thread (start);
			searchThread.IsBackground = true;
			searchThread.Priority = ThreadPriority.Lowest;
			searchThread.Start ();
		}
		
		void SearchThread ()
		{
			Solution s = ProjectService.Solution;
			if (s == null)
				return;
		
			foreach (IProject p in s.AllProjects) {
				if (searchFiles) {
					string toMatch;
					lock (matchLock) {
						toMatch = matchString;
					}

					foreach (ProjectItem item in p.Items) {
						ProjectFile file = item as ProjectFile;
						if (file == null)
							continue;
						CheckFile (file.FullPath, toMatch);
					}
				} else {
					
					string toMatch;
					lock (matchLock) {
						toMatch = matchString;
					}
// TODO: Project Conversion			
//					IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (p);
//					foreach (IClass c in ctx.GetProjectContents())
//						CheckType (c, toMatch);
				}
			}
		}
		
		void CheckFile (string path, string toMatch)
		{
			string name = System.IO.Path.GetFileName (path);
			if (toMatch.Length > 0 && !name.ToLower ().Contains (toMatch))
				return;
					
			Pixbuf icon = GetIcon (Services.Icons.GetImageForFile (path));
						
			object[] data = new object[] { icon, name, path, path, -1, -1 };
			AddItem (data);
		}
		
		void CheckType (IClass c, string toMatch)
		{
			if (toMatch.Length > 0 && !c.Name.ToLower ().Contains (toMatch))
				return;
			
			if (c.Region == null)
				return;
			
			Pixbuf icon = GetIcon (Services.Icons.GetIcon (c));
			
			object[] data = new object [] { icon, c.Name, c.FullyQualifiedName, c.Region.FileName, c.Region.BeginLine, c.Region.BeginColumn };
			AddItem (data);
		}

		void AddItem (object[] data)
		{
			// Add the result to the results list, and asychronously call the
			// AddItemGui method which will be in charge of adding them to the tree.
			
			lock (searchResult) {
				searchResult.Add (data);
				if (searchResult.Count == 1)
					GLib.Idle.Add (AddItemGui);
			}
		}
		
		bool AddItemGui ()
		{
			// Add items to the tree. Do it in blocks of 50, to avoid freezing
			// the GUI when there are a lot of items to add.
			
			lock (searchResult) {
				int max = Math.Min (50, searchResult.Count);
				for (int n=0; n<max; n++) {
					list.AppendValues ((object[]) searchResult [n]);
				}
				SelectFirstItem ();
				searchResult.RemoveRange (0, max);
				return searchResult.Count > 0;
			}
		}
		
		void SelectFirstItem ()
		{
			TreeIter iter;
			
			// Don't select if something is already selected
			if (userSelecting && tree.Selection.GetSelected (out iter)) {
				tree.ScrollToCell (model.GetPath (iter), tree.Columns[0],false, 0f, 0f);
				return;
			}
			
			// Select the first thing in the list.
			if(model.GetIterFirst (out iter))
				Select (iter);
		}
		
		private Pixbuf GetIcon (string id)
		{
			if (!icons.ContainsKey (id)) {
				IconSize size = IconSize.Menu;
				Pixbuf icon = tree.RenderIcon (id, size, ""); 
				icons.Add (id, icon);
			}
			return icons [id];
		}

		private void Select (TreeIter iter)
		{
			tree.Selection.SelectIter (iter);
			TreePath path = model.GetPath (iter);
			tree.ScrollToCell (path, null, false, 0, 0);
		}
		
		protected virtual void HandleKeyPress (object o, 
		                                       KeyPressEventArgs args)
		{
			// Up and down move the tree selection up and down
			// for rapid selection changes.
			Gdk.EventKey key = args.Event;
			switch (key.Key) {
			case Gdk.Key.Up:
				MoveSelectionUp ();
				args.RetVal = true;
				break;
			case Gdk.Key.Down:
				MoveSelectionDown ();
				args.RetVal = true;
				break;
			}
		}
		
		void MoveSelectionUp ()
		{
			TreeSelection sel = tree.Selection;
			TreeIter iter;
			
			if (!sel.GetSelected (out iter))
				return;
			
			// Bah, no IterPrev.
			TreePath path = model.GetPath (iter);
			if (path.Prev ()) {
				// Go to the previous node.
				if (model.GetIter (out iter, path))
					Select (iter);
			} else {
				// Go to the last node.
				int num = model.IterNChildren();
				if (model.IterNthChild (out iter, num - 1))
					Select (iter);
			}
			userSelecting = true;
		}

		void MoveSelectionDown ()
		{
			TreeSelection sel = tree.Selection;
			TreeIter iter;

			if (sel.GetSelected (out iter)) {
				if (model.IterNext (ref iter))
					Select (iter);
				else if (model.GetIterFirst (out iter))
					Select (iter);
			}
			userSelecting = true;
		}
		
		void UpdateList ()
		{
			updating = true;
			toggleFiles.Active = searchFiles;
			toggleTypes.Active = !searchFiles;
			updating = false;
			PerformSearch ();
		}

		protected virtual void OnToggleFilesClicked(object sender, System.EventArgs e)
		{
			if (updating)
				return;
			searchFiles = true;
			UpdateList ();
		}

		protected virtual void OnToggleTypesClicked(object sender, System.EventArgs e)
		{
			if (updating)
				return;
			searchFiles = false;
			UpdateList ();
		}
	}
}