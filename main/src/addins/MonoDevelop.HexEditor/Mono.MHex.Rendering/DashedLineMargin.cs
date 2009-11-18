// 
// DashedLineMargin.cs
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
	public class DashedLineMargin : Margin
	{
		Gdk.GC foldDashedLineGC, foldDashedLineGC2, bgGC, bgGC2;

		public override int Width {
			get {
				return 1;
			}
		}
		
		public override int CalculateWidth (int bytesInRow)
		{
			return 1;
		}
		
		public DashedLineMargin (HexEditor hexEditor) : base (hexEditor)
		{
		}
		
		internal protected override void OptionsChanged ()
		{
			DisposeGCs ();
			
			foldDashedLineGC = CreateDashedLineGC (Style.DashedLineFg);
			foldDashedLineGC2 = CreateDashedLineGC (Style.DashedLineBg);
			bgGC = new Gdk.GC (Editor.GdkWindow) {
				RgbFgColor = Style.HexDigitBg
			};
			bgGC2 = new Gdk.GC (Editor.GdkWindow) {
				RgbFgColor = Style.HexDigit
			};
		}
		
		Gdk.GC CreateDashedLineGC (Gdk.Color fg)
		{
			var gc = new Gdk.GC (Editor.GdkWindow);
			gc.RgbFgColor = fg;
			gc.SetLineAttributes (1, Gdk.LineStyle.OnOffDash, Gdk.CapStyle.NotLast, Gdk.JoinStyle.Bevel);
			gc.SetDashes (0, new sbyte[] { 1, 1 }, 2);
			return gc;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			DisposeGCs ();
		}
		
		void DisposeGCs ()
		{
			if (foldDashedLineGC != null) {
				foldDashedLineGC.Dispose ();
				foldDashedLineGC = null;
			}
			if (foldDashedLineGC2 != null) {
				foldDashedLineGC2.Dispose ();
				foldDashedLineGC2 = null;
			}
			if (bgGC != null) {
				bgGC.Dispose ();
				bgGC = null;
			}
			if (bgGC2 != null) {
				bgGC2.Dispose ();
				bgGC2 = null;
			}
		}
		
		internal protected override void Draw (Gdk.Drawable win, Gdk.Rectangle area, long line, int x, int y)
		{
			int top = y;
			int bottom = top + this.Editor.LineHeight;
			bool isOdd = (top + (int)this.Data.VAdjustment.Value) % 2 != 0;
			win.DrawLine (isOdd? bgGC : bgGC2, x, top, x, bottom);
			win.DrawLine (isOdd? foldDashedLineGC : foldDashedLineGC2, x, top, x, bottom);
		}
	}}
