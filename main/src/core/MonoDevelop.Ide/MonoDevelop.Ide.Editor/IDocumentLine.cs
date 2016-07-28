//
// IDocumentLine.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;
using System.Text;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// A line inside a <see cref="ITextDocument"/>.
	/// </summary>
	public interface IDocumentLine : ISegment
	{
		/// <summary>
		/// Gets the length of the line including the line delimiter.
		/// </summary>
		int LengthIncludingDelimiter {
			get;
		}

		int EndOffsetIncludingDelimiter {
			get;
		}

		/// <summary>
		/// Gets the text segment of the line including the line delimiter.
		/// </summary>
		ISegment SegmentIncludingDelimiter {
			get;
		}

		/// <summary>
		/// Gets the unicode newline for this line. Returns UnicodeNewline.Unknown for no new line (in the last line of the document)
		/// </summary>EndOffsetIncludingDelimiterEndOffsetIncludingDelimiter
		UnicodeNewline UnicodeNewline {
			get;
		}

		/// <summary>
		/// Gets the length of the line terminator.
		/// Returns 1 or 2; or 0 at the end of the document.
		/// </summary>
		int DelimiterLength { get; }

		/// <summary>
		/// Gets the number of this line.
		/// The first line has the number 1.
		/// </summary>
		int LineNumber { get; }

		/// <summary>
		/// Gets the previous line. Returns null if this is the first line in the document.
		/// </summary>
		IDocumentLine PreviousLine { get; }

		/// <summary>
		/// Gets the next line. Returns null if this is the last line in the document.
		/// </summary>
		IDocumentLine NextLine { get; }
	}

	public static class DocumentLineExt
	{
		/// <summary>
		/// This method gets the line indentation.
		/// </summary>
		/// <param name = "line"></param>
		/// <param name="doc">
		/// The <see cref="IReadonlyTextDocument"/> the line belongs to.
		/// </param>
		/// <returns>
		/// The indentation of the line (all whitespace chars up to the first non ws char).
		/// </returns>
		public static string GetIndentation (this IDocumentLine line, IReadonlyTextDocument  doc)
		{
			if (line == null)
				throw new ArgumentNullException (nameof (line));
			if (doc == null)
				throw new ArgumentNullException (nameof (doc));
			var result = new StringBuilder ();
			int offset = line.Offset;
			int max = Math.Min (offset + line.LengthIncludingDelimiter, doc.Length);
			for (int i = offset; i < max; i++) {
				char ch = doc.GetCharAt (i);
				if (ch != ' ' && ch != '\t')
					break;
				result.Append (ch);
			}
			return result.ToString ();
		}
	}
}

