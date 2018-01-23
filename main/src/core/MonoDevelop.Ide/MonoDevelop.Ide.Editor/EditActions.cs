//
// EditActions.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Util;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// This class contains some common actions for the text editor.
	/// </summary>
	public static class EditActions
	{
		public static void MoveCaretDown (TextEditor editor)
		{
			editor.EditorOperations.MoveLineDown (false);
		}

		public static void MoveCaretUp (TextEditor editor)
		{
			editor.EditorOperations.MoveLineUp (false);
		}

		public static void MoveCaretRight (TextEditor editor)
		{
			editor.EditorOperations.MoveToPreviousCharacter (false);
		}

		public static void MoveCaretLeft (TextEditor editor)
		{
			editor.EditorOperations.MoveToNextCharacter (false);
		}

		public static void MoveCaretToLineEnd (TextEditor editor)
		{
			editor.EditorOperations.MoveToEndOfLine (false);
		}

		public static void MoveCaretToLineStart (TextEditor editor)
		{
			editor.EditorOperations.MoveToStartOfLine (false);
		}

		public static void MoveCaretToDocumentStart (TextEditor editor)
		{
			editor.EditorOperations.MoveToStartOfDocument (false);
		}

		public static void MoveCaretToDocumentEnd (TextEditor editor)
		{
			editor.EditorOperations.MoveToEndOfDocument (false);
		}

		public static void Backspace (TextEditor editor)
		{
			editor.EditorOperations.Backspace ();
		}

		public static void Delete (TextEditor editor)
		{
			editor.EditorOperations.Delete ();
		}

		public static void ClipboardCopy (TextEditor editor)
		{
			editor.EditorOperations.CopySelection ();
		}

		public static void ClipboardCut (TextEditor editor)
		{
			editor.EditorOperations.CutSelection ();
		}

		public static void ClipboardPaste (TextEditor editor)
		{
			editor.EditorOperations.Paste ();
		}



		public static void SelectAll (TextEditor editor)
		{
			editor.EditorOperations.SelectAll ();
		}

		public static void NewLine (TextEditor editor)
		{
			editor.EditorOperations.InsertNewLine ();
		}

		public static void PageUp (TextEditor textEditor)
		{
			textEditor.EditorOperations.PageUp (false);
		}

		public static void PageDown (TextEditor textEditor)
		{
			textEditor.EditorOperations.PageDown (false);
		}

		public static void Undo (TextEditor editor)
		{
			((IMonoDevelopEditorOperations)editor.EditorOperations).Undo ();
		}

		public static void Redo (TextEditor editor)
		{
			((IMonoDevelopEditorOperations)editor.EditorOperations).Redo ();
		}

		public static void DeleteCurrentLine (TextEditor textEditor)
		{
			textEditor.EditorOperations.DeleteFullLine ();
		}

		public static void DeleteCurrentLineToEnd (TextEditor textEditor)
		{
			textEditor.EditorOperations.DeleteToEndOfLine ();
		}

		public static void ScrollLineUp (TextEditor textEditor)
		{
			textEditor.EditorOperations.ScrollLineTop ();
		}

		public static void ScrollLineDown (TextEditor textEditor)
		{
			textEditor.EditorOperations.ScrollLineBottom ();
		}

		public static void ScrollPageUp (TextEditor textEditor)
		{
			textEditor.EditorOperations.ScrollPageUp ();
		}

		public static void ScrollPageDown (TextEditor textEditor)
		{
			textEditor.EditorOperations.ScrollPageDown ();
		}

		public static void GotoMatchingBrace (TextEditor textEditor)
		{
			var offset = SimpleBracketMatcher.GetMatchingBracketOffset (textEditor, textEditor.CaretOffset);
			if (offset > 0)
				textEditor.CaretOffset = offset;
		}

		public static void MovePrevWord (TextEditor textEditor)
		{
			textEditor.EditorOperations.MoveToPreviousWord (false);
		}

		public static void MoveNextWord (TextEditor textEditor)
		{
			textEditor.EditorOperations.MoveToNextWord (false);
		}

		public static void MovePrevSubWord (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).MoveToPrevSubWord ();
		}

		public static void MoveNextSubWord (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).MoveToNextSubWord ();
		}

		public static void ShowQuickInfo (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).ShowQuickInfo ();
		}      


		public static void TransposeCharacters (TextEditor textEditor)
		{
			// Code from Mono.TextEditor.MiscActions.TransposeCharacters
			if (textEditor.CaretOffset == 0)
				return;
			var line = textEditor.GetLine (textEditor.CaretLine);
			if (line == null)
				return;
			using (var undoGroup = textEditor.OpenUndoGroup ()) {
				int transposeOffset = textEditor.CaretOffset - 1;
				char ch;
				if (textEditor.CaretColumn == 0) {
					var lineAbove = textEditor.GetLine (textEditor.CaretLine - 1);
					if (lineAbove.Length == 0 && line.Length == 0)
						return;

					if (line.Length != 0) {
						ch = textEditor.GetCharAt (textEditor.CaretOffset);
						textEditor.RemoveText (textEditor.CaretOffset, 1);
						textEditor.InsertText (lineAbove.Offset + lineAbove.Length, ch.ToString ());
						return;
					}

					int lastCharOffset = lineAbove.Offset + lineAbove.Length - 1;
					ch = textEditor.GetCharAt (lastCharOffset);
					textEditor.RemoveText (lastCharOffset, 1);
					textEditor.InsertAtCaret (ch.ToString ());
					return;
				}

				int offset = textEditor.CaretOffset;
				if (textEditor.CaretColumn >= line.Length + 1) {
					offset = line.Offset + line.Length - 1;
					transposeOffset = offset - 1;
					// case one char in line:
					if (transposeOffset < line.Offset) {
						var lineAbove = textEditor.GetLine (textEditor.CaretLine - 1);
						transposeOffset = lineAbove.Offset + lineAbove.Length;
						ch = textEditor.GetCharAt (offset);
						textEditor.RemoveText (offset, 1);
						textEditor.InsertText (transposeOffset, ch.ToString ());
						textEditor.CaretOffset = line.Offset;
						return;
					}
				}

				ch = textEditor.GetCharAt (offset);
				textEditor.ReplaceText (offset, 1, textEditor.GetCharAt (transposeOffset).ToString ());
				textEditor.ReplaceText (transposeOffset, 1, ch.ToString ());
				if (textEditor.CaretColumn < line.Length + 1)
					textEditor.CaretOffset = offset + 1;
			}
		}

		public static void DuplicateCurrentLine (TextEditor textEditor)
		{
			// Code from Mono.TextEditor.MiscActions.DuplicateLine
			using (var undoGroup = textEditor.OpenUndoGroup ()) {
				if (textEditor.IsSomethingSelected) {
					var selectedText = textEditor.SelectedText;
					textEditor.ClearSelection ();
					textEditor.InsertAtCaret (selectedText);
				} else {
					var line = textEditor.GetLine (textEditor.CaretLine);
					if (line == null)
						return;
					textEditor.InsertText (line.Offset, textEditor.GetTextAt (line.SegmentIncludingDelimiter));
				}
			}
		}

		public static void JoinLines (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).JoinLines ();
		}

		public static void RecenterEditor (TextEditor textEditor)
		{
			textEditor.EditorOperations.ScrollLineCenter ();
		}

		public static void StartCaretPulseAnimation (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).StartCaretPulseAnimation ();
		}

		public static void DeleteNextSubword (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).DeleteNextSubword ();
		}

		public static void DeletePreviousSubword (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).DeletePreviousSubword ();
		}

		public static void DeleteNextWord (TextEditor textEditor)
		{
			textEditor.EditorOperations.DeleteWordToRight ();
		}

		public static void DeletePreviousWord (TextEditor textEditor)
		{
			textEditor.EditorOperations.DeleteWordToLeft ();
		}

		public static void InsertNewLinePreserveCaretPosition (TextEditor textEditor)
		{
			if (textEditor.IsReadOnly)
				return;
			using (var undoGroup = textEditor.OpenUndoGroup ()) {
				var loc = textEditor.CaretLocation;
				InsertNewLine (textEditor);
				textEditor.CaretLocation = loc;
			}
		}

		public static void InsertNewLineAtEnd (TextEditor textEditor)
		{
			if (textEditor.IsReadOnly)
				return;
			using (var undoGroup = textEditor.OpenUndoGroup ()) {
				MoveCaretToLineEnd (textEditor);
				InsertNewLine (textEditor);
			}
		}

		public static void InsertNewLine (TextEditor textEditor)
		{
			textEditor.EditorOperations.InsertNewLine ();
		}

		public static void RemoveTab (TextEditor textEditor)
		{
			textEditor.EditorOperations.Untabify ();
		}

		public static void InsertTab (TextEditor textEditor)
		{
			textEditor.EditorOperations.Tabify ();
		}

		public static void SwitchCaretMode (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).SwitchCaretMode ();
		}

		public static void MoveBlockUp (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).MoveBlockUp ();
		}

		public static void MoveBlockDown (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).MoveBlockDown ();
		}

		public static void ToggleBlockSelectionMode (TextEditor textEditor)
		{
			((IMonoDevelopEditorOperations)textEditor.EditorOperations).ToggleBlockSelectionMode ();
		}

		public static void IndentSelection (TextEditor editor)
		{
			editor.EditorOperations.Indent ();
		}

		public static void UnIndentSelection (TextEditor editor)
		{
			editor.EditorOperations.Unindent ();
		}

		#region SelectionActions

		static void RunSelectionAction (TextEditor textEditor, Action<TextEditor> action)
		{
			using (var undo = textEditor.OpenUndoGroup ()) {
				var anchor = textEditor.IsSomethingSelected ? textEditor.SelectionAnchorOffset : textEditor.CaretOffset;
				action (textEditor);
				textEditor.SetSelection (anchor, textEditor.CaretOffset);
			}
		}

		public static void SelectionMoveToDocumentEnd (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveCaretToDocumentEnd);
		}

		public static void ExpandSelectionToLine (TextEditor textEditor)
		{
			// from MonoDevelop.Ide.Editor.SelectionActions.ExpandSelectionToLine
			using (var undoGroup = textEditor.OpenUndoGroup ()) {
				var curLineSegment = textEditor.GetLine (textEditor.CaretLine).SegmentIncludingDelimiter;
				var range = textEditor.SelectionRange;
				var selection = TextSegment.FromBounds (
					System.Math.Min (range.Offset, curLineSegment.Offset),
					System.Math.Max (range.EndOffset, curLineSegment.EndOffset));
				textEditor.CaretOffset = selection.EndOffset;
				textEditor.SelectionRange = selection;
			}
		}

		public static void SelectionMoveToDocumentStart (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveCaretToDocumentStart);
		}

		public static void SelectionMoveLineEnd (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveCaretToLineEnd);
		}

		public static void SelectionMoveLineStart (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveCaretToLineStart);
		}

		public static void SelectionMoveDown (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveCaretDown);
		}

		public static void SelectionMoveUp (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveCaretUp);
		}

		public static void SelectionPageUp (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, PageUp);
		}

		public static void SelectionPageDown (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, PageDown);
		}

		public static void SelectionMovePrevSubWord (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MovePrevSubWord);
		}

		public static void SelectionMoveNextSubWord (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveNextSubWord);
		}

		public static void SelectionMoveLeft (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveCaretLeft);
		}

		public static void SelectionMoveRight (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveCaretRight);
		}

		public static void SelectionMovePrevWord (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MovePrevWord);
		}

		public static void SelectionMoveNextWord (TextEditor textEditor)
		{
			RunSelectionAction (textEditor, MoveNextWord);
		}

		public static void SortSelectedLines (TextEditor textEditor)
		{
			var selectionRegion = textEditor.SelectionRegion;
			var start = selectionRegion.Begin;
			var end = selectionRegion.End;
			var caret = textEditor.CaretLocation;

			int startLine = start.Line;
			int endLine = end.Line;
			if (startLine == endLine)
				return;

			int length = 0;
			var lines = new string[endLine - startLine + 1];
			for (int i = startLine; i <= endLine; i++) {
				//get lines *with* line endings
				var lineText = textEditor.GetLineText (i, true);
				lines [i - startLine] = lineText;
				length += lineText.Length;
			}

			var linesUnsorted = new string[lines.Length];

			Array.Sort (lines, StringComparer.Ordinal);

			bool changed = false;
			for (int i = 0; i <= lines.Length; i++) {
				//can't simply use reference comparison as Array.Sort is not stable
				if (string.Equals (lines [i], linesUnsorted [i], StringComparison.Ordinal)) {
					continue;
				}
				changed = true;
				break;
			}
			if (!changed)
				return;


			var sb = new StringBuilder ();
			for (int i = 0; i < lines.Length; i++) {
				sb.Append (lines [i]);
			}

			var startOffset = textEditor.LocationToOffset (startLine, 1);
			textEditor.ReplaceText (startOffset, length, sb.ToString ());

			textEditor.CaretLocation = LimitColumn (textEditor, caret);
			textEditor.SetSelection (LimitColumn (textEditor, start), LimitColumn (textEditor, end));
		}

		static DocumentLocation LimitColumn (TextEditor data, DocumentLocation loc)
		{
			return new DocumentLocation (loc.Line, System.Math.Min (loc.Column, data.GetLine (loc.Line).Length + 1));
		}
		#endregion
	}
}
