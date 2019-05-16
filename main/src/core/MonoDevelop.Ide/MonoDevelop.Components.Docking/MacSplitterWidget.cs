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
using MonoDevelop.Components.Mac;

namespace MonoDevelop.Components.Docking
{
	internal class MacSplitterWidget : NSView
	{
		DockGroup dockGroup;
		int dockIndex;
		Widget parent;

		int dragPos, dragSize;
		bool hover, dragging;

		public MacSplitterWidget ()
		{
			var options = NSTrackingAreaOptions.ActiveInKeyWindow |
				NSTrackingAreaOptions.InVisibleRect |
				NSTrackingAreaOptions.MouseEnteredAndExited |
				NSTrackingAreaOptions.EnabledDuringMouseDrag;
			var trackingArea = new NSTrackingArea (default, options, this, null);
			AddTrackingArea (trackingArea);
		}

		public override void MouseEntered (NSEvent theEvent)
		{
			if (!dragging) {
				SetResizeCursor ();
			}
			hover = true;
			base.MouseEntered (theEvent);
		}

		public override void MouseExited (NSEvent theEvent)
		{
			if (!dragging) {
				NSCursor.ArrowCursor.Set ();
			}
			hover = false;
			base.MouseExited (theEvent);
		}

		void SetResizeCursor ()
		{
			if (dockGroup == null) {
				return;
			}
			if (dockGroup.Type == DockGroupType.Horizontal) {
				NSCursor.ResizeLeftRightCursor.Set ();
			} else {
				NSCursor.ResizeUpDownCursor.Set ();
			}
		}

		public override void MouseMoved (NSEvent theEvent)
		{
			if (!dragging) {
				SetResizeCursor ();
			}
			base.MouseMoved (theEvent);
		}

		void RaiseEndDrag ()
		{
			if (dragging) {
				dragging = false;
				AddRemoveFilter (false);
			}
		}

		public void Init (DockGroup grp, int index)
		{
			dockGroup = grp;
			dockIndex = index;
		}

		public override void MouseDown (NSEvent theEvent)
		{
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

			AddRemoveFilter (true);
		}

		public override void MouseUp (NSEvent theEvent)
		{
			if (hover) {
				SetResizeCursor ();
			} else {
				NSCursor.ArrowCursor.Set ();
			}
			RaiseEndDrag ();
			base.MouseUp (theEvent);
		}

		public override void MouseDragged (NSEvent theEvent)
		{
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

		void AddRemoveFilter (bool enable)
		{
			if (enable)
				Gdk.Window.AddFilterForAll (Filter);
			else
				Gdk.Window.RemoveFilterForAll (Filter);
		}
		static Gdk.FilterReturn Filter (IntPtr xevent, Gdk.Event evnt) => Gdk.FilterReturn.Remove;

	}

}

#endif