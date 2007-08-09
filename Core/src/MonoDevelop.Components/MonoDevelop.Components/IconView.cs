using System;

using Gtk;

namespace MonoDevelop.Components {
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
