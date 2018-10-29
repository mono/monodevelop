/* 
/* 
 * ExpanderButton.cs - A click button implementing INativeChildView
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using AppKit;
using CoreGraphics;
using MonoDevelop.MacIntegration.Helpers;

namespace MonoDevelop.MacIntegration.Toolbox
{
	class ExpanderButton : NSButton
	{
		const int Margin = 5;
		static CGPoint textPoint = new CGPoint (7, 5);

		public event EventHandler Focused;

		public NSColor ForegroundColor { get; set; } = NSColor.ControlText;

		public NSImage ExpanderImage { get; set; }

		public override bool CanBecomeKeyView => true;

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}

		public ExpanderButton (IntPtr handle) : base (handle)
		{
			BezelStyle = NSBezelStyle.RegularSquare;
			SetButtonType (NSButtonType.OnOff);
			Bordered = false;
			Title = "";
		}

		public void SetCustomTitle (string title)
		{
			var font = NativeViewHelper.GetSystemFont (false, (int)NSFont.SmallSystemFontSize);
			AttributedTitle = NativeViewHelper.GetAttributedString (title, ForegroundColor, font);
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			if (ExpanderImage != null) {
				var context = NSGraphicsContext.CurrentContext;
				context.SaveGraphicsState ();
				ExpanderImage.Draw (new CGRect (Frame.Width - ExpanderImage.Size.Height - Margin, 4, ExpanderImage.Size.Width, ExpanderImage.Size.Height));
				context.RestoreGraphicsState ();
			}
		}
	}
}
