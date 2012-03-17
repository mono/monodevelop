// 
// ISyntaxMode.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Text;

namespace Mono.TextEditor.Highlighting
{
	/// <summary>
	/// The basic inferace for all syntax modes
	/// </summary>
	public interface ISyntaxMode
	{
		/// <summary>
		/// Gets or sets the document the syntax mode is attached to. To detach it's set to null.
		/// </summary>
		TextDocument Document {
			get;
			set;
		}

		/// <summary>
		/// Gets colorized segments (aka chunks) from offset to offset + length.
		/// </summary>
		/// <param name='style'>
		/// The color scheme used te generate the chunks.
		/// </param>
		/// <param name='line'>
		/// The starting line at (offset). This is the same as Document.GetLineByOffset (offset).
		/// </param>
		/// <param name='offset'>
		/// The starting offset.
		/// </param>
		/// <param name='length'>
		/// The length of the text converted to chunks.
		/// </param>
		IEnumerable<Chunk> GetChunks (ColorSheme style, LineSegment line, int offset, int length);
	}

	public static class SyntaxModeHelper
	{
		public static string GetMarkup (this ISyntaxMode mode, ITextEditorOptions options, ColorSheme style, int offset, int length, bool removeIndent, bool useColors = true, bool replaceTabs = true)
		{
			var doc = mode.Document;

			int indentLength = SyntaxMode.GetIndentLength (doc, offset, length, false);
			int curOffset = offset;

			StringBuilder result = new StringBuilder ();
			while (curOffset < offset + length && curOffset < doc.Length) {
				LineSegment line = doc.GetLineByOffset (curOffset);
				int toOffset = System.Math.Min (line.Offset + line.EditableLength, offset + length);
				Stack<ChunkStyle> styleStack = new Stack<ChunkStyle> ();
				foreach (var chunk in mode.GetChunks (style, line, curOffset, toOffset - curOffset)) {

					ChunkStyle chunkStyle = style.GetChunkStyle (chunk);
					bool setBold = chunkStyle.Bold && (styleStack.Count == 0 || !styleStack.Peek ().Bold) ||
							!chunkStyle.Bold && (styleStack.Count == 0 || styleStack.Peek ().Bold);
					bool setItalic = chunkStyle.Italic && (styleStack.Count == 0 || !styleStack.Peek ().Italic) ||
							!chunkStyle.Italic && (styleStack.Count == 0 || styleStack.Peek ().Italic);
					bool setUnderline = chunkStyle.Underline && (styleStack.Count == 0 || !styleStack.Peek ().Underline) ||
							!chunkStyle.Underline && (styleStack.Count == 0 || styleStack.Peek ().Underline);
					bool setColor = styleStack.Count == 0 || TextViewMargin.GetPixel (styleStack.Peek ().Color) != TextViewMargin.GetPixel (chunkStyle.Color);
					if (setColor || setBold || setItalic || setUnderline) {
						if (styleStack.Count > 0) {
							result.Append ("</span>");
							styleStack.Pop ();
						}
						result.Append ("<span");
						if (useColors) {
							result.Append (" foreground=\"");
							result.Append (SyntaxMode.ColorToPangoMarkup (chunkStyle.Color));
							result.Append ("\"");
						}
						if (chunkStyle.Bold)
							result.Append (" weight=\"bold\"");
						if (chunkStyle.Italic)
							result.Append (" style=\"italic\"");
						if (chunkStyle.Underline)
							result.Append (" underline=\"single\"");
						result.Append (">");
						styleStack.Push (chunkStyle);
					}

					for (int i = 0; i < chunk.Length && chunk.Offset + i < doc.Length; i++) {
						char ch = doc.GetCharAt (chunk.Offset + i);
						switch (ch) {
						case '&':
							result.Append ("&amp;");
							break;
						case '<':
							result.Append ("&lt;");
							break;
						case '>':
							result.Append ("&gt;");
							break;
						case '\t':
							if (replaceTabs) {
								result.Append (new string (' ', options.TabSize));
							} else {
								result.Append ('\t');
							}
							break;
						default:
							result.Append (ch);
							break;
						}
					}
				}
				while (styleStack.Count > 0) {
					result.Append("</span>");
					styleStack.Pop ();
				}

				curOffset = line.EndOffset;
				if (removeIndent)
					curOffset += indentLength;
				if (result.Length > 0 && curOffset < offset + length)
					result.AppendLine ();
			}
			return result.ToString ();
		}
	}
}

