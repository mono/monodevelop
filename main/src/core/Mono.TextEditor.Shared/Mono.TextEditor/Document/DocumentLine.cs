// LineSegment.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Text;
using System.Collections.Generic;
using Mono.TextEditor.Highlighting;
using System.Linq;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using MonoDevelop.Core;

namespace Mono.TextEditor
{
	/// <summary>
	/// A line inside a <see cref="T:Mono.TextEditor.TextDocument"/>.
	/// </summary>
	abstract class DocumentLine : IDocumentLine
	{
		/// <summary>
		/// Gets the length of the line.
		/// </summary>
		/// <remarks>The length does not include the line delimeter.</remarks>
		public int Length {
			get {
				return LengthIncludingDelimiter - DelimiterLength;
			}
		}

		/// <summary>
		/// Gets the unicode newline for this line. Returns UnicodeNewline.Unknown for no new line (in the last line of the document)
		/// </summary>
		public UnicodeNewline UnicodeNewline {
			get;
			internal set;
		}

		/// <summary>
		/// Gets the length of the line terminator.
		/// Returns 1 or 2; or 0 at the end of the document.
		/// </summary>
		public int DelimiterLength {
			get { 
				switch (UnicodeNewline) {
				case UnicodeNewline.Unknown:
					return 0;
				case UnicodeNewline.CRLF:
					return 2;
				default:
					return 1;
				}
			}
		}

		/// <summary>
		/// Gets the start offset of the line.
		/// </summary>
		public abstract int Offset { get; set; }

		/// <summary>
		/// Gets the length of the line including the line delimiter.
		/// </summary>
		public int LengthIncludingDelimiter {
			get;
			set;
		}

		/// <summary>
		/// Gets the text segment of the line.
		/// </summary>
		/// <remarks>The text segment does not include the line delimeter.</remarks>
		public TextSegment Segment {
			get {
				return new TextSegment (Offset, Length);
			}
		}

		/// <summary>
		/// Gets the text segment of the line including the line delimiter.
		/// </summary>
		public ISegment SegmentIncludingDelimiter {
			get {
				return new TextSegment (Offset, LengthIncludingDelimiter);
			}
		}


		/// <summary>
		/// Gets the end offset of the line.
		/// </summary>
		/// <remarks>The end offset does not include the line delimeter.</remarks>
		public int EndOffset {
			get {
				return Offset + Length;
			}
		}

		/// <summary>
		/// Gets the end offset of the line including the line delimiter.
		/// </summary>
		public int EndOffsetIncludingDelimiter {
			get {
				return Offset + LengthIncludingDelimiter;
			}
		}


		/// <summary>
		/// Gets the number of this line.
		/// The first line has the number 1.
		/// </summary>
		public abstract int LineNumber { get; }

		/// <summary>
		/// Gets the next line. Returns null if this is the last line in the document.
		/// </summary>
		public abstract DocumentLine NextLine { get; }

		/// <summary>
		/// Gets the previous line. Returns null if this is the first line in the document.
		/// </summary>
		public abstract DocumentLine PreviousLine { get; }


		MonoDevelop.Ide.Editor.IDocumentLine MonoDevelop.Ide.Editor.IDocumentLine.PreviousLine {
			get {
				return PreviousLine;
			}
		}

		MonoDevelop.Ide.Editor.IDocumentLine MonoDevelop.Ide.Editor.IDocumentLine.NextLine {
			get {
				return NextLine;
			}
		}

		protected DocumentLine (int length, UnicodeNewline unicodeNewline)
		{
			LengthIncludingDelimiter = length;
			UnicodeNewline = unicodeNewline;
		}


		/// <summary>
		/// This method gets the line indentation.
		/// </summary>
		/// <param name="doc">
		/// The <see cref="TextDocument"/> the line belongs to.
		/// </param>
		/// <returns>
		/// The indentation of the line (all whitespace chars up to the first non ws char).
		/// </returns>
		public string GetIndentation (TextDocument doc)
		{
			var result = StringBuilderCache.Allocate ();
			int offset = Offset;
			int max = System.Math.Min (offset + LengthIncludingDelimiter, doc.Length);
			for (int i = offset; i < max; i++) {
				char ch = doc.GetCharAt (i);
				if (ch != ' ' && ch != '\t')
					break;
				result.Append (ch);
			}
			return StringBuilderCache.ReturnAndFree (result);
		}

		public int GetLogicalColumn (TextEditorData editor, int visualColumn)
		{
			int curVisualColumn = 1;
			int offset = Offset;
			int max = offset + Length;
			var textLength = editor.Document.Length;
			int tabSize = editor.Options != null ? editor.Options.TabSize : 4;
			for (int i = offset; i < max; i++) {
				if (i < textLength && editor.Document.GetCharAt (i) == '\t') {
					curVisualColumn = TextViewMargin.GetNextTabstop (editor, curVisualColumn, tabSize);
				} else {
					curVisualColumn++;
				}
				if (curVisualColumn > visualColumn)
					return i - offset + 1;
			}
			return Length + (visualColumn - curVisualColumn) + 1;
		}

		public int GetVisualColumn (TextEditorData editor, int logicalColumn)
		{
			int result = 1;
			int offset = Offset;
			var tabSize = editor.Options.TabSize;
			if (editor.Options.IndentStyle == IndentStyle.Virtual && Length == 0 && logicalColumn > DocumentLocation.MinColumn) {
				foreach (char ch in editor.GetIndentationString (Offset)) {
					if (ch == '\t') {
						result += tabSize;
						continue;
					}
					result++;
				}
				return result;
			}

			for (int i = 0; i < logicalColumn - 1; i++) {
				if (i < Length && editor.Document.GetCharAt (offset + i) == '\t') {
					result = TextViewMargin.GetNextTabstop (editor, result, tabSize);
				} else {
					result++;
				}
			}
			return result;
		}

		/// <summary>
		/// Determines whether this line contains the specified offset. 
		/// </summary>
		/// <returns>
		/// <c>true</c> if this line contains the specified offset (upper bound exclusive); otherwise, <c>false</c>.
		/// </returns>
		/// <param name='offset'>
		/// The offset.
		/// </param>
		public bool Contains (int offset)
		{
			int o = Offset;
			return o <= offset && offset < o + LengthIncludingDelimiter;
		}

		/// <summary>
		/// Determines whether this line contains the specified segment. 
		/// </summary>
		/// <returns>
		/// <c>true</c> if this line contains the specified segment (upper bound inclusive); otherwise, <c>false</c>.
		/// </returns>
		/// <param name='segment'>
		/// The segment.
		/// </param>
		public bool Contains (TextSegment segment)
		{
			return Offset <= segment.Offset && segment.EndOffset <= EndOffsetIncludingDelimiter;
		}

		public static implicit operator TextSegment (DocumentLine line)
		{
			return line.Segment;
		}

		public override string ToString ()
		{
			return String.Format ("[DocumentLine: Offset={0}, Length={1}, DelimiterLength={2}]", Offset, LengthIncludingDelimiter, DelimiterLength);
		}
	}
}
