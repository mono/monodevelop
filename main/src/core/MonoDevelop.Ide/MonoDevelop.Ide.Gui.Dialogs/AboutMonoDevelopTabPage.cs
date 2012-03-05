// AboutMonoDevelopTabPage.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//   Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2009 RemObjects Software
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

using Gdk;
using Gtk;
using GLib;
using Pango;
using System.Reflection;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	class AboutMonoDevelopTabPage: VBox
	{
		ScrollBox aboutPictureScrollBox;
		Pixbuf imageSep;

		public AboutMonoDevelopTabPage ()
		{
			BorderWidth = 0;

			aboutPictureScrollBox = new ScrollBox ();

			PackStart (aboutPictureScrollBox, false, false, 0);
			using (var stream = BrandingService.GetStream ("AboutImageSep.png", true))
				imageSep = new Pixbuf (stream);
			PackStart (new Gtk.Image (imageSep), false, false, 0);
			
			var label = new Label ();
			label.Markup = string.Format (
				"<b>{0}</b>\n    {1}", 
				GettextCatalog.GetString ("Version"), 
				BuildVariables.PackageVersion == BuildVariables.PackageVersionLabel ? BuildVariables.PackageVersionLabel : String.Format ("{0} ({1})", 
				BuildVariables.PackageVersionLabel, 
				BuildVariables.PackageVersion));
			
			var hBoxVersion = new HBox ();
			hBoxVersion.PackStart (label, false, false, 5);
			this.PackStart (hBoxVersion, false, true, 0);
			
			label = null;
			label = new Label ();
			label.Markup = GettextCatalog.GetString ("<b>License</b>\n    {0}", GettextCatalog.GetString ("Released under the GNU Lesser General Public License."));
			var hBoxLicense = new HBox ();
			hBoxLicense.PackStart (label, false, false, 5);
			this.PackStart (hBoxLicense, false, true, 5);
			
			label = null;
			label = new Label ();
			label.Markup = GettextCatalog.GetString ("<b>Copyright</b>\n    (c) 2004-{0} by MonoDevelop contributors", DateTime.Now.Year);
			var hBoxCopyright = new HBox ();
			hBoxCopyright.PackStart (label, false, false, 5);
			this.PackStart (hBoxCopyright, false, true, 5);
			
			this.ShowAll ();
		}
		
		internal class ScrollBox : DrawingArea
		{
			Pixbuf image;
			Pixbuf imageBg;
			int imageHeight, imageWidth;
			Pixbuf monoPowered;
			Pango.Layout layout;
			int monoLogoSpacing = 80;
			Gdk.GC backGc;
			
			int scrollHeightPx, scrollStartPx, scrolledUpPx;
			long startTicks;
			const int fps = 20;
			const int pixelsPerFrame = 1;
			const int pauseSeconds = 5;
			
			internal uint TimerHandle;
			string[] authors = new string[] {
				"Lluis Sanchez Gual",
				"Michael Hutchinson",
				"Mike Krüger",
				"Jeff Stedfast",
				"Alan McGovern",
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
				"Andrea Krüger",
				"Jakub Steiner"
			};
			
			string[] oldAuthors = new string[] {
				"Aaron Bockover",
				"Alberto Paro",
				"Alejandro Serrano",
				"Alexandre Gomes",
				"Alex Graveley",
				"Alfonso Santos Luaces",
				"Andre Filipe de Assuncao e Brito",
				"Andrea Krüger",
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
				"Jakub Steiner",
				"James Fitzsimons",
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
				"Nikhil Sarda",
				"nricciar",
				"Paco Martínez",
				"Pawel Rozanski",
				"Pedro Abelleira Seco",
				"Peter Johanson",
				"Philip Turnbull",
				"Richard Torkar",
				"Rolf Bjarne Kvinge",
				"Rusty Howell",
				"Sanjoy Das",
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
			
			Gdk.Color bgColor = new Gdk.Color (49, 49, 74);
			Gdk.Color textColor = new Gdk.Color (0xFF, 0xFF, 0xFF);
			
			void LoadBranding ()
			{
				try {
					var textColStr = BrandingService.GetString ("AboutBox", "TextColor");
					if (textColStr != null)
						Gdk.Color.Parse (textColStr, ref textColor);
					var bgColStr = BrandingService.GetString ("AboutBox", "BackgroundColor");
					if (bgColStr != null)
						Gdk.Color.Parse (bgColStr, ref bgColor);
					
					//branders may provide either fg image or bg image, or both
					using (var stream = BrandingService.GetStream ("AboutImage.png", false)) {
						image = (stream != null ? new Gdk.Pixbuf (stream) : null);
					}
					using (var streamBg = BrandingService.GetStream ("AboutImageBg.png", false)) {
						imageBg = (streamBg != null ? new Gdk.Pixbuf (streamBg) : null);
					}
					
					//if branding did not provide any image, use the built-in one
					if (imageBg == null && image == null) {
						image = Gdk.Pixbuf.LoadFromResource ("AboutImage.png");
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Error loading about box branding", ex);
				}
			}
			
			public ScrollBox ()
			{
				LoadBranding ();
				this.Realized += new EventHandler (OnRealized);
				this.ModifyBg (Gtk.StateType.Normal, bgColor);
				this.ModifyText (Gtk.StateType.Normal, textColor);
				
				if (image != null) {
					imageHeight = image.Height;
					imageWidth = image.Width;
				}
				if (imageBg != null) {
					imageHeight = Math.Max (imageHeight, imageBg.Height);
					imageWidth = Math.Max (imageWidth, imageBg.Width);
				}
				
				monoPowered = Gdk.Pixbuf.LoadFromResource ("mono-powered.png");
				this.SetSizeRequest (imageWidth, imageHeight - 1);
				
				uint timeout = 1000 / (uint)fps;
				TimerHandle = GLib.Timeout.Add (timeout, delegate {
					this.QueueDrawArea (0, 0, Allocation.Width, imageHeight);
					return true;
				});
				
				this.startTicks = DateTime.Now.Ticks;
			}
			
			string CreditText {
				get {
					var sb = new StringBuilder ();
					sb.Append (GettextCatalog.GetString ("<b>Contributors to this Release</b>\n\n"));
					
					for (int n = 0; n < authors.Length; n++) {
						sb.Append (authors [n]);
						if (n % 2 == 1)
							sb.Append ("\n");
						else if (n < authors.Length - 1)
							sb.Append (",  ");
					}
					
					sb.Append ("\n\n<b>" + GettextCatalog.GetString ("Previous Contributors") + "</b>\n\n");
					for (int n = 0; n < oldAuthors.Length; n++) {
						sb.Append (oldAuthors [n]);
						if (n % 2 == 1)
							sb.Append ("\n");
						else if (n < oldAuthors.Length - 1)
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
			
			void UpdateScrollPosition ()
			{
				long elapsed = DateTime.Now.Ticks - startTicks;
				long pauseTicks = pauseSeconds * TimeSpan.TicksPerSecond;
				long ticksPerPixel = TimeSpan.TicksPerSecond / (pixelsPerFrame * fps);
				long totalScrollTicks = scrollHeightPx * ticksPerPixel  + pauseTicks;
				long tickPosition = elapsed % totalScrollTicks;
				scrolledUpPx = (int) Math.Min ((tickPosition / ticksPerPixel), scrollHeightPx);
			}
			
			void DrawText (Drawable window)
			{
				var alloc = Allocation;
				int pos = scrollStartPx - scrolledUpPx;
				window.DrawLayout (Style.TextGC (StateType.Normal), 0, pos, layout);
				int logoPos = pos + scrollHeightPx - monoPowered.Height / 2 - imageHeight / 2;
				window.DrawPixbuf (backGc, monoPowered, 0, 0, (alloc.Width / 2) - (monoPowered.Width / 2),
					logoPos, -1, -1, RgbDither.Normal, 0, 0);
			}
			
			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				UpdateScrollPosition ();
				
				var alloc = this.Allocation;
				if (imageBg != null) {
					evnt.Window.DrawPixbuf (backGc, imageBg, 0, 0,
						(alloc.Width - imageBg.Width) / 2, 0,
						-1, -1, RgbDither.Normal, 0, 0);
				}
				DrawText (evnt.Window);
				if (this.image != null) {
					evnt.Window.DrawPixbuf (backGc, image, 0, 0,
						(alloc.Width - image.Width) / 2, 0,
						-1, -1, RgbDither.Normal, 0, 0);
				}
				return false;
			}
			
			protected void OnRealized (object o, EventArgs args)
			{
				scrollStartPx = imageHeight;
				
				layout = new Pango.Layout (this.PangoContext);
				// FIXME: this seems wrong but works
				layout.Width = Allocation.Width * (int)Pango.Scale.PangoScale;
				layout.Wrap = Pango.WrapMode.Word;
				layout.Alignment = Pango.Alignment.Center;
				FontDescription fd = FontDescription.FromString ("Tahoma 10");
				layout.FontDescription = fd;
				layout.SetMarkup (CreditText);
				
				int widthPx, heightPx;
				layout.GetPixelSize (out widthPx, out heightPx);
				
				this.scrollHeightPx = heightPx + monoLogoSpacing + monoPowered.Height + imageHeight / 2;
				
				backGc = new Gdk.GC (GdkWindow);
				backGc.RgbBgColor = bgColor;
			}
			
			protected override void OnDestroyed ()
			{
				base.OnDestroyed ();
				backGc.Dispose ();
				GLib.Source.Remove (TimerHandle);
			}
		}
	}
}