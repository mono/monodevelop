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
using System.Diagnostics;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public enum SelectionMode {
		Normal,
		Block
	}
	
	public class TextEditorData : IDisposable
	{
		ITextEditorOptions options;
		TextDocument document; 
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
				this.currentMode.AddedToEditor (this);
				if (oldMode != null)
					oldMode.RemovedFromEditor (this);
			}
		}
		
		public TextEditor Parent {
			get;
			set;
		}
		
		public string FileName {
			get {
				return Document.FileName;
			}
		}
		
		public string MimeType {
			get {
				return Document.MimeType;
			}
		}

		public TextEditorData () : this (new TextDocument ())
		{
		}

		public TextEditorData (TextDocument doc)
		{
			LineHeight = 16;

			caret = new Caret (this);
			caret.PositionChanged += CaretPositionChanged;

			options = TextEditorOptions.DefaultOptions;
			Document = doc;
			this.SearchEngine = new BasicSearchEngine ();

			this.heightTree = new HeightTree (this);
			this.heightTree.Rebuild ();
		}

		void HandleDocTextSet (object sender, EventArgs e)
		{
			this.heightTree.Rebuild ();
			ClearSelection ();
		}

		public double GetLineHeight (LineSegment line)
		{
			if (Parent == null)
				return LineHeight;
			return Parent.GetLineHeight (line);
		}
		
		public double GetLineHeight (int line)
		{
			if (Parent == null)
				return LineHeight;
			return Parent.GetLineHeight (line);
		}

		void HandleDocLineChanged (object sender, LineEventArgs e)
		{
			e.Line.WasChanged = true;
		}

		
		public TextDocument Document {
			get {
				return document;
			}
			set {
				DetachDocument ();
				this.document = value;
				this.document.BeginUndo += OnBeginUndo;
				this.document.EndUndo += OnEndUndo;

				this.document.Undone += DocumentHandleUndone;
				this.document.Redone += DocumentHandleRedone;
				this.document.LineChanged += HandleDocLineChanged;

				this.document.TextSet += HandleDocTextSet;
				this.document.Folded += HandleTextEditorDataDocumentFolded;
				this.document.FoldTreeUpdated += HandleTextEditorDataDocumentFoldTreeUpdated;

				this.document.splitter.LineInserted += HandleDocumentsplitterhandleLineInserted;
				this.document.splitter.LineRemoved += HandleDocumentsplitterhandleLineRemoved;
			}
		}


		void HandleDocumentsplitterhandleLineRemoved (object sender, LineEventArgs e)
		{
			heightTree.RemoveLine (OffsetToLineNumber (e.Line.Offset));
		}

		void HandleDocumentsplitterhandleLineInserted (object sender, LineEventArgs e)
		{
			heightTree.InsertLine (OffsetToLineNumber (e.Line.Offset));
		}

		/// <value>
		/// The eol mark used in this document - it's taken from the first line in the document,
		/// if no eol mark is found it's using the default (Environment.NewLine).
		/// The value is saved, even when all lines are deleted the eol marker will still be the old eol marker.
		/// </value>
		public string EolMarker {
			get {
				if (Options.OverrideDocumentEolMarker)
					return Options.DefaultEolMarker;
				string eol = null;
				if (Document.LineCount > 0) {
					LineSegment line = Document.GetLine (DocumentLocation.MinLine);
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
		
		ColorSheme colorStyle;
		public ColorSheme ColorStyle {
			get {
				return colorStyle ?? ColorSheme.Empty;
			}
			set {
				colorStyle = value;
			}
		}
		
		public string GetMarkup (int offset, int length, bool removeIndent, bool useColors = true, bool replaceTabs = true)
		{
			ISyntaxMode mode = Document.SyntaxMode;

			int indentLength = SyntaxMode.GetIndentLength (Document, offset, length, false);
			int curOffset = offset;

			StringBuilder result = new StringBuilder ();
			while (curOffset < offset + length && curOffset < Document.Length) {
				LineSegment line = Document.GetLineByOffset (curOffset);
				int toOffset = System.Math.Min (line.Offset + line.EditableLength, offset + length);
				Stack<ChunkStyle> styleStack = new Stack<ChunkStyle> ();
				foreach (var chunk in mode.GetChunks (ColorStyle, line, curOffset, toOffset - curOffset)) {

					ChunkStyle chunkStyle = ColorStyle.GetChunkStyle (chunk);
					bool setBold = chunkStyle.Bold && (styleStack.Count == 0 || !styleStack.Peek ().Bold) ||
							!chunkStyle.Bold && (styleStack.Count == 0 || styleStack.Peek ().Bold);
					bool setItalic = chunkStyle.Italic && (styleStack.Count == 0 || !styleStack.Peek ().Italic) ||
							!chunkStyle.Italic && (styleStack.Count == 0 || styleStack.Peek ().Italic);
					bool setUnderline = chunkStyle.Underline && (styleStack.Count == 0 || !styleStack.Peek ().Underline) ||
							!chunkStyle.Underline && (styleStack.Count == 0 || styleStack.Peek ().Underline);
					bool setColor = styleStack.Count == 0 || TextViewMargin.GetPixel (styleStack.Peek ().Color) != TextViewMargin.GetPixel (chunkStyle.Color);
					if (setColor || setBold || setItalic || setUnderline) {
						if (styleStack.Count > 0) {
							result.Append ("</span>");
							styleStack.Pop ();
						}
						result.Append ("<span");
						if (useColors) {
							result.Append (" foreground=\"");
							result.Append (SyntaxMode.ColorToPangoMarkup (chunkStyle.Color));
							result.Append ("\"");
						}
						if (chunkStyle.Bold)
							result.Append (" weight=\"bold\"");
						if (chunkStyle.Italic)
							result.Append (" style=\"italic\"");
						if (chunkStyle.Underline)
							result.Append (" underline=\"single\"");
						result.Append (">");
						styleStack.Push (chunkStyle);
					}

					for (int i = 0; i < chunk.Length && chunk.Offset + i < Document.Length; i++) {
						char ch = Document.GetCharAt (chunk.Offset + i);
						switch (ch) {
						case '&':
							result.Append ("&amp;");
							break;
						case '<':
							result.Append ("&lt;");
							break;
						case '>':
							result.Append ("&gt;");
							break;
						case '\t':
							if (replaceTabs) {
								result.Append (new string (' ', options.TabSize));
							} else {
								result.Append ('\t');
							}
							break;
						default:
							result.Append (ch);
							break;
						}
					}
				}
				while (styleStack.Count > 0) {
					result.Append("</span>");
					styleStack.Pop ();
				}

				curOffset = line.EndOffset;
				if (removeIndent)
					curOffset += indentLength;
				if (result.Length > 0 && curOffset < offset + length)
					result.AppendLine ();
			}
			return result.ToString ();
		}

		public IEnumerable<Chunk> GetChunks (LineSegment line, int offset, int length)
		{
			return document.SyntaxMode.GetChunks (ColorStyle, line, offset, length);
		}		
	
		public int Insert (int offset, string value)
		{
			return Replace (offset, 0, value);
		}
		
		public void Remove (int offset, int count)
		{
			Replace (offset, count, null);
		}
		
		public void Remove (TextSegment removeSegment)
		{
			Remove (removeSegment.Offset, removeSegment.Length);
		}
		
		public string FormatString (DocumentLocation loc, string str)
		{
			if (string.IsNullOrEmpty (str))
				return "";
			StringBuilder sb = new StringBuilder ();
			bool convertTabs = Options.TabsToSpaces;
			
			for (int i = 0; i < str.Length; i++) {
				char ch = str [i];
				switch (ch) {
				case '\u00A0': // convert non breaking spaces to standard spaces.
					sb.Append (' ');
					break;
				case '\t':
					if (convertTabs) {
						int tabWidth = TextViewMargin.GetNextTabstop (this, loc.Column) - loc.Column;
						sb.Append (new string (' ', tabWidth));
						loc.Column += tabWidth;
					} else 
						goto default;
					break;
				case '\r':
					if (i + 1 < str.Length && str [i + 1] == '\n')
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
			return sb.ToString ();
		}
		
		public string FormatString (int offset, string str)
		{
			return FormatString (Document.OffsetToLocation (offset), str);
		}
		
		public int Replace (int offset, int count, string value)
		{
			string formattedString = FormatString (offset, value);
			((IBuffer)document).Replace (offset, count, formattedString);
			return formattedString.Length;
		}
			
		public void InsertAtCaret (string text)
		{
			if (String.IsNullOrEmpty (text))
				return;
			using (var undo = OpenUndoGroup ()) {
				if (IsSomethingSelected && MainSelection.SelectionMode == SelectionMode.Block) {
					var visualInsertLocation = LogicalToVisualLocation (MainSelection.Anchor);
					for (int lineNumber = MainSelection.MinLine; lineNumber <= MainSelection.MaxLine; lineNumber++) {
						var lineSegment = GetLine (lineNumber);
						int insertOffset = lineSegment.GetLogicalColumn (this, visualInsertLocation.Column) - 1;
						string textToInsert;
						if (lineSegment.EditableLength < insertOffset) {
							int visualLastColumn = lineSegment.GetVisualColumn (this, lineSegment.EditableLength + 1);
							int charsToInsert = visualInsertLocation.Column - visualLastColumn;
							int spaceCount = charsToInsert % Options.TabSize;
							textToInsert = new string ('\t', (charsToInsert - spaceCount) / Options.TabSize) + new string (' ', spaceCount) + text;
							insertOffset = lineSegment.EditableLength;
						} else {
							textToInsert = text;
						}
						Insert (lineSegment.Offset + insertOffset, textToInsert);
					}
					Caret.PreserveSelection = true;
					Caret.Column += text.Length;
					MainSelection.Lead = new DocumentLocation (MainSelection.Lead.Line, Caret.Column);
					MainSelection.Anchor = new DocumentLocation (MainSelection.Anchor.Line, Caret.Column);
					Document.CommitMultipleLineUpdate (MainSelection.MinLine, MainSelection.MaxLine);
				} else {
					EnsureCaretIsNotVirtual ();
					int offset = Caret.Offset;
					int length = Insert (offset, text);
					Caret.Offset = offset + length;
				}
			}
		}

		void DetachDocument ()
		{
			if (document == null) 
				return;
			
			document.BeginUndo -= OnBeginUndo;
			document.EndUndo -= OnEndUndo;

			document.Undone -= DocumentHandleUndone;
			document.Redone -= DocumentHandleRedone;
			document.LineChanged -= HandleDocLineChanged;
			
			document.TextSet -= HandleDocTextSet;
			document.Folded -= HandleTextEditorDataDocumentFolded;
			document.FoldTreeUpdated -= HandleTextEditorDataDocumentFoldTreeUpdated;
			
			document.splitter.LineInserted -= HandleDocumentsplitterhandleLineInserted;
			document.splitter.LineRemoved -= HandleDocumentsplitterhandleLineRemoved;
			
			document = null;
		}

		public void Dispose ()
		{
			options = options.Kill ();

			DetachDocument ();
		}

		/// <summary>
		/// Removes the indent on the caret line, if the indent mode is set to virtual and the indent matches
		/// the current virtual indent in that line.
		/// </summary>
		public void FixVirtualIndentation ()
		{
			if (!HasIndentationTracker || Options.IndentStyle != IndentStyle.Virtual)
				return;
			var line = Document.GetLine (Caret.Line);
			if (line != null && line.EditableLength > 0 && GetIndentationString (Caret.Location) == Document.GetTextAt (line.Offset, line.EditableLength))
				Remove (line.Offset, line.EditableLength);
		}

		void CaretPositionChanged (object sender, DocumentLocationEventArgs args)
		{
			if (!caret.PreserveSelection)
				this.ClearSelection ();
			if (Options.RemoveTrailingWhitespaces && args.Location.Line != Caret.Line) {
				LineSegment line = Document.GetLine (args.Location.Line);
				if (line != null && line.WasChanged)
					TextDocument.RemoveTrailingWhitespaces (this, line);
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
		
		public int FindCurrentWordEnd (int offset)
		{
			return this.Options.WordFindStrategy.FindCurrentWordEnd (this.Document, offset);
		}
		
		public int FindCurrentWordStart (int offset)
		{
			return this.Options.WordFindStrategy.FindCurrentWordStart (this.Document, offset);
		}
		
		public delegate void PasteCallback (int insertionOffset, string text, int insertedChars);
		
		public event PasteCallback Paste;
		
		public void PasteText (int insertionOffset, string text, int insertedChars)
		{
			if (Paste != null)
				Paste (insertionOffset, text, insertedChars);
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

		void OnEndUndo (object sender, TextDocument.UndoOperationEventArgs e)
		{
			if (e == null)
				return;
			e.Operation.Tag = new TextEditorDataState (this, savedCaretPos, savedSelection);
		}

		void DocumentHandleUndone (object sender, TextDocument.UndoOperationEventArgs e)
		{
			TextEditorDataState state = e.Operation.Tag as TextEditorDataState;
			if (state != null)
				state.UndoState ();
		}

		void DocumentHandleRedone (object sender, TextDocument.UndoOperationEventArgs e)
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
		
		public event EventHandler SelectionChanging;
		
		protected virtual void OnSelectionChanging (EventArgs e)
		{
			EventHandler handler = this.SelectionChanging;
			if (handler != null)
				handler (this, e);
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
					OnSelectionChanging (EventArgs.Empty);
					if (mainSelection != null)
						mainSelection.Changed -= HandleMainSelectionChanged;
					mainSelection = value;
					if (mainSelection != null) 
						mainSelection.Changed += HandleMainSelectionChanged;
					OnSelectionChanged (EventArgs.Empty);
				}
			}
		}

		void HandleMainSelectionChanged (object sender, EventArgs e)
		{
			OnSelectionChanged (EventArgs.Empty);
		}
		
		public IEnumerable<Selection> Selections {
			get {
				yield return MainSelection;
			}
		}
		
//		public DocumentLocation LogicalToVisualLocation (DocumentLocation location)
//		{
//			return LogicalToVisualLocation (this, location);
//		}
//		
//		public DocumentLocation VisualToLogicalLocation (DocumentLocation location)
//		{
//			int line = VisualToLogicalLine (location.Line);
//			int column = Document.GetLine (line).GetVisualColumn (this, location.Column);
//			return new DocumentLocation (line, column);
//		}
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
		public TextSegment SelectionRange {
			get {
				return MainSelection != null ? MainSelection.GetSelectionRange (this) : new TextSegment (Caret.Offset, 0);
			}
			set {
				if (this.SelectionRange != value) {
					OnSelectionChanging (EventArgs.Empty);
					if (value.IsEmpty) {
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
				var selection = this.SelectionRange;
				Replace (selection.Offset, selection.Length, value);
				if (this.Caret.Offset > selection.Offset)
					this.Caret.Offset = selection.Offset + value.Length;
				this.SelectionRange = new TextSegment (selection.Offset, value.Length);
			}
		}
		
		
		public IEnumerable<LineSegment> SelectedLines {
			get {
				if (!IsSomethingSelected) 
					return document.GetLinesBetween (caret.Line, caret.Line);
				var selection = MainSelection;
				int startLineNr = selection.MinLine;
				int endLineNr = selection.MaxLine;
						
				bool skipEndLine = selection.Anchor < selection.Lead ? selection.Lead.Column == DocumentLocation.MinColumn : selection.Anchor.Column == DocumentLocation.MinColumn;
				if (skipEndLine)
					endLineNr--;
				return document.GetLinesBetween (startLineNr, endLineNr);
			}
		}
		
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

		public void SetSelection (int anchorLine, int anchorColumn, int leadLine, int leadColumn)
		{
			SetSelection (new DocumentLocation (anchorLine, anchorColumn), new DocumentLocation (leadLine, leadColumn));
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
				var segment = selection.GetSelectionRange (this);
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
					int col1 = curLine.GetLogicalColumn (this, startCol) - 1;
					int col2 = System.Math.Min (curLine.GetLogicalColumn (this, endCol) - 1, curLine.EditableLength);
					if (col1 >= col2)
						continue;
					Remove (curLine.Offset + col1, col2 - col1);
					
					if (Caret.Line == lineNr && Caret.Column >= col1)
						Caret.Column = col1 + 1;
				}
				int column = System.Math.Min (selection.Anchor.Column, selection.Lead.Column);
				selection.Anchor = new DocumentLocation (selection.Anchor.Line, column);
				selection.Lead = new DocumentLocation (selection.Lead.Line, column);
				Caret.Column = column;
				Caret.PreserveSelection = preserve;
				break;
			}
			FixVirtualIndentation ();
		}
		
		public void DeleteSelectedText ()
		{
			DeleteSelectedText (true);
		}
		
		public void DeleteSelectedText (bool clearSelection)
		{
			if (!IsSomethingSelected)
				return;
			bool needUpdate = false;
			using (var undo = OpenUndoGroup ()) {
				EnsureCaretIsNotVirtual ();
				foreach (Selection selection in Selections) {
					var segment = selection.GetSelectionRange (this);
					needUpdate |= Document.OffsetToLineNumber (segment.Offset) != Document.OffsetToLineNumber (segment.EndOffset);
					DeleteSelection (selection);
				}
				if (clearSelection)
					ClearSelection ();
				FixVirtualIndentation ();
			}
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
				if (searchEngine != value) {
					value.TextEditorData = this;
					value.SearchRequest = SearchRequest;
					searchEngine = value;
					OnSearchChanged (EventArgs.Empty);
				}
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
				Caret.Offset = result.Offset + result.Length;
				if (setSelection)
					MainSelection = new Selection (Document.OffsetToLocation (result.Offset), Caret.Location);
			}
			return result;
		}
		
		public bool SearchReplace (string withPattern, bool setSelection)
		{
			bool result = false;
			if (this.IsSomethingSelected) {
				var selection = MainSelection.GetSelectionRange (this);
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
			using (var undo = OpenUndoGroup ()) {
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
			}
			return result;
		}
		#endregion
		
		#region VirtualSpace Manager
		IIndentationTracker indentationTracker = null;
		public bool HasIndentationTracker {
			get {
				return indentationTracker != null;	
			}	
		}

		public IIndentationTracker IndentationTracker {
			get {
				if (!HasIndentationTracker)
					throw new InvalidOperationException ("Indentation tracker not installed.");
				return indentationTracker;
			}
			set {
				indentationTracker = value;
			}
		}
		
		public string GetIndentationString (DocumentLocation loc)
		{
			return IndentationTracker.GetIndentationString (loc.Line, loc.Column);
		}
		
		public string GetIndentationString (int lineNumber, int column)
		{
			return IndentationTracker.GetIndentationString (lineNumber, column);
		}
		
		public string GetIndentationString (int offset)
		{
			return IndentationTracker.GetIndentationString (offset);
		}
		
		public int GetVirtualIndentationColumn (DocumentLocation loc)
		{
			return IndentationTracker.GetVirtualIndentationColumn (loc.Line, loc.Column);
		}
		
		public int GetVirtualIndentationColumn (int lineNumber, int column)
		{
			return IndentationTracker.GetVirtualIndentationColumn (lineNumber, column);
		}
		
		public int GetVirtualIndentationColumn (int offset)
		{
			return IndentationTracker.GetVirtualIndentationColumn (offset);
		}
		
		/// <summary>
		/// Ensures the caret is not in a virtual position by adding whitespaces up to caret position.
		/// That method should always be called in an undo group.
		/// </summary>
		public int EnsureCaretIsNotVirtual ()
		{
			Debug.Assert (document.IsInAtomicUndo);
			LineSegment line = Document.GetLine (Caret.Line);
			if (line == null)
				return 0;
			if (Caret.Column > line.EditableLength + 1) {
				string virtualSpace;
				if (HasIndentationTracker && line.EditableLength == 0) {
					virtualSpace = GetIndentationString (Caret.Location);
				} else {
					virtualSpace = new string (' ', Caret.Column - 1 - line.EditableLength);
				}
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

		#region Document delegation
		public int Length {
			get {
				return document.Length;
			}
		}

		public string Text {
			get {
				return document.Text;
			}
			set {
				document.Text = value;
			}
		}

		public string GetTextBetween (int startOffset, int endOffset)
		{
			return document.GetTextBetween (startOffset, endOffset);
		}
		
		public string GetTextBetween (DocumentLocation start, DocumentLocation end)
		{
			return document.GetTextBetween (start, end);
		}
		
		public string GetTextBetween (int startLine, int startColumn, int endLine, int endColumn)
		{
			return document.GetTextBetween (startLine, startColumn, endLine, endColumn);
		}

		public string GetTextAt (int offset, int count)
		{
			return document.GetTextAt (offset, count);
		}

		public string GetTextAt (TextSegment segment)
		{
			return document.GetTextAt (segment);
		}
		
		public char GetCharAt (int offset)
		{
			return document.GetCharAt (offset);
		}
		
		public string GetLineText (int line)
		{
			return Document.GetLineText (line);
		}
		
		public string GetLineText (int line, bool includeDelimiter)
		{
			return Document.GetLineText (line, includeDelimiter);
		}

		public IEnumerable<LineSegment> Lines {
			get {
				return Document.Lines;
			}
		}
		
		public int LineCount {
			get {
				return Document.LineCount;
			}
		}
		
		public int LocationToOffset (int line, int column)
		{
			return Document.LocationToOffset (line, column);
		}
		
		public int LocationToOffset (DocumentLocation location)
		{
			return Document.LocationToOffset (location);
		}
		
		public DocumentLocation OffsetToLocation (int offset)
		{
			return Document.OffsetToLocation (offset);
		}

		public string GetLineIndent (int lineNumber)
		{
			return Document.GetLineIndent (lineNumber);
		}
		
		public string GetLineIndent (LineSegment segment)
		{
			return Document.GetLineIndent (segment);
		}
		
		public LineSegment GetLine (int lineNumber)
		{
			return Document.GetLine (lineNumber);
		}
		
		public LineSegment GetLineByOffset (int offset)
		{
			return Document.GetLineByOffset (offset);
		}
		
		public int OffsetToLineNumber (int offset)
		{
			return Document.OffsetToLineNumber (offset);
		}
		
		public IDisposable OpenUndoGroup()
		{
			return Document.OpenUndoGroup ();
		}
		#endregion
		
		#region Parent functions
		public bool HasFocus {
			get {
				return Parent != null ? Parent.HasFocus : false;
			}
		}
		
		public void ScrollToCaret ()
		{
			if (Parent != null)
				Parent.ScrollToCaret ();
		}
		
		public void ScrollTo (int offset)
		{
			if (Parent != null)
				Parent.ScrollTo (offset);
		}
		
		public void ScrollTo (int line, int column)
		{
			if (Parent != null)
				Parent.ScrollTo (line, column);
		}

		public void ScrollTo (DocumentLocation loc)
		{
			if (Parent != null)
				Parent.ScrollTo (loc);
		}
		
		public void CenterToCaret ()
		{
			if (Parent != null)
				Parent.CenterToCaret ();
		}
		
		public void CenterTo (DocumentLocation p)
		{
			if (Parent != null)
				Parent.CenterTo (p);
		}
		
		public void CenterTo (int offset)
		{
			if (Parent != null)
				Parent.CenterTo (offset);
		}
		
		public void CenterTo (int line, int column)
		{
			if (Parent != null)
				Parent.CenterTo (line, column);
		}
		
		public void SetCaretTo (int line, int column)
		{
			SetCaretTo (line, column, true);
		}
		
		public void SetCaretTo (int line, int column, bool highlight)
		{
			SetCaretTo (line, column, highlight, true);
		}
		
		public void SetCaretTo (int line, int column, bool highlight, bool centerCaret)
		{
			if (Parent != null) {
				Parent.SetCaretTo (line, column, highlight, centerCaret);
			} else {
				Caret.Location = new DocumentLocation (line, column);
			}
		}
		#endregion
		
		#region folding
		
		public double LineHeight {
			get;
			internal set;
		}
		
		public int VisibleLineCount {
			get {
				return heightTree.VisibleLineCount;
			}
		}	
		
		
		public double TotalHeight {
			get {
				return heightTree.TotalHeight;
			}
		}
		
		internal HeightTree heightTree;
		
		public DocumentLocation LogicalToVisualLocation (DocumentLocation location)
		{
			int line = LogicalToVisualLine (location.Line);
			LineSegment lineSegment = this.GetLine (location.Line);
			int column = lineSegment != null ? lineSegment.GetVisualColumn (this, location.Column) : location.Column;
			return new DocumentLocation (line, column);
		}

		public DocumentLocation LogicalToVisualLocation (int line, int column)
		{
			return LogicalToVisualLocation (new DocumentLocation (line, column));
		}

		public int LogicalToVisualLine (int logicalLine)
		{
			return heightTree.LogicalToVisualLine (logicalLine);
		}

		public int VisualToLogicalLine (int visualLineNumber)
		{
			return heightTree.VisualToLogicalLine (visualLineNumber);
		}
		
		void HandleTextEditorDataDocumentFoldTreeUpdated (object sender, EventArgs e)
		{
			heightTree.Rebuild ();
		}

		void HandleTextEditorDataDocumentFolded (object sender, FoldSegmentEventArgs e)
		{
			int start = OffsetToLineNumber (e.FoldSegment.StartLine.Offset);
			int end = OffsetToLineNumber (e.FoldSegment.EndLine.Offset);
			
			if (e.FoldSegment.IsFolded) {
				if (e.FoldSegment.Marker != null)
					heightTree.Unfold (e.FoldSegment.Marker, start, end - start);
				e.FoldSegment.Marker = heightTree.Fold (start, end - start);
			} else {
				heightTree.Unfold (e.FoldSegment.Marker, start, end - start);
				e.FoldSegment.Marker = null;
			}
		}
		
		#endregion
	
	
		#region SkipChars
		public class SkipChar
		{
			
			public int Start { get; set; }
			
			public int Offset { get; set; }

			public char Char  { get; set; }

			public override string ToString ()
			{
				return string.Format ("[SkipChar: Start={0}, Offset={1}, Char={2}]", Start, Offset, Char);
			}
		}
		
		List<SkipChar> skipChars = new List<SkipChar> ();
		
		public List<SkipChar> SkipChars {
			get {
				return skipChars;
			}
		}
		
		public void SetSkipChar (int offset, char ch)
		{
			skipChars.Add (new SkipChar () {
				Start = offset - 1,
				Offset = offset,
				Char = ch
			});
		}

		#endregion

		/// <summary>
		/// Creates the a text editor data object which document can't be changed. This is useful for 'view' only
		/// documents.
		/// </summary>
		/// <remarks>
		/// The Document itself is very fast because it uses a special case buffer and line splitter implementation.
		/// Additionally highlighting is turned off as default.
		/// </remarks>
		public static TextEditorData CreateImmutable (string input, bool suppressHighlighting = true)
		{
			return new TextEditorData (TextDocument.CreateImmutableDocument (input, suppressHighlighting));
		}
	}
}
