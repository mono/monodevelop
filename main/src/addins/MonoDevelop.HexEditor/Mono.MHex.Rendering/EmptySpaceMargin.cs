// 
// EmptySpaceMargin.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace Mono.MHex.Rendering
{
	public class EmptySpaceMargin : Margin
	{
		public override int Width {
			get { 
				return Editor.Allocation.Width - XOffset; 
			}
		}

		public override int CalculateWidth (int bytesInRow)
		{
			return 0;
		}

		public EmptySpaceMargin (HexEditor hexEditor) : base (hexEditor)
		{
		}
		Gdk.GC bgGC;
		
		internal protected override void OptionsChanged ()
		{
			bgGC = GetGC (Style.HexDigitBg);
		}

		protected internal override void Draw (Gdk.Drawable drawable, Gdk.Rectangle area, long line, int x, int y)
		{
			drawable.DrawRectangle (bgGC, true, x, y, Width, Editor.LineHeight);
		}
		
	}
}
