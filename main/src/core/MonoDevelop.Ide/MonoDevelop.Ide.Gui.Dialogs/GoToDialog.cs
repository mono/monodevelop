//
// GoToDialog.cs
//
// Author:
//   Zach Lute (zach.lute@gmail.com)
//   Aaron Bockover (abockover@novell.com)
//   Jacob Ilsø Christensen
//   Lluis Sanchez
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Collections.ObjectModel;
using System.Threading;
using Gdk;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class GoToDialog : Gtk.Dialog
	{
		const int COL_ICON = 0;
		const int COL_FILE = 1;
		const int COL_PATH = 2;
		const int COL_FILEPATH = 3;
		const int COL_LINE = 4;
		const int COL_COLUMN = 5;
		
		Dictionary<string, Pixbuf> icons;
		int cellHeight;
		
		ListStore list;
		TreeModelSort model;
		
		object matchLock;
		string matchString;
		
		// Thread management
		Thread searchThread;
		AutoResetEvent searchThreadWait;
		bool searchCycleActive;
		bool searchThreadDispose;
		
		bool searchFiles;
		bool updating;
		bool userSelecting;
		
		ArrayList searchResult = new ArrayList ();
		
		string filename;
		int fileLine;
		int fileCol;
		
		protected string Filename {
			get { return filename; }
			set { filename = value; }
		}
		
		protected int FileLine {
			get { return fileLine; }
		}
		
		protected int FileColumn {
			get { return fileCol; }
		}
		
		protected bool SearchFiles {
			get { return searchFiles; }
			set {
				if (searchFiles == value) {
					return;
				}
				
				searchFiles = value;
				Title = searchFiles 
					? GettextCatalog.GetString ("Go to File")
					: GettextCatalog.GetString ("Go to Type");
				
				UpdateList ();
			}
		}
		
		protected GoToDialog (bool searchFiles)
		{	
			this.searchFiles = searchFiles;
			
			matchLock = new object ();
			matchString = String.Empty;
			
			icons = new Dictionary<string, Pixbuf> ();
			
			Build ();
			SetupTreeView ();
			matchEntry.GrabFocus ();
			
			SearchFiles = searchFiles;
		}
		
	    public static void Run (bool searchFiles)
		{
			GoToDialog dialog = new GoToDialog (searchFiles);
			try {
				if ((ResponseType)dialog.Run () == ResponseType.Ok) {
					IdeApp.Workbench.OpenDocument (dialog.Filename, dialog.FileLine, dialog.FileColumn, true);
				}
			} finally {
				dialog.Destroy ();
			}
	    }
		
		private void SetupTreeView ()
		{
			list = new ListStore (typeof (Pixbuf),
				typeof (string),
				typeof (string),
				typeof (string),
				typeof (int),
				typeof (int));
			
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
			cellHeight = 29;
			tree.Selection.Changed += OnSelectionChanged;
		}
		
		void OnSelectionChanged (object ob, EventArgs args)
		{
			openButton.Sensitive = tree.Selection.CountSelectedRows () > 0;
		}
		
		protected void HandleShown (object sender, System.EventArgs e)
		{
			// Perform the search over in case things have changed.
			PerformSearch ();
			
			// Highlight the text so they can quickly type over it.
			matchEntry.SelectRegion (0, matchEntry.Text.Length);
			matchEntry.GrabFocus ();
		}

		protected virtual void HandleOpen (object o, EventArgs args)
		{
			OpenFile ();
		}
		
		protected virtual void HandleRowActivate (object o, RowActivatedArgs args)
		{
			OpenFile ();
		}
		
		protected virtual void HandleEntryActivate (object o, EventArgs args)
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
				Respond (ResponseType.Ok);
			} else {
				Filename = String.Empty;
				Respond (ResponseType.Cancel);
			}
		}

		protected virtual void HandleEntryChanged (object sender, System.EventArgs e)
		{
			// Find the matching files and display them in the tree.
			PerformSearch ();
		}
		
		protected override void OnShown ()
		{
			base.OnShown ();
			UpdateList ();
		}
		
		public override void Destroy ()
		{
			// Set the thread into a dispose state and wake it up so it can exit
			if (searchCycleActive && searchThread != null && searchThreadWait != null) {
				searchCycleActive = false;
				searchThreadDispose = true;
				searchThreadWait.Set ();
			}
			
			base.Destroy ();
		}
		
		void StopActiveSearch ()
		{
			// Tell the thread's search code that it should stop working and 
			// then have the thread wait on the handle until told to resume
			if (searchCycleActive && searchThread != null && searchThreadWait != null) {
				searchCycleActive = false;
				searchThreadWait.Reset ();
			}
		}
		
		void PerformSearch ()
		{
			userSelecting = false;
			
			StopActiveSearch ();
			
			string toMatch = matchEntry.Text.ToLower ();
				
			lock (matchLock) {
				matchString = toMatch;
			}
			
			lock (searchResult) {
				// Clean the results list
				searchResult.Clear ();
			}

			// Queuing this seems to prevent things getting
			// added from queued events after the clear.
			if (list != null) {
				DispatchService.GuiDispatch (list.Clear);
			}
			
			if (searchThread == null) {
				// Create the handle the search thread will wait on when there is nothing to do
				searchThreadWait = new AutoResetEvent (false);
				
				// Only a single thread will be used for searching
				ThreadStart start = new ThreadStart (SearchThread);
				searchThread = new Thread (start);
				searchThread.IsBackground = true;
				searchThread.Priority = ThreadPriority.Lowest;
				searchThread.Start ();
			}
			
			// Wake the handle up so the search thread can do some work
			searchCycleActive = true;
			searchThreadWait.Set ();
		}
		
		void SearchThread ()
		{
			// The thread will remain active until the dialog goes away
			while (true) {
				searchThreadWait.WaitOne ();
				if (searchThreadDispose) {
					break;
				}
				
				try {
					SearchThreadCycle ();
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in GoToDialog", ex);
				}
			}
			
			// Reset all thread state even though this shouldn't be
			// necessary since we destroy and never reuse the dialog
			searchCycleActive = false;
			searchThreadDispose = false;
			
			searchThreadWait.Close ();
			searchThreadWait = null;
			searchThread = null;
		}
		
		void SearchThreadCycle ()
		{
			// This is the inner thread worker; it actually does the searching
			// Any where we enter loop, a check is added to see if the search
			// should be aborted entirely so we can return to the wait handle
			
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (!searchCycleActive) return;
				
				// We only want to check it here if it's not part
				// of the open combine.  Otherwise, it will get
				// checked down below.
				if(doc.Project != null)
					continue;
				
				string toMatch;
				lock (matchLock) {
					toMatch = matchString;
				}
				
				if (searchFiles) {
					CheckFile (doc.FileName, toMatch);
				} else {
					ICompilationUnit info = doc.CompilationUnit;
					if(info != null) {
						foreach (IType c in info.Types) {
							if (!searchCycleActive) return;
							CheckType (c, toMatch);
						}
					}
				}
			}
			
			ReadOnlyCollection<Project> projects = IdeApp.Workspace.GetAllProjects ();
			if (projects.Count < 1)
				return;

			foreach (SolutionItem entry in projects) {
				if (!searchCycleActive) return;
				
				Project p = entry as Project;
				if (p == null)
					continue;
				
				string toMatch;
				lock (matchLock) {
					toMatch = matchString;
				}
				
				if (searchFiles) {
					ProjectFileCollection files = p.Files;
					if (files.Count < 1)
						continue;

					foreach (ProjectFile file in files) {
						if (!searchCycleActive) return;
						CheckFile (file.FilePath, toMatch);
					}
				} else {

					foreach (IType c in ProjectDomService.GetProjectDom (p).Types) {
						if (!searchCycleActive) return;
						CheckType (c, toMatch);
					}
				}
			}
		}
		
		void CheckFile (string path, string toMatch)
		{
			string name = System.IO.Path.GetFileName (path);
			if (toMatch.Length > 0 && !name.ToLower ().Contains (toMatch))
				return;
					
			string icon = Services.Icons.GetImageForFile (path);
			AddItem (icon, name, path, path, -1, -1);
		}
		
		void CheckType (IType c, string toMatch)
		{
			if (toMatch.Length > 0 && !c.FullName.ToLower ().Contains (toMatch))
				return;
			
			if (c.BodyRegion.IsEmpty)
				return;
			
			AddItem (c.StockIcon, c.Name, c.FullName, c.CompilationUnit.FileName, 
				c.Location.Line, c.Location.Column);
		}

		void AddItem (params object[] data)
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
					object[] data = (object[]) searchResult [n];
					Pixbuf icon = GetIcon ((string)data[0]);
					data[0] = icon;
					list.AppendValues (data);
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
				Pixbuf icon = tree.RenderIcon (id, size, String.Empty); 
				icons.Add (id, icon);
			}
			return icons [id];
		}

		private void Select (TreeIter iter)
		{
			if ( tree.Selection == null )
				return;
			
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
			int page = (int)(tree.VisibleRect.Height / cellHeight);
			switch (key.Key) {
			case Gdk.Key.Page_Down:
				MoveSelectionDown (page);
				args.RetVal = true;
				break;
			case Gdk.Key.Page_Up:
				MoveSelectionUp (page);
				args.RetVal = true;
				break;
			case Gdk.Key.Up:
				MoveSelectionUp (1);
				args.RetVal = true;
				break;
			case Gdk.Key.Down:
				MoveSelectionDown (1);
				args.RetVal = true;
				break;
			}
		}
		
		void MoveSelectionUp (int n)
		{
			TreeSelection sel = tree.Selection;
			TreeIter iter;
			
			if (!sel.GetSelected (out iter))
				return;
			
			TreePath path = model.GetPath (iter);
			while (--n > 0 && path.Prev ()) 
				;
			
			if (path.Prev ()) {
				// Go to the previous node.
				if (model.GetIter (out iter, path))
					Select (iter);
				userSelecting = true;
			} else {
				SelectFirstNode();
			}
		}
		
		void SelectLastNode ()
		{
			TreeSelection sel = tree.Selection;
			TreeIter iter;
			
			if (!sel.GetSelected (out iter))
				return;
			while (model.IterNext (ref iter)) {
				Select (iter);
			}
			userSelecting = true;
		}

		void SelectFirstNode()
		{
			TreeSelection sel = tree.Selection;
			TreeIter iter;
			
			if (!sel.GetSelected (out iter))
				return;
			if (model.GetIterFirst (out iter))
				Select (iter);
			userSelecting = true;
		}
		
		void MoveSelectionDown (int n)
		{
			TreeSelection sel = tree.Selection;
			TreeIter iter;
			
			if (!sel.GetSelected (out iter))
				return;
			
			TreePath path = model.GetPath (iter);
			while (n-- > 0)
				path.Next ();
			if (model.GetIter (out iter, path))
				Select (iter);
			else 
				SelectLastNode ();
			userSelecting = true;
		}
		
		void UpdateList ()
		{
			updating = true;
			toggleFiles.Active = searchFiles;
			toggleTypes.Active = !searchFiles;
			updating = false;
			if (Visible)
				PerformSearch ();
		}

		protected virtual void OnToggleFilesClicked(object sender, System.EventArgs e)
		{
			if (updating)
				return;
			this.SearchFiles = true;
			matchEntry.GrabFocus ();
		}

		protected virtual void OnToggleTypesClicked(object sender, System.EventArgs e)
		{
			if (updating)
				return;
			this.SearchFiles = false;
			matchEntry.GrabFocus ();
		}
	}
}
