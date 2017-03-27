//
// WelcomePageBarButton.cs
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
using MonoDevelop.Core;
using System.Xml.Linq;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageBarButton: EventBox
	{
		Xwt.ImageView image;
		Widget imageWidget;
		Gtk.Label label;

		Xwt.Drawing.Image imageNormal, imageHover;
		bool mouseOver;
		string actionLink;
		private static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);

		public string FontFamily { get; set; }
		public string HoverColor { get; set; }
		public string Color { get; set; }
		public int FontSize { get; set; }
		protected string Text { get; set; }
		protected Pango.Weight FontWeight { get; set; }
		protected bool Bold { 
			get { return FontWeight == Pango.Weight.Bold; }
			set { FontWeight = value ? Pango.Weight.Bold : Pango.Weight.Normal; }
		}
		HBox box = new HBox ();

		public int IconTextSpacing {
			get { return box.Spacing; }
			set { box.Spacing = value; }
		}

		public bool MouseOver {
			get {
				return mouseOver;
			}
		}

		/// <summary>
		/// If false the window button isn't inserted into the page bar.
		/// </summary>
		public virtual bool IsVisible { 
			get { return true; }
		}

		public WelcomePageBarButton (string title, string href, string iconResource = null)
		{
			var actionHandler = new ActionDelegate (this);
			actionHandler.PerformPress += HandlePress;

			Accessible.Role = Atk.Role.Link;

			Accessible.SetTitle (title);
			if (!string.IsNullOrEmpty (href)) {
				Accessible.SetUrl (href);
			}
			Accessible.Description = "Opens the link in a web browser";

			UpdateStyle ();

			VisibleWindow = false;
			this.Text = GettextCatalog.GetString (title);
			this.actionLink = href;
			if (!string.IsNullOrEmpty (iconResource)) {
				imageHover = Xwt.Drawing.Image.FromResource (iconResource);
				imageNormal = imageHover.WithAlpha (0.7);
			}

			box.Accessible.SetShouldIgnore (true);

			IconTextSpacing = Styles.WelcomeScreen.Links.IconTextSpacing;
			image = new Xwt.ImageView ();
			label = CreateLabel ();
			imageWidget = image.ToGtkWidget ();

			label.Accessible.SetShouldIgnore (true);
			imageWidget.Accessible.SetShouldIgnore (true);

			box.PackStart (imageWidget, false, false, 0);
			if (imageNormal == null)
				imageWidget.NoShowAll = true;
			box.PackStart (label, false, false, 0);
			box.ShowAll ();
			Add (box);

			Gui.Styles.Changed += UpdateStyle;
			Update ();

			Events |= (Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonReleaseMask);
		}

		void UpdateStyle (object sender = null, EventArgs e = null)
		{
			OnUpdateStyle ();
			if (label != null) {
				box.Remove (label);
				box.PackStart (label = CreateLabel ());
				box.ShowAll ();
				Update ();
			}
			QueueResize ();
		}

		protected virtual void OnUpdateStyle ()
		{
			FontFamily = Platform.IsMac ? Styles.WelcomeScreen.FontFamilyMac : Styles.WelcomeScreen.FontFamilyWindows;
			HoverColor = Styles.WelcomeScreen.Links.HoverColor;
			Color = Styles.WelcomeScreen.Links.Color;
			FontSize = Styles.WelcomeScreen.Links.FontSize;
			FontWeight = Pango.Weight.Bold;
		}

		protected virtual Label CreateLabel ()
		{
			return new Label ();
		}

		protected void SetImage (Xwt.Drawing.Image normal, Xwt.Drawing.Image hover)
		{
			imageHover = hover;
			imageNormal = normal;

			if (imageNormal == null) {
				imageWidget.NoShowAll = true;
				image.Hide ();
			} else {
				imageWidget.NoShowAll = false;
				ShowAll ();
			}

			Update ();
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = hand_cursor;
			mouseOver = true;
			Update ();
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = null;
			mouseOver = false;
			Update ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1 && new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height).Contains ((int)evnt.X, (int)evnt.Y)) {
				OnClicked ();
				return true;
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		void HandlePress (object sender, EventArgs args)
		{
			OnClicked ();
		}

		protected virtual void OnClicked ()
		{
			WelcomePageSection.DispatchLink (actionLink);
		}

		protected void Update ()
		{
			if (imageNormal != null)
				image.Image = mouseOver ? imageHover : imageNormal;
			var color = mouseOver ? HoverColor : Color;
			label.Markup = WelcomePageSection.FormatText (FontFamily, FontSize, FontWeight, color, Text);
		}

		protected override void OnDestroyed ()
		{
			Gui.Styles.Changed -= UpdateStyle;
			base.OnDestroyed ();
		}
	}
}

