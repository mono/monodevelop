// 
// DocumentRegion.cs
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
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	/// <summary>
	/// An (Begin, End) pair representing a document span. It's a TextSegment working with lines &amp; columns instead of offsets.
	/// </summary>
	struct DocumentRegion : IEquatable<DocumentRegion>
	{
		public static readonly DocumentRegion Empty = new DocumentRegion (0, 0, 0, 0);

		public bool IsEmpty {
			get {
				return beginLine < 1;
			}
		}

		readonly int beginLine;
		public int BeginLine {
			get {
				return beginLine;
			}
		}

		readonly int beginColumn;
		public int BeginColumn {
			get {
				return beginColumn;
			}
		}

		readonly int endLine;
		public int EndLine {
			get {
				return endLine;
			}
		}

		readonly int endColumn;
		public int EndColumn {
			get {
				return endColumn;
			}
		}

		public DocumentLocation Begin {
			get {
				return new DocumentLocation (BeginLine, BeginColumn);
			}
		}
		
		public DocumentLocation End {
			get {
				return new DocumentLocation (EndLine, EndColumn);
			}
		}

		public DocumentRegion (int beginLine, int beginColumn, int endLine, int endColumn)
		{
			this.beginLine = beginLine;
			this.beginColumn = beginColumn;
			this.endLine = endLine;
			this.endColumn = endColumn;
		}
		
		public DocumentRegion (DocumentLocation begin, DocumentLocation end)
		{
			beginLine = begin.Line;
			beginColumn = begin.Column;
			endLine = end.Line;
			endColumn = end.Column;
		}

		public bool Contains (DocumentLocation location)
		{
			return Begin <= location && location < End;
		}
		
		public bool Contains (int line, int column)
		{
			return Contains (new DocumentLocation (line, column));
		}

		public override bool Equals (object obj)
		{
			return obj is DocumentRegion && Equals ((DocumentRegion)obj);
		}

		public override int GetHashCode ()
		{
			return unchecked (Begin.GetHashCode () ^ End.GetHashCode ());
		}

		public bool Equals (DocumentRegion other)
		{
			return Begin == other.Begin && End == other.End;
		}

		public static bool operator == (DocumentRegion left, DocumentRegion right)
		{
			return left.Equals(right);
		}
		
		public static bool operator != (DocumentRegion left, DocumentRegion right)
		{
			return !left.Equals(right);
		}

		public ISegment GetSegment (TextDocument document)
		{
			if (document == null)
				throw new System.ArgumentNullException ("document");
			var begin = document.LocationToOffset (Begin);
			var end = document.LocationToOffset (End);
			return new TextSegment (begin, end - begin);
		}

		public override string ToString ()
		{
			return string.Format ("[DocumentRegion: Begin={0}, End={1}]", Begin, End);
		}
	}
}

