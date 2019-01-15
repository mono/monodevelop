// MonodocTreePad.cs
//
// Author:
//   Todd Berman <tberman@sevenl.net>
//
// Copyright (c) 2003 Todd Berman
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
using System.Collections;

using Gtk;
using Monodoc;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Pads
{
	internal class HelpTree : PadContent
	{
		TreeStore store;
		MonoDevelop.Ide.Gui.Components.PadTreeView  tree_view;

		ScrolledWindow scroller;
		TreeIter root_iter;

		public override string Id {
			get { return "MonoDevelop.Ide.Gui.Pads.HelpTree"; }
		}
	
		public HelpTree ()
		{
			tree_view = new MonoDevelop.Ide.Gui.Components.PadTreeView ();

			tree_view.AppendColumn ("name_col", tree_view.TextRenderer, "text", 0);
			tree_view.RowExpanded += new Gtk.RowExpandedHandler (RowExpanded);
			tree_view.RowActivated += RowActivated;
			
			store = new TreeStore (typeof (string), typeof (Node));
			tree_view.Model = store;
			tree_view.HeadersVisible = false;
			
			scroller = new MonoDevelop.Components.CompactScrolledWindow ();
			scroller.ShadowType = Gtk.ShadowType.None;
			scroller.Add (tree_view);
			
			if (HelpService.HelpTree != null) {
				root_iter = store.AppendValues (GettextCatalog.GetString ("Mono Documentation"), HelpService.HelpTree);
				PopulateNode (root_iter);
	
				tree_view.ExpandRow (new TreePath ("0"), false);
				TreeIter child_iter;
			start:
				if (store.IterChildren (out child_iter, root_iter)) {
					do {
						if (!store.IterHasChild (child_iter)) {
							store.Remove (ref child_iter);
							goto start;
						}
					} while (store.IterNext (ref child_iter));
				}
			}
			scroller.ShowAll ();
		}

		Hashtable populated = new Hashtable ();
		void RowExpanded (object o, Gtk.RowExpandedArgs args)
		{
			Node node = (Node)store.GetValue (args.Iter, 1);
			if (node == null)
				return;
			if (populated.ContainsKey (node))
				return;
#pragma warning disable 618
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
				var path = store.GetPath (iter);
					
				if (path.Equals (store.GetPath (root_iter))) return;

				if (store.IterHasChild (iter)) {
					tree_view.ExpandRow (path, false);
					return;
				}

				Node n = (Node)store.GetValue (iter, 1);
				
				IdeApp.HelpOperations.ShowHelp (n.PublicUrl);
			}
		}

#pragma warning disable 618
		void PopulateNode (TreeIter parent)
		{
			Node node = (Node)store.GetValue (parent, 1);
			if (node.Nodes == null)
				return;

			foreach (Node n in node.Nodes) {
				store.AppendValues (parent, n.Caption, n);
			}
		}

		public override Control Control {
			get { return scroller; }
		}
	}

}
