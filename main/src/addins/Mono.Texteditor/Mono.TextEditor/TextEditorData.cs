// TextEditorData.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Gtk;

namespace Mono.TextEditor
{
	public class TextEditorData
	{
		Document   document; 
		Caret      caret;
		
		Adjustment hadjustment = new Adjustment (0, 0, 0, 0, 0, 0); 
		public Adjustment HAdjustment {
			get {
				return hadjustment;
			}
			set {
				hadjustment = value;
			}
		}
		
		Adjustment vadjustment = new Adjustment (0, 0, 0, 0, 0, 0);
		public Adjustment VAdjustment {
			get {
				return vadjustment;
			}
			set {
				vadjustment = value;
			}
		}
		
		
		string searchPattern = "";
		public string SearchPattern {
			get {
				return searchPattern;
			}
			set {
				searchPattern = value;
			}
		}
		
		public TextEditorData ()
		{
			Document = new Document ();
		}
		
		public Document Document {
			get {
				return document;
			}
			set {
				this.document = value;
				caret = new Caret (document);
				caret.PositionChanged += delegate {
					if (!caret.PreserveSelection)
						this.ClearSelection ();
				};
			}
		}
		
		public Mono.TextEditor.Caret Caret {
			get {
				return caret;
			}
		}
		
		Mono.TextEditor.Highlighting.Style colorStyle;
		public Mono.TextEditor.Highlighting.Style ColorStyle {
			get {
				return colorStyle;
			}
			set {
				colorStyle = value;
			}
		}
		
		#region Selection management
		int      selectionAnchor = -1;
		ISegment selectionRange  = null;
		
		public bool IsSomethingSelected {
			get {
				return selectionRange != null; 
			}
		}
		
		public bool IsMultiLineSelection {
			get {
				return IsSomethingSelected && document.OffsetToLineNumber (selectionRange.Offset) != document.OffsetToLineNumber (selectionRange.EndOffset);
			}
		}
		
		public ISegment SelectionRange {
			get {
				return selectionRange;
			}
			set {
				selectionRange = value;
				OnSelectionChanged (EventArgs.Empty);
			}
//			get {
//				if (!IsSomethingSelected)
//					return null;
//				SelectionMarker start;
//				SelectionMarker end;
//				 
//				if (SelectionAnchor.Segment.Offset < SelectionEnd.Segment.Offset || SelectionAnchor.Segment.Offset == SelectionEnd.Segment.Offset && SelectionAnchor.Column < SelectionEnd.Column) {
//					start = SelectionAnchor;
//					end   = SelectionEnd;
//				} else {
//					start = SelectionEnd;
//					end   = SelectionAnchor;
//				}
//				
//				int startOffset = start.Segment.Offset + start.Column;
//				int endOffset   = end.Segment.Offset + end.Column;
//				return new Segment (startOffset, endOffset - startOffset);
//			}
//			set {
//				if (value == null) {
//					ClearSelection ();
//					return;
//				}
//				int start, end;
//				if (value.Offset < value.EndOffset) {
//					start = value.Offset;
//					end   = value.EndOffset;
//				} else {
//					start = value.EndOffset;
//					end   = value.Offset;
//				}
//				LineSegment startLine = Document.GetLineByOffset (start);
//				LineSegment endLine   = Document.GetLineByOffset (end);
//				this.Caret.Offset = end;
//				selectionStart = new SelectionMarker (startLine, start - startLine.Offset);
//				selectionEnd   = new SelectionMarker (endLine, end - endLine.Offset);
//				OnSelectionChanged (EventArgs.Empty);				
//			}
		}
		
		public string SelectedText {
			get {
				if (!IsSomethingSelected)
					return null;
				return this.Document.GetTextAt (this.SelectionRange);
			}
			set {
				if (!IsSomethingSelected)
					return;
				ISegment selection = this.SelectionRange;
				Document.Replace (selection.Offset, selection.Length, value);
				if (this.Caret.Offset > selection.Offset)
					this.Caret.Offset   = selection.Offset + value.Length;
				this.SelectionRange = new Segment(selection.Offset, value.Length);
			}
		}
		
		public IEnumerable<LineSegment> SelectedLines {
			get {
				if (!this.IsSomethingSelected) {
					yield return this.document.GetLine (this.caret.Line);
				} else {
					int startLineNr = Document.OffsetToLineNumber (SelectionRange.Offset);
					RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = this.document.GetLine (startLineNr).Iter;
					do {
						if (iter.Current == Document.GetLineByOffset (SelectionRange.EndOffset) && (iter.Current.Offset == Caret.Offset ||
						                                                                                 iter.Current.Offset == SelectionRange.EndOffset))
							break;
						yield return iter.Current;
						if (iter.Current == Document.GetLineByOffset (SelectionRange.EndOffset))
							break;
					} while (iter.MoveNext ());
				}
			}
		}
		
		public int SelectionAnchor {
			get {
				return selectionAnchor;
			}
			set {
				selectionAnchor = value;
			}
		}
		
		public void ClearSelection ()
		{
			this.selectionAnchor = -1;
			this.selectionRange  = null;
			OnSelectionChanged (EventArgs.Empty);
		}
		
		public void ExtendSelectionTo (DocumentLocation location)
		{
			ExtendSelectionTo (document.LocationToOffset (location));
		}
		
		public void ExtendSelectionTo (int offset)
		{
			if (selectionAnchor < 0)
				selectionAnchor = offset;
			int from, to;
			if (offset < selectionAnchor) {
				from = offset;
				to   = selectionAnchor;
			} else {
				to   = offset;
				from = selectionAnchor;
			}
			this.SelectionRange = new Segment (from, to - from);
		}
		
		public void SetSelectLines (int from, int to)
		{
			if (to < from) {
				int tmp = from;
				from = to;
				to = tmp;
			}
			LineSegment fromLine =Document.GetLine (from);
			LineSegment toLine = Document.GetLine (to);
			SelectionRange = new Segment (fromLine.Offset, toLine.EndOffset - fromLine.Offset);
		}
				
		
		public void DeleteSelectedText ()
		{
			if (!IsSomethingSelected)
				return;
			document.BeginAtomicUndo ();
			ISegment selection = SelectionRange;
			bool needUpdate = Document.OffsetToLineNumber (selection.Offset) != Document.OffsetToLineNumber (selection.EndOffset);
			if (Caret.Offset > selection.Offset)
				Caret.Offset -= selection.Length;
			
			Document.Remove (selection.Offset, selection.Length);
			if (needUpdate)
				Document.RequestUpdate (new LineToEndUpdate (Document.OffsetToLineNumber (selection.Offset)));
			ClearSelection();
			document.EndAtomicUndo ();
			if (needUpdate)
				Document.CommitDocumentUpdate ();
		}
#endregion
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged (EventArgs args)
		{
			if (SelectionChanged != null) 
				SelectionChanged (this, args);
		}
	}
}
