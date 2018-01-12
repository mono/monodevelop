//
// XwtPopoverWindow.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2016 (c) Microsoft Corporation
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
using Xwt;
using Xwt.Backends;
using Xwt.Motion;
using Xwt.Drawing;
using MonoDevelop.Ide;

namespace MonoDevelop.Components
{
	public class XwtPopup : PopupWindow
	{
		object target;
		PopupPosition targetPosition, currentPosition;
		Rectangle targetRect;
		Point targetWindowOrigin = new Point (-1, -1);
		bool eventProvided;

		public PopupPosition CurrentPosition {
			get {
				return currentPosition;
			}
			protected set {
				if (currentPosition != value) {
					currentPosition = value;
					CurrentPositionChanged ();
				}
			}
		}

		protected Rectangle TargetRectangle {
			get { return targetRect; }
			set { targetRect = value; }
		}

		protected PopupPosition TargetPosition {
			get { return targetPosition; }
			set { targetPosition = value; }
		}

		protected Point TargetWindowOrigin {
			get { return targetWindowOrigin; }
		}

		protected bool EventProvided {
			get { return eventProvided; }
		}

		protected Toolkit Toolkit { get; private set; }

		protected virtual void CurrentPositionChanged ()
		{
		}

		public double RectOffset { get; set; }

		/// <summary>
		/// Gets or sets the maximum Y top bound. The popover window will be placed below this bound.
		/// 0 means it's not set. Default value: 0
		/// </summary>
		public int MaximumYTopBound { get; set; }

		protected bool HasParent {
			get { return target != null; }
		}

		public XwtPopup (PopupType type) : base (type)
		{
			currentPosition = targetPosition = PopupPosition.Top;
			Toolkit = Toolkit.CurrentEngine;
		}

		public void ShowPopup (Control widget, Rectangle targetRectangle, PopupPosition position)
		{
			if (widget is XwtControl)
				ShowPopup (((XwtControl)widget).Widget, targetRectangle, position);
			if (widget.nativeWidget is Gtk.Widget)
				ShowPopup (widget.GetNativeWidget<Gtk.Widget> (), null, targetRectangle.ToGdkRectangle (), position);
			#if MAC
			else if (widget.nativeWidget is AppKit.NSView)
				ShowPopup (widget.GetNativeWidget<AppKit.NSView> (), targetRectangle, position);
			#endif
			else
				throw new NotSupportedException ("The control is not supported");
		}

		#if MAC
		private void ShowPopup (AppKit.NSView widget, Rectangle targetRectangle, PopupPosition position)
		{
			target = widget;
			targetRect = targetRectangle;
			targetPosition = position;
			try {
				TransientFor = Toolkit.Load (Xwt.ToolkitType.XamMac)?.WrapWindow (widget.Window);
			} catch {
				if (MessageDialog.RootWindow != null)
					TransientFor = MessageDialog.RootWindow;
			}
			var pos = GtkUtil.GetSceenBounds (widget);
			targetWindowOrigin = new Point (pos.X, pos.Y);
			ShowPopupInternal ();
		}
		#endif

		public void ShowPopup (Control widget, PopupPosition position)
		{
			ShowPopup (widget, Rectangle.Zero, position);
		}

		public void ShowPopup (Gtk.Widget widget, Gdk.EventButton evt, PopupPosition position)
		{
			ShowPopup (widget, evt, Gdk.Rectangle.Zero, position);
		}

		public void ShowPopup (Gtk.Widget widget, Gdk.Rectangle targetRectangle, PopupPosition position)
		{
			ShowPopup (widget, null, targetRectangle, position);
		}

		public void ShowPopup (Rectangle onScreenArea, PopupPosition position)
		{
			target = IdeApp.Workbench.RootWindow;
			targetRect = onScreenArea;
			targetPosition = position;
			if (MessageDialog.RootWindow != null)
				TransientFor = MessageDialog.RootWindow;
			targetWindowOrigin = new Point (onScreenArea.X, onScreenArea.Y);
			ShowPopupInternal ();
		}

		public void ShowPopup (Widget widget, PopupPosition position)
		{
			ShowPopup (widget, Rectangle.Zero, position);
		}

		public void ShowPopup (Widget widget, Rectangle targetRectangle, PopupPosition position)
		{
			target = widget;
			targetRect = targetRectangle;
			targetPosition = position;
			TransientFor = widget.ParentWindow;
			var pos = GtkUtil.GetSceenBounds (widget);
			targetWindowOrigin = new Point (pos.X, pos.Y);
			ShowPopupInternal ();
		}

		void ShowPopup (Gtk.Widget parent, Gdk.EventButton evt, Gdk.Rectangle targetRectangle, PopupPosition position)
		{
			target = parent;
			targetRect = targetRectangle.ToXwtRectangle ();
			targetPosition = position;
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
			Gtk.Window parentWindow = parent.Toplevel as Gtk.Window;
			if (parentWindow != null)
				try {
					TransientFor = Toolkit.Load (ToolkitType.Gtk).WrapWindow (parentWindow);
				} catch {
					if (MessageDialog.RootWindow != null)
						TransientFor = MessageDialog.RootWindow;
				}
			else if (MessageDialog.RootWindow != null)
				TransientFor = MessageDialog.RootWindow;
			ShowPopupInternal ();
		}

		void ShowPopupInternal ()
		{
			Opacity = 0;
			RepositionWindow ();
			Show ();
			Opacity = 1;
		}

		protected Rectangle GetScreenCoordinates (Rectangle targetRect)
		{
			var screenLocation = Point.Zero;
			Toolkit targetToolkit = null;
			var xwtTarget = target as Widget;
			if (xwtTarget != null) {
				screenLocation = xwtTarget.ConvertToScreenCoordinates (targetRect.Location);
				targetToolkit = xwtTarget.Surface.ToolkitEngine;
			} else {
				targetToolkit = GetToolkitForWidget (target);
				screenLocation = targetToolkit.GetScreenBounds (target).Offset (targetRect.Location).Location; 
			}

			screenLocation = TranslateCoordinates (targetToolkit, Toolkit, screenLocation);
			return new Rectangle (screenLocation, targetRect.Size);
		}

		static Point TranslateCoordinates (Xwt.Toolkit sourceToolkit, Xwt.Toolkit targetToolkit, Point point)
		{
			if (sourceToolkit == targetToolkit)
				return point;
			sourceToolkit.Invoke (() => {
				var desktopBounds = Desktop.Bounds;
				point.X -= desktopBounds.X;
				point.Y -= desktopBounds.Y;
			});
			targetToolkit.Invoke (() => {
				var desktopBounds = Desktop.Bounds;
				point.X += desktopBounds.X;
				point.Y += desktopBounds.Y;
			});
			return point;
		}

		static Toolkit GetToolkitForWidget (object nativeWidget)
		{
			if (nativeWidget is Gtk.Widget)
				return Toolkit.Load (ToolkitType.Gtk);
			#if MAC
			if (nativeWidget is AppKit.NSView)
				return Toolkit.Load (ToolkitType.XamMac);
			#endif
			#if WIN32
			if (nativeWidget is System.Windows.FrameworkElement)
				return Toolkit.Load (ToolkitType.Wpf);
			#endif
			throw new NotSupportedException (string.Format ("Widget of type '{0}' does not belong to a supported Toolkit.", nativeWidget.GetType ()));
		}

		protected Size GetParentSize ()
		{
			var gtkTarget = target as Gtk.Widget;
			if (gtkTarget != null) {
				var alloc = gtkTarget.Allocation;
				return new Size (alloc.Width, alloc.Height);
			}
			#if MAC
			var nsTarget = target as AppKit.NSView;
			if (nsTarget != null) {
				var frame = nsTarget.Frame;
				return new Size (frame.Width, frame.Height);
			}
			#endif
			var xwtTarget = target as Widget;
			if (xwtTarget != null) {
				var size = xwtTarget.Size;
				return new Size ((int)size.Width, (int)size.Height);
			}
			return Size.Zero;
		}

		protected Rectangle GetUsableMonitorGeometry (Rectangle targetRect)
		{
			var rect = Rectangle.Zero;
			Toolkit.Invoke (() => {
				var screen = Desktop.GetScreenAtLocation (targetRect.Location);
				if (screen != null)
					rect = screen.VisibleBounds;
			});
			return rect;
		}

		Rectangle cachedBounds;
		bool repositioning;
		protected override void OnBoundsChanged (BoundsChangedEventArgs a)
		{
			base.OnBoundsChanged (a);
			if (!Visible || repositioning)
				return;
			if (cachedBounds.Size != Size) {
				repositioning = true;
				RepositionWindow ();
			}
			cachedBounds = a.Bounds;
			repositioning = false;
		}

		public virtual void RepositionWindow (Rectangle? newTargetRect = null)
		{
			if (!HasParent)
				return;

			if (newTargetRect.HasValue)
				targetRect = newTargetRect.Value;

			Rectangle currentRect = targetRect;
			PopupPosition position = targetPosition;
			CurrentPosition = targetPosition;

			var psize = GetParentSize ();

			if (eventProvided) {
				currentRect.X = targetWindowOrigin.X;
				currentRect.Y = targetWindowOrigin.Y;
				currentRect.Width = currentRect.Height = 1;
			} else {
				if (currentRect.IsEmpty)
					currentRect = new Rectangle (0, 0, psize.Width, psize.Height);
				currentRect = GetScreenCoordinates (currentRect);
			}

			currentRect.Inflate (RectOffset, RectOffset);
			if (!Core.Platform.IsWindows)
				currentRect.Inflate (-1, -1);

			var request = Content.Surface.GetPreferredSize (true) + new Size (Padding.HorizontalSpacing, Padding.VerticalSpacing);;
			
			Rectangle geometry = GetUsableMonitorGeometry (currentRect);

			if (!geometry.IsEmpty) {
				// Add some spacing between the screen border and the popover window
				geometry = geometry.Inflate (-5, -5);

				// Flip the orientation if the window doesn't fit the screen.

				int intPos = (int)position;
				switch ((PopupPosition)(intPos & 0x0f)) {
				case PopupPosition.Top:
					if (currentRect.Bottom + request.Height > geometry.Bottom)
						intPos = (intPos & 0xf0) | (int)PopupPosition.Bottom;
					break;
				case PopupPosition.Bottom:
					if (currentRect.Top - request.Height < geometry.Y)
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

			if ((position & PopupPosition.Top) != 0 || (position & PopupPosition.Bottom) != 0) {
				if (((int)position & 0x10) != 0)
					x = currentRect.X;
				else if (((int)position & 0x20) != 0)
					x = currentRect.Right - request.Width;
				else
					x = currentRect.X + (currentRect.Width - request.Width) / 2;

				if (x < geometry.Left)
					x = geometry.Left;
				else if (x + request.Width > geometry.Right)
					x = geometry.Right - request.Width;
			} else {
				if (((int)position & 0x10) != 0)
					y = currentRect.Y;
				else if (((int)position & 0x20) != 0)
					y = currentRect.Bottom - request.Height;
				else
					y = currentRect.Y + (currentRect.Height - request.Height) / 2;

				if (y < geometry.Top)
					y = geometry.Top;
				else if (y + request.Height > geometry.Bottom)
					y = geometry.Bottom - request.Height;
				if (MaximumYTopBound > 0)
					y = Math.Max (MaximumYTopBound, y);
			}

			Location = new Point (x, y);
		}

		public void Destroy ()
		{
			Dispose ();
		}
	}
}
