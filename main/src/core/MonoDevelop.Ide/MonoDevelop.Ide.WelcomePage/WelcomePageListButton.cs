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
using Mono.TextEditor;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageListButton: EventBox
	{
		static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
		string title, subtitle, actionUrl;
		protected Xwt.Drawing.Image icon;
		protected bool mouseOver;
		protected bool pinned;
		protected Gdk.Rectangle starRect;
		protected bool mouseOverStar;

		protected Pango.Weight TitleFontWeight { get; set; }

		static protected readonly Xwt.Drawing.Image starNormal;
		static protected readonly Xwt.Drawing.Image starNormalHover;
		static protected readonly Xwt.Drawing.Image starPinned;
		static protected readonly Xwt.Drawing.Image starPinnedHover;

		public event EventHandler PinClicked;

		public bool DrawLeftBorder { get; set; }
		public bool DrawRightBorder { get; set; }

		public int BorderPadding { get; set; }

		public int LeftTextPadding { get; set; }
		public int InternalPadding { get; set; }


		string smallTitleColor = Styles.WelcomeScreen.Pad.SmallTitleColor;
		public string SmallTitleColor {
			get {
				return smallTitleColor;
			}
			set {
				smallTitleColor = value;
			}
		}

		string mediumTitleColor = Styles.WelcomeScreen.Pad.MediumTitleColor;
		public string MediumTitleColor {
			get {
				return mediumTitleColor;
			}
			set {
				mediumTitleColor = value;
			}
		}

		string titleFontFace = Platform.IsMac ? Styles.WelcomeScreen.Pad.TitleFontFamilyMac : Styles.WelcomeScreen.Pad.TitleFontFamilyWindows;
		public string TitleFontFace {
			get {
				return titleFontFace;
			}
			set {
				titleFontFace = value;
			}
		}

		string smallTitleFontFace = Platform.IsMac ? Styles.WelcomeScreen.Pad.TitleFontFamilyMac : Styles.WelcomeScreen.Pad.TitleFontFamilyWindows;
		public string SmallTitleFontFace {
			get {
				return smallTitleFontFace;
			}
			set {
				smallTitleFontFace = value;
			}
		}

		string hoverBackgroundColor = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.HoverBackgroundColor;
		public string HoverBackgroundColor {
			get {
				return hoverBackgroundColor;
			}
			set {
				hoverBackgroundColor = value;
			}
		}

		string hoverBorderColor = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.HoverBorderColor;
		public string HoverBorderColor {
			get {
				return hoverBorderColor;
			}
			set {
				hoverBorderColor = value;
			}
		}

		int titleFontSize = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TitleFontSize;
		public int TitleFontSize {
			get {
				return titleFontSize;
			}
			set {
				titleFontSize = value;
			}
		}

		int smallTitleFontSize = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.PathFontSize;
		public int SmallTitleFontSize {
			get {
				return smallTitleFontSize;
			}
			set {
				smallTitleFontSize = value;
			}
		}

		static WelcomePageListButton ()
		{
			starNormal = Xwt.Drawing.Image.FromResource ("unstar-overlay-16.png");
			starNormalHover = Xwt.Drawing.Image.FromResource ("unstar-overlay-hover-16.png");
			starPinned = Xwt.Drawing.Image.FromResource ("star-overlay-16.png");
			starPinnedHover = Xwt.Drawing.Image.FromResource ("star-overlay-hover-16.png");
		}

		public WelcomePageListButton (string title, string subtitle, Xwt.Drawing.Image icon, string actionUrl)
		{
			VisibleWindow = false;
			this.title = title;
			this.subtitle = subtitle;
			this.icon = icon;
			this.actionUrl = actionUrl;
			this.SmallTitleColor = smallTitleColor;
			this.MediumTitleColor = mediumTitleColor;
			WidthRequest = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.Width;
			HeightRequest = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.Height + 2;
			Events |= (Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask);

			LeftTextPadding = Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TextLeftPadding;
			InternalPadding = Styles.WelcomeScreen.Pad.Padding;
		}

		public bool AllowPinning { get; set; }

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
				} else if (mouseOver) {
					WelcomePageSection.DispatchLink (actionUrl);
					return true;
				}
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			var so = starRect.Contains (Allocation.X + (int)evnt.X, Allocation.Y + (int)evnt.Y);
			if (so != mouseOverStar) {
				mouseOverStar = so;
				QueueDraw ();
			}
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

				Pango.Layout titleLayout = new Pango.Layout (PangoContext);
				titleLayout.Width = Pango.Units.FromPixels (textWidth);
				titleLayout.Ellipsize = Pango.EllipsizeMode.End;
				titleLayout.SetMarkup (WelcomePageSection.FormatText (TitleFontFace, titleFontSize, TitleFontWeight, MediumTitleColor, title));

				Pango.Layout subtitleLayout = null;

				if (!string.IsNullOrEmpty (subtitle)) {
					subtitleLayout = new Pango.Layout (PangoContext);
					subtitleLayout.Width = Pango.Units.FromPixels (textWidth);
					subtitleLayout.Ellipsize = Pango.EllipsizeMode.Start;
					subtitleLayout.SetMarkup (WelcomePageSection.FormatText (SmallTitleFontFace, smallTitleFontSize, Pango.Weight.Normal, SmallTitleColor, subtitle));
				}

				int height = 0;
				int w, h1, h2;
				titleLayout.GetPixelSize (out w, out h1);
				height += h1;

				if (subtitleLayout != null) {
					height += Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TitleBottomMargin;
					subtitleLayout.GetPixelSize (out w, out h2);
					height += h2;
				}

				int tx = Allocation.X + InternalPadding + LeftTextPadding;
				int ty = Allocation.Y + (Allocation.Height - height) / 2;
				DrawLayout (ctx, titleLayout, TitleFontFace, titleFontSize, TitleFontWeight, MediumTitleColor, tx, ty);

				if (subtitleLayout != null) {
					ty += h1 + Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TitleBottomMargin;
					DrawLayout (ctx, subtitleLayout, SmallTitleFontFace, smallTitleFontSize, Pango.Weight.Normal, SmallTitleColor, tx, ty);
				}
			}
			return true;
		}
	}
}

