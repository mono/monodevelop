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

namespace Mono.TextEditor
{
	public class DashedLineMargin : Margin
	{
		TextEditor editor;
//		Gdk.GC foldDashedLineGC, foldDashedLineGC2, bgGC, bgGC2;
		
		Gdk.GC bgGC, gc;

		public override int Width {
			get {
				return 1;
			}
		}
		
		public DashedLineMargin (TextEditor editor)
		{
			this.editor = editor;
		}
		
		internal protected override void OptionsChanged ()
		{
			DisposeGCs ();
			bgGC = new Gdk.GC (this.editor.GdkWindow) {
				RgbFgColor = editor.ColorStyle.Default.BackgroundColor
			};
			gc = new Gdk.GC (this.editor.GdkWindow) {
				RgbFgColor = editor.ColorStyle.FoldLine.Color
			};
/*			foldDashedLineGC = CreateDashedLineGC (editor.ColorStyle.Default.Color);
			foldDashedLineGC2 = CreateDashedLineGC (editor.ColorStyle.Default.BackgroundColor);
			bgGC = new Gdk.GC (this.editor.GdkWindow) {
				RgbFgColor = editor.ColorStyle.Default.BackgroundColor
			};
			bgGC2 = new Gdk.GC (this.editor.GdkWindow) {
				RgbFgColor = editor.ColorStyle.Default.Color
			};*/
		}
		
/*		Gdk.GC CreateDashedLineGC (Gdk.Color fg)
		{
			var gc = new Gdk.GC (editor.GdkWindow);
			gc.RgbFgColor = fg;
			gc.SetLineAttributes (1, Gdk.LineStyle.OnOffDash, Gdk.CapStyle.NotLast, Gdk.JoinStyle.Bevel);
			gc.SetDashes (0, new sbyte[] { 1, 1 }, 2);
			return gc;
		}*/
		
		public override void Dispose ()
		{
			base.Dispose ();
			DisposeGCs ();
			editor = null;
		}
		
		void DisposeGCs ()
		{
			gc = gc.Kill ();
			bgGC = bgGC.Kill ();
/*			foldDashedLineGC = foldDashedLineGC.Kill ();
			foldDashedLineGC2 = foldDashedLineGC2.Kill ();
			bgGC = bgGC.Kill ();
			bgGC2 = bgGC2.Kill ();*/
		}
		
		public bool UseBGColor {
			get;
			set;
		}
		
		internal protected override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int line, int x, int y, int lineHeight)
		{
			
			win.DrawLine (UseBGColor ? bgGC : gc, x, y, x, y + lineHeight);
			
//			int top = y;
//			int bottom = top + lineHeight;
//			bool isOdd = (top + (int)editor.VAdjustment.Value) % 2 != 0;
//			win.DrawLine (isOdd? bgGC : bgGC2, x, top, x, bottom);
//			win.DrawLine (isOdd? foldDashedLineGC : foldDashedLineGC2, x, top, x, bottom);
		}
	}
}

/* Cairo version (todo for 2.6: Render with cairo instead of gdk)
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
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public class DashedLineMargin : Margin
	{
		TextEditor editor;
		Cairo.Color lineColor, bgColor;

		public override int Width {
			get {
				return System.Math.Max (1, (int)editor.Options.Zoom);
			}
		}
		
		public DashedLineMargin (TextEditor editor)
		{
			this.editor = editor;
		}
		
		internal protected override void OptionsChanged ()
		{
			lineColor = Style.ToCairoColor (editor.ColorStyle.FoldLine.Color);
			bgColor = Style.ToCairoColor (editor.ColorStyle.FoldLine.BackgroundColor);
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			editor = null;
		}
		
		public bool UseBGColor {
			get;
			set;
		}
		
		internal protected override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int line, int x, int y, int lineHeight)
		{
			using (var g = Gdk.CairoHelper.Create (win)) {
				g.MoveTo (x + (double)Width / 2, y);
				g.LineTo (x + (double)Width / 2, y + lineHeight);
				g.LineWidth = System.Math.Max (1.0, editor.Options.Zoom);
				g.Color = UseBGColor ? bgColor : lineColor;
				g.Stroke ();
			}
		}
	}
}*/
