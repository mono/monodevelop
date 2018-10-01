/* 
 * Toolbox.cs - A toolbox widget
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
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

using AppKit;
using CoreGraphics;

namespace MonoDevelop.DesignerSupport.Toolbox.NativeViews
{
	class PaddingView : NSView
	{
		public int Height {
			get;
			set;
		} = 100;

		int padding = 10;
		public int Padding {
			get => padding;
			set {
				if (padding == value) {
					return;
				}
				padding = value;
			}
		}

		NSView content;
		public NSView Content {
			get => content;
			set {
				if (content == value) {
					return;
				}

				if (content != null) {
					content.RemoveFromSuperview ();
				}

				content = value;
				AddSubview (content);
				SetFrameSize (Frame.Size);
				NeedsDisplay = true;
			}
		}

		public override CGSize IntrinsicContentSize => new CGSize (Frame.Width, Height); 

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
			if (content != null) {
				content.Frame = new CGRect (Frame.X + Padding, Frame.Y + Padding, Frame.Width - Padding, Frame.Height - Padding);
			}
		}
	}
}
