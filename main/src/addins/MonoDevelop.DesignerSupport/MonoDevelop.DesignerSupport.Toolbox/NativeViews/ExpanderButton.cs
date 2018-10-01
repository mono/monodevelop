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

using System.Drawing;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	public class ExpanderButton : NSButton
	{
		const int Margin = 5;
		static CGPoint textPoint = new CGPoint (7, 5);

		public ExpanderButton ()
		{
			BezelStyle = NSBezelStyle.TexturedSquare;
			SetButtonType (NSButtonType.OnOff);
			Bordered = false;
			Title = "";
			WantsLayer = true;
			Layer.BackgroundColor = NSColor.Clear.CGColor;
		}

		public void SetCustomTitle (string title)
		{
			var font = NativeViewHelper.GetSystemFont (false, (int)NSFont.SmallSystemFontSize);
			AttributedTitle = NativeViewHelper.GetAttributedString (title, Styles.HeaderForegroundColor, font);
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			Styles.HeaderCellBackgroundSelectedColor.Set ();
			NSBezierPath.FillRect (Bounds);

			AttributedTitle.DrawAtPoint (textPoint);

			if (Image != null) {
				var context = NSGraphicsContext.CurrentContext;
				context.SaveGraphicsState ();
				Image.Draw (new CGRect (Frame.Width - Image.Size.Height - Margin, 4, Image.Size.Width, Image.Size.Height));
				context.RestoreGraphicsState ();
			}
		}

		public bool Visible {
			get { return !Hidden; }
			set {
				Hidden = !value;
			}
		}
	}
}
