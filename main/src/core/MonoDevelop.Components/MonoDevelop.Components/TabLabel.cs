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
			icon.Xpad = 2;
			
			EventBox eventBox = new EventBox ();
			eventBox.BorderWidth = 0;
			eventBox.VisibleWindow = false;			
			eventBox.Add (icon);
			this.PackStart (eventBox, false, true, 0);

			titleBox = new EventBox ();
			titleBox.VisibleWindow = false;			
			titleBox.Add (title);
			this.PackStart (titleBox, true, true, 0);
			
			Gtk.Rc.ParseString ("style \"MonoDevelop.TabLabel.CloseButton\" {\n GtkButton::inner-border = {0,0,0,0}\n }\n");
			Gtk.Rc.ParseString ("widget \"*.MonoDevelop.TabLabel.CloseButton\" style  \"MonoDevelop.TabLabel.CloseButton\"\n");
			Button button = new Button ();
			button.CanDefault = false;
			Gtk.Image closeIcon = new Gtk.Image (closeImage);
			closeIcon.SetPadding (0, 0);
			button.Image = closeIcon;
			button.Relief = ReliefStyle.None;
			button.BorderWidth = 0;
			button.SetSizeRequest (18, 18);
			button.Clicked += new EventHandler(ButtonClicked);
			button.Name = "MonoDevelop.TabLabel.CloseButton";
			this.PackStart (button, false, true, 0);
			this.ClearFlag (WidgetFlags.CanFocus);
			this.BorderWidth = 0;

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
