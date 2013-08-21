// 
// HexEditorMargin.cs
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
	class HexEditorMargin : Margin
	{
		double groupWidth, byteWidth;
			
		public int LineHeight {
			get {
				return Editor.LineHeight;
			}
		}
		
		public override double Width {
			get {
				return CalculateWidth (Editor.BytesInRow);
			}
		}
		
		public override double CalculateWidth (int bytesInRow)
		{
			return (bytesInRow / Editor.Options.GroupBytes) * groupWidth;
		}
		
//		Pango.TabArray tabArray = null;
		
		public HexEditorMargin (HexEditor hexEditor) : base (hexEditor)
		{
			
		}

		internal protected override void OptionsChanged ()
		{
			var layout = new TextLayout (Editor);
			layout.Font = Editor.Options.Font;
			string groupString = new string ('0', Editor.Options.GroupBytes * 2);
			layout.Text = groupString + " ";
			double lineHeight;
			var sz = layout.GetSize ();
			groupWidth = sz.Width;
			lineHeight = sz.Height;
			 
			Data.LineHeight = lineHeight;
			
			layout.Text = "00";
			byteWidth = layout.GetSize ().Width;

			layout.Dispose ();
			
//			tabArray = new Pango.TabArray (1, true);
//			tabArray.SetTab (0, Pango.TabAlign.Left, groupWidth);
		}

		
		protected override LayoutWrapper RenderLine (long line)
		{
			var layout = new TextLayout (Editor);
			layout.Font = Editor.Options.Font;
//			layout.Tabs = tabArray;
			StringBuilder sb = new StringBuilder ();
			long startOffset = line * Editor.BytesInRow;
			long endOffset   = System.Math.Min (startOffset + Editor.BytesInRow, Data.Length);
			byte[] lineBytes = Data.GetBytes (startOffset, (int)(endOffset - startOffset));
			for (int i = 0; i < lineBytes.Length; i++) {
				sb.Append (string.Format ("{0:X2}", lineBytes[i]));
				if (i % Editor.Options.GroupBytes == 0)
					sb.Append (" "); // \t
			}
			
			layout.Text = sb.ToString ();
			Margin.LayoutWrapper result = new LayoutWrapper (layout);
			if (Data.IsSomethingSelected) {
				ISegment selection = Data.MainSelection.Segment;
				HandleSelection (selection.Offset, selection.EndOffset, startOffset, endOffset, null, delegate(long start, long end) {
					result.Layout.SetForeground (Style.Selection, (int)(start - startOffset) * 3, (int)(end - start) * 3 - 1);
					result.Layout.SetBackgound (Style.SelectionBg, (int)(start - startOffset) * 3, (int)(end - start) * 3 - 1);
				});
			}
			return result;
		}

		uint TranslateColumn (long column)
		{
			return (uint)(column * 3);
		}

		
		internal protected override void Draw (Context ctx, Rectangle area, long line, double x, double y)
		{
			ctx.Rectangle (x, y, Width, Editor.LineHeight);
			ctx.SetColor (Style.HexDigitBg); 
			ctx.Fill ();

			LayoutWrapper layout = GetLayout (line);
			char ch;
			if (!Data.IsSomethingSelected && Caret.InTextEditor && line == Data.Caret.Line) {
				ctx.Rectangle (CalculateCaretXPos (false, out ch), y, byteWidth, Editor.LineHeight);
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
			return CalculateCaretXPos (true, out ch);
		}
		double CalculateCaretXPos (bool useSubPositon, out char ch)
		{
			int byteInRow = (int)Caret.Offset % BytesInRow;
			int groupNumber = byteInRow / Editor.Options.GroupBytes;
			int groupByte = byteInRow % Editor.Options.GroupBytes;
			int caretIndex = groupNumber * (Editor.Options.GroupBytes * 2 + 1) + groupByte * 2;
			if (useSubPositon)
				caretIndex += Caret.SubPosition;
			LayoutWrapper layout = GetLayout ((int)Caret.Line);
			var rectangle = layout.Layout.GetCoordinateFromIndex (caretIndex);
			var text = layout.Layout.Text;

			ch = caretIndex < text.Length ? text [caretIndex] : ' ';
			if (layout.IsUncached)
				layout.Dispose ();
			return XOffset + rectangle.X;
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			if (args.Button != PointerButton.Left)
				return;
			
			Caret.InTextEditor = false;
			int groupChar;
			Caret.Offset      = GetOffset (args.X, args.Line, out groupChar);
			Caret.SubPosition = groupChar % 2;
		}

		long GetOffset (double x, long line, out int groupChar)
		{
			int groupNumber = (int)(x / groupWidth);
			groupChar = (int)((x % groupWidth) / Editor.textEditorMargin.charWidth);
			return line * BytesInRow + groupNumber * Editor.Options.GroupBytes + groupChar / 2;
		}

		internal protected override void MouseHover (MarginMouseMovedEventArgs args)
		{
			base.MouseHover (args);

			if (Editor.pressedButton == -1)
				return;
			Caret.InTextEditor = false;
			
			int groupChar;
			long hoverOffset = GetOffset (args.X, args.Line, out groupChar);
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
