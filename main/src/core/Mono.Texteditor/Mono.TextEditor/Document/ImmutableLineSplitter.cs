//
// ImmutableLineSplitter.cs
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
using System.Linq;
using ICSharpCode.NRefactory;
using System.Collections.Generic;

namespace Mono.TextEditor
{
	class ImmutableLineSplitter : ILineSplitter
	{
		readonly LineSegment[] lines;

		sealed class LineSegment : DocumentLine
		{
			readonly ImmutableLineSplitter splitter;
			readonly int lineNumber;

			public override int Offset { get; set; }

			public override int LineNumber {
				get {
					return lineNumber + 1;
				}
			}

			public override DocumentLine NextLine {
				get {
					return splitter.Get (lineNumber + 1);
				}
			}

			public override DocumentLine PreviousLine {
				get {
					return splitter.Get (lineNumber - 1);
				}
			}

			public LineSegment (ImmutableLineSplitter splitter, int lineNumber, int offset, int length, UnicodeNewline newLine) : base(length, newLine)
			{
				this.splitter = splitter;
				this.lineNumber = lineNumber;
				Offset = offset;
			}
		}

		public ImmutableLineSplitter (ILineSplitter src)
		{
			if (src == null)
				throw new ArgumentNullException ("src");
			lines = new LineSegment[src.Count];
			int cur = 0;
			foreach (var line in src.Lines) {
				lines [cur] = new LineSegment (this, cur, line.Offset, line.LengthIncludingDelimiter, line.UnicodeNewline);
				cur++;
			}
		}

		#region ILineSplitter implementation

		public event EventHandler<LineEventArgs> LineChanged;

		public event EventHandler<LineEventArgs> LineInserted;

		public event EventHandler<LineEventArgs> LineRemoved;

		public void Clear ()
		{
		}

		public void Initalize (string text, out DocumentLine longestLine)
		{
			longestLine = null;
		}

		public DocumentLine Get (int number)
		{
			return lines [number - 1];
		}

		public DocumentLine GetLineByOffset (int offset)
		{
			return lines [OffsetToLineNumber (offset) - 1];
		}

		public int OffsetToLineNumber (int offset)
		{
			int min = 0;
			int max = lines.Length - 1;
			while (min <= max) {
				int mid = min >> 1 + max >> 1;
				if (offset < lines [mid].Offset) {
					max = mid - 1;
				} else if (offset > lines [mid].EndOffset) {
					min = mid + 1;
				} else {
					return mid + 1;
				}
			}
			return lines.Length;
		}

		public void TextReplaced (object sender, DocumentChangeEventArgs args)
		{
		}

		public void TextRemove (int offset, int length)
		{
		}

		public void TextInsert (int offset, string text)
		{
		}

		public IEnumerable<DocumentLine> GetLinesBetween (int startLine, int endLine)
		{
			for (int i = startLine; i <= endLine; i++)
				yield return Get (i);
		}

		public IEnumerable<DocumentLine> GetLinesStartingAt (int startLine)
		{
			for (int i = startLine; i <= Count; i++)
				yield return Get (i);
		}

		public IEnumerable<DocumentLine> GetLinesReverseStartingAt (int startLine)
		{
			for (int i = startLine; i-- > DocumentLocation.MinLine;)
				yield return Get (i);
		}

		public int Count {
			get {
				return lines.Length;
			}
		}

		public bool LineEndingMismatch {
			get;
			set;
		}

		public System.Collections.Generic.IEnumerable<DocumentLine> Lines {
			get {
				return lines;
			}
		}

		#endregion


	}
}

