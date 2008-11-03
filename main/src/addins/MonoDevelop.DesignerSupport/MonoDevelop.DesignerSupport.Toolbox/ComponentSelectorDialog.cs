//
// ComponentSelectorDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	internal partial class ComponentSelectorDialog : Gtk.Dialog
	{
		const int ColChecked = 0;
		const int ColName = 1;
		const int ColNamespace = 2;
		const int ColLibrary = 3;
		const int ColPath = 4;
		const int ColIcon = 5;
		const int ColItem = 6;
		const int ColShowCheck = 7;
		const int ColBold = 8;
		
		TreeStore store;
		ComponentIndex index;
		bool indexModified;
		bool showCategories;
		Dictionary<ItemToolboxNode, ItemToolboxNode> currentItems = new Dictionary<ItemToolboxNode, ItemToolboxNode> ();
		
		public ComponentSelectorDialog (IToolboxConsumer currentConsumer)
		{
			using (IProgressMonitor monitor = new MessageDialogProgressMonitor (true, true, false, true)) {
				index = DesignerSupport.Service.ToolboxService.GetComponentIndex (monitor);
			}
			
			this.Build();
			
			store = new TreeStore (typeof(bool), typeof(string), typeof(string), typeof(string), typeof(string), typeof(Gdk.Pixbuf), typeof(ItemToolboxNode), typeof(bool), typeof(int));
			
			TreeViewColumn col;
			col = new TreeViewColumn ();
			Gtk.CellRendererToggle crt = new CellRendererToggle ();
			col.PackStart (crt, false);
			col.AddAttribute (crt, "active", ColChecked);
			col.AddAttribute (crt, "visible", ColShowCheck);
			crt.Toggled += OnToggleItem;
			col.SortColumnId = ColChecked;
			listView.AppendColumn (col);
			
			col = new TreeViewColumn ();
			col.Spacing = 3;
			col.Title = GettextCatalog.GetString ("Name");
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			CellRendererText crx = new CellRendererText ();
			crx.Width = 150;
			col.PackStart (crp, false);
			col.PackStart (crx, false);
			col.AddAttribute (crp, "pixbuf", ColIcon);
			col.AddAttribute (crp, "visible", ColShowCheck);
			col.AddAttribute (crx, "text", ColName);
			col.AddAttribute (crx, "weight", ColBold);
			listView.AppendColumn (col);
			col.Resizable = true;
			col.SortColumnId = ColName;
			
			col = listView.AppendColumn (GettextCatalog.GetString ("Library"), new CellRendererText (), "text", ColLibrary);
			col.Resizable = true;
			col.SortColumnId = ColLibrary;
			
			col = listView.AppendColumn (GettextCatalog.GetString ("Location"), new CellRendererText (), "text", ColPath);
			col.Resizable = true;
			col.SortColumnId = ColPath;
			
			store.SetSortColumnId (ColName, SortType.Ascending);
			listView.SearchColumn = ColName;
			listView.Model = store;
			
			foreach (ItemToolboxNode it in DesignerSupport.Service.ToolboxService.UserItems)
				currentItems [it] = it;
			
			List<string> list = new List<string> ();
			foreach (ComponentIndexFile ifile in index.Files) {
				foreach (ItemToolboxNode co in ifile.Components) {
					if (!list.Contains (co.ItemDomain))
						list.Add (co.ItemDomain);
				}
			}
			
			string defaultDomain = null;
			if (currentConsumer != null)
				defaultDomain = currentConsumer.DefaultItemDomain;
			
			comboType.AppendText (GettextCatalog.GetString ("All"));
			comboType.Active = 0;

			for (int n=0; n<list.Count; n++) {
				string s = list [n];
				comboType.AppendText (s);
				if (s == defaultDomain)
					comboType.Active = n+1;
			}
		}
		
		public void Fill ()
		{
			store.Clear ();
			foreach (ComponentIndexFile ifile in index.Files) {
				foreach (ItemToolboxNode co in ifile.Components) {
					if (comboType.Active <= 0 || comboType.ActiveText == co.ItemDomain)
						AddItem (ifile, co);
				}
			}
		}
		
		void AddItem (ComponentIndexFile ifile, ItemToolboxNode co)
		{
			Gdk.Pixbuf img = co.Icon != null ? co.Icon.ScaleSimple (16, 16, Gdk.InterpType.Bilinear) : null;
			if (showCategories) {
				TreeIter it;
				bool found = false;
				if (store.GetIterFirst (out it)) {
					do {
						if (co.Category == (string) store.GetValue (it, ColName)) {
							found = true;
							break;
						}
					}
					while (store.IterNext (ref it));
				}
				if (!found)
					it = store.AppendValues (false, co.Category, string.Empty, string.Empty, string.Empty, null, null, false, (int)Pango.Weight.Bold);
				store.AppendValues (it, currentItems.ContainsKey (co), co.Name, string.Empty, ifile.Name, ifile.Location, img, co, true, (int)Pango.Weight.Normal);
			}
			else
				store.AppendValues (currentItems.ContainsKey (co), co.Name, string.Empty, ifile.Name, ifile.Location, img, co, true, (int)Pango.Weight.Normal);
		}

		protected virtual void OnComboTypeChanged (object sender, System.EventArgs e)
		{
			Fill ();
		}
		
		void OnToggleItem (object ob, Gtk.ToggledArgs args)
		{
			Gtk.TreeIter it;
			if (store.GetIterFromString (out it, args.Path)) {
				bool b = (bool) store.GetValue (it, ColChecked);
				ItemToolboxNode item = (ItemToolboxNode) store.GetValue (it, ColItem);
				if (!b)
					currentItems.Add (item, item);
				else
					currentItems.Remove (item);
				store.SetValue (it, ColChecked, !b);
			}
		}

		protected void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			if (indexModified)
				index.Save ();
			DesignerSupport.Service.ToolboxService.UpdateUserItems (currentItems.Values);
			Respond (Gtk.ResponseType.Ok);
		}

		protected virtual void OnButton24Clicked (object sender, System.EventArgs e)
		{
			FileSelector fcd = new FileSelector (GettextCatalog.GetString ("Add items to toolbox"), Gtk.FileChooserAction.Open);
			try {
				fcd.DefaultResponse = Gtk.ResponseType.Ok;
				fcd.Filter = new Gtk.FileFilter ();
				fcd.Filter.AddPattern ("*.dll");
				fcd.SelectMultiple = true;

				Gtk.ResponseType response = (Gtk.ResponseType) fcd.Run( );
				fcd.Hide ();
				
				if (response == Gtk.ResponseType.Ok) {
					indexModified = true;
					// Add the new files to the index
					using (IProgressMonitor monitor = new MessageDialogProgressMonitor (true, false, false, true)) {
						monitor.BeginTask (GettextCatalog.GetString ("Looking for components..."), fcd.Filenames.Length);
						foreach (string s in fcd.Filenames) {
							ComponentIndexFile cif = index.AddFile (s);
							monitor.Step (1);
							if (cif != null) {
								// Select all new items by default
								foreach (ItemToolboxNode it in cif.Components)
									currentItems.Add (it, it);
							}
							else
								MessageService.ShowWarning (GettextCatalog.GetString ("The file '{0}' does not contain any component.", s));
						}
					}
					Fill ();
				}
			} finally {
				fcd.Destroy ();
			}
		}

		protected virtual void OnCheckbutton1Clicked (object sender, System.EventArgs e)
		{
			showCategories = checkGroupByCat.Active;
			Fill ();
		}
	}
}
