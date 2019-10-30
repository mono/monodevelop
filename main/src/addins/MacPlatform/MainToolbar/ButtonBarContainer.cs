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
using System.Linq;
using AppKit;
using CoreGraphics;
using MonoDevelop.Ide;
using MonoDevelop.Components.Mac;

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

		public override void KeyDown (NSEvent theEvent)
		{
			if (theEvent.KeyCode == (ushort)KeyCodes.Tab) {
				if (theEvent.ModifierFlags == (NSEventModifierMask)KeyModifierFlag.None) {
					if (buttonBars.Count > 0) {
						var success = buttonBars[0].IncreaseFocusIndex ();
						if (success) return;
					}
				} else if (theEvent.ModifierFlags == (NSEventModifierMask)KeyModifierFlag.Shift) {
					if (buttonBars.Count > 0) {
						var success = buttonBars[0].DecreaseFocusIndex ();
						if (success) return;
					}
				}
			} else if (theEvent.KeyCode == (ushort)KeyCodes.Space || theEvent.KeyCode == (ushort)KeyCodes.Enter) {
					if (buttonBars.Count > 0) {
						var buttonBar = buttonBars[0];
						buttonBar.ExecuteFocused ();
					}
			}

		   	base.KeyDown (theEvent);
		}
		
		public override bool AcceptsFirstResponder () => buttonBars.Any ();

		public override bool BecomeFirstResponder ()
		{
			if (buttonBars.Any ())
				buttonBars.FirstOrDefault ().HasFocus = true;
			else
				Window.MakeFirstResponder (NextKeyView);
			return base.BecomeFirstResponder ();
		}

		public override bool ResignFirstResponder ()
		{
			if (buttonBars.Any ()) 
				buttonBars.FirstOrDefault ().HasFocus = false;
			return base.ResignFirstResponder ();
		}  

		public ButtonBarContainer ()
		{
			Ide.Gui.Styles.Changed += (o, e) => LayoutButtonBars ();
		}

		internal const float SegmentWidth = 33.0f;
		const float buttonBarSpacing = 8.0f;
		const float extraPadding = 6.0f;

		void ResizeRequested (object sender, EventArgs e)
		{
			LayoutButtonBars ();
		}

		void LayoutButtonBars ()
		{
			nfloat nextX = 0;
			nfloat y = 0;
			nfloat height = AwesomeBar.ToolbarWidgetHeight;

			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark) {
				y = 2;
				height += 2;
			} else {
				height += 5;
				y = -1;
			}

			foreach (ButtonBar bar in buttonBars) {
				var frame = new CGRect (nextX, y, extraPadding + (bar.SegmentCount * SegmentWidth), height);
				bar.Frame = frame;

#pragma warning disable EPS06 // Hidden struct copy operation
				nextX = frame.GetMaxX () + buttonBarSpacing;
#pragma warning restore EPS06 // Hidden struct copy operation
			}

			SetFrameSize (new CGSize (nextX - buttonBarSpacing, height));

			if (SizeChanged != null) {
				SizeChanged (this, EventArgs.Empty);
			}
		}
	}
}

