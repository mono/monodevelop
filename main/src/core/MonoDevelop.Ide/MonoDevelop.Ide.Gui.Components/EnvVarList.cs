// EnvVarList.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Components
{
	[System.ComponentModel.Category("MonoDevelop.Projects.Gui")]
	[System.ComponentModel.ToolboxItem(true)]
	public class EnvVarList: ScrolledWindow
	{
		TreeView list;
		ListStore store;
		string createMsg;
		
		public EnvVarList()
		{
			list = new TreeView ();
			store = new ListStore (typeof(string), typeof(string), typeof(bool), typeof(string));
			list.Model = store;
			list.SearchColumn = -1; // disable the interactive search
			Add (list);
			
			CellRendererText crt = new CellRendererText ();
			crt.Editable = true;
			TreeViewColumn col = list.AppendColumn (GettextCatalog.GetString ("Variable"), crt, "text", 0, "foreground", 3);
			col.Resizable = true;
			
			CellRendererText crv = new CellRendererText ();
			col = list.AppendColumn (GettextCatalog.GetString ("Value"), crv, "text", 1, "editable", 2);
			col.Resizable = true;
			
			crt.Edited += OnExpEdited;
			crt.EditingStarted += OnExpEditing;
			crv.Edited += OnValEdited;
			
			createMsg = GettextCatalog.GetString ("Click here to add a new variable");
			AppendInserter ();
			
			ShowAll ();
		}
		
		public void LoadValues (IDictionary<string, string> values)
		{
			store.Clear ();
			foreach (KeyValuePair<string,string> val in values)
				store.AppendValues (val.Key, val.Value, true, null);
			AppendInserter ();
		}
		
		public void StoreValues (IDictionary<string, string> values)
		{
			TreeIter it;
			if (!store.GetIterFirst (out it))
				return;
			do {
				bool inserter = !(bool) store.GetValue (it, 2);
				if (!inserter) {
					string var = (string) store.GetValue (it, 0);
					string val = (string) store.GetValue (it, 1);
					values [var] = val;
				}
			} while (store.IterNext (ref it));
		}
		
		void OnExpEditing (object s, Gtk.EditingStartedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			Gtk.Entry e = (Gtk.Entry) args.Editable;
			if (e.Text == createMsg)
				e.Text = string.Empty;
		}
		
		void OnExpEdited (object s, Gtk.EditedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			
			bool isInserter = !(bool) store.GetValue (it, 2);
			
			if (args.NewText.Length == 0) {
				if (!isInserter)
					store.Remove (ref it);
				return;
			}
			
			store.SetValue (it, 0, args.NewText);
			store.SetValue (it, 2, true);
			store.SetValue (it, 3, null);
			
			if (isInserter)
				AppendInserter ();
		}
		
		void OnValEdited (object s, Gtk.EditedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			store.SetValue (it, 1, args.NewText);
		}
		
		void AppendInserter ()
		{
			store.AppendValues (createMsg, "", false, "gray");
		}
	}
}
