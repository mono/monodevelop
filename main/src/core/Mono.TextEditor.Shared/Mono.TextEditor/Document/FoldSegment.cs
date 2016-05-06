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
		internal bool isFolded;
		internal bool isAttached;
		public bool IsFolded {
			get {
				return isFolded;
			}
			set {
				if (isFolded != value) {
					isFolded = value;
					if (isAttached)
						doc.InformFoldChanged (new FoldSegmentEventArgs (this));
				}
			}
		}

		public HeightTree.FoldMarker Marker { get; set;}
		
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
		
		public DocumentLine StartLine { 
			get {
				return doc.GetLineByOffset (System.Math.Min (doc.TextLength, System.Math.Max (0, Offset)));
			} 
		}
		
		public DocumentLine EndLine {
			get {
				return doc.GetLineByOffset (System.Math.Min (doc.TextLength, System.Math.Max (0, EndOffset)));
			}
		}

		public bool IsInvalid {
			get {
				return Offset < 0;
			}
		}
		
		public FoldingType FoldingType { get; set; }
		TextDocument doc;
		
		public FoldSegment (TextDocument doc, string description, int offset, int length, FoldingType foldingType) : base (offset, length)
		{
			this.doc = doc;
			this.isFolded = false;
			this.Description = description;
			this.FoldingType = foldingType;
		}
		
		public FoldSegment (FoldSegment foldSegment) : base (foldSegment.Offset, foldSegment.Length)
		{
			this.doc = foldSegment.doc;
			this.isFolded = foldSegment.IsFolded;
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
