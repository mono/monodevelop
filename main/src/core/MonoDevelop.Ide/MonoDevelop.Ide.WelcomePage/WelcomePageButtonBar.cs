//
// WelcomePageButtonBar.cs
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
using System.Xml.Linq;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageButtonBar: HBox
	{
		Gdk.Pixbuf sticker;

		public WelcomePageButtonBar (XElement el)
		{
			Spacing = Styles.WelcomeScreen.Links.LinkSeparation;

			Gtk.Alignment alignment = new Alignment (1, 0.5f, 0, 0);
			alignment.SetPadding (0, 0, 20, 20);
			PackStart (alignment, true, true, 0);

			Gtk.HBox hbox = new HBox ();
			foreach (var child in el.Elements ()) {
				if (child.Name != "Link")
					throw new InvalidOperationException ("Unexpected child '" + child.Name + "'");
				var button = new WelcomePageBarButton (child);
				hbox.PackStart (button, false, false, Styles.WelcomeScreen.Links.LinkSeparation / 2);
			}
			alignment.Add (hbox);
			ShowAll ();

			SetSizeRequest (-1, 40);

			sticker = Gdk.Pixbuf.LoadFromResource ("bar-sticker.png");
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Color = new Cairo.Color (.14, .15, .16);
				context.Rectangle (evnt.Area.ToCairoRect ());
				context.Fill ();

				Gdk.CairoHelper.SetSourcePixbuf (context, sticker, 0, 0);
				context.MoveTo (Allocation.X, Allocation.Y);
				context.Paint ();

				context.MoveTo (Allocation.X, Allocation.Bottom + 0.5);
				context.LineTo (Allocation.Right, Allocation.Bottom + 0.5);
				context.LineWidth = 1;
				context.Color = new Cairo.Color (.2, .21, .22);
				context.Stroke ();

				Pango.Layout layout = new Pango.Layout (PangoContext);
				layout.SetText (BrandingService.ApplicationName);
				layout.FontDescription = Pango.FontDescription.FromString ("normal");
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (18);

				int w, h;
				layout.GetPixelSize (out w, out h);

				context.MoveTo (Allocation.X + 150, Allocation.Y + (Allocation.Height / 2) - (h / 2) - 1);
				context.Color = new Cairo.Color (1, 1, 1);
				Pango.CairoHelper.ShowLayout (context, layout);

				// draw outside of allocation, evil
				context.Rectangle (Allocation.X, Allocation.Y + Allocation.Height, Allocation.Width, 3);
				using (var lg = new Cairo.LinearGradient (0, Allocation.Y + Allocation.Height, 0, Allocation.Y + Allocation.Height + 3)) {
					lg.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.2));
					lg.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
					context.Pattern = lg;
					context.Fill ();
				}
			}
			return base.OnExposeEvent (evnt);
		}
	}
}

