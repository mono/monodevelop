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

namespace Mono.MHex.Rendering
{
	public class HexEditorMargin : Margin
	{
		int groupWidth, byteWidth;
			
		public int LineHeight {
			get {
				return Editor.LineHeight;
			}
		}
		
		public override int Width {
			get {
				return CalculateWidth (Editor.BytesInRow);
			}
		}
		
		public override int CalculateWidth (int bytesInRow)
		{
			return (bytesInRow / Editor.Options.GroupBytes) * groupWidth;
		}
		
		Pango.TabArray tabArray = null;
		
		public HexEditorMargin (HexEditor hexEditor) : base (hexEditor)
		{
			
		}
		
		Gdk.GC bgGC;
		Gdk.GC fgGC;

		internal protected override void OptionsChanged ()
		{
			Pango.Layout layout = new Pango.Layout (Editor.PangoContext);
			layout.FontDescription = Editor.Options.Font;
			string groupString = new string ('0', Editor.Options.GroupBytes * 2);
			layout.SetText (groupString + " ");
			int lineHeight;
			layout.GetPixelSize (out groupWidth, out lineHeight);
			Data.LineHeight = lineHeight;
			
			layout.SetText ("00");
			layout.GetPixelSize (out byteWidth, out lineHeight);
			
			layout.Dispose ();
			
			tabArray = new Pango.TabArray (1, true);
			tabArray.SetTab (0, Pango.TabAlign.Left, groupWidth);
			
			bgGC = GetGC (Style.HexDigitBg);
			fgGC = GetGC (Style.HexDigit);
		}

		
		protected override LayoutWrapper RenderLine (long line)
		{
			Pango.Layout layout = new Pango.Layout (Editor.PangoContext);
			layout.FontDescription = Editor.Options.Font;
			layout.Tabs = tabArray;
			StringBuilder sb = new StringBuilder ();
			long startOffset = line * Editor.BytesInRow;
			long endOffset   = System.Math.Min (startOffset + Editor.BytesInRow, Data.Length);
			byte[] lineBytes = Data.GetBytes (startOffset, (int)(endOffset - startOffset));
			for (int i = 0; i < lineBytes.Length; i++) {
				sb.Append (string.Format ("{0:X2}", lineBytes[i]));
				if (i % Editor.Options.GroupBytes == 0)
					sb.Append ("\t");
			}
			
			layout.SetText (sb.ToString ());
			char[] lineChars = sb.ToString ().ToCharArray ();
			Margin.LayoutWrapper result = new LayoutWrapper (layout);
			uint curIndex = 0, byteIndex = 0;
			if (Data.IsSomethingSelected) {
				ISegment selection = Data.MainSelection.Segment;
				HandleSelection (selection.Offset, selection.EndOffset, startOffset, endOffset, null, delegate(long start, long end) {
					Pango.AttrForeground selectedForeground = new Pango.AttrForeground (Style.Selection.Red, 
					                                                                    Style.Selection.Green, 
					                                                                    Style.Selection.Blue);
					selectedForeground.StartIndex = TranslateToUTF8Index (lineChars, TranslateColumn (start - startOffset), ref curIndex, ref byteIndex);
					selectedForeground.EndIndex = TranslateToUTF8Index (lineChars, TranslateColumn (end - startOffset) - 1, ref curIndex, ref byteIndex);
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

		uint TranslateColumn (long column)
		{
			return (uint)(column * 3);
		}

		
		internal protected override void Draw (Gdk.Drawable drawable, Gdk.Rectangle area, long line, int x, int y)
		{
			drawable.DrawRectangle (bgGC, true, x, y, Width, Editor.LineHeight);
			LayoutWrapper layout = GetLayout (line);
			
			if (!Data.IsSomethingSelected && Caret.InTextEditor && line == Data.Caret.Line) {
				drawable.DrawRectangle (GetGC (Style.HighlightOffset), true, CalculateCaretXPos (false), y, byteWidth, Editor.LineHeight);
			}
			
			drawable.DrawLayout (fgGC, x, y, layout.Layout);
			if (layout.IsUncached)
				layout.Dispose ();
		}
		
		public int CalculateCaretXPos ()
		{
			return CalculateCaretXPos (true);
		}
		int CalculateCaretXPos (bool useSubPositon)
		{
			int byteInRow = (int)Caret.Offset % BytesInRow;
			int groupNumber = byteInRow / Editor.Options.GroupBytes;
			int groupByte = byteInRow % Editor.Options.GroupBytes;
			int caretIndex = groupNumber * (Editor.Options.GroupBytes * 2 + 1) + groupByte * 2;
			if (useSubPositon)
				caretIndex += Caret.SubPosition;
			LayoutWrapper layout = GetLayout ((int)Caret.Line);
			Pango.Rectangle rectangle = layout.Layout.IndexToPos (caretIndex);
			if (layout.IsUncached)
				layout.Dispose ();
			return XOffset + (int)(rectangle.X / Pango.Scale.PangoScale);
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			if (args.Button != 1)
				return;
			
			Caret.InTextEditor = false;
			int groupChar;
			Caret.Offset      = GetOffset (args, out groupChar);
			Caret.SubPosition = groupChar % 2;
		}

		long GetOffset (MarginMouseEventArgs args, out int groupChar)
		{
			int groupNumber = args.X / groupWidth;
			groupChar = (args.X % groupWidth) / Editor.textEditorMargin.charWidth;
			return args.Line * BytesInRow + groupNumber * Editor.Options.GroupBytes + groupChar / 2;
		}
		
		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);

			if (args.Button != 1)
				return;
			Caret.InTextEditor = false;
			
			int groupChar;
			long hoverOffset = GetOffset (args, out groupChar);
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
