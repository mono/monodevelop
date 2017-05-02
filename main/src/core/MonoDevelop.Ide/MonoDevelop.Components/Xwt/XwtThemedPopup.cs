//
// XwtThemedPopup.cs
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
using System.Linq;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace MonoDevelop.Components
{
	public class XwtThemedPopup : XwtPopup
	{
		XwtPopoverCanvas container;

		const int MinArrowSpacing = 5;

		public new Widget Content {
			get {
				return container.Content;
			}
			set {
				container.Content = value;
			}
		}

		public XwtThemedPopup () : this (PopupType.Tooltip)
		{
		}

		public XwtThemedPopup (PopupType type) : base (type)
		{
			base.Content = container = new XwtPopoverCanvas ();
			BackgroundColor = Xwt.Drawing.Colors.Transparent;
			Decorated = true;
			Theme.TargetPosition = CurrentPosition;
		}

		public bool ShowArrow {
			get { return Theme.ShowArrow; }
			set { Theme.ShowArrow = value; }
		}

		public XwtPopupWindowTheme Theme {
			get {
				return container.Theme;
			}
			set {
				container.Theme = value;
			}
		}

		protected override void CurrentPositionChanged ()
		{
			Theme.TargetPosition = CurrentPosition;
		}

		Size HandleGetPreferredSize (SizeConstraint widthConstraint, SizeConstraint heightConstraint)
		{
			return OnGetPreferredContentSize (widthConstraint, heightConstraint);
		}

		void HandleDrawContent (Context ctx, Rectangle contentBounds, Rectangle backgroundBounds)
		{
			OnDrawContent (ctx, contentBounds, backgroundBounds);
		}

		bool HandlePagerLeftClicked ()
		{
			return OnPagerLeftClicked ();
		}

		bool HandlePagerRightClicked ()
		{
			return OnPagerRightClicked ();
		}

		protected virtual Size OnGetPreferredContentSize (SizeConstraint widthConstraint, SizeConstraint heightConstraint)
		{
			return new Size (-1, -1);
		}

		protected virtual void OnDrawContent (Context ctx, Rectangle contentBounds, Rectangle backgroundBounds)
		{
		}

		public event EventHandler PagerLeftClicked;
		public event EventHandler PagerRightClicked;

		protected virtual bool OnPagerLeftClicked ()
		{
			if (PagerLeftClicked != null) {
				var args = new WidgetEventArgs ();
				PagerLeftClicked (this, args);
				return args.Handled;
			}
			return false;
		}

		protected virtual bool OnPagerRightClicked ()
		{
			if (PagerRightClicked != null) {
				var args = new WidgetEventArgs ();
				PagerRightClicked (this, args);
				return args.Handled;
			}
			return false;
		}

		public override void RepositionWindow (Rectangle? newTargetRect = default (Rectangle?))
		{
			if (!HasParent)
				return;

			if (newTargetRect.HasValue) //Update caret if parameter is given
				TargetRectangle = newTargetRect.Value;

			Rectangle currentRect = TargetRectangle;
			PopupPosition position = TargetPosition;
			CurrentPosition = TargetPosition;

			var psize = GetParentSize ();

			if (EventProvided) {
				currentRect.X = TargetWindowOrigin.X;
				currentRect.Y = TargetWindowOrigin.Y;
				currentRect.Width = currentRect.Height = 1;
			} else {
				if (currentRect.IsEmpty)
					currentRect = new Rectangle (0, 0, psize.Width, psize.Height);
				currentRect = GetScreenCoordinates (currentRect);
			}

			currentRect.Inflate (RectOffset, RectOffset);
			if (!Core.Platform.IsWindows)
				currentRect.Inflate (-1, -1);

			var request = container.Surface.GetPreferredSize (true) + new Size (Padding.HorizontalSpacing, Padding.VerticalSpacing);

			Rectangle geometry = GetUsableMonitorGeometry (currentRect);

			if (!geometry.IsEmpty) {
				// 	// Add some spacing between the screen border and the popover window
				geometry = geometry.Inflate (-5, -5);

				// Flip the orientation if the window doesn't fit the screen.

				int intPos = (int)position;
				switch ((PopupPosition)(intPos & 0x0f)) {
				case PopupPosition.Top:
					if (currentRect.Bottom + request.Height > geometry.Bottom)
						intPos = (intPos & 0xf0) | (int)PopupPosition.Bottom;
					break;
				case PopupPosition.Bottom:
					if (currentRect.Top - request.Height < geometry.X)
						intPos = (intPos & 0xf0) | (int)PopupPosition.Top;
					break;
				case PopupPosition.Right:
					if (currentRect.X - request.Width < geometry.X)
						intPos = (intPos & 0xf0) | (int)PopupPosition.Left;
					break;
				case PopupPosition.Left:
					if (currentRect.Right + request.Width > geometry.Right)
						intPos = (intPos & 0xf0) | (int)PopupPosition.Right;
					break;
				}

				position = (PopupPosition)intPos;
			}

			CurrentPosition = position;

			// Calculate base coordinate
			double x = 0, y = 0;

			switch ((PopupPosition)((int)position & 0x0f)) {
			case PopupPosition.Top:
				y = currentRect.Bottom + 1;
				break;
			case PopupPosition.Bottom:
				y = currentRect.Y - request.Height; break;
			case PopupPosition.Right:
				x = currentRect.X - request.Width; break;
			case PopupPosition.Left:
				x = currentRect.Right + 1; break;
			}
			double offset;
			if ((position & PopupPosition.Top) != 0 || (position & PopupPosition.Bottom) != 0) {
				if (((int)position & 0x10) != 0)
					x = currentRect.X - MinArrowSpacing - Theme.ArrowWidth / 2;
				else if (((int)position & 0x20) != 0)
					x = currentRect.Right - request.Width + MinArrowSpacing + Theme.ArrowWidth / 2;
				else
					x = currentRect.X + (currentRect.Width - request.Width) / 2;

				if (x < geometry.Left)
					x = geometry.Left;
				else if (x + request.Width > geometry.Right)
					x = geometry.Right - request.Width;

				offset = currentRect.X + currentRect.Width / 2 - x;
				if (offset - Theme.ArrowWidth / 2 < MinArrowSpacing)
					offset = MinArrowSpacing + Theme.ArrowWidth / 2;
				if (offset > request.Width - MinArrowSpacing - Theme.ArrowWidth / 2)
					offset = request.Width - MinArrowSpacing - Theme.ArrowWidth / 2;
			} else {
				if (((int)position & 0x10) != 0)
					y = currentRect.Y - MinArrowSpacing - Theme.ArrowWidth / 2;
				else if (((int)position & 0x20) != 0)
					y = currentRect.Bottom - request.Height + MinArrowSpacing + Theme.ArrowWidth / 2;
				else
					y = currentRect.Y + (currentRect.Height - request.Height) / 2;

				if (y < geometry.Top)
					y = geometry.Top;
				else if (y + request.Height > geometry.Bottom)
					y = geometry.Bottom - request.Height;
				if (MaximumYTopBound > 0)
					y = Math.Max (MaximumYTopBound, y);

				offset = currentRect.Y + currentRect.Height / 2 - y;
				if (offset - Theme.ArrowWidth / 2 < MinArrowSpacing)
					offset = MinArrowSpacing + Theme.ArrowWidth / 2;
				if (offset > request.Height - MinArrowSpacing - Theme.ArrowWidth / 2)
					offset = request.Height - MinArrowSpacing - Theme.ArrowWidth / 2;
			}
			Theme.ArrowOffset = (int)offset;

			Location = new Point (x, y);
		}

		class XwtPopoverCanvas : Canvas
		{
			Widget content;
			XwtPopupWindowTheme theme;
			WidgetSpacing padding;

			public XwtPopupWindowTheme Theme {
				get {
					if (theme == null) {
						theme = new XwtPopupWindowTheme ();
						theme.RedrawNeeded += OnRedrawNeeded;
					}
					return theme;
				}
				set {
					if (theme == value)
						return;

					theme.RedrawNeeded -= OnRedrawNeeded;
					theme = value;
					theme.RedrawNeeded += OnRedrawNeeded;

					UpdatePadding ();
					QueueDraw ();
				}
			}

			void OnRedrawNeeded (object sender, EventArgs args)
			{
				UpdatePadding ();
				QueueDraw ();
			}

			public new XwtThemedPopup ParentWindow {
				get { return base.ParentWindow as XwtThemedPopup; }
			}

			public new Widget Content {
				get { return content; }
				set {
					if (value == null)
						throw new InvalidOperationException ();
					if (content == value)
						return;
					if (content != null) {
						RemoveChild (content);
					}
					content = value;
					var size = content.Surface.GetPreferredSize ();
					AddChild (content, new Rectangle (new Point (Padding.Left, Padding.Top), size));
					QueueForReallocate ();
				}
			}

			WidgetSpacing Padding {
				get {
					return padding;
				}
				set {
					if (padding != value) {
						padding = value;
						OnPreferredSizeChanged ();
					}
				}
			}

			void UpdatePadding ()
			{
				double top, left, bottom, right;
				top = left = bottom = right = (Theme.Padding + (Core.Platform.IsWindows ? 1 : 2));

				if (Theme.ShowArrow) {
					if ((Theme.TargetPosition & PopupPosition.Top) != 0)
						top += Theme.ArrowLength;
					else if ((Theme.TargetPosition & PopupPosition.Bottom) != 0)
						bottom += Theme.ArrowLength;
					else if ((Theme.TargetPosition & PopupPosition.Left) != 0)
						left += Theme.ArrowLength;
					else if ((Theme.TargetPosition & PopupPosition.Right) != 0)
						right += Theme.ArrowLength;
				}
				Padding = new WidgetSpacing (left, top, right, bottom);
			}

			protected override void OnBoundsChanged ()
			{
				base.OnBoundsChanged ();
				if (content == null)
					return;
				var childBounds = new Rectangle ();
				childBounds.Size = content.Surface.GetPreferredSize ();
				childBounds.X = Padding.Left + content.MarginLeft;
				childBounds.Y = Padding.Top + content.MarginTop;
				SetChildBounds (content, childBounds);
			}

			protected override void OnChildPreferredSizeChanged ()
			{
				base.OnChildPreferredSizeChanged ();
			}

			protected override Size OnGetPreferredSize (SizeConstraint widthConstraint, SizeConstraint heightConstraint)
			{
				var sizeRequest = ParentWindow.HandleGetPreferredSize (widthConstraint, heightConstraint);
				if (sizeRequest.Height > -1 && sizeRequest.Width > -1) {
					sizeRequest.Width += Padding.HorizontalSpacing;
					sizeRequest.Height += Padding.VerticalSpacing;
					return sizeRequest;
				}

				if (content == null)
					return base.OnGetPreferredSize (widthConstraint, heightConstraint);
				var s = content.Surface.GetPreferredSize (true);
				s.Width += Padding.HorizontalSpacing;
				s.Height += Padding.VerticalSpacing;
				return s;
			}

			protected sealed override void OnDraw (Context ctx, Rectangle dirtyRect)
			{
				var borderBounds = BorderAllocation;
				Theme.RenderBackground (ctx, borderBounds, Theme.TargetPosition);
				ctx.Save ();

				var contentBounds = borderBounds.Inflate (-Theme.Padding, -Theme.Padding);
				ctx.MoveTo (contentBounds.Location);
				ParentWindow.HandleDrawContent (ctx, contentBounds, Bounds);
				base.OnDraw (ctx, dirtyRect);

				ctx.Restore ();
				if (Theme.DrawPager)
					Theme.RenderPager (ctx, borderBounds);

				Theme.RenderBorder (ctx, borderBounds, Theme.TargetPosition);
				Theme.RenderShadow (ctx, borderBounds, Theme.TargetPosition);
			}

			Rectangle BorderAllocation {
				get {
					var rect = Bounds;
					if (Theme.ShowArrow) {
						if ((Theme.TargetPosition & PopupPosition.Top) != 0) {
							rect.Y += Theme.ArrowLength;
							rect.Height -= Theme.ArrowLength;
						} else if ((Theme.TargetPosition & PopupPosition.Bottom) != 0) {
							rect.Height -= Theme.ArrowLength;
						} else if ((Theme.TargetPosition & PopupPosition.Left) != 0) {
							rect.X += Theme.ArrowLength;
							rect.Width -= Theme.ArrowLength;
						} else if ((Theme.TargetPosition & PopupPosition.Right) != 0) {
							rect.Width -= Theme.ArrowLength;
						}
					}
					if (!Core.Platform.IsWindows) {
						if ((Theme.TargetPosition & PopupPosition.Top) != 0) {
							rect.Y += 1;
							rect.Height -= 1;
						} else if ((Theme.TargetPosition & PopupPosition.Bottom) != 0) {
							rect.Height -= 1;
						} else if ((Theme.TargetPosition & PopupPosition.Left) != 0) {
							rect.X += 1;
							rect.Width -= 1;
						} else if ((Theme.TargetPosition & PopupPosition.Right) != 0) {
							rect.Width -= 1;
						}
					}
					return rect;
				}
			}

			protected override void OnButtonPressed (ButtonEventArgs args)
			{
				if (args.Button == PointerButton.Left && Theme.DrawPager) {
					if (Theme.HitTestPagerLeftArrow (BorderAllocation, args.Position))
							args.Handled = ParentWindow.HandlePagerLeftClicked ();
					else if (Theme.HitTestPagerRightArrow (BorderAllocation, args.Position))
						args.Handled = ParentWindow.HandlePagerRightClicked ();
				}
				base.OnButtonPressed (args);
			}
		}
	}
}
