//
// PointerEventTrapView.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2019 
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

#if MAC

using System;
using AppKit;

namespace MonoDevelop.Components.Mac
{
	/// <summary>
	/// DragEventTrapView base class to disable GDK events while an NSView is being dragged
	/// </summary>
	/// <remarks>
	/// This NSView tracks pointer events and disables GDK events during a dragging operation.
	/// This allows the cursor to shortly leave the view bounds and prevents losing of events
	/// if the pointer enters a different view or Gtk widget shortly.
	///
	/// The main purpose is to support native views inside splitters without losing splitter
	/// drag support. See <see cref="Docking.MacSplitterWidget"/> and <see cref="HPanedThin"/> for examples.
	///
	/// However DragEventTrapView does not handle the actual drag operation, so it could
	/// be used for other use cases, where GDK events or other NSViews might interfere and
	/// steal pointer events.
	///
	/// NOTE: this view should be added with NSWindowOrderingMode.Above to its superview, to
	///       make sure the it's the first widget to receive pointer events.
	/// </remarks>
	abstract class DragEventTrapView : NSView
	{
		NSCursor currentCursor;
		bool hover, dragging;

		double lastEventTimestamp = Foundation.NSProcessInfo.ProcessInfo.SystemUptime;
		bool enableGdkEventFiler;
		bool gdkEventFilterInserted;
		bool isDisposed;

		protected DragEventTrapView ()
		{
			const NSTrackingAreaOptions options = NSTrackingAreaOptions.ActiveInKeyWindow |
				NSTrackingAreaOptions.InVisibleRect |
				NSTrackingAreaOptions.MouseEnteredAndExited;
			var trackingArea = new NSTrackingArea (default, options, this, null);
			AddTrackingArea (trackingArea);
		}

		/// <summary>
		/// Override to supply a custom NSCursor during a drag operation
		/// </summary>
		/// <returns>NSCursor to be shown while dragging, or null to disable cursor changes</returns>
		protected virtual NSCursor GetDragCursor ()
		{
			return null;
		}

		void SetDefaultCursor ()
		{
			if (currentCursor != null) {
				currentCursor.Pop ();
				currentCursor = null;
			}
		}

		void SetDragCursor ()
		{
			var cursor = GetDragCursor ();
			if (currentCursor != null) {
				if (currentCursor == cursor)
					return;
				currentCursor.Pop ();
			}
			currentCursor = cursor;
			currentCursor.Push ();
		}

		public override void MouseEntered (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			if (!dragging) {
				SetDragCursor ();
			}
			hover = true;
			base.MouseEntered (theEvent);
		}

		public override void MouseExited (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			if (!dragging) {
				SetDefaultCursor ();
			}
			hover = false;
			base.MouseExited (theEvent);
		}

		public override void MouseMoved (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			if (!dragging) {
				SetDragCursor ();
			}
			base.MouseMoved (theEvent);
		}

		public override void MouseDown (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			dragging = true;

			AddGdkEventFilter ();
		}

		public override void MouseUp (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			if (hover) {
				SetDragCursor ();
			} else {
				SetDefaultCursor ();
			}
			RaiseEndDrag ();
			base.MouseUp (theEvent);
		}

		public override void MouseDragged (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			SetDragCursor ();
		}

		void RaiseEndDrag ()
		{
			if (dragging) {
				dragging = false;
				RemoveGdkEventFilter ();
			}
		}

		public override void RemoveFromSuperview ()
		{
			RemoveGdkEventFilter ();
			base.RemoveFromSuperview ();
		}

		protected override void Dispose (bool disposing)
		{
			isDisposed = true;
			if (disposing)
				RemoveGdkEventFilter ();
			base.Dispose (disposing);
		}

		void AddGdkEventFilter ()
		{
			enableGdkEventFiler = true;
			if (!gdkEventFilterInserted) {
				Gdk.Window.AddFilterForAll (Filter);
				gdkEventFilterInserted = true;
			}
		}

		void RemoveGdkEventFilter ()
		{
			enableGdkEventFiler = false;
			if (gdkEventFilterInserted) {
				Gdk.Window.RemoveFilterForAll (Filter);
				gdkEventFilterInserted = false;
			}
		}

		Gdk.FilterReturn Filter (IntPtr xevent, Gdk.Event evnt)
		{
			if (enableGdkEventFiler && !isDisposed && Window != null) {
				// always recover GDK events after 2 seconds of inactivity, this makes sure
				// that we don't lose GDK events forever if some other native view steals events
				// which are required for the drag exit condition.
				if (Foundation.NSProcessInfo.ProcessInfo.SystemUptime - lastEventTimestamp < 2.0)
					return Gdk.FilterReturn.Remove;
			}
			RemoveGdkEventFilter ();
			return Gdk.FilterReturn.Continue;
		}
	}
}

#endif
