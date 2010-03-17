// IconView.cs
//
// Author:
//   nricciar
//   John Luke  <john.luke@gmail.com>
//
// Copyright (c) 2007 John Luke, nricciar
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

namespace MonoDevelop.Components {
	[System.ComponentModel.Category("MonoDevelop.Components")]
	[System.ComponentModel.ToolboxItem(true)]
	public class IconView : ScrolledWindow {
		Gtk.IconView iconView;
		ListStore store;

		public event EventHandler IconSelected;
		public event EventHandler IconDoubleClicked;

		public IconView ()
		{
			store = new ListStore (typeof (Gdk.Pixbuf), typeof (string), typeof (object));
			iconView = new Gtk.IconView (store);
			iconView.PixbufColumn = 0;
			iconView.ColumnSpacing = 6;
			iconView.RowSpacing = 6;
			CellRendererText cr = new CellRendererText ();
			cr.WrapWidth = 80;
			cr.WrapMode = Pango.WrapMode.Word;
			cr.Yalign = 0.0f;
			cr.Xalign = 0.5f;
			iconView.PackEnd (cr, true);
			iconView.ItemWidth = 80;
			iconView.SetAttributes (cr, "text", 1);

			iconView.SelectionChanged += new EventHandler (HandleIconSelected);
			iconView.ActivateCursorItem += new ActivateCursorItemHandler (HandleDoubleClick);

			this.Add (iconView);
			this.WidthRequest = 350;
			this.HeightRequest = 200;
			this.ShadowType = Gtk.ShadowType.In;
		}

		public void AddIcon (Image icon, string name, object obj)
		{
			store.AppendValues (icon.Pixbuf, name, obj);
		}
		
		public void AddIcon (string stock, Gtk.IconSize sz, string name, object obj)
		{
			store.AppendValues (iconView.RenderIcon (stock, sz, String.Empty), name, obj);
		}
		
		public object CurrentlySelected {
			get {
				TreePath[] paths = iconView.SelectedItems;
				if (paths.Length > 0)
				{
					TreeIter iter;
					store.GetIter (out iter, paths[0]);
					return store.GetValue (iter, 2);
				}
				return null;
			}
			set {
				TreeIter foundIter = TreeIter.Zero, iter;
				if (!store.GetIterFirst (out iter))
					return;
				
				do {
					if (value == store.GetValue (iter, 2)) {
						foundIter = iter;
						break;
					}
				} while (store.IterNext (ref iter));
				
				if (foundIter.Stamp != TreeIter.Zero.Stamp) {
					TreePath path = store.GetPath (foundIter);
					iconView.SelectPath (path);
				}
			}
		}

		void HandleDoubleClick (object o, ActivateCursorItemArgs e)
		{
			if ( IconDoubleClicked != null)
				IconDoubleClicked (this, EventArgs.Empty);
		}

		void HandleIconSelected (object o, EventArgs args)
		{
			if (IconSelected != null)
				IconSelected (this, EventArgs.Empty);
		}

		public void Clear ()
		{
			store.Clear ();
		}
	}
}
