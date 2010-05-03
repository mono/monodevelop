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
using System.IO;

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
		
		static Adjustment emptyAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
		Adjustment hadjustment = emptyAdjustment; 
		public Adjustment HAdjustment {
			get {
				return hadjustment;
			}
			set {
				hadjustment = value;
			}
		}
		
		Adjustment vadjustment = emptyAdjustment;
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
		}

		void HandleDocLineChanged (object sender, LineEventArgs e)
		{
			e.Line.WasChanged = true;
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
				
				this.document.Undone += DocumentHandleUndone;
				this.document.Redone += DocumentHandleRedone;
				this.document.LineChanged += HandleDocLineChanged;
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
			options = options.Kill ();
			
			if (document != null) {
				document.LineChanged -= HandleDocLineChanged;
				document.BeginUndo -= OnBeginUndo;
				document.EndUndo   -= OnEndUndo;
				document.Undone -= DocumentHandleUndone;
				document.Redone -= DocumentHandleRedone;
				
				// DOCUMENT MUST NOT BE DISPOSED !!! (Split View shares document)
				document = null;
			}
			caret = caret.Kill (x => x.PositionChanged -= CaretPositionChanged);
			SelectionChanging -= HandleSelectionChanging;
		}
		
		void CaretPositionChanged (object sender, DocumentLocationEventArgs args)
		{
			if (!caret.PreserveSelection)
				this.ClearSelection ();
			
			if (Options.RemoveTrailingWhitespaces && args.Location.Line != Caret.Line) {
				LineSegment line = Document.GetLine (args.Location.Line);
				if (line != null && line.WasChanged)
					Document.RemoveTrailingWhitespaces (this, line);
			}
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

		
		public delegate void PasteCallback (int insertionOffset, string text);
		
		public event PasteCallback Paste;
		
		public void PasteText (int insertionOffset, string text)
		{
			if (Paste != null)
				Paste (insertionOffset, text);
		}
		
		#region undo/redo handling
		int       savedCaretPos;
		Selection savedSelection;
		//List<TextEditorDataState> states = new List<TextEditorDataState> ();
		
		void OnBeginUndo (object sender, EventArgs args)
		{
			savedCaretPos  = Caret.Offset;
			savedSelection = Selection.Clone (MainSelection);
		}
		
		void OnEndUndo (object sender, Document.UndoOperationEventArgs e)
		{
			if (e == null)
				return;
			e.Operation.Tag = new TextEditorDataState (this, savedCaretPos, savedSelection);
		}

		void DocumentHandleUndone (object sender, Document.UndoOperationEventArgs e)
		{
			TextEditorDataState state = e.Operation.Tag as TextEditorDataState;
			if (state != null)
				state.UndoState ();
		}

		void DocumentHandleRedone (object sender, Document.UndoOperationEventArgs e)
		{
			TextEditorDataState state = e.Operation.Tag as TextEditorDataState;
			if (state != null)
				state.RedoState ();
		}
		
		class TextEditorDataState
		{
			int      undoCaretPos;
			Selection undoSelection;
			
			int      redoCaretPos;
			Selection redoSelection;
			
			TextEditorData editor;
			
			public TextEditorDataState (TextEditorData editor, int caretPos, Selection selection)
			{
				this.editor        = editor;
				this.undoCaretPos  = caretPos;
				this.undoSelection = selection;
				
				this.redoCaretPos  = editor.Caret.Offset;
				this.redoSelection = Mono.TextEditor.Selection.Clone (editor.MainSelection);
			}
			
			public void UndoState ()
			{
				editor.Caret.Offset   = this.undoCaretPos;
				editor.MainSelection = this.undoSelection;
			}
			
			public void RedoState ()
			{
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
				if (MainSelection != null)
					MainSelection.SelectionMode = value;
			}
		}
		
		Selection mainSelection = null;
		public Selection MainSelection {
			get {
				return mainSelection;
			}
			set {
				if (mainSelection == null && value == null)
					return;
				if (mainSelection == null && value != null || mainSelection != null && value == null || !mainSelection.Equals (value)) {
					mainSelection = value;
					if (mainSelection != null) {
						mainSelection.Changed += delegate {
							OnSelectionChanged (EventArgs.Empty);
						};
					}
					OnSelectionChanged (EventArgs.Empty);
				}
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
			int column = Document.GetLine (line).GetVisualColumn (this, location.Column);
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
		
		public void SetSelection (int anchorOffset, int leadOffset)
		{
			DocumentLocation anchor = document.OffsetToLocation (anchorOffset);
			DocumentLocation lead = document.OffsetToLocation (leadOffset);
			MainSelection = new Selection (anchor, lead);
		}

		public void SetSelection (DocumentLocation anchor, DocumentLocation lead)
		{
			MainSelection = new Selection (anchor, lead);
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
			if (selection == null)
				throw new ArgumentNullException ("selection");
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
				DocumentLocation visEnd = LogicalToVisualLocation (selection.Lead);
				int startCol = System.Math.Min (visStart.Column, visEnd.Column);
				int endCol = System.Math.Max (visStart.Column, visEnd.Column);
				bool preserve = Caret.PreserveSelection;
				Caret.PreserveSelection = true;
				for (int lineNr = selection.MinLine; lineNr <= selection.MaxLine; lineNr++) {
					LineSegment curLine = Document.GetLine (lineNr);
					int col1 = curLine.GetLogicalColumn (this, startCol);
					int col2 = System.Math.Min (curLine.GetLogicalColumn (this, endCol), curLine.EditableLength);
					if (col1 >= col2)
						continue;
					Remove (curLine.Offset + col1, col2 - col1);
					
					if (Caret.Line == lineNr && Caret.Column >= col1)
						Caret.Column -= col2 - col1;
				}
				int column = System.Math.Min (selection.Anchor.Column, selection.Lead.Column);
				selection.Anchor = new DocumentLocation (selection.Anchor.Line, column);
				selection.Lead = new DocumentLocation (selection.Lead.Line, column);
				Caret.Column = column;
				Caret.PreserveSelection = preserve;
				break;
			}
		
		}
		
		public void DeleteSelectedText ()
		{
			DeleteSelectedText (true);
		}
		public void DeleteSelectedText (bool clearSelection)
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
			if (clearSelection)
				ClearSelection ();
			document.EndAtomicUndo ();
			if (needUpdate)
				Document.CommitDocumentUpdate ();
		}
		
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged (EventArgs args)
		{
//			Console.WriteLine ("----");
//			Console.WriteLine (Environment.StackTrace);
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
				if (currentSearchRequest == null) {
					currentSearchRequest = new SearchRequest ();
					currentSearchRequest.Changed += delegate {
						OnSearchChanged (EventArgs.Empty);
					};
				}
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
		
		public SearchResult FindNext (bool setSelection)
		{
			int startOffset = Caret.Offset;
			if (IsSomethingSelected && IsMatchAt (startOffset)) {
				startOffset = MainSelection.GetLeadOffset (this);
			}
			
			SearchResult result = SearchForward (startOffset);
			if (result != null) {
				Caret.Offset = result.Offset + result.Length;
				if (setSelection)
					MainSelection = new Selection (Document.OffsetToLocation (result.Offset), Caret.Location);
			}
			return result;
		}
		
		public SearchResult FindPrevious (bool setSelection)
		{
			int startOffset = Caret.Offset - SearchEngine.SearchRequest.SearchPattern.Length;
			if (IsSomethingSelected && IsMatchAt (MainSelection.GetAnchorOffset (this))) 
				startOffset = MainSelection.GetAnchorOffset (this);
			
			int searchOffset;
			if (startOffset < 0) {
				searchOffset = Document.Length - 1;
			} else {
				searchOffset = (startOffset + Document.Length - 1) % Document.Length;
			}
			SearchResult result = SearchBackward (searchOffset);
			if (result != null) {
				result.SearchWrapped = result.EndOffset > startOffset;
				Caret.Offset  = result.Offset + result.Length;
				if (setSelection)
					MainSelection = new Selection (Document.OffsetToLocation (result.Offset), Caret.Location);
			}
			return result;
		}
		
		public bool SearchReplace (string withPattern, bool setSelection)
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
			return FindNext (setSelection) != null || result;
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
			if (line == null)
				return 0;
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
		
		public Stream OpenStream ()
		{
			return new MemoryStream (System.Text.Encoding.UTF8.GetBytes (Document.Text), false);
		}
		
		public void RaiseUpdateAdjustmentsRequested ()
		{
			OnUpdateAdjustmentsRequested (EventArgs.Empty);
		}
		
		protected virtual void OnUpdateAdjustmentsRequested (EventArgs e)
		{
			EventHandler handler = this.UpdateAdjustmentsRequested;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler UpdateAdjustmentsRequested;

		public void RequestRecenter ()
		{
			EventHandler handler = RecenterEditor;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}
		public event EventHandler RecenterEditor;
	}
}
