// LineSegment.cs
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
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mono.TextEditor
{	
	public class LineSegment : ISegment, IDisposable
	{
		int length;		
		int delimiterLength;
		public bool IsBookmarked = false;
		List<TextMarker> markers = null;
		RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeNode treeNode;
		
		public RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator Iter {
			get {
				return new RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator (treeNode);
			}
		}
		
		public ReadOnlyCollection<TextMarker> Markers {
			get {
				return markers != null ? markers.AsReadOnly () : null;
			}
		}
				
		public int EditableLength {
			get {
				return Length - delimiterLength;
			}
		}
		
		public int DelimiterLength {
			get {
				return delimiterLength;
			}
			set {
				delimiterLength = value;
			}
		}
		
		public int Offset {
			get {
				return treeNode != null ? LineSegmentTree.GetOffsetFromNode (treeNode) : -1;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		Mono.TextEditor.Highlighting.Span[] startSpan = null;
		public Highlighting.Span[] StartSpan {
			get {
				return startSpan;
			}
			set {
				startSpan = value != null && value.Length == 0 ? null : value;
			}
		}

		public int Length {
			get {
				return length;
			}
			set {
				length = value;
			}
		}
		
		public int EndOffset {
			get {
				return Offset + Length;
			}
		}

		internal RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeNode TreeNode {
			get {
				return treeNode;
			}
			set {
				treeNode = value;
			}
		}

		public LineSegment (int length, int delimiterLength)
		{
			this.length          = length;
			this.delimiterLength = delimiterLength;
		}
		
		public void AddMarker (TextMarker marker)
		{
			if (markers == null)
				markers = new List<TextMarker> ();
			marker.LineSegment = this;
			markers.Add (marker);
		}
		
		public void ClearMarker ()
		{
			if (markers != null) {
				markers.Clear ();
				markers = null;
			}
		}
		
		public void RemoveMarker (TextMarker marker)
		{
			marker.LineSegment = null;
			if (markers == null)
				return;
			markers.Remove (marker);
			if (markers.Count == 0)
				markers = null;
		}
		
		public void RemoveMarker (Type type)
		{
			if (markers == null)
				return;
			for (int i = 0; i < markers.Count; i++) {
				if (markers[i].GetType () == type) {
					RemoveMarker (markers[i]);
					if (markers == null)
						return;
					i--;
				}
			}
		}
		
		/// <summary>
		/// This method gets the line indentation.
		/// </summary>
		/// <param name="doc">
		/// The <see cref="Document"/> the line belongs to.
		/// </param>
		/// <returns>
		/// The indentation of the line (all whitespace chars up to the first non ws char).
		/// </returns>
		public string GetIndentation (Document doc)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = Offset; i < this.EndOffset; i++) {
				char ch = doc.GetCharAt (i);
				if (!char.IsWhiteSpace (ch))
					break;
				result.Append (ch);
			}
			return result.ToString ();
		}
		
		public int GetLogicalColumn (IBuffer doc, int visualColumn)
		{
			int curVisualColumn = 0;
			for (int i = 0; i < EditableLength; i++) {
				if (doc.GetCharAt (Offset + i) == '\t') {
					curVisualColumn = TextViewMargin.GetNextTabstop (curVisualColumn);
				} else {
					curVisualColumn++;
				}
				if (curVisualColumn > visualColumn)
					return i;
			}
			return EditableLength;
		}
		
		public int GetVisualColumn (IBuffer doc, int logicalColumn)
		{
			int result = 0;
			for (int i = 0; i < logicalColumn; i++) {
				if (i < EditableLength && doc.GetCharAt (Offset + i) == '\t') {
					result = TextViewMargin.GetNextTabstop (result);
				} else {
					result++;
				}
			}
			return result;
		}
		
		
		public bool Contains (int offset)
		{
			return Offset <= offset && offset < EndOffset;
		}
		public bool Contains (ISegment segment)
		{
			return segment != null && Offset <= segment.Offset && segment.EndOffset <= EndOffset;
		}
		
		public override string ToString ()
		{
			return String.Format ("[LineSegment: Offset={0}, Length={1}, DelimiterLength={2}, StartSpan={3}]", this.Offset, this.Length, this.DelimiterLength, StartSpan == null ? "null" : StartSpan.Length.ToString());
		}
		
		public void Dispose ()
		{
			this.startSpan = null;
			if (markers != null) {
				markers.Clear ();
				markers = null;
			}
			treeNode = null;
		}
	}
}
