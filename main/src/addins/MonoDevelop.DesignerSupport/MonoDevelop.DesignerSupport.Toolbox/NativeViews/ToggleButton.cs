//
// ToggleButton.cs
//
// Author:
//       josemedranojimenez <jose.medrano@xamarin.com>
//
// Copyright (c) 2018 (c) Jose Medrano
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

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ToggleButton : NSButton
	{
	
		public ToggleButton () 
		{
			BezelStyle = NSBezelStyle.TexturedSquare;
			SetButtonType (NSButtonType.OnOff);
			Bordered = false;
			Title = "";
			WantsLayer = true;
			Layer.BackgroundColor = NSColor.Clear.CGColor;
			FocusRingType = NSFocusRingType.None;
		}

		NSTrackingArea trackingArea;
		public override void UpdateTrackingAreas ()
		{
			base.UpdateTrackingAreas ();
			if (trackingArea != null) {
				RemoveTrackingArea (trackingArea);
				trackingArea.Dispose ();
			}
			var viewBounds = Bounds;
			var options = NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.MouseEnteredAndExited;
			trackingArea = new NSTrackingArea (viewBounds, options, this, null);
			AddTrackingArea (trackingArea);
		}

		bool isMouseHover, isFirstResponder;
		public override void MouseEntered (NSEvent theEvent)
		{
			base.MouseEntered (theEvent);
			isMouseHover = true;
			NeedsDisplay = true;
		}

		public override void MouseExited (NSEvent theEvent)
		{
			base.MouseExited (theEvent);
			isMouseHover = false;
			NeedsDisplay = true;
		}

		public override bool BecomeFirstResponder ()
		{
			isFirstResponder = true;
			NeedsDisplay = true;
			return base.BecomeFirstResponder ();
		}

		public override bool ResignFirstResponder ()
		{
			isFirstResponder = false;
			NeedsDisplay = true;
			return base.ResignFirstResponder ();
		}

		public override CGSize IntrinsicContentSize => new CGSize (26, 26);

		public bool Visible {
			get { return !Hidden; }
			set {
				Hidden = !value;
			}
		}

		public bool Active {
			get => State == NSCellStateValue.On;
			set {
				State = value ? NSCellStateValue.On : NSCellStateValue.Off;
			}
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			if (isMouseHover || isFirstResponder || State == NSCellStateValue.On) {
				var path = NSBezierPath.FromRoundedRect (dirtyRect, Styles.ToggleButtonCornerRadius, Styles.ToggleButtonCornerRadius);
				path.ClosePath ();
				path.LineWidth = Styles.ToggleButtonLineWidth;
				if (State == NSCellStateValue.On) {
					Styles.ToggleButtonHoverClickedBackgroundColor.Set ();
				} else {
					Styles.ToggleButtonHoverBackgroundColor.Set ();
				}

				path.Fill ();
				Styles.ToggleButtonHoverBorderColor.Set ();
				path.Stroke ();
			} else {
				NSColor.Clear.Set ();
				NSBezierPath.FillRect (Bounds);
			}

			if (Image != null) {
				var startX = (Frame.Width - Image.Size.Width) / 2;
				var startY = (Frame.Width - Image.Size.Width) / 2;
				var context = NSGraphicsContext.CurrentContext;
				context.SaveGraphicsState ();
				Image.Draw (new CGRect (startX, startY, Image.Size.Width, Image.Size.Height));
				context.RestoreGraphicsState ();
			}
		}
	}
}
