// 
// TextEditorMargin.cs
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
using System.Text;
using Mono.MHex.Data;

namespace Mono.MHex.Rendering
{
	public class TextEditorMargin : Margin
	{
		internal int charWidth;
		public override int Width {
			get {
				return charWidth * Editor.BytesInRow;
			}
		}
		
		public override int CalculateWidth (int bytesInRow)
		{
			return charWidth * bytesInRow;
		}
		
		public TextEditorMargin (HexEditor hexEditor) : base (hexEditor)
		{
		}
		
		Gdk.GC bgGC;
		Gdk.GC fgGC;
		internal protected override void OptionsChanged ()
		{
			Pango.Layout layout = new Pango.Layout (Editor.PangoContext);
			layout.FontDescription = Editor.Options.Font;
			layout.SetText (".");
			int height;
			layout.GetPixelSize (out charWidth, out height);
			layout.Dispose ();
			bgGC = GetGC (Style.HexDigitBg);
			fgGC = GetGC (Style.HexDigit);
		}
		
		protected override LayoutWrapper RenderLine (long line)
		{
			Pango.Layout layout = new Pango.Layout (Editor.PangoContext);
			layout.FontDescription = Editor.Options.Font;
			StringBuilder sb = new StringBuilder ();
			long startOffset = line * Editor.BytesInRow;
			long endOffset   = System.Math.Min (startOffset + Editor.BytesInRow, Data.Length);
			byte[] lineBytes = Data.GetBytes (startOffset, (int)(endOffset - startOffset));
			for (int i = 0; i < lineBytes.Length; i++) {
				byte b = lineBytes[i];
				char ch = (char)b;
				if (b < 128 && (Char.IsLetterOrDigit (ch) || Char.IsPunctuation (ch))) {
					sb.Append (ch);
				} else {
					sb.Append (".");
				}
			}
			
			layout.SetText (sb.ToString ());
			char[] lineChars = layout.Text.ToCharArray ();
			Margin.LayoutWrapper result = new LayoutWrapper (layout);
			uint curIndex = 0, byteIndex = 0;
			if (Data.IsSomethingSelected) {
				ISegment selection = Data.MainSelection.Segment;
				HandleSelection (selection.Offset, selection.EndOffset, startOffset, endOffset, null, delegate(long start, long end) {
					Pango.AttrForeground selectedForeground = new Pango.AttrForeground (Style.Selection.Red, 
					                                                                    Style.Selection.Green, 
					                                                                    Style.Selection.Blue);
					selectedForeground.StartIndex = TranslateToUTF8Index (lineChars, (uint)(start - startOffset), ref curIndex, ref byteIndex);
					selectedForeground.EndIndex = TranslateToUTF8Index (lineChars, (uint)(end - startOffset), ref curIndex, ref byteIndex);
					
					result.Add (selectedForeground);
					
					Pango.AttrBackground attrBackground = new Pango.AttrBackground (Style.SelectionBg.Red, 
					                                                                Style.SelectionBg.Green, 
					                                                                Style.SelectionBg.Blue);
					attrBackground.StartIndex = selectedForeground.StartIndex;
					attrBackground.EndIndex = selectedForeground.EndIndex;
					result.Add (attrBackground);

				});
			}
			result.SetAttributes ();
			return result;
		}
		
		internal protected override void Draw (Gdk.Drawable drawable, Gdk.Rectangle area, long line, int x, int y)
		{
			
			drawable.DrawRectangle (bgGC, true, x, y, Width, Editor.LineHeight);
			LayoutWrapper layout = GetLayout (line);
			if (!Data.IsSomethingSelected && !Caret.InTextEditor && line == Data.Caret.Line) {
				int column = (int)(Caret.Offset % BytesInRow);
				int xOffset = charWidth * column;
				drawable.DrawRectangle (GetGC (Style.HighlightOffset), true, x + xOffset, y, charWidth, Editor.LineHeight);
			}
			drawable.DrawLayout (fgGC, x, y, layout.Layout);
			if (layout.IsUncached)
				layout.Dispose ();
		}

		public int CalculateCaretXPos ()
		{
			return (int)(XOffset + Caret.Offset % BytesInRow * charWidth);
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			if (args.Button != 1)
				return;
			
			Caret.InTextEditor = true;
			Caret.Offset = GetOffset (args);
		}

		long GetOffset (MarginMouseEventArgs args)
		{
			return args.Line * BytesInRow + args.X / charWidth;
		}
		
		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);

			if (args.Button != 1)
				return;
			Caret.InTextEditor = true;
			
			long hoverOffset = GetOffset (args);
			if (Data.MainSelection == null) {
				Data.SetSelection (hoverOffset, hoverOffset);
			} else {
				Data.ExtendSelectionTo (hoverOffset);
			}
			Caret.PreserveSelection = true;
			Caret.Offset = hoverOffset;
			Caret.PreserveSelection = false;
		}
	}
}
