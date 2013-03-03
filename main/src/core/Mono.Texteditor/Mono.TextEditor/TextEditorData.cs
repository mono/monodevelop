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
using ICSharpCode.NRefactory.Editor;
using Xwt.Drawing;

namespace Mono.TextEditor
{
	public enum SelectionMode {
		Normal,
		Block
	}
	
	public class TextEditorData : IDisposable
	{
		ITextEditorOptions    options;
		readonly TextDocument document; 
		readonly Caret        caret;
		
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
				return currentMode;
			}
			set {
				var oldMode = currentMode;
				currentMode = value;
				currentMode.AddedToEditor (this);
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

		public bool IsDisposed {
			get;
			protected set;
		}

		ISelectionSurroundingProvider selectionSurroundingProvider = new DefaultSelectionSurroundingProvider ();
		public ISelectionSurroundingProvider SelectionSurroundingProvider {
			get {
				return selectionSurroundingProvider;
			}
			set {
				if (value == null)
					throw new ArgumentNullException ("surrounding provider needs to be != null");
				selectionSurroundingProvider = value;
			}
		}

		bool? customTabsToSpaces;
		public bool TabsToSpaces {
			get {
				return customTabsToSpaces.HasValue ? customTabsToSpaces.Value : options.TabsToSpaces;
			}
			set {
				customTabsToSpaces = value;
			}
		}
		#region Tooltip providers
		internal List<TooltipProvider> tooltipProviders = new List<TooltipProvider> ();
		public IEnumerable<TooltipProvider> TooltipProviders {
			get { return tooltipProviders; }
		}

		public void ClearTooltipProviders ()
		{
			foreach (var tp in tooltipProviders) {
				var disposableProvider = tp as IDisposable;
				if (disposableProvider == null)
					continue;
				disposableProvider.Dispose ();
			}
			tooltipProviders.Clear ();
		}
		
		public void AddTooltipProvider (TooltipProvider provider)
		{
			tooltipProviders.Add (provider);
		}
		
		public void RemoveTooltipProvider (TooltipProvider provider)
		{
			tooltipProviders.Remove (provider);
		}
		#endregion

		public TextEditorData () : this (new TextDocument ())
		{
		}

		public TextEditorData (TextDocument doc)
		{
			LineHeight = 16;

			caret = new Caret (this);
			caret.PositionChanged += CaretPositionChanged;

			options = TextEditorOptions.DefaultOptions;
			
			document = doc;
			document.BeginUndo += OnBeginUndo;
			document.EndUndo += OnEndUndo;

			document.Undone += DocumentHandleUndone;
			document.Redone += DocumentHandleRedone;
			document.LineChanged += HandleDocLineChanged;
			document.TextReplaced += HandleTextReplaced;

			document.TextSet += HandleDocTextSet;
			document.Folded += HandleTextEditorDataDocumentFolded;
			document.FoldTreeUpdated += HandleFoldTreeUpdated;
			SearchEngine = new BasicSearchEngine ();

			HeightTree = new HeightTree (this);
			HeightTree.Rebuild ();
		}

		void HandleFoldTreeUpdated (object sender, EventArgs e)
		{
			HeightTree.Rebuild ();
		}

		void HandleDocTextSet (object sender, EventArgs e)
		{
			if (vadjustment != null)
				vadjustment.Value = vadjustment.Lower;
			if (hadjustment != null)
				hadjustment.Value = hadjustment.Lower;
			HeightTree.Rebuild ();
			ClearSelection ();
			caret.SetDocument (document);
		}

		public double GetLineHeight (DocumentLine line)
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
		}

		void HandleTextReplaced (object sender, DocumentChangeEventArgs e)
		{
			caret.UpdateCaretPosition (e);
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
					DocumentLine line = Document.GetLine (DocumentLocation.MinLine);
					if (line.DelimiterLength > 0) 
						eol = Document.GetTextAt (line.Length, line.DelimiterLength);
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
		
		ColorScheme colorStyle;
		public ColorScheme ColorStyle {
			get {
				return colorStyle ?? Mono.TextEditor.Highlighting.SyntaxModeService.DefaultColorStyle;
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
			while (curOffset < offset + length && curOffset < Document.TextLength) {
				DocumentLine line = Document.GetLineByOffset (curOffset);
				int toOffset = System.Math.Min (line.Offset + line.Length, offset + length);
				var styleStack = new Stack<ChunkStyle> ();

				foreach (var chunk in mode.GetChunks (ColorStyle, line, curOffset, toOffset - curOffset)) {
					var chunkStyle = ColorStyle.GetChunkStyle (chunk);
					bool setBold = (styleStack.Count > 0 && styleStack.Peek ().FontWeight != chunkStyle.FontWeight) || 
						chunkStyle.FontWeight != FontWeight.Normal;
					bool setItalic = (styleStack.Count > 0 && styleStack.Peek ().FontStyle != chunkStyle.FontStyle) || 
						chunkStyle.FontStyle != FontStyle.Normal;
					bool setUnderline = chunkStyle.Underline && (styleStack.Count == 0 || !styleStack.Peek ().Underline) ||
							!chunkStyle.Underline && (styleStack.Count == 0 || styleStack.Peek ().Underline);
					bool setColor = styleStack.Count == 0 || TextViewMargin.GetPixel (styleStack.Peek ().Foreground) != TextViewMargin.GetPixel (chunkStyle.Foreground);
					if (setColor || setBold || setItalic || setUnderline) {
						if (styleStack.Count > 0) {
							result.Append ("</span>");
							styleStack.Pop ();
						}
						result.Append ("<span");
						if (useColors) {
							result.Append (" foreground=\"");
							result.Append (SyntaxMode.ColorToPangoMarkup (chunkStyle.Foreground));
							result.Append ("\"");
						}
						if (chunkStyle.FontWeight != Xwt.Drawing.FontWeight.Normal)
							result.Append (" weight=\"" + chunkStyle.FontWeight + "\"");
						if (chunkStyle.FontStyle != Xwt.Drawing.FontStyle.Normal)
							result.Append (" style=\"" + chunkStyle.FontStyle + "\"");
						if (chunkStyle.Underline)
							result.Append (" underline=\"single\"");
						result.Append (">");
						styleStack.Push (chunkStyle);
					}

					for (int i = 0; i < chunk.Length && chunk.Offset + i < Document.TextLength; i++) {
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
					result.Append ("</span>");
					styleStack.Pop ();
				}

				curOffset = line.EndOffsetIncludingDelimiter;
				if (removeIndent)
					curOffset += indentLength;
				if (result.Length > 0 && curOffset < offset + length)
					result.AppendLine ();
			}
			return result.ToString ();
		}

		public IEnumerable<Chunk> GetChunks (DocumentLine line, int offset, int length)
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
		
		public void Remove (DocumentRegion region)
		{
			Remove (region.GetSegment (document));
		}

		public string FormatString (DocumentLocation loc, string str)
		{
			if (string.IsNullOrEmpty (str))
				return "";
			StringBuilder sb = new StringBuilder ();
			bool convertTabs = TabsToSpaces;
			
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
						loc = new DocumentLocation (loc.Line, loc.Column + tabWidth);
					} else 
						goto default;
					break;
				case '\r':
					if (i + 1 < str.Length && str [i + 1] == '\n')
						i++;
					goto case '\n';
				case '\n':
					sb.Append (EolMarker);
					loc = new DocumentLocation (loc.Line + 1, 1);
					break;
				default:
					sb.Append (ch);
					loc = new DocumentLocation (loc.Line, loc.Column + 1);
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
			document.Replace (offset, count, formattedString);
			return formattedString.Length;
		}
			
		public void InsertAtCaret (string text)
		{
			if (String.IsNullOrEmpty (text))
				return;
			using (var undo = OpenUndoGroup ()) {
				if (IsSomethingSelected && MainSelection.SelectionMode == SelectionMode.Block) {
					var visualInsertLocation = LogicalToVisualLocation (MainSelection.Anchor);
					var selection = MainSelection;
					Caret.PreserveSelection = true;
					for (int lineNumber = selection.MinLine; lineNumber <= selection.MaxLine; lineNumber++) {
						var lineSegment = GetLine (lineNumber);
						int insertOffset = lineSegment.GetLogicalColumn (this, visualInsertLocation.Column) - 1;
						string textToInsert;
						if (lineSegment.Length < insertOffset) {
							int visualLastColumn = lineSegment.GetVisualColumn (this, lineSegment.Length + 1);
							int charsToInsert = visualInsertLocation.Column - visualLastColumn;
							int spaceCount = charsToInsert % Options.TabSize;
							textToInsert = new string ('\t', (charsToInsert - spaceCount) / Options.TabSize) + new string (' ', spaceCount) + text;
							insertOffset = lineSegment.Length;
						} else {
							textToInsert = text;
						}
						Insert (lineSegment.Offset + insertOffset, textToInsert);
					}
					MainSelection = new Selection (
								new DocumentLocation (selection.Anchor.Line, Caret.Column),
								new DocumentLocation (selection.Lead.Line, Caret.Column),
								Mono.TextEditor.SelectionMode.Block);
					Caret.PreserveSelection = false;
					Document.CommitMultipleLineUpdate (selection.MinLine, selection.MaxLine);
				} else {
					EnsureCaretIsNotVirtual ();
					Insert (Caret.Offset, text);
				}
			}
		}

		void DetachDocument ()
		{
			document.BeginUndo -= OnBeginUndo;
			document.EndUndo -= OnEndUndo;

			document.Undone -= DocumentHandleUndone;
			document.Redone -= DocumentHandleRedone;
			document.LineChanged -= HandleDocLineChanged;
			document.TextReplaced -= HandleTextReplaced;

			document.TextSet -= HandleDocTextSet;
			document.Folded -= HandleTextEditorDataDocumentFolded;
			document.FoldTreeUpdated -= HandleFoldTreeUpdated;
		}

		public void Dispose ()
		{
			if (IsDisposed)
				return;
			document.WaitForFoldUpdateFinished ();
			IsDisposed = true;
			options = options.Kill ();
			HeightTree.Dispose ();
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
			if (line != null && line.Length > 0 && GetIndentationString (Caret.Location) == Document.GetTextAt (line.Offset, line.Length))
				Remove (line.Offset, line.Length);
		}

		public void FixVirtualIndentation (int lineNumber)
		{
			if (!HasIndentationTracker || Options.IndentStyle != IndentStyle.Virtual)
				return;
			var line = Document.GetLine (lineNumber);
			if (line != null && line.Length > 0 && GetIndentationString (lineNumber, line.Length + 1) == Document.GetTextAt (line.Offset, line.Length))
				Remove (line.Offset, line.Length);
		}

		void CaretPositionChanged (object sender, DocumentLocationEventArgs args)
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
			return options.WordFindStrategy.FindNextWordOffset (Document, offset);
		}
		
		public int FindPrevWordOffset (int offset)
		{
			return options.WordFindStrategy.FindPrevWordOffset (Document, offset);
		}
		
		public int FindNextSubwordOffset (int offset)
		{
			return options.WordFindStrategy.FindNextSubwordOffset (Document, offset);
		}

		public int FindPrevSubwordOffset (int offset)
		{
			return options.WordFindStrategy.FindPrevSubwordOffset (Document, offset);
		}
		
		public int FindCurrentWordEnd (int offset)
		{
			return Options.WordFindStrategy.FindCurrentWordEnd (Document, offset);
		}
		
		public int FindCurrentWordStart (int offset)
		{
			return Options.WordFindStrategy.FindCurrentWordStart (Document, offset);
		}
		
		public delegate void PasteCallback (int insertionOffset, string text, int insertedChars);
		
		public event PasteCallback Paste;
		
		public void PasteText (int insertionOffset, string text, int insertedChars)
		{
			if (Paste != null)
				Paste (insertionOffset, text, insertedChars);
		}
		
		#region undo/redo handling
		DocumentLocation savedCaretPos;
		Selection savedSelection;
		//List<TextEditorDataState> states = new List<TextEditorDataState> ();

		void OnBeginUndo (object sender, EventArgs args)
		{
			savedCaretPos  = Caret.Location;
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
			var state = e.Operation.Tag as TextEditorDataState;
			if (state != null)
				state.UndoState ();
		}

		void DocumentHandleRedone (object sender, TextDocument.UndoOperationEventArgs e)
		{
			var state = e.Operation.Tag as TextEditorDataState;
			if (state != null)
				state.RedoState ();
		}

		class TextEditorDataState
		{
			DocumentLocation undoCaretPos;
			Selection undoSelection;

			DocumentLocation redoCaretPos;
			Selection redoSelection;
			
			TextEditorData editor;
			
			public TextEditorDataState (TextEditorData editor, DocumentLocation caretPos, Selection selection)
			{
				this.editor        = editor;
				undoCaretPos  = caretPos;
				undoSelection = selection;
				
				redoCaretPos  = editor.Caret.Location;
				redoSelection = Mono.TextEditor.Selection.Clone (editor.MainSelection);
			}
			
			public void UndoState ()
			{
				editor.Caret.Location = undoCaretPos;
				editor.MainSelection = undoSelection;
			}
			
			public void RedoState ()
			{
				editor.Caret.Location = redoCaretPos;
				editor.MainSelection = redoSelection;
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
			var handler = SelectionChanging;
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

		/// <summary>
		/// Gets or sets the selection range. If nothing is selected (Caret.Offset, 0) is returned.
		/// </summary>
		public TextSegment SelectionRange {
			get {
				return MainSelection != null ? MainSelection.GetSelectionRange (this) : new TextSegment (Caret.Offset, 0);
			}
			set {
				if (SelectionRange != value) {
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
				return Document.GetTextAt (SelectionRange);
			}
			set {
				if (!IsSomethingSelected)
					return;
				var selection = SelectionRange;
				Replace (selection.Offset, selection.Length, value);
				if (Caret.Offset > selection.Offset)
					Caret.Offset = selection.Offset + value.Length;
				SelectionRange = new TextSegment (selection.Offset, value.Length);
			}
		}
		
		
		public IEnumerable<DocumentLine> SelectedLines {
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
			if (!IsSomethingSelected)
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
			var anchor = document.OffsetToLocation (anchorOffset);
			var lead = document.OffsetToLocation (leadOffset);
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
			                               document.OffsetToLocation (Document.GetLine (to).EndOffsetIncludingDelimiter));
		}

		internal void DeleteSelection (Selection selection)
		{
			if (selection == null)
				throw new ArgumentNullException ("selection");
			switch (selection.SelectionMode) {
			case SelectionMode.Normal:
				var segment = selection.GetSelectionRange (this);
				int len = System.Math.Min (segment.Length, Document.TextLength - segment.Offset);
				var loc = selection.Anchor < selection.Lead ? selection.Anchor : selection.Lead;
				caret.Location = loc;
				EnsureCaretIsNotVirtual ();
				if (len > 0)
					Remove (segment.Offset, len);
				caret.Location = loc;
				break;
			case SelectionMode.Block:
				DocumentLocation visStart = LogicalToVisualLocation (selection.Anchor);
				DocumentLocation visEnd = LogicalToVisualLocation (selection.Lead);
				int startCol = System.Math.Min (visStart.Column, visEnd.Column);
				int endCol = System.Math.Max (visStart.Column, visEnd.Column);
				bool preserve = Caret.PreserveSelection;
				Caret.PreserveSelection = true;
				for (int lineNr = selection.MinLine; lineNr <= selection.MaxLine; lineNr++) {
					DocumentLine curLine = Document.GetLine (lineNr);
					int col1 = curLine.GetLogicalColumn (this, startCol) - 1;
					int col2 = System.Math.Min (curLine.GetLogicalColumn (this, endCol) - 1, curLine.Length);
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
					EnsureIsNotVirtual (selection.Anchor);
					EnsureIsNotVirtual (selection.Lead);
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
				searchOffset = Document.TextLength - 1;
			} else {
				searchOffset = (startOffset + Document.TextLength - 1) % Document.TextLength;
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
			if (IsSomethingSelected) {
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
				if (!SearchRequest.SearchRegion.IsInvalid)
					offset = SearchRequest.SearchRegion.Offset;
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
			return EnsureIsNotVirtual (Caret.Location);
		}

		int EnsureIsNotVirtual (DocumentLocation loc)
		{
			return EnsureIsNotVirtual (loc.Line, loc.Column);
		}

		int EnsureIsNotVirtual (int line, int column)
		{
			Debug.Assert (document.IsInAtomicUndo);
			DocumentLine documentLine = Document.GetLine (line);
			if (documentLine == null)
				return 0;
			if (column > documentLine.Length + 1) {
				string virtualSpace;
				if (HasIndentationTracker && documentLine.Length == 0) {
					virtualSpace = GetIndentationString (line, column);
				} else {
					virtualSpace = new string (' ', column - 1 - documentLine.Length);
				}
				var oldPreserve = Caret.PreserveSelection;
				Caret.PreserveSelection = true;
				Insert (documentLine.Offset, virtualSpace);
				Caret.PreserveSelection = oldPreserve;
				
				// No need to reposition the caret, because it's already at the correct position
				// The only difference is that the position is not virtual anymore.
				return virtualSpace.Length;
			}
			return 0;
		}

		#endregion
		
		public Stream OpenStream ()
		{
			return new MemoryStream (Encoding.UTF8.GetBytes (Document.Text), false);
		}
		
		public void RaiseUpdateAdjustmentsRequested ()
		{
			OnUpdateAdjustmentsRequested (EventArgs.Empty);
		}
		
		protected virtual void OnUpdateAdjustmentsRequested (EventArgs e)
		{
			var handler = UpdateAdjustmentsRequested;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler UpdateAdjustmentsRequested;

		public void RequestRecenter ()
		{
			var handler = RecenterEditor;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}
		public event EventHandler RecenterEditor;

		#region Document delegation
		public int Length {
			get {
				return document.TextLength;
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

		public ITextSourceVersion Version {
			get {
				return document.Version;
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
		
		public string GetTextAt (DocumentRegion region)
		{
			return document.GetTextAt (region);
		}

		public string GetTextAt (TextSegment segment)
		{
			return document.GetTextAt (segment);
		}
		
		public char GetCharAt (int offset)
		{
			return document.GetCharAt (offset);
		}
		
		public char GetCharAt (DocumentLocation location)
		{
			return document.GetCharAt (location);
		}

		public char GetCharAt (int line, int column)
		{
			return document.GetCharAt (line, column);
		}

		public string GetLineText (int line)
		{
			return Document.GetLineText (line);
		}
		
		public string GetLineText (int line, bool includeDelimiter)
		{
			return Document.GetLineText (line, includeDelimiter);
		}

		public IEnumerable<DocumentLine> Lines {
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
		
		public string GetLineIndent (DocumentLine segment)
		{
			return Document.GetLineIndent (segment);
		}
		
		public DocumentLine GetLine (int lineNumber)
		{
			return Document.GetLine (lineNumber);
		}
		
		public DocumentLine GetLineByOffset (int offset)
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
				return HeightTree.VisibleLineCount;
			}
		}	
		
		
		public double TotalHeight {
			get {
				return HeightTree.TotalHeight;
			}
		}
		
		public readonly HeightTree HeightTree;
		
		public DocumentLocation LogicalToVisualLocation (DocumentLocation location)
		{
			int line = LogicalToVisualLine (location.Line);
			var lineSegment = GetLine (location.Line);
			int column = lineSegment != null ? lineSegment.GetVisualColumn (this, location.Column) : location.Column;
			return new DocumentLocation (line, column);
		}

		public DocumentLocation LogicalToVisualLocation (int line, int column)
		{
			return LogicalToVisualLocation (new DocumentLocation (line, column));
		}

		public int LogicalToVisualLine (int logicalLine)
		{
			return HeightTree.LogicalToVisualLine (logicalLine);
		}

		public int VisualToLogicalLine (int visualLineNumber)
		{
			return HeightTree.VisualToLogicalLine (visualLineNumber);
		}
		

		void HandleTextEditorDataDocumentFolded (object sender, FoldSegmentEventArgs e)
		{
			int start = e.FoldSegment.StartLine.LineNumber;
			int end = e.FoldSegment.EndLine.LineNumber;
			
			if (e.FoldSegment.IsFolded) {
				if (e.FoldSegment.Marker != null)
					HeightTree.Unfold (e.FoldSegment.Marker, start, end - start);
				e.FoldSegment.Marker = HeightTree.Fold (start, end - start);
			} else {
				HeightTree.Unfold (e.FoldSegment.Marker, start, end - start);
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
