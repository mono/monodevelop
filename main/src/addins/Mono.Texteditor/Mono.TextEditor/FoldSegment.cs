// FoldSegment.cs
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

namespace Mono.TextEditor
{
	public class FoldSegment : Segment, System.IComparable
	{
		bool   isFolded = false;
		string description;
		int         column;
		int         endColumn;
		FoldingType foldingType;
		LineSegment startLine;
		LineSegment endLine;
		
		public bool IsFolded {
			get {
				return isFolded;
			}
			set {
				isFolded = value;
			}
		}

		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}

		public int Column {
			get {
				return column;
			}
			set {
				column = value;
			}
		}
		
		public override int Offset {
			get {
				return StartLine != null ? StartLine.Offset + Column : base.Offset;
			}
			set {
				base.Offset = value;
			}
		}
		
		public override int Length {
			get {
				return EndLine != null ? EndLine.Offset + EndColumn - Offset : base.Length;
			}
			set {
				base.Length = value;
			}
		}
		
		
		public LineSegment StartLine {
			get {
				return startLine;
			}
			set {
				startLine = value;
			}
		}

		public LineSegment EndLine {
			get {
				return endLine;
			}
			set {
				endLine = value;
			}
		}

		public int EndColumn {
			get {
				return endColumn;
			}
			set {
				endColumn = value;
			}
		}

		public FoldingType FoldingType {
			get {
				return foldingType;
			}
			set {
				foldingType = value;
			}
		}
		
		public FoldSegment (string description, int offset, int length, FoldingType foldingType) : base (offset, length)
		{
			this.description = description;
			this.foldingType = foldingType;
		}
		
		public override string ToString ()
		{
			return String.Format ("[FoldSegment: Description= {0}, Offset={1}, Length={2}]", this.description, this.Offset, this.Length);
		}
		
		public int CompareTo (object obj)
		{
			FoldSegment segment = (FoldSegment)obj;
			
			return this.Offset != segment.Offset ? this.Offset.CompareTo (segment.Offset) : 0;
		}
	}
}
