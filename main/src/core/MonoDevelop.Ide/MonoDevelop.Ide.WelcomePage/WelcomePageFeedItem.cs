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
using System.Text.RegularExpressions;
using MonoDevelop.Components;
using System.Text;

namespace MonoDevelop.Ide.WelcomePage
{
	class WelcomePageFeedItem : Gtk.EventBox
	{
		static string linkUnderlinedFormat;
		static string linkFormat;
		static string descFormat;
		static string subtitleFormat;

		Label titleLabel;
		Label subtitleLabel;
		Label summaryLabel;

		ImageView image;
		string text, desc, icon, subtitle;
		Gtk.IconSize iconSize = IconSize.Menu;
		VBox box;

		const int MaxCharacters = 200;

		private static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);

		static WelcomePageFeedItem ()
		{
			UpdateStyle ();
			Gui.Styles.Changed += (sender, e) => UpdateStyle();
		}

		static void UpdateStyle ()
		{
			var face = Platform.IsMac ? Styles.WelcomeScreen.Pad.TitleFontFamilyMac : Styles.WelcomeScreen.Pad.TitleFontFamilyWindows;
			linkUnderlinedFormat = Styles.GetFormatString (face, Styles.WelcomeScreen.Pad.MediumTitleFontSize, Styles.WelcomeScreen.Pad.News.Item.TitleHoverColor, Pango.Weight.Bold);
			linkFormat = Styles.GetFormatString (face, Styles.WelcomeScreen.Pad.MediumTitleFontSize, Styles.WelcomeScreen.Pad.MediumTitleColor, Pango.Weight.Bold);
			descFormat = Styles.GetFormatString (Styles.WelcomeScreen.Pad.SummaryFontFamily, Styles.WelcomeScreen.Pad.SummaryFontSize, Styles.WelcomeScreen.Pad.TextColor);
			subtitleFormat = Styles.GetFormatString (face, Styles.WelcomeScreen.Pad.SmallTitleFontSize, Styles.WelcomeScreen.Pad.SmallTitleColor);
		}
		
		public WelcomePageFeedItem (XElement el)
			: this (el, Gtk.IconSize.Menu)
		{
		}
		
		public WelcomePageFeedItem (XElement el, Gtk.IconSize iconSize)
			: this ()
		{
			this.iconSize = iconSize;
			
			string title = (string)(el.Attribute ("title") ?? el.Attribute ("_title"));
			if (string.IsNullOrEmpty (title))
				throw new InvalidOperationException ("Link is missing title");
			this.text = GettextCatalog.GetString (title);
			
			subtitle = (string)el.Attribute ("pubDate");
			SetDate (subtitle);

			string href = (string)el.Attribute ("href");
			if (string.IsNullOrEmpty (href))
				throw new InvalidOperationException ("Link is missing href");
			this.LinkUrl = href;
			
			string desc = (string)(el.Attribute ("desc") ?? el.Attribute ("_desc"));
			desc = CleanHtml (desc);

			if (!string.IsNullOrEmpty (desc)) {
				desc = desc.Replace (" [...]", "...");
				desc = GettextCatalog.GetString (desc);

				if (desc.Length > MaxCharacters) {
					int truncateIndex = desc.IndexOf (" ", MaxCharacters);
					if (truncateIndex > 0)
						desc = desc.Substring (0, truncateIndex) + "...";
				}

				this.desc = desc;
			}


			
			string tooltip = (string) (el.Attribute ("tooltip") ?? el.Attribute ("_tooltip"));
			if (!string.IsNullOrEmpty (tooltip))
				this.TooltipText = GettextCatalog.GetString (tooltip);
			else
				this.TooltipText = GetLinkTooltip (href);
			
			string icon = (string) el.Attribute ("icon");
			if (!string.IsNullOrEmpty (icon))
				this.icon = icon;
			
			UpdateLabel (false);
			UpdateImage ();
		}
		
		public WelcomePageFeedItem (string label, string link)
			: this ()
		{
			this.text = label;
			this.LinkUrl = link;
			UpdateLabel (false);
		}

		public WelcomePageFeedItem (string label, string link, string description, string date)
			: this ()
		{
			this.text = label;
			this.LinkUrl = link;

			if (description.Length > MaxCharacters) {
				int truncateIndex = description.IndexOf (" ", MaxCharacters);
				if (truncateIndex > 0)
					description = description.Substring (0, truncateIndex) + "...";
			}
			this.desc = description;
			SetDate (date);
			UpdateLabel (false);
		}
		
		public WelcomePageFeedItem ()
		{
			VisibleWindow = false;

			box = new VBox ();

			titleLabel = new Label () { Xalign = 0 };
			titleLabel.Wrap = false;
			titleLabel.Ellipsize = Pango.EllipsizeMode.End;
			titleLabel.LineWrapMode = Pango.WrapMode.Word;
			box.PackStart (titleLabel, false, false, 0);

			subtitleLabel = new Label () { Xalign = 0 };
			var align = new Gtk.Alignment (0, 0, 1f, 1f) { 
				TopPadding = Styles.WelcomeScreen.Pad.MediumTitleMarginBottom,
				BottomPadding = Styles.WelcomeScreen.Pad.SummaryParagraphMarginTop
			};
			align.Add (subtitleLabel);
			box.PackStart (align, false, false, 0);

			summaryLabel = new Label () { Xalign = 0 };
			summaryLabel.Wrap = true;
			box.PackStart (summaryLabel, true, true, 0);

			Pango.AttrRise rise = new Pango.AttrRise (Pango.Units.FromPixels (7));
			summaryLabel.Attributes = new Pango.AttrList ();
			summaryLabel.Attributes.Insert (rise);

			Add (box);

			Gui.Styles.Changed += UpdateStyle;
		}

		void UpdateStyle (object sender, EventArgs args)
		{
			UpdateLabel (false);
		}

		int allocWidth;

		void SetDate (string dateString)
		{
			if (string.IsNullOrEmpty (dateString)) {
				subtitle = "Today";
			} else {
				DateTime date;
				if (DateTime.TryParse (dateString, out date)) {

					// Round to begining of day. A change of day will be "yesterday", even if it happened 5 minutes ago
					var today = DateTime.Today;
					date = date.Date;

					int days = (int)Math.Round ((today - date).TotalDays);
					var weeks = days / 7;

					if (days <= 0) {
						subtitle = GettextCatalog.GetString ("Today");
					}
					else if (days == 1) {
						subtitle = GettextCatalog.GetString ("Yesterday");
					}
					else if (days < 7) {
						subtitle = GettextCatalog.GetPluralString ("{0} day ago", "{0} days ago", days, days);
					}
					else if (weeks < 4) {
						subtitle = GettextCatalog.GetPluralString ("{0} week ago", "{0} weeks ago", weeks, weeks);
					}
					else
						subtitle = date.ToShortDateString ();
				} else {
					subtitle = "Today";
				}
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (allocWidth != allocation.Width) {
				allocWidth = allocation.Width;
				titleLabel.WidthRequest = allocWidth;
				summaryLabel.WidthRequest = allocWidth;
			}
			base.OnSizeAllocated (allocation);
		}
		
		public string LinkUrl { get; private set; }
		
		public new string Title {
			get {
				return text;
			}
			set {
				text = value;
				UpdateLabel (false);
			}
		}
		
		public new string Subtitle {
			get {
				return subtitle;
			}
			set {
				subtitle = value;
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
			titleLabel.Markup = string.Format (underlined? linkUnderlinedFormat : linkFormat, GLib.Markup.EscapeText (text));
			subtitleLabel.Markup = string.Format (subtitleFormat, GLib.Markup.EscapeText (subtitle ?? ""));
			summaryLabel.Markup = string.Format (descFormat, SummaryHtmlToPango(desc ?? ""));
		}

		public static string SummaryHtmlToPango(string summaryHtml)
		{
			var result = new StringBuilder ();
			bool inTag =  false;
			for (int i = 0; i < summaryHtml.Length; i++) {
				char ch = summaryHtml [i];
				if (inTag) {
					if (ch == '>')
						inTag = false;
					continue;
				}
				switch (ch) {
				case '\n':
					result.Append (" ");
					break;
				case '<':
					inTag = true;
					break;
				case '\'':
					result.Append ("&apos;");
					break;
				case '"':
					result.Append ("&quot;");
					break;
				default:
					result.Append (ch);
					break;
				}
			}
			return result.ToString ();
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
				image = new ImageView ();
				box.PackStart (image, false, false, 0);
				box.ReorderChild (image, 0);
			}
			image.Image = ImageService.GetIcon (icon, iconSize);
		}

		string CleanHtml (string txt)
		{
			return Regex.Replace (txt, "<.*?>", "");
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = hand_cursor;
			UpdateLabel (true);
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = null;
			UpdateLabel (false);
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1 && new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height).Contains ((int)evnt.X, (int)evnt.Y)) {
				WelcomePageSection.DispatchLink (LinkUrl);
				return true;
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		string GetLinkTooltip (string link)
		{
			if (link == null)
				return "";
			if (link.IndexOf ("monodevelop://") != -1)
				return "";
				
			if (link.IndexOf ("project://") != -1) {
				string message = link;
				message = message.Substring (10);
				string msg = GettextCatalog.GetString ("Open solution {0}", message);
				if (IdeApp.Workspace.IsOpen)
					msg += " - " + GettextCatalog.GetString ("Hold Control key to open in current workspace.");
				return msg;
			} else {
				return GettextCatalog.GetString ("Open {0}", link);
			}
		}

		protected override void OnDestroyed ()
		{
			Gui.Styles.Changed -= UpdateStyle;
			base.OnDestroyed ();
		}
	}
}