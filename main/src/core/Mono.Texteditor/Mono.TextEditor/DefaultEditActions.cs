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
			int startLineNr, endLineNr;
			GetSelectedLines (data, out startLineNr, out endLineNr);
			
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
			
			if (data.Caret.Column != DocumentLocation.MinColumn) {
				data.Caret.PreserveSelection = true;
				data.Caret.Column = System.Math.Max (DocumentLocation.MinColumn, data.Caret.Column - last);
				data.Caret.PreserveSelection = false;
			}
			
			data.Document.EndAtomicUndo ();
			data.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			data.Document.CommitDocumentUpdate ();
		}
		
		static void SelectLineBlock (TextEditorData data, int endLineNr, int startLineNr)
		{
			if (startLineNr == endLineNr) {
				data.MainSelection = new Selection (startLineNr, DocumentLocation.MinColumn, startLineNr + 1, DocumentLocation.MinColumn);
				return;
			}
			LineSegment endLine = data.Document.GetLine (endLineNr);
			data.MainSelection = new Selection (startLineNr, DocumentLocation.MinColumn, endLineNr, endLine.Length + 1);
		}
		
		public static void RemoveTab (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
//			if (data.IsMultiLineSelection) {
				RemoveIndentSelection (data);
/*				return;
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
			}*/
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
			int startLineNr, endLineNr;
			GetSelectedLines (data, out startLineNr, out endLineNr);
			
			data.Document.BeginAtomicUndo ();
			foreach (LineSegment line in data.SelectedLines) {
				data.Insert (line.Offset, data.Options.IndentationString);
			}
			if (data.IsSomethingSelected) 
				SelectLineBlock (data, endLineNr, startLineNr);
			
			if (data.Caret.Column != DocumentLocation.MinColumn) {
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
				DocumentLocation visualLocation = data.LogicalToVisualLocation (data.Caret.Location);
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
			data.Caret.Column = line.EditableLength + 1;
			InsertNewLine (data);
		}
		
		public static void InsertNewLine (TextEditorData data)
		{
			if (!data.CanEditSelection)
				return;
			
			data.Document.BeginAtomicUndo ();
			if (data.IsSomethingSelected)
				data.DeleteSelectedText ();
			
			data.EnsureCaretIsNotVirtual ();
			StringBuilder sb = new StringBuilder (data.EolMarker);
			if (data.Options.AutoIndent)
				sb.Append (data.Document.GetLineIndent (data.Caret.Line));
			int offset = data.Caret.Offset;
			data.Insert (offset, sb.ToString ());
			data.Caret.Offset = offset + sb.Length;
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
			if (CancelPreEditMode (data))
				return;
			data.Document.Undo ();
		}
		
		public static bool CancelPreEditMode (TextEditorData data)
		{
			var editor = data.Parent;
			if (editor != null && editor.preeditOffset >= 0) {
				editor.ResetIMContext ();
				return true;
			}
			return false;
		}
		
		public static void Redo (TextEditorData data)
		{
			if (data.Document.ReadOnly)
				return;
			if (CancelPreEditMode (data))
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
			
			data.Document.BeginAtomicUndo ();
			//Mono.TextEditor.LineSegment startLine = data.Document.GetLine (lineStart);
			//int relCaretOffset = data.Caret.Offset - startLine.Offset;
			
			Mono.TextEditor.LineSegment prevLine = data.Document.GetLine (lineStart - 1);
			string text = data.Document.GetTextAt (prevLine.Offset, prevLine.EditableLength);
			List<TextMarker> prevLineMarkers = new List<TextMarker> (prevLine.Markers);
			prevLine.ClearMarker ();
			
			for (int i = lineStart - 1; i <= lineEnd; i++) {
				LineSegment cur = data.Document.GetLine (i);
				LineSegment next = data.Document.GetLine (i + 1);
				data.Replace (cur.Offset, cur.EditableLength, i != lineEnd ? data.Document.GetTextAt (next.Offset, next.EditableLength) : text);
				data.Document.GetLine (i).ClearMarker ();
				foreach (TextMarker marker in (i != lineEnd ? data.Document.GetLine (i + 1).Markers : prevLineMarkers)) {
					data.Document.GetLine (i).AddMarker (marker);
				}
			}
			
			data.Caret.Line--;
			if (setSelection)
				data.SetSelection (data.Document.GetLine (lineStart - 1).Offset, data.Document.GetLine (lineEnd - 1).Offset + data.Document.GetLine (lineEnd - 1).EditableLength);
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
			
			if (lineStart <= 0)
				return;
			data.Document.BeginAtomicUndo ();
			
			//Mono.TextEditor.LineSegment startLine = data.Document.GetLine (lineStart);
			//int relCaretOffset = data.Caret.Offset - startLine.Offset;
			
			Mono.TextEditor.LineSegment nextLine = data.Document.GetLine (lineEnd + 1);
			if (nextLine == null)
				return;
			string text = data.Document.GetTextAt (nextLine.Offset, nextLine.EditableLength);
			List<TextMarker> prevLineMarkers = new List<TextMarker> (nextLine.Markers);
			nextLine.ClearMarker ();
			
			for (int i = lineEnd + 1; i >= lineStart; i--) {
				LineSegment cur = data.Document.GetLine (i);
				LineSegment prev = data.Document.GetLine (i - 1);
				data.Replace (cur.Offset, cur.EditableLength, i != lineStart ? data.Document.GetTextAt (prev.Offset, prev.EditableLength) : text);
				data.Document.GetLine (i).ClearMarker ();
				foreach (TextMarker marker in (i != lineStart ? data.Document.GetLine (i - 1).Markers : prevLineMarkers)) {
					data.Document.GetLine (i).AddMarker (marker);
				}
			}
			
			data.Caret.Line++;
			if (setSelection)
				data.SetSelection (data.Document.GetLine (lineStart + 1).Offset, data.Document.GetLine (lineEnd + 1).Offset + data.Document.GetLine (lineEnd + 1).EditableLength);
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
			if (data.Caret.Column >= line.EditableLength + 1) {
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
			if (data.Caret.Column < line.EditableLength + 1)
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