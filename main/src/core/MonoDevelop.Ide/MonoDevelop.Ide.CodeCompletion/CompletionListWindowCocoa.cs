//
// CompletionListWindowCocoa.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2018 
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
using Gtk;
using Xwt;
using CoreGraphics;
using MonoDevelop.Components;
using Foundation;

namespace MonoDevelop.Ide.CodeCompletion
{
	internal class CompletionListWindowCocoa : NSViewController, ICompletionView
	{
		NSWindow parentWindow;
		WindowTransparencyDecorator decorator;

		ICompletionViewEventSink eventSink;

		public CompletionListWindowCocoa ()
		{
			parentWindow = NSWindow.GetWindowWithContentViewController (this);

			parentWindow.StyleMask = NSWindowStyle.Borderless | NSWindowStyle.FullSizeContentView;
			parentWindow.TitleVisibility = NSWindowTitleVisibility.Hidden;
			parentWindow.TitlebarAppearsTransparent = true;
		}

		CompletionListCocoa listView;

		public override void LoadView ()
		{
			View = new NSView ();

			View.Frame = new CGRect (0, 0, 400, 400);
			View.TranslatesAutoresizingMaskIntoConstraints = false;
			View.WidthAnchor.ConstraintEqualToConstant (400).Active = true;
			View.HeightAnchor.ConstraintEqualToConstant (400).Active = true;

			var scroller = new NSScrollView (new CGRect (0, 0, 400, 400));
			scroller.HasVerticalScroller = true;

			View.AddSubview (scroller);

			scroller.ContentView.PostsBoundsChangedNotifications = true;
			NSNotificationCenter.DefaultCenter.AddObserver ((NSString)"NSViewBoundsDidChangeNotification", (n) => {
				eventSink.OnListScrolled ();
			}, scroller.ContentView);

			listView = new CompletionListCocoa ();
			listView.Frame = new CGRect (0, 0, 400, 400);
			listView.SelectionChanged += OnSelectionChanged;

			scroller.DocumentView = listView;
		}

		public void ShowFilteredItems (CompletionListFilterResult filterResult)
		{
			View.Frame = new CGRect (0, 0, 400, 400);
			listView.ShowFilteredItems (filterResult);
		}

		public void ResetState ()
		{
			listView.ResetState ();
		}

		public int SelectedItemIndex {
			get => listView.SelectedIndex;
			set { Console.WriteLine ($"Setting index: {value}"); listView.SelectedIndex = value; }
		}
		public bool InCategoryMode { get => true; set { return; } }
		public bool SelectionEnabled { get => true; set { return; } }

		public bool Visible => parentWindow.IsVisible;

		public Rectangle Allocation => new Rectangle (parentWindow.Frame.X, parentWindow.Frame.Y, parentWindow.Frame.Width, parentWindow.Frame.Height);

		public int X => (int)parentWindow.Frame.X;

		public int Y => (int)parentWindow.Frame.Y;

		public Gtk.Window TransientFor {
			get => null;
			set {
				return;
			}
		}

		public void Destroy ()
		{
			parentWindow.Close ();
		}

		public void Hide ()
		{
			decorator.Detach ();
			parentWindow.OrderOut (this);
		}

		public void HideLoadingMessage ()
		{
		}

		public void Initialize (IListDataProvider provider, ICompletionViewEventSink eventSink)
		{
			this.eventSink = eventSink;
			listView.DataProvider = provider;
		}

		public void MoveCursor (int relative)
		{
			if (relative + listView.SelectedRow < 0) {
				return;
			}

			var newRow = listView.SelectedRow + relative;
			listView.SelectRow (newRow, false);
			listView.ScrollRowToVisible (newRow);
		}

		public void PageDown ()
		{
			MoveCursor (-8);
		}

		public void PageUp ()
		{
			MoveCursor (8);
		}

		public void Reposition (int triggerX, int triggerY, int triggerHeight, bool force)
		{
			var scr = parentWindow.Screen;
			// Convert to Cocoa Y
			var halfCocoaHeight = scr.Frame.Height / 2.0;
			var dy = halfCocoaHeight - (triggerY + triggerHeight);

			// FIXME: Proper screen calculation
			//triggerX -= (int)scr.Frame.Width;
			parentWindow.SetFrameTopLeftPoint (new CGPoint (scr.Frame.X + triggerX, scr.Frame.Y + (halfCocoaHeight + dy)));
		}

		public bool RepositionDeclarationViewWindow (TooltipInformationWindow declarationviewwindow, int selectedItem)
		{
			if (listView == null) {
				return false;
			}
			var rect = listView.RectForRow (listView.SelectedRow);
			var offset = ((NSClipView)listView.Superview).DocumentVisibleRect ().Y;
			var y = rect.Y - offset;
			if (y < 0 || y + rect.Height > View.Window.Frame.Height) {
				return false;
			}
			declarationviewwindow.ShowPopup (View, new Rectangle(0, (int)y, (int)rect.Width, (int)rect.Height), PopupPosition.Left);
			return true;
		}

		public void RepositionWindow (Rectangle? r)
		{
			if (!r.HasValue) {
				return;
			}

			parentWindow.SetFrameTopLeftPoint (new CGPoint (r.Value.X, r.Value.Y));
		}

		public void ResetSizes ()
		{
		}

		public void Show ()
		{
			decorator = new WindowTransparencyDecorator (parentWindow);
			parentWindow.OrderFront (this);
		}

		public void ShowLoadingMessage ()
		{
		}

		public void ShowPreviewCompletionEntry ()
		{
		}

		void OnSelectionChanged (object sender, EventArgs args)
		{
			eventSink.OnSelectedItemChanged ();
		}
	}
}

#endif