// 
// FolderListSelector.cs
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
using System.Collections.Generic;
using Gtk;

namespace MonoDevelop.Components
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class FolderListSelector : Gtk.Bin
	{
		ListStore store;
		List<string> directories = new List<string> ();
		
		public FolderListSelector ()
		{
			this.Build ();
			
			store = new ListStore (typeof(String));
			dirList.Model = store;
			dirList.SearchColumn = -1; // disable the interactive search
			dirList.AppendColumn ("", new CellRendererText (), "text", 0);
			dirList.Selection.Changed += delegate {
				UpdateStatus ();
			};
			UpdateStatus ();
		}
		
		public List<string> Directories {
			get {
				return directories;
			}
			set {
				directories = value;
				UpdateList ();
			}
		}
		
		void UpdateList ()
		{
			store.Clear ();
			foreach (string dir in directories)
				store.AppendValues (dir);
		}
		
		void UpdateStatus ()
		{
			buttonAdd.Sensitive = !string.IsNullOrEmpty (folderentry.Path);
			TreeIter it;
			if (dirList.Selection.GetSelected (out it)) {
				buttonUpdate.Sensitive = !string.IsNullOrEmpty (folderentry.Path);
				buttonRemove.Sensitive = true;
				TreeIter fi;
				store.GetIterFirst (out fi);
				buttonUp.Sensitive = !(store.GetPath (it).Equals (store.GetPath (fi)));
				buttonDown.Sensitive = store.IterNext (ref it);
			} else {
				buttonUpdate.Sensitive = buttonRemove.Sensitive = buttonUp.Sensitive = buttonDown.Sensitive = false;
			}
		}

		void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			string path = folderentry.Path;
			directories.Add (path);
			TreeIter it = store.AppendValues (path);
			FocusRow (it);
		}
		
		void FocusRow (TreeIter it)
		{
			dirList.Selection.SelectIter (it);
			dirList.ScrollToCell (store.GetPath (it), dirList.Columns[0], false, 0, 0);
			UpdateStatus ();
		}

		void OnFolderentryPathChanged (object sender, System.EventArgs e)
		{
			UpdateStatus ();
		}

		void OnButtonRemoveClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (dirList.Selection.GetSelected (out it)) {
				string dir = (string) store.GetValue (it, 0);
				directories.Remove (dir);
				store.Remove (ref it);
				if (store.IterIsValid (it))
					FocusRow (it);
				else
					UpdateStatus ();
			}
		}

		void OnButtonUpdateClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (dirList.Selection.GetSelected (out it)) {
				string dir = (string) store.GetValue (it, 0);
				int i = directories.IndexOf (dir);
				directories [i] = folderentry.Path;
				store.SetValue (it, 0, folderentry.Path);
				UpdateStatus ();
			}
		}

		void OnButtonUpClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			dirList.Selection.GetSelected (out it);
			string dir = (string) store.GetValue (it, 0);
			int i = store.GetPath (it).Indices [0];
			string prevDir = directories [i - 1];
			TreeIter pi;
			store.IterNthChild (out pi, i - 1);
			store.SetValue (pi, 0, dir);
			store.SetValue (it, 0, prevDir);
			directories [i - 1] = dir;
			directories [i] = prevDir;
			FocusRow (pi);
		}

		void OnButtonDownClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			dirList.Selection.GetSelected (out it);
			string prevDir = (string) store.GetValue (it, 0);
			int i = directories.IndexOf (prevDir);
			string dir = directories [++i];
			TreeIter pi = it;
			store.IterNext (ref it);
			store.SetValue (pi, 0, dir);
			store.SetValue (it, 0, prevDir);
			directories [i - 1] = dir;
			directories [i] = prevDir;
			FocusRow (it);
		}
	}
}
