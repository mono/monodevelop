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
using Xwt.Drawing;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Threading;
using MonoDevelop.Ide;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace Mono.TextEditor
{
	class TextEditorData : IDisposable
	{
		ITextEditorOptions    options;
		TextDocument document; 
		readonly CaretImpl        caret;
		
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
				OnEditModeChanged (new EditModeChangedEventArgs (oldMode, currentMode));
			}
		}

		protected virtual void OnEditModeChanged (EditModeChangedEventArgs e)
		{
			var handler = EditModeChanged;
			if (handler != null)
				handler (this, e);
		}

		/// <summary>
		/// Occurs when the edit mode changed.
		/// </summary>
		public event EventHandler<EditModeChangedEventArgs> EditModeChanged;
		
		public MonoTextEditor Parent {
			get;
			set;
		}
		
		public string FileName {
			get {
				return Document != null ? Document.FileName : null;
			}
		}
		
		public string MimeType {
			get {
				return Document != null ? Document.MimeType : null;
			}
		}

		public bool IsDisposed {
			get;
			protected set;
		}

		SelectionSurroundingProvider selectionSurroundingProvider;
		public SelectionSurroundingProvider SelectionSurroundingProvider {
			get {
				return selectionSurroundingProvider;
			}
			set {
				if (value == null)
					throw new ArgumentNullException (nameof (value), "surrounding provider needs to be != null");
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

		bool? customShowRuler;
		public bool ShowRuler {
			get {
				return customShowRuler.HasValue ? customShowRuler.Value : options.ShowRuler;
			}
			set {
				customShowRuler = value;
			}
		}

		bool? customHighlightCaretLine;
		public bool HighlightCaretLine {
			get {
				return !this.IsSomethingSelected && (customHighlightCaretLine.HasValue ? customHighlightCaretLine.Value : options.HighlightCaretLine);
			}
			set {
				customHighlightCaretLine = value;
			}
		}

		#region Tooltip providers
		internal List<TooltipProvider> tooltipProviders = new List<TooltipProvider> ();
		public IEnumerable<TooltipProvider> TooltipProviders {
			get { return tooltipProviders; }
		}
		
		/// <summary>
		/// If set the tooltips wont show up.
		/// </summary>
		public bool SuppressTooltips {
			get;
			set;
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

			caret = new CaretImpl (this);
			caret.PositionChanged += CaretPositionChanged;

			options = TextEditorOptions.DefaultOptions;
			document = doc;
			AttachDocument ();
			SearchEngine = new BasicSearchEngine ();

			HeightTree = new HeightTree (this);
			HeightTree.Rebuild ();
			IndentationTracker = new DefaultIndentationTracker (document);
			selectionSurroundingProvider = new DefaultSelectionSurroundingProvider (this);
		}

		void AttachDocument ()
		{
			if (document == null)
				return;
			document.BeginUndo += OnBeginUndo;
			document.EndUndo += OnEndUndo;
			document.Undone += DocumentHandleUndone;
			document.Redone += DocumentHandleRedone;
			document.TextChanged += HandleTextReplaced;
			document.Folded += HandleTextEditorDataDocumentFolded;
			document.FoldTreeUpdated += HandleFoldTreeUpdated;
			document.HeightChanged += Document_HeightChanged;
		}

		void Document_HeightChanged (object sender, EventArgs e)
		{
			HeightTree.Rebuild ();
		}

		void HandleFoldTreeUpdated (object sender, EventArgs e)
		{
			HeightTree.Rebuild ();
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
		
		public TextDocument Document {
			get {
				return document;
			}
		}

		void HandleTextReplaced (object sender, TextChangeEventArgs e)
		{
			caret.UpdateCaretPosition (e);

			if (Options.TabsToSpaces && document.IsTextSet && !document.IsInUndo) {
				string tabReplacement = new string (' ', Options.TabSize);
				var newChanges = new List<Microsoft.CodeAnalysis.Text.TextChange> ();
				for (int i = 0; i < e.TextChanges.Count; ++i) {
					var change = e.TextChanges[i];
					string replaceText = change.InsertedText.Text.Replace ("\t", tabReplacement);
					if (replaceText.Length != change.InsertedText.Length) {
						newChanges.Add (new Microsoft.CodeAnalysis.Text.TextChange (new Microsoft.CodeAnalysis.Text.TextSpan (change.NewOffset, change.InsertionLength), replaceText)); 
					}
				}
				if (newChanges.Count > 0)
					document.ApplyTextChanges (newChanges);
			}
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
				if (Document.LineCount > 0) {
					var line = Document.GetLine (DocumentLocation.MinLine);
					switch (line.UnicodeNewline) {
					case UnicodeNewline.LF:
						return "\u000A";
					case UnicodeNewline.CRLF:
						return "\u000D\u000A";
					case UnicodeNewline.CR:
						return "\u000D";
					case UnicodeNewline.NEL:
						return "\u0085";
					//case UnicodeNewline.VT:
					//	return "\u000B";
					//case UnicodeNewline.FF:
					//	return "\u000C";
					case UnicodeNewline.LS:
						return "\u2028";
					case UnicodeNewline.PS:
						return "\u2029";
					}
				} 
				return Options.DefaultEolMarker;
			}
		}
		
		internal ITextEditorOptions Options {
			get {
				return options;
			}
			set {
				options = value;
			}
		}
		
		public CaretImpl Caret {
			get {
				return caret;
			}
		}

		MonoDevelop.Ide.Editor.Highlighting.EditorTheme colorStyle;
		internal MonoDevelop.Ide.Editor.Highlighting.EditorTheme ColorStyle {
			get {
				return colorStyle ?? SyntaxHighlightingService.DefaultColorStyle;
			}
			set {
				colorStyle = value;
			}
		}

		internal MonoDevelop.Ide.Editor.Highlighting.EditorTheme EditorTheme {
			get {
				return MonoDevelop.Ide.Editor.Highlighting.SyntaxHighlightingService.GetEditorTheme (Options.EditorThemeName);
			}
		}

		string ConvertToPangoMarkup (string str, bool replaceTabs = true)
		{
			if (str == null)
				throw new ArgumentNullException ("str");
			var result = StringBuilderCache.Allocate ();
			foreach (char ch in str) {
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
			return StringBuilderCache.ReturnAndFree (result);
		}

		internal static int CalcIndentLength (string indent)
		{
			int result = 0;
			foreach (var ch in indent) {
				if (ch == '\t') {
					result = result - result % DefaultSourceEditorOptions.Instance.TabSize + DefaultSourceEditorOptions.Instance.TabSize;
				} else {
					result++;
				}
			}
			return result;
		}


		internal static int CalcOffset (string indent, int indentLength)
		{
			int result = 0;
			int offset = 0;
			foreach (var ch in indent) {
				if (ch == '\t') {
					result = result - result % DefaultSourceEditorOptions.Instance.TabSize + DefaultSourceEditorOptions.Instance.TabSize;
				} else {
					result++;
				}
				if (result > indentLength)
					return offset;
				offset++;
			}
			return offset;
		}

		public string GetMarkup (int offset, int length, bool removeIndent, bool useColors = true, bool replaceTabs = true, bool fitIdeStyle = false)
		{
			var mode = Document.SyntaxMode;
			var style = fitIdeStyle ? SyntaxHighlightingService.GetEditorTheme(Parent.GetIdeColorStyleName()) : ColorStyle;

			if (style == null) {
				var str = Document.GetTextAt (offset, length);
				if (removeIndent)
					str = str.TrimStart (' ', '\t');
				return ConvertToPangoMarkup (str, replaceTabs);
			}
			int indentLength = -1;
			int curOffset = offset;

			StringBuilder result = StringBuilderCache.Allocate ();
			while (curOffset < offset + length && curOffset < Document.Length) {
				DocumentLine line = Document.GetLineByOffset (curOffset);
				int toOffset = System.Math.Min (line.Offset + line.Length, offset + length);
				var styleStack = new Stack<MonoDevelop.Ide.Editor.Highlighting.ChunkStyle> ();
				if (removeIndent) {
					var indentString = line.GetIndentation (Document);
					var curIndent = CalcIndentLength (indentString);
					if (indentLength < 0) {
						indentLength = curIndent;
					} else {
						curOffset += CalcOffset (indentString, System.Math.Min (curIndent, indentLength));
					}
				}

				foreach (var chunk in GetChunks (line, curOffset, toOffset - curOffset).WaitAndGetResult (default (CancellationToken))) {
					if (chunk.Length == 0)
						continue;
					var chunkStyle = style.GetChunkStyle (chunk.ScopeStack);
					bool setBold = (styleStack.Count > 0 && styleStack.Peek ().FontWeight != chunkStyle.FontWeight) || 
						chunkStyle.FontWeight != FontWeight.Normal;
					bool setItalic = (styleStack.Count > 0 && styleStack.Peek ().FontStyle != chunkStyle.FontStyle) || 
						chunkStyle.FontStyle != FontStyle.Normal;
					bool setUnderline = chunkStyle.Underline && (styleStack.Count == 0 || styleStack.Peek ().Underline) ||
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
							result.Append (chunkStyle.Foreground.ToPangoString ());
							result.Append ("\"");
						}
						if (chunkStyle.FontWeight != Xwt.Drawing.FontWeight.Normal)
							result.Append (" weight=\"").Append (chunkStyle.FontWeight.ToString ()).Append ("\"");
						if (chunkStyle.FontStyle != Xwt.Drawing.FontStyle.Normal)
							result.Append (" style=\"").Append (chunkStyle.FontStyle.ToString ()).Append ("\"");
						if (chunkStyle.Underline)
							result.Append (" underline=\"single\"");
						result.Append (">");
						styleStack.Push (chunkStyle);
					}
					result.Append (ConvertToPangoMarkup (Document.GetTextBetween (chunk.Offset, System.Math.Min (chunk.EndOffset, Document.Length)), replaceTabs));
				}
				while (styleStack.Count > 0) {
					result.Append ("</span>");
					styleStack.Pop ();
				}

				curOffset = line.EndOffsetIncludingDelimiter;
				if (result.Length > 0 && curOffset < offset + length)
					result.AppendLine ();
			}
			return StringBuilderCache.ReturnAndFree (result);
		}

		internal async Task<IEnumerable<MonoDevelop.Ide.Editor.Highlighting.ColoredSegment>> GetChunks (DocumentLine line, int offset, int length)
		{
			var lineOffset = line.Offset;
			return TextViewMargin.TrimChunks (
				(await document.SyntaxMode.GetHighlightedLineAsync (line, CancellationToken.None).ConfigureAwait(false))
				        .Segments
				        .Select (c => c.WithOffset (c.Offset + lineOffset))
				        .ToList (), offset, length);
		}
	
		public int Insert (int offset, string value)
		{
			return Replace (offset, 0, value);
		}
		
		public void Remove (int offset, int count)
		{
			Replace (offset, count, null);
		}
		
		public void Remove (ISegment removeSegment)
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
			StringBuilder sb = StringBuilderCache.Allocate ();
			bool convertTabs = TabsToSpaces;
			var tabSize = Options.TabSize;
			for (int i = 0; i < str.Length; i++) {
				char ch = str [i];
				switch (ch) {
				case '\u00A0': // convert non breaking spaces to standard spaces.
					sb.Append (' ');
					break;
				case '\t':
					if (convertTabs) {
						int tabWidth = TextViewMargin.GetNextTabstop (this, loc.Column, tabSize) - loc.Column;
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
			return StringBuilderCache.ReturnAndFree (sb);
		}
		
		public string FormatString (int offset, string str)
		{
			return FormatString (Document.OffsetToLocation (offset), str);
		}
		
		public int Replace (int offset, int count, string value)
		{
			string formattedString = FormatString (offset, value);
			document.ReplaceText (offset, count, formattedString);
			return formattedString.Length;
		}
			
		public void InsertAtCaret (string text)
		{
			if (String.IsNullOrEmpty (text))
				return;
			using (var undo = OpenUndoGroup ()) {
				DeleteSelectedText (IsSomethingSelected ? MainSelection.SelectionMode != MonoDevelop.Ide.Editor.SelectionMode.Block : true);
				// Needs to be called after delete text, delete text handles virtual caret postitions itself,
				// but afterwards the virtual position may need to be restored.
				EnsureCaretIsNotVirtual ();

				if (IsSomethingSelected && MainSelection.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block) {
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
					var visualColumn = GetLine (Caret.Location.Line).GetVisualColumn (this, Caret.Column);
					MainSelection = new MonoDevelop.Ide.Editor.Selection (
						new DocumentLocation (selection.Anchor.Line, GetLine (selection.Anchor.Line).GetLogicalColumn (this, visualColumn)),
						new DocumentLocation (selection.Lead.Line, GetLine (selection.Lead.Line).GetLogicalColumn (this, visualColumn)),
						MonoDevelop.Ide.Editor.SelectionMode.Block
					);
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
			if (document == null)
				return;
			document.BeginUndo -= OnBeginUndo;
			document.EndUndo -= OnEndUndo;

			document.Undone -= DocumentHandleUndone;
			document.Redone -= DocumentHandleRedone;
			document.TextChanged -= HandleTextReplaced;

			document.Folded -= HandleTextEditorDataDocumentFolded;
			document.FoldTreeUpdated -= HandleFoldTreeUpdated;
			document.HeightChanged -= Document_HeightChanged;
			document.Dispose ();
			document = null;
		}

		public void Dispose ()
		{
			if (IsDisposed)
				return;
			document.InterruptFoldWorker ();
			IsDisposed = true;
			options = options.Kill ();
			HeightTree.Dispose ();
			DetachDocument ();
			ClearTooltipProviders ();
			DisposeIndentationTracker();
			tooltipProviders = null;
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
			if (line != null && line.Length > 0 && GetIndentationString (caret.Line - 1, int.MaxValue) == Document.GetTextAt (line.Offset, line.Length))
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
			if (document.IsReadOnly)
				return false;
			if (document.ReadOnlyCheckDelegate != null)
				return document.ReadOnlyCheckDelegate (line);
			return true;
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

		#region undo/redo handling
		DocumentLocation savedCaretPos;
		MonoDevelop.Ide.Editor.Selection savedSelection;
		//List<TextEditorDataState> states = new List<TextEditorDataState> ();

		void OnBeginUndo (object sender, EventArgs args)
		{
			savedCaretPos  = Caret.Location;
			savedSelection = MainSelection;
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
			MonoDevelop.Ide.Editor.Selection undoSelection;

			DocumentLocation redoCaretPos;
			MonoDevelop.Ide.Editor.Selection redoSelection;
			
			TextEditorData editor;
			
			public TextEditorDataState (TextEditorData editor, DocumentLocation caretPos, MonoDevelop.Ide.Editor.Selection selection)
			{
				this.editor        = editor;
				undoCaretPos  = caretPos;
				undoSelection = selection;
				
				redoCaretPos  = editor.Caret.Location;
				redoSelection = editor.MainSelection;
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
				return !MainSelection.IsEmpty && MainSelection.Anchor != MainSelection.Lead; 
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
					return !document.IsReadOnly;
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
		
		public MonoDevelop.Ide.Editor.SelectionMode SelectionMode {
			get {
				return !MainSelection.IsEmpty ? MainSelection.SelectionMode : MonoDevelop.Ide.Editor.SelectionMode.Normal;
			}
			set {
				if (MainSelection.IsEmpty)
					return;
				MainSelection = MainSelection.WithSelectionMode (value);
			}
		}
		
		MonoDevelop.Ide.Editor.Selection mainSelection = MonoDevelop.Ide.Editor.Selection.Empty;
		public MonoDevelop.Ide.Editor.Selection MainSelection {
			get {
				return mainSelection;
			}
			set {
				if (mainSelection.IsEmpty && value.IsEmpty)
					return;
				if (mainSelection.IsEmpty && !value.IsEmpty || !mainSelection.IsEmpty && value.IsEmpty || !mainSelection.Equals (value)) {
					OnSelectionChanging (EventArgs.Empty);
					mainSelection = value;
					OnSelectionChanged (EventArgs.Empty);
				}
			}
		}

		public IEnumerable<MonoDevelop.Ide.Editor.Selection> Selections {
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
				if (MainSelection.IsEmpty)
					return -1;
				return MainSelection.GetAnchorOffset (this);
			}
			set {
				DocumentLocation location = Document.OffsetToLocation (value);
				if (mainSelection.IsEmpty) {
					MainSelection = new MonoDevelop.Ide.Editor.Selection (location, location);
				} else {
					if (MainSelection.Lead == location) {
						MainSelection = MainSelection.WithLead (MainSelection.Anchor);
					} else {
						MainSelection = MainSelection.WithAnchor (location);
					}
				}
			}
		}

		public int SelectionLead {
			get {
				if (MainSelection.IsEmpty)
					return -1;
				return MainSelection.GetLeadOffset (this);
			}
			set {
				DocumentLocation location = Document.OffsetToLocation (value);
				if (mainSelection.IsEmpty) {
					MainSelection = new MonoDevelop.Ide.Editor.Selection (location, location);
				} else {
					if (MainSelection.Anchor == location) {
						MainSelection = MainSelection.WithAnchor (MainSelection.Lead);
					} else {
						MainSelection = MainSelection.WithLead (location);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the selection range. If nothing is selected (Caret.Offset, 0) is returned.
		/// </summary>
		public ISegment SelectionRange {
			get {
				return !MainSelection.IsEmpty ? MainSelection.GetSelectionRange (this) : new TextSegment (Caret.Offset, 0);
			}
			set {
				if (SelectionRange != value) {
					OnSelectionChanging (EventArgs.Empty);
					if (value.Length == 0) {
						MainSelection = MonoDevelop.Ide.Editor.Selection.Empty;
					} else {
						DocumentLocation loc1 = document.OffsetToLocation (value.Offset);
						DocumentLocation loc2 = document.OffsetToLocation (value.EndOffset);
						if (MainSelection.IsEmpty) {
							MainSelection = new MonoDevelop.Ide.Editor.Selection (loc1, loc2);
						} else {
							if (MainSelection.Anchor == loc1) {
								MainSelection = MainSelection.WithLead (loc2);
							} else if (MainSelection.Anchor == loc2) {
								MainSelection = MainSelection.WithLead (loc1);
							} else {
								MainSelection = new MonoDevelop.Ide.Editor.Selection (loc1, loc2);
							}
						}
						
					}
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
			MainSelection = MonoDevelop.Ide.Editor.Selection.Empty;
		}
		
		public void ExtendSelectionTo (DocumentLocation location)
		{
			if (MainSelection.IsEmpty) {
				MainSelection = new MonoDevelop.Ide.Editor.Selection (location, location);
			} else {
				MainSelection = MainSelection.WithLead (location);
			}
		}
		
		public void SetSelection (int anchorOffset, int leadOffset)
		{
			if (anchorOffset == leadOffset) {
				MainSelection = MonoDevelop.Ide.Editor.Selection.Empty;
				return;
			}
			var anchor = document.OffsetToLocation (anchorOffset);
			var lead = document.OffsetToLocation (leadOffset);
			MainSelection = new MonoDevelop.Ide.Editor.Selection (anchor, lead);
		}

		public void SetSelection (DocumentLocation anchor, DocumentLocation lead)
		{
			MainSelection = anchor == lead ? MonoDevelop.Ide.Editor.Selection.Empty : new MonoDevelop.Ide.Editor.Selection (anchor, lead);
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
			MainSelection = new MonoDevelop.Ide.Editor.Selection (document.OffsetToLocation (Document.GetLine (from).Offset), 
			                               document.OffsetToLocation (Document.GetLine (to).EndOffsetIncludingDelimiter));
		}

		internal void DeleteSelection (MonoDevelop.Ide.Editor.Selection selection)
		{
			if (selection.IsEmpty)
				throw new ArgumentNullException ("selection was empty.");
			switch (selection.SelectionMode) {
			case MonoDevelop.Ide.Editor.SelectionMode.Normal:
				var segment = selection.GetSelectionRange (this);
				int len = System.Math.Min (segment.Length, Document.Length - segment.Offset);
				var loc = selection.Anchor < selection.Lead ? selection.Anchor : selection.Lead;
				caret.Location = loc;
				EnsureCaretIsNotVirtual ();
				if (len > 0)
					Remove (segment.Offset, len);
				break;
			case MonoDevelop.Ide.Editor.SelectionMode.Block:
				DocumentLocation visStart = LogicalToVisualLocation (selection.Anchor);
				DocumentLocation visEnd = LogicalToVisualLocation (selection.Lead);
				int startCol = System.Math.Min (visStart.Column, visEnd.Column);
				int endCol = System.Math.Max (visStart.Column, visEnd.Column);
				bool preserve = Caret.PreserveSelection;
				Caret.PreserveSelection = true;
				var changes = new List<Microsoft.CodeAnalysis.Text.TextChange> ();

				for (int lineNr = selection.MinLine; lineNr <= selection.MaxLine; lineNr++) {
					DocumentLine curLine = Document.GetLine (lineNr);
					int col1 = curLine.GetLogicalColumn (this, startCol) - 1;
					int col2 = System.Math.Min (curLine.GetLogicalColumn (this, endCol) - 1, curLine.Length);
					if (col1 >= col2)
						continue;
					changes.Add (new Microsoft.CodeAnalysis.Text.TextChange (new Microsoft.CodeAnalysis.Text.TextSpan (curLine.Offset + col1, col2 - col1), ""));
				}
				Document.ApplyTextChanges (changes);
				int column = System.Math.Min (selection.Anchor.Column, selection.Lead.Column);
				MainSelection = selection.WithRange (
					new DocumentLocation (selection.Anchor.Line, column),
					new DocumentLocation (selection.Lead.Line, column)
				);
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
				foreach (var selection in Selections) {
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
			if (SearchEngine.SearchRequest == null || string.IsNullOrEmpty (SearchEngine.SearchRequest.SearchPattern))
				return null;

			int startOffset = Caret.Offset;
			if (IsSomethingSelected && IsMatchAt (startOffset)) {
				startOffset = MainSelection.GetLeadOffset (this);
			}
			
			SearchResult result = SearchForward (startOffset);
			if (result != null) {
				Caret.Offset = result.Offset + result.Length;
				if (setSelection)
					MainSelection = new MonoDevelop.Ide.Editor.Selection (Document.OffsetToLocation (result.Offset), Caret.Location);
			}
			return result;
		}
		
		public SearchResult FindPrevious (bool setSelection)
		{
			if (SearchEngine.SearchRequest == null || string.IsNullOrEmpty (SearchEngine.SearchRequest.SearchPattern))
				return null;
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
					MainSelection = new MonoDevelop.Ide.Editor.Selection (Document.OffsetToLocation (result.Offset), Caret.Location);
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
			return searchEngine.ReplaceAll (withPattern);
		}
		#endregion
		
		#region VirtualSpace Manager
		IndentationTracker indentationTracker = null;
		public bool HasIndentationTracker {
			get {
				return indentationTracker != null;	
			}	
		}

		public IndentationTracker IndentationTracker {
			get {
				if (!HasIndentationTracker)
					return null;
				return indentationTracker;
			}
			set {
				DisposeIndentationTracker();
				if (value != null)
					indentationTracker = new CachedIndentationTracker (this, value);
			}
		}

		void DisposeIndentationTracker()
		{
			var disposableIndentationTracker = indentationTracker as IDisposable;
			if (disposableIndentationTracker != null)
				disposableIndentationTracker.Dispose();
			indentationTracker = null;
		}

		sealed class CachedIndentationTracker : IndentationTracker, IDisposable
		{
			const int maximumCachedLines = 100;
			readonly TextDocument textDocument;
			readonly IndentationTracker baseTracker;

			Dictionary<int, string> indentationCache = new Dictionary<int, string>();

			public override IndentationTrackerFeatures SupportedFeatures {
				get {
					return baseTracker.SupportedFeatures;
				}
			}

			public CachedIndentationTracker (TextEditorData textEditorData, IndentationTracker baseTracker)
			{
				this.textDocument = textEditorData.Document;
				this.baseTracker = baseTracker;
				textDocument.TextChanged += Document_TextChanged;
			}

			void Document_TextChanged (object sender, TextChangeEventArgs e)
			{
				indentationCache.Clear ();
			}

			public override string GetIndentationString (int lineNumber)
			{
				string result;
				if (!indentationCache.TryGetValue (lineNumber, out result)) {
					result = baseTracker.GetIndentationString (lineNumber);
					if (indentationCache.Count > maximumCachedLines)
						indentationCache.Clear ();
					indentationCache.Add (lineNumber, result);
				}
				return result;
			}

			public void Dispose()
			{
				textDocument.TextChanged -= Document_TextChanged;
				if (baseTracker is IDisposable)
					((IDisposable)baseTracker).Dispose ();
			}
		}
		
		public string GetIndentationString (DocumentLocation loc)
		{
			return IndentationTracker.GetIndentationString (loc.Line);
		}
		
		public string GetIndentationString (int lineNumber, int column)
		{
			return IndentationTracker.GetIndentationString (lineNumber);
		}
		
		public string GetIndentationString (int offset)
		{
			var lineNumber = OffsetToLineNumber (offset);
			return IndentationTracker.GetIndentationString (lineNumber);
		}
		
		public int GetVirtualIndentationColumn (DocumentLocation loc)
		{
			return 1 + CountIndent (GetIndentationString (loc));
		}
		
		public int GetVirtualIndentationColumn (int lineNumber, int column)
		{
			return 1 + CountIndent (GetIndentationString (lineNumber, column));
		}
		
		public int GetVirtualIndentationColumn (int offset)
		{
			return 1 + CountIndent (GetIndentationString (offset));
		}

		static int CountIndent (string str)
		{
			// '\t' == 1 - virtual indent is here the character indent not the visual one.
			return str.Length;
		}

		/// <summary>
		/// Ensures the caret is not in a virtual position by adding whitespaces up to caret position.
		/// That method should always be called in an undo group.
		/// </summary>
		public int EnsureCaretIsNotVirtual ()
		{
			return EnsureIsNotVirtual (Caret.Location);
		}

		public bool IsCaretInVirtualLocation {
			get {
				DocumentLine documentLine = Document.GetLine (Caret.Line);
				if (documentLine == null)
					return true;
				return Caret.Column > documentLine.Length + 1;
			}
		}

		int EnsureIsNotVirtual (DocumentLocation loc)
		{
			return EnsureIsNotVirtual (loc.Line, loc.Column);
		}

		int EnsureIsNotVirtual (int line, int column)
		{
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

		#region Text Paste
		/// <summary>
		/// Gets or sets the text paste handler.
		/// </summary>
		public TextPasteHandler TextPasteHandler {
			get;
			set;
		}

		public int PasteText (int offset, string text, byte[] copyData, ref IDisposable undoGroup)
		{
			if (TextPasteHandler != null) {
				string newText;
				try {
					newText = TextPasteHandler.FormatPlainText (offset, text, copyData);
				} catch (Exception e) {
					Console.WriteLine ("Text paste handler exception:" + e);
					newText = text;
				}
				if (newText != text) {
					var inserted = Insert (offset, text);
					if (options.GenerateFormattingUndoStep) {
						undoGroup.Dispose ();
						undoGroup = OpenUndoGroup ();
					}
					var result = Replace (offset, inserted, newText);
					TextPasteHandler.PostFomatPastedText (offset, result);
					return result;
				}
			}
			var insertedChars = Insert (offset, text);
			if (options.GenerateFormattingUndoStep) {
				undoGroup.Dispose ();
				undoGroup = OpenUndoGroup ();
			}
			if (TextPasteHandler != null) {
				TextPasteHandler.PostFomatPastedText (offset, insertedChars);
			}
			return insertedChars;
		}
		#endregion

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

		public string GetTextAt (ISegment segment)
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
				return Document != null ? Document.LineCount : 0;
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

		public IDisposable OpenUndoGroup(OperationType operationType)
		{
			return Document.OpenUndoGroup (operationType);
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
			int start = e.FoldSegment.GetStartLine (Document).LineNumber;
			int end = e.FoldSegment.GetEndLine (Document).LineNumber;
			
			if (e.FoldSegment.IsCollapsed) {
				if (e.FoldSegment.Marker != null)
					HeightTree.Unfold (e.FoldSegment.Marker, start, end - start);
				e.FoldSegment.Marker = HeightTree.Fold (start, end - start);
			} else {
				HeightTree.Unfold (e.FoldSegment.Marker, start, end - start);
				e.FoldSegment.Marker = null;
			}
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
