// 
// GutterMargin.cs
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
using Xwt.Drawing;
using Xwt;

namespace Mono.MHex.Rendering
{
	class GutterMargin : Margin
	{
		double width;
		public override double Width {
			get {
				return width;
			}
		}
		
		public override double CalculateWidth (int bytesInRow)
		{
			return width;
		}
		
		public GutterMargin (HexEditor hexEditor) : base (hexEditor)
		{
		}
		
		internal protected override void OptionsChanged ()
		{
			var layout = new TextLayout (Editor);
			layout.Font = Editor.Options.Font;
			layout.Text = string.Format ("0{0:X}", Data.Length) + "_";
//			int height;
			width = layout.GetSize ().Width;
			layout.Dispose ();
		}

		protected override LayoutWrapper RenderLine (long line)
		{
			var layout = new TextLayout (Editor);
			layout.Font = Editor.Options.Font;
			layout.Text = string.Format ("{0:X}", line * Editor.BytesInRow);
			return new LayoutWrapper (layout);
		}
		
		internal protected override void Draw (Context ctx, Rectangle area, long line, double x, double y)
		{
			ctx.Rectangle (x, y, Width, Editor.LineHeight);
			ctx.SetColor (Style.HexOffsetBg);
			ctx.Fill ();

			if (line >= 0 && line * Editor.BytesInRow < Data.Length) {
				LayoutWrapper layout = GetLayout (line);
				var sz = layout.Layout.GetSize ();
				ctx.SetColor (line != Caret.Line ? Style.HexOffset : Style.HexOffsetHighlighted);
				ctx.DrawTextLayout (layout.Layout, x + Width - sz.Width - 4, y);
				if (layout.IsUncached)
					layout.Dispose ();
			}
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			if (args.Button != PointerButton.Left)
				return;
			Caret.Offset = args.Line * BytesInRow;
		}

		
	}
}
