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
using MonoDevelop.Ide.Gui;
using Gdk;
using Xwt.Motion;

namespace MonoDevelop.Components
{
	public class PopoverWindow : Gtk.Window, IAnimatable
	{
		PopoverWindowTheme theme;

		PopupPosition position;
		Gtk.Alignment alignment;

		Gdk.Rectangle currentCaret;
		Gdk.Point targetWindowOrigin = new Point (-1, -1);
		Gtk.Widget parent;
		Xwt.Widget xwtParent;
		bool eventProvided;

		Gdk.Size targetSize;
		Gdk.Size paintSize;

		bool disableSizeCheck;

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
			AddEvents ((int)EventMask.ButtonReleaseMask);

			alignment = new Alignment (0, 0, 1f, 1f);
			alignment.Show ();
			Add (alignment);

			disableSizeCheck = false;

			SizeRequested += (object o, SizeRequestedArgs args) => {
				if (this.AnimationIsRunning("Resize") && !disableSizeCheck) {
					Gtk.Requisition result = new Gtk.Requisition ();
					result.Width  = Math.Max (args.Requisition.Width, Math.Max (Allocation.Width, targetSize.Width));
					result.Height = Math.Max (args.Requisition.Height, Math.Max (Allocation.Height, targetSize.Height));
					args.Requisition = result;
				}
			};

			UpdatePadding ();
		}

		public PopoverWindowTheme Theme { 
			get { 
				if (theme == null) {
					theme = new PopoverWindowTheme ();
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

		public int CaretSpacing { get; set; }

		public bool ShowArrow {
			get { return Theme.ShowArrow; }
			set { Theme.ShowArrow = value; }
		}

		public Gtk.Alignment ContentBox {
			get { return alignment; }
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

		public void ShowPopup (Xwt.Rectangle onScreenArea, PopupPosition position)
		{
			this.parent = IdeApp.Workbench.RootWindow;
			this.currentCaret = new Rectangle ((int)onScreenArea.X, (int)onScreenArea.Y, (int)onScreenArea.Width, (int)onScreenArea.Height);
			Theme.TargetPosition = position;
			targetWindowOrigin = new Point ((int)onScreenArea.X, (int)onScreenArea.Y);
			RepositionWindow ();
		}

		internal void ShowPopup (Xwt.Widget widget, Xwt.Rectangle caret, PopupPosition position)
		{
			xwtParent = widget;
			this.currentCaret = new Gdk.Rectangle ((int)caret.X, (int)caret.Y, (int)caret.Width, (int)caret.Height);
			Theme.TargetPosition = position;
			var pos = GtkUtil.GetSceenBounds (widget);
			targetWindowOrigin = new Point ((int)pos.X, (int)pos.Y);
			RepositionWindow ();
		}

		void ShowPopup (Gtk.Widget parent, Gdk.EventButton evt, Gdk.Rectangle caret, PopupPosition position)
		{
			this.parent = parent;
			this.currentCaret = caret;
			Theme.TargetPosition = position;
			Gdk.Window targetWindow;
			if (evt != null) {
				eventProvided = true;
				targetWindow = evt.Window;
			} else
				targetWindow = parent.GdkWindow;
			
			if (targetWindow != null) {
				int x, y;
				targetWindow.GetOrigin (out x, out y);
				targetWindowOrigin = new Point (x, y);
			}
			RepositionWindow ();
		}

		Gdk.Rectangle GetScreenCoordinates (Gdk.Rectangle caret)
		{
			if (parent != null)
				return GtkUtil.ToScreenCoordinates (parent, parent.GdkWindow, caret);
			if (xwtParent != null) {
				return GtkUtil.ToScreenCoordinates (xwtParent, caret.ToXwtRectangle ());
			}
			return Gdk.Rectangle.Zero;
		}

		Gdk.Size GetParentSize ()
		{
			if (parent != null) {
				var alloc = parent.Allocation;
				return new Size (alloc.Width, alloc.Height);
			}
			if (xwtParent != null) {
				var size = xwtParent.Size;
				return new Size ((int) size.Width, (int) size.Height);
			}
			return Size.Empty;
		}

		bool HasParent {
			get { return parent != null || xwtParent != null; }
		}

		Gdk.Rectangle GetUsableMonitorGeometry (Gdk.Rectangle caret)
		{
			Screen screen = null;
			if (parent != null)
				screen = parent.Screen;
			else if (xwtParent != null)
				screen = Gdk.Screen.Default; // FIXME: should we try to get the Screen from the backend?

			if (screen != null)
				return GtkWorkarounds.GetUsableMonitorGeometry (screen, screen.GetMonitorAtPoint (caret.X, caret.Y));
			
			return Gdk.Rectangle.Zero;
		}
		
		void IAnimatable.BatchBegin () { }
		void IAnimatable.BatchCommit () { QueueDraw (); }

		public void AnimatedResize ()
		{
			if (!GtkUtil.ScreenSupportsARGB ()) {
				QueueResize();
				return;
			}

			disableSizeCheck = true;
			Gtk.Requisition sizeReq = Gtk.Requisition.Zero;
			// use OnSizeRequested instead of SizeRequest to bypass internal GTK caching
			OnSizeRequested (ref sizeReq);
			disableSizeCheck = false;

			Gdk.Size size = new Gdk.Size (sizeReq.Width, sizeReq.Height);

			// ensure that our apint area is big enough for our padding
			if (paintSize.Width <= 15 || paintSize.Height <= 15)
				paintSize = size;

			targetSize = size;
			Gdk.Size start = paintSize;
			Func<double, Gdk.Size> transform = x => new Gdk.Size ((int)(start.Width + (size.Width - start.Width) * x),
			                                                     (int)(start.Height + (size.Height - start.Height) * x));
			this.Animate ("Resize",
			              transform,
			              s => paintSize = s,
			              length: 150,
			              easing: Easing.SinInOut,
			              finished: (x, aborted) => { if (!aborted) MaybeReanimate(); });
			QueueResize ();
		}

		protected override void OnDestroyed ()
		{
			this.AbortAnimation ("Resize");
			base.OnDestroyed ();
		}

		void MaybeReanimate ()
		{
			disableSizeCheck = true;
			Gtk.Requisition sizeReq = Gtk.Requisition.Zero;
			OnSizeRequested (ref sizeReq);
			disableSizeCheck = false;

			if (sizeReq.Width == paintSize.Width && sizeReq.Height == paintSize.Height)
				QueueResize ();
			else
				AnimatedResize (); //Desired size changed mid animation
		}

		/// <summary>
		/// Gets or sets the maximum Y top bound. The popover window will be placed below this bound.
		/// 0 means it's not set. Default value: 0
		/// </summary>
		public int MaximumYTopBound {
			get;
			set;
		}

		protected PopupPosition CurrentPosition { get { return position; }}

		public virtual void RepositionWindow (Gdk.Rectangle? newCaret = null)
		{
			if (!HasParent)
				return;

			if (newCaret.HasValue) {//Update caret if parameter is given
				currentCaret = newCaret.Value;
			}
			Gdk.Rectangle caret = currentCaret;
			if (targetWindowOrigin.X < 0)
				return;
			int x = targetWindowOrigin.X;
			int y = targetWindowOrigin.Y;
			PopupPosition position = Theme.TargetPosition;
			this.position = Theme.TargetPosition;
			UpdatePadding ();

			var psize = GetParentSize ();

			if (eventProvided) {
				caret.X = x;
				caret.Y = y;
				caret.Width = caret.Height = 1;
			} else {
				if (caret.Equals (Gdk.Rectangle.Zero))
					caret = new Gdk.Rectangle (0, 0, psize.Width, psize.Height);
				caret = GetScreenCoordinates (caret);
			}

			caret.Inflate (CaretSpacing, CaretSpacing);
			if (!Core.Platform.IsWindows)
				caret.Inflate (-1, -1);

			Gtk.Requisition request = SizeRequest ();
			Gdk.Rectangle geometry = GetUsableMonitorGeometry (caret);

			// Add some spacing between the screen border and the popover window
			geometry.Inflate (-5, -5);

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
				y = caret.Bottom + 1;
				break;
			case PopupPosition.Bottom:
				y = caret.Y - request.Height; break;
			case PopupPosition.Right:
				x = caret.X - request.Width; break;
			case PopupPosition.Left:
				x = caret.Right + 1; break;
			}
			int offset;
			if ((position & PopupPosition.Top) != 0 || (position & PopupPosition.Bottom) != 0) {
				if (((int)position & 0x10) != 0)
					x = caret.X - MinArrowSpacing - Theme.ArrowWidth/2;
				else if (((int)position & 0x20) != 0)
					x = caret.Right - request.Width + MinArrowSpacing + Theme.ArrowWidth/2;
				else
					x = caret.X + (caret.Width - request.Width) / 2;

				if (x < geometry.Left)
					x = geometry.Left;
				else if (x + request.Width > geometry.Right)
					x = geometry.Right - request.Width;

				offset = caret.X + caret.Width / 2 - x;
				if (offset - Theme.ArrowWidth/2 < MinArrowSpacing)
					offset = MinArrowSpacing + Theme.ArrowWidth/2;
				if (offset > request.Width - MinArrowSpacing - Theme.ArrowWidth/2)
					offset = request.Width - MinArrowSpacing - Theme.ArrowWidth/2;
			}
			else {
				if (((int)position & 0x10) != 0)
					y = caret.Y - MinArrowSpacing - Theme.ArrowWidth/2;
				else if (((int)position & 0x20) != 0)
					y = caret.Bottom - request.Height + MinArrowSpacing + Theme.ArrowWidth/2;
				else
					y = caret.Y + (caret.Height - request.Height) / 2;

				if (y < geometry.Top)
					y = geometry.Top;
				else if (y + request.Height > geometry.Bottom)
					y = geometry.Bottom - request.Height;
				if (MaximumYTopBound > 0)
					y = Math.Max (MaximumYTopBound, y);

				offset = caret.Y + caret.Height / 2 - y;
				if (offset - Theme.ArrowWidth/2 < MinArrowSpacing)
					offset = MinArrowSpacing + Theme.ArrowWidth/2;
				if (offset > request.Height - MinArrowSpacing - Theme.ArrowWidth/2)
					offset = request.Height - MinArrowSpacing - Theme.ArrowWidth/2;
			}
			Theme.ArrowOffset = offset;
			this.position = position;
			UpdatePadding ();

			Move (x, y);
			Show ();
			if (!ShowWindowShadow)
				DesktopService.RemoveWindowShadow (this);
		}
		
		public bool SupportsAlpha {
			get;
			private set;
		}

		public bool ShowWindowShadow {
			get;
			set;
		}

		void CheckScreenColormap ()
		{
			SupportsAlpha = Screen.IsComposited;
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
			bool retVal;
			bool changed;
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Save ();
				if (SupportsAlpha) {
					context.Operator = Cairo.Operator.Source;
					context.SetSourceRGBA (1, 1, 1, 0);
				} else {
					context.Operator = Cairo.Operator.Over;
					context.SetSourceRGB (1, 1, 1);
				}
				context.Paint ();
				context.Restore ();

				OnDrawContent (evnt, context); // Draw content first so we can easily clip it
				retVal = base.OnExposeEvent (evnt);

				changed = Theme.SetBorderPath (context, BorderAllocation, position);
				context.Operator = Cairo.Operator.DestIn;
				context.SetSourceRGBA (1, 1, 1, 1);
				context.Fill ();
				context.Operator = Cairo.Operator.Over;

				// protect against overriden methods which leave in a bad state
				context.Save ();
				if (Theme.DrawPager) {
					Theme.RenderPager (context, 
					                   PangoContext,
					                   BorderAllocation);
				}

				Theme.RenderShadow (context, BorderAllocation, position);
				context.Restore ();
			}

			if (changed)
				GtkWorkarounds.UpdateNativeShadow (this);

			return retVal;
		}

		protected virtual void OnDrawContent (Gdk.EventExpose evnt, Cairo.Context context)
		{
			Theme.RenderBackground (context, new Gdk.Rectangle (Allocation.X, Allocation.Y, paintSize.Width, paintSize.Height));
		}

		void UpdatePadding ()
		{
			uint top,left,bottom,right;
			top = left = bottom = right = (uint)(Theme.Padding + (Core.Platform.IsWindows ? 1 : 2));

			if (ShowArrow) {
				if ((position & PopupPosition.Top) != 0)
					top += (uint)Theme.ArrowLength;
				else if ((position & PopupPosition.Bottom) != 0)
					bottom += (uint)Theme.ArrowLength;
				else if ((position & PopupPosition.Left) != 0)
					left += (uint)Theme.ArrowLength;
				else if ((position & PopupPosition.Right) != 0)
					right += (uint)Theme.ArrowLength;
			}
			alignment.SetPadding (top, bottom, left, right);
		}

		void OnRedrawNeeded (object sender, EventArgs args)
		{
			UpdatePadding ();
			QueueDraw ();
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			if (!this.AnimationIsRunning ("Resize"))
				paintSize = new Gdk.Size (allocation.Width, allocation.Height);

			base.OnSizeAllocated (allocation);
		}

		protected Rectangle ChildAllocation {
			get {
				var rect = BorderAllocation;
				rect.Inflate (-Theme.Padding - 1, -Theme.Padding - 1);
				return rect;
			}
		}

		Rectangle BorderAllocation {
			get {
				var rect = new Gdk.Rectangle (Allocation.X, Allocation.Y, paintSize.Width, paintSize.Height);
				if (ShowArrow) {
					if ((position & PopupPosition.Top) != 0) {
						rect.Y += Theme.ArrowLength;
						rect.Height -= Theme.ArrowLength;
					}
					else if ((position & PopupPosition.Bottom) != 0) {
						rect.Height -= Theme.ArrowLength;
					}
					else if ((position & PopupPosition.Left) != 0) {
						rect.X += Theme.ArrowLength;
						rect.Width -= Theme.ArrowLength;
					}
					else if ((position & PopupPosition.Right) != 0) {
						rect.Width -= Theme.ArrowLength;
					}
				}
				if (!Core.Platform.IsWindows) {
					if ((position & PopupPosition.Top) != 0) {
						rect.Y += 1;
						rect.Height -= 1;
					}
					else if ((position & PopupPosition.Bottom) != 0) {
						rect.Height -= 1;
					}
					else if ((position & PopupPosition.Left) != 0) {
						rect.X += 1;
						rect.Width -= 1;
					}
					else if ((position & PopupPosition.Right) != 0) {
						rect.Width -= 1;
					}
				}
				return rect;
			}
		}

		public event EventHandler PagerLeftClicked;
		public event EventHandler PagerRightClicked;

		protected virtual void OnPagerLeftClicked ()
		{
			if (PagerLeftClicked != null)
				PagerLeftClicked (this, null);
		}

		protected virtual void OnPagerRightClicked ()
		{
			if (PagerRightClicked != null)
				PagerRightClicked (this, null);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (evnt.Button != 1 || !Theme.DrawPager)
				return base.OnButtonPressEvent (evnt);

			var retval = false;
			if (retval = Theme.HitTestPagerLeftArrow (PangoContext, BorderAllocation, new Point ((int)evnt.X, (int)evnt.Y)))
				OnPagerLeftClicked ();
			else if (retval = Theme.HitTestPagerRightArrow (PangoContext, BorderAllocation, new Point ((int)evnt.X, (int)evnt.Y)))
				OnPagerRightClicked ();

			return retval;
		}
	}
}

