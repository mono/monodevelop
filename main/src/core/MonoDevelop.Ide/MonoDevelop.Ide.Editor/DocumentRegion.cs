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
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// An (Begin, End) pair representing a document span. It's a TextSegment working with lines &amp; columns instead of offsets.
	/// </summary>
	public readonly struct DocumentRegion : IEquatable<DocumentRegion>
	{
		public static readonly DocumentRegion Empty = new DocumentRegion (0, 0, 0, 0);

		/// <summary>
		/// Gets a value indicating whether this DocumentRegion is empty.
		/// </summary>
		public bool IsEmpty {
			get {
				return BeginLine < 1;
			}
		}
		public int BeginLine { get; }
		public int BeginColumn { get; }
		public int EndLine { get; }
		public int EndColumn { get; }

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
			this.BeginLine = beginLine;
			this.BeginColumn = beginColumn;
			this.EndLine = endLine;
			this.EndColumn = endColumn;
		}
		
		public DocumentRegion (DocumentLocation begin, DocumentLocation end)
		{
			BeginLine = begin.Line;
			BeginColumn = begin.Column;
			EndLine = end.Line;
			EndColumn = end.Column;
		}

		public bool Contains (DocumentLocation location)
		{
			return Begin <= location && location < End;
		}
		
		public bool Contains (int line, int column)
		{
			return Contains (new DocumentLocation (line, column));
		}

		public bool IsInside (DocumentLocation location)
		{
			return Begin <= location && location <= End;
		}

		public bool IsInside (int line, int column)
		{
			return IsInside (new DocumentLocation (line, column));
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

		public TextSegment GetSegment (TextEditor document)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			var begin = document.LocationToOffset (Begin);
			var end = document.LocationToOffset (End);
			return new TextSegment (begin, end - begin);
		}

		public static implicit operator Microsoft.CodeAnalysis.Text.LinePositionSpan (DocumentRegion location)
		{
			return new Microsoft.CodeAnalysis.Text.LinePositionSpan (location.Begin, location.End);
		}

		public static implicit operator DocumentRegion(Microsoft.CodeAnalysis.Text.LinePositionSpan location)
		{
			return new DocumentRegion (location.Start, location.End);
		}


		public static implicit operator DocumentRegion(FileLinePositionSpan location)
		{
			return new DocumentRegion (location.StartLinePosition, location.EndLinePosition);
		}


		public override string ToString ()
		{
			return string.Format ("[DocumentRegion: Begin={0}, End={1}]", Begin, End);
		}
	}
}

