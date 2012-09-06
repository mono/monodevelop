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

		string text;
		Gdk.Pixbuf imageNormal, imageHover;
		bool mouseOver;
		string actionLink;
		private static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);

		public WelcomePageBarButton (XElement el)
		{
			VisibleWindow = false;
			string title = (string)(el.Attribute ("title") ?? el.Attribute ("_title"));
			if (string.IsNullOrEmpty (title))
				throw new InvalidOperationException ("Link is missing title");
			this.text = GettextCatalog.GetString (title);
			
			string href = (string)el.Attribute ("href");
			if (string.IsNullOrEmpty (href))
				throw new InvalidOperationException ("Link is missing href");
			this.actionLink = href;

			string icon = (string)el.Attribute ("icon");
			if (!string.IsNullOrEmpty (icon)) {
				imageHover = WelcomePageBranding.GetImage (icon, true);
				imageNormal = ImageService.MakeTransparent (imageHover, 0.7);
			}

			HBox box = new HBox ();
			box.Spacing = Styles.WelcomeScreen.Links.IconTextSpacing;
			image = new Image ();
			label = new Label ();
			if (imageNormal != null)
				box.PackStart (image, false, false, 0);
			box.PackStart (label, false, false, 0);
			box.ShowAll ();
			Add (box);

			Update ();

			Events |= (Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonReleaseMask);
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
			WelcomePageSection.DispatchLink (actionLink);
			return base.OnButtonReleaseEvent (evnt);
		}

		void Update ()
		{
			if (imageNormal != null)
				image.Pixbuf = mouseOver ? imageHover : imageNormal;
			var face = Platform.IsMac ? Styles.WelcomeScreen.FontFamilyMac : Styles.WelcomeScreen.FontFamilyWindows;
			var color = mouseOver ? Styles.WelcomeScreen.Links.HoverColor : Styles.WelcomeScreen.Links.Color;
			label.Markup = WelcomePageSection.FormatText (face, Styles.WelcomeScreen.Links.FontSize, color, text);
		}
	}
}

