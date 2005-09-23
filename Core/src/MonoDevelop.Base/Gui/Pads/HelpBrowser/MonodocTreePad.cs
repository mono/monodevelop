//
// MonodocTreePad - Pad to embed the monodoc help tree.
//
// Author: Todd Berman <tberman@sevenl.net>
//
// (C) 2003 Todd Berman

using System;
using System.Collections;

using Gtk;
using Monodoc;

using MonoDevelop.Gui;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Pads
{
	public class HelpTree : AbstractPadContent
	{
		TreeStore store;
		TreeView  tree_view;

		ScrolledWindow scroller;
		TreeIter root_iter;
	
		public HelpTree () : base (GettextCatalog.GetString ("Help"), Gtk.Stock.Help)
		{
			tree_view = new TreeView ();

			tree_view.AppendColumn ("name_col", new CellRendererText (), "text", 0);
			tree_view.RowExpanded += new Gtk.RowExpandedHandler (RowExpanded);
			tree_view.Selection.Changed += new EventHandler (RowActivated);
			
			store = new TreeStore (typeof (string), typeof (Node));
			root_iter = store.AppendValues (GettextCatalog.GetString ("Mono Documentation"), Runtime.Documentation.HelpTree);

			PopulateNode (root_iter);

			tree_view.Model = store;
			tree_view.HeadersVisible = false;
			
			scroller = new ScrolledWindow ();
			scroller.ShadowType = Gtk.ShadowType.In;
			scroller.Add (tree_view);

			tree_view.ExpandRow (new TreePath ("0"), false);
			TreeIter child_iter;
		start:
			store.IterChildren (out child_iter, root_iter);
			do {
				if (!store.IterHasChild (child_iter)) {
					store.Remove (ref child_iter);
					goto start;
				}
			} while (store.IterNext (ref child_iter));
			
			Control.ShowAll ();
		}

		Hashtable populated = new Hashtable ();
		void RowExpanded (object o, Gtk.RowExpandedArgs args)
		{
			Node node = (Node)store.GetValue (args.Iter, 1);
			if (node == null)
				return;
			if (populated.ContainsKey (node))
				return;
			if (node.Nodes == null)
				return;
			TreeIter iter;
			if (store.IterChildren (out iter, args.Iter)) {
				do {
					PopulateNode (iter);
				} while (store.IterNext (ref iter));
			}
			populated[node] = true;
		}

		void RowActivated (object o, EventArgs e)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;

			if (tree_view.Selection.GetSelected (out model, out iter)) {

				if (iter.Equals (root_iter)) return;

				Node n = (Node)store.GetValue (iter, 1);
				
				string url = n.URL;
				Node match;
				string s;

				if (n.tree.HelpSource != null) {
					s = n.tree.HelpSource.GetText (url, out match);
					if (s != null) {
						ShowDocs (s, match, url);
						return;
					}
				}

				s = Runtime.Documentation.HelpTree.RenderUrl (url, out match);
				if (s != null) {
					ShowDocs (s, match, url);
					return;
				}
				Runtime.LoggingService.Info ("Couldnt find match");
			}
		}

		void ShowDocs (string text, Node matched_node, string url)
		{
			foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
				if (content.ContentName == GettextCatalog.GetString ("Documentation")) {
					((HelpViewer)content).Render (text, matched_node, url);
					content.WorkbenchWindow.SelectWindow ();
					return;
				}
			}
			HelpViewer new_content = new HelpViewer ();
			new_content.Render (text, matched_node, url);
			WorkbenchSingleton.Workbench.ShowView (new_content, true);
		}

		void PopulateNode (TreeIter parent)
		{
			Node node = (Node)store.GetValue (parent, 1);
			if (node.Nodes == null)
				return;

			foreach (Node n in node.Nodes) {
				store.AppendValues (parent, n.Caption, n);
			}
		}

		public override Gtk.Widget Control {
			get { return scroller; }
		}
	}

}
