//  CommonAboutDialog.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
		int monoLogoSpacing = 30;
		int textTop;
		int scrollPause;
		int scrollStart;

		internal uint TimerHandle;
		
		string[] authors = new string[]
		{
			"Aaron Bockover",
			"Alberto Paro",
			"Alejandro Serrano",
			"Alexandre Gomes",
			"Alex Graveley",
			"Andrés G. Aragoneses",
			"Andre Filipe de Assuncao e Brito",
			"Antonio Ognio",
			"Ankit Jain",
			"Ben Maurer",
			"Ben Motmans",
			"Christian Hergert",
			"Daniel Kornhauser",
			"Daniel Morgan",
			"David Makovský",
			"Eric Butler",
			"Erik Dasque",
			"Franciso Martinez",
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
			"Levi Bard",
			"Lluis Sanchez Gual",
			"Marcos David Marín Amador",
			"Martin Willemoes Hansen",
			"Marek Sieradzki",
			"Matej Urbas",
			"Maurício de Lemos Rodrigues Collares Neto",
			"Michael Hutchinson",
			"Miguel de Icaza",
			"Mike Krüger",
			"Muthiah Annamalai",
			"Nick Drochak",
			"nricciar",
			"Paco Martínez",
			"Pawel Rozanski",
			"Pedro Abelleira Seco",
			"Peter Johanson",
			"Philip Turnbull",
			"Richard Torkar",
			"Scott Ellington",
			"Todd Berman",
			"Vincent Daron",
			"Wade Berrier",
			"Yan-ren Tsai",
			"Zach Lute"
		};
		
		public ScrollBox ()
		{
			this.SetSizeRequest (450, 220);
			this.Realized += new EventHandler (OnRealized);
			this.ExposeEvent += new ExposeEventHandler (OnExposed);
			
			image = new Gdk.Pixbuf (GetType().Assembly, "AboutImage.png");
			monoPowered = new Gdk.Pixbuf (GetType().Assembly, "mono-powered.png");
			
			TimerHandle = GLib.Timeout.Add (50, new TimeoutHandler (ScrollDown));
		}

		string CreditText {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.Append (GettextCatalog.GetString ("<b>Ported and developed by:</b>\n"));

				for (int n=0; n<authors.Length; n++) {
					sb.Append (authors [n]);
					if (n % 2 == 1)
						sb.Append ("\n");
					else
						sb.Append (",  ");
				}

				string trans = GettextCatalog.GetString ("translator-credits");
				if (trans != "translator-credits")
				{
					sb.Append (GettextCatalog.GetString ("\n\n<b>Translated by:</b>\n"));
					sb.Append (trans);
				}
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
			this.QueueDrawArea (0, 0, w, 220);
			return true;
		}
		
		private void DrawImage ()
		{
			if (image != null) {
				int w, h;
				this.GdkWindow.GetSize (out w, out h);
				this.GdkWindow.DrawPixbuf (this.Style.BackgroundGC (StateType.Normal), image, 0, 0, (w - image.Width) / 2, 0, -1, -1, RgbDither.Normal,  0,  0);
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
			int w, h;
			this.GdkWindow.GetSize (out w, out h);
			int tw, maxHeight;
			layout.GetPixelSize (out tw, out maxHeight);
			
			this.GdkWindow.DrawLayout (this.Style.BlackGC, 0, textTop - scroll, layout);
			this.GdkWindow.DrawPixbuf (this.Style.BackgroundGC (StateType.Normal), monoPowered, 0, 0, (w/2) - (monoPowered.Width/2), textTop - scroll + maxHeight + monoLogoSpacing, -1, -1, RgbDither.Normal,  0,  0);

			if (scroll == maxHeight && scrollPause == 0)
				scrollPause = 60;
			if (scroll > maxHeight + monoLogoSpacing + monoPowered.Height)
				scroll = scrollStart;
		}
		
		protected void OnExposed (object o, ExposeEventArgs args)
		{
			int w, h;
			this.GdkWindow.GetSize (out w, out h);
			this.GdkWindow.DrawRectangle (this.Style.WhiteGC, true, 0, 0, w, h);
			this.DrawText ();
			this.DrawImage ();
			this.GdkWindow.DrawRectangle (this.Style.WhiteGC, true, 0, 210, w, 10);
		}

		protected void OnRealized (object o, EventArgs args)
		{
			int x, y;
			int w, h;
			GdkWindow.GetOrigin (out x, out y);
			GdkWindow.GetSize (out w, out h);
			
			textTop = y + image.Height - 30;
			scrollStart = -(220 - textTop);
			scroll = scrollStart;
			
			layout = new Pango.Layout (this.PangoContext);
			// FIXME: this seems wrong but works
			layout.Width = w * (int)Pango.Scale.PangoScale;
			layout.Wrap = Pango.WrapMode.Word;
			layout.Alignment = Pango.Alignment.Center;
			FontDescription fd = FontDescription.FromString ("Tahoma 10");
			layout.FontDescription = fd;
			layout.SetMarkup (CreditText);	
		}
	}
	
	internal class CommonAboutDialog : Dialog
	{
		
		ScrollBox aboutPictureScrollBox;
		
		public CommonAboutDialog ()
		{
			HasSeparator = false;
			this.VBox.BorderWidth = 0;
			
			AllowGrow = false;
			this.Title = GettextCatalog.GetString ("About MonoDevelop");
			this.TransientFor = IdeApp.Workbench.RootWindow;
			aboutPictureScrollBox = new ScrollBox ();
		
			this.VBox.PackStart (aboutPictureScrollBox, false, false, 0);
		
			Notebook nb = new Notebook ();
			nb.BorderWidth = 6;
//			nb.SetSizeRequest (440, 240);
			this.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 255));
//			nb.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 255));
			VersionInformationTabPage vinfo = new VersionInformationTabPage ();
			
			nb.AppendPage (new AboutMonoDevelopTabPage (), new Label (GettextCatalog.GetString ("About MonoDevelop")));

			nb.AppendPage (vinfo, new Label (GettextCatalog.GetString ("Version Info")));
			this.VBox.PackStart (nb, true, true, 0);
			this.AddButton (Gtk.Stock.Close, (int) ResponseType.Close);
			this.ShowAll ();
		}
		
		public new int Run ()
		{
			int tmp = base.Run ();
			GLib.Source.Remove (aboutPictureScrollBox.TimerHandle);
			return tmp;
		}
	}
}
