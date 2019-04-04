//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using AppKit;
using CoreGraphics;
using ObjCRuntime;

namespace Gtk
{
	public sealed class GtkNSViewHost : Widget
	{
		const string LIBGTKQUARTZ = "libgtk-quartz-2.0.dylib";

		[DllImport (LIBGTKQUARTZ)]
		static extern IntPtr gdk_quartz_window_get_nsview (IntPtr window);

		[DllImport (LIBGTKQUARTZ)]
		static extern IntPtr gdk_quartz_event_get_nsevent (IntPtr evnt);

		[DllImport (LIBGTKQUARTZ)]
		static extern void gdk_window_coords_to_parent (
			IntPtr window,
			double x,
			double y,
			out double parent_x,
			out double parent_y);

		[DllImport (LIBGTKQUARTZ)]
		static extern bool gdk_window_has_native (IntPtr window);

		static NSEvent GetNSEvent (Gdk.Event evnt)
		{
			if (evnt == null || evnt.Handle == IntPtr.Zero)
				return null;

			var nsEventHandle = gdk_quartz_event_get_nsevent (evnt.Handle);
			if (nsEventHandle == IntPtr.Zero)
				return null;

			return Runtime.GetNSObject<NSEvent> (nsEventHandle);
		}

		NSView view;
		NSView superview;

		public GtkNSViewHost (NSView view)
		{
			this.view = view ?? throw new ArgumentNullException (nameof (view));

			WidgetFlags |= WidgetFlags.NoWindow;
		}

		void UpdateViewFrame ()
		{
			LogEnter ();
			try {
				if (view == null)
					return;

				var window = GdkWindow;
				var allocation = Allocation;
				double x = allocation.X;
				double y = allocation.Y;

				while (window != null && !gdk_window_has_native (window.Handle)) {
					gdk_window_coords_to_parent (window.Handle, x, y, out var nx, out var ny);
					Log ($"({x},{y}) -> ({nx},{ny})");
					x = nx;
					y = ny;
					window = window.Parent;
				}

				view.Frame = new CGRect (x, y, allocation.Width, allocation.Height);
				Log ($"Frame: {view.Frame}");
			} finally {
				LogExit ();
			}
		}

		static NSView RecursivelyFindSubviewForPredicate (NSView view, Predicate<NSView> predicate)
		{
			if (view == null)
				return null;

			if (predicate (view))
				return view;

			var subviews = view.Subviews;
			if (subviews != null && subviews.Length > 0) {
				foreach (var subview in subviews) {
					if (subview != null) {
						var foundView = RecursivelyFindSubviewForPredicate (subview, predicate);
						if (foundView != null)
							return foundView;
					}
				}
			}

			return null;
		}

		NSView GetAcceptsFirstResponderView ()
			=> RecursivelyFindSubviewForPredicate (view, v => v.AcceptsFirstResponder ());

		protected override void OnDestroyed ()
		{
			LogEnter ();
			try {
				view?.RemoveFromSuperview ();
				view = null;
				superview = null;

				base.OnDestroyed ();
			} finally {
				LogExit ();
			}
		}

		protected override void OnRealized ()
		{
			LogEnter ();
			try {
				GdkWindow = Parent?.GdkWindow;

				if (GdkWindow != null && GdkWindow.Handle != IntPtr.Zero) {
					var superviewHandle = gdk_quartz_window_get_nsview (GdkWindow.Handle);
					if (superviewHandle != IntPtr.Zero)
						superview = Runtime.GetNSObject<NSView> (superviewHandle);
				}

				if (superview != null && view != null)
					superview.AddSubview (view);

				base.OnRealized ();

				UpdateViewFrame ();
				CanFocus = GetAcceptsFirstResponderView () != null;
			} finally {
				LogExit ();
			}
		}

		protected override void OnUnrealized ()
		{
			LogEnter ();
			try {
				Unmap ();

				view?.RemoveFromSuperview ();
				superview = null;

				base.OnUnrealized ();
			} finally {
				LogExit ();
			}
		}

		protected override void OnMapped ()
		{
			LogEnter ();
			try {
				if (view != null)
					view.Hidden = false;

				base.OnMapped ();

				UpdateViewFrame ();
			} finally {
				LogExit ();
			}
		}

		protected override void OnUnmapped ()
		{
			LogEnter ();
			try {
				if (view != null)
					view.Hidden = true;

				base.OnUnmapped ();

			} finally {
				LogExit ();
			}
		}

		protected override bool OnConfigureEvent (Gdk.EventConfigure evnt)
		{
			LogEnter ();
			try {
				return base.OnConfigureEvent (evnt);
			} finally {
				LogExit ();
			}
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			LogEnter ();
			try {
				if (view == null) {
					Log ("Calling base. 'view' is null");
					base.OnSizeRequested (ref requisition);
					return;
				}

				var fittingSize = view.FittingSize;
				requisition.Width = (int)fittingSize.Width;
				requisition.Height = (int)fittingSize.Height;
				Log ($"Setting requisition to {requisition.Width}x{requisition.Height}");
			} finally {
				LogExit ();
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			LogEnter ();
			try {
				base.OnSizeAllocated (allocation);

				UpdateViewFrame ();
			} finally {
				LogExit ();
			}
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			LogEnter ();
			try {
				var acceptsFirstResponderView = GetAcceptsFirstResponderView ();
				if (acceptsFirstResponderView == null) {
					Log ("neither view nor descendants accept first responder");
					return false;
				}

				if (acceptsFirstResponderView.Window == null) {
					Log ("first responder found, but it does not have a window");
					return false;
				}

				acceptsFirstResponderView.Window.MakeFirstResponder (acceptsFirstResponderView);

				UpdateViewFrame ();

				return base.OnFocusInEvent (evnt);
			} finally {
				LogExit ();
			}
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			LogEnter ();
			try {
				var firstResponder = view?.Window.FirstResponder as NSView;
				if (firstResponder != null && view?.AncestorSharedWithView (firstResponder) == view)
					firstResponder.Window.MakeFirstResponder (null);
				return base.OnFocusOutEvent (evnt);
			} finally {
				LogExit ();
			}
		}

		protected override bool OnWidgetEvent (Gdk.Event evnt)
		{
			LogEnter ();
			try {
				UpdateViewFrame ();
				return base.OnWidgetEvent (evnt);
			} finally {
				LogExit ();
			}
		}

		bool ForwardEvent<TEvent> (
			TEvent evnt,
			Action<NSView, NSEvent> forwardCall,
			Func<TEvent, bool> baseCall) where TEvent : Gdk.Event
		{
			var acceptsFirstResponderView = GetAcceptsFirstResponderView ();
			if (acceptsFirstResponderView == null)
				return false;

			var nsEvent = GetNSEvent (evnt);
			if (nsEvent == null)
				return false;

			forwardCall (acceptsFirstResponderView, nsEvent);

			return baseCall (evnt);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			LogEnter ();
			try {
				return ForwardEvent (
					evnt,
					(v, e) => v.KeyDown (e),
					base.OnKeyReleaseEvent);
			} finally {
				LogExit ();
			}
		}

		protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
		{
			LogEnter ();
			try {
				return ForwardEvent (
					evnt,
					(v, e) => v.KeyUp (e),
					base.OnKeyReleaseEvent);
			} finally {
				LogExit ();
			}
		}

		#region Tracing

		int traceDepth;
		int traceGeneration;

		[Conditional ("DEBUG")]
		void LogIndent ()
			=> Debug.Write ($"{traceGeneration:0000}|{new string (' ', traceDepth * 2)}");

		[Conditional ("DEBUG")]
		void Log (string message, [CallerMemberName] string memberName = null)
		{
			LogIndent ();
			Debug.WriteLine ($"{memberName}: {message}");
		}

		[Conditional ("DEBUG")]
		void LogEnter ([CallerMemberName] string memberName = null)
		{
			if (traceDepth == 0)
				traceGeneration++;
			LogIndent ();
			Debug.WriteLine ($"Enter: {memberName}");
			traceDepth++;
		}

		[Conditional ("DEBUG")]
		void LogExit ([CallerMemberName] string memberName = null)
		{
			traceDepth--;
			LogIndent ();
			Debug.WriteLine ($"Exit: {memberName}");
		}

		#endregion
	}
}
#endif