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
using Gdk;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

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
		Cairo.Color backgroundColor;
		Cairo.Color shadowColor;
		Pango.FontDescription font;
		int currentPage;
		int pages;
		bool drawPager;
		bool pagerVertical;
		bool showArrow;
		PopupPosition targetPosition;
		static readonly int pagerArrowPadding = 4;

		public event EventHandler RedrawNeeded;

		/// <summary>
		/// Gets or sets the color of the background.
		/// </summary>
		public Cairo.Color BackgroundColor { 
			get { return backgroundColor; }
			set { SetAndEmit (value, backgroundColor, ref backgroundColor); }
		}

		Cairo.Color pagerBackgroundColor = CairoExtensions.ParseColor ("ffffff");
		/// <summary>
		/// Gets or sets the color of the background color of the pager.
		/// </summary>
		public Cairo.Color PagerBackgroundColor {
			get {
				return pagerBackgroundColor;
			}
			set {
				pagerBackgroundColor = value;
			}
		}

		/// <summary>
		/// Gets or sets the color of the border of the entire window. Set to transparent to disable border drawing.
		/// </summary>
		public Cairo.Color ShadowColor { 
			get { return shadowColor; }
			set { SetAndEmit (value, shadowColor, ref shadowColor); } 
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
					// FIXME: VV: Sane value!
					//return new Cairo.Color (BorderColor.R * .7,
					//                        BorderColor.G * .7,
					//                        BorderColor.B * .7,
					//                        BorderColor.A);
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
		/// Set the background color.
		/// </summary>
		public void SetBackgroundColor (Cairo.Color color)
		{
			BackgroundColor = color;
		}

		public PopoverWindowTheme ()
		{
			CornerRadius = 4;
			Padding = 6;
			ArrowWidth = 10;
			ArrowLength = 5;
			BackgroundColor = Styles.PopoverWindow.DefaultBackgroundColor.ToCairoColor ();
			ShadowColor = Styles.PopoverWindow.ShadowColor.ToCairoColor ();

			Font = Pango.FontDescription.FromString ("Normal");
		}

		public void SetSchemeColors (EditorTheme scheme)
		{
			;
			BackgroundColor = SyntaxHighlightingService.GetColor (scheme, EditorThemeColors.TooltipBackground);
			PagerTextColor = SyntaxHighlightingService.GetColor (scheme, EditorThemeColors.TooltipPagerText);
			PagerBackgroundColor = SyntaxHighlightingService.GetColor (scheme, EditorThemeColors.TooltipPager);
			PagerTriangleColor = SyntaxHighlightingService.GetColor (scheme, EditorThemeColors.TooltipPagerTriangle);
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
			context.SetSourceColor (BackgroundColor);
			context.LineWidth = 1;
			context.Stroke ();
		}

		public virtual void RenderShadow (Cairo.Context context, Gdk.Rectangle region, PopupPosition arrowPosition)
		{
			RenderBorder (context, region, arrowPosition);
			double r = CornerRadius;
			double x = region.X + 0.5, y = region.Y + 0.5, w = region.Width - 1, h = region.Height - 1;
			context.MoveTo(x + w, y + h - r);
			context.Arc(x + w - r, y + h - r, r, 0, Math.PI * 0.5);
			if (ShowArrow && (arrowPosition & PopupPosition.Bottom) != 0) {
				double apos = ArrowOffset;
				context.LineTo (x + apos + ArrowWidth / 2, y + h);
				context.RelLineTo (-ArrowWidth / 2, ArrowLength);
				context.RelLineTo (-ArrowWidth / 2, -ArrowLength);
			}
			context.Arc(x + r, y + h - r, r, Math.PI * 0.5, Math.PI);

			// FIXME: VV: Remove gradient features
			using (var lg = new Cairo.LinearGradient (0, y + h - r, 0, y + h)) {
				lg.AddColorStop (0.5, ShadowColor.MultiplyAlpha (0.0));
				lg.AddColorStop (1, ShadowColor);
				context.SetSource (lg);
				context.LineWidth = 1;
				context.Stroke ();
			}
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
			context.Rectangle (region.X, region.Y, region.Width, region.Height);
			context.SetSourceColor (BackgroundColor);
			context.Fill ();
		}

		/// <summary>
		/// Renders the pager.
		/// </summary>
		public virtual void RenderPager (Cairo.Context context, Pango.Context pangoContext, Gdk.Rectangle region)
		{
			//set global clip to avoid going outside general rendering area
			CairoExtensions.RoundedRectangle (context, region.X, region.Y, region.Width, region.Height, CornerRadius);
			context.Clip ();

			using (var layout = SetupPagerText (pangoContext)) {
				var boundingBox = GetPagerBounds (layout, region);

				RenderPagerBackground (context, boundingBox);

				Gdk.Rectangle arrowRect = new Gdk.Rectangle (boundingBox.X + pagerArrowPadding,
															 boundingBox.Y + (boundingBox.Height - Styles.PopoverWindow.PagerTriangleSize) / 2,
															 Styles.PopoverWindow.PagerTriangleSize,
															 Styles.PopoverWindow.PagerTriangleSize);

				RenderPagerArrow (context, arrowRect, PagerVertical ? ArrowType.Up : ArrowType.Left);
				arrowRect.X = boundingBox.X + boundingBox.Width - (pagerArrowPadding + Styles.PopoverWindow.PagerTriangleSize);
				RenderPagerArrow (context, arrowRect, PagerVertical ? ArrowType.Down : ArrowType.Right);

				RenderPagerText (context, layout, boundingBox);
			}
		}

		Gdk.Rectangle GetPagerBounds (Pango.Layout layout, Gdk.Rectangle region)
		{
			int textWidth, textHeight;
			layout.GetPixelSize (out textWidth, out textHeight);

			int width = textWidth + Styles.PopoverWindow.PagerTriangleSize * 2 + 20;
			int height = Styles.PopoverWindow.PagerHeight;

			return new Gdk.Rectangle (region.X + region.Width - width, 0, width, height);
		}

		public bool HitTestPagerLeftArrow (Pango.Context pangoContext, Gdk.Rectangle region, Gdk.Point hitPoint)
		{
			using (Pango.Layout layout = SetupPagerText (pangoContext)) {
				var boundingBox = GetPagerBounds (layout, region);
				Gdk.Rectangle arrowActiveRect = new Gdk.Rectangle (boundingBox.X,
																   boundingBox.Y,
																   Styles.PopoverWindow.PagerTriangleSize + (pagerArrowPadding * 2),
																   boundingBox.Height);
				return arrowActiveRect.Contains (hitPoint);
			}
		}

		public bool HitTestPagerRightArrow (Pango.Context pangoContext, Gdk.Rectangle region, Gdk.Point hitPoint)
		{
			using (Pango.Layout layout = SetupPagerText (pangoContext)) {
				var boundingBox = GetPagerBounds (layout, region);
				Gdk.Rectangle arrowActiveRect = new Gdk.Rectangle (boundingBox.X + boundingBox.Width - (pagerArrowPadding * 2 + Styles.PopoverWindow.PagerTriangleSize),
																   boundingBox.Y,
																   Styles.PopoverWindow.PagerTriangleSize + (pagerArrowPadding * 2),
																   boundingBox.Height);
				return arrowActiveRect.Contains (hitPoint);
			}
		}

		/// <summary>
		/// Sets the Pango.Layout for pager text as it will be rendered. This will be used to perform sizing on the rest of the pager.
		/// </summary>
		protected virtual Pango.Layout SetupPagerText (Pango.Context pangoContext)
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

			context.SetSourceColor (PagerTextColor);
			Pango.CairoHelper.ShowLayout (context, layout);
		}

		/// <summary>
		/// Renders the pager background. Clip is already set to prevent rendering outside of the primary borders of the popoverwindow.
		/// </summary>
		protected virtual void RenderPagerBackground (Cairo.Context context, Gdk.Rectangle bounds)
		{
			// draw background
			CairoExtensions.RoundedRectangle (context, 
			                                  bounds.X + 1, 
			                                  bounds.Y + 1, 
			                                  bounds.Width - 2, 
			                                  bounds.Height - 1, 
			                                  CornerRadius, 
                                              CairoCorners.All);
			
			context.SetSourceColor (PagerBackgroundColor);
			context.Fill ();
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

			context.SetSourceColor (PagerTriangleColor);
			context.Fill ();
		}
	}
}

