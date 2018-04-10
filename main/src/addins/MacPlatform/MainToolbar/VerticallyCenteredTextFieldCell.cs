//
// VerticallyCenteredTextFieldCell.cs
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

namespace MonoDevelop.MacIntegration.MainToolbar
{
	class VerticallyCenteredTextFieldCell : NSTextFieldCell
	{
		nfloat offset;
		public VerticallyCenteredTextFieldCell (nfloat yOffset)
		{
			offset = yOffset;
		}

		// This is invoked from the `Copy (NSZone)` method. ObjC sometimes clones the native NSTextFieldCell so we need to be able
		// to create a new managed wrapper for it.
		protected VerticallyCenteredTextFieldCell (IntPtr ptr)
			: base (ptr)
		{
		}

		/// <summary>
		/// Like what happens for the ios designer, AppKit can sometimes clone the native `NSTextFieldCell` using the Copy (NSZone)
		/// method. We *need* to ensure we can create a new managed wrapper for the cloned native object so we need the IntPtr
		/// constructor. NOTE: By keeping this override in managed we ensure the new wrapper C# object is created ~immediately,
		/// which makes it easier to debug issues.
		/// </summary>
		/// <returns>The copy.</returns>
		/// <param name="zone">Zone.</param>
		public override NSObject Copy (NSZone zone)
		{
			// Don't remove this override because the comment on this explains why we need this!
			var newCell = (VerticallyCenteredTextFieldCell) base.Copy (zone);
			newCell.offset = offset;
			return newCell;
		}

		public override CGRect DrawingRectForBounds (CGRect theRect)
		{
			// Get the parent's idea of where we should draw.
			CGRect newRect = base.DrawingRectForBounds (theRect);

			// Ideal size for the text.
			CGSize textSize = CellSizeForBounds (theRect);

			// Center in the rect.
			nfloat heightDelta = newRect.Size.Height - textSize.Height;
			if (heightDelta > 0) {
				newRect.Size = new CGSize (newRect.Width, newRect.Height - heightDelta);
				newRect.Location = new CGPoint (newRect.X, newRect.Y + heightDelta / 2 + offset);
			}
			return newRect;
		}
	}
}

