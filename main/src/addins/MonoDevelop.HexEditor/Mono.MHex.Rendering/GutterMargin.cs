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

namespace Mono.MHex.Rendering
{
	public class GutterMargin : Margin
	{
		int width;
		public override int Width {
			get {
				return width;
			}
		}
		
		public override int CalculateWidth (int bytesInRow)
		{
			return width;
		}
		
		public GutterMargin (HexEditor hexEditor) : base (hexEditor)
		{
		}
		
		internal protected override void OptionsChanged ()
		{
			Pango.Layout layout = new Pango.Layout (Editor.PangoContext);
			layout.FontDescription = Editor.Options.Font;
			layout.SetText (string.Format ("0{0:X}", Data.Length) + "_");
			int height;
			layout.GetPixelSize (out this.width, out height);
			layout.Dispose ();
		}

		protected override LayoutWrapper RenderLine (long line)
		{
			Pango.Layout layout = new Pango.Layout (Editor.PangoContext);
			layout.FontDescription = Editor.Options.Font;
			layout.SetText (string.Format ("{0:X}", line * Editor.BytesInRow));
			return new LayoutWrapper (layout);
		}
		
		internal protected override void Draw (Gdk.Drawable drawable, Gdk.Rectangle area, long line, int x, int y)
		{
			drawable.DrawRectangle (GetGC (Style.HexOffsetBg), true, x, y, Width, Editor.LineHeight);
			LayoutWrapper layout = GetLayout (line);
			int w, h;
			layout.Layout.GetPixelSize (out w, out h);
			drawable.DrawLayout (GetGC (line != Caret.Line ? Style.HexOffset : Style.HexOffsetHighlighted), x + Width - w - 4, y, layout.Layout);
			if (layout.IsUncached)
				layout.Dispose ();
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			if (args.Button != 1)
				return;
			Caret.Offset = args.Line * BytesInRow;
		}

		
	}
}
