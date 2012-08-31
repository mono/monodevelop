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
using Mono.TextEditor.Highlighting;
using System.Linq;

namespace Mono.TextEditor
{
	/// <summary>
	/// A line inside a <see cref="T:Mono.TextEditor.TextDocument"/>.
	/// </summary>
	public abstract class DocumentLine : ICSharpCode.NRefactory.Editor.IDocumentLine
	{
		List<TextLineMarker> markers;

		public IEnumerable<TextLineMarker> Markers {
			get {
				return markers ?? Enumerable.Empty<TextLineMarker> ();
			}
		}

		public int MarkerCount {
			get {
				return markers != null ? markers.Count : 0;
			}
		}

		/// <summary>
		/// Gets the length of the line.
		/// </summary>
		/// <remarks>The length does not include the line delimeter.</remarks>
		public int Length {
			get {
				return LengthIncludingDelimiter - DelimiterLength;
			}
		}

		/// <summary>
		/// Gets the length of the line terminator.
		/// Returns 1 or 2; or 0 at the end of the document.
		/// </summary>
		public int DelimiterLength {
			get;
			set;
		}

		public bool WasChanged {
			get;
			set;
		}

		CloneableStack<Span> startSpan;
		static readonly CloneableStack<Span> EmptySpan = new CloneableStack<Span> ();

		public CloneableStack<Span> StartSpan {
			get {
				return startSpan ?? EmptySpan;
			}
			set {
				startSpan = value != null && value.Count == 0 ? null : value;
			}
		}

		/// <summary>
		/// Gets the start offset of the line.
		/// </summary>
		public abstract int Offset { get; set; }

		/// <summary>
		/// Gets the length of the line including the line delimiter.
		/// </summary>
		public int LengthIncludingDelimiter {
			get;
			set;
		}

		/// <summary>
		/// Gets the text segment of the line.
		/// </summary>
		/// <remarks>The text segment does not include the line delimeter.</remarks>
		public TextSegment Segment {
			get {
				return new TextSegment (Offset, Length);
			}
		}

		/// <summary>
		/// Gets the text segment of the line including the line delimiter.
		/// </summary>
		public TextSegment SegmentIncludingDelimiter {
			get {
				return new TextSegment (Offset, LengthIncludingDelimiter);
			}
		}

		/// <summary>
		/// Gets the end offset of the line.
		/// </summary>
		/// <remarks>The end offset does not include the line delimeter.</remarks>
		public int EndOffset {
			get {
				return Offset + Length;
			}
		}

		/// <summary>
		/// Gets the end offset of the line including the line delimiter.
		/// </summary>
		public int EndOffsetIncludingDelimiter {
			get {
				return Offset + LengthIncludingDelimiter;
			}
		}

		public bool IsBookmarked {
			get {
				if (markers == null)
					return false;
				return markers.Contains (BookmarkMarker.Instance);
			}
			set {
				if (value) {
					if (!IsBookmarked)
						AddMarker (BookmarkMarker.Instance);
				} else {
					if (markers != null)
						markers.Remove (BookmarkMarker.Instance);
				}
			}
		}

		/// <summary>
		/// Gets the number of this line.
		/// The first line has the number 1.
		/// </summary>
		public abstract int LineNumber { get; }

		/// <summary>
		/// Gets the next line. Returns null if this is the last line in the document.
		/// </summary>
		public abstract DocumentLine NextLine { get; }

		/// <summary>
		/// Gets the previous line. Returns null if this is the first line in the document.
		/// </summary>
		public abstract DocumentLine PreviousLine { get; }

		protected DocumentLine (int length, int delimiterLength)
		{
			LengthIncludingDelimiter          = length;
			DelimiterLength = delimiterLength;
		}

		internal void AddMarker (TextLineMarker marker)
		{
			if (markers == null)
				markers = new List<TextLineMarker> ();
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

		internal void RemoveMarker (TextLineMarker marker)
		{
			marker.LineSegment = null;
			if (markers == null)
				return;
			markers.Remove (marker);
			if (markers.Count == 0)
				markers = null;
		}

		internal TextLineMarker GetMarker (Type type)
		{
			if (markers == null)
				return null;
			return markers.Find (m => m.GetType () == type);
		}

		internal void RemoveMarker (Type type)
		{
			for (int i = 0; markers != null && i < markers.Count; i++) {
				if (markers[i].GetType () == type) {
					RemoveMarker (markers[i]);
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
		public string GetIndentation (TextDocument doc)
		{
			var result = new StringBuilder ();
			int offset = Offset;
			int max = System.Math.Min (offset + LengthIncludingDelimiter, doc.TextLength);
			for (int i = offset; i < max; i++) {
				char ch = doc.GetCharAt (i);
				if (ch != ' ' && ch != '\t')
					break;
				result.Append (ch);
			}
			return result.ToString ();
		}

		public int GetLogicalColumn (TextEditorData editor, int visualColumn)
		{
			int curVisualColumn = 1;
			int offset = Offset;
			int max = offset + Length;
			for (int i = offset; i < max; i++) {
				if (i < editor.Document.TextLength && editor.Document.GetCharAt (i) == '\t') {
					curVisualColumn = TextViewMargin.GetNextTabstop (editor, curVisualColumn);
				} else {
					curVisualColumn++;
				}
				if (curVisualColumn > visualColumn)
					return i - offset + 1;
			}
			return Length + (visualColumn - curVisualColumn) + 1;
		}

		public int GetVisualColumn (TextEditorData editor, int logicalColumn)
		{
			int result = 1;
			int offset = Offset;
			if (editor.Options.IndentStyle == IndentStyle.Virtual && Length == 0 && logicalColumn > DocumentLocation.MinColumn) {
				foreach (char ch in editor.GetIndentationString (Offset)) {
					if (ch == '\t') {
						result += editor.Options.TabSize;
						continue;
					}
					result++;
				}
				return result;
			}
			for (int i = 0; i < logicalColumn - 1; i++) {
				if (i < Length && editor.Document.GetCharAt (offset + i) == '\t') {
					result = TextViewMargin.GetNextTabstop (editor, result);
				} else {
					result++;
				}
			}
			return result;
		}

		/// <summary>
		/// Determines whether this line contains the specified offset. 
		/// </summary>
		/// <returns>
		/// <c>true</c> if this line contains the specified offset (upper bound exclusive); otherwise, <c>false</c>.
		/// </returns>
		/// <param name='offset'>
		/// The offset.
		/// </param>
		public bool Contains (int offset)
		{
			int o = Offset;
			return o <= offset && offset < o + LengthIncludingDelimiter;
		}

		/// <summary>
		/// Determines whether this line contains the specified segment. 
		/// </summary>
		/// <returns>
		/// <c>true</c> if this line contains the specified segment (upper bound inclusive); otherwise, <c>false</c>.
		/// </returns>
		/// <param name='segment'>
		/// The segment.
		/// </param>
		public bool Contains (TextSegment segment)
		{
			return Offset <= segment.Offset && segment.EndOffset <= EndOffsetIncludingDelimiter;
		}

		public static implicit operator TextSegment (DocumentLine line)
		{
			return line.Segment;
		}

		public override string ToString ()
		{
			return String.Format ("[DocumentLine: Offset={0}, Length={1}, DelimiterLength={2}, StartSpan={3}]", Offset, LengthIncludingDelimiter, DelimiterLength, StartSpan == null ? "null" : StartSpan.Count.ToString());
		}

		#region IDocumentLine implementation
		int ICSharpCode.NRefactory.Editor.IDocumentLine.TotalLength {
			get {
				return LengthIncludingDelimiter;
			}
		}

		ICSharpCode.NRefactory.Editor.IDocumentLine ICSharpCode.NRefactory.Editor.IDocumentLine.PreviousLine {
			get {
				return this.PreviousLine;
			}
		}

		ICSharpCode.NRefactory.Editor.IDocumentLine ICSharpCode.NRefactory.Editor.IDocumentLine.NextLine {
			get {
				return this.NextLine;
			}
		}

		bool ICSharpCode.NRefactory.Editor.IDocumentLine.IsDeleted {
			get {
				return false;
			}
		}
		#endregion
	}
}
