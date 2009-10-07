//  CommonAboutDialog.cs
//
// Author:
//   Todd Berman  <tberman@sevenl.net>
//   John Luke  <jluke@cfl.rr.com>
//   Lluis Sanchez Gual  <lluis@novell.com>
//   Viktoria Dudka  <viktoriad@remobjects.com>
//
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2004 John Luke
// Copyright (C) 2008 Novell, Inc.
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
//
//

using System;
using System.Text;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

using Gdk;
using Gtk;
using GLib;
using Pango;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class ScrollBox : DrawingArea
	{
		Pixbuf image;
		Pixbuf monoPowered;
		int scroll;
		Pango.Layout layout;
		int monoLogoSpacing = 80;
		int textTop;
		int scrollPause;
		int scrollStart;
		Gdk.GC backGc;

		internal uint TimerHandle;

		string[] authors = new string[] {
			"Lluis Sanchez Gual",
			"Michael Hutchinson",
			"Mike Krueger",
			"Mike Kestner",
			"Ankit Jain",
			"Jonathan Pobst",
			"Christian Hergert",
			"Levi Bard",
			"Carlo Kok",
			"Viktoria Dudka",
			"Marc Christensen",
			"Andrew Jorgensen",
			"Jérémie Laval",
			"Luciano N. Callero",
			"Zach Lute",
			"Andrea Krüeger"
		};
		
		string[] oldAuthors = new string[] {
			"Aaron Bockover",
			"Alberto Paro",
			"Alejandro Serrano",
			"Alexandre Gomes",
			"Alex Graveley",
			"Alfonso Santos Luaces",
			"Andre Filipe de Assuncao e Brito",
			"Andrea Krüeger",
			"Andrés G. Aragoneses",
			"Andrew Jorgensen",
			"Ankit Jain",
			"Antonio Ognio",
			"Ben Maurer",
			"Ben Motmans",
			"Carlo Kok",
			"Christian Hergert",
			"Daniel Kornhauser",
			"Daniel Morgan",
			"David Makovský",
			"Eric Butler",
			"Erik Dasque",
			"Geoff Norton",
			"Gustavo Giráldez",
			"Iain McCoy",
			"Inigo Illan",
			"Jacob Ilsø Christensen",
			"James Fitzsimons",
			"Jeff Stedfast",
			"Jérémie Laval",
			"Jeroen Zwartepoorte",
			"John BouAnton",
			"John Luke",
			"Joshua Tauberer",
			"Jonathan Hernández Velasco",
			"Jonathan Pobst",
			"Levi Bard",
			"Lluis Sanchez Gual",
			"Luciano N. Callero",
			"Marc Christensen",
			"Marcos David Marín Amador",
			"Martin Willemoes Hansen",
			"Marek Sieradzki",
			"Matej Urbas",
			"Maurício de Lemos Rodrigues Collares Neto",
			"Michael Hutchinson",
			"Miguel de Icaza",
			"Mike Krüger",
			"Mike Kestner",
			"Mitchell Wheeler",
			"Muthiah Annamalai",
			"Nick Drochak",
			"nricciar",
			"Paco Martínez",
			"Pawel Rozanski",
			"Pedro Abelleira Seco",
			"Peter Johanson",
			"Philip Turnbull",
			"Richard Torkar",
			"Rolf Bjarne Kvinge",
			"Rusty Howell",
			"Scott Ellington",
			"Thomas Wiest",
			"Todd Berman",
			"Vincent Daron",
			"Vinicius Depizzol",
			"Viktoria Dudka",
			"Wade Berrier",
			"Yan-ren Tsai",
			"Zach Lute"
		};

		public ScrollBox ()
		{
			this.Realized += new EventHandler (OnRealized);
			this.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (49, 49, 74));

			image = new Gdk.Pixbuf (GetType ().Assembly, "AboutImage.png");
			monoPowered = new Gdk.Pixbuf (GetType ().Assembly, "mono-powered.png");
			this.SetSizeRequest (450, image.Height - 1);

			TimerHandle = GLib.Timeout.Add (50, new TimeoutHandler (ScrollDown));
		}

		string CreditText {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.Append (GettextCatalog.GetString ("<b>Contributors to this Release</b>\n\n"));

				for (int n = 0; n < authors.Length; n++) {
					sb.Append (authors[n]);
					if (n % 2 == 1)
						sb.Append ("\n"); else
						sb.Append (",  ");
				}

				sb.Append ("\n\n<b>" + GettextCatalog.GetString ("Previous Contributors") + "</b>\n\n");
				for (int n = 0; n < oldAuthors.Length; n++) {
					sb.Append (oldAuthors[n]);
					if (n % 2 == 1)
						sb.Append ("\n"); else
						sb.Append (",  ");
				}

				string trans = GettextCatalog.GetString ("translator-credits");
				if (trans != "translator-credits") {
					sb.Append (GettextCatalog.GetString ("\n\n<b>Translated by:</b>\n\n"));
					sb.Append (trans);
				}
				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendLine (GettextCatalog.GetString ("<b>Using some icons from:</b>"));
				sb.AppendLine ();
				sb.Append ("http://www.famfamfam.com/lab/icons/silk");
				return sb.ToString ();
			}
		}

		bool ScrollDown ()
		{
			if (scrollPause > 0) {
				if (--scrollPause == 0)
					++scroll;
			} else
				++scroll;
			int w, h;
			this.GdkWindow.GetSize (out w, out h);
			this.QueueDrawArea (0, 0, w, image.Height);
			return true;
		}

		private void DrawImage ()
		{
			if (image != null) {
				int w, h;
				this.GdkWindow.GetSize (out w, out h);
				this.GdkWindow.DrawPixbuf (backGc, image, 0, 0, (w - image.Width) / 2, 0, -1, -1, RgbDither.Normal, 0,
				0);
			}
		}

//		int GetTextHeight ()
//		{
//			int w, h;
//			layout.GetPixelSize (out w, out h);
//			return h;
//		}

		private void DrawText ()
		{
			int width, height;
			GdkWindow.GetSize (out width, out height);

			int widthPixel, heightPixel;
			layout.GetPixelSize (out widthPixel, out heightPixel);

			GdkWindow.DrawLayout (Style.WhiteGC, 0, textTop - scroll, layout);
			GdkWindow.DrawPixbuf (backGc, monoPowered, 0, 0, (width / 2) - (monoPowered.Width / 2), textTop - scroll + heightPixel + monoLogoSpacing, -1, -1, RgbDither.Normal, 0,
			0);

			heightPixel = heightPixel - 80 + image.Height;

			if ((scroll == heightPixel) && (scrollPause == 0))
				scrollPause = 60;
			if (scroll > heightPixel + monoLogoSpacing + monoPowered.Height)
				scroll = scrollStart;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			int w, h;

			this.GdkWindow.GetSize (out w, out h);
			this.DrawText ();
			this.DrawImage ();
//			this.GdkWindow.DrawRectangle (backGc, true, 0, 210, w, 10);
			return false;
		}

		protected void OnRealized (object o, EventArgs args)
		{
			int x, y;
			int w, h;
			GdkWindow.GetOrigin (out x, out y);
			GdkWindow.GetSize (out w, out h);

			textTop = y + image.Height - 30;
			scrollStart = -(image.Height - textTop);
			scroll = scrollStart;

			layout = new Pango.Layout (this.PangoContext);
			// FIXME: this seems wrong but works
			layout.Width = w * (int)Pango.Scale.PangoScale;
			layout.Wrap = Pango.WrapMode.Word;
			layout.Alignment = Pango.Alignment.Center;
			FontDescription fd = FontDescription.FromString ("Tahoma 10");
			layout.FontDescription = fd;
			layout.SetMarkup (CreditText);

			backGc = new Gdk.GC (GdkWindow);
			backGc.RgbBgColor = new Gdk.Color (49, 49, 74);
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			backGc.Dispose ();
		}

	}

	internal class CommonAboutDialog : Dialog
	{
		ScrollBox aboutPictureScrollBox;
		Pixbuf imageSep;

		public CommonAboutDialog ()
		{
			Title = GettextCatalog.GetString ("About MonoDevelop");
			TransientFor = IdeApp.Workbench.RootWindow;
			AllowGrow = false;
			HasSeparator = false;

			VBox.BorderWidth = 0;

			aboutPictureScrollBox = new ScrollBox ();

			VBox.PackStart (aboutPictureScrollBox, false, false, 0);
			imageSep = new Pixbuf (typeof(CommonAboutDialog).Assembly, "AboutImageSep.png");
			VBox.PackStart (new Gtk.Image (imageSep), false, false, 0);

			Notebook notebook = new Notebook ();
			notebook.BorderWidth = 6;
			notebook.AppendPage (new AboutMonoDevelopTabPage (), new Label (Title));
			notebook.AppendPage (new VersionInformationTabPage (), new Label (GettextCatalog.GetString ("Version Info")));
			VBox.PackStart (notebook, true, true, 4);

			AddButton (Gtk.Stock.Close, (int)ResponseType.Close);

			ShowAll ();
		}

		void ChangeColor (Gtk.Widget w)
		{
			w.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (69, 69, 94));
			w.ModifyBg (Gtk.StateType.Active, new Gdk.Color (69, 69, 94));
			w.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 255));
			w.ModifyFg (Gtk.StateType.Active, new Gdk.Color (255, 255, 255));
			w.ModifyFg (Gtk.StateType.Prelight, new Gdk.Color (255, 255, 255));
			Gtk.Container c = w as Gtk.Container;
			if (c != null) {
				foreach (Widget cw in c.Children)
					ChangeColor (cw);
			}
		}

		public new int Run ()
		{
			int tmp = base.Run ();
			GLib.Source.Remove (aboutPictureScrollBox.TimerHandle);
			return tmp;
		}
	}
}
