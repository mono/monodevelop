//
// DropDownBox.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using Pango;
using Gdk;

namespace MonoDevelop.SourceEditor
{
	public class DropDownBox : Gtk.Button
	{
		Gtk.Label label;
		Gtk.Image image;
		int defaultIconHeight, defaultIconWidth;
		
		public DropDownBoxListWindow.IListDataProvider DataProvider {
			get;
			set;
		}

		public object CurrentItem {
			get;
			set;
		}
		
		public int DefaultIconHeight {
			get { return defaultIconHeight; }
			set {
				defaultIconHeight = value;
				image.HeightRequest = Math.Max (image.HeightRequest, value);
			}
		}
		
		public int DefaultIconWidth  {
			get { return defaultIconWidth; }
			set {
				defaultIconWidth = value;
				image.WidthRequest = Math.Max (image.WidthRequest, value);
			}
		}
		
		public void SetItem (string text, Gdk.Pixbuf icon, object currentItem)
		{
			if (currentItem != CurrentItem) {// don't update when the same item is set.
				label.Text = text;
				this.CurrentItem = currentItem;
				image.Pixbuf = icon;
				image.HeightRequest = Math.Max (image.HeightRequest, DefaultIconHeight);
				image.WidthRequest = Math.Max (image.WidthRequest, DefaultIconWidth);
			//	this.HeightRequest = icon.Height + 4;
				image.Show ();
				this.QueueDraw ();
			}
			
			if (ItemSet != null)
				ItemSet (this, EventArgs.Empty);
		}
		
		public void SetItem (int i)
		{
			SetItem (DataProvider.GetText (i), DataProvider.GetIcon (i), DataProvider.GetTag (i));
		}
		public DropDownBox ()
		{
//			this.Events = Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask;
			this.CanDefault = false;
			HBox hbox = new HBox (false, 0);
			hbox.BorderWidth = 0;
			image = new Gtk.Image ();
			image.SetPadding (0, 0);
			hbox.PackStart (image, false, false, 3);
			
			label = new Label ();
			label.Xalign = 0;
			label.SetPadding (0, 0);
			
			hbox.PackStart (label, true, true, 3);
			
			hbox.PackEnd (new Arrow (ArrowType.Down, ShadowType.None), false, false, 1);
			hbox.PackEnd (new VSeparator (), false, false, 1);
			Child = hbox;
			
			Gtk.Rc.ParseString ("style \"MonoDevelop.DropDownBox\" {\n GtkButton::inner-border = {0,0,0,0}\n }\n");
			Gtk.Rc.ParseString ("widget \"*.MonoDevelop.DropDownBox\" style  \"MonoDevelop.DropDownBox\"\n");
			this.Name = "MonoDevelop.DropDownBox";
		}
		
		protected override void OnDestroyed ()
		{
			DestroyWindow ();
			base.OnDestroyed ();
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (window != null && window.ProcessKey (evnt.Key, evnt.State))
				return true;
			return base.OnKeyPressEvent (evnt);
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			DestroyWindow ();
			return base.OnFocusOutEvent (evnt);
		}

		DropDownBoxListWindow window = null;
		void DestroyWindow ()
		{
			if (window != null) {
				window.Destroy ();
				window = null;
			}
		}
		
		void PositionListWindow ()
		{
			if (window == null)
				return;
			int dx, dy;
			ParentWindow.GetOrigin (out dx, out dy);
			dx += this.Allocation.X;
			dy += this.Allocation.Bottom;
			window.Move (dx, dy);
			window.WidthRequest = Allocation.Width;
			window.Show ();
			window.GrabFocus ();
		}
		
		protected override void OnPressed ()
		{
			if (window != null) {
				DestroyWindow ();
			} else {
				if (DataProvider != null) {
					DataProvider.Reset ();
					if (DataProvider.IconCount > 0) {
						window = new DropDownBoxListWindow (this);
						PositionListWindow ();
						window.SelectItem (CurrentItem);
						window.ShowAll ();
					}
				}
			}
			base.OnPressed ();
		}
		
		public EventHandler ItemSet;
	}
}
