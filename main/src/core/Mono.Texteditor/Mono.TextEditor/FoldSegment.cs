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
	public class FoldSegment : TreeSegment, System.IComparable
	{
		bool isFolded;
		public bool IsFolded {
			get {
				return isFolded;
			}
			set {
				isFolded = value;
				doc.InformFoldChanged (new FoldSegmentEventArgs (this));
			}
		}
		
		public string Description { get; set; }
		
		public int Column { get; set; }
		public int EndColumn { get; set; }

	/*	public override int Offset {
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
		}*/
		
		public LineSegment StartLine { 
			get {
				return doc.GetLineByOffset (Offset);
			} 
		}
		
		public LineSegment EndLine {
			get {
				return doc.GetLineByOffset (EndOffset);
			}
		}
		
		public FoldingType FoldingType { get; set; }
		Document doc;
		
		public FoldSegment (Document doc,string description,int offset,int length,FoldingType foldingType) : base (offset, length)
		{
			this.doc = doc;
			this.IsFolded = false;
			this.Description = description;
			this.FoldingType = foldingType;
		}
		
		public FoldSegment (FoldSegment foldSegment) : base (foldSegment.Offset, foldSegment.Length)
		{
			this.doc = foldSegment.doc;
			this.IsFolded = foldSegment.IsFolded;
			this.Description = foldSegment.Description;
			this.FoldingType = foldSegment.FoldingType;
		}
		
		public override string ToString()
		{
			return string.Format("[FoldSegment: IsFolded={0}, Description={1}, Column={2}, Offset={3}, Length={4}, StartLine={5}, EndLine={6}, EndColumn={7}, FoldingType={8}]", IsFolded, Description, Column, Offset, Length, StartLine, EndLine, EndColumn, FoldingType);
		}
		
		public int CompareTo (object obj)
		{
			FoldSegment segment = (FoldSegment)obj;
			return this.Offset != segment.Offset ? this.Offset.CompareTo (segment.Offset) : 0;
		}
	}
	
	[Serializable]
	public sealed class FoldSegmentEventArgs : EventArgs
	{
		public FoldSegment FoldSegment {
			get;
			set;
		}
		
		public FoldSegmentEventArgs (FoldSegment foldSegment)
		{
			this.FoldSegment = foldSegment;
		}
	}
	
}
