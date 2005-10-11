
using System;
using System.Collections;
using Gtk;
using Gdk;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Setup;

namespace MonoDevelop.Core.Gui.Dialogs
{
	class AddinTreeWidget
	{
		protected Gtk.TreeView treeView;
		protected Gtk.TreeStore treeStore;
		bool allowSelection;
		ArrayList selected = new ArrayList ();
		Hashtable addinData = new Hashtable ();
		
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
			col.Title = GettextCatalog.GetString ("Add-in");
			
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
			col.Title = GettextCatalog.GetString ("Version");
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
				AddinInfo info = (AddinInfo) treeStore.GetValue (it, 0);
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
			treeStore.Clear ();
		}
		
		public TreeIter AddAddin (AddinInfo info, object dataItem, bool enabled)
		{
			addinData [info] = dataItem;
			TreeIter piter = TreeIter.Zero;
			if (info.Category == "") {
				string otherCat = GettextCatalog.GetString ("Other");
				piter = FindCategory (otherCat);
			} else {
				piter = FindCategory (info.Category);
			}
			
			TreeIter iter = treeStore.AppendNode (piter);
			UpdateRow (iter, info, dataItem, enabled);
			return iter;
		}
		
		protected virtual void UpdateRow (TreeIter iter, AddinInfo info, object dataItem, bool enabled)
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
			
			treeStore.SetValue (iter, ColImage, Services.Resources.GetIcon ("md-package"));
			treeStore.SetValue (iter, ColShowImage, true);
			
			treeStore.SetValue (iter, ColSelected, sel);
		}
		
		public object GetAddinData (AddinInfo info)
		{
			return addinData [info];
		}
		
		public AddinInfo[] GetSelectedAddins ()
		{
			return (AddinInfo[]) selected.ToArray (typeof(AddinInfo));
		}
		
		TreeIter FindCategory (string namePath)
		{
			TreeIter iter = TreeIter.Zero;
			string[] paths = namePath.Split ('/');
			foreach (string name in paths) {
				TreeIter child;
				if (!FindCategory (iter, name, out child))
					iter = treeStore.AppendValues (null, null, name, "", false, false, null, false);
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
				if (((string) treeStore.GetValue (child, 2)) == name) {
					return true;
				}
			} while (treeStore.IterNext (ref child));

			return false;
		}
		
		public AddinInfo ActiveAddin {
			get {
				Gtk.TreeModel foo;
				Gtk.TreeIter iter;
				if (!treeView.Selection.GetSelected (out foo, out iter))
					return null;
					
				return (AddinInfo) treeStore.GetValue (iter, 0);
			}
		}
		
		public object ActiveAddinData {
			get {
				AddinInfo ai = ActiveAddin;
				return ai != null ? GetAddinData (ai) : null;
			}
		}
		
		public object SaveStatus ()
		{
			TreeIter iter;
			
			if (!treeStore.GetIterFirst (out iter))
				return null;
			ArrayList list = new ArrayList ();
			
			do {
				SaveStatus (list, iter);
			} while (treeStore.IterNext (ref iter));
			
			return list;
		}
		
		public void RestoreStatus (object ob)
		{
			if (ob == null)
				return;
			foreach (TreePath path in (ArrayList)ob)
				treeView.ExpandRow (path, false);			
		}
		
		void SaveStatus (ArrayList list, TreeIter iter)
		{
			Gtk.TreePath path = treeStore.GetPath (iter);
			if (treeView.GetRowExpanded (path))
				list.Add (path);
			else if (treeStore.IterChildren (out iter, iter)) {
				do {
					SaveStatus (list, iter);
				} while (treeStore.IterNext (ref iter));
			}
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
			AddinInfo info = (AddinInfo) treeStore.GetValue (iter, ColAddin);
				
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
			AddinInfo info = (AddinInfo) treeStore.GetValue (iter, ColAddin);
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
