//
// SearchPopupWindow.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

	public class PopoverWindow : Gtk.Window
	{
		bool showArrow;
		PopupPosition position;
		int arrowOffset;
		int cornerRadius = 4;
		int padding = 6;
		Cairo.Color backgroundColor = new Cairo.Color (1, 1, 1);
		Cairo.Color borderColor = new Cairo.Color (0.7, 0.7, 0.7);
		Gtk.Alignment alignment;

		Gdk.Rectangle currentCaret;
		PopupPosition targetPosition;
		Gdk.Window targetWindow;
		Gtk.Widget parent;
		bool eventProvided;

		Tweener resizeTweener;

		Gdk.Size animStartSize;
		Gdk.Size animFinishSize;
		Gdk.Size paintSize;

		bool disableSizeCheck;

		const int ArrowLength = 5;
		const int ArrowWidth = 10;
		const int MinArrowSpacing = 5;

		public PopoverWindow () : this(Gtk.WindowType.Popup)
		{
		}

		public PopoverWindow (Gtk.WindowType type) : base(type)
		{
			SkipTaskbarHint = true;
			SkipPagerHint = true;
			AppPaintable = true;
			TypeHint = WindowTypeHint.Tooltip;
			CheckScreenColormap ();

			alignment = new Alignment (0, 0, 1f, 1f);
			alignment.Show ();
			Add (alignment);

			disableSizeCheck = false;

			resizeTweener = new Tweener (200, 16);
			resizeTweener.Easing = new SinInOutEasing ();
			resizeTweener.ValueUpdated += (sender, e) => {
				paintSize = new Gdk.Size ((int)(animStartSize.Width + (animFinishSize.Width - animStartSize.Width) * resizeTweener.Value),
				                          (int)(animStartSize.Height + (animFinishSize.Height - animStartSize.Height) * resizeTweener.Value));
				QueueDraw ();
			};

			resizeTweener.Finished += (sender, e) => {
				paintSize = animFinishSize;

				QueueResize ();
				QueueDraw ();
			};

			SizeRequested += (object o, SizeRequestedArgs args) => {
				if (resizeTweener.IsRunning && !disableSizeCheck) {
					Gtk.Requisition result = new Gtk.Requisition ();
					result.Width  = Math.Max (args.Requisition.Width, Math.Max (Allocation.Width, animFinishSize.Width));
					result.Height = Math.Max (args.Requisition.Height, Math.Max (Allocation.Height, animFinishSize.Height));
					args.Requisition = result;
				}
			};
		}

		public Gtk.Alignment ContentBox {
			get { return alignment; }
		}

		public bool ShowArrow {
			get {
				return showArrow;
			}
			set {
				showArrow = value;
				UpdatePadding ();
				QueueDraw ();
			}
		}

		public int CornerRadius {
			get { return cornerRadius; }
			set {
				cornerRadius = value; 
				padding = value;
				QueueDraw (); 
				UpdatePadding ();
			}
		}

		public Cairo.Color BackgroundColor {
			get { return backgroundColor; }
			set { backgroundColor = value; QueueDraw (); }
		}
		
		public Cairo.Color BorderColor {
			get { return borderColor; }
			set { borderColor = value; QueueDraw (); }
		}
		
		public void ShowPopup (Gtk.Widget widget, PopupPosition position)
		{
			ShowPopup (widget, null, Gdk.Rectangle.Zero, position);
		}
		
		public void ShowPopup (Gtk.Widget widget, Gdk.EventButton evt, PopupPosition position)
		{
			ShowPopup (widget, evt, Gdk.Rectangle.Zero, position);
		}

		public void ShowPopup (Gtk.Widget widget, Gdk.Rectangle caret, PopupPosition position)
		{
			ShowPopup (widget, null, caret, position);
		}

		void ShowPopup (Gtk.Widget parent, Gdk.EventButton evt, Gdk.Rectangle caret, PopupPosition position)
		{
			this.parent = parent;
			this.currentCaret = caret;
			this.targetPosition = position;

			if (evt != null) {
				eventProvided = true;
				targetWindow = evt.Window;
			} else
				targetWindow = parent.GdkWindow;

			RepositionWindow ();
		}

		public void AnimatedResize ()
		{
			disableSizeCheck = true;
			Gtk.Requisition sizeReq;
			// use OnSizeRequested instead of SizeRequest to bypass internal GTK caching
			OnSizeRequested (ref sizeReq);
			disableSizeCheck = false;

			Gdk.Size size = new Gdk.Size (sizeReq.Width, sizeReq.Height);

			if (paintSize.Width <= 0 || paintSize.Height <= 0)
				paintSize = size;

			if (resizeTweener.IsRunning)
				resizeTweener.Reset ();

			animFinishSize = size;
			animStartSize = paintSize;

			resizeTweener.Start ();
			QueueResize ();
		}

		public void RepositionWindow ()
		{
			if (parent == null)
				return;

			int x, y;
			Gdk.Rectangle caret = currentCaret;
			Gdk.Window window = targetWindow;
			if (targetWindow == null)
				return;
			PopupPosition position = targetPosition;
			this.position = targetPosition;
			UpdatePadding ();

			window.GetOrigin (out x, out y);
			var alloc = parent.Allocation;

			if (eventProvided) {
				caret.X = x;
				caret.Y = y;
				caret.Width = caret.Height = 1;
			} else {
				if (caret.Equals (Gdk.Rectangle.Zero))
					caret = new Gdk.Rectangle (0, 0, alloc.Width, alloc.Height);
				caret = GtkUtil.ToScreenCoordinates (parent, parent.GdkWindow, caret);
			}

			Gtk.Requisition request = SizeRequest ();
			var screen = parent.Screen;
			Gdk.Rectangle geometry = GtkWorkarounds.GetUsableMonitorGeometry (screen, screen.GetMonitorAtPoint (x, y));

			// Flip the orientation if the window doesn't fit the screen.

			int intPos = (int) position;
			switch ((PopupPosition)(intPos & 0x0f)) {
			case PopupPosition.Top:
				if (caret.Bottom + request.Height > geometry.Bottom)
					intPos = (intPos & 0xf0) | (int)PopupPosition.Bottom;
				break;
			case PopupPosition.Bottom:
				if (caret.Top - request.Height < geometry.X)
					intPos = (intPos & 0xf0) | (int)PopupPosition.Top;
				break;
			case PopupPosition.Right:
				if (caret.X - request.Width < geometry.X)
					intPos = (intPos & 0xf0) | (int)PopupPosition.Left;
				break;
			case PopupPosition.Left:
				if (caret.Right + request.Width > geometry.Right)
					intPos = (intPos & 0xf0) | (int)PopupPosition.Right;
				break;
			}

			position = (PopupPosition) intPos;
			UpdatePadding ();

			// Calculate base coordinate

			switch ((PopupPosition)((int)position & 0x0f)) {
			case PopupPosition.Top:
				y = caret.Bottom;
				break;
			case PopupPosition.Bottom:
				y = caret.Y - request.Height; break;
			case PopupPosition.Right:
				x = caret.X - request.Width; break;
			case PopupPosition.Left:
				x = caret.Right; break;
			}

			if ((position & PopupPosition.Top) != 0 || (position & PopupPosition.Bottom) != 0) {
				if (((int)position & 0x10) != 0)
					x = caret.X - MinArrowSpacing - ArrowWidth/2;
				else if (((int)position & 0x20) != 0)
					x = caret.Right - request.Width + MinArrowSpacing + ArrowWidth/2;
				else
					x = caret.X + (caret.Width - request.Width) / 2;

				if (x < geometry.Left)
					x = geometry.Left;
				else if (x + request.Width > geometry.Right)
					x = geometry.Right - request.Width;

				arrowOffset = caret.X + caret.Width / 2 - x;
				if (arrowOffset - ArrowWidth/2 < MinArrowSpacing)
					arrowOffset = MinArrowSpacing + ArrowWidth/2;
				if (arrowOffset > request.Width - MinArrowSpacing - ArrowWidth/2)
					arrowOffset = request.Width - MinArrowSpacing - ArrowWidth/2;
			}
			else {
				if (((int)position & 0x10) != 0)
					y = caret.Y - MinArrowSpacing - ArrowWidth/2;
				else if (((int)position & 0x20) != 0)
					y = caret.Bottom - request.Height + MinArrowSpacing + ArrowWidth/2;
				else
					y = caret.Y + (caret.Height - request.Height) / 2;

				if (y < geometry.Top)
					y = geometry.Top;
				else if (y + request.Height > geometry.Bottom)
					y = geometry.Bottom - request.Height;

				arrowOffset = caret.Y + caret.Height / 2 - y;
				if (arrowOffset - ArrowWidth/2 < MinArrowSpacing)
					arrowOffset = MinArrowSpacing + ArrowWidth/2;
				if (arrowOffset > request.Height - MinArrowSpacing - ArrowWidth/2)
					arrowOffset = request.Height - MinArrowSpacing - ArrowWidth/2;
			}

			this.position = position;
			UpdatePadding ();

			Move (x, y);
			Show ();
		}
		
		public bool SupportsAlpha {
			get;
			private set;
		}

		void CheckScreenColormap ()
		{
			SupportsAlpha = Screen.RgbaColormap != null;
			if (SupportsAlpha) {
				Colormap = Screen.RgbaColormap;
			} else {
				Colormap = Screen.RgbColormap;
			}
		}

		protected override void OnScreenChanged (Gdk.Screen previous_screen)
		{
			base.OnScreenChanged (previous_screen);
			CheckScreenColormap ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Save ();
				if (SupportsAlpha) {
					context.SetSourceRGBA (1, 1, 1, 0);
				} else {
					context.SetSourceRGB (1, 1, 1);
				}
				context.Operator = Cairo.Operator.DestIn;
				context.Paint ();
				context.Restore ();

				context.Save ();
				BorderPath (context);
				context.Clip ();
				OnDrawContent (evnt, context);
				context.Restore ();

				BorderPath (context);
				context.Color = borderColor;
				context.LineWidth = 1;
				context.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}

		protected virtual void OnDrawContent (Gdk.EventExpose evnt, Cairo.Context context)
		{
			context.Rectangle (Allocation.X, Allocation.Y, paintSize.Width, paintSize.Height);
			context.Color = backgroundColor;
			context.Fill ();
		}

		void BorderPath (Cairo.Context cr)
		{
			double r = cornerRadius;
			if (showArrow) {
				double apos = arrowOffset;
				var re = BorderAllocation;
				double x = re.X + 0.5, y = re.Y + 0.5, w = re.Width - 1, h = re.Height - 1;

				cr.MoveTo(x + r, y);
				if ((position & PopupPosition.Top) != 0) {
					cr.LineTo (x + apos - ArrowWidth / 2, y);
					cr.RelLineTo (ArrowWidth / 2, -ArrowLength);
					cr.RelLineTo (ArrowWidth / 2, ArrowLength);
				}
				cr.Arc(x + w - r, y + r, r, Math.PI * 1.5, Math.PI * 2);
				if ((position & PopupPosition.Right) != 0) {
					cr.LineTo (x + w, y + apos - ArrowWidth / 2);
					cr.RelLineTo (ArrowLength, ArrowWidth / 2);
					cr.RelLineTo (-ArrowLength, ArrowWidth / 2);
				}
				cr.Arc(x + w - r, y + h - r, r, 0, Math.PI * 0.5);
				if ((position & PopupPosition.Bottom) != 0) {
					cr.LineTo (x + apos + ArrowWidth / 2, y + h);
					cr.RelLineTo (-ArrowWidth / 2, ArrowLength);
					cr.RelLineTo (-ArrowWidth / 2, -ArrowLength);
				}
				cr.Arc(x + r, y + h - r, r, Math.PI * 0.5, Math.PI);
				if ((position & PopupPosition.Left) != 0) {
					cr.LineTo (x, y + apos + ArrowWidth / 2);
					cr.RelLineTo (-ArrowLength, -ArrowWidth / 2);
					cr.RelLineTo (ArrowLength, -ArrowWidth / 2);
				}
				cr.Arc(x + r, y + r, r, Math.PI, Math.PI * 1.5);
			}
			else
				CairoExtensions.RoundedRectangle (cr, 0.5, 0.5, paintSize.Width - 1, paintSize.Height - 1, r);
		}

		void UpdatePadding ()
		{
			uint top,left,bottom,right;
			top = left = bottom = right = (uint)padding + 1;

			if (showArrow) {
				if ((position & PopupPosition.Top) != 0)
					top += ArrowLength;
				else if ((position & PopupPosition.Bottom) != 0)
					bottom += ArrowLength;
				else if ((position & PopupPosition.Left) != 0)
					left += ArrowLength;
				else if ((position & PopupPosition.Right) != 0)
					right += ArrowLength;
			}
			alignment.SetPadding (top, bottom, left, right);
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			if (!resizeTweener.IsRunning)
				paintSize = new Gdk.Size (allocation.Width, allocation.Height);

			base.OnSizeAllocated (allocation);
		}

		protected Rectangle ChildAllocation {
			get {
				var rect = BorderAllocation;
				rect.Inflate (-padding - 1, -padding - 1);
				return rect;
			}
		}

		Rectangle BorderAllocation {
			get {
				var rect = new Gdk.Rectangle (Allocation.X, Allocation.Y, paintSize.Width, paintSize.Height);
				if (showArrow) {
					if ((position & PopupPosition.Top) != 0) {
						rect.Y += ArrowLength;
						rect.Height -= ArrowLength;
					}
					else if ((position & PopupPosition.Bottom) != 0) {
						rect.Height -= ArrowLength;
					}
					else if ((position & PopupPosition.Left) != 0) {
						rect.X += ArrowLength;
						rect.Width -= ArrowLength;
					}
					else if ((position & PopupPosition.Right) != 0) {
						rect.Width -= ArrowLength;
					}
				}
				return rect;
			}
		}
	}
}

