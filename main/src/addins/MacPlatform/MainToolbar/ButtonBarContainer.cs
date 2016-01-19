//
// ButtonBarContainer.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.Collections.Generic;
using AppKit;
using CoreGraphics;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	public class ButtonBarContainer : NSView
	{
		internal event EventHandler<EventArgs> SizeChanged;

		List<ButtonBar> buttonBars;
		internal List<ButtonBar> ButtonBars {
			get {
				return buttonBars;
			}

			set {
				if (buttonBars != null) {
					foreach (var bar in buttonBars) {
						bar.ResizeRequested -= ResizeRequested;
						bar.RemoveFromSuperview ();
					}
				}

				buttonBars = value;
				foreach (var bar in buttonBars) {
					bar.ResizeRequested += ResizeRequested;
					AddSubview (bar);
				}

				LayoutButtonBars ();
			}
		}

		public ButtonBarContainer ()
		{
		}

		const float segmentWidth = 33.0f;
		const float buttonBarSpacing = 8.0f;
		const float extraPadding = 6.0f;

		void ResizeRequested (object sender, EventArgs e)
		{
			LayoutButtonBars ();
		}

		void LayoutButtonBars ()
		{
			nfloat nextX = 0;

			foreach (ButtonBar bar in buttonBars) {
				var frame = new CGRect (nextX, 0, extraPadding + (bar.SegmentCount * segmentWidth), AwesomeBar.ToolbarWidgetHeight);
				bar.Frame = frame;

				nextX = frame.GetMaxX () + buttonBarSpacing;
			}

			SetFrameSize (new CGSize (nextX - buttonBarSpacing, AwesomeBar.ToolbarWidgetHeight));

			if (SizeChanged != null) {
				SizeChanged (this, EventArgs.Empty);
			}
		}
	}
}

