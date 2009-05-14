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
using System.Text;
using Gtk;

namespace Mono.TextEditor
{
	public enum SelectionMode {
		Normal,
		Block
	}
	
	public class TextEditorData : IDisposable
	{
		ITextEditorOptions options;
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
		EditMode currentMode = null;
		public EditMode CurrentMode {
			get {
				return this.currentMode;
			}
			set {
				EditMode oldMode = this.currentMode;
				this.currentMode = value;
				if (oldMode != null)
					oldMode.RemovedFromTextEditor ();
			}
		}
		
		public TextEditorData () : this (new Document ())
		{
			
		}
		
		public TextEditorData (Document doc)
		{
			options = TextEditorOptions.DefaultOptions;
			Document = doc;
			this.SearchEngine = new BasicSearchEngine ();
			SelectionChanging += HandleSelectionChanging;
			Caret.PositionChanged += delegate(object sender, DocumentLocationEventArgs e) {
				if (Options.RemoveTrailingWhitespaces && e.Location.Line != Caret.Line) {
					LineSegment line = Document.GetLine (e.Location.Line);
					if (line != null)
						Document.RemoveTrailingWhitespaces (this, line);
				}
			};
		}
		
		public Document Document {
			get {
				return document;
			}
			set {
				this.document = value;
				caret = new Caret (this, document);
				caret.PositionChanged += CaretPositionChanged;
				this.document.BeginUndo += OnBeginUndo;
				this.document.EndUndo += OnEndUndo;
			}
		}
		
		/// <value>
		/// The eol mark used in this document - it's taken from the first line in the document,
		/// if no eol mark is found it's using the default (Environment.NewLine).
		/// The value is saved, even when all lines are deleted the eol marker will still be the old eol marker.
		/// </value>
		string eol = null;
		public string EolMarker {
			get {
				if (Options.OverrideDocumentEolMarker)
					return Options.DefaultEolMarker;
				if (eol == null && Document.LineCount > 0) {
					LineSegment line = Document.GetLine (0);
					if (line.DelimiterLength > 0) 
						eol = Document.GetTextAt (line.EditableLength, line.DelimiterLength);
				}
				return !String.IsNullOrEmpty (eol) ? eol : Options.DefaultEolMarker;
			}
		}
		
		public ITextEditorOptions Options {
			get {
				return options;
			}
			set {
				options = value;
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
		
	
		public int Insert (int offset, string value)
		{
			return Replace (offset, 0, value);
		}
		
		public void Remove (int offset, int count)
		{
			Replace (offset, count, null);
		}
		
		public int Replace (int offset, int count, string value)
		{
			StringBuilder sb = new StringBuilder ();
			if (value != null) {
				bool convertTabs = Options.TabsToSpaces;
				DocumentLocation loc = Document.OffsetToLocation (offset);
				for (int i = 0; i < value.Length; i++) {
					char ch = value[i];
					switch (ch) {
					case '\t':
						if (convertTabs) {
							int tabWidth = TextViewMargin.GetNextTabstop (this, loc.Column) - loc.Column;
							sb.Append (new string (' ', tabWidth));
							loc.Column += tabWidth;
						} else 
							goto default;
						break;
					case '\r':
						if (i + 1 < value.Length && value[i + 1] == '\n')
							i++;
						goto case '\n';
					case '\n':
						sb.Append (EolMarker);
						loc.Line++;
						loc.Column = 0;
						break;
					default:
						sb.Append (ch);
						loc.Column++;
						break;
					}
				}
			}
			
			((IBuffer)document).Replace (offset, count, sb.ToString ());
			return sb.Length;
		}
			
		public void InsertAtCaret (string text)
		{
			if (String.IsNullOrEmpty (text))
				return;
			Document.BeginAtomicUndo ();
			EnsureCaretIsNotVirtual ();
			int length = Insert (Caret.Offset, text);
			Caret.Offset += length;
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
			options = options.Kill ();
			
			if (document != null) {
				document.BeginUndo -= OnBeginUndo;
				document.EndUndo   -= OnEndUndo;
				// DOCUMENT MUST NOT BE DISPOSED !!! (Split View shares document)
				document = null;
			}
			caret = caret.Kill (x => x.PositionChanged -= CaretPositionChanged);
			SelectionChanging -= HandleSelectionChanging;
		}
		
		void CaretPositionChanged (object sender, EventArgs args)
		{
			if (!caret.PreserveSelection)
				this.ClearSelection ();
		}
		
		public bool CanEdit (int line)
		{
			if (document.ReadOnlyCheckDelegate != null)
				return document.ReadOnlyCheckDelegate (line);
			return !document.ReadOnly;
		}

		public int FindNextWordOffset (int offset)
		{
			return this.options.WordFindStrategy.FindNextWordOffset (this.Document, offset);
		}
		
		public int FindPrevWordOffset (int offset)
		{
			return this.options.WordFindStrategy.FindPrevWordOffset (this.Document, offset);
		}
		
		public int FindNextSubwordOffset (int offset)
		{
			return this.options.WordFindStrategy.FindNextSubwordOffset (this.Document, offset);
		}
		
		public int FindPrevSubwordOffset (int offset)
		{
			return this.options.WordFindStrategy.FindPrevSubwordOffset (this.Document, offset);
		}

		#region undo/redo handling
		int       savedCaretPos;
		Selection savedSelection;
		List<TextEditorDataState> states = new List<TextEditorDataState> ();
		
		void OnBeginUndo (object sender, EventArgs args)
		{
			savedCaretPos  = Caret.Offset;
			savedSelection = Selection.Clone (MainSelection);
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
			Selection undoSelection;
			
			int      redoCaretPos;
			Selection redoSelection;
			
			Document.UndoOperation operation;
			TextEditorData editor;
			
			public TextEditorDataState (TextEditorData editor, Document.UndoOperation operation, int caretPos, Selection selection)
			{
				this.editor        = editor;
				this.undoCaretPos  = caretPos;
				this.undoSelection = selection;
				this.operation     = operation;
				this.redoCaretPos  = editor.Caret.Offset;
				this.redoSelection = Mono.TextEditor.Selection.Clone (editor.MainSelection);
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
				editor.MainSelection = this.undoSelection;
			}
			
			void RedoDone (object sender, EventArgs args)
			{
				if (editor == null)
					return;
				editor.Caret.Offset   = this.redoCaretPos;
				editor.MainSelection = this.redoSelection;
			}
		}
		#endregion
		
		#region Selection management
		public bool IsSomethingSelected {
			get {
				return MainSelection != null && MainSelection.Anchor != MainSelection.Lead; 
			}
		}
		
		public bool IsMultiLineSelection {
			get {
				return IsSomethingSelected && MainSelection.Anchor.Line - MainSelection.Lead.Line != 0;
			}
		}
		
		public bool CanEditSelection {
			get {
				// To be improved when we support read-only regions
				if (IsSomethingSelected)
					return !document.ReadOnly;
				return CanEdit (caret.Line);
			}
		}
		class TextEditorDataEventArgs : EventArgs
		{
			TextEditorData data;
			public TextEditorData TextEditorData {
				get {
					return data;
				}
			}
			public TextEditorDataEventArgs (TextEditorData data)
			{
				this.data = data;
			}
		}
		
		void HandleSelectionChanging (object sender, TextEditorDataEventArgs args)
		{
			if (args.TextEditorData != this)
				this.ClearSelection ();
		}
		
		static event EventHandler<TextEditorDataEventArgs> SelectionChanging;
		
		static void OnSelectionChanging (TextEditorDataEventArgs args)
		{
			if (SelectionChanging != null)
				SelectionChanging (null, args);
		}
		
		public SelectionMode SelectionMode {
			get {
				return MainSelection != null ? MainSelection.SelectionMode : SelectionMode.Normal;
			}
			set {
				MainSelection.SelectionMode = value;
			}
		}
		
		Selection mainSelection = null;
		public Selection MainSelection {
			get {
				return mainSelection;
			}
			set {
				mainSelection = value;
				if (mainSelection != null) {
					mainSelection.Changed += delegate {
						OnSelectionChanged (EventArgs.Empty);
					};
				}
				OnSelectionChanged (EventArgs.Empty);
			}
		}
		
		public IEnumerable<Selection> Selections {
			get {
				yield return MainSelection;
			}
		}
		
		public DocumentLocation LogicalToVisualLocation (DocumentLocation location)
		{
			return Document.LogicalToVisualLocation (this, location);
		}
		
		public DocumentLocation VisualToLogicalLocation (DocumentLocation location)
		{
			int line = Document.VisualToLogicalLine (location.Line);
			int column = Document.GetLine (line).GetVisualColumn (this, Document, location.Column);
			return new DocumentLocation (line, column);
		}
		public int SelectionAnchor {
			get {
				if (MainSelection == null)
					return -1;
				return MainSelection.GetAnchorOffset (this);
			}
			set {
				DocumentLocation location = Document.OffsetToLocation (value);
				if (mainSelection == null) {
					MainSelection = new Selection (location, location);
				} else {
					if (MainSelection.Lead == location)
						MainSelection.Lead = MainSelection.Anchor;
					MainSelection.Anchor = location;
				}
			}
		}
		public ISegment SelectionRange {
			get {
				return MainSelection != null ? MainSelection.GetSelectionRange (this) : null;
			}
			set {
				if (!Segment.Equals (this.SelectionRange, value)) {
					OnSelectionChanging (new TextEditorDataEventArgs (this));
					if (value == null || value.Length == 0) {
						MainSelection = null;
					} else {
						DocumentLocation loc1 = document.OffsetToLocation (value.Offset);
						DocumentLocation loc2 = document.OffsetToLocation (value.EndOffset);
						if (MainSelection == null) {
							MainSelection = new Selection (loc1, loc2);
						} else {
							if (MainSelection.Anchor == loc1) {
								MainSelection.Lead = loc2;
							} else if (MainSelection.Anchor == loc2) {
								MainSelection.Lead = loc1;
							} else {
								MainSelection = new Selection (loc1, loc2);
							}
						}
						
					}
					OnSelectionChanged (EventArgs.Empty);
				}
			}
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
				Replace (selection.Offset, selection.Length, value);
				if (this.Caret.Offset > selection.Offset)
					this.Caret.Offset = selection.Offset + value.Length;
				this.SelectionRange = new Segment(selection.Offset, value.Length);
			}
		}
		
		
		public IEnumerable<LineSegment> SelectedLines {
			get {
				if (!this.IsSomethingSelected) {
					yield return this.document.GetLine (this.caret.Line);
				} else {
					foreach (Selection selection in Selections) {
						int startLineNr = selection.MinLine;
						RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = this.document.GetLine (startLineNr).Iter;
						LineSegment endLine = Document.GetLine (selection.MaxLine);
						bool skipEndLine = selection.Anchor < selection.Lead ? selection.Lead.Column == 0 : selection.Anchor.Column == 0;
						do {
							if (iter.Current == endLine && skipEndLine)
								break;
							yield return iter.Current;
							if (iter.Current == endLine)
								break;
						} while (iter.MoveNext ());
					}
				}
			}
		}
	/*	
		public int SelectionAnchor {
			get {
				return selectionAnchor;
			}
			set {
				selectionAnchor = value;
			}
		}*/
		
		public void ClearSelection ()
		{
			if (!this.IsSomethingSelected)
				return;
			MainSelection = null;
			OnSelectionChanged (EventArgs.Empty);
		}
		
		public void ExtendSelectionTo (DocumentLocation location)
		{
			if (MainSelection == null)
				MainSelection = new Selection (location, location);
			MainSelection.Lead = location;
		}
		
		public void ExtendSelectionTo (int offset)
		{
			ExtendSelectionTo (document.OffsetToLocation (offset));
		}
		
		public void SetSelectLines (int from, int to)
		{
			MainSelection = new Selection (document.OffsetToLocation (Document.GetLine (from).Offset), 
			                               document.OffsetToLocation (Document.GetLine (to).EndOffset));
		}
		
		internal void DeleteSelection (Selection selection)
		{
			switch (selection.SelectionMode) {
				case SelectionMode.Normal:
					ISegment segment = selection.GetSelectionRange (this);
					if (Caret.Offset > segment.Offset)
						Caret.Offset -= System.Math.Min (segment.Length, Caret.Offset - segment.Offset);
					int len = System.Math.Min (segment.Length, Document.Length - segment.Offset);
					if (len > 0)
						Remove (segment.Offset, len);
					break;
				case SelectionMode.Block:
					DocumentLocation visStart = LogicalToVisualLocation (selection.Anchor);
					DocumentLocation visEnd   = LogicalToVisualLocation (selection.Lead);
					int startCol = System.Math.Min (visStart.Column, visEnd.Column);
					int endCol   = System.Math.Max (visStart.Column, visEnd.Column);
					for (int lineNr = selection.MinLine; lineNr <= selection.MaxLine; lineNr++) {
						LineSegment curLine = Document.GetLine (lineNr);
						int col1 = curLine.GetLogicalColumn (this, Document, startCol);
						int col2 = curLine.GetLogicalColumn (this, Document, endCol);
						Remove (curLine.Offset  + col1, col2 - col1);
						if (Caret.Line == lineNr && Caret.Column >= col1)
							Caret.Column -= col2 - col1;
					}
					break;
				}
		}
		
		public void DeleteSelectedText ()
		{
			if (!IsSomethingSelected)
				return;
			document.BeginAtomicUndo ();
			bool needUpdate = false;
			foreach (Selection selection in Selections) {
				ISegment segment = selection.GetSelectionRange (this);
				needUpdate |= Document.OffsetToLineNumber (segment.Offset) != Document.OffsetToLineNumber (segment.EndOffset);
				DeleteSelection (selection);
			}
			ClearSelection ();
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
				value.SearchRequest = SearchRequest;
				searchEngine = value;
				searchEngine.SearchRequest.Changed += delegate {
					OnSearchChanged (EventArgs.Empty);
				};
				OnSearchChanged (EventArgs.Empty);
			}
		}
		
		protected virtual void OnSearchChanged (EventArgs args)
		{
			if (SearchChanged != null)
				SearchChanged (this, args);
		}
		
		public event EventHandler SearchChanged;
		
		SearchRequest currentSearchRequest;
		
		public SearchRequest SearchRequest {
			get {
				if (currentSearchRequest == null)
					currentSearchRequest = new SearchRequest ();
				return currentSearchRequest;
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
				startOffset = MainSelection.GetLeadOffset (this);
			}
			
			SearchResult result = SearchForward (startOffset);
			if (result != null) {
				Caret.Offset = result.Offset + result.Length;
				MainSelection = new Selection (Document.OffsetToLocation (result.Offset), Caret.Location);
			}
			return result;
		}
		
		public SearchResult FindPrevious ()
		{
			int startOffset = Caret.Offset;
			if (IsSomethingSelected && IsMatchAt (MainSelection.GetAnchorOffset (this))) 
				startOffset = MainSelection.GetAnchorOffset (this);
			
			SearchResult result = SearchBackward ((startOffset + Document.Length - 1) % Document.Length);
			if (result != null) {
				result.SearchWrapped = result.Offset > startOffset;
				Caret.Offset  = result.Offset + result.Length;
				MainSelection = new Selection (Document.OffsetToLocation (result.Offset), Caret.Location);
			}
			return result;
		}
		
		public bool SearchReplace (string withPattern)
		{
			bool result = false;
			if (this.IsSomethingSelected) {
				ISegment selection = MainSelection.GetSelectionRange (this);
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
		
		public int SearchReplaceAll (string withPattern)
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
				offset = searchResult.Offset + withPattern.Length;
				result++;
			}
			if (result > 0)
				ClearSelection ();
			Document.EndAtomicUndo ();
			return result;
		}
		#endregion
		
		#region VirtualSpace Manager
		IVirtualSpaceManager virtualSpaceManager = null;
		public IVirtualSpaceManager VirtualSpaceManager {
			get {
				if (virtualSpaceManager == null)
					virtualSpaceManager = new DefaultVirtualSpaceManager (this.document);
				return virtualSpaceManager;
			}
			set {
				virtualSpaceManager = value;
			}
		}
		
		public interface IVirtualSpaceManager
		{
			string GetVirtualSpaces (int lineNumber, int column);
			int GetNextVirtualColumn (int lineNumber, int column);
		}
		
		class DefaultVirtualSpaceManager : IVirtualSpaceManager
		{
			Document doc;
			public DefaultVirtualSpaceManager (Document doc)
			{
				this.doc = doc;
			}
			public string GetVirtualSpaces (int lineNumber, int column)
			{
				LineSegment line = doc.GetLine (lineNumber);
				if (line == null)
					return "";
				int count = column - line.EditableLength;
				return new string (' ', System.Math.Max (0, count));
			}
			
			public int GetNextVirtualColumn (int lineNumber, int column)
			{
				return column + 1;
			}
		}
		
		public string GetVirtualSpaces (int lineNumber, int column)
		{
			return VirtualSpaceManager.GetVirtualSpaces (lineNumber, column);
		}
		
		public int GetNextVirtualColumn (int lineNumber, int column)
		{
			return VirtualSpaceManager.GetNextVirtualColumn (lineNumber, column);
		}
		
		public int EnsureCaretIsNotVirtual ()
		{
			LineSegment line = Document.GetLine (Caret.Line);
			if (Caret.Column > line.EditableLength) {
				string virtualSpace = GetVirtualSpaces (Caret.Line, Caret.Column);
				Insert (Caret.Offset, virtualSpace);
				// No need to reposition the caret, because it's already at the correct position
				// The only difference is that the position is not virtual anymore.
				return virtualSpace.Length;
			}
			return 0;
		}
		#endregion
	}
}
