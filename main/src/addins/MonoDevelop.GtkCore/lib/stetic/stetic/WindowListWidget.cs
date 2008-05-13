// WindowListWidget.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;

namespace Stetic
{
	public class WindowListWidget: ScrolledWindow
	{
		TreeView list;
		ListStore store;
		
		public event EventHandler ComponentActivated;
		
		public WindowListWidget()
		{
			ShowAll ();
			list = new TreeView ();
			store = new ListStore (typeof(ProjectItemInfo), typeof(Gdk.Pixbuf), typeof(string));
			list.Model = store;
			list.HeadersVisible = false;

			list.AppendColumn ("", new CellRendererPixbuf (), "pixbuf", 1);
			list.AppendColumn ("", new CellRendererText (), "text", 2);
			
			Add (list);
			ShowAll ();
			
			list.RowActivated += OnRowActivated;
		}
		
		public void Fill (Project project)
		{
			store.Clear ();
			foreach (WidgetInfo wi in project.Widgets) {
				Gdk.Pixbuf pic = null;
				if (wi.IsWindow) {
					ClassDescriptor cd = Stetic.Registry.LookupClassByName ("Gtk.Window");
					if (cd != null)
						pic = cd.Icon;
				} else {
					ClassDescriptor cd = Stetic.Registry.LookupClassByName ("Gtk.Bin");
					if (cd != null)
						pic = cd.Icon;
				}
				store.AppendValues (wi, pic, wi.Name);
			}
		}
		
		public void Clear ()
		{
			store.Clear ();
		}
		
		void OnRowActivated (object s, Gtk.RowActivatedArgs args)
		{
			if (ComponentActivated != null)
				ComponentActivated (this, args);
		}
		
		public ProjectItemInfo Selection {
			get {
				TreeIter it;
				if (!list.Selection.GetSelected (out it))
					return null;
				return (ProjectItemInfo) store.GetValue (it, 0);
			}
			set {
				TreeIter it;
				if (store.GetIterFirst (out it)) {
					do {
						ProjectItemInfo pit = (ProjectItemInfo) store.GetValue (it, 0);
						if (pit == value) {
							list.Selection.SelectIter (it);
							list.ScrollToCell (store.GetPath (it), list.Columns[0], false, 0, 0);
							return;
						}
					} while (store.IterNext (ref it));
				}
				list.Selection.UnselectAll ();
			}
		}
	}
}
