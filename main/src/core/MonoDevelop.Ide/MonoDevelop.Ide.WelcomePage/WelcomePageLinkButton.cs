// 
// WelcomePageLinkButton.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
	class WelcomePageLinkButton : Gtk.Button
	{
		static readonly string linkUnderlinedFormat = "<span underline=\"single\" foreground=\"" + WelcomePageBranding.LinkColor + "\">{0}</span>";
		static readonly string linkFormat = "<span foreground=\"" + WelcomePageBranding.LinkColor + "\">{0}</span>";
		static readonly string descFormat = "<span size=\"small\" foreground=\"" + WelcomePageBranding.TextColor + "\">{0}</span>";
		
		Label label;
		Image image;
		string text, desc, icon;
		Gtk.IconSize iconSize = IconSize.Menu;
		HBox box;
		
		public WelcomePageLinkButton (XElement el)
			: this (el, Gtk.IconSize.Menu)
		{
		}
		
		public WelcomePageLinkButton (XElement el, Gtk.IconSize iconSize)
			: this ()
		{
			this.iconSize = iconSize;
			
			string title = (string) (el.Attribute ("title") ?? el.Attribute ("_title"));
			if (string.IsNullOrEmpty (title))
				throw new InvalidOperationException ("Link is missing title");
			this.text = GettextCatalog.GetString (title);
			
			string href = (string) el.Attribute ("href");
			if (string.IsNullOrEmpty (href))
				throw new InvalidOperationException ("Link is missing href");
			this.LinkUrl = href;
			
			string desc = (string) (el.Attribute ("desc") ?? el.Attribute ("_desc"));
			if (!string.IsNullOrEmpty (desc))
				this.desc = GettextCatalog.GetString (desc);
			
			string tooltip = (string) (el.Attribute ("tooltip") ?? el.Attribute ("_tooltip"));
			if (!string.IsNullOrEmpty (tooltip))
				this.TooltipText = GettextCatalog.GetString (tooltip);
			
			string icon = (string) el.Attribute ("icon");
			if (!string.IsNullOrEmpty (icon))
				this.icon = icon;
			
			UpdateLabel (false);
			UpdateImage ();
		}
		
		public WelcomePageLinkButton (string label, string link)
			: this ()
		{
			this.text = label;
			this.LinkUrl = link;
			UpdateLabel (false);
		}
		
		public WelcomePageLinkButton ()
		{
			Relief = ReliefStyle.None;
			
			label = new Label () {
				Xalign = 0,
				Xpad = 0,
				Ypad = 0,
			};
			
			box = new HBox (false, 6);
			box.PackStart (label, true, true, 0);
			Add (box);
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			DestroyStatusBar ();
		}
		
		public string LinkUrl { get; private set; }
		
		public new string Label {
			get {
				return text;
			}
			set {
				text = value;
				UpdateLabel (false);
			}
		}
		
		public string Description {
			get {
				return desc;
			}
			set {
				desc = value;
				UpdateLabel (false);
			}
		}
		
		public string Icon {
			get {
				return icon;
			}
			set {
				icon = value;
				UpdateImage ();
			}
		}
		
		void UpdateLabel (bool underlined)
		{
			string markup = string.Format (underlined? linkUnderlinedFormat : linkFormat, text);
			if (!string.IsNullOrEmpty (desc))
				markup += "\n" + string.Format (descFormat, desc);
			label.Markup = markup;
		}
		
		void UpdateImage ()
		{
			if (string.IsNullOrEmpty (icon)) {
				if (image != null) {
					box.Remove (image);
					image.Destroy ();
					image = null;
				}
				return;
			}
			if (image == null) {
				image = new Gtk.Image ();
				int w, h;
				Gtk.Icon.SizeLookup (iconSize, out w, out h);
				image.IconSize = w;
				box.PackStart (image, false, false, 0);
				box.ReorderChild (image, 0);
			}
			image.Pixbuf = ImageService.GetPixbuf (icon, iconSize);
		}
		
		public int MaxWidthChars {
			get { return label.MaxWidthChars; }
			set { label.MaxWidthChars = value; }
		}
		
		public Pango.EllipsizeMode Ellipsize {
			get { return label.Ellipsize; }
			set { label.Ellipsize = value; }
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			SetLinkStatus (LinkUrl);
			UpdateLabel (true);
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			UpdateLabel (false);
			DestroyStatusBar ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override void OnClicked ()
		{
			base.OnClicked ();
			DispatchLink (LinkUrl);
		}
		
		void DispatchLink (string uri)
		{
			try {
				if (uri.StartsWith ("project://")) {
					string projectUri = uri.Substring ("project://".Length);			
					Uri fileuri = new Uri (projectUri);
					Gdk.ModifierType mtype = Mono.TextEditor.GtkWorkarounds.GetCurrentKeyModifiers ();
					bool inWorkspace = (mtype & Gdk.ModifierType.ControlMask) != 0;
					IdeApp.Workspace.OpenWorkspaceItem (fileuri.LocalPath, !inWorkspace);
				} else if (uri.StartsWith ("monodevelop://")) {
					var cmdId = uri.Substring ("monodevelop://".Length);
					IdeApp.CommandService.DispatchCommand (cmdId, MonoDevelop.Components.Commands.CommandSource.WelcomePage);
				} else {
					DesktopService.ShowUrl (uri);
				}
			} catch (Exception ex) {
				MessageService.ShowException (ex, GettextCatalog.GetString ("Could not open the url '{0}'", uri));
			}
		}
		
		static StatusBarContext statusBar;
		
		void DestroyStatusBar ()
		{
			if (statusBar != null) {
				statusBar.Dispose ();
				statusBar = null;
			}
		}
		
		void SetLinkStatus (string link)
		{
			if (link == null) {
				DestroyStatusBar ();
				return;
			}
			if (link.IndexOf ("monodevelop://") != -1)
				return;
				
			if (statusBar == null)
				statusBar = IdeApp.Workbench.StatusBar.CreateContext ();
			
			if (link.IndexOf ("project://") != -1) {
				string message = link;
				message = message.Substring (10);
				string msg = GettextCatalog.GetString ("Open solution {0}", message);
				if (IdeApp.Workspace.IsOpen)
					msg += " - " + GettextCatalog.GetString ("Hold Control key to open in current workspace.");
				statusBar.ShowMessage (msg);
			} else {
				string msg = GettextCatalog.GetString ("Open {0}", link);
				statusBar.ShowMessage (msg);
			}
		}
	}
}