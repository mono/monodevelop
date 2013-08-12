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
		private static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
		string title, subtitle, actionUrl;
		Gdk.Pixbuf icon;
		bool mouseOver;
		bool pinned;
		Gdk.Rectangle starRect;
		bool mouseOverStar;

		static Gdk.Pixbuf starNormal;
		static Gdk.Pixbuf starNormalHover;
		static Gdk.Pixbuf starPinned;
		static Gdk.Pixbuf starPinnedHover;

		public event EventHandler PinClicked;

		public bool DrawLeftBorder { get; set; }
		public bool DrawRightBorder { get; set; }

		public int BorderPadding { get; set; }

		public int LeftTextPadding { get; set; }
		public int InternalPadding { get; set; }

		static WelcomePageListButton ()
		{
			starNormal = Gdk.Pixbuf.LoadFromResource ("star-normal.png");
			starNormalHover = Gdk.Pixbuf.LoadFromResource ("star-normal-hover.png");
			starPinned = Gdk.Pixbuf.LoadFromResource ("star-pinned.png");
			starPinnedHover = Gdk.Pixbuf.LoadFromResource ("star-pinned-hover.png");
		}

		public WelcomePageListButton (string title, string subtitle, Gdk.Pixbuf icon, string actionUrl)
		{
			VisibleWindow = false;
			this.title = title;
			this.subtitle = subtitle;
			this.icon = icon;
			this.actionUrl = actionUrl;
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

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (evnt.Window)) {
				if (mouseOver) {
					if (BorderPadding <= 0) {
						ctx.Rectangle (Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
						ctx.Color = CairoExtensions.ParseColor (Styles.WelcomeScreen.Pad.Solutions.SolutionTile.HoverBackgroundColor);
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
						ctx.Color = CairoExtensions.ParseColor (Styles.WelcomeScreen.Pad.Solutions.SolutionTile.HoverBorderColor);
						ctx.Stroke ();
					} else {
						Gdk.Rectangle region = Allocation;
						region.Inflate (-BorderPadding, -BorderPadding);

						ctx.RoundedRectangle (region.X + 0.5, region.Y + 0.5, region.Width - 1, region.Height - 1, 3);
						ctx.Color = CairoExtensions.ParseColor (Styles.WelcomeScreen.Pad.Solutions.SolutionTile.HoverBackgroundColor);
						ctx.FillPreserve ();
						
						ctx.LineWidth = 1;
						ctx.Color = CairoExtensions.ParseColor (Styles.WelcomeScreen.Pad.Solutions.SolutionTile.HoverBorderColor);
						ctx.Stroke ();
					}
				}

				// Draw the icon

				int x = Allocation.X + InternalPadding;
				int y = Allocation.Y + (Allocation.Height - icon.Height) / 2;
				Gdk.CairoHelper.SetSourcePixbuf (ctx, icon, x, y);
				ctx.Paint ();

				if (AllowPinning && (mouseOver || pinned)) {
					Gdk.Pixbuf star;
					if (pinned) {
						if (mouseOverStar)
							star = starPinnedHover;
						else
							star = starPinned;
					} else {
						if (mouseOverStar)
							star = starNormalHover;
						else
							star = starNormal;
					}
					x = x + icon.Width - star.Width + 3;
					y = y + icon.Height - star.Height + 3;
					Gdk.CairoHelper.SetSourcePixbuf (ctx, star, x, y);
					ctx.Paint ();
					starRect = new Gdk.Rectangle (x, y, star.Width, star.Height);
				}

				// Draw the text

				int textWidth = Allocation.Width - LeftTextPadding - InternalPadding * 2;

				var face = Platform.IsMac ? Styles.WelcomeScreen.Pad.TitleFontFamilyMac : Styles.WelcomeScreen.Pad.TitleFontFamilyWindows;
				Pango.Layout titleLayout = new Pango.Layout (PangoContext);
				titleLayout.Width = Pango.Units.FromPixels (textWidth);
				titleLayout.Ellipsize = Pango.EllipsizeMode.End;
				titleLayout.SetMarkup (WelcomePageSection.FormatText (face, Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TitleFontSize, false, Styles.WelcomeScreen.Pad.MediumTitleColor, title));

				Pango.Layout subtitleLayout = null;

				if (!string.IsNullOrEmpty (subtitle)) {
					subtitleLayout = new Pango.Layout (PangoContext);
					subtitleLayout.Width = Pango.Units.FromPixels (textWidth);
					subtitleLayout.Ellipsize = Pango.EllipsizeMode.Start;
					subtitleLayout.SetMarkup (WelcomePageSection.FormatText (face, Styles.WelcomeScreen.Pad.Solutions.SolutionTile.PathFontSize, false, Styles.WelcomeScreen.Pad.SmallTitleColor, subtitle));
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
				ctx.MoveTo (tx, ty);
				Pango.CairoHelper.ShowLayout (ctx, titleLayout);

				if (subtitleLayout != null) {
					ty += h1 + Styles.WelcomeScreen.Pad.Solutions.SolutionTile.TitleBottomMargin;
					ctx.MoveTo (tx, ty);
					Pango.CairoHelper.ShowLayout (ctx, subtitleLayout);
				}
			}
			return true;
		}
	}
}

