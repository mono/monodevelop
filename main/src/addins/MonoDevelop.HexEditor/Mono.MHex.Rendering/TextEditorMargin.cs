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
using Xwt.Drawing;
using Xwt;

namespace Mono.MHex.Rendering
{
	class TextEditorMargin : Margin
	{
		internal double charWidth;
		public override double Width {
			get {
				return CalculateWidth (Editor.BytesInRow);
			}
		}
		
		public override double CalculateWidth (int bytesInRow)
		{
			switch (Editor.Options.StringRepresentationType) {
			case StringRepresentationTypes.ASCII:
				return charWidth * bytesInRow;
			case StringRepresentationTypes.UTF16:
				return charWidth * bytesInRow / 2;
			default:
				throw new NotImplementedException (Editor.Options.StringRepresentationType.ToString ());
			}
		}
		
		public TextEditorMargin (HexEditor hexEditor) : base (hexEditor)
		{
		}
		
		internal protected override void OptionsChanged ()
		{
			var layout = new TextLayout (Editor);
			layout.Font = Editor.Options.Font;
			layout.Text = ".";
//			int height;
			charWidth = layout.GetSize ().Width;
			layout.Dispose ();
		}
		
		protected override LayoutWrapper RenderLine (long line)
		{
			var layout = new TextLayout (Editor);
			layout.Font = Editor.Options.Font;
			StringBuilder sb = new StringBuilder ();
			long startOffset = line * Editor.BytesInRow;
			long endOffset   = System.Math.Min (startOffset + Editor.BytesInRow, Data.Length);
			byte[] lineBytes = Data.GetBytes (startOffset, (int)(endOffset - startOffset));
			switch (Editor.Options.StringRepresentationType) {
			case StringRepresentationTypes.ASCII:
				for (int i = 0; i < lineBytes.Length; i++) {
					byte b = lineBytes [i];
					char ch = (char)b;
					if (!char.IsControl (ch)) {
						sb.Append (ch);
					} else {
						sb.Append (".");
					}
				}
				break;
			case StringRepresentationTypes.UTF16:
				for (int i = 0; i < lineBytes.Length - 1; i += 2) {

					char ch = Encoding.Unicode.GetChars (lineBytes, i, 2) [0];
					if (char.IsLetterOrDigit (ch) || char.IsWhiteSpace (ch) || char.IsSymbol (ch) || char.IsPunctuation (ch))
						sb.Append (ch);
					else
						sb.Append (".");
				}
				break;
			default:
				throw new NotImplementedException (Editor.Options.StringRepresentationType.ToString ());
			}
			
			layout.Text = sb.ToString ();
			Margin.LayoutWrapper result = new LayoutWrapper (layout);
			if (Data.IsSomethingSelected) {
				ISegment selection = Data.MainSelection.Segment;
				HandleSelection (selection.Offset, selection.EndOffset, startOffset, endOffset, null, delegate(long start, long end) {
					result.Layout.SetForeground (Style.Selection, (int)(start - startOffset)/2, (int)(end - start)/2);
					result.Layout.SetBackground (Style.SelectionBg, (int)(start - startOffset)/2, (int)(end - start)/2);
				});
			}
			return result;
		}

		internal protected override void Draw (Context ctx, Rectangle area, long line, double x, double y)
		{
			ctx.Rectangle (x, y, Width, Editor.LineHeight);
			ctx.SetColor (Style.HexDigitBg);
			ctx.Fill ();

			LayoutWrapper layout = GetLayout (line);
			if (!Data.IsSomethingSelected && !Caret.InTextEditor && line == Data.Caret.Line) {
				var column = (int)(Caret.Offset % BytesInRow);
				if (Editor.Options.StringRepresentationType == StringRepresentationTypes.UTF16)
					column /= 2;
				var xOffset = charWidth * column;
				ctx.Rectangle (x + xOffset, y, charWidth, Editor.LineHeight);
				ctx.SetColor (Style.HighlightOffset);
				ctx.Fill ();
			}
			ctx.SetColor (Style.HexDigit);
			ctx.DrawTextLayout (layout.Layout, x, y);
			if (layout.IsUncached)
				layout.Dispose ();
		}

		public double CalculateCaretXPos (out char ch)
		{
			var layout = GetLayout (Data.Caret.Line);
			ch = (char)Data.GetByte (Caret.Offset);

			var rectangle = layout.Layout.GetCoordinateFromIndex ((int)(Caret.Offset % BytesInRow));

			if (layout.IsUncached)
				layout.Dispose ();

			return XOffset + rectangle.X;
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			if (args.Button != PointerButton.Left)
				return;
			
			Caret.InTextEditor = true;
			Caret.Offset = GetOffset (args.X, args.Line);
		}

		long GetOffset (double x, long line)
		{
			if (Editor.Options.StringRepresentationType == StringRepresentationTypes.UTF16) {
				return (long)(line * BytesInRow + x / charWidth);
			} else {
				return (long)(line * BytesInRow + x / (charWidth * 2));
			}
		}
		
		internal protected override void MouseHover (MarginMouseMovedEventArgs args)
		{
			base.MouseHover (args);
			if (Editor.pressedButton == -1)
				return;
			Caret.InTextEditor = true;
			
			long hoverOffset = GetOffset (args.X, args.Line);
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
