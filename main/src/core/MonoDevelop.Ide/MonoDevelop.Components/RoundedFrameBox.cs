//
// RoundedFrameBox.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.Components
{
	public class RoundedFrameBox: Widget
	{
		double borderWidth = 1;
		bool borderLeft = true, borderRight = true, borderTop = true, borderBottom = true;
		WidgetSpacing padding;
		BorderCornerRadius cornerRadius;
		FrameCanvas canvas;
		Color borderColor = Styles.ThinSplitterColor;
		Color innerColor = Colors.Transparent;

		class FrameCanvas: Canvas
		{
			Widget child;

			public void Resize ()
			{
				OnPreferredSizeChanged ();
				QueueDraw ();
			}

			public Widget Child {
				get { return child; }
				set {
					if (child != null)
						RemoveChild (child);
					child = value;
					if (child != null)
						AddChild (child);
					Resize ();
					QueueForReallocate ();
				}
			}

			protected override Size OnGetPreferredSize (SizeConstraint widthConstraint, SizeConstraint heightConstraint)
			{
				var parent = (RoundedFrameBox)Parent;
				var s = new Size (Math.Max (parent.Padding.HorizontalSpacing, Math.Max (parent.BorderSpacing.HorizontalSpacing, parent.CornerRadius.Spacing.HorizontalSpacing)),
				                  Math.Max (parent.Padding.VerticalSpacing, Math.Max (parent.BorderSpacing.VerticalSpacing, parent.CornerRadius.Spacing.VerticalSpacing)));
				if (child != null)
					s += child.Surface.GetPreferredSize (widthConstraint - s.Width, heightConstraint - s.Height, true);
				return s;
			}

			protected override void OnReallocate ()
			{
				if (child != null) {
					var parent = (RoundedFrameBox)Parent;
					var rect = Bounds;
					var border = parent.BorderSpacing;
					rect.X += Math.Max (parent.Padding.Left, Math.Max (border.Left, parent.CornerRadius.Spacing.Left));
					rect.Y += Math.Max (parent.Padding.Top, Math.Max (border.Top, parent.CornerRadius.Spacing.Top));
					rect.Width -= Math.Max (parent.Padding.HorizontalSpacing, Math.Max (border.HorizontalSpacing, parent.CornerRadius.Spacing.HorizontalSpacing));
					rect.Height -= Math.Max (parent.Padding.VerticalSpacing, Math.Max (border.VerticalSpacing, parent.CornerRadius.Spacing.VerticalSpacing));
					rect = child.Surface.GetPlacementInRect (rect);
					SetChildBounds (child, rect);
				}
			}

			protected override void OnDraw (Context ctx, Rectangle dirtyRect)
			{
				base.OnDraw (ctx, dirtyRect);

				if (((RoundedFrameBox)Parent).innerColor != Colors.Transparent)
					Draw (ctx, true);
				Draw (ctx, false);
			}

			void PathTo (Context ctx, double x, double y, bool stroke)
			{
				if (stroke)
					ctx.LineTo (x, y);
				else
					ctx.MoveTo (x, y);
			}

			void Draw (Context ctx, bool fill)
			{
				var parent = (RoundedFrameBox)Parent;
				var border = parent.BorderSpacing;
				var radius = parent.cornerRadius;
				var rect = Bounds;

				ctx.MoveTo (rect.X, rect.Y);



				if (border.Top > 0) {
					ctx.NewPath ();
					if (radius.TopLeft > 0)
						ctx.Arc (rect.Left + radius.TopLeft + border.Left / 2, rect.Top + radius.TopLeft + border.Top / 2, radius.TopLeft, 180, 270);
					else
						PathTo (ctx, rect.Left, rect.Top + border.Top / 2, fill);

					if (radius.TopRight > 0) {
						ctx.LineTo (rect.Right - (radius.TopRight + border.Right / 2), rect.Top + border.Top / 2);
						ctx.Arc (rect.Right - (radius.TopRight + border.Right / 2), rect.Top + radius.TopRight + border.Top / 2, radius.TopRight, 270, 0);
					} else
						ctx.LineTo (rect.Right, rect.Top + border.Top / 2);
				} else
					PathTo (ctx, rect.Right - border.Right / 2, rect.Top, fill);

				if (border.Right > 0)
					// TODO: round corners if top/bottom border disabled
					ctx.LineTo (rect.Right - border.Right / 2, rect.Bottom - (border.Bottom > 0 ? radius.BottomRight : 0));
				else
					PathTo (ctx, rect.Right - border.Right / 2, rect.Bottom - (border.Bottom > 0 ? radius.BottomRight : 0), fill);

				if (border.Bottom > 0) {
					if (radius.BottomRight > 0)
						ctx.Arc (rect.Right - (radius.BottomRight + border.Right / 2), rect.Bottom - (radius.BottomRight + border.Bottom / 2), radius.BottomRight, 0, 90);
					else
						PathTo (ctx, rect.Right, rect.Bottom - border.Bottom / 2, fill);

					if (radius.BottomLeft > 0) {
						ctx.LineTo (rect.Left + (radius.BottomLeft + border.Left / 2), rect.Bottom - border.Bottom / 2);
						ctx.Arc (rect.Left + radius.BottomLeft + border.Left / 2, rect.Bottom - (radius.BottomLeft + border.Bottom / 2), radius.BottomLeft, 90, 180);
					} else
						ctx.LineTo (rect.Left + border.Left / 2, rect.Bottom - border.Bottom / 2);
				} else
					PathTo (ctx, rect.Left + border.Left / 2, rect.Bottom - border.Bottom / 2, fill);

				if (border.Left > 0)
					// TODO: round corners if top/bottom border disabled
					ctx.LineTo (rect.Left + border.Left / 2, rect.Top + (border.Top > 0 ? radius.TopLeft : 0));
				else
					PathTo (ctx, rect.Left + border.Left / 2, rect.Top + (border.Top > 0 ? radius.TopLeft : 0), fill);

				if (fill) {
					ctx.SetColor (parent.innerColor);
					ctx.Fill ();
				} else {
					ctx.SetColor (parent.borderColor);
					ctx.SetLineWidth (parent.borderWidth);
					ctx.Stroke ();
				}
			}
		}

		public RoundedFrameBox ()
		{
			canvas = SetInternalChild (new FrameCanvas ());
			base.Content = canvas;
		}

		public WidgetSpacing Padding {
			get { return padding; }
			set {
				padding = value;
				canvas.Resize ();
			}
		}

		public double PaddingLeft {
			get { return Padding.Left; }
			set { Padding = new WidgetSpacing (value, Padding.Top, Padding.Right, Padding.Bottom); }
		}

		public double PaddingTop {
			get { return Padding.Top; }
			set { Padding = new WidgetSpacing (Padding.Left, value, Padding.Right, Padding.Bottom); }
		}

		public double PaddingRight {
			get { return Padding.Right; }
			set { Padding = new WidgetSpacing (Padding.Left, Padding.Top, value, Padding.Bottom); }
		}

		public double PaddingBottom {
			get { return Padding.Bottom; }
			set { Padding = new WidgetSpacing (Padding.Left, Padding.Top, Padding.Right, value); }
		}

		WidgetSpacing BorderSpacing {
			get {
				return new WidgetSpacing (
					borderLeft ? borderWidth : 0,
					borderTop ? borderWidth : 0,
					borderRight ? borderWidth : 0,
					borderBottom ? borderWidth : 0
				);
			}
		}

		public double BorderWidth {
			get { return borderWidth; }
			set {
				borderWidth = value;
				canvas.Resize ();
			}
		}

		public bool BorderLeft {
			get { return borderLeft; }
			set {
				borderLeft = value;
				canvas.Resize ();
			}
		}

		public bool BorderRight {
			get { return borderRight; }
			set {
				borderRight = value;
				canvas.Resize ();
			}
		}

		public bool BorderTop {
			get { return borderTop; }
			set {
				borderTop = value;
				canvas.Resize ();
			}
		}

		public bool BorderBottom {
			get { return borderBottom; }
			set {
				borderBottom = value;
				canvas.Resize ();
			}
		}

		public Color BorderColor {
			get { return borderColor; }
			set { borderColor = value; canvas.QueueDraw (); }
		}

		public Color InnerBackgroundColor {
			get { return innerColor; }
			set { innerColor = value; canvas.QueueDraw (); }
		}

		public BorderCornerRadius CornerRadius {
			get { return cornerRadius; }
			set {
				cornerRadius = value;
				canvas.Resize ();
			}
		}

		public double CornerRadiusTopLeft {
			get { return CornerRadius.TopLeft; }
			set { CornerRadius = new BorderCornerRadius (value, CornerRadius.TopRight, CornerRadius.BottomRight, CornerRadius.BottomLeft); }
		}

		public double CornerRadiusTopRight {
			get { return CornerRadius.TopRight; }
			set { CornerRadius = new BorderCornerRadius (CornerRadius.TopLeft, value, CornerRadius.BottomRight, CornerRadius.BottomLeft); }
		}

		public double CornerRadiusBottomRight {
			get { return CornerRadius.BottomRight; }
			set { CornerRadius = new BorderCornerRadius (CornerRadius.TopLeft, CornerRadius.TopRight, value, CornerRadius.BottomLeft); }
		}

		public double CornerRadiusBottomLeft {
			get { return CornerRadius.BottomLeft; }
			set { CornerRadius = new BorderCornerRadius (CornerRadius.TopLeft, CornerRadius.TopRight, CornerRadius.BottomRight, value); }
		}

		public void Clear ()
		{
			Content = null;
		}

		public new Widget Content {
			get { return canvas.Child; }
			set {
 				var current = canvas.Child;
				canvas.Child = null;
				UnregisterChild (current);
 				RegisterChild (value);
				canvas.Child = value; 
			}
		}

		public struct BorderCornerRadius
		{
			double topLeft;
			double topRight;
			double bottomLeft;
			double bottomRight;

			static public implicit operator BorderCornerRadius (double value)
			{
				return new BorderCornerRadius (value, value, value, value);
			}

			public BorderCornerRadius (double topLeft = 0, double topRight = 0, double bottomRight = 0, double bottomLeft = 0) : this ()
			{
				this.topLeft = topLeft;
				this.topRight = topRight;
				this.bottomLeft = bottomLeft;
				this.bottomRight = bottomRight;
				UpdateSpacing ();
			}

			void UpdateSpacing ()
			{
				Spacing = new WidgetSpacing (
					Math.Max (TopLeft, BottomLeft),
					Math.Max (TopLeft, TopRight),
					Math.Max (TopRight, BottomRight),
					Math.Max (BottomLeft, BottomRight)
				);
			}

			public WidgetSpacing Spacing { get; private set; }

			public double TopLeft {
				get { return topLeft; }
				set {
					topLeft = value;
					UpdateSpacing ();
				}
			}

			public double TopRight {
				get { return topRight; }
				set {
					topRight = value;
					UpdateSpacing ();
				}
			}

			public double BottomLeft {
				get { return bottomLeft; }
				set {
					bottomLeft = value;
					UpdateSpacing ();
				}
			}

			public double BottomRight {
				get { return bottomRight; }
				set {
					bottomRight = value;
					UpdateSpacing ();
				}
			}
		}
	}
}
