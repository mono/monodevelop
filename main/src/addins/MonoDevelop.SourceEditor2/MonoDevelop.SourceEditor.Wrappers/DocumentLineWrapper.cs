//
// DocumentLineWrapper.cs
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

namespace MonoDevelop.SourceEditor.Wrappers
{
	class DocumentLineWrapper : MonoDevelop.Ide.Editor.IDocumentLine
	{
		public Mono.TextEditor.DocumentLine Line {
			get;
			private set;
		}

		public DocumentLineWrapper (Mono.TextEditor.DocumentLine line)
		{
			if (line == null)
				throw new ArgumentNullException ("line");
			this.Line = line;
		}

		#region IDocumentLine implementation
		int MonoDevelop.Ide.Editor.IDocumentLine.LengthIncludingDelimiter {
			get {
				return Line.LengthIncludingDelimiter;
			}
		}

		int MonoDevelop.Ide.Editor.IDocumentLine.EndOffsetIncludingDelimiter {
			get {
				return Line.EndOffsetIncludingDelimiter;
			}
		}

		MonoDevelop.Core.Text.ISegment MonoDevelop.Ide.Editor.IDocumentLine.SegmentIncludingDelimiter {
			get {
				return MonoDevelop.Core.Text.TextSegment.FromBounds (Line.Offset, Line.EndOffsetIncludingDelimiter);
			}
		}

		MonoDevelop.Core.Text.UnicodeNewline MonoDevelop.Ide.Editor.IDocumentLine.UnicodeNewline {
			get {
				return (MonoDevelop.Core.Text.UnicodeNewline)Line.UnicodeNewline;
			}
		}

		int MonoDevelop.Ide.Editor.IDocumentLine.DelimiterLength {
			get {
				return Line.DelimiterLength;
			}
		}

		int MonoDevelop.Ide.Editor.IDocumentLine.LineNumber {
			get {
				return Line.LineNumber;
			}
		}

		MonoDevelop.Ide.Editor.IDocumentLine MonoDevelop.Ide.Editor.IDocumentLine.PreviousLine {
			get {
				var prev = Line.PreviousLine;
				return prev != null ? new DocumentLineWrapper (prev) : null;
			}
		}

		MonoDevelop.Ide.Editor.IDocumentLine MonoDevelop.Ide.Editor.IDocumentLine.NextLine {
			get {
				var next = Line.NextLine;
				return next != null ? new DocumentLineWrapper (next) : null;
			}
		}

		bool MonoDevelop.Ide.Editor.IDocumentLine.IsDeleted {
			get {
				return false;
			}
		}
		#endregion

		#region ISegment implementation
		int MonoDevelop.Core.Text.ISegment.Offset {
			get {
				return Line.Offset;
			}
		}

		int MonoDevelop.Core.Text.ISegment.Length {
			get {
				return Line.Length;
			}
		}

		int MonoDevelop.Core.Text.ISegment.EndOffset {
			get {
				return Line.EndOffset;
			}
		}
		#endregion
	}
}