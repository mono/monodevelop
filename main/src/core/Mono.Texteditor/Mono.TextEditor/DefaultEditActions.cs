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

using Gtk;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public static class ScrollActions
	{
		public static void Up (TextEditorData data)
		{
			data.VAdjustment.Value = System.Math.Max (data.VAdjustment.Lower, 
			                                          data.VAdjustment.Value - data.VAdjustment.StepIncrement); 
		}
		
		public static void Down (TextEditorData data)
		{
			data.VAdjustment.Value = System.Math.Min (data.VAdjustment.Upper - data.VAdjustment.PageSize, 
			                                          data.VAdjustment.Value + data.VAdjustment.StepIncrement); 
		}
		
		public static void PageUp (TextEditorData data)
		{
			data.VAdjustment.Value = System.Math.Max (data.VAdjustment.Lower, 
			                                          data.VAdjustment.Value - data.VAdjustment.PageSize); 
		}
		
		public static void PageDown (TextEditorData data)
		{
			data.VAdjustment.Value = System.Math.Min (data.VAdjustment.Upper - data.VAdjustment.PageSize, 
			                                          data.VAdjustment.Value + data.VAdjustment.PageSize); 
		}
	}
	
	public static class MiscActions
	{
		public static void GotoMatchingBracket (TextEditorData data)
		{
			int matchingBracketOffset = data.Document.GetMatchingBracketOffset (data.Caret.Offset);
			if (matchingBracketOffset == -1 && data.Caret.Offset > 0)
				matchingBracketOffset = data.Document.GetMatchingBracketOffset (data.Caret.Offset - 1);
			
			if (matchingBracketOffset != -1)
				data.Caret.Offset = matchingBracketOffset;
		}
		
		public static int RemoveTabInLine (TextEditorData data, LineSegment line)
		{
			if (line.Length == 0)
				return 0;
			char ch = data.Document.GetCharAt (line.Offset); 
			if (ch == '\t') {
				data.Remove (line.Offset, 1);
				return 1;
			} else if (ch == ' ') {
				int removeCount = 0;
				for (int i = 0; i < data.Options.IndentationSize;) {
					ch = data.Document.GetCharAt (line.Offset + i);
					if (ch == ' ') {
						removeCount ++;
						i++;
					} else  if (ch == '\t') {
						removeCount ++;
						i += data.Options.TabSize;
					} else {
						break;
					}
				}
				data.Remove (line.Offset, removeCount);
				return removeCount;
			}
			return 0;
		}
		
		public static void RemoveIndentSelection (TextEditorData data)
		{
			Debug.Assert (data.IsSomethingSelected);
			int startLineNr = data.IsSomethingSelected ? data.MainSelection.MinLine : data.Caret.Line;
			int endLineNr   = data.IsSomethingSelected ? data.MainSelection.MaxLine : data.Caret.Line;
			
			if (endLineNr < 0)
				endLineNr = data.Document.LineCount;
			LineSegment anchorLine   = data.IsSomethingSelected ? data.Document.GetLine (data.MainSelection.Anchor.Line) : null;
			int         anchorColumn = data.IsSomethingSelected ? data.MainSelection.Anchor.Column : -1;
			
			data.Document.BeginAtomicUndo ();
			int first = -1;
			int last  = 0;
			foreach (LineSegment line in data.SelectedLines) {
				last = RemoveTabInLine (data, line);
				if (first < 0)
					first = last;
			}
			
			if (data.IsSomethingSelected) 
				SelectLineBlock (data, endLineNr, startLineNr);
			
			if (data.Caret.Column != 0) {
				data.Caret.PreserveSelection = true;
				data.Caret.Column = System.Math.Max (0, data.Caret.Column - last);
				data.Caret.PreserveSelection = false;
			}
			
			data.Document.EndAtomicUndo ();
			data.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			data.Document.CommitDocumentUpdate ();
		}
		
		static void SelectLineBlock (TextEditorData data, int endLineNr, int startLineNr)
		{
			LineSegment endLine = data.Document.GetLine (endLineNr);
			data.MainSelection = new Selection (startLineNr, 0, endLineNr, endLine.Length);
		}
		
		public static void RemoveTab (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			if (data.IsMultiLineSelection) {
				RemoveIndentSelection (data);
				return;
			} else {
				LineSegment line = data.Document.GetLine (data.Caret.Line);
				int visibleColumn = 0;
				for (int i = 0; i < data.Caret.Column; ++i)
					visibleColumn += data.Document.GetCharAt (line.Offset + i) == '\t' ? data.Options.TabSize : 1;
				
				int newColumn = ((visibleColumn / data.Options.IndentationSize) - 1) * data.Options.IndentationSize;
				
				visibleColumn = 0;
				for (int i = 0; i < data.Caret.Column; ++i) {
					visibleColumn += data.Document.GetCharAt (line.Offset + i) == '\t' ? data.Options.TabSize : 1;
					if (visibleColumn >= newColumn) {
						data.Caret.Column = i;
						break;
					}
				}
			}
		}
		
		public static void IndentSelection (TextEditorData data)
		{
			int startLineNr = data.IsSomethingSelected ? data.MainSelection.MinLine : data.Caret.Line;
			int endLineNr   = data.IsSomethingSelected ? data.MainSelection.MaxLine : data.Caret.Line;
			if (endLineNr < 0)
				endLineNr = data.Document.LineCount;
			LineSegment anchorLine   = data.IsSomethingSelected ? data.Document.GetLine (data.MainSelection.Anchor.Line) : null;
			int         anchorColumn = data.IsSomethingSelected ? data.MainSelection.Anchor.Column : -1;
			data.Document.BeginAtomicUndo ();
			foreach (LineSegment line in data.SelectedLines) {
				data.Insert (line.Offset, data.Options.IndentationString);
			}
			if (data.IsSomethingSelected) 
				SelectLineBlock (data, endLineNr, startLineNr);
			
			if (data.Caret.Column != 0) {
				data.Caret.PreserveSelection = true;
				data.Caret.Column++;
				data.Caret.PreserveSelection = false;
			}
			
			data.Document.EndAtomicUndo ();
			data.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			data.Document.CommitDocumentUpdate ();
		}
		
		public static void InsertTab (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			
			if (data.IsMultiLineSelection) {
				IndentSelection (data);
				return;
			}
			data.Document.BeginAtomicUndo ();
			if (data.IsSomethingSelected) {
				data.DeleteSelectedText ();
			}
			string indentationString = "\t";
			bool convertTabToSpaces = data.Options.TabsToSpaces;
			
			if (!convertTabToSpaces && !data.Options.AllowTabsAfterNonTabs) {
				for (int i = 1; i < data.Caret.Column; i++) {
					if (data.Document.GetCharAt (data.Caret.Offset - i) != '\t') {
						convertTabToSpaces = true;
						break;
					}
				}
			}
			
			if (convertTabToSpaces) {
				DocumentLocation visualLocation = data.Document.LogicalToVisualLocation (data, data.Caret.Location);
				int tabWidth = TextViewMargin.GetNextTabstop (data, visualLocation.Column) - visualLocation.Column;
				indentationString = new string (' ', tabWidth);
			}
			int length = data.Insert (data.Caret.Offset, indentationString);
			data.Caret.Column += length;
			data.Document.EndAtomicUndo ();
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
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			data.Caret.Column = line.EditableLength;
			InsertNewLine (data);
		}
		
		public static void InsertNewLine (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			
			data.Document.BeginAtomicUndo ();
			if (data.IsSomethingSelected) {
				data.DeleteSelectedText ();
			}
			
			string newLine = data.EolMarker;
			int caretColumnOffset = 0;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			
			if (data.Options.AutoIndent) {
				int i;
				for (i = 0; i < line.EditableLength; i++) {
					char ch = data.Document.GetCharAt (line.Offset + i);
					if (!Char.IsWhiteSpace (ch))
						break;
				}
				caretColumnOffset = i;
				newLine += data.Document.GetTextBetween (line.Offset, line.Offset + i);
			}
			
			data.Insert (data.Caret.Offset, newLine);
			data.Caret.Location = new DocumentLocation (data.Caret.Line + 1, caretColumnOffset);
			data.Document.EndAtomicUndo ();
		}
		
		public static void SwitchCaretMode (TextEditorData data)
		{
			data.Caret.IsInInsertMode = !data.Caret.IsInInsertMode;
			data.Document.RequestUpdate (new SinglePositionUpdate (data.Caret.Line, data.Caret.Column));
			data.Document.CommitDocumentUpdate ();
		}
		
		public static void Undo (TextEditorData data)
		{
			if (data.Document.ReadOnly)
				return;
			data.Document.Undo ();
		}
		
		public static void Redo (TextEditorData data)
		{
			if (data.Document.ReadOnly)
				return;
			data.Document.Redo ();
		}
		
		public static void MoveBlockUp (TextEditorData data)
		{
			int lineStart = data.Caret.Line;
			int lineEnd = data.Caret.Line;
			bool setSelection = lineStart != lineEnd;
			if (data.IsSomethingSelected) {
				setSelection = true;
				lineStart = data.MainSelection.MinLine;
				lineEnd = data.MainSelection.MaxLine;
			}
			
			if (lineStart <= 0)
				return;
			
			Mono.TextEditor.LineSegment startLine = data.Document.GetLine (lineStart);
			Mono.TextEditor.LineSegment endLine = data.Document.GetLine (lineEnd);
			Mono.TextEditor.LineSegment prevLine = data.Document.GetLine (lineStart - 1);
			int relCaretOffset = data.Caret.Offset - startLine.Offset;
			data.Document.BeginAtomicUndo ();
			
			string text = data.Document.GetTextBetween (startLine.Offset, endLine.EndOffset);
			
			int delta = endLine.EndOffset - startLine.Offset;
			int newStartOffset = prevLine.Offset;
			bool hasEmptyDelimiter = endLine.DelimiterLength == 0;
			
			data.Remove (startLine.Offset, delta);
			
			// handle the last line case
			
			if (hasEmptyDelimiter) {
				text = text + data.EolMarker;
				data.Remove (prevLine.Offset + prevLine.EditableLength, prevLine.DelimiterLength);
			}
			
			data.Insert (newStartOffset, text);
			data.Caret.Offset = newStartOffset + relCaretOffset;
			if (setSelection)
				data.SetSelection (newStartOffset, newStartOffset + delta - endLine.DelimiterLength);
			data.Document.EndAtomicUndo ();
		}
		
		public static void MoveBlockDown (TextEditorData data)
		{
			int lineStart = data.Caret.Line;
			int lineEnd = data.Caret.Line;
			bool setSelection = lineStart != lineEnd;
			if (data.IsSomethingSelected) {
				setSelection = true;
				lineStart = data.MainSelection.MinLine;
				lineEnd = data.MainSelection.MaxLine;
			}
			
			if (lineEnd + 1 >= data.Document.LineCount)
				return;
			
			Mono.TextEditor.LineSegment startLine = data.Document.GetLine (lineStart);
			Mono.TextEditor.LineSegment endLine = data.Document.GetLine (lineEnd);
			Mono.TextEditor.LineSegment nextLine = data.Document.GetLine (lineEnd + 1);
			int relCaretOffset = data.Caret.Offset - startLine.Offset;
			data.Document.BeginAtomicUndo ();
			string text = data.Document.GetTextBetween (startLine.Offset, endLine.EndOffset);
			
			int nextLineOffset = nextLine.EndOffset;
			int delta = endLine.EndOffset - startLine.Offset;
			int newStartOffset = nextLineOffset - delta;
			
			// handle the last line case
			if (nextLine.DelimiterLength == 0) {
				text = data.EolMarker + text.Substring (0, text.Length - endLine.DelimiterLength);
				newStartOffset += data.EolMarker.Length;
			}
			data.Insert (nextLineOffset, text);
			data.Remove (startLine.Offset, delta);
			
			data.Caret.Offset = newStartOffset + relCaretOffset;
			if (setSelection)
				data.SetSelection (newStartOffset, newStartOffset + text.Length - endLine.DelimiterLength);
			data.Document.EndAtomicUndo ();
		}
		
		/// <summary>
		/// Transpose characters (Emacs C-t)
		/// </summary>
		public static void TransposeCharacters (TextEditorData data)
		{
			if (data.Caret.Offset == 0)
				return;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			if (line == null)
				return;
			int transposeOffset = data.Caret.Offset - 1;
			char ch;
			if (data.Caret.Column == 0) {
				LineSegment lineAbove = data.Document.GetLine (data.Caret.Line - 1);
				if (lineAbove.EditableLength == 0 && line.EditableLength == 0) 
					return;
				
				if (line.EditableLength != 0) {
					ch = data.Document.GetCharAt (data.Caret.Offset);
					data.Remove (data.Caret.Offset, 1);
					data.Insert (lineAbove.Offset + lineAbove.EditableLength, ch.ToString ());
					data.Document.CommitLineUpdate (data.Caret.Line - 1);
					return;
				}
				
				int lastCharOffset = lineAbove.Offset + lineAbove.EditableLength - 1;
				ch = data.Document.GetCharAt (lastCharOffset);
				data.Remove (lastCharOffset, 1);
				data.Insert (data.Caret.Offset, ch.ToString ());
				data.Document.CommitLineUpdate (data.Caret.Line - 1);
				data.Caret.Offset++;
				return;
			}
			
			int offset = data.Caret.Offset;
			if (data.Caret.Column >= line.EditableLength) {
				offset = line.Offset + line.EditableLength - 1;
				transposeOffset = offset - 1;
				// case one char in line:
				if (transposeOffset < line.Offset) {
					LineSegment lineAbove = data.Document.GetLine (data.Caret.Line - 1);
					transposeOffset = lineAbove.Offset + lineAbove.EditableLength;
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
			if (data.Caret.Column < line.EditableLength)
				data.Caret.Offset = offset + 1;
		}
		/// <summary>
		/// Emacs c-l recenter editor command.
		/// </summary>
		public static void RecenterEditor (TextEditorData data)
		{
			data.RequestRecenter ();
		}
		
	}
}