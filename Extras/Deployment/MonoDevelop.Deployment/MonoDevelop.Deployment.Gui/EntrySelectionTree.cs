
using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.Deployment.Gui
{
	public partial class EntrySelectionTree : Gtk.Bin
	{
		TreeStore store;
		
		public EntrySelectionTree()
		{
			this.Build();
			
			store = new TreeStore (typeof(string), typeof(string), typeof(object));
			tree.Model = store;
			
			tree.HeadersVisible = false;
			Gtk.CellRendererPixbuf cr = new Gtk.CellRendererPixbuf();
			cr.Yalign = 0;
			tree.AppendColumn ("", cr, "stock-id", 0);
			tree.AppendColumn ("", new Gtk.CellRendererText(), "markup", 1);
			
		}
		
		public void Fill (CombineEntry selection)
		{
			AddEntry (TreeIter.Zero, IdeApp.ProjectOperations.CurrentOpenCombine, selection);
		}
		
		void AddEntry (TreeIter iter, CombineEntry entry, CombineEntry selection)
		{
			string icon;
			if (entry is Combine)
				icon = MonoDevelop.Core.Gui.Stock.CombineIcon;
			else if (entry is Project)
				icon = IdeApp.Services.Icons.GetImageForProjectType (((Project)entry).ProjectType);
			else
				icon = MonoDevelop.Core.Gui.Stock.SolutionIcon;
			
			if (!iter.Equals (TreeIter.Zero))
				iter = store.AppendValues (iter, icon, entry.Name, entry);
			else
				iter = store.AppendValues (icon, entry.Name, entry);
			
			if (selection == entry) {
				tree.ExpandToPath (store.GetPath (iter));
				tree.Selection.SelectIter (iter);
			}
			
			if (entry is Combine) {
				foreach (CombineEntry ce in ((Combine)entry).Entries) {
					if (!(ce is PackagingProject))
						AddEntry (iter, ce, selection);
				}
			}
		}
		
		public CombineEntry Selection {
			get {
				Gtk.TreeModel model;
				Gtk.TreeIter iter;
				
				if (tree.Selection.GetSelected (out model, out iter)) {
					return (CombineEntry) store.GetValue (iter, 2);
				} else
					return null;
			}
		}
	}
}
