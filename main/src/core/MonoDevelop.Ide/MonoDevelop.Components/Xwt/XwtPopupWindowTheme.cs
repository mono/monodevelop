//
// XwtPopoverWindowTheme.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.Components
{
	public enum PagerArrowDirection
	{
		Up,
		Down,
		Left,
		Right,
		None
	}
	
	public class XwtPopupWindowTheme
	{
		int cornerRadius;
		int padding;
		int arrowWidth;
		int arrowLength;
		Color backgroundColor;
		Color shadowColor;
		Color borderColor;
		Font font;
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
		public Color BackgroundColor {
			get { return backgroundColor; }
			set { SetAndEmit (value, backgroundColor, ref backgroundColor); }
		}

		Color pagerBackgroundColor = Color.FromName ("ffffff");
		/// <summary>
		/// Gets or sets the color of the background color of the pager.
		/// </summary>
		public Color PagerBackgroundColor {
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
		public Color BorderColor {
			get { return borderColor; }
			set { SetAndEmit (value, borderColor, ref borderColor); }
		}

		/// <summary>
		/// Gets or sets the color of the bottom shadow border of the window. Set to transparent to disable shadow drawing.
		/// </summary>
		public Color ShadowColor {
			get { return shadowColor; }
			set { SetAndEmit (value, shadowColor, ref shadowColor); }
		}

		Color pagerTriangleColor = Color.FromName ("737373");

		/// <summary>
		/// Gets or sets the color of the triangle color of the pager.
		/// </summary>
		public Color PagerTriangleColor {
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
		Color pagerTextColor;
		bool pagerColorSet = false;
		public Color PagerTextColor {
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
		public Font Font {
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
		public void SetBackgroundColor (Color color)
		{
			BackgroundColor = color;
		}

		public XwtPopupWindowTheme ()
		{
			CornerRadius = 4;
			Padding = 6;
			ArrowWidth = 10;
			ArrowLength = 5;
			BackgroundColor = Styles.PopoverWindow.DefaultBackgroundColor;
			ShadowColor = Styles.PopoverWindow.ShadowColor;
			BorderColor = Colors.Transparent;

			Font = Font.SystemFont.WithWeight (FontWeight.Normal);
		}

		public void SetSchemeColors (EditorTheme scheme)
		{
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
			if (newValue.Equals (oldValue))
				return;
			result = newValue;
			EmitRedrawNeeded ();
		}

		public virtual void RenderBorder (Context context, Rectangle region, PopupPosition arrowPosition)
		{
			if (BorderColor.Alpha <= 0)
				return;
			SetBorderPath (context, region, arrowPosition);
			context.SetColor (BorderColor);
			context.SetLineWidth (1);
			context.Stroke ();
		}

		public virtual void RenderShadow (Context context, Rectangle region, PopupPosition arrowPosition)
		{
			if (ShadowColor.Alpha <= 0)
				return;
			double r = CornerRadius;

			// if we proceed with r == 0 the .Arc below will throw anyway
			if (r <= 0)
				return;
			double x = region.X + 0.5, y = region.Y + 0.5, w = region.Width - 1, h = region.Height - 1;
			context.MoveTo (x + w, y + h - r);
			context.Arc (x + w - r, y + h - r, r, 0, 90);
			if (ShowArrow && (arrowPosition & PopupPosition.Bottom) != 0) {
				double apos = ArrowOffset;
				context.LineTo (x + apos + ArrowWidth / 2, y + h);
				context.RelLineTo (-ArrowWidth / 2, ArrowLength);
				context.RelLineTo (-ArrowWidth / 2, -ArrowLength);
			}
			context.Arc (x + r, y + h - r, r, 90, 180);
			context.SetColor (ShadowColor);
			context.SetLineWidth (1);
			context.Stroke ();
		}

		object setBorderPathLastArgs;
		public virtual bool SetBorderPath (Context context, Rectangle region, PopupPosition arrowPosition)
		{
			double r = CornerRadius;
			if (ShowArrow) {
				double apos = ArrowOffset;
				double x = region.X + 0.5, y = region.Y + 0.5, w = region.Width - 1, h = region.Height - 1;

				context.MoveTo (x + r, y);
				if ((arrowPosition & PopupPosition.Top) != 0) {
					context.LineTo (x + apos - ArrowWidth / 2, y);
					context.RelLineTo (ArrowWidth / 2, -ArrowLength);
					context.RelLineTo (ArrowWidth / 2, ArrowLength);
				}
				context.LineTo (x + w - r, y);
				context.Arc (x + w - r, y + r, r, -90, 0);
				if ((arrowPosition & PopupPosition.Right) != 0) {
					context.LineTo (x + w, y + apos - ArrowWidth / 2);
					context.RelLineTo (ArrowLength, ArrowWidth / 2);
					context.RelLineTo (-ArrowLength, ArrowWidth / 2);
				}
				context.LineTo (x + w, y + h - r);
				context.Arc (x + w - r, y + h - r, r, 0, 90);
				if ((arrowPosition & PopupPosition.Bottom) != 0) {
					context.LineTo (x + apos + ArrowWidth / 2, y + h);
					context.RelLineTo (-ArrowWidth / 2, ArrowLength);
					context.RelLineTo (-ArrowWidth / 2, -ArrowLength);
				}
				context.LineTo (x + r, y + h);
				context.Arc (x + r, y + h - r, r, 90, 180);
				if ((arrowPosition & PopupPosition.Left) != 0) {
					context.LineTo (x, y + apos + ArrowWidth / 2);
					context.RelLineTo (-ArrowLength, -ArrowWidth / 2);
					context.RelLineTo (ArrowLength, -ArrowWidth / 2);
				}
				context.LineTo (x, y + r);
				context.Arc (x + r, y + r, r, 180, 270);
			} else
				context.RoundRectangle (region.X + 0.5, region.Y + 0.5, region.Width - 1, region.Height - 1, r);

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
		public virtual void FillBackground (Context context)
		{
			context.SetColor (BackgroundColor);
			context.FillPreserve ();
		}

		/// <summary>
		/// Renders the background and clips the context to the visible border.
		/// </summary>
		public virtual void RenderBackground (Context context, Rectangle region, PopupPosition arrowPosition)
		{
			SetBorderPath (context, region, arrowPosition);
			context.SetColor (BackgroundColor);
			context.FillPreserve ();
			context.Clip ();
		}

		/// <summary>
		/// Renders the pager.
		/// </summary>
		public virtual void RenderPager (Context context, Rectangle region)
		{
			//set global clip to avoid going outside general rendering area
			context.RoundRectangle (region.X, region.Y, region.Width, region.Height, CornerRadius);
			context.Clip ();

			using (var layout = SetupPagerText ()) {
				var boundingBox = GetPagerBounds (layout, region);

				RenderPagerBackground (context, boundingBox);

				Rectangle arrowRect = new Rectangle (boundingBox.X + pagerArrowPadding,
															 boundingBox.Y + (boundingBox.Height - Styles.PopoverWindow.PagerTriangleSize) / 2,
															 Styles.PopoverWindow.PagerTriangleSize,
															 Styles.PopoverWindow.PagerTriangleSize);

				RenderPagerArrow (context, arrowRect, PagerVertical ? PagerArrowDirection.Up : PagerArrowDirection.Left);
				arrowRect.X = boundingBox.X + boundingBox.Width - (pagerArrowPadding + Styles.PopoverWindow.PagerTriangleSize);
				RenderPagerArrow (context, arrowRect, PagerVertical ? PagerArrowDirection.Down : PagerArrowDirection.Right);

				RenderPagerText (context, layout, boundingBox);
			}
		}

		Rectangle GetPagerBounds (TextLayout layout, Rectangle region)
		{
			var textSize = layout.GetSize ();

			var width = textSize.Width + Styles.PopoverWindow.PagerTriangleSize * 2 + 20;
			var height = Styles.PopoverWindow.PagerHeight;

			return new Rectangle (region.X + region.Width - width, 0, width, height);
		}

		public bool HitTestPagerLeftArrow (Rectangle region, Point hitPoint)
		{
			using (var layout = SetupPagerText ()) {
				var boundingBox = GetPagerBounds (layout, region);
				Rectangle arrowActiveRect = new Rectangle (boundingBox.X,
																   boundingBox.Y,
																   Styles.PopoverWindow.PagerTriangleSize + (pagerArrowPadding * 2),
																   boundingBox.Height);
				return arrowActiveRect.Contains (hitPoint);
			}
		}

		public bool HitTestPagerRightArrow (Rectangle region, Point hitPoint)
		{
			using (var layout = SetupPagerText ()) {
				var boundingBox = GetPagerBounds (layout, region);
				Rectangle arrowActiveRect = new Rectangle (boundingBox.X + boundingBox.Width - (pagerArrowPadding * 2 + Styles.PopoverWindow.PagerTriangleSize),
																   boundingBox.Y,
																   Styles.PopoverWindow.PagerTriangleSize + (pagerArrowPadding * 2),
																   boundingBox.Height);
				return arrowActiveRect.Contains (hitPoint);
			}
		}

		/// <summary>
		/// Sets the Pango.Layout for pager text as it will be rendered. This will be used to perform sizing on the rest of the pager.
		/// </summary>
		protected virtual TextLayout SetupPagerText ()
		{
			var layout = new TextLayout ();
			layout.Text = string.Format ("{0} of {1}", CurrentPage + 1, NumPages);
			layout.Font = Font.WithSize (Pango.Units.FromPixels (Styles.PopoverWindow.PagerHeight - 5) / Pango.Scale.PangoScale);

			return layout;
		}

		/// <summary>
		/// Renders the pager text using the results of SetupPagerText.
		/// </summary>
		protected virtual void RenderPagerText (Context context, TextLayout layout, Rectangle bounds)
		{
			var s = layout.GetSize ();
			context.SetColor (PagerTextColor);
			context.DrawTextLayout (layout, bounds.X + (bounds.Width - s.Width) / 2, bounds.Y + (bounds.Height - s.Height) / 2);
		}

		/// <summary>
		/// Renders the pager background. Clip is already set to prevent rendering outside of the primary borders of the popoverwindow.
		/// </summary>
		protected virtual void RenderPagerBackground (Context context, Rectangle bounds)
		{
			// draw background
			context.RoundRectangle (bounds.X + 1,
			                        bounds.Y + 1,
			                        bounds.Width - 2,
			                        bounds.Height - 1,
			                        CornerRadius);

			context.SetColor (PagerBackgroundColor);
			context.Fill ();
		}

		/// <summary>
		/// Renders a single pager arrow inside a bounding box.
		/// </summary>
		protected virtual void RenderPagerArrow (Context context, Rectangle bounds, PagerArrowDirection direction)
		{
			switch (direction) {
			case PagerArrowDirection.Up:
				context.MoveTo (bounds.X + bounds.Width / 2.0, bounds.Y);
				context.LineTo (bounds.X, bounds.Y + bounds.Height);
				context.LineTo (bounds.X + bounds.Width, bounds.Y + bounds.Height);
				context.ClosePath ();
				break;
			case PagerArrowDirection.Down:
				context.MoveTo (bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height);
				context.LineTo (bounds.X, bounds.Y);
				context.LineTo (bounds.X + bounds.Width, bounds.Y);
				context.ClosePath ();
				break;
			case PagerArrowDirection.Left:
				context.MoveTo (bounds.X, bounds.Y + bounds.Height / 2.0);
				context.LineTo (bounds.X + bounds.Width, bounds.Y);
				context.LineTo (bounds.X + bounds.Width, bounds.Y + bounds.Height);
				context.ClosePath ();
				break;
			case PagerArrowDirection.Right:
				context.MoveTo (bounds.X + bounds.Width, bounds.Y + bounds.Height / 2.0);
				context.LineTo (bounds.X, bounds.Y);
				context.LineTo (bounds.X, bounds.Y + bounds.Height);
				context.ClosePath ();
				break;
			default:
				return;
			}

			context.SetColor (PagerTriangleColor);
			context.Fill ();
		}
	}
}
