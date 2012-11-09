//
// WelcomePageBarButton.cs
//
// Author:
//       lluis <${AuthorEmail}>
//
// Copyright (c) 2012 lluis
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
using System;
using Gtk;
using MonoDevelop.Core;
using System.Xml.Linq;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageBarButton: EventBox
	{
		Gtk.Image image;
		Gtk.Label label;

		Gdk.Pixbuf imageNormal, imageHover;
		bool mouseOver;
		string actionLink;
		private static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);

		public string FontFamily { get; set; }
		public string HoverColor { get; set; }
		public string Color { get; set; }
		public int FontSize { get; set; }
		protected string Text { get; set; }
		protected bool Bold { get; set; }

		public WelcomePageBarButton (string title, string href, string iconResource = null)
		{
			FontFamily = Platform.IsMac ? Styles.WelcomeScreen.FontFamilyMac : Styles.WelcomeScreen.FontFamilyWindows;
			HoverColor = Styles.WelcomeScreen.Links.HoverColor;
			Color = Styles.WelcomeScreen.Links.Color;
			FontSize = Styles.WelcomeScreen.Links.FontSize;

			VisibleWindow = false;
			this.Text = GettextCatalog.GetString (title);
			this.actionLink = href;
			if (!string.IsNullOrEmpty (iconResource)) {
				imageHover = Gdk.Pixbuf.LoadFromResource (iconResource);
				imageNormal = ImageService.MakeTransparent (imageHover, 0.7);
			}

			HBox box = new HBox ();
			box.Spacing = Styles.WelcomeScreen.Links.IconTextSpacing;
			image = new Image ();
			label = new Label ();
			box.PackStart (image, false, false, 0);
			if (imageNormal == null)
				image.NoShowAll = true;
			box.PackStart (label, false, false, 0);
			box.ShowAll ();
			Add (box);

			Update ();

			Events |= (Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonReleaseMask);
		}

		protected void SetImage (Gdk.Pixbuf normal, Gdk.Pixbuf hover)
		{
			imageHover = hover;
			imageNormal = normal;

			if (imageNormal == null) {
				image.NoShowAll = true;
				image.Hide ();
			} else {
				image.NoShowAll = false;
				ShowAll ();
			}

			Update ();
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = hand_cursor;
			mouseOver = true;
			Update ();
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = null;
			mouseOver = false;
			Update ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1 && new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height).Contains ((int)evnt.X, (int)evnt.Y)) {
				OnClicked ();
				return true;
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		protected virtual void OnClicked ()
		{
			WelcomePageSection.DispatchLink (actionLink);
		}

		protected void Update ()
		{
			if (imageNormal != null)
				image.Pixbuf = mouseOver ? imageHover : imageNormal;
			var color = mouseOver ? HoverColor : Color;
			label.Markup = WelcomePageSection.FormatText (FontFamily, FontSize, Bold, color, Text);
		}
	}
}

