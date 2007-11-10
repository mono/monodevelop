
using System;
using System.Xml;
using System.IO;
using System.Collections;
using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment.Linux
{
	public partial class MenuCategorySelectorDialog : Gtk.Dialog
	{
		TreeStore store;
		Hashtable insertedCats;
		XmlElement categories;
		ArrayList selection = new ArrayList ();
		
		public MenuCategorySelectorDialog()
		{
			this.Build();
			
			store = new TreeStore (typeof(string), typeof(string), typeof(bool));
			tree.Model = store;
			tree.HeadersVisible = false;
			
			TreeViewColumn col = new TreeViewColumn ();
			Gtk.CellRendererToggle tog = new CellRendererToggle ();
			tog.Toggled += OnToggled;
			col.PackStart (tog, false);
			
			Gtk.CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			
			col.AddAttribute (crt, "markup", 1);
			col.AddAttribute (tog, "active", 2);
			tree.AppendColumn (col);
			
			XmlDocument doc = DesktopEntry.GetDesktopInfo ();
			categories = doc.DocumentElement ["Categories"];
			
			store.DefaultSortFunc = new Gtk.TreeIterCompareFunc (CompareNodes);
			store.SetSortColumnId (/* GTK_TREE_SORTABLE_DEFAULT_SORT_COLUMN_ID */ -1, Gtk.SortType.Ascending);
			
			store.AppendValues ("__other", Mono.Unix.Catalog.GetString ("Additional categories"), false);
			
			insertedCats = new Hashtable ();
			insertedCats ["__other"] = null;
			
			foreach (XmlElement cat in categories.SelectNodes ("Category"))
				InsertCategory (cat);
			
			insertedCats = null;
			doc = null;
		}
		
		public ArrayList Selection {
			get { return selection; }
		}
		
		private int CompareNodes (TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			if ((string) store.GetValue (iter1, 0) == "__other")
				return 1;
				
			if ((string) store.GetValue (iter2, 0) == "__other")
				return -1;
			
			string s1 = (string) store.GetValue (iter1, 1);
			string s2 = (string) store.GetValue (iter2, 1);
			return string.Compare (s1, s2);
		}
		
		void InsertCategory (string s)
		{
			if (insertedCats.Contains (s))
				return;
			
			XmlElement cat = (XmlElement) categories.SelectSingleNode ("Category[@name='" + s + "']");
			if (cat == null)
				LoggingService.LogError ("Category not found: " + s);
			else
				InsertCategory (cat);
		}
		
		void InsertCategory (XmlElement cat)
		{
			if (insertedCats.Contains (cat.GetAttribute ("name")))
				return;
			
			insertedCats [cat.GetAttribute ("name")] = null;
			
			string rels = cat.GetAttribute ("related");
			if (rels.Length > 0) {
				foreach (string rel in rels.Split (';'))
					AddNode (cat, rel);
			} else {
				if (cat.GetAttribute ("main") != "yes")
					AddNode (cat, "__other");
				else
					AddNode (cat, null);
			}
		}
		
		void AddNode (XmlElement cat, string parent)
		{
			if (parent == null) {
				AddValues (cat, TreeIter.Zero);
				return;
			}
			
			InsertCategory (parent);
			
			TreeIter iter;
			store.GetIterFirst (out iter);
			
			do {
				AddNode (cat, parent, iter);
			} while (store.IterNext (ref iter));
		}
		
		void AddNode (XmlElement cat, string parent, TreeIter iter)
		{
			if (parent == (string) store.GetValue (iter, 0))
				AddValues (cat, iter);
			
			if (!store.IterChildren (out iter, iter))
				return;
			
			do {
				AddNode (cat, parent, iter);
			} while (store.IterNext (ref iter));
		}
		
		void AddValues (XmlElement cat, TreeIter iter)
		{
			string lab;
			if (cat.GetAttribute ("main") == "yes")
				lab = "<b>" + GLib.Markup.EscapeText (Mono.Unix.Catalog.GetString (cat.GetAttribute ("_label"))) + "</b>";
			else
				lab = GLib.Markup.EscapeText (Mono.Unix.Catalog.GetString (cat.GetAttribute ("_label")));
			
			if (iter.Equals (TreeIter.Zero))
				store.AppendValues (Mono.Unix.Catalog.GetString (cat.GetAttribute ("name")), lab, false);
			else
				store.AppendValues (iter, Mono.Unix.Catalog.GetString (cat.GetAttribute ("name")), lab, false);
		}

		protected virtual void OnToggled(object sender, Gtk.ToggledArgs args)
		{
			TreeIter iter;
			store.GetIterFromString (out iter, args.Path);
			
			bool sel = (bool) store.GetValue (iter, 2);
			string cat = (string) store.GetValue (iter, 0);
			if (sel)
				selection.Remove (cat);
			else
				selection.Add (cat);
			
			store.SetValue (iter, 2, !sel);
			
			// A 'main' category must always be selected when a subcategory is selected 
			while (store.IterParent (out iter, iter)) {
				string txt = (string) store.GetValue (iter, 1);
				if (txt.StartsWith ("<b>")) {
					store.SetValue (iter, 2, true);
					cat = (string) store.GetValue (iter, 0);
					if (!selection.Contains (cat))
						selection.Add (cat);
				}
			}
		}
	}
}
