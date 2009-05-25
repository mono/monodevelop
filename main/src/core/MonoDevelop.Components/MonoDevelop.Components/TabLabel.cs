//
// Authors: John Luke  <jluke@cfl.rr.com>
//          Jacob Ilsø Christensen <jacobilsoe@gmail.com>
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
		private EventBox titleBox;
		private Tooltips tips;
		private static Gdk.Pixbuf closeImage;
		
		static TabLabel ()
		{
			closeImage = Gdk.Pixbuf.LoadFromResource ("MonoDevelop.Close.png");
		}
		
		protected TabLabel (IntPtr p): base (p)
		{
		}

		public TabLabel (Label label, Gtk.Image icon) : base (false, 0)
		{	
			this.title = label;
			this.icon = icon;
			
			EventBox eventBox = new EventBox ();
			eventBox.VisibleWindow = false;			
			eventBox.Add (icon);
			this.PackStart (eventBox, false, true, 2);

			titleBox = new EventBox ();
			titleBox.VisibleWindow = false;			
			titleBox.Add (title);
			this.PackStart (titleBox, true, true, 0);
			
			Button button = new Button ();
			button.Add (new Gtk.Image (closeImage));
			button.Relief = ReliefStyle.None;
			button.SetSizeRequest (18, 18);
			button.Clicked += new EventHandler(ButtonClicked);
			this.PackStart (button, false, false, 2);
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
		
		public event EventHandler CloseClicked;
				
		public void SetTooltip (string tip, string desc)
		{
			if (tips == null) tips = new Tooltips ();
			tips.SetTip (titleBox, tip, desc);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (evnt.Button == 2 && CloseClicked != null)
			{
				CloseClicked(this, null);
				return true;
			}
							
			return false;
		}
							
		private void ButtonClicked(object o, EventArgs eventArgs)
		{
			if (CloseClicked != null)
			{
				CloseClicked(this, null);				
			}
		}
	}
}
