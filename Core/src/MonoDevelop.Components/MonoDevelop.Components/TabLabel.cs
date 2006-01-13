//
// Author: John Luke  <jluke@cfl.rr.com>
// License: LGPL
//

using System;
using System.Drawing;
using Gtk;
using Gdk;

namespace MonoDevelop.Components
{
	public class TabLabel : HBox
	{
		private Label title;
		private Gtk.Image icon;
		private Button btn;
		private EventBox titleBox;
		Tooltips tips;
		
		protected TabLabel (IntPtr p): base (p)
		{
		}

		public TabLabel (Label label, Gtk.Image icon) : base (false, 2)
		{
			this.titleBox = new EventBox ();
			this.icon = icon;
			this.PackStart (icon, false, true, 2);

			title = label;
			titleBox.Add (title);
			this.PackStart (titleBox, true, true, 0);
			
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
		
		public void SetTooltip (string tip, string desc)
		{
			if (tips == null) tips = new Tooltips ();
			tips.SetTip (titleBox, tip, desc);
		}
	}
}
