
using System;
using System.Collections;
using Gtk;

namespace Stetic.Editor
{
	// This is the internal class the represents the actual two-column
	// icon list. This can't just be handled as an HBox inside the main
	// window, because we need to override SetScrollAdjustments.
	class IconList : Gtk.HBox 
	{
		ThemedIconColumn left, right;
		ArrayList IconNames = new ArrayList ();

		public event EventHandler Activated;

		protected IconList ()
		{
			left = new ThemedIconColumn ();
			PackStart (left);
			right = new ThemedIconColumn ();
			PackStart (right);

			left.Selection.Changed += LeftSelectionChanged;
			right.Selection.Changed += RightSelectionChanged;
			left.RowActivated += RowActivated;
			right.RowActivated += RowActivated;
			left.KeyPressEvent += ColumnKeyPressEvent;
			right.KeyPressEvent += ColumnKeyPressEvent;
		}
		
		protected void AddIcon (string name, Gdk.Pixbuf pixbuf, string label)
		{
			int i = IconNames.Count;
			IconNames.Add (name);
			
			if (i % 2 == 0)
				left.Append (pixbuf, name);
			else
				right.Append (pixbuf, name);
		}
		
		protected void Clear ()
		{
			IconNames.Clear ();
			left.Clear ();
			right.Clear ();
		}
		
		void RowActivated (object obj, RowActivatedArgs args)
		{
			if (Activated != null)
				Activated (this, EventArgs.Empty);
		}

		public int SelectionIndex {
			get {
				Gtk.TreePath[] selection;
				selection = left.Selection.GetSelectedRows ();
				if (selection.Length > 0)
					return selection[0].Indices[0] * 2;
				selection = right.Selection.GetSelectedRows ();
				if (selection.Length > 0)
					return selection[0].Indices[0] * 2 + 1;
				return -1;
			}
			set {
				if (value != -1) {
					if (value % 2 == 0)
						left.SelectRow (value / 2);
					else
						right.SelectRow (value / 2);
				} else {
					left.Selection.UnselectAll ();
					right.Selection.UnselectAll ();
				}
			}
		}
		
		public string Selection {
			get {
				int i = SelectionIndex;
				if (i != -1)
					return (string) IconNames [i];
				else
					return null;
			}
			set {
				if (value != null)
					SelectionIndex = IconNames.IndexOf (value);
				else
					SelectionIndex = -1;
			}
		}

		public event EventHandler SelectionChanged;

		public void Find (string text)
		{
			int selection = SelectionIndex;
			for (int i = (selection + 1) % IconNames.Count; i != selection; i = (i + 1) % IconNames.Count) {
				if (((string)IconNames[i]).IndexOf (text) != -1) {
					SelectionIndex = i;
					return;
				}
			}
			SelectionIndex = -1;
		}

		void LeftSelectionChanged (object obj, EventArgs args)
		{
			if (left.Selection.GetSelectedRows().Length != 0)
				right.Selection.UnselectAll ();
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}

		void RightSelectionChanged (object obj, EventArgs args)
		{
			if (right.Selection.GetSelectedRows().Length != 0)
				left.Selection.UnselectAll ();
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}

		[GLib.ConnectBefore]
		void ColumnKeyPressEvent (object obj, KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Right) {
				if (obj == (object)left) {
					SelectionIndex++;
					right.GrabFocus ();
				}
				args.RetVal = true;
			} else if (args.Event.Key == Gdk.Key.Left) {
				if (obj == (object)right) {
					SelectionIndex--;
					left.GrabFocus ();
				}
				args.RetVal = true;
			}
		}

		protected override void OnSetScrollAdjustments (Gtk.Adjustment hadj, Gtk.Adjustment vadj)
		{
			left.SetScrollAdjustments (null, vadj);
			right.SetScrollAdjustments (null, vadj);
		}
	}
	
	// Another internal class. This is a single column of the ThemedIconList
	class ThemedIconColumn : Gtk.TreeView 
	{
		public ThemedIconColumn ()
		{
			Model = store = new Gtk.ListStore (typeof (Gdk.Pixbuf),
							   typeof (string));
			HeadersVisible = false;
			EnableSearch = false;

			TreeViewColumn col;
			CellRenderer renderer;

			col = new TreeViewColumn ();
			renderer = new CellRendererPixbuf ();
			col.PackStart (renderer, false);
			col.AddAttribute (renderer, "pixbuf", 0);
			renderer = new CellRendererText ();
			col.PackStart (renderer, false);
			col.AddAttribute (renderer, "text", 1);
			AppendColumn (col);
		}

		Gtk.ListStore store;

		public void Append (Gdk.Pixbuf pixbuf, string name)
		{
			if (name.Length > 30)
				name = name.Substring (0, 30) + "...";
			store.AppendValues (pixbuf, name);
		}

		public void SelectRow (int row)
		{
			Gtk.TreeIter iter;
			if (store.IterNthChild (out iter, row)) {
				Gtk.TreePath path = store.GetPath (iter);

				SetCursor (path, null, false);

				// We want the initial selection to be centered
				if (!IsRealized)
					ScrollToCell (path, null, true, 0.5f, 0.0f);
			}
		}
		
		public void Clear ()
		{
			store.Clear ();
		}
	}		
}


