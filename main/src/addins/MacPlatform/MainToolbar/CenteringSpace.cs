//
// CenteringSpace.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	[Register]
	class CenteringSpaceView : NSView
	{
		internal CenteringSpaceToolbarItem toolbarItem;
		public override bool AcceptsFirstMouse (NSEvent theEvent)
		{
			return false;
		}

		public override void ViewDidMoveToWindow ()
		{
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResizeNotification, notif =>
				DispatchService.GuiDispatch (toolbarItem.UpdateWidth));
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.WillEnterFullScreenNotification, notif =>
				CenteringSpaceToolbarItem.WindowFullscreening = true);
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidEnterFullScreenNotification, notif => {
				CenteringSpaceToolbarItem.WindowFullscreening = false;
				DispatchService.GuiDispatch (toolbarItem.UpdateWidth);
			});

			base.ViewDidMoveToWindow ();
		}
	}

	[Register]
	class CenteringSpaceToolbarItem : NSToolbarItem
	{
		internal static bool WindowFullscreening;
		public CenteringSpaceToolbarItem ()
		{
			Initialize ();
		}

		public CenteringSpaceToolbarItem (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		public CenteringSpaceToolbarItem (string itemIdentifier) : base (itemIdentifier)
		{
			Initialize ();
		}

		void Initialize ()
		{
			Label = "";
			View = new CenteringSpaceView {
				toolbarItem = this,
				Frame = new CGRect (0, 0, 1, 1),
			};
		}

		public override CGSize MinSize {
			get {
				// Do NOT let this calculate any values while the window is fullscreening.
				// Everything changes, and the size might end up with bogus values and cause a native crash
				// that is totally unrelated. See BXC 29261.
				if (WindowFullscreening)
					return base.MinSize;

				NSToolbarItem[] items = Toolbar.Items;
				int index = Array.IndexOf (items, this);

				if (index != -1 && View.Superview != null) {
					CGRect frame = View.Superview.Frame;
					if (frame.Left > 0) {
						nfloat space = 0;
						// There is a next item.
						if (items.Length > index + 1) {
							NSView nextItem = items [index + 1].View.Superview;
							if (nextItem != null) {
								CGRect nextFrame = nextItem.Frame;
								CGRect toolbarFrame = nextItem.Superview.Frame;

								// nextFrame is in center of the toolbar.
								// so Left + space = toolbarFrame / 2 - nextFrame.Width / 2.
								space = (toolbarFrame.Width - nextFrame.Width) / 2 - frame.Left;
								if (space < 0)
									space = 0;
							}
						}

						base.MinSize = new CGSize (space, base.MinSize.Height);
						base.MaxSize = new CGSize (space, base.MaxSize.Height);
					}
				}
				return base.MinSize;
			}
			set { base.MinSize = value; }
		}

		internal void UpdateWidth ()
		{
			// Trigger updates.
			MinSize = MinSize;
		}
	}
}

