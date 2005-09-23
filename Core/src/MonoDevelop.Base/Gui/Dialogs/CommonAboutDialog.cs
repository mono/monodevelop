// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text;

using MonoDevelop.Gui;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

using Gdk;
using Gtk;
using GLib;
using Pango;

namespace MonoDevelop.Gui.Dialogs
{
	internal class ScrollBox : DrawingArea
	{
		Pixbuf image;
		int scroll = -220;
		Pango.Layout layout;

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
			"Christian Hergert"
		};
		
		public ScrollBox ()
		{
			this.SetSizeRequest (400, 220);
			this.Realized += new EventHandler (OnRealized);
			this.ExposeEvent += new ExposeEventHandler (OnExposed);
			
			image = new Gdk.Pixbuf (GetType().Assembly, "Icons.AboutImage");
			
			TimerHandle = GLib.Timeout.Add (50, new TimeoutHandler (ScrollDown));
		}

		string CreditText {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.Append ("<b>Ported and developed by:</b>\n");

				foreach (string s in authors)
				{
					sb.Append (s);
					sb.Append ("\n");
				}

				string trans = GettextCatalog.GetString ("translator-credits");
				if (trans != "translator-credits")
				{
					sb.Append ("\n\n<b>Translated by:</b>\n");
					sb.Append (trans);
				}
				return sb.ToString ();
			}
		}
		
		bool ScrollDown ()
		{
			++scroll;
			this.QueueDrawArea (200, 0, 200, 220);
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
			// FIXME: calculate this
			// nameCount * lineHeight
			return 325;
		}
		
		private void DrawText ()
		{
			this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), 200, 0 - scroll, layout);
	
			if (scroll > GetTextHeight ())
				scroll = -scroll;
		}
		
		protected void OnExposed (object o, ExposeEventArgs args)
		{
			this.DrawImage ();
			this.DrawText ();
		}

		protected void OnRealized (object o, EventArgs args)
		{
			layout = new Pango.Layout (this.PangoContext);
			// FIXME: this seems wrong but works
			layout.Width = 253952;
			layout.Wrap = Pango.WrapMode.Word;
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
			this.Title = GettextCatalog.GetString ("About MonoDevelop");
			this.TransientFor = (Gtk.Window) WorkbenchSingleton.Workbench;
			aboutPictureScrollBox = new ScrollBox ();
		
			this.VBox.PackStart (aboutPictureScrollBox, false, false, 0);
		
			Notebook nb = new Notebook ();
			nb.SetSizeRequest (400, 280);
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
			// FIXME: remove?
			//GLib.Timeout.Remove (timerHandle);
			return tmp;
		}
	}
}
