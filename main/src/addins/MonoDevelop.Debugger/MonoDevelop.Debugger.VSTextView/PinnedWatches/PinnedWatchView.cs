//
// MacPinnedWatchView.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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

using AppKit;

using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.VSTextView.PinnedWatches
{
	sealed class PinnedWatchView : NSView
	{
		readonly PinnedWatch watch;
		readonly StackFrame frame;
		readonly ObjectValueTreeViewController controller;
		readonly NSLayoutConstraint heightConstraint;
		readonly NSLayoutConstraint widthConstraint;
		readonly MacObjectValueTreeView treeView;
		MacDebuggerTooltipWindow popover;
		NSLayoutConstraint superHeightConstraint;
		NSLayoutConstraint superWidthConstraint;
		ObjectValue objectValue;
		bool disposed;

		public PinnedWatchView (PinnedWatch watch, StackFrame frame)
		{
			this.watch = watch ?? throw new ArgumentNullException (nameof (watch));
			this.frame = frame;

			controller = new ObjectValueTreeViewController ();
			controller.SetStackFrame (frame);
			controller.AllowEditing = true;
			controller.AllowExpanding = false;

			treeView = controller.GetMacControl (ObjectValueTreeViewFlags.PinnedWatchFlags);
			treeView.UIElementName = "PinnedWatch";
			controller.PinnedWatch = watch;

			if (watch.Value != null)
				controller.AddValue (objectValue = watch.Value);

			var rect = treeView.Frame;

			if (rect.Height < 1)
				treeView.Frame = new CoreGraphics.CGRect (rect.X, rect.Y, rect.Width, 19);

			AddSubview (treeView);
			Frame = treeView.Frame;

			heightConstraint = HeightAnchor.ConstraintEqualToConstant (treeView.Frame.Height);
			heightConstraint.Active = true;

			widthConstraint = WidthAnchor.ConstraintEqualToConstant (treeView.Frame.Width);
			widthConstraint.Active = true;

			DebuggingService.ResumedEvent += OnDebuggerResumed;
			DebuggingService.PausedEvent += OnDebuggerPaused;
			treeView.Resized += OnTreeViewResized;

			AddTrackingArea (new NSTrackingArea (
				default,
				NSTrackingAreaOptions.ActiveInActiveApp |
				NSTrackingAreaOptions.InVisibleRect |
				NSTrackingAreaOptions.MouseEnteredAndExited,
				this,
				null));
		}

		public override void MouseEntered (NSEvent theEvent)
		{
			if (popover != null && popover.Shown)
				return;

			if (objectValue != null && objectValue.HasChildren) {
				if (popover == null)
					popover = new MacDebuggerTooltipWindow (watch.Location, frame, objectValue, watch);
				popover.Show (treeView.Frame, this, NSRectEdge.MaxXEdge);
				popover.Expand ();
			}
		}

		public void Refresh()
		{
			controller.Root.Children [0].Refresh ();
		}

		public void SetObjectValue (ObjectValue value)
		{
			if (value == objectValue)
				return;

			controller.ClearAll ();

			if (value != null)
				controller.AddValue (value);

			objectValue = value;
		}

		public override void ViewDidMoveToSuperview ()
		{
			base.ViewDidMoveToSuperview ();

			if (Superview != null) {
				superHeightConstraint = Superview.HeightAnchor.ConstraintEqualToConstant (Frame.Height);
				superWidthConstraint = Superview.WidthAnchor.ConstraintEqualToConstant (Frame.Width);
				superHeightConstraint.Active = true;
				superWidthConstraint.Active = true;
			} else {
				superHeightConstraint?.Dispose ();
				superWidthConstraint?.Dispose ();
				superHeightConstraint = null;
				superWidthConstraint = null;
			}
		}

		void OnTreeViewResized (object sender, EventArgs e)
		{
			//const string CocoaTextViewScrollView = "CocoaTextViewScrollView";
			const string CocoaTextViewControl = "CocoaTextViewControl";
			//const string CocoaEditorGridView = "CocoaEditorGridView";

			var materialView = Superview;

			// Find our parent CocoaTextViewControl
			var textView = materialView;
			while (textView != null && textView.GetType ().Name != CocoaTextViewControl)
				textView = textView.Superview;

			if (textView == null)
				return;

			var origin = textView.ConvertPointFromView (Frame.Location, this);
			var maxHeight = NMath.Max (textView.Frame.Bottom - origin.Y, treeView.RowHeight * 2);
			var height = (treeView.RowHeight + treeView.IntercellSpacing.Height) * treeView.RowCount;
			var width = treeView.OptimalTooltipWidth;

			height = NMath.Min (height, maxHeight);

			heightConstraint.Constant = height;
			widthConstraint.Constant = width;

			superHeightConstraint.Constant = height;
			superWidthConstraint.Constant = width;
		}

		void OnDebuggerResumed (object sender, EventArgs e)
		{
			controller.ChangeCheckpoint ();
			controller.AllowEditing = false;
		}

		void OnDebuggerPaused (object sender, EventArgs e)
		{
			controller.AllowEditing = true;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				DebuggingService.ResumedEvent -= OnDebuggerResumed;
				DebuggingService.PausedEvent -= OnDebuggerPaused;
				treeView.Resized -= OnTreeViewResized;
				superHeightConstraint?.Dispose ();
				superWidthConstraint?.Dispose ();
				superHeightConstraint = null;
				superWidthConstraint = null;
				heightConstraint.Dispose ();
				widthConstraint.Dispose ();
				disposed = true;
			}

			base.Dispose (disposing);
		}
	}
}
