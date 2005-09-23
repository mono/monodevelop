//
// Author: John Luke  <jluke@cfl.rr.com>
// License: LGPL
//

using System;
using System.Drawing;
using Gtk;
using Gdk;

namespace MonoDevelop.Gui.Widgets
{
	public class TabLabel : HBox
	{
		private Label title;
		private Gtk.Image icon;
		private Button btn;
		
		protected TabLabel (IntPtr p): base (p)
		{
		}

		public TabLabel (Label label, Gtk.Image icon) : base (false, 2)
		{
			this.icon = icon;
			this.PackStart (icon, false, true, 2);

			title = label;
			this.PackStart (title, true, true, 0);
			
			btn = new Button ();
			btn.Add (new Gtk.Image (GetType().Assembly, "MonoDevelop.Close.png"));
			btn.Relief = ReliefStyle.None;
			btn.SetSizeRequest (18, 18);
			this.PackStart (btn, false, false, 2);
			this.ClearFlag (WidgetFlags.CanFocus);

			this.ShowAll ();
		}
		
		public Label Label
		{
			get { return title; }
			set { title = value; }
		}
		
		public Gtk.Image Icon
		{
			get { return icon; }
			set { icon = value; }
		}

		public Button Button
		{
			get { return btn; }
		}
	}
}
