//
// AddinTreeWidget.cs
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
using Gtk;
using Gdk;
using Mono.Addins;
using Mono.Addins.Setup;
using Mono.Unix;

namespace Mono.Addins.Gui
{
	public class AddinTreeWidget
	{
		protected Gtk.TreeView treeView;
		protected Gtk.TreeStore treeStore;
		bool allowSelection;
		ArrayList selected = new ArrayList ();
		Hashtable addinData = new Hashtable ();
		
		Gdk.Pixbuf package;
		Gdk.Pixbuf userPackage;
		
		public event EventHandler SelectionChanged;
		
		const int ColAddin = 0;
		const int ColData = 1;
		const int ColName = 2;
		const int ColVersion = 3;
		const int ColAllowSelection = 4;
		const int ColSelected = 5;
		const int ColImage = 6;
		const int ColShowImage = 7;
		
		public AddinTreeWidget (Gtk.TreeView treeView)
		{
			package = Gdk.Pixbuf.LoadFromResource ("package-x-generic_22.png");
			userPackage = Gdk.Pixbuf.LoadFromResource ("user-package.png");
			
			this.treeView = treeView;
			ArrayList list = new ArrayList ();
			AddStoreTypes (list);
			Type[] types = (Type[]) list.ToArray (typeof(Type));
			treeStore = new Gtk.TreeStore (types);
			treeView.Model = treeStore;
			CreateColumns ();
		}
		
		protected virtual void AddStoreTypes (ArrayList list)
		{
			list.Add (typeof(object));
			list.Add (typeof(object));
			list.Add (typeof(string));
			list.Add (typeof(string));
			list.Add (typeof(bool));
			list.Add (typeof(bool));
			list.Add (typeof (Pixbuf));
			list.Add (typeof(bool));
		}
		
		protected virtual void CreateColumns ()
		{
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = Catalog.GetString ("Add-in");
			
			CellRendererToggle crtog = new CellRendererToggle ();
			crtog.Activatable = true;
			crtog.Toggled += new ToggledHandler (OnAddinToggled);
			col.PackStart (crtog, false);
			
			CellRendererPixbuf pr = new CellRendererPixbuf ();
			col.PackStart (pr, false);
			col.AddAttribute (pr, "pixbuf", ColImage);
			col.AddAttribute (pr, "visible", ColShowImage);
			
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			
			col.AddAttribute (crt, "markup", ColName);
			col.AddAttribute (crtog, "visible", ColAllowSelection);
			col.AddAttribute (crtog, "active", ColSelected);
			treeView.AppendColumn (col);
			
			col = new TreeViewColumn ();
			col.Title = Catalog.GetString ("Version");
			col.PackStart (crt, true);
			col.AddAttribute (crt, "markup", ColVersion);
			treeView.AppendColumn (col);
		}
		
		public bool AllowSelection {
			get { return allowSelection; }
			set { allowSelection = value; }
		}
		
		void OnAddinToggled (object o, ToggledArgs args)
		{
			TreeIter it;
			if (treeStore.GetIter (out it, new TreePath (args.Path))) {
				bool sel = !(bool) treeStore.GetValue (it, 5);
				treeStore.SetValue (it, 5, sel);
				AddinHeader info = (AddinHeader) treeStore.GetValue (it, 0);
				if (sel)
					selected.Add (info);
				else
					selected.Remove (info);

				OnSelectionChanged (EventArgs.Empty);
			}
		}
		
		protected virtual void OnSelectionChanged (EventArgs e)
		{
			if (SelectionChanged != null)
				SelectionChanged (this, e);
		}
		
		public void Clear ()
		{
			addinData.Clear ();
			selected.Clear ();
			treeStore.Clear ();
		}
		
		public TreeIter AddAddin (AddinHeader info, object dataItem, bool enabled)
		{
			return AddAddin (info, dataItem, enabled, false);
		}
		
		public TreeIter AddAddin (AddinHeader info, object dataItem, bool enabled, bool userDir)
		{
			Gdk.Pixbuf icon;
			if (userDir)
				icon = userPackage;
			else
				icon = package;

			addinData [info] = dataItem;
			TreeIter piter = TreeIter.Zero;
			if (info.Category == "") {
				string otherCat = Catalog.GetString ("Other");
				piter = FindCategory (otherCat);
			} else {
				piter = FindCategory (info.Category);
			}
			
			TreeIter iter = treeStore.AppendNode (piter);
			UpdateRow (iter, info, dataItem, enabled, icon);
			return iter;
		}
		
		protected virtual void UpdateRow (TreeIter iter, AddinHeader info, object dataItem, bool enabled, Gdk.Pixbuf icon)
		{
			bool sel = selected.Contains (info);
			
			treeStore.SetValue (iter, ColAddin, info);
			treeStore.SetValue (iter, ColData, dataItem);
			
			if (enabled) {
				treeStore.SetValue (iter, ColName, info.Name);
				treeStore.SetValue (iter, ColVersion, info.Version);
				treeStore.SetValue (iter, ColAllowSelection, allowSelection);
			}
			else {
				treeStore.SetValue (iter, ColName, "<span foreground=\"grey\">" + info.Name + "</span>");
				treeStore.SetValue (iter, ColVersion, "<span foreground=\"grey\">" + info.Version + "</span>");
				treeStore.SetValue (iter, ColAllowSelection, false);
			}
			
			treeStore.SetValue (iter, ColImage, icon);
			treeStore.SetValue (iter, ColShowImage, true);
			
			treeStore.SetValue (iter, ColSelected, sel);
		}
		
		public object GetAddinData (AddinHeader info)
		{
			return addinData [info];
		}
		
		public AddinHeader[] GetSelectedAddins ()
		{
			return (AddinHeader[]) selected.ToArray (typeof(AddinHeader));
		}
		
		TreeIter FindCategory (string namePath)
		{
			TreeIter iter = TreeIter.Zero;
			string[] paths = namePath.Split ('/');
			foreach (string name in paths) {
				TreeIter child;
				if (!FindCategory (iter, name, out child)) {
					if (iter.Equals (TreeIter.Zero))
						iter = treeStore.AppendValues (null, null, name, "", false, false, null, false);
					else
						iter = treeStore.AppendValues (iter, null, null, name, "", false, false, null, false);
				}
				else
					iter = child;
			}
			return iter;
		}
		
		bool FindCategory (TreeIter piter, string name, out TreeIter child)
		{
			if (piter.Equals (TreeIter.Zero)) {
				if (!treeStore.GetIterFirst (out child))
					return false;
			}
			else if (!treeStore.IterChildren (out child, piter))
				return false;

			do {
				if (((string) treeStore.GetValue (child, ColName)) == name) {
					return true;
				}
			} while (treeStore.IterNext (ref child));

			return false;
		}
		
		public AddinHeader ActiveAddin {
			get {
				Gtk.TreeModel foo;
				Gtk.TreeIter iter;
				if (!treeView.Selection.GetSelected (out foo, out iter))
					return null;
					
				return (AddinHeader) treeStore.GetValue (iter, 0);
			}
		}
		
		public object ActiveAddinData {
			get {
				AddinHeader ai = ActiveAddin;
				return ai != null ? GetAddinData (ai) : null;
			}
		}
		
		public object SaveStatus ()
		{
			TreeIter iter;
			ArrayList list = new ArrayList ();
			
			// Save the current selection
			Gtk.TreeModel foo;
			if (treeView.Selection.GetSelected (out foo, out iter))
				list.Add (treeStore.GetPath (iter));
			else
				list.Add (null);
			
			if (!treeStore.GetIterFirst (out iter))
				return null;
			
			// Save the expand state
			do {
				SaveStatus (list, iter);
			} while (treeStore.IterNext (ref iter));
			
			return list;
		}
		
		void SaveStatus (ArrayList list, TreeIter iter)
		{
			Gtk.TreePath path = treeStore.GetPath (iter);
			if (treeView.GetRowExpanded (path))
				list.Add (path);
			if (treeStore.IterChildren (out iter, iter)) {
				do {
					SaveStatus (list, iter);
				} while (treeStore.IterNext (ref iter));
			}
		}
		
		public void RestoreStatus (object ob)
		{
			if (ob == null)
				return;
				
			// The first element is the selection
			ArrayList list = (ArrayList) ob;
			TreePath selpath = (TreePath) list [0];
			list.RemoveAt (0);
			
			foreach (TreePath path in list)
				treeView.ExpandRow (path, false);

			if (selpath != null)
				treeView.Selection.SelectPath (selpath);
		}
		
		public void SelectAll ()
		{
			TreeIter iter;
			
			if (!treeStore.GetIterFirst (out iter))
				return;
			do {
				SelectAll (iter);
			} while (treeStore.IterNext (ref iter));
			OnSelectionChanged (EventArgs.Empty);
		}
		
		void SelectAll (TreeIter iter)
		{
			AddinHeader info = (AddinHeader) treeStore.GetValue (iter, ColAddin);
				
			if (info != null) {
				treeStore.SetValue (iter, ColSelected, true);
				if (!selected.Contains (info))
					selected.Add (info);
				treeView.ExpandToPath (treeStore.GetPath (iter));
			} else {
				if (treeStore.IterChildren (out iter, iter)) {
					do {
						SelectAll (iter);
					} while (treeStore.IterNext (ref iter));
				}
			}
		}
		
		public void UnselectAll ()
		{
			TreeIter iter;
			if (!treeStore.GetIterFirst (out iter))
				return;
			do {
				UnselectAll (iter);
			} while (treeStore.IterNext (ref iter));
			OnSelectionChanged (EventArgs.Empty);
		}
		
		void UnselectAll (TreeIter iter)
		{
			AddinHeader info = (AddinHeader) treeStore.GetValue (iter, ColAddin);
			if (info != null) {
				treeStore.SetValue (iter, ColSelected, false);
				selected.Remove (info);
			} else {
				if (treeStore.IterChildren (out iter, iter)) {
					do {
						UnselectAll (iter);
					} while (treeStore.IterNext (ref iter));
				}
			}
		}
	}
}
