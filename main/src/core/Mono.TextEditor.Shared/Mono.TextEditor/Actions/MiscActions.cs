// DefaultEditActions.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using MonoDevelop.Ide.Editor;
using Microsoft.VisualStudio.Text.Implementation;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;

namespace Mono.TextEditor
{
	static class MiscActions
	{
		public static Microsoft.CodeAnalysis.Text.TextChange RemoveTabInLine (TextEditorData data, DocumentLine line)
		{
			if (line.LengthIncludingDelimiter == 0)
				return default (Microsoft.CodeAnalysis.Text.TextChange);
			using (var undoGroup = data.OpenUndoGroup ()) {
				char ch = data.Document.GetCharAt (line.Offset); 
				if (ch == '\t') {			 
					return new Microsoft.CodeAnalysis.Text.TextChange (new TextSpan (line.Offset, 1), "");
				} else if (ch == ' ') {
					int removeCount = 0;
					for (int i = 0; i < data.Options.IndentationSize;) {
						ch = data.Document.GetCharAt (line.Offset + i);
						if (ch == ' ') {
							removeCount++;
							i++;
						} else if (ch == '\t') {
							removeCount++;
							i += data.Options.TabSize;
						} else {
							break;
						}
					}
					return new Microsoft.CodeAnalysis.Text.TextChange (new TextSpan (line.Offset, removeCount), "");
				}
			}
			return default (Microsoft.CodeAnalysis.Text.TextChange);
		}
		
		public static void RemoveIndentSelection (TextEditorData data)
		{
			if (!data.IsSomethingSelected)
				return;
			int startLineNr, endLineNr;
			GetSelectedLines (data, out startLineNr, out endLineNr);
			
			using (var undo = data.OpenUndoGroup (OperationType.Format)) {
				var anchor = data.MainSelection.Anchor;
				var lead = data.MainSelection.Lead;
				bool first = true;
				bool removedFromLast = false;
				int removeLast = 0;
				bool removedFromFirst = false;
				int removeFirst = 0;
				var changes = new List<Microsoft.CodeAnalysis.Text.TextChange> ();
				DocumentLine last = null;
				foreach (var line in data.SelectedLines) {
					var remove = RemoveTabInLine (data, line);
					removedFromLast |= remove.Span.Length > 0;
					removeLast = remove.Span.Length;
					if (first) {
						removedFromFirst = remove.Span.Length > 0;
						removeFirst = remove.Span.Length;
						first = false;
					}
					if (remove.Span.Length > 0)
						changes.Add (remove);
					last = line;
				}
				data.Document.ApplyTextChanges (changes);
				if (last != null)
					data.Document.CommitLineUpdate (last);
				var ac = System.Math.Max (DocumentLocation.MinColumn, anchor.Column - (anchor < lead ? removeFirst : removeLast));
				var lc = System.Math.Max (DocumentLocation.MinColumn, lead.Column - (anchor < lead ? removeLast : removeFirst));
				
				if (anchor < lead) {
					if (!removedFromFirst)
						ac = anchor.Column;
					if (!removedFromLast)
						lc = lead.Column;
				} else {
				if (!removedFromFirst)
						lc = lead.Column;
					if (!removedFromLast)
						ac = anchor.Column;
				}
				data.SetSelection (anchor.Line, ac, lead.Line, lc);
			}
			data.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			data.Document.CommitDocumentUpdate ();
		}

		public static void RemoveTab (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				if (data.CanEditSelection)
					RemoveIndentSelection (data);
				return;
			}
			var line = data.Document.GetLine (data.Caret.Line);
			if (line != null) {
				using (var undo = data.OpenUndoGroup ()) {
					data.EnsureCaretIsNotVirtual ();
					data.Document.ApplyTextChanges (new [] { RemoveTabInLine (data, line) });
					data.FixVirtualIndentation ();
				}
			}
		}

		public static void GetSelectedLines (TextEditorData data, out int startLineNr, out int endLineNr)
		{
			if (data.IsSomethingSelected) {
				DocumentLocation start, end;
				if (data.MainSelection.Anchor < data.MainSelection.Lead) {
					start = data.MainSelection.Anchor;
					end = data.MainSelection.Lead;
				} else {
					start = data.MainSelection.Lead;
					end = data.MainSelection.Anchor;
				}
				startLineNr = start.Line;
				endLineNr = end.Column == DocumentLocation.MinColumn ? end.Line - 1 : end.Line;
			} else {
				startLineNr = endLineNr = data.Caret.Line;
			}
			
			if (endLineNr < DocumentLocation.MinLine)
				endLineNr = data.Document.LineCount;
		}

		public static void IndentSelection (TextEditorData data)
		{
			if (!data.IsSomethingSelected)
				return;
			int startLineNr, endLineNr;
			GetSelectedLines (data, out startLineNr, out endLineNr);
			var anchor = data.MainSelection.Anchor;
			var lead = data.MainSelection.Lead;
			var indentationString = data.Options.IndentationString;
			var changes = new List<Microsoft.CodeAnalysis.Text.TextChange> ();

			foreach (DocumentLine line in data.SelectedLines) {
				if (data.Options.IndentStyle == IndentStyle.Virtual && line.Length == 0)
					continue;
				changes.Add (new Microsoft.CodeAnalysis.Text.TextChange (new TextSpan (line.Offset, 0), indentationString)); 
			}

			using (var undo = data.OpenUndoGroup (OperationType.Format)) {
				data.Document.ApplyTextChanges (changes);
			}
			int chars = indentationString.Length;
			var leadCol = lead.Column > 1 || lead < anchor ? lead.Column + chars : 1;
			var anchorCol = anchor.Column > 1 || anchor < lead ? anchor.Column + chars : 1;
			data.SetSelection (anchor.Line, anchorCol, lead.Line, leadCol);
			data.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			data.Document.CommitDocumentUpdate ();
		}
		
		public static void InsertTab (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			if (data.IsMultiLineSelection && data.MainSelection.SelectionMode != SelectionMode.Block) {
				IndentSelection (data);
				return;
			}
			using (var undo = data.OpenUndoGroup ()) {
				string indentationString = "\t";
				bool convertTabToSpaces = data.TabsToSpaces;
				
				if (!convertTabToSpaces && !data.Options.AllowTabsAfterNonTabs) {
					for (int i = 1; i < data.Caret.Column; i++) {
						if (data.Document.GetCharAt (data.Caret.Offset - i) != '\t') {
							convertTabToSpaces = true;
							break;
						}
					}
				}
					
				if (convertTabToSpaces) {
					DocumentLocation visualLocation = data.LogicalToVisualLocation (data.Caret.Location);
					int tabWidth = TextViewMargin.GetNextTabstop (data, visualLocation.Column) - visualLocation.Column;
					indentationString = new string (' ', tabWidth);
				}
				if (data.IsSomethingSelected)
					data.DeleteSelectedText (data.MainSelection.SelectionMode != SelectionMode.Block);
				data.InsertAtCaret (indentationString);
			}
		}
		
		public static void InsertNewLinePreserveCaretPosition (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			DocumentLocation loc = data.Caret.Location;
			InsertNewLine (data);
			data.Caret.Location = loc;
		}
		
		public static void InsertNewLineAtEnd (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;

			using (var undoGroup = data.OpenUndoGroup ()) {
				DocumentLine line = data.Document.GetLine (data.Caret.Line);
				data.Caret.Column = line.Length + 1;
				InsertNewLine (data);
			}
		}
		
		static void NewLineSmartIndent (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				data.EnsureCaretIsNotVirtual ();

				var oldCaretLine = data.Caret.Location.Line;

				string indentString = data.GetIndentationString (data.Caret.Location);
				data.InsertAtCaret (data.EolMarker);

				// Don't insert the indent string if the EOL insertion modified the caret location in an unexpected fashion
				//  (This likely means someone has custom logic regarding insertion of the EOL)
				if (data.Caret.Location.Line == oldCaretLine + 1 && data.Caret.Location.Column == 1)
					data.InsertAtCaret (indentString);
			}
		}
		
		public static void InsertNewLine (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			
			using (var undo = data.OpenUndoGroup ()) {
				if (data.IsSomethingSelected) {
					var end = data.MainSelection.End;
					data.DeleteSelectedText ();
					if (end.Column == 1) {
						CaretMoveActions.InternalCaretMoveHome (data, true, false);
						return;
					}
				}
				switch (data.Options.IndentStyle) {
				case IndentStyle.None:
					data.InsertAtCaret (data.EolMarker);
					break;
				case IndentStyle.Auto:
					data.EnsureCaretIsNotVirtual ();
					var indent = data.Document.GetLineIndent (data.Caret.Line);
					if (data.Caret.Column >= indent.Length) {
						data.InsertAtCaret (data.EolMarker + indent);
					} else {
						data.InsertAtCaret (data.EolMarker);
					}
					break;
				case IndentStyle.Smart:
					if (!data.HasIndentationTracker)
						goto case IndentStyle.Auto;
					NewLineSmartIndent (data);
					break;
				case IndentStyle.Virtual:
					if (!data.HasIndentationTracker)
						goto case IndentStyle.Auto;
					var oldLine = data.Caret.Line;
					var curLine = data.GetLine (oldLine);
					var indentCol = data.GetVirtualIndentationColumn (data.Caret.Location);
					if (curLine.Length >= data.Caret.Column) {
						NewLineSmartIndent (data);
						data.FixVirtualIndentation ();
						data.FixVirtualIndentation (oldLine);
						break;
					}
					data.Insert (data.Caret.Offset, data.EolMarker);
					data.FixVirtualIndentation (oldLine);
					data.Caret.Column = indentCol;
					break;
				default:
					throw new ArgumentOutOfRangeException ();
				}
			}
		}
		
		public static void SwitchCaretMode (TextEditorData data)
		{
			data.Caret.IsInInsertMode = !data.Caret.IsInInsertMode;
			data.Document.RequestUpdate (new SinglePositionUpdate (data.Caret.Line, data.Caret.Column));
			data.Document.CommitDocumentUpdate ();
		}
		
		public static void Undo (TextEditorData data)
		{
			try {
				if (data.Document.IsReadOnly)
					return;
				if (CancelPreEditMode (data))
					return;
				var offset = data.Document.UndoBeginOffset;
				data.Document.Undo ();
				if (offset >= 0)
					data.ScrollTo (offset);
			} catch (Exception e) {
				LoggingService.LogError ("Error while performing undo.", e);
			}
		}


		public static bool CancelPreEditMode (TextEditorData data)
		{
			var editor = data.Parent;
			if (editor != null && !string.IsNullOrEmpty (editor.preeditString)) {
				editor.ResetIMContext ();
				return true;
			}
			return false;
		}
		
		public static void Redo (TextEditorData data)
		{
			try {
				if (data.Document.IsReadOnly)
					return;
				if (CancelPreEditMode (data))
					return;
				var offset = data.Document.RedoBeginOffset;
				data.Document.Redo ();
				if (offset >= 0)
					data.ScrollTo (offset);
			} catch (Exception e) {
				LoggingService.LogError ("Error while performing redo.", e);
			}
		}

		public static void MoveBlockUp (TextEditorData data)
		{
			int lineStart = data.Caret.Line;
			int lineEnd = data.Caret.Line;
			bool setSelection = lineStart != lineEnd;
			DocumentLocation anchor = DocumentLocation.Empty, lead = DocumentLocation.Empty;
			if (data.IsSomethingSelected) {
				setSelection = true;
				lineStart = data.MainSelection.MinLine;
				lineEnd = data.MainSelection.MaxLine;
				anchor = data.MainSelection.Anchor;
				lead = data.MainSelection.Lead;
			}
			
			if (lineStart <= 0)
				return;
			
			using (var undo = data.OpenUndoGroup ()) {
				//Mono.TextEditor.LineSegment startLine = data.Document.GetLine (lineStart);
				//int relCaretOffset = data.Caret.Offset - startLine.Offset;
				
				Mono.TextEditor.DocumentLine prevLine = data.Document.GetLine (lineStart - 1);
				if (prevLine == null)
					return;
				string text = data.Document.GetTextAt (prevLine.Offset, prevLine.Length);
				List<TextLineMarker> prevLineMarkers = new List<TextLineMarker> (data.Document.GetMarkers (prevLine));
				data.Document.ClearMarkers (prevLine);
				var loc = data.Caret.Location;
				for (int i = lineStart - 1; i <= lineEnd; i++) {
					DocumentLine cur = data.Document.GetLine (i);
					DocumentLine next = data.Document.GetLine (i + 1);
					data.Replace (cur.Offset, cur.Length, i != lineEnd ? data.Document.GetTextAt (next.Offset, next.Length) : text);
					data.Document.ClearMarkers (data.Document.GetLine (i));
					foreach (TextLineMarker marker in (i != lineEnd ? data.Document.GetMarkers (data.Document.GetLine (i + 1)) : prevLineMarkers)) {
						data.Document.AddMarker (data.Document.GetLine (i), marker);
					}
				}
				
				data.Caret.Location = new DocumentLocation (loc.Line - 1, loc.Column);
				if (setSelection)
					data.SetSelection (anchor.Line - 1, anchor.Column, lead.Line - 1, lead.Column);
			}
		}
		
		public static void MoveBlockDown (TextEditorData data)
		{
			int lineStart = data.Caret.Line;
			int lineEnd = data.Caret.Line;
			bool setSelection = lineStart != lineEnd;
			DocumentLocation anchor = DocumentLocation.Empty, lead = DocumentLocation.Empty;
			if (data.IsSomethingSelected) {
				setSelection = true;
				lineStart = data.MainSelection.MinLine;
				lineEnd = data.MainSelection.MaxLine;
				anchor = data.MainSelection.Anchor;
				lead = data.MainSelection.Lead;
			}
			
			if (lineStart <= 0)
				return;
			using (var undo = data.OpenUndoGroup ()) {
				
				//Mono.TextEditor.LineSegment startLine = data.Document.GetLine (lineStart);
				//int relCaretOffset = data.Caret.Offset - startLine.Offset;
				
				Mono.TextEditor.DocumentLine nextLine = data.Document.GetLine (lineEnd + 1);
				if (nextLine == null)
					return;
				string text = data.Document.GetTextAt (nextLine.Offset, nextLine.Length);

				List<TextLineMarker> prevLineMarkers = new List<TextLineMarker> (data.Document.GetMarkers (nextLine));
				data.Document.ClearMarkers (nextLine);
				var loc = data.Caret.Location;
				for (int i = lineEnd + 1; i >= lineStart; i--) {
					DocumentLine cur = data.Document.GetLine (i);
					DocumentLine prev = data.Document.GetLine (i - 1);
					data.Replace (cur.Offset, cur.Length, i != lineStart ? data.Document.GetTextAt (prev.Offset, prev.Length) : text);
					data.Document.ClearMarkers (data.Document.GetLine (i));
					foreach (TextLineMarker marker in (i != lineStart ? data.Document.GetMarkers (data.Document.GetLine (i - 1)) : prevLineMarkers)) {
						data.Document.AddMarker (data.Document.GetLine (i), marker);
					}
				}
				
				data.Caret.Location = new DocumentLocation (loc.Line + 1, loc.Column);
				if (setSelection)
					data.SetSelection (anchor.Line + 1, anchor.Column, lead.Line + 1, lead.Column);
			}
		}
		
		/// <summary>
		/// Transpose characters (Emacs C-t)
		/// </summary>
		public static void TransposeCharacters (TextEditorData data)
		{
			if (data.Caret.Offset == 0)
				return;
			DocumentLine line = data.Document.GetLine (data.Caret.Line);
			if (line == null)
				return;
			using (var undoGroup = data.OpenUndoGroup ()) {
				int transposeOffset = data.Caret.Offset - 1;
				char ch;
				if (data.Caret.Column == 0) {
					DocumentLine lineAbove = data.Document.GetLine (data.Caret.Line - 1);
					if (lineAbove.Length == 0 && line.Length == 0)
						return;
				
					if (line.Length != 0) {
						ch = data.Document.GetCharAt (data.Caret.Offset);
						data.Remove (data.Caret.Offset, 1);
						data.Insert (lineAbove.Offset + lineAbove.Length, ch.ToString ());
						data.Document.CommitLineUpdate (data.Caret.Line - 1);
						return;
					}
				
					int lastCharOffset = lineAbove.Offset + lineAbove.Length - 1;
					ch = data.Document.GetCharAt (lastCharOffset);
					data.Remove (lastCharOffset, 1);
					data.InsertAtCaret (ch.ToString ());
					return;
				}
			
				int offset = data.Caret.Offset;
				if (data.Caret.Column >= line.Length + 1) {
					offset = line.Offset + line.Length - 1;
					transposeOffset = offset - 1;
					// case one char in line:
					if (transposeOffset < line.Offset) {
						DocumentLine lineAbove = data.Document.GetLine (data.Caret.Line - 1);
						transposeOffset = lineAbove.Offset + lineAbove.Length;
						ch = data.Document.GetCharAt (offset);
						data.Remove (offset, 1);
						data.Insert (transposeOffset, ch.ToString ());
						data.Caret.Offset = line.Offset;
						data.Document.CommitLineUpdate (data.Caret.Line - 1);
						return;
					}
				}
			
				ch = data.Document.GetCharAt (offset);
				data.Replace (offset, 1, data.Document.GetCharAt (transposeOffset).ToString ());
				data.Replace (transposeOffset, 1, ch.ToString ());
				if (data.Caret.Column < line.Length + 1)
					data.Caret.Offset = offset + 1;
			}
		}
		/// <summary>
		/// Emacs c-l recenter editor command.
		/// </summary>
		public static void RecenterEditor (TextEditorData data)
		{
			data.RequestRecenter ();
		}

		public static void DuplicateLine (TextEditorData data)
		{
			using (var undoGroup = data.OpenUndoGroup ()) {
				if (data.IsSomethingSelected) {
					var selectedText = data.SelectedText;
					data.ClearSelection ();
					data.InsertAtCaret (selectedText);
				} else {
					DocumentLine line = data.Document.GetLine (data.Caret.Line);
					if (line == null)
						return;
					if (line.DelimiterLength == 0) {
						data.Insert (line.Offset, data.GetTextAt (line.SegmentIncludingDelimiter) + data.EolMarker);
					} else {
						data.Insert (line.Offset, data.GetTextAt (line.SegmentIncludingDelimiter));
					}
				}
			}
		}

	}
}