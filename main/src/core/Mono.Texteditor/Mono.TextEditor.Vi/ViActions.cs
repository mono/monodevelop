// 
// ViActions.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;

namespace Mono.TextEditor.Vi
{
	
	
	public static class ViActions
	{
		public static void MoveToNextEmptyLine (TextEditorData data)
		{
			if (data.Caret.Line == data.Document.LineCount) {
				data.Caret.Offset = data.Document.Length;
				return;
			}
			
			int line = data.Caret.Line + 1;
			LineSegment currentLine = data.Document.GetLine (line);
			while (line <= data.Document.LineCount) {
				line++;
				LineSegment nextLine = data.Document.GetLine (line);
				if (currentLine.EditableLength != 0 && nextLine.EditableLength == 0) {
					data.Caret.Offset = nextLine.Offset;
					return;
				}
				currentLine = nextLine;
			}
			
			data.Caret.Offset = currentLine.Offset;
		}
		
		public static void MoveToPreviousEmptyLine (TextEditorData data)
		{
			if (data.Caret.Line == DocumentLocation.MinLine) {
				data.Caret.Offset = 0;
				return;
			}
			
			int line = data.Caret.Line - 1;
			LineSegment currentLine = data.Document.GetLine (line);
			while (line > DocumentLocation.MinLine) {
				line--;
				LineSegment previousLine = data.Document.GetLine (line);
				if (currentLine.EditableLength != 0 && previousLine.EditableLength == 0) {
					data.Caret.Offset = previousLine.Offset;
					return;
				}
				currentLine = previousLine;
			}
			
			data.Caret.Offset = currentLine.Offset;
		}
		
		public static void NewLineBelow (TextEditorData data)
		{
			LineSegment currentLine = data.Document.GetLine (data.Caret.Line);
			data.Caret.Offset = currentLine.Offset + currentLine.EditableLength;
			MiscActions.InsertNewLine (data);
		}
		
		public static void NewLineAbove (TextEditorData data)
		{
			if (data.Caret.Line == DocumentLocation.MinLine ) {
				data.Caret.Offset = 0;
				MiscActions.InsertNewLine (data);
				data.Caret.Offset = 0;
				return;
			}
			
			LineSegment currentLine = data.Document.GetLine (data.Caret.Line - 1);
			data.Caret.Offset = currentLine.Offset + currentLine.EditableLength;
			MiscActions.InsertNewLine (data);
		}
		
		public static void Join (TextEditorData data)
		{
			int startLine, endLine, startOffset, length, lastSpaceOffset;
			
			if (data.IsSomethingSelected) {
				startLine = data.Document.OffsetToLineNumber (data.SelectionRange.Offset);
				endLine = data.Document.OffsetToLineNumber (data.SelectionRange.EndOffset - 1);
			} else {
				startLine = endLine = data.Caret.Line;
			}
			
			//single-line joins
			if (endLine == startLine)
				endLine++;
			
			if (endLine > data.Document.LineCount)
				return;
			
			LineSegment seg = data.Document.GetLine (startLine);
			startOffset = seg.Offset;
			StringBuilder sb = new StringBuilder (data.Document.GetTextAt (seg).TrimEnd ());
			lastSpaceOffset = startOffset + sb.Length;
			
			for (int i = startLine + 1; i <= endLine; i++) {
				seg = data.Document.GetLine (i);
				lastSpaceOffset = startOffset + sb.Length;
				sb.Append (" ");
				sb.Append (data.Document.GetTextAt (seg).Trim ());
			}
			length = (seg.Offset - startOffset) + seg.EditableLength;
			// TODO: handle conversion issues ? 
			data.Replace (startOffset, length, sb.ToString ());
			data.Caret.Offset = lastSpaceOffset;
		}
		
		public static void ToggleCase (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				if (!data.CanEditSelection)
					return;
				
				StringBuilder sb = new StringBuilder (data.SelectedText);
				for (int i = 0; i < sb.Length; i++) {
					char ch = sb[i];
					if (Char.IsLower (ch))
						sb[i] = Char.ToUpper (ch);
					else if (Char.IsUpper (ch))
						sb[i] = Char.ToLower (ch);
				}
				data.Replace (data.SelectionRange.Offset, data.SelectionRange.Length, sb.ToString ());
			}
			else if (data.CanEdit (data.Caret.Line)) {
				char ch = data.Document.GetCharAt (data.Caret.Offset);
				if (Char.IsLower (ch))
					ch = Char.ToUpper (ch);
				else if (Char.IsUpper (ch))
					ch = Char.ToLower (ch);
				int length = data.Replace (data.Caret.Offset, 1, new string (ch, 1));
				LineSegment seg = data.Document.GetLine (data.Caret.Line);
				if (data.Caret.Column < seg.EditableLength)
					data.Caret.Offset += length;
			}
		}
		
		public static void Right (TextEditorData data)
		{
			LineSegment segment = data.Document.GetLine (data.Caret.Line);
			if (segment.EndOffset-1 > data.Caret.Offset) {
				CaretMoveActions.Right (data);
				RetreatFromLineEnd (data);
			}
		}
		
		public static void Left (TextEditorData data)
		{
			if (DocumentLocation.MinColumn < data.Caret.Column) {
				CaretMoveActions.Left (data);
			}
		}
		
		public static void Down (TextEditorData data)
		{
			int desiredColumn = System.Math.Max (data.Caret.Column, data.Caret.DesiredColumn);
			
			CaretMoveActions.Down (data);
			RetreatFromLineEnd (data);
			
			data.Caret.DesiredColumn = desiredColumn;
		}
		
		public static void Up (TextEditorData data)
		{
			int desiredColumn = System.Math.Max (data.Caret.Column, data.Caret.DesiredColumn);
			
			CaretMoveActions.Up (data);
			RetreatFromLineEnd (data);
			
			data.Caret.DesiredColumn = desiredColumn;
		}
		
		public static void WordEnd (TextEditorData data)
		{
			data.Caret.Offset = data.FindCurrentWordEnd (data.Caret.Offset);
		}
		
		public static void WordStart (TextEditorData data)
		{
			data.Caret.Offset = data.FindCurrentWordStart (data.Caret.Offset);
		}
		
		public static void LineEnd (TextEditorData data)
		{
			int desiredColumn = System.Math.Max (data.Caret.Column, data.Caret.DesiredColumn);
			
			CaretMoveActions.LineEnd (data);
			RetreatFromLineEnd (data);
			
			data.Caret.DesiredColumn = desiredColumn;
		}
		
		internal static bool IsEol (char c)
		{
			return (c == '\r' || c == '\n');
		}
		
		internal static void RetreatFromLineEnd (TextEditorData data)
		{
			if (data.Caret.Mode == CaretMode.Block && !data.IsSomethingSelected && !data.Caret.PreserveSelection) {
				while (DocumentLocation.MinColumn < data.Caret.Column && (data.Caret.Offset >= data.Document.Length
				                                 || IsEol (data.Document.GetCharAt (data.Caret.Offset)))) {
					Left (data);
				}
			}
		}
		
		public static Action<TextEditorData> VisualSelectionFromMoveAction (Action<TextEditorData> moveAction)
		{
			return delegate (TextEditorData data) {
				//get info about the old selection state
				DocumentLocation oldCaret = data.Caret.Location, oldAnchor = oldCaret, oldLead = oldCaret;
				if (data.MainSelection != null) {
					oldLead = data.MainSelection.Lead;
					oldAnchor = data.MainSelection.Anchor;
				}
				
				//do the action, preserving selection
				SelectionActions.StartSelection (data);
				moveAction (data);
				SelectionActions.EndSelection (data);
				
				DocumentLocation newCaret = data.Caret.Location, newAnchor = newCaret, newLead = newCaret;
				if (data.MainSelection != null) {
					newLead = data.MainSelection.Lead;
					newAnchor = data.MainSelection.Anchor;
				}
				
				//Console.WriteLine ("oc{0}:{1} oa{2}:{3} ol{4}:{5}", oldCaret.Line, oldCaret.Column, oldAnchor.Line, oldAnchor.Column, oldLead.Line, oldLead.Column);
				//Console.WriteLine ("nc{0}:{1} na{2}:{3} nl{4}:{5}", newCaret.Line, newCaret.Line, newAnchor.Line, newAnchor.Column, newLead.Line, newLead.Column);
				
				//pivot the anchor around the anchor character
				if (oldAnchor < oldLead && newAnchor >= newLead) {
					data.SetSelection (new DocumentLocation (newAnchor.Line, newAnchor.Column + 1), newLead);
				} else if (oldAnchor > oldLead && newAnchor <= newLead) {
					data.SetSelection (new DocumentLocation (newAnchor.Line, newAnchor.Column - 1), newLead);
				}
				
				//pivot the lead about the anchor character
				if (newAnchor == newLead) {
					if (oldAnchor < oldLead)
						SelectionActions.FromMoveAction (Left) (data);
					else
						SelectionActions.FromMoveAction (Right) (data);
				}
				//pivot around the anchor line
				else {
					if (oldAnchor < oldLead && newAnchor > newLead && (
							(newLead.Line == newAnchor.Line && oldLead.Line == oldAnchor.Line + 1) ||
						    (newLead.Line == newAnchor.Line - 1 && oldLead.Line == oldAnchor.Line)))
						SelectionActions.FromMoveAction (Left) (data);
					else if (oldAnchor > oldLead && newAnchor < newLead && (
							(newLead.Line == newAnchor.Line && oldLead.Line == oldAnchor.Line - 1) ||
							(newLead.Line == newAnchor.Line + 1 && oldLead.Line == oldAnchor.Line)))
						SelectionActions.FromMoveAction (Right) (data);
				}
			};
		}
	}
}
