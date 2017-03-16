//
// LazyLineSplitter.cs
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
using System.Collections.Generic;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	class LazyLineSplitter : ILineSplitter
	{
		internal TextDocument src;
		LineSegment[] lines;

		sealed class LineSegment : DocumentLine
		{
			readonly LazyLineSplitter splitter;
			readonly int lineNumber;

			public override int Offset { get; set; }

			public override int LineNumber {
				get {
					return lineNumber + 1;
				}
			}

			public override DocumentLine NextLine {
				get {
					if (lineNumber + 2 >= splitter.Count)
						return null;
					return splitter.Get (lineNumber + 2);
				}
			}

			public override DocumentLine PreviousLine {
				get {
					if (lineNumber == 0)
						return null;
					return splitter.Get (lineNumber);
				}
			}

			public LineSegment (LazyLineSplitter splitter, int lineNumber, int offset, int length, UnicodeNewline newLine) : base(length, newLine)
			{
				this.splitter = splitter;
				this.lineNumber = lineNumber;
				Offset = offset;
			}
			public override string ToString ()
			{
				return string.Format ("[LineSegment: lineNumber={0}, Offset={1}]", lineNumber, Offset);
			}
		}

		void EnsureBuild()
		{
			if (this.lines != null)
				return;
			var text = src.Text;
			var nodes = new List<LineSegment> ();

			var delimiterType = UnicodeNewline.Unknown;
			int offset = 0, maxLength = 0, lineNumber = 0;
			while (true) {
				var delimiter = LineSplitter.NextDelimiter (text, offset);
				if (delimiter.IsInvalid)
					break;
				int delimiterEndOffset = delimiter.Offset + delimiter.Length;
				var length = delimiterEndOffset - offset;
				var newLine = new LineSegment (this, lineNumber++, offset, length, delimiter.UnicodeNewline);
				nodes.Add (newLine);
				if (length > maxLength) {
					maxLength = length;
				}
				if (offset > 0) {
					LineEndingMismatch |= delimiterType != delimiter.UnicodeNewline;
				} else {
					delimiterType = delimiter.UnicodeNewline;
				}
				offset = delimiterEndOffset;
			}
			var lastLine = new LineSegment (this, lineNumber++, offset, text.Length - offset, UnicodeNewline.Unknown);
			nodes.Add (lastLine);
			this.lines = nodes.ToArray ();
		}

		public LazyLineSplitter (int lineCount)
		{
			this.Count = lineCount;
		}

		#region ILineSplitter implementation
		public void Clear ()
		{
		}

		public void Initalize (string text, out DocumentLine longestLine)
		{
			longestLine = null;
		}

		public DocumentLine Get (int number)
		{
			EnsureBuild ();
			return lines [number - 1];
		}

		public DocumentLine GetLineByOffset (int offset)
		{
			EnsureBuild ();
			return lines [OffsetToLineNumber (offset) - 1];
		}

		public int OffsetToLineNumber (int offset)
		{
			EnsureBuild ();
			int min = 0;
			int max = lines.Length - 1;
			while (min <= max) {
				int mid = (min + max) / 2;
				var middleLine = lines [mid];
				if (offset < middleLine.Offset) {
					max = mid - 1;
				} else if (offset > middleLine.EndOffset) {
					min = mid + 1;
				} else {
					return mid + 1;
				}
			}
			return lines.Length;
		}

		public void TextReplaced (object sender, TextChangeEventArgs args)
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
			get;
			private set;
		}

		public bool LineEndingMismatch {
			get;
			set;
		}

		public System.Collections.Generic.IEnumerable<DocumentLine> Lines {
			get {
				EnsureBuild ();
				return lines;
			}
		}

		#endregion
	}
}