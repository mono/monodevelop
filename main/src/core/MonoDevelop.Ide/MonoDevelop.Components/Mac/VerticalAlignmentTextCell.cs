//
// VerticalAlignmentTextCell.cs
//
// Author:
//       Jose Medrano <josmed@microsoft.com>
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
using CoreGraphics;
using Foundation;

namespace MonoDevelop.Components.Mac
{
	public class VerticalAlignmentTextCell : NSTextFieldCell
	{
		public NSTextBlockVerticalAlignment VerticalAligment { get; set; }

		public VerticalAlignmentTextCell (NSTextBlockVerticalAlignment verticalAlignment = NSTextBlockVerticalAlignment.Top)
		{
			VerticalAligment = verticalAlignment;
		}

		public VerticalAlignmentTextCell (NSCoder coder) : base (coder)
		{
		}

		public VerticalAlignmentTextCell (string aString) : base (aString)
		{
		}

		public VerticalAlignmentTextCell (NSImage image) : base (image)
		{
		}

		protected VerticalAlignmentTextCell (NSObjectFlag t) : base (t)
		{
		}

		protected internal VerticalAlignmentTextCell (IntPtr handle) : base (handle)
		{
		}

		public override CGRect DrawingRectForBounds (CGRect theRect)
		{
			var newRect = base.DrawingRectForBounds (theRect);
			if (VerticalAligment == NSTextBlockVerticalAlignment.Top)
				return newRect;
			
			var textSize = CellSizeForBounds (theRect);
			//Center in the proposed rect
			float heightDelta = (float)(newRect.Size.Height - textSize.Height);
			float height = (float)newRect.Height;
			float y = (float)newRect.Y;
			if (heightDelta > 0) {
				height -= heightDelta;
				if (VerticalAligment == NSTextBlockVerticalAlignment.Bottom)
					y += heightDelta;
				else
					y += heightDelta / 2f;
			}
			return new CGRect (newRect.X, y, newRect.Width, height);
		}
	}
}

#endif
