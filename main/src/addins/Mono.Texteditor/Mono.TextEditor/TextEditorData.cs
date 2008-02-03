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
		SelectionMarker selectionStart;
		SelectionMarker selectionEnd;
		Adjustment hadjustment = new Adjustment (0, 0, 0, 0, 0, 0); 
		Adjustment vadjustment = new Adjustment (0, 0, 0, 0, 0, 0);
		Document   document; 
		Caret      caret;

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
		
		public bool IsSomethingSelected {
			get {
				return SelectionStart != null && SelectionEnd != null; 
			}
		}
		
		public ISegment SelectionRange {
			get {
				if (!IsSomethingSelected)
					return null;
				SelectionMarker start;
				SelectionMarker end;
				 
				if (SelectionStart.Segment.Offset < SelectionEnd.Segment.Offset || SelectionStart.Segment.Offset == SelectionEnd.Segment.Offset && SelectionStart.Column < SelectionEnd.Column) {
					start = SelectionStart;
					end   = SelectionEnd;
				} else {
					start = SelectionEnd;
					end   = SelectionStart;
				}
				
				int startOffset = start.Segment.Offset + start.Column;
				int endOffset   = end.Segment.Offset + end.Column;
				return new Segment (startOffset, endOffset - startOffset);
			}
			set {
				int start, end;
				if (value.Offset < value.EndOffset) {
					start = value.Offset;
					end   = value.EndOffset;
				} else {
					start = value.EndOffset;
					end   = value.Offset;
				}
				LineSegment startLine = document.Splitter.GetByOffset (start);
				LineSegment endLine   = document.Splitter.GetByOffset (end);
				this.Caret.Offset = end;
				selectionStart = new SelectionMarker (startLine, start - startLine.Offset);
				selectionEnd   = new SelectionMarker (endLine, end - endLine.Offset);
				OnSelectionChanged (EventArgs.Empty);				
			}
		}
		
		public string SelectedText {
			get {
				if (!IsSomethingSelected)
					return null;
				return this.document.Buffer.GetTextAt (this.SelectionRange);
			}
		}
		
		public IEnumerable<LineSegment> SelectedLines {
			get {
				int startLineNr = Document.Splitter.GetLineNumberForOffset (SelectionRange.Offset);
				RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = this.document.Splitter.Get (startLineNr).Iter;
				do {
					if (iter.Current == Document.Splitter.GetByOffset (SelectionRange.EndOffset) && (iter.Current.Offset == Caret.Offset ||
					                                iter.Current.Offset == SelectionRange.EndOffset))
						break;
					yield return iter.Current;
					if (iter.Current == Document.Splitter.GetByOffset (SelectionRange.EndOffset))
						break;
				} while (iter.MoveNext ());
			}
		}
		
		public Adjustment HAdjustment {
			get {
				return hadjustment;
			}
			set {
				hadjustment = value;
			}
		}

		public Adjustment VAdjustment {
			get {
				return vadjustment;
			}
			set {
				vadjustment = value;
			}
		}

		public SelectionMarker SelectionStart {
			get {
				return selectionStart;
			}
			set {
				selectionStart = value;
				OnSelectionChanged (EventArgs.Empty);
			}
		}

		public SelectionMarker SelectionEnd {
			get {
				return selectionEnd;
			}
			set {
				selectionEnd = value;
				OnSelectionChanged (EventArgs.Empty);
			}
		}
		
		public void ClearSelection ()
		{
			this.selectionStart = this.selectionEnd = null;
			OnSelectionChanged (EventArgs.Empty);
		}
		
		public void DeleteSelectedText ()
		{
			if (!IsSomethingSelected)
				return;
			ISegment selection = SelectionRange;
			if (Caret.Offset > selection.Offset)
				Caret.Offset -= selection.Length;
			Document.Buffer.Remove (selection.Offset, selection.Length);
			ClearSelection();
		}
		
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged (EventArgs args)
		{
			if (SelectionChanged != null) 
				SelectionChanged (this, args);
		}
	}
}
