// 
// WelcomePageFallbackWidget.cs
// 
// Author:
//   Scott Ellington
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// Copyright (c) 2005 Scott Ellington
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
using System.IO;
using System.Xml;
using Gdk;
using Gtk;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Desktop;
using System.Reflection;
using System.Xml.Linq;

namespace MonoDevelop.Ide.WelcomePage
{
	class WelcomePageWidget : Gtk.EventBox
	{
		Gdk.Pixbuf topPixbuf, logoPixbuf, backgroundPixbuf;
		Gtk.HBox colBox;
		
		public WelcomePageWidget ()
		{
			if (WelcomePageBranding.ShowLogo) {
				logoPixbuf = WelcomePageBranding.GetLogoImage ();
				topPixbuf = WelcomePageBranding.GetTopBorderImage ();
			}
			backgroundPixbuf = WelcomePageBranding.GetBackgroundImage ();

			Gdk.Color color = Gdk.Color.Zero;
			if (!Gdk.Color.Parse (WelcomePageBranding.BackgroundColor, ref color))
				color = Style.White;
			ModifyBg (StateType.Normal, color);
			
			var mainAlignment = new Gtk.Alignment (0f, 0f, 1f, 1f);
			if (WelcomePageBranding.ShowLogo) {
				var logoHeight = WelcomePageBranding.LogoHeight;
				mainAlignment.SetPadding ((uint)(logoHeight + WelcomePageBranding.Spacing), 0, (uint)WelcomePageBranding.Spacing, 0);
			} else {
				mainAlignment.SetPadding (Styles.WelcomeScreen.VerticalPadding, Styles.WelcomeScreen.VerticalPadding, Styles.WelcomeScreen.HorizontalPadding, Styles.WelcomeScreen.HorizontalPadding);
			}
			this.Add (mainAlignment);
			
			colBox = new Gtk.HBox (false, WelcomePageBranding.Spacing);
			mainAlignment.Add (colBox);
			
			BuildContent ();
			
			ShowAll ();

			IdeApp.Workbench.GuiLocked += OnLock;
			IdeApp.Workbench.GuiUnlocked += OnUnlock;
		}

		void OnLock (object s, EventArgs a)
		{
			Sensitive = false;
		}
		
		void OnUnlock (object s, EventArgs a)
		{
			Sensitive = true;
		}
		
		void BuildContent ()
		{
			BuildContent (colBox, WelcomePageBranding.Content.Root);
		}

		void BuildContent (Gtk.Box parentBox, XElement root)
		{
			foreach (var el in root.Elements ()) {

				Widget w;
				switch (el.Name.LocalName) {
				case "Column":
					w = new Gtk.VBox (false, WelcomePageBranding.Spacing);
					var widthAtt = el.Attribute ("minWidth");
					if (widthAtt != null) {
						int width = (int) widthAtt;
						w.SizeRequested += delegate (object o, SizeRequestedArgs args) {
							var req = args.Requisition;
							req.Width = Math.Max (req.Width, width);
							args.Requisition = req;
						};
					}
					BuildContent ((Gtk.Box)w, el);
					break;
				case "Row":
					w = new Gtk.HBox (false, WelcomePageBranding.Spacing);
					var heightAtt = el.Attribute ("minHeight");
					if (heightAtt != null) {
						int height = (int) heightAtt;
						w.SizeRequested += delegate (object o, SizeRequestedArgs args) {
							var req = args.Requisition;
							req.Height = Math.Max (req.Height, height);
							args.Requisition = req;
						};
					}
					BuildContent ((Gtk.Box)w, el);
					break;
				case "ButtonBar":
					w = new WelcomePageButtonBar (el);
					break;
				case "Links":
					w = new WelcomePageLinksList (el);
					break;
				case "RecentProjects":
					w = new WelcomePageRecentProjectsList (el);
					break;
				case "NewsFeed":
					w = new WelcomePageNewsFeed (el);
					break;
				case "Youtube":
					w = new WelcomePageYoutubeFeed (el);
					break;
				default:
					throw new InvalidOperationException ("Unknown welcome page element '" + el.Name + "'");
				}

				parentBox.PackStart (w, false, false, 0);
			}
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			//draw the background

			if (backgroundPixbuf != null) {
				var gc = Style.BackgroundGC (State);
				var height = backgroundPixbuf.Height;
				var width = backgroundPixbuf.Width;
				for (int y = Allocation.Y; y < Allocation.Bottom; y += height) {
					if (evnt.Region.RectIn (new Gdk.Rectangle (Allocation.X, y, Allocation.Width, height)) == OverlapType.Out)
						continue;
					for (int x = Allocation.X; x < Allocation.Right && x < evnt.Area.Right; x += width) {
						if (x + width < evnt.Area.X)
							continue;
						evnt.Window.DrawPixbuf (gc, backgroundPixbuf, 0, 0, x, y, width, height, RgbDither.None, 0, 0);
					}
				}
			}

			if (logoPixbuf != null) {
				var gc = Style.BackgroundGC (State);
				var lRect = new Rectangle (Allocation.X, Allocation.Y, logoPixbuf.Width, logoPixbuf.Height);
				if (evnt.Region.RectIn (lRect) != OverlapType.Out)
					evnt.Window.DrawPixbuf (gc, logoPixbuf, 0, 0, lRect.X, lRect.Y, lRect.Width, lRect.Height, RgbDither.None, 0, 0);
				
				var bgRect = new Rectangle (Allocation.X + logoPixbuf.Width, Allocation.Y, Allocation.Width - logoPixbuf.Width, topPixbuf.Height);
				if (evnt.Region.RectIn (bgRect) != OverlapType.Out)
					for (int x = bgRect.X; x < bgRect.Right; x += topPixbuf.Width)
						evnt.Window.DrawPixbuf (gc, topPixbuf, 0, 0, x, bgRect.Y, topPixbuf.Width, bgRect.Height, RgbDither.None, 0, 0);
			}
			
			foreach (Widget widget in Children)
				PropagateExpose (widget, evnt);
			
			return true;
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			IdeApp.Workbench.GuiLocked -= OnLock;
			IdeApp.Workbench.GuiUnlocked -= OnUnlock;
		}
	}
}