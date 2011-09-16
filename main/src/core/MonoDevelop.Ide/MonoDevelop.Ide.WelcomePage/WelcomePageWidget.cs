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
		Gdk.Pixbuf bgPixbuf, logoPixbuf;
		Gtk.HBox colBox;
		
		public WelcomePageWidget ()
		{
			logoPixbuf = WelcomePageBranding.GetLogoImage ();
			bgPixbuf = WelcomePageBranding.GetTopBorderImage ();
			
			Gdk.Color color = Gdk.Color.Zero;
			if (!Gdk.Color.Parse (WelcomePageBranding.BackgroundColor, ref color))
				color = Style.White;
			ModifyBg (StateType.Normal, color);
			
			var mainAlignment = new Gtk.Alignment (0f, 0f, 1f, 1f);
			mainAlignment.SetPadding ((uint) (WelcomePageBranding.LogoHeight + WelcomePageBranding.Spacing), 0, (uint) WelcomePageBranding.Spacing, 0);
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
			foreach (var col in WelcomePageBranding.Content.Root.Elements ("Column")) {
				var colWidget = new Gtk.VBox (false, WelcomePageBranding.Spacing);
				var widthAtt = col.Attribute ("minWidth");
				if (widthAtt != null) {
					int width = (int) widthAtt;
					colWidget.SizeRequested += delegate (object o, SizeRequestedArgs args) {
						var req = args.Requisition;
						req.Width = Math.Max (req.Width, width);
						args.Requisition = req;
					};
				}
				colBox.PackStart (colWidget, false, false, 0);
				
				foreach (var el in col.Elements ()) {
					string title = (string) (el.Attribute ("title") ?? el.Attribute ("_title"));
					if (!string.IsNullOrEmpty (title))
						title = GettextCatalog.GetString (title);
					
					Widget w;
					switch (el.Name.LocalName) {
					case "Links":
						w = new WelcomePageLinksList (el);
						break;
					case "RecentProjects":
						w = new WelcomePageRecentProjectsList (el);
						break;
					case "NewsFeed":
						w = new WelcomePageNewsFeed (el);
						break;
					default:
						throw new InvalidOperationException ("Unknown welcome page element '" + el.Name + "'");
					}
					
					AddSection (colWidget, title, w);
				}
			}
		}
		
		static readonly string headerFormat =
			"<span size=\"" + WelcomePageBranding.HeaderTextSize + "\" foreground=\""
			+ WelcomePageBranding.HeaderTextColor + "\">{0}</span>";
		
		void AddSection (VBox col, string title, Widget w)
		{
			var a = new Alignment (0f, 0f, 1f, 1f);
			a.Add (w);
			
			if (string.IsNullOrEmpty (title)) {
				col.PackStart (a, false, false, 0);
				return;
			}
			
			var box = new VBox (false, 2);
			var label = new Gtk.Label () { Markup = string.Format (headerFormat, title), Xalign = (uint) 0 };
			box.PackStart (label, false, false, 0);
			box.PackStart (a, false, false, 0);
			col.PackStart (box, false, false, 0);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			//draw the background
			if (logoPixbuf != null) {
				var gc = Style.BackgroundGC (State);
				var lRect = new Rectangle (Allocation.X, Allocation.Y, logoPixbuf.Width, logoPixbuf.Height);
				if (evnt.Region.RectIn (lRect) != OverlapType.Out)
					evnt.Window.DrawPixbuf (gc, logoPixbuf, 0, 0, lRect.X, lRect.Y, lRect.Width, lRect.Height, RgbDither.None, 0, 0);
				
				var bgRect = new Rectangle (Allocation.X + logoPixbuf.Width, Allocation.Y, Allocation.Width - logoPixbuf.Width, bgPixbuf.Height);
				if (evnt.Region.RectIn (bgRect) != OverlapType.Out)
					for (int x = bgRect.X; x < bgRect.Right; x += bgPixbuf.Width)
						evnt.Window.DrawPixbuf (gc, bgPixbuf, 0, 0, x, bgRect.Y, bgPixbuf.Width, bgRect.Height, RgbDither.None, 0, 0);
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