//
// WelcomePageListButton.cs
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
using MonoDevelop.Core;
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageListButton: EventBox
	{
		static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
		string title, subtitle, actionUrl, fileName;
		protected Xwt.Drawing.Image icon;
		protected bool mouseOver;
		protected bool pinned;
		protected readonly bool hasRemoveButton;
		protected bool itemAccessible;
		protected Gdk.Rectangle starRect;
		protected Gdk.Rectangle removeRect;
		protected bool mouseOverStar;
		protected bool mouseOverRemove;

		protected Pango.Weight TitleFontWeight { get; set; }

		static protected readonly Xwt.Drawing.Image starNormal;
		static protected readonly Xwt.Drawing.Image starNormalHover;
		static protected readonly Xwt.Drawing.Image starPinned;
		static protected readonly Xwt.Drawing.Image starPinnedHover;
		static protected readonly Xwt.Drawing.Image removeImage;

		public event EventHandler PinClicked;

		public bool DrawLeftBorder { get; set; }
		public bool DrawRightBorder { get; set; }

		public int BorderPadding { get; set; }

		public int LeftTextPadding { get; set; }
		public int InternalPadding { get; set; }

		public string SmallTitleColor { get; set; }
		public string MediumTitleColor { get; set; }

		public string SmallTitleDisabledColor { get; set; }
		public string MediumTitleDisabledColor { get; set; }

		public string TitleFontFace { get; set; }
		public string SmallTitleFontFace { get; set; }

		public string HoverBackgroundColor { get; set; }
		public string HoverBorderColor { get; set; }

		public int TitleFontSize { get; set; }
		public int SmallTitleFontSize { get; set; }

		static WelcomePageListButton ()
		{
			starNormal = Xwt.Drawing.Image.FromResource ("unstar-16.png");
			starNormalHover = Xwt.Drawing.Image.FromResource ("unstar-hover-16.png");
			starPinned = Xwt.Drawing.Image.FromResource ("star-16.png");
			starPinnedHover = Xwt.Drawing.Image.FromResource ("star-hover-16.png");
			removeImage = Xwt.Drawing.Image.FromResource ("remove-16.png");
		}

		public WelcomePageListButton (string title, string subtitle, Xwt.Drawing.Image icon, string actionUrl) : this(title, subtitle, icon, actionUrl, null)
		{
		}

		public WelcomePageListButton (string title, string subtitle, Xwt.Drawing.Image icon, string actionUrl, string fileName)
		{
			VisibleWindow = false;
			this.title = title;
			this.subtitle = subtitle;
			this.icon = icon;
			this.actionUrl = actionUrl;
			this.fileName = fileName;
			hasRemoveButton = fileName != null;

			WidthRequest = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.Width;
			HeightRequest = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.Height + 2;
			Events |= (Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask);

			LeftTextPadding = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TextLeftPadding;
			InternalPadding = Styles.WelcomeScreen.Pad.Padding;

			Gui.Styles.Changed += UpdateStyle;
			UpdateStyle ();

			IdeApp.FocusIn += UpdateFileStatus;
			UpdateFileStatus (this, EventArgs.Empty);
		}

		void UpdateFileStatus (object sender, EventArgs args)
		{
			ItemAccessible = fileName == null || System.IO.File.Exists (fileName);
		}

		void UpdateStyle (object sender = null, EventArgs e = null)
		{
			OnUpdateStyle ();
			QueueDraw ();
		}

		protected virtual void OnUpdateStyle ()
		{
			SmallTitleColor = Styles.WelcomeScreen.Pad.SmallTitleColor;
			MediumTitleColor = Styles.WelcomeScreen.Pad.MediumTitleColor;

			SmallTitleDisabledColor = Styles.WelcomeScreen.Pad.SmallTitleDisabledColor;
			MediumTitleDisabledColor = Styles.WelcomeScreen.Pad.MediumTitleDisabledColor;

			TitleFontFace = Platform.IsMac ? Styles.WelcomeScreen.Pad.TitleFontFamilyMac : Styles.WelcomeScreen.Pad.TitleFontFamilyWindows;
			SmallTitleFontFace = Platform.IsMac ? Styles.WelcomeScreen.Pad.TitleFontFamilyMac : Styles.WelcomeScreen.Pad.TitleFontFamilyWindows;

			HoverBackgroundColor = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.HoverBackgroundColor;
			HoverBorderColor = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.HoverBorderColor;

			TitleFontSize = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TitleFontSize;
			SmallTitleFontSize = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.PathFontSize;
		}

		public bool AllowPinning { get; set; }

		public bool ItemAccessible {
			get { return itemAccessible; }
			set {
				itemAccessible = value;
				Sensitive = ItemAccessible;
				QueueDraw ();
			}
		}

		public bool Pinned {
			get { return pinned; }
			set {
				pinned = value;
				QueueDraw ();
			}
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = hand_cursor;
			mouseOver = true;
			QueueDraw ();
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = null;
			mouseOver = false;
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				if (mouseOverStar && AllowPinning) {
					pinned = !pinned;
					QueueDraw ();
					if (PinClicked != null)
						PinClicked (this, EventArgs.Empty);
					return true;
				} else if (mouseOverRemove) {
					WelcomePageSection.RemoveItem (fileName, withDialog: false);
					return true;
				} else if (mouseOver) {
					WelcomePageSection.DispatchLink (actionUrl);
					return true;
				}
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			int x = Allocation.X + (int)evnt.X;
			int y = Allocation.Y + (int)evnt.Y;
			bool updated = false;

			var so = starRect.Contains (x, y);
			if (so != mouseOverStar) {
				mouseOverStar = so;
				updated = true;
			}

			so = removeRect.Contains (x, y);
			if (so != mouseOverRemove) {
				mouseOverRemove = so;
				updated = true;
			}

			if (updated)
				QueueDraw ();

			return base.OnMotionNotifyEvent (evnt);
		}

		protected virtual void DrawLayout (Cairo.Context ctx, Pango.Layout layout, string fontFace, int fontSize, Pango.Weight weight, string color, int tx, int ty)
		{
			ctx.MoveTo (tx, ty);
			Pango.CairoHelper.ShowLayout (ctx, layout);
		}

		protected virtual void DrawHoverBackground (Cairo.Context ctx)
		{
			if (BorderPadding <= 0) {
				ctx.Rectangle (Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
				ctx.SetSourceColor (CairoExtensions.ParseColor (HoverBackgroundColor));
				ctx.Fill ();
				ctx.MoveTo (Allocation.X, Allocation.Y + 0.5);
				ctx.RelLineTo (Allocation.Width, 0);
				ctx.MoveTo (Allocation.X, Allocation.Y + Allocation.Height - 0.5);
				ctx.RelLineTo (Allocation.Width, 0);
				if (DrawRightBorder) {
					ctx.MoveTo (Allocation.Right + 0.5, Allocation.Y + 0.5);
					ctx.LineTo (Allocation.Right + 0.5, Allocation.Bottom - 0.5);
				}
				if (DrawLeftBorder) {
					ctx.MoveTo (Allocation.Left + 0.5, Allocation.Y + 0.5);
					ctx.LineTo (Allocation.Left + 0.5, Allocation.Bottom - 0.5);
				}
				ctx.LineWidth = 1;
				ctx.SetSourceColor (CairoExtensions.ParseColor (HoverBorderColor));
				ctx.Stroke ();
			}
			else {
				Gdk.Rectangle region = Allocation;
				region.Inflate (-BorderPadding, -BorderPadding);
				ctx.RoundedRectangle (region.X + 0.5, region.Y + 0.5, region.Width - 1, region.Height - 1, 3);
				ctx.SetSourceColor (CairoExtensions.ParseColor (HoverBackgroundColor));
				ctx.FillPreserve ();
				ctx.LineWidth = 1;
				ctx.SetSourceColor (CairoExtensions.ParseColor (HoverBorderColor));
				ctx.Stroke ();
			}
		}

		protected virtual void DrawIcon (Cairo.Context ctx)
		{
			int x = Allocation.X + InternalPadding;
			int y = Allocation.Y + (Allocation.Height - (int)icon.Height) / 2;
			ctx.DrawImage (this, icon, x, y);
			if (AllowPinning && (mouseOver || pinned)) {
				Xwt.Drawing.Image star;
				if (pinned) {
					if (mouseOverStar)
						star = starPinnedHover;
					else
						star = starPinned;
				}
				else {
					if (mouseOverStar)
						star = starNormalHover;
					else
						star = starNormal;
				}
				x = x + (int)icon.Width - (int)star.Width + 3;
				y = y + (int)icon.Height - (int)star.Height + 3;
				ctx.DrawImage (this, star, x, y);
				starRect = new Gdk.Rectangle (x, y, (int)star.Width, (int)star.Height);
			}

			if (hasRemoveButton && (mouseOver || !ItemAccessible)) {
				x = Allocation.Right - InternalPadding;
				y = Allocation.Y + Allocation.Height / 2 - (int)removeImage.Height;
				ctx.DrawImage (this, removeImage, x, y);
				removeRect = new Gdk.Rectangle (x, y, (int)removeImage.Width, (int)removeImage.Height);
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (evnt.Window)) {
				if (mouseOver)
					DrawHoverBackground (ctx);

				// Draw the icon
				DrawIcon (ctx);

				// Draw the text

				int textWidth = Allocation.Width - LeftTextPadding - InternalPadding * 2;

				using (var titleLayout = new Pango.Layout (PangoContext))
				{
					titleLayout.Width = Pango.Units.FromPixels (textWidth);
					titleLayout.Ellipsize = Pango.EllipsizeMode.End;
					titleLayout.SetMarkup (WelcomePageSection.FormatText (TitleFontFace, TitleFontSize, Pango.Weight.Bold, ItemAccessible ? MediumTitleColor : MediumTitleDisabledColor, title));

					Pango.Layout subtitleLayout = null;

					if (!string.IsNullOrEmpty (subtitle))
					{
						subtitleLayout = new Pango.Layout (PangoContext);
						subtitleLayout.Width = Pango.Units.FromPixels (textWidth);
						subtitleLayout.Ellipsize = Pango.EllipsizeMode.Start;
						subtitleLayout.SetMarkup (WelcomePageSection.FormatText (SmallTitleFontFace, SmallTitleFontSize, Pango.Weight.Normal, ItemAccessible ? SmallTitleColor : SmallTitleDisabledColor, subtitle));
					}

					int height = 0;
					int w, h1, h2;
					titleLayout.GetPixelSize (out w, out h1);
					height += h1;

					if (subtitleLayout != null)
					{
						height += Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TitleBottomMargin;
						subtitleLayout.GetPixelSize (out w, out h2);
						height += h2;
					}

					int tx = Allocation.X + InternalPadding + LeftTextPadding;
					int ty = Allocation.Y + (Allocation.Height - height) / 2;
					DrawLayout (ctx, titleLayout, TitleFontFace, TitleFontSize, Pango.Weight.Bold, ItemAccessible ? MediumTitleColor : MediumTitleDisabledColor, tx, ty);

					if (subtitleLayout != null)
					{
						ty += h1 + Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TitleBottomMargin;
						DrawLayout (ctx, subtitleLayout, SmallTitleFontFace, SmallTitleFontSize, Pango.Weight.Normal, ItemAccessible ? SmallTitleColor : SmallTitleDisabledColor, tx, ty);
						subtitleLayout.Dispose ();
					}
				}
			}
			return true;
		}

		protected override void OnDestroyed ()
		{
			Gui.Styles.Changed -= UpdateStyle;
			IdeApp.FocusIn -= UpdateFileStatus;
			base.OnDestroyed ();
		}
	}
}

