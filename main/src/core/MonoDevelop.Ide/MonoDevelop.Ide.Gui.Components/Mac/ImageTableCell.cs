// 
// ImageTableCell.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
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

namespace MonoDevelop.Ide.Gui.Components.Internal
{
	class ImageTableCell: NSImageCell
	{
		TreeItem val;

		public ImageTableCell ()
		{
		}
		
		public ImageTableCell (IntPtr p): base (p)
		{
		}
		
		public override CGSize CellSize {
			get {
				NSImage img = ObjectValue as NSImage;
				if (img != null) {
					var s = img.Size;
					s.Width += 4;
					s.Height += 4;
					return s;
				}
				else
					return base.CellSize;
			}
		}

		public void Fill (TreeItem val)
		{
			this.val = val;
			if (val.Icon != null)
				ObjectValue = (NSImage)DesktopService.NativeToolkit.GetNativeImage (val.Icon);
		}

		public override void DrawInteriorWithFrame (CGRect cellFrame, NSView inView)
		{
			base.DrawInteriorWithFrame (cellFrame, inView);

			CGContext ctx = NSGraphicsContext.CurrentContext.GraphicsPort;
			var c = DesktopService.NativeToolkit.WrapContext (inView, ctx);
			if (val.OverlayBottomRight != null) {
				var img = val.OverlayBottomRight;
				var p = new Xwt.Point (cellFrame.Right - img.Width, cellFrame.Bottom - img.Height);
				c.DrawImage (val.OverlayBottomRight, p);
			}
		}
	}
}

#endif