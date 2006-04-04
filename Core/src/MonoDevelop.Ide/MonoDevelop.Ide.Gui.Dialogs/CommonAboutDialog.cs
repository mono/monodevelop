// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Properties;
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
			"Todd Berman",
			"Pedro Abelleira Seco",
			"John Luke",
			"Daniel Kornhauser",
			"Alex Graveley",
			"nricciar",
			"John Bou Antoun",
			"Ben Maurer",
			"Jeroen Zwartepoorte",
			"Gustavo Giráldez",
			"Miguel de Icaza",
			"Inigo Illan",
			"Iain McCoy",
			"Nick Drochak",
			"Paweł Różański",
			"Richard Torkar",
			"Erik Dasque",
			"Paco Martinez",
			"Lluis Sanchez Gual",
			"Christian Hergert",
			"Jacob Ilsø Christensen"
		};
		
		public ScrollBox ()
		{
			this.SetSizeRequest (450, 220);
			this.Realized += new EventHandler (OnRealized);
			this.ExposeEvent += new ExposeEventHandler (OnExposed);
			
			image = new Gdk.Pixbuf (GetType().Assembly, "Icons.AboutImage");
			monoPowered = new Gdk.Pixbuf (GetType().Assembly, "mono-powered.png");
			
			TimerHandle = GLib.Timeout.Add (50, new TimeoutHandler (ScrollDown));
		}

		string CreditText {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.Append (GettextCatalog.GetString ("<b>Ported and developed by:</b>\n"));

				foreach (string s in authors)
				{
					sb.Append (s);
					sb.Append ("\n");
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
			this.QueueDrawArea (0, 0, 450, 220);
			return true;
		}
		
		private void DrawImage ()
		{
			if (image != null) {
				this.GdkWindow.DrawPixbuf (this.Style.BackgroundGC (StateType.Normal), image, 0, 0, 0, 0, -1, -1, RgbDither.Normal,  0,  0);
			}
		}

		int GetTextHeight ()
		{
			int w, h;
			layout.GetPixelSize (out w, out h);
			return h;
		}
		
		private void DrawText ()
		{
			int maxHeight = GetTextHeight ();
			
			this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), 0, textTop - scroll, layout);
			this.GdkWindow.DrawPixbuf (this.Style.BackgroundGC (StateType.Normal), monoPowered, 0, 0, (450/2) - (monoPowered.Width/2), textTop - scroll + maxHeight + monoLogoSpacing, -1, -1, RgbDither.Normal,  0,  0);

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
			GdkWindow.GetOrigin (out x, out y);
			textTop = y + image.Height - 30;
			scrollStart = -(220 - textTop);
			scroll = scrollStart;
			
			layout = new Pango.Layout (this.PangoContext);
			// FIXME: this seems wrong but works
			layout.Width = 450 * (int)Pango.Scale.PangoScale;
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
			nb.SetSizeRequest (440, 240);
			this.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 255));
			nb.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 255));
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
