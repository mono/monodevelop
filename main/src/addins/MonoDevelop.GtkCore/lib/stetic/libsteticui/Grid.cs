using Gtk;
using System;
using System.Collections;

namespace Stetic {

	internal class Grid : Gtk.Container {

		public Grid () : base ()
		{
			BorderWidth = 2;
			WidgetFlags |= WidgetFlags.NoWindow;
			lines = new ArrayList ();
			tips = new Gtk.Tooltips ();
			group = null;
		}

		Gtk.Tooltips tips;

		// Padding constants
		const int groupPad = 6;
		const int hPad = 6;
		const int linePad = 3;

		// Theme-based sizes; computed at first SizeRequest
		static int indent = -1;
		static int lineHeight = -1;

		Grid[] group;
		public static void Connect (params Grid[] grids)
		{
			for (int i = 0; i < grids.Length; i++) {
				grids[i].group = new Grid[grids.Length - 1];
				Array.Copy (grids, 0, grids[i].group, 0, i);
				Array.Copy (grids, i + 1, grids[i].group, i, grids.Length - i - 1);
			}
		}

		class Pair {
			Gtk.Widget label;
			Gtk.Widget editor;

			public Pair (Grid grid, string name, Widget editor) : this (grid, name, editor, null) {}

			public Pair (Grid grid, string name, Widget editor, string description)
			{
				Gtk.Label l = new Label (name);
				l.UseMarkup = true;
				l.Justify = Justification.Left;
				l.Xalign = 0;
				l.Show ();

				if (description == null)
					label = l;
				else {
					Gtk.EventBox ebox = new Gtk.EventBox ();
					ebox.Add (l);
					ebox.Show ();
					grid.tips.SetTip (ebox, description, null);
					label = ebox;
				}
				label.Parent = grid;

				this.editor = editor;
				editor.Parent = grid;
				editor.Show ();
			}

			public Widget Label {
				get {
					return label;
				}
			}

			public Widget Editor {
				get {
					return editor;
				}
			}
		}

		// list of widgets and Stetic.Grid.Pairs
		ArrayList lines;

		public void Append (Widget w)
		{
			w.Parent = this;
			w.Show ();

			lines.Add (w);
			QueueDraw ();
		}

		public void Append (Widget w, string description)
		{
			if ((w.WidgetFlags & WidgetFlags.NoWindow) != 0) {
				Gtk.EventBox ebox = new Gtk.EventBox ();
				ebox.Add (w);
				ebox.Show ();
				w = ebox;
			}
			w.Parent = this;
			w.Show ();

			tips.SetTip (w, description, null);

			lines.Add (w);
			QueueDraw ();
		}

		public void AppendLabel (string text)
		{
			Gtk.Label label = new Label (text);
			label.UseMarkup = true;
			label.Justify = Justification.Left;
			label.Xalign = 0;
			Append (label);
		}

		public void AppendGroup (string name, bool expanded)
		{
			Gtk.Expander exp = new Expander ("<b>" + name + "</b>");
			exp.UseMarkup = true;
			exp.Expanded = expanded;
			exp.AddNotification ("expanded", ExpansionChanged);
			Append (exp);
		}

		public void AppendPair (string label, Widget editor, string description)
		{
			Stetic.Grid.Pair pair = new Pair (this, label, editor, description);
			lines.Add (pair);
			QueueDraw ();
		}

		protected override void OnRemoved (Widget w)
		{
			w.Unparent ();
		}

		void ExpansionChanged (object obj, GLib.NotifyArgs args)
		{
			Gtk.Expander exp = obj as Gtk.Expander;

			int ind = lines.IndexOf (exp);
			if (ind == -1)
				return;

			ind++;
			while (ind < lines.Count && !(lines[ind] is Gtk.Expander)) {
				if (lines[ind] is Widget) {
					Widget w = (Widget)lines[ind];
					if (exp.Expanded)
						w.Show ();
					else
						w.Hide ();
				} else if (lines[ind] is Pair) {
					Pair p = (Pair)lines[ind];
					if (exp.Expanded) {
						p.Label.Show ();
						p.Editor.Show ();
					} else {
						p.Label.Hide ();
						p.Editor.Hide ();
					}
				}
				ind++;
			}

			QueueDraw ();
		}

		protected void Clear ()
		{
			foreach (object obj in lines) {
				if (obj is Widget)
					((Widget)obj).Destroy ();
				else if (obj is Pair) {
					Pair p = (Pair)obj;
					p.Label.Destroy ();
					p.Editor.Destroy ();
				}
			}

			lines.Clear ();
			tips = new Gtk.Tooltips ();
		}

		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			if (!include_internals)
				return;

			foreach (object obj in lines) {
				if (obj is Widget)
					callback ((Widget)obj);
				else if (obj is Pair) {
					Pair p = (Pair)obj;
					callback (p.Label);
					callback (p.Editor);
				}
			}
		}

		// These are figured out at requisition time and used again at
		// allocation time.
		int lwidth, ewidth;

		void SizeRequestGrid (Grid grid, ref Gtk.Requisition req)
		{
			bool visible = true;

			req.Width = req.Height = 0;
			foreach (object obj in grid.lines) {
				if (obj is Expander) {
					Gtk.Widget w = (Gtk.Widget)obj;
					Gtk.Requisition childreq;

					childreq = w.SizeRequest ();
					if (req.Width < childreq.Width)
						req.Width = childreq.Width;
					req.Height += groupPad + childreq.Height;

					visible = ((Gtk.Expander)obj).Expanded;

					if (indent == -1) {
						// Seems like there should be an easier way...
						int focusWidth = (int)w.StyleGetProperty ("focus-line-width");
						int focusPad = (int)w.StyleGetProperty ("focus-padding");
						int expanderSize = (int)w.StyleGetProperty ("expander-size");
						int expanderSpacing = (int)w.StyleGetProperty ("expander-spacing");
						indent = focusWidth + focusPad + expanderSize + 2 * expanderSpacing;
					}
				} else if (obj is Widget) {
					Gtk.Widget w = (Gtk.Widget)obj;
					Gtk.Requisition childreq;

					childreq = w.SizeRequest ();
					if (lwidth < childreq.Width)
						lwidth = childreq.Width;
					if (visible)
						req.Height += linePad + childreq.Height;
				} else if (obj is Pair) {
					Pair p = (Pair)obj;
					Gtk.Requisition lreq, ereq;

					lreq = p.Label.SizeRequest ();
					ereq = p.Editor.SizeRequest ();

					if (lineHeight == -1)
						lineHeight = (int)(1.5 * lreq.Height);

					if (lreq.Width > lwidth)
						lwidth = lreq.Width;
					if (ereq.Width > ewidth)
						ewidth = ereq.Width;

					if (visible)
						req.Height += Math.Max (lineHeight, ereq.Height) + linePad;
				}
			}

			req.Width = Math.Max (req.Width, indent + lwidth + hPad + ewidth);
			req.Height += 2 * (int)BorderWidth;
			req.Width += 2 * (int)BorderWidth;
		}

		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			lwidth = ewidth = 0;

			if (group != null) {
				foreach (Grid grid in group)
					SizeRequestGrid (grid, ref req);
			}

			SizeRequestGrid (this, ref req);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle alloc)
		{
			int xbase = alloc.X + (int)BorderWidth;
			int ybase = alloc.Y + (int)BorderWidth;

			base.OnSizeAllocated (alloc);

			int y = ybase;
			bool visible = true;

			foreach (object obj in lines) {
				if (!visible && !(obj is Expander))
					continue;

				if (obj is Widget) {
					Gtk.Widget w = (Gtk.Widget)obj;
					if (!w.Visible)
						continue;

					Gdk.Rectangle childalloc;
					Gtk.Requisition childreq;

					childreq = w.ChildRequisition;

					if (obj is Expander) {
						childalloc.X = xbase;
						childalloc.Width = alloc.Width - 2 * (int)BorderWidth;
						visible = ((Gtk.Expander)obj).Expanded;
						y += groupPad;
					} else {
						childalloc.X = xbase + indent;
						childalloc.Width = lwidth;
						y += linePad;
					}
					childalloc.Y = y;
					childalloc.Height = childreq.Height;
					w.SizeAllocate (childalloc);

					y += childalloc.Height;
				} else if (obj is Pair) {
					Pair p = (Pair)obj;
					if (!p.Editor.Visible) {
						p.Label.Hide ();
						continue;
					} else if (!p.Label.Visible)
						p.Label.Show ();

					Gtk.Requisition lreq, ereq;
					Gdk.Rectangle lalloc, ealloc;

					lreq = p.Label.ChildRequisition;
					ereq = p.Editor.ChildRequisition;

					lalloc.X = xbase + indent;
					if (ereq.Height < lineHeight * 2)
						lalloc.Y = y + (ereq.Height - lreq.Height) / 2;
					else
						lalloc.Y = y + (lineHeight - lreq.Height) / 2;
					lalloc.Width = lwidth;
					lalloc.Height = lreq.Height;
					p.Label.SizeAllocate (lalloc);

					ealloc.X = lalloc.X + lwidth + hPad;
					ealloc.Y = y + Math.Max (0, (lineHeight - ereq.Height) / 2);
					ealloc.Width = Math.Max (ewidth, alloc.Width - 2 * (int)BorderWidth - ealloc.X);
					ealloc.Height = ereq.Height;
					p.Editor.SizeAllocate (ealloc);

					y += Math.Max (ereq.Height, lineHeight) + linePad;
				}
			}
		}
	}
}
