//
// PopoverWindowTheme.cs
//
// Author:
//       Jason Smith <jason.smith@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using Mono.TextEditor;
using Gdk;

namespace MonoDevelop.Components
{
	public enum PopupPosition
	{
		Top = 0x01,
		TopLeft = 0x11,
		TopRight = 0x21,
		Bottom = 0x02,
		BottomLeft = 0x12,
		BottomRight = 0x22,
		Left = 0x04,
		LeftTop = 0x14,
		LeftBottom = 0x24,
		Right = 0x08,
		RightTop = 0x18,
		RightBottom = 0x28
	}

	public class PopoverWindowTheme
	{
		int cornerRadius;
		int padding;
		int arrowWidth;
		int arrowLength;
		Cairo.Color topColor;
		Cairo.Color bottomColor;
		Cairo.Color borderColor;
		Pango.FontDescription font;
		int currentPage;
		int pages;
		bool drawPager;
		bool pagerVertical;
		bool showArrow;
		PopupPosition targetPosition;

		public event EventHandler RedrawNeeded;

		/// <summary>
		/// Gets or sets the color of the top of the gradient used to render the background.
		/// </summary>
		public Cairo.Color TopColor { 
			get { return topColor; }
			set { SetAndEmit (value, topColor, ref topColor); }
		}

		/// <summary>
		/// Gets or sets the color of the bottom of the gradient used to render the background.
		/// </summary>
		public Cairo.Color BottomColor { 
			get { return bottomColor; }
			set { SetAndEmit (value, bottomColor, ref bottomColor); } 
		}

		/// <summary>
		/// Gets or sets the color of the border of the entire window. Set to transparent to disable border drawing.
		/// </summary>
		public Cairo.Color BorderColor { 
			get { return borderColor; }
			set { SetAndEmit (value, borderColor, ref borderColor); } 
		}


		Cairo.Color pagerBackgroundColorTop = CairoExtensions.ParseColor ("ffffff");
		/// <summary>
		/// Gets or sets the color of the top background color of the pager.
		/// </summary>
		public Cairo.Color PagerBackgroundColorTop {
			get {
				return pagerBackgroundColorTop;
			}
			set {
				pagerBackgroundColorTop = value;
			}
		}

		Cairo.Color pagerBackgroundColorBottom = CairoExtensions.ParseColor ("f5f5f5");
		/// <summary>
		/// Gets or sets the color of the bottom background color of the pager.
		/// </summary>
		public Cairo.Color PagerBackgroundColorBottom {
			get {
				return pagerBackgroundColorBottom;
			}
			set {
				pagerBackgroundColorBottom = value;
			}
		}

		Cairo.Color pagerTriangleColor = CairoExtensions.ParseColor ("737373");

		/// <summary>
		/// Gets or sets the color of the triangle color of the pager.
		/// </summary>
		public Cairo.Color PagerTriangleColor {
			get {
				return pagerTriangleColor;
			}
			set {
				pagerTriangleColor = value;
			}
		}

		/// <summary>
		/// Gets or sets the color of the text color of the pager.
		/// </summary>
		Cairo.Color pagerTextColor;
		bool pagerColorSet = false;
		public Cairo.Color PagerTextColor {
			get {
				if (!pagerColorSet) {
					return new Cairo.Color (BorderColor.R * .7,
					                        BorderColor.G * .7,
					                        BorderColor.B * .7,
					                        BorderColor.A);
				}
				return pagerTextColor;
			}
			set {
				pagerTextColor = value;
				pagerColorSet = true;
			}
		}

		/// <summary>
		/// Gets or sets the font description to be used for rendering of text elements inside the basic pager window.
		/// </summary>
		public Pango.FontDescription Font { 
			get { return font; }
			set { SetAndEmit (value, font, ref font); }
		}

		/// <summary>
		/// Gets or sets the corner radius. Corner radius cannot be set above 0 if ARGB is not supported.
		/// </summary>
		public int CornerRadius { 
			get { return cornerRadius; } 
			set {
				if (!GtkUtil.ScreenSupportsARGB ())
					value = 0;
				SetAndEmit (value, cornerRadius, ref cornerRadius);
			} 
		}

		/// <summary>
		/// Gets or sets the padding.
		/// </summary>
		public int Padding { 
			get { return padding; } 
			set { SetAndEmit (value, padding, ref padding); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="MonoDevelop.Components.PopoverWindowTheme"/> draws a pager.
		/// </summary>
		public bool DrawPager { 
			get { return drawPager; }
			set { SetAndEmit (value, drawPager, ref drawPager); }
		}

		/// <summary>
		/// Gets or sets the current page.
		/// </summary>
		public int CurrentPage { 
			get { return currentPage; }
			set { SetAndEmit (value, currentPage, ref currentPage); }
		}

		/// <summary>
		/// Gets or sets the number of pages.
		/// </summary>
		public int NumPages { 
			get { return pages; }
			set { SetAndEmit (value, pages, ref pages); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="MonoDevelop.Components.PopoverWindowTheme"/> should use a vertically styled pager.
		/// </summary>
		public bool PagerVertical {
			get { return pagerVertical; }
			set { SetAndEmit (value, pagerVertical, ref pagerVertical); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="MonoDevelop.Components.PopoverWindowTheme"/> should paint a thumb.
		/// This value will automatically be set to false if ARGB is not supported.
		/// </summary>
		public bool ShowArrow {
			get { return showArrow && GtkUtil.ScreenSupportsARGB (); }
			set { SetAndEmit (value, showArrow, ref showArrow); }
		}

		/// <summary>
		/// Gets or sets the target position for the thumb arrow. This may be overriden by the positioning of the window as needed.
		/// </summary>
		public PopupPosition TargetPosition {
			get { return targetPosition; }
			set { SetAndEmit (value, targetPosition, ref targetPosition); }
		}

		public int ArrowWidth {
			get { return arrowWidth; }
			set { SetAndEmit (value, arrowWidth, ref arrowWidth); }
		}

		public int ArrowLength {
			get { return arrowLength; }
			set { SetAndEmit (value, arrowLength, ref arrowLength); }
		}

		public int ArrowOffset { private get; set; }

		/// <summary>
		/// Convenience method to set the top and bottom color to the same color.
		/// </summary>
		public void SetFlatColor (Cairo.Color color)
		{
			TopColor = color;
			BottomColor = color;
		}

		public PopoverWindowTheme ()
		{
			CornerRadius = 4;
			Padding = 6;
			ArrowWidth = 10;
			ArrowLength = 5;
			TopColor = new Cairo.Color (1, 1, 1);
			BottomColor = new Cairo.Color (1, 1, 1);
			BorderColor = new Cairo.Color (0.7, 0.7, 0.7);

			Font = Pango.FontDescription.FromString ("Normal");
		}

		public void SetSchemeColors (Mono.TextEditor.Highlighting.ColorScheme scheme)
		{
			TopColor = scheme.TooltipText.Background.AddLight (0.03);
			BottomColor = scheme.TooltipText.Background;
			BorderColor = scheme.TooltipBorder.GetColor ("color");
			PagerTextColor = scheme.TooltipPagerText.GetColor ("color");
			PagerBackgroundColorTop = scheme.TooltipPagerTop.GetColor ("color");
			PagerBackgroundColorBottom = scheme.TooltipPagerBottom.GetColor ("color");
			PagerTriangleColor = scheme.TooltipPagerTriangle.GetColor ("color");
		}
		
		void EmitRedrawNeeded ()
		{
			if (RedrawNeeded != null)
				RedrawNeeded (this, EventArgs.Empty);
		}

		void SetAndEmit<T> (T newValue, T oldValue, ref T result)
		{
			if (newValue.Equals(oldValue))
				return;
			result = newValue;
			EmitRedrawNeeded ();
		}

		public virtual void RenderBorder (Cairo.Context context, Gdk.Rectangle region, PopupPosition arrowPosition)
		{
			SetBorderPath (context, region, arrowPosition);
			context.Color = BorderColor;
			context.LineWidth = 1;
			context.Stroke ();
		}

		object setBorderPathLastArgs;
		public virtual bool SetBorderPath (Cairo.Context context, Gdk.Rectangle region, PopupPosition arrowPosition)
		{
			double r = CornerRadius;
			if (ShowArrow) {
				double apos = ArrowOffset;
				double x = region.X + 0.5, y = region.Y + 0.5, w = region.Width - 1, h = region.Height - 1;

				context.MoveTo(x + r, y);
				if ((arrowPosition & PopupPosition.Top) != 0) {
					context.LineTo (x + apos - ArrowWidth / 2, y);
					context.RelLineTo (ArrowWidth / 2, -ArrowLength);
					context.RelLineTo (ArrowWidth / 2, ArrowLength);
				}
				context.Arc(x + w - r, y + r, r, Math.PI * 1.5, Math.PI * 2);
				if ((arrowPosition & PopupPosition.Right) != 0) {
					context.LineTo (x + w, y + apos - ArrowWidth / 2);
					context.RelLineTo (ArrowLength, ArrowWidth / 2);
					context.RelLineTo (-ArrowLength, ArrowWidth / 2);
				}
				context.Arc(x + w - r, y + h - r, r, 0, Math.PI * 0.5);
				if ((arrowPosition & PopupPosition.Bottom) != 0) {
					context.LineTo (x + apos + ArrowWidth / 2, y + h);
					context.RelLineTo (-ArrowWidth / 2, ArrowLength);
					context.RelLineTo (-ArrowWidth / 2, -ArrowLength);
				}
				context.Arc(x + r, y + h - r, r, Math.PI * 0.5, Math.PI);
				if ((arrowPosition & PopupPosition.Left) != 0) {
					context.LineTo (x, y + apos + ArrowWidth / 2);
					context.RelLineTo (-ArrowLength, -ArrowWidth / 2);
					context.RelLineTo (ArrowLength, -ArrowWidth / 2);
				}
				context.Arc(x + r, y + r, r, Math.PI, Math.PI * 1.5);
			} else {
				CairoExtensions.RoundedRectangle (context, region.X + 0.5, region.Y + 0.5, region.Width - 1, region.Height - 1, r);
			}

			var args = new {
				Region = region,
				ArrowPostion = arrowPosition,
				ArrowWidth = this.ArrowWidth,
				ArrowLength = this.arrowLength,
				ArrowOffset = this.ArrowOffset,
			};

			bool result = setBorderPathLastArgs == null ? true : setBorderPathLastArgs.Equals (args);
			setBorderPathLastArgs = args;
			return result;
		}

		/// <summary>
		/// Renders the background. Rendering will automatically be clipped to border constraints in later steps.
		/// </summary>
		public virtual void RenderBackground (Cairo.Context context, Gdk.Rectangle region)
		{
			using (var lg = new Cairo.LinearGradient (0, region.Y, 0, region.Y + region.Height)) {
				lg.AddColorStop (0, TopColor);
				lg.AddColorStop (1, BottomColor);
				context.Rectangle (region.X, region.Y, region.Width, region.Height);
				context.Pattern = lg;
				context.Fill ();
			}
		}

		/// <summary>
		/// Renders the pager.
		/// </summary>
		public virtual void RenderPager (Cairo.Context context, Pango.Context pangoContext, Gdk.Rectangle region)
		{
			//set global clip to avoid going outside general rendering area
			CairoExtensions.RoundedRectangle (context, region.X, region.Y, region.Width, region.Height, CornerRadius);
			context.Clip ();

			Pango.Layout layout = SetupPagerText (context, pangoContext);
			int textWidth, textHeight;
			layout.GetPixelSize (out textWidth, out textHeight);

			int width = textWidth + Styles.PopoverWindow.PagerTriangleSize * 2 + 20;
			int height = Styles.PopoverWindow.PagerHeight;

			Gdk.Rectangle boundingBox = new Gdk.Rectangle (region.X + region.Width - width, 0, width, height);
			RenderPagerBackground (context, boundingBox);

			int arrowPadding = 4;
			Gdk.Rectangle arrowRect = new Gdk.Rectangle (boundingBox.X + arrowPadding, 
			                                             boundingBox.Y + (boundingBox.Height - Styles.PopoverWindow.PagerTriangleSize) / 2,
			                                             Styles.PopoverWindow.PagerTriangleSize,
			                                             Styles.PopoverWindow.PagerTriangleSize);

			RenderPagerArrow (context, arrowRect, PagerVertical ? ArrowType.Up : ArrowType.Left);
			arrowRect.X = boundingBox.X + boundingBox.Width - (arrowPadding + Styles.PopoverWindow.PagerTriangleSize);
			RenderPagerArrow (context, arrowRect, PagerVertical ? ArrowType.Down : ArrowType.Right);

			RenderPagerText (context, layout, boundingBox);
		}

		/// <summary>
		/// Sets the Pango.Layout for pager text as it will be rendered. This will be used to perform sizing on the rest of the pager.
		/// </summary>
		protected virtual Pango.Layout SetupPagerText (Cairo.Context context, Pango.Context pangoContext)
		{
			Pango.Layout pl = new Pango.Layout (pangoContext);
			pl.SetText (string.Format ("{0} of {1}", CurrentPage + 1, NumPages));
			pl.FontDescription = Font;
			pl.FontDescription.AbsoluteSize = Pango.Units.FromPixels (Styles.PopoverWindow.PagerHeight - 5);

			return pl;
		}

		/// <summary>
		/// Renders the pager text using the results of SetupPagerText.
		/// </summary>
		protected virtual void RenderPagerText (Cairo.Context context, Pango.Layout layout, Gdk.Rectangle bounds)
		{
			int w, h;
			layout.GetPixelSize (out w, out h);

			context.MoveTo (bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2);

			context.Color = PagerTextColor;
			PangoCairoHelper.ShowLayout (context, layout);

			layout.Dispose ();
		}

		/// <summary>
		/// Renders the pager background. Clip is already set to prevent rendering outside of the primary borders of the popoverwindow.
		/// </summary>
		protected virtual void RenderPagerBackground (Cairo.Context context, Gdk.Rectangle bounds)
		{
			// draw background
			CairoExtensions.RoundedRectangle (context, 
			                                  bounds.X, 
			                                  bounds.Y, 
			                                  bounds.Width, 
			                                  bounds.Height, 
			                                  CornerRadius, 
			                                  CairoCorners.BottomLeft);
			using (var lg = new Cairo.LinearGradient (0, bounds.Y, 0, bounds.Y + bounds.Height)) {
				lg.AddColorStop (0, PagerBackgroundColorTop);
				lg.AddColorStop (1, PagerBackgroundColorBottom);

				context.Pattern = lg;
				context.Fill ();
			}

			// draw outline
			CairoExtensions.RoundedRectangle (context, 
			                                  bounds.X + .5, 
			                                  bounds.Y + .5, 
			                                  bounds.Width - 1, 
			                                  bounds.Height - 1, 
			                                  CornerRadius, 
			                                  CairoCorners.BottomLeft);
			context.LineWidth = 1;
			context.Color = BorderColor;
			context.Stroke ();
		}

		/// <summary>
		/// Renders a single pager arrow inside a bounding box.
		/// </summary>
		protected virtual void RenderPagerArrow (Cairo.Context context, Gdk.Rectangle bounds, ArrowType direction)
		{
			switch (direction) {
			case ArrowType.Up:
				context.MoveTo (bounds.X + bounds.Width / 2.0, bounds.Y);
				context.LineTo (bounds.X, bounds.Y + bounds.Height);
				context.LineTo (bounds.X + bounds.Width, bounds.Y + bounds.Height);
				context.ClosePath ();
				break;
			case ArrowType.Down:
				context.MoveTo (bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height);
				context.LineTo (bounds.X, bounds.Y);
				context.LineTo (bounds.X + bounds.Width, bounds.Y);
				context.ClosePath ();
				break;
			case ArrowType.Left:
				context.MoveTo (bounds.X, bounds.Y + bounds.Height / 2.0);
				context.LineTo (bounds.X + bounds.Width, bounds.Y);
				context.LineTo (bounds.X + bounds.Width, bounds.Y + bounds.Height);
				context.ClosePath ();
				break;
			case ArrowType.Right:
				context.MoveTo (bounds.X + bounds.Width, bounds.Y + bounds.Height / 2.0);
				context.LineTo (bounds.X, bounds.Y);
				context.LineTo (bounds.X, bounds.Y + bounds.Height);
				context.ClosePath ();
				break;
			default:
				return;
			}

			context.Color = PagerTriangleColor;
			context.Fill ();
		}
	}
}

