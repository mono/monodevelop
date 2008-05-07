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
	public class TextEditorData : IDisposable
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
		
		public TextEditorData ()
		{
			Document = new Document ();
			this.SearchEngine = new BasicSearchEngine ();
		}
		
		public Document Document {
			get {
				return document;
			}
			set {
				this.document = value;
				caret = new Caret (document);
				caret.PositionChanged += CaretPositionChanged;
				this.document.BeginUndo += OnBeginUndo;
				this.document.EndUndo += OnEndUndo;
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
		
		public void InsertAtCaret (string text)
		{
			if (String.IsNullOrEmpty (text))
				return;
			Document.BeginAtomicUndo ();
			Document.Insert (Caret.Offset, text);
			Caret.Offset += text.Length;
			Document.EndAtomicUndo ();
		}
		
		public void Dispose ()
		{
			if (this.states != null) {
				foreach (IDisposable disposeable in this.states) {
					disposeable.Dispose ();
				}
				this.states = null;
			}
			
			if (document != null) {
				document.BeginUndo -= OnBeginUndo;
				document.EndUndo   -= OnEndUndo;
				// DOCUMENT MUST NOT BE DISPOSED !!! (Split View shares document)
				document = null;
			}
			if (caret != null) {
				caret.PositionChanged -= CaretPositionChanged;
				caret.Dispose ();
				caret = null;
			}
		}
		
		void CaretPositionChanged (object sender, EventArgs args)
		{
			if (!caret.PreserveSelection)
				this.ClearSelection ();
		}
		
		#region undo/redo handling
		int      savedCaretPos;
		ISegment savedSelection;
		List<TextEditorDataState> states = new List<TextEditorDataState> ();
		
		void OnBeginUndo (object sender, EventArgs args)
		{
			savedCaretPos  = Caret.Offset;
			savedSelection = SelectionRange;
		}
		
		void OnEndUndo (object sender, Document.UndoOperation operation)
		{
			if (operation == null)
				return;
			TextEditorDataState state = new TextEditorDataState (this, operation, savedCaretPos, savedSelection);
			state.Attach ();
			states.Add (state);
		}
		
		class TextEditorDataState : IDisposable
		{
			int      undoCaretPos;
			ISegment undoSelection;
			
			int      redoCaretPos;
			ISegment redoSelection;
			
			Document.UndoOperation operation;
			TextEditorData editor;
			
			public TextEditorDataState (TextEditorData editor, Document.UndoOperation operation, int caretPos, ISegment selection)
			{
				this.editor        = editor;
				this.undoCaretPos  = caretPos;
				this.undoSelection = selection;
				this.operation     = operation;
				this.redoCaretPos  = editor.Caret.Offset;
				this.redoSelection = editor.SelectionRange;
				this.operation.Disposed += delegate {
					if (editor != null)
						editor.states.Remove (this);
					Dispose ();
				};
			}
			
			public void Attach ()
			{
				if (operation == null)
					return;
				operation.UndoDone += UndoDone;
				operation.RedoDone += RedoDone;
			}
			
			public void Dispose ()
			{
				if (operation != null) {
					operation.UndoDone -= UndoDone;
					operation.RedoDone -= RedoDone;
					operation = null;
				}
				editor = null;
				undoSelection = redoSelection = null;
			}
			
			void UndoDone (object sender, EventArgs args)
			{
				if (editor == null)
					return;
				editor.Caret.Offset   = this.undoCaretPos;
				editor.SelectionRange = this.undoSelection;
			}
			
			void RedoDone (object sender, EventArgs args)
			{
				if (editor == null)
					return;
				editor.Caret.Offset   = this.redoCaretPos;
				editor.SelectionRange = this.redoSelection;
			}
		}
		#endregion
		
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
					this.Caret.Offset = selection.Offset + value.Length;
				this.SelectionRange = new Segment(selection.Offset, value.Length);
			}
		}
		
		public IEnumerable<LineSegment> SelectedLines {
			get {
				List<LineSegment> result = new List<LineSegment> ();
				if (!this.IsSomethingSelected) {
					result.Add (this.document.GetLine (this.caret.Line));
				} else {
					int startLineNr = Document.OffsetToLineNumber (SelectionRange.Offset);
					RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = this.document.GetLine (startLineNr).Iter;
					LineSegment endLine = Document.GetLineByOffset (SelectionRange.EndOffset);
					bool skipEndLine = SelectionRange.EndOffset == endLine.Offset;
					do {
						if (iter.Current == endLine && skipEndLine)
							break;
						result.Add (iter.Current);
						if (iter.Current == Document.GetLineByOffset (SelectionRange.EndOffset))
							break;
					} while (iter.MoveNext ());
				}
				return result;
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
			if (!this.IsSomethingSelected)
				return;
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
			LineSegment fromLine = Document.GetLine (from);
			LineSegment toLine = Document.GetLine (to);
			if (this.SelectionAnchor < 0)
				this.SelectionAnchor = fromLine.Offset;
			SelectionRange = new Segment (fromLine.Offset, toLine.EndOffset - fromLine.Offset);
		}
		
		public void DeleteSelectedText ()
		{
			if (!IsSomethingSelected)
				return;
			document.BeginAtomicUndo ();
			ISegment selection = SelectionRange;
			ClearSelection ();
			
			bool needUpdate = Document.OffsetToLineNumber (selection.Offset) != Document.OffsetToLineNumber (selection.EndOffset);
			if (Caret.Offset > selection.Offset)
				Caret.Offset -= System.Math.Min (selection.Length, Caret.Offset - selection.Offset);
			Document.Remove (selection.Offset, selection.Length);
			if (needUpdate)
				Document.RequestUpdate (new LineToEndUpdate (Document.OffsetToLineNumber (selection.Offset)));
			document.EndAtomicUndo ();
			if (needUpdate)
				Document.CommitDocumentUpdate ();
		}
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged (EventArgs args)
		{
			if (SelectionChanged != null) 
				SelectionChanged (this, args);
		}
		#endregion
		
		#region Search & Replace
		ISearchEngine searchEngine;
		public ISearchEngine SearchEngine {
			get {
				return searchEngine;
			}
			set {
				value.TextEditorData = this;
				searchEngine = value;
			}
		}
		
		bool isCaseSensitive = true;
		bool isWholeWordOnly = false;
		
		public bool IsCaseSensitive {
			get {
				return isCaseSensitive;
			}
			set {
				isCaseSensitive = value;
				searchEngine.CompilePattern ();
			}
		}
		
		public bool IsWholeWordOnly {
			get {
				return isWholeWordOnly;
			}
			
			set {
				isWholeWordOnly = value;
				searchEngine.CompilePattern ();
			}
		}
		
		public bool IsMatchAt (int offset)
		{
			return searchEngine.IsMatchAt (offset);
		}
		
		public SearchResult GetMatchAt (int offset)
		{
			return searchEngine.GetMatchAt (offset);
		}
			
		public SearchResult SearchForward (int fromOffset)
		{
			return searchEngine.SearchForward (fromOffset);
		}
		
		public SearchResult SearchBackward (int fromOffset)
		{
			return searchEngine.SearchBackward (fromOffset);
		}
		
		public SearchResult FindNext ()
		{
			int startOffset = Caret.Offset;
			if (IsSomethingSelected && IsMatchAt (startOffset)) {
				startOffset = SelectionRange.EndOffset;
			}
			SearchResult result = SearchForward (startOffset);
			if (result != null) {
				Caret.Offset = result.Offset + result.Length;
				SelectionAnchor = Caret.Offset;
				SelectionRange = new Segment (result.Offset, result.Length);
			}
			return result;
		}
		
		public SearchResult FindPrevious ()
		{
			int startOffset = Caret.Offset;
			if (IsSomethingSelected && IsMatchAt (SelectionRange.Offset)) 
				startOffset = SelectionRange.Offset;
			
			SearchResult result = SearchBackward ((startOffset + Document.Length - 1) % Document.Length);
			if (result != null) {
				result.SearchWrapped = result.Offset > startOffset;
				Caret.Offset  = result.Offset + result.Length;
				SelectionAnchor = Caret.Offset;
				SelectionRange = new Segment (result.Offset, result.Length);
			}
			return result;
		}
		
		public bool Replace (string withPattern)
		{
			bool result = false;
			if (this.IsSomethingSelected) {
				ISegment selection = this.SelectionRange;
				SearchResult match = searchEngine.GetMatchAt (selection.Offset, selection.Length);
				if (match != null) {
					searchEngine.Replace (match, withPattern);
					ClearSelection ();
					Caret.Offset = selection.Offset + withPattern.Length;
					result = true;
				}
			}
			return FindNext () != null || result;
		}
		
		public int ReplaceAll (string withPattern)
		{
			int result = 0;
			Document.BeginAtomicUndo ();
			int offset = 0;
			SearchResult searchResult; 
			while (true) {
				searchResult = SearchForward (offset);
				if (searchResult == null || searchResult.SearchWrapped)
					break;
				searchEngine.Replace (searchResult, withPattern);
				offset = searchResult.EndOffset;
				result++;
			}
			if (result > 0)
				ClearSelection ();
			Document.EndAtomicUndo ();
			return result;
		}
		
		#endregion
	}
}
