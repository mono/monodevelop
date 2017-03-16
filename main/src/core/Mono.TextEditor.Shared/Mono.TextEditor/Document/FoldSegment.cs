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
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	class FoldSegment : TreeSegment, System.IComparable, IFoldSegment
	{
		internal bool isFolded;
		internal bool isAttached;
		public bool IsCollapsed {
			get {
				return isFolded;
			}
			set {
				isFolded = value;
			}
		}

		public HeightTree.FoldMarker Marker { get; set;}
		
		public string CollapsedText { get; set; }
		
		public DocumentLine GetStartLine (TextDocument doc)
        { 
			return doc.GetLineByOffset (System.Math.Min (doc.Length, System.Math.Max (0, Offset)));
		}
		
		public DocumentLine GetEndLine (TextDocument doc)
        {
			return doc.GetLineByOffset (System.Math.Min (doc.Length, System.Math.Max (0, EndOffset)));
		}

		public bool IsInvalid {
			get {
				return Offset < 0;
			}
		}
		
		public FoldingType FoldingType { get; set; }
		
		public FoldSegment (string description, int offset, int length, FoldingType foldingType) : base (offset, length)
		{
			this.isFolded = false;
			this.CollapsedText = description;
			this.FoldingType = foldingType;
		}
		
		public FoldSegment (FoldSegment foldSegment) : base (foldSegment.Offset, foldSegment.Length)
		{
			this.isFolded = foldSegment.IsCollapsed;
			this.CollapsedText = foldSegment.CollapsedText;
			this.FoldingType = foldSegment.FoldingType;
		}
		
		public override string ToString()
		{
			return string.Format("[FoldSegment: IsFolded={0}, Description={1}, Offset={2}, Length={3}, FoldingType={4}]", IsCollapsed, CollapsedText, Offset, Length, FoldingType);
		}
		
		public int CompareTo (object obj)
		{
			FoldSegment segment = (FoldSegment)obj;
			return this.Offset != segment.Offset ? this.Offset.CompareTo (segment.Offset) : 0;
		}
	}
	
	[Serializable]
	sealed class FoldSegmentEventArgs : EventArgs
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
