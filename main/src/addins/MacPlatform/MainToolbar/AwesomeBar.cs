//
// AwesomeBar.cs
//
// Author:
//       iain <iain@xamarin.com>
//
// Copyright (c) 2015 Copyright © 2015 Xamarin, Inc
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
using Foundation;
using CoreGraphics;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	public class AwesomeBar : NSView
	{
		internal RunButton RunButton { get; set; }
		internal SelectorView SelectorView { get; set; }
		internal StatusBar StatusBar { get; set; }
		internal SearchBar SearchBar { get; set; }
		internal ButtonBarContainer ButtonBarContainer { get; private set; }

		public AwesomeBar ()
		{
			RunButton = new RunButton ();
			AddSubview (RunButton);

			SelectorView = new SelectorView ();
			SelectorView.SizeChanged += (object sender, EventArgs e) => UpdateLayout ();
			AddSubview (SelectorView);

			ButtonBarContainer = new ButtonBarContainer ();
			ButtonBarContainer.SizeChanged += (object sender, EventArgs e) => UpdateLayout ();
			AddSubview (ButtonBarContainer);

			StatusBar = new StatusBar ();
			AddSubview (StatusBar);

			SearchBar = new SearchBar ();
			AddSubview (SearchBar);
		}

		const float toolbarPadding = 8.0f;
		const float maxSearchBarWidth = 270.0f;
		const float minSearchBarWidth = 150.0f;
		const float maxStatusBarWidth = 700.0f;
		const float minStatusBarWidth = 220.0f;
		const float runButtonWidth = 38.0f;
		public const float ToolbarWidgetHeight = 25.0f;

		void UpdateLayout ()
		{
			RunButton.Frame = new CGRect (toolbarPadding, 0, runButtonWidth, ToolbarWidgetHeight);
			var statusbarWidth = Math.Round (Math.Max (Math.Min (Frame.Width * 0.3, maxStatusBarWidth), minStatusBarWidth));
			var searchbarWidth = maxSearchBarWidth;
			if (statusbarWidth < searchbarWidth) {
				searchbarWidth = minSearchBarWidth;
			}

			// We only need to work out the width on the left side of the window because the statusbar is centred
			// Gap + RunButton.Width + Gap + ButtonBar.Width + Gap + Half of StatusBar.Width
			var spaceLeft = (Frame.Width / 2) - (toolbarPadding + runButtonWidth + toolbarPadding + ButtonBarContainer.Frame.Width + toolbarPadding + (statusbarWidth / 2));
			StatusBar.Frame = new CGRect ((Frame.Width - statusbarWidth) / 2 + 0.5, 0, statusbarWidth, ToolbarWidgetHeight);
			SearchBar.Frame = new CGRect (Frame.Width - searchbarWidth - 10, 0, searchbarWidth, ToolbarWidgetHeight);

			var selectorSize = SelectorView.SizeThatFits (new CGSize (spaceLeft, ToolbarWidgetHeight));

			SelectorView.Frame = new CGRect (toolbarPadding + runButtonWidth + toolbarPadding, 0, selectorSize.Width, ToolbarWidgetHeight);

			ButtonBarContainer.SetFrameOrigin (new CGPoint(SelectorView.Frame.GetMaxX () + toolbarPadding, 0));

			// Finally check if the StatusBar overlaps the ButtonBarContainer (and its padding) and adjust is accordingly
			if (StatusBar.Frame.IntersectsWith (ButtonBarContainer.Frame.Inset (-toolbarPadding, 0))) {
				StatusBar.SetFrameOrigin (new CGPoint (ButtonBarContainer.Frame.GetMaxX () + toolbarPadding, StatusBar.Frame.Y));
			}
		}

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);

			var locationInSV = Superview.ConvertPointFromView (theEvent.LocationInWindow, null);
			if (theEvent.ClickCount == 2 && HitTest (locationInSV) == this) {
				Window.Zoom (this);
			}
		}

		public override CGRect Frame {
			get {
				return base.Frame;
			}
			set {
				base.Frame = value;
				UpdateLayout ();
			}
		}
    }
}

