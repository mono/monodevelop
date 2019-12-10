//
// MacDebuggerTooltipWindow.cs
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

namespace MonoDevelop.Debugger
{
	sealed class MacDebuggerTooltipWindow : NSPopover
	{
		readonly ObjectValueTreeViewController controller;
		readonly NSLayoutConstraint heightConstraint;
		readonly NSLayoutConstraint widthConstraint;
		readonly MacObjectValueTreeView treeView;
		readonly NSScrollView scrollView;
		bool disposed;

		public MacDebuggerTooltipWindow (PinnedWatchLocation location, StackFrame frame, ObjectValue value, PinnedWatch watch)
		{
			Animates = false;
			Behavior = NSPopoverBehavior.Semitransient;

			controller = new ObjectValueTreeViewController ();
			controller.SetStackFrame (frame);
			controller.AllowEditing = true;
			controller.PinnedWatch = watch;
			controller.PinnedWatchLocation = location;

			treeView = controller.GetMacControl (ObjectValueTreeViewFlags.TooltipFlags);
			treeView.UIElementName = "Tooltip";
			treeView.NodePinned += OnPinStatusChanged;
			treeView.StartEditing += OnStartEditing;
			treeView.EndEditing += OnEndEditing;
			controller.AddValue (value);

			scrollView = new NSScrollView {
				HasVerticalScroller = true,
				AutohidesScrollers = true,
				DocumentView = treeView,
				Frame = treeView.Frame
			};

			ContentViewController = new NSViewController {
				View = scrollView
			};

			widthConstraint = scrollView.WidthAnchor.ConstraintEqualToConstant (treeView.Frame.Width);
			widthConstraint.Active = true;

			heightConstraint = scrollView.HeightAnchor.ConstraintEqualToConstant (treeView.Frame.Height);
			heightConstraint.Active = true;

			treeView.Resized += OnTreeViewResized;
		}

		public void Expand ()
		{
			treeView.ExpandItem (treeView.ItemAtRow (0), false);
		}

		public DebuggerSession GetDebuggerSession ()
		{
			return controller.GetStackFrame ()?.DebuggerSession;
		}

		static nfloat GetMaxHeight (NSWindow window)
		{
			var visibleFrame = window.Screen.VisibleFrame;

			// Note: You would think that we could make use of the full VisualFrame height,
			// but macOS will not actually make our tooltip window that large no matter
			// what.
			//
			// The downside of *trying* to use the full VisualFrame height is that the
			// scrollView will think that it is that tall when it in fact is not, thereby
			// making it impossible to scroll all the way to the top (and/or, potentially,
			// the bottom).
			//
			// On my machine, the VisualFrame height is 972 (Frame height is 1050 with a
			// menubar 23 pixels tall and a dock that is 55 pixels tall).
			//
			// macOS does not seem to allow the tooltip window to get larger than 943 pixels
			// which is 29 pixels shorter than the VisualFrame height. Let's just round that
			// up to 30 pixels.

			return visibleFrame.Height - 30;
		}

		void OnTreeViewResized (object sender, EventArgs e)
		{
			var height = (treeView.RowHeight + treeView.IntercellSpacing.Height) * treeView.RowCount;
			var maxHeight = GetMaxHeight (treeView.Window);

			height = NMath.Min (height, maxHeight);

			widthConstraint.Constant = treeView.OptimalTooltipWidth;
			heightConstraint.Constant = height;
		}

		void OnPinStatusChanged (object sender, EventArgs args)
		{
			Close ();
		}

		void OnStartEditing (object sender, EventArgs args)
		{
			//Modal = true;
			//PresentViewControllerAsModalWindow (this);
		}

		void OnEndEditing (object sender, EventArgs args)
		{
			//Modal = false;
		}

		void PreviewWindowManager_WindowClosed (object sender, EventArgs e)
		{
			// When Preview window is closed we want to put focus(IsActive=true) back on DebugValueWindow
			// otherwise CommandManager will think IDE doesn't have any window Active/Focused and think
			// user switched to another app and DebugValueWindow will closed itself on "FocusOut" event
			//Present ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				//PreviewWindowManager.WindowClosed -= PreviewWindowManager_WindowClosed;
				treeView.Resized -= OnTreeViewResized;
				treeView.NodePinned -= OnPinStatusChanged;
				treeView.StartEditing -= OnStartEditing;
				treeView.EndEditing -= OnEndEditing;
				heightConstraint.Dispose ();
				widthConstraint.Dispose ();
			}

			base.Dispose (disposing);
		}
	}
}
