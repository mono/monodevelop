//
//
// Author:
//   Jose Medrano
//
//
// Copyright (C) 2918 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#if MAC

using System;
using AppKit;
using Gtk;

namespace MonoDevelop.Components.Docking
{
	internal class MacSplitterWidget : NSView
	{
		DockGroup dockGroup;
		int dockIndex;

		int dragPos, dragSize;
		bool hover, dragging;

		public MacSplitterWidget ()
		{
			const NSTrackingAreaOptions options = NSTrackingAreaOptions.ActiveInKeyWindow |
				NSTrackingAreaOptions.InVisibleRect |
				NSTrackingAreaOptions.MouseEnteredAndExited;
			var trackingArea = new NSTrackingArea (default, options, this, null);
			AddTrackingArea (trackingArea);
		}

		public override void MouseEntered (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			if (!dragging) {
				SetResizeCursor ();
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

		NSCursor currentCursor;

		void SetResizeCursor ()
		{
			if (dockGroup == null || currentCursor != null) {
				return;
			}
			if (dockGroup.Type == DockGroupType.Horizontal) {
				currentCursor = NSCursor.ResizeLeftRightCursor;
			} else {
				currentCursor = NSCursor.ResizeUpDownCursor;
			}
			currentCursor.Push ();
		}

		public override void MouseMoved (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			if (!dragging) {
				SetResizeCursor ();
			}
			base.MouseMoved (theEvent);
		}

		void RaiseEndDrag ()
		{
			if (dragging) {
				dragging = false;
				RemoveGdkEventFilter ();
			}
		}

		public void Init (DockGroup grp, int index)
		{
			dockGroup = grp;
			dockIndex = index;
		}

		public override void MouseDown (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			dragging = true;

			var point = NSEvent.CurrentMouseLocation;
			var obj = dockGroup.VisibleObjects [dockIndex];

			if (dockGroup.Type == DockGroupType.Horizontal) {
				dragPos = (int)point.X;
				dragSize = obj.Allocation.Width;
			} else {
				dragPos = (int)point.Y;
				dragSize = obj.Allocation.Height;
			}

			AddGdkEventFilter ();
		}

		void SetDefaultCursor ()
		{
			if (currentCursor != null) {
				currentCursor.Pop ();
				currentCursor = null;
			}
		}

		public override void MouseUp (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			if (hover) {
				SetResizeCursor ();
			} else {
				SetDefaultCursor ();
			}
			RaiseEndDrag ();
			base.MouseUp (theEvent);
		}

		public override void MouseDragged (NSEvent theEvent)
		{
			lastEventTimestamp = theEvent.Timestamp;
			SetResizeCursor ();

			var point = NSEvent.CurrentMouseLocation;
			int newpos;
			if (dockGroup.Type == DockGroupType.Horizontal) {
				newpos = (int)point.X;
			} else {
				newpos = (int)point.Y;
			}

			if (newpos != dragPos) {
				int nsize;
				if (dockGroup.Type == DockGroupType.Horizontal) {
					nsize = dragSize + (newpos - dragPos);
				} else {
					nsize = dragSize - (newpos - dragPos);
				}
				dockGroup.ResizeItem (dockIndex, nsize);
			}
		}

		double lastEventTimestamp = Foundation.NSProcessInfo.ProcessInfo.SystemUptime;
		bool enableGdkEventFiler;
		bool gdkEventFilterInserted;
		bool isDisposed;

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
