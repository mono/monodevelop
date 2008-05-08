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

namespace Mono.TextEditor
{
#region Caret Movement
	public class CaretMoveLeft : EditAction
	{
		public override void Run (TextEditorData data)
		{
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			List<FoldSegment> foldings = data.Document.GetEndFoldings (line);
			FoldSegment segment = null;
			foreach (FoldSegment folding in foldings) {
				if (folding.IsFolded && folding.EndColumn == data.Caret.Column) {
					segment = folding;
					break;
				}
			}
			if (segment != null) {
				data.Caret.Location = data.Document.OffsetToLocation (segment.StartLine.Offset + segment.Column); 
				return;
			}
			
			if (data.Caret.Column > 0) {
				data.Caret.Column--;
			} else if (data.Caret.Line > 0) {
				LineSegment prevLine = data.Document.GetLine (data.Caret.Line - 1);
				data.Caret.Location = new DocumentLocation (data.Caret.Line - 1, prevLine.EditableLength);
			}
		}
	}
	
	public class CaretMovePrevWord : EditAction
	{
		public override void Run (TextEditorData data)
		{
			data.Caret.Offset = data.Document.FindPrevWordOffset (data.Caret.Offset);
		}
	}
	
	public class DeletePrevWord : EditAction
	{
		public override void Run (TextEditorData data)
		{
			int oldLine = data.Caret.Line;
			int offset = data.Document.FindPrevWordOffset (data.Caret.Offset);
			if (data.Caret.Offset != offset) {
				data.Document.Remove (offset, data.Caret.Offset - offset);
				data.Caret.Offset = offset;
			}
			if (oldLine != data.Caret.Line)
				data.Document.CommitLineToEndUpdate (data.Caret.Line);
			
		}
	}
	
	public class DeleteNextWord : EditAction
	{
		public override void Run (TextEditorData data)
		{
			int offset = data.Document.FindNextWordOffset (data.Caret.Offset);
			if (data.Caret.Offset != offset) 
				data.Document.Remove (data.Caret.Offset, offset - data.Caret.Offset);
			data.Document.CommitLineToEndUpdate (data.Caret.Line);
		}
	}
	
	public class DeleteCaretLine : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (data.Document.LineCount <= 1)
				return;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			data.Document.Remove (line.Offset, line.Length);
			data.Document.CommitLineToEndUpdate (data.Caret.Line);
		}
	}
	
	public class DeleteCaretLineToEnd : EditAction
	{
		public override void Run (TextEditorData data)
		{
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			data.Document.Remove (line.Offset + data.Caret.Column, line.EditableLength - data.Caret.Column);
			data.Document.CommitLineUpdate (data.Caret.Line);
		}
	}
	
	
	public class CaretMoveRight : EditAction
	{
		public override void Run (TextEditorData data)
		{
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			List<FoldSegment> foldings = data.Document.GetStartFoldings (line);
			FoldSegment segment = null;
			foreach (FoldSegment folding in foldings) {
				if (folding.IsFolded && folding.Column == data.Caret.Column) {
					segment = folding;
					break;
				}
			}
			if (segment != null) {
				data.Caret.Location = data.Document.OffsetToLocation (segment.EndLine.Offset + segment.EndColumn); 
				return;
			}
			if (data.Caret.Column < line.EditableLength) {
				data.Caret.Column++;
			} else if (data.Caret.Line + 1 < data.Document.LineCount) {
				data.Caret.Location = new DocumentLocation (data.Caret.Line + 1, 0);
			}
		}
	}
	public class CaretMoveNextWord : EditAction
	{
		public override void Run (TextEditorData data)
		{
			data.Caret.Offset = data.Document.FindNextWordOffset (data.Caret.Offset);
		}
	}
	
	public class CaretMoveUp : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (data.Caret.Line > 0) {
				data.Caret.Line = data.Document.VisualToLogicalLine (data.Document.LogicalToVisualLine (data.Caret.Line) - 1);
			}
		}
	}
	
	public class CaretMoveDown : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (data.Caret.Line < data.Document.LineCount - 1) {
				data.Caret.Line = data.Document.VisualToLogicalLine (data.Document.LogicalToVisualLine (data.Caret.Line) + 1);
			}
		}
	}
	
	public class CaretMoveHome : EditAction
	{
		int GetHomeMark (Document document, LineSegment line)
		{
			int result;
			for (result = 0; result < line.EditableLength; result++)
				if (!Char.IsWhiteSpace (document.GetCharAt (line.Offset + result)))
					return result;
			return result;
		}
		
		public override void Run (TextEditorData data)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			DocumentLocation newLocation = data.Caret.Location;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			int homeMark = GetHomeMark (data.Document, line);
			newLocation.Column = newLocation.Column == homeMark ? 0 : homeMark;
			
			// handle folding
			List<FoldSegment> foldings = data.Document.GetEndFoldings (line);
			FoldSegment segment = null;
			foreach (FoldSegment folding in foldings) {
				if (folding.IsFolded && folding.Contains (data.Document.LocationToOffset (newLocation))) {
					segment = folding;
					break;
				}
			}
			if (segment != null) 
				newLocation = data.Document.OffsetToLocation (segment.StartLine.Offset); 
			
			if (newLocation != data.Caret.Location) 
				data.Caret.Location = newLocation;
		}
	}
	
	public class CaretMoveEnd : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			DocumentLocation newLocation = data.Caret.Location;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			newLocation.Column = line.EditableLength;
			
			// handle folding
			List<FoldSegment> foldings = data.Document.GetStartFoldings (line);
			FoldSegment segment = null;
			foreach (FoldSegment folding in foldings) {
				if (folding.IsFolded && folding.Contains (data.Document.LocationToOffset (newLocation))) {
					segment = folding;
					break;
				}
			}
			if (segment != null) 
				newLocation = data.Document.OffsetToLocation (segment.EndLine.Offset + segment.EndColumn); 
			if (newLocation != data.Caret.Location) 
				data.Caret.Location = newLocation;
			
		}
	}

	public class CaretMoveToDocumentStart : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			data.Caret.Location = new DocumentLocation (0, 0);
		}
	}
	
	public class CaretMoveToDocumentEnd : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			data.Caret.Offset = data.Document.Length;
		}
	}
	
	public class SelectionSelectAll : CaretMoveLeft
	{
		public override void Run (TextEditorData data)
		{
			data.Caret.AutoScrollToCaret = false;
			data.Caret.PreserveSelection = true;
			data.SelectionAnchor = 0;
			new CaretMoveToDocumentEnd ().Run (data);
			data.ExtendSelectionTo (data.Document.Length);
			data.Caret.PreserveSelection = false;
			data.Caret.AutoScrollToCaret = true;
		}
	}
	
	public class SelectionMoveLeft : CaretMoveLeft
	{
		public static void StartSelection (TextEditorData data)
		{
			data.Caret.PreserveSelection = true;
			if (!data.IsSomethingSelected)
				data.SelectionAnchor = data.Caret.Offset;
		}
		
		public static void EndSelection (TextEditorData data)
		{
			data.ExtendSelectionTo (data.Caret.Offset);
			data.Caret.PreserveSelection = false;
		}
		
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class SelectionMovePrevWord : CaretMovePrevWord
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class SelectionMoveRight : CaretMoveRight
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	public class SelectionMoveNextWord : CaretMoveNextWord
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class SelectionMoveUp : CaretMoveUp
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class SelectionMoveDown : CaretMoveDown
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class SelectionMoveHome : CaretMoveHome
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class SelectionMoveEnd : CaretMoveEnd
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}

	public class SelectionMoveToDocumentStart : CaretMoveToDocumentStart
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class SelectionMoveToDocumentEnd : CaretMoveToDocumentEnd
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class SelectionPageUpAction : PageUpAction
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class SelectionPageDownAction : PageDownAction
	{
		public override void Run (TextEditorData data)
		{
			SelectionMoveLeft.StartSelection (data);
			base.Run (data);
			SelectionMoveLeft.EndSelection (data);
		}
	}
	
	public class ScrollUpAction : CaretMoveToDocumentEnd
	{
		public override void Run (TextEditorData data)
		{
			data.VAdjustment.Value = System.Math.Max (data.VAdjustment.Lower, data.VAdjustment.Value - data.VAdjustment.StepIncrement); 
		}
	}
	
	public class ScrollDownAction : CaretMoveToDocumentEnd
	{
		public override void Run (TextEditorData data)
		{
			data.VAdjustment.Value = System.Math.Min (data.VAdjustment.Upper - data.VAdjustment.PageSize, data.VAdjustment.Value + data.VAdjustment.StepIncrement); 
		}
	}	
	
	public class PageUpAction : CaretMoveToDocumentEnd
	{
		public static int LineHeight {
			get {
				return 16;
			}
		}
		public override void Run (TextEditorData data)
		{
			int pageIncrement =  LineHeight * ((int)(data.VAdjustment.PageIncrement / LineHeight) - 1);
			data.VAdjustment.Value = System.Math.Max (data.VAdjustment.Lower, data.VAdjustment.Value - pageIncrement); 
			if (data.Caret.Line - pageIncrement / LineHeight > 0)
				data.Caret.Line -= pageIncrement / LineHeight;
			else
				data.Caret.Line = 0;
		}
	}
	
	public class PageDownAction : CaretMoveToDocumentEnd
	{
		public override void Run (TextEditorData data)
		{
			int pageIncrement =  PageUpAction.LineHeight * ((int)(data.VAdjustment.PageIncrement / PageUpAction.LineHeight) - 1);
			if (data.VAdjustment.Value < data.VAdjustment.Upper - data.VAdjustment.PageIncrement)
				data.VAdjustment.Value = data.VAdjustment.Value + pageIncrement;
			if (data.Caret.Line + pageIncrement / PageUpAction.LineHeight < data.Document.LineCount)
				data.Caret.Line += pageIncrement / PageUpAction.LineHeight;
			else 
				data.Caret.Line = data.Document.LineCount - 1;
		}
	}	
	
	public class GotoMatchingBracket : EditAction
	{
		public override void Run (TextEditorData data)
		{
			int matchingBracketOffset = data.Document.GetMatchingBracketOffset (data.Caret.Offset);
			if (matchingBracketOffset == -1 && data.Caret.Offset > 0)
				matchingBracketOffset = data.Document.GetMatchingBracketOffset (data.Caret.Offset - 1);
			
			if (matchingBracketOffset != -1)
				data.Caret.Offset = matchingBracketOffset;
		}
	}

#endregion
	
	public class RemoveTab : EditAction
	{
		public static int RemoveTabInLine (Document document, LineSegment line)
		{
			if (line.Length == 0)
				return 0;
			char ch = document.GetCharAt (line.Offset); 
			if (ch == '\t') {
				document.Remove (line.Offset, 1);
				return 1;
			} else if (ch == ' ') {
				int removeCount = 0;
				for (int i = 0; i < TextEditorOptions.Options.IndentationSize;) {
					ch = document.GetCharAt (line.Offset + i);
					if (ch == ' ') {
						removeCount ++;
						i++;
					} else  if (ch == '\t') {
						removeCount ++;
						i += TextEditorOptions.Options.TabSize;
					} else {
						break;
					}
				}
				document.Remove (line.Offset, removeCount);
				return removeCount;
			}
			return 0;
		}
		
		public static void RemoveIndentSelection (TextEditorData data)
		{
			Debug.Assert (data.IsSomethingSelected);
			int startLineNr = data.IsSomethingSelected ? data.Document.OffsetToLineNumber (data.SelectionRange.Offset) : data.Caret.Line;
			int endLineNr   = data.IsSomethingSelected ? data.Document.OffsetToLineNumber (data.SelectionRange.EndOffset) : data.Caret.Line;
			if (endLineNr < 0)
				endLineNr = data.Document.LineCount;
			LineSegment anchorLine   = data.IsSomethingSelected ? data.Document.GetLineByOffset (data.SelectionAnchor) : null;
			int         anchorColumn = data.IsSomethingSelected ? data.SelectionAnchor - anchorLine.Offset : -1;
			data.Document.BeginAtomicUndo ();
			int first = -1;
			int last  = 0;
			foreach (LineSegment line in data.SelectedLines) {
				last = RemoveTabInLine (data.Document, line);
				if (first < 0)
					first = last;
			}
			
			if (data.IsSomethingSelected) {
				if (data.SelectionAnchor < data.Caret.Offset) {
					data.SelectionAnchor = System.Math.Min (anchorLine.Offset + anchorLine.EditableLength, System.Math.Max (anchorLine.Offset, data.SelectionAnchor - first));
				} else {
					data.SelectionAnchor = System.Math.Min (anchorLine.Offset + anchorLine.EditableLength, System.Math.Max (anchorLine.Offset, anchorLine.Offset + anchorColumn - last));
				}
			}
			
			if (data.Caret.Column != 0) {
				data.Caret.PreserveSelection = true;
				data.Caret.Column = System.Math.Max (0, data.Caret.Column - last);
				data.Caret.PreserveSelection = false;
			}
			
			if (data.IsSomethingSelected) 
				data.ExtendSelectionTo (data.Caret.Location);
			
			data.Document.EndAtomicUndo ();
			data.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			data.Document.CommitDocumentUpdate ();
		}
		
		public override void Run (TextEditorData data)
		{
			if (data.IsMultiLineSelection) {
				RemoveIndentSelection (data);
				return;
			} else {
				LineSegment line = data.Document.GetLine (data.Caret.Line);
				int visibleColumn = 0;
				for (int i = 0; i < data.Caret.Column; ++i)
					visibleColumn += data.Document.GetCharAt (line.Offset + i) == '\t' ? TextEditorOptions.Options.TabSize : 1;
				
				int newColumn = ((visibleColumn / TextEditorOptions.Options.IndentationSize) - 1) * TextEditorOptions.Options.IndentationSize;
				
				visibleColumn = 0;
				for (int i = 0; i < data.Caret.Column; ++i) {
					visibleColumn += data.Document.GetCharAt (line.Offset + i) == '\t' ? TextEditorOptions.Options.TabSize : 1;
					if (visibleColumn >= newColumn) {
						data.Caret.Column = i;
						break;
					}
				}
			}
		}
	}
		
	public class InsertTab : EditAction
	{
		public static void IndentSelection (TextEditorData data)
		{
			int startLineNr = data.IsSomethingSelected ? data.Document.OffsetToLineNumber (data.SelectionRange.Offset) : data.Caret.Line;
			int endLineNr   = data.IsSomethingSelected ? data.Document.OffsetToLineNumber (data.SelectionRange.EndOffset) : data.Caret.Line;
			if (endLineNr < 0)
				endLineNr = data.Document.LineCount;
			
			LineSegment anchorLine   = data.IsSomethingSelected ? data.Document.GetLineByOffset (data.SelectionAnchor) : null;
			int         anchorColumn = data.IsSomethingSelected ? data.SelectionAnchor - anchorLine.Offset : -1;
			data.Document.BeginAtomicUndo ();
			foreach (LineSegment line in data.SelectedLines) {
				data.Document.Insert (line.Offset, TextEditorOptions.Options.IndentationString);
			}
			if (data.IsSomethingSelected) {
				if (data.SelectionAnchor < data.Caret.Offset) {
					if (anchorColumn != 0) 
						data.SelectionAnchor = System.Math.Min (anchorLine.Offset + anchorLine.EditableLength, System.Math.Max (anchorLine.Offset, data.SelectionAnchor + TextEditorOptions.Options.IndentationString.Length));
				} else {
					if (anchorColumn != 0) {
						data.SelectionAnchor = System.Math.Min (anchorLine.Offset + anchorLine.EditableLength, System.Math.Max (anchorLine.Offset, anchorLine.Offset + anchorColumn + TextEditorOptions.Options.IndentationString.Length));
					} else {
						data.SelectionAnchor = anchorLine.Offset;
					}
				}
			}
			
			if (data.Caret.Column != 0) {
				data.Caret.PreserveSelection = true;
				data.Caret.Column++;
				data.Caret.PreserveSelection = false;
			}
			if (data.IsSomethingSelected) 
				data.ExtendSelectionTo (data.Caret.Offset);
			data.Document.EndAtomicUndo ();
			data.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			data.Document.CommitDocumentUpdate ();
		}
		
		public override void Run (TextEditorData data)
		{
			if (data.IsMultiLineSelection) {
				IndentSelection (data);
				return;
			}
			data.Document.BeginAtomicUndo ();
			if (data.IsSomethingSelected) {
				data.DeleteSelectedText ();
			}
			data.Document.Insert (data.Caret.Offset, TextEditorOptions.Options.IndentationString);
			data.Caret.Column += TextEditorOptions.Options.IndentationString.Length;
			data.Document.EndAtomicUndo ();
		}
	}
	
	public class InsertNewLine : EditAction
	{
		public override void Run (TextEditorData data)
		{
			data.Document.BeginAtomicUndo ();
			if (data.IsSomethingSelected) {
				data.DeleteSelectedText ();
			}
			
			string newLine = data.Document.EolMarker;
			int caretColumnOffset = 0;
			
			if (TextEditorOptions.Options.AutoIndent) {
				LineSegment line = data.Document.GetLine (data.Caret.Line);
				int i;
				for (i = 0; i < line.EditableLength; i++) {
					char ch = data.Document.GetCharAt (line.Offset + i);
					if (!Char.IsWhiteSpace (ch))
						break;
				}
				caretColumnOffset = i;
				newLine += data.Document.GetTextBetween (line.Offset, line.Offset + i);
			}
			
			data.Document.Insert (data.Caret.Offset, newLine);
			data.Caret.Column = caretColumnOffset;
			data.Caret.Line++;
			data.Document.EndAtomicUndo ();
		}
	}
	
	public class BackspaceAction : EditAction
	{
		protected virtual void RemoveCharBeforCaret (TextEditorData data)
		{
			data.Document.Remove (data.Caret.Offset - 1, 1);
			data.Caret.Column--;
		}
		
		public override void Run (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				data.DeleteSelectedText ();
				return;
			}
			if (data.Caret.Offset == 0)
				return;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			if (data.Caret.Offset == line.Offset) {
				LineSegment lineAbove = data.Document.GetLine (data.Caret.Line - 1);
				data.Caret.Location = new DocumentLocation (data.Caret.Line - 1, lineAbove.EditableLength);
				data.Document.Remove (lineAbove.EndOffset - lineAbove.DelimiterLength, lineAbove.DelimiterLength);
			} else {
				RemoveCharBeforCaret (data);
			}
		}
	}
	
	public class DeleteAction : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				data.DeleteSelectedText ();
				return;
			}
			if (data.Caret.Offset >= data.Document.Length)
				return;
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			if (data.Caret.Column == line.EditableLength) {
				if (data.Caret.Line < data.Document.LineCount) { 
					data.Document.Remove (line.EndOffset - line.DelimiterLength, line.DelimiterLength);
					if (line.EndOffset == data.Document.Length)
						line.DelimiterLength = 0;
				}
			} else {
				data.Document.Remove (data.Caret.Offset, 1); 
				data.Document.CommitLineUpdate (data.Caret.Line);
			}
		}
	}
	
	public class SwitchCaretModeAction : EditAction
	{
		public override void Run (TextEditorData data)
		{
			data.Caret.IsInInsertMode = !data.Caret.IsInInsertMode;
			data.Document.RequestUpdate (new SinglePositionUpdate (data.Caret.Line, data.Caret.Column));
			data.Document.CommitDocumentUpdate ();
		}
	}
	
	
#region Clipboard
	public class CutAction : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				new CopyAction ().Run (data);
				new DeleteAction ().Run (data);
			}
		}
	}
	
	public class CopyAction : EditAction
	{
		public const int TextType     = 1;		
		public const int RichTextType = 2;
		
		const int UTF8_FORMAT = 8;
		
		public static readonly Gdk.Atom CLIPBOARD_ATOM        = Gdk.Atom.Intern ("CLIPBOARD", false);
		public static readonly Gdk.Atom PRIMARYCLIPBOARD_ATOM = Gdk.Atom.Intern ("PRIMARY", false);
		static readonly Gdk.Atom RTF_ATOM = Gdk.Atom.Intern ("text/rtf", false);
		
		public void SetData (SelectionData selection_data, uint info)
		{
			if (selection_data == null)
				return;
			switch (info) {
			case TextType:
				selection_data.Text = text;
				break;
			case RichTextType:
				selection_data.Set (RTF_ATOM, UTF8_FORMAT, System.Text.Encoding.UTF8.GetBytes (rtf.ToString ()));
				break;
			}
		}
		
			
		void ClipboardGetFunc (Clipboard clipboard, SelectionData selection_data, uint info)
		{
			SetData (selection_data, info);
		}
		void ClipboardClearFunc (Clipboard clipboard)
		{
			// NOTHING ?
		}
		
		string text;
		string rtf;
		static string GenerateRtf (TextEditorData data)
		{
			if (!data.IsSomethingSelected) {
				return "";
			}
			StringBuilder rtfText = new StringBuilder ();
			List<Gdk.Color> colorList = new List<Gdk.Color> ();

			ISegment selection = data.SelectionRange;
			LineSegment line    = data.Document.GetLineByOffset (selection.Offset);
			LineSegment endLine = data.Document.GetLineByOffset (selection.EndOffset);
			
			RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = line.Iter;
			bool isItalic = false;
			bool isBold   = false;
			int curColor  = -1;
			do {
				line = iter.Current;
				Mono.TextEditor.Highlighting.SyntaxMode mode = data.Document.SyntaxMode != null && TextEditorOptions.Options.EnableSyntaxHighlighting ? data.Document.SyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode.Default;
				Chunk[] chunks = mode.GetChunks (data.Document, data.ColorStyle, line, line.Offset, line.Offset + line.EditableLength);
				foreach (Chunk chunk in chunks) {
					int start = System.Math.Max (selection.Offset, chunk.Offset);
					int end   = System.Math.Min (chunk.EndOffset, selection.EndOffset);
					if (start < end) {
						bool appendSpace = false;
						if (isBold != chunk.Style.Bold) {
							rtfText.Append (chunk.Style.Bold ? @"\b" : @"\b0");
							isBold = chunk.Style.Bold;
							appendSpace = true;
						}
						if (isItalic != chunk.Style.Italic) {
							rtfText.Append (chunk.Style.Italic ? @"\i" : @"\i0");
							isItalic = chunk.Style.Italic;
							appendSpace = true;
						}
						if (!colorList.Contains (chunk.Style.Color)) 
							colorList.Add (chunk.Style.Color);
						int color = colorList.IndexOf (chunk.Style.Color);
						if (curColor != color) {
							curColor = color;
							rtfText.Append (@"\cf" + (curColor + 1));					
							appendSpace = true;
						}
						for (int i = start; i < end; i++) {
							char ch = data.Document.GetCharAt (i);
							if (appendSpace && ch != '\t') {
								rtfText.Append (' ');
								appendSpace = false;
							}							
							switch (ch) {
							case '\\':
								rtfText.Append (@"\\");
								break;
							case '{':
								rtfText.Append (@"\{");
								break;
							case '}':
								rtfText.Append (@"\}");
								break;
							case '\t':
								rtfText.Append (@"\tab");
								appendSpace = true;
								break;
							default:
								rtfText.Append (ch);
								break;
							}
						}
					}
				}
				if (line == endLine)
					break;
				rtfText.Append (@"\par");
			} while (iter.MoveNext ());
			
			// color table
			StringBuilder colorTable = new StringBuilder ();
			colorTable.Append (@"{\colortbl ;");
			for (int i = 0; i < colorList.Count; i++) {
				Gdk.Color color = colorList[i];
				colorTable.Append (@"\red");
				colorTable.Append (color.Red / 256);
				colorTable.Append (@"\green");
				colorTable.Append (color.Green / 256); 
				colorTable.Append (@"\blue");
				colorTable.Append (color.Blue / 256);
				colorTable.Append (";");
			}
			colorTable.Append ("}");
			
			
			StringBuilder rtf = new StringBuilder();
			rtf.Append (@"{\rtf1\ansi\deff0\adeflang1025");
			
			// font table
			rtf.Append (@"{\fonttbl");
			rtf.Append (@"{\f0\fnil\fprq1\fcharset128 " + TextEditorOptions.Options.Font.Family + ";}");
			rtf.Append ("}");
			
			rtf.Append (colorTable.ToString ());
			
			rtf.Append (@"\viewkind4\uc1\pard");
			rtf.Append (@"\f0");
			try {
				string fontName = TextEditorOptions.Options.Font.ToString ();
				double fontSize = Double.Parse (fontName.Substring (fontName.LastIndexOf (' ')  + 1)) * 2;
				rtf.Append (@"\fs");
				rtf.Append (fontSize);
			} catch (Exception) {};
			rtf.Append (@"\cf1");
			rtf.Append (rtfText.ToString ());
			rtf.Append("}");
	//		System.Console.WriteLine(rtf);
			return rtf.ToString ();
		}
		
		public static Gtk.TargetList TargetList {
			get {
				Gtk.TargetList list = new Gtk.TargetList ();
				list.Add (RTF_ATOM, /* FLAGS */ 0, RichTextType);
				list.AddTextTargets (TextType);
				return list;
			}
		}
		
		void CopyData (TextEditorData data, ISegment segment)
		{
			if (segment != null && data != null && data.Document != null) {
				try {
					text = segment.Length > 0 ? data.Document.GetTextAt (segment) : "";
				} catch (Exception) {
					System.Console.WriteLine("Copy data failed - unable to get text at:" + segment);
					throw;
				}
				try {
					rtf  = GenerateRtf (data);
				} catch (Exception) {
					System.Console.WriteLine("Copy data failed - unable to generate rtf for text at:" + segment);
					throw;
				}
			} else {
				text = rtf = null;
			}
		}
		public void CopyData (TextEditorData data)
		{
			CopyData (data, data.SelectionRange);
		}
		
		TextEditorData data;
		ISegment       selection;
		
		void ClipboardGetFuncLazy (Clipboard clipboard, SelectionData selection_data, uint info)
		{
			CopyData (data, selection);
			ClipboardGetFunc (clipboard, selection_data, info);
		}
		
		public void CopyToPrimary (TextEditorData data)
		{
			this.data      = data;
			this.selection = data.SelectionRange;
			
			Clipboard clipboard = Clipboard.Get (CopyAction.PRIMARYCLIPBOARD_ATOM);
			clipboard.SetWithData ((Gtk.TargetEntry[])TargetList, ClipboardGetFuncLazy, ClipboardClearFunc);
		}
		public void ClearPrimary ()
		{
			Clipboard clipboard = Clipboard.Get (CopyAction.PRIMARYCLIPBOARD_ATOM);
			clipboard.Clear ();
		}
		
		
		public override void Run (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				Clipboard clipboard = Clipboard.Get (CopyAction.CLIPBOARD_ATOM);
				CopyAction action = new CopyAction ();
				action.CopyData (data);
				
				if (Copy != null)
					Copy (action.text);
				clipboard.SetWithData ((Gtk.TargetEntry[])TargetList, action.ClipboardGetFunc, action.ClipboardClearFunc);
			}
		}
		public delegate void CopyDelegate (string text);
		public static event CopyDelegate Copy;
	}
	
	public class PasteAction : EditAction
	{
		static int PasteFrom (Clipboard clipboard, TextEditorData data)
		{
			int result = -1;
			if (clipboard.WaitIsTextAvailable ()) {
				data.Document.BeginAtomicUndo ();
				if (data.IsSomethingSelected) {
					data.DeleteSelectedText ();
				}
				StringBuilder sb = new StringBuilder (clipboard.WaitForText ());
				data.Document.Insert (data.Caret.Offset, sb);
				//int oldLine = data.Caret.Line;
				result = sb.Length;
				data.Caret.Offset += sb.Length;
				data.Document.EndAtomicUndo ();
			}
			return result;
		}
		public static int PasteFromPrimary (TextEditorData data)
		{
			return PasteFrom (Clipboard.Get (CopyAction.PRIMARYCLIPBOARD_ATOM), data);
		}
		public override void Run (TextEditorData data)
		{
			PasteFrom (Clipboard.Get (CopyAction.CLIPBOARD_ATOM), data);
		}
	}
#endregion
	
	public class UndoAction : EditAction
	{
		public override void Run (TextEditorData data)
		{
			data.Document.Undo ();
		}
	}
	
	public class RedoAction : EditAction
	{
		public override void Run (TextEditorData data)
		{
			data.Document.Redo ();
		}
	}
#region Bookmarks
	public class GotoNextBookmark : EditAction
	{
		int GetNextOffset (Document document, int lineNumber)
		{
			LineSegment startLine = document.GetLine (lineNumber);
			RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = startLine.Iter;
			while (iter.MoveNext ()) {
				LineSegment line = iter.Current;
				if (line.IsBookmarked) {
					return line.Offset;
				}
			}
			return -1;
		}
		public override void Run (TextEditorData data)
		{
			int offset = GetNextOffset (data.Document, data.Caret.Line);
			if (offset < 0)
				offset = GetNextOffset (data.Document, 0);
			if (offset >= 0)
				data.Caret.Offset = offset;
		}
	}
	
	public class GotoPrevBookmark : EditAction
	{
		int GetPrevOffset (Document document, int lineNumber)
		{
			LineSegment startLine = document.GetLine (lineNumber);
			RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = startLine.Iter;
			while (iter.MoveBack ()) {
				LineSegment line = iter.Current;
				if (line.IsBookmarked) {
					return line.Offset;
				}
			}
			return -1;
		}
		public override void Run (TextEditorData data)
		{
			int offset = GetPrevOffset (data.Document, data.Caret.Line);
			if (offset < 0)
				offset = GetPrevOffset (data.Document, data.Document.LineCount - 1);
			if (offset >= 0)
				data.Caret.Offset = offset;
		}
	}
	
	public class ClearAllBookmarks : EditAction
	{
		public override void Run (TextEditorData data)
		{
			bool redraw = false;
			foreach (LineSegment line in data.Document.Lines) {
				redraw |= line.IsBookmarked;
				line.IsBookmarked = false;
			}
			if (redraw) {
				data.Document.RequestUpdate (new UpdateAll ());
				data.Document.CommitDocumentUpdate ();
			}
		}
	}
	
#endregion
	
}