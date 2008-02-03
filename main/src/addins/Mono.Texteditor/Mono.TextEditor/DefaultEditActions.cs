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
using System.Text;

using Gtk;

namespace Mono.TextEditor
{
#region Caret Movement
	public class CaretMoveLeft : EditAction
	{
		public override void Run (TextEditorData data)
		{
			LineSegment line = data.Document.Splitter.Get (data.Caret.Line);
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
				LineSegment prevLine = data.Document.Splitter.Get (data.Caret.Line - 1);
				data.Caret.Location = new DocumentLocation (data.Caret.Line - 1, prevLine.EditableLength);
			}
		}
	}
	
	public class CaretMovePrevWord : EditAction
	{
		public static int FindPrevWordOffset (Document document, int offset)
		{
			if (offset <= 0)
				return 0;
			int  result = offset - 1;
			bool isLetter = Char.IsLetterOrDigit (document.Buffer.GetCharAt (result));
			while (result > 0) {
				char ch = document.Buffer.GetCharAt (result);
				if (isLetter) {
					if (Char.IsLetterOrDigit (ch)) 
						result--;
					else {
						result++;
						break;
					}
				} else {
					if (Char.IsLetterOrDigit (ch)) {
						return FindPrevWordOffset (document, result);
					} else 
						result--;
				}
			}
			foreach (FoldSegment segment in document.GetFoldingsFromOffset (result)) {
				if (segment.IsFolded)
					result = System.Math.Min (result, segment.StartLine.Offset + segment.Column);
			}
			return result;
		}
		public override void Run (TextEditorData data)
		{
			data.Caret.Offset = FindPrevWordOffset (data.Document, data.Caret.Offset);
		}
	}
	
	public class DeletePrevWord : EditAction
	{
		public override void Run (TextEditorData data)
		{
			int offset = CaretMovePrevWord.FindPrevWordOffset (data.Document, data.Caret.Offset);
			if (data.Caret.Offset != offset) {
				data.Document.Buffer.Remove (offset, data.Caret.Offset - offset);
				data.Caret.Offset = offset;
			}
		}
	}
	
	public class DeleteNextWord : EditAction
	{
		public override void Run (TextEditorData data)
		{
			int offset = CaretMoveNextWord.FindNextWordOffset (data.Document, data.Caret.Offset);
			if (data.Caret.Offset != offset) {
				data.Document.Buffer.Remove (data.Caret.Offset, offset - data.Caret.Offset);
			}
		}
	}
	
	public class DeleteCaretLine : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (data.Document.Splitter.LineCount <= 1)
				return;
			LineSegment line = data.Document.Splitter.Get (data.Caret.Line);
			data.Document.Buffer.Remove (line.Offset, line.Length);
			data.Caret.SetColumn ();
		}
	}
	
	public class DeleteCaretLineToEnd : EditAction
	{
		public override void Run (TextEditorData data)
		{
			LineSegment line = data.Document.Splitter.Get (data.Caret.Line);
			data.Document.Buffer.Remove (line.Offset + data.Caret.Column, line.EditableLength - data.Caret.Column);
		}
	}
	
	
	public class CaretMoveRight : EditAction
	{
		public override void Run (TextEditorData data)
		{
			LineSegment line = data.Document.Splitter.Get (data.Caret.Line);
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
			} else if (data.Caret.Line + 1 < data.Document.Splitter.LineCount) {
				data.Caret.Location = new DocumentLocation (data.Caret.Line + 1, 0);
			}
		}
	}
	public class CaretMoveNextWord : EditAction
	{
		public static int FindNextWordOffset (Document document, int offset)
		{
			if (offset + 1 >= document.Buffer.Length)
				return document.Buffer.Length;
			int result = offset + 1;
			bool isLetter = Char.IsLetterOrDigit (document.Buffer.GetCharAt (result));
			while (result < document.Buffer.Length) {
				char ch = document.Buffer.GetCharAt (result);
				if (isLetter) {
					if (Char.IsLetterOrDigit (ch)) 
						result++;
					else {
						break;
					}
				} else {
					if (Char.IsLetterOrDigit (ch)) {
						return FindNextWordOffset (document, result);
					} else 
						result++;
				}
			}
			foreach (FoldSegment segment in document.GetFoldingsFromOffset (result)) {
				if (segment.IsFolded)
					result = System.Math.Max (result, segment.EndLine.Offset + segment.EndColumn);
			}
			return result;
		}
		public override void Run (TextEditorData data)
		{
			data.Caret.Offset = FindNextWordOffset (data.Document, data.Caret.Offset);
		}
	}
	
	public class CaretMoveUp : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (data.Caret.Line > 0) {
				data.Caret.Line = data.Document.VisualToLogicalLine (data.Document.LogicalToVisualLine (data.Caret.Line) - 1);
				data.Caret.SetColumn ();
			}
		}
	}
	
	public class CaretMoveDown : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (data.Caret.Line < data.Document.Splitter.LineCount - 1) {
				data.Caret.Line = data.Document.VisualToLogicalLine (data.Document.LogicalToVisualLine (data.Caret.Line) + 1);
				data.Caret.SetColumn ();
			}
		}
	}
	
	public class CaretMoveHome : EditAction
	{
		int GetHomeMark (Document document, LineSegment line)
		{
			int result;
			for (result = 0; result < line.EditableLength; result++)
				if (!Char.IsWhiteSpace (document.Buffer.GetCharAt (line.Offset + result)))
					return result;
			return result;
		}
		
		public override void Run (TextEditorData data)
		{
			DocumentLocation newLocation = data.Caret.Location;
			LineSegment line = data.Document.Splitter.Get (data.Caret.Line);
			int homeMark = GetHomeMark (data.Document, line);
			newLocation.Column = newLocation.Column == homeMark ? 0 : homeMark;
			if (newLocation != data.Caret.Location) 
				data.Caret.Location = newLocation;
		}
	}
	
	public class CaretMoveEnd : EditAction
	{
		public override void Run (TextEditorData data)
		{
			DocumentLocation newLocation = data.Caret.Location;
			LineSegment line = data.Document.Splitter.Get (data.Caret.Line);
			newLocation.Column = line.EditableLength;
			if (newLocation != data.Caret.Location) 
				data.Caret.Location = newLocation;
		}
	}

	public class CaretMoveToDocumentStart : EditAction
	{
		public override void Run (TextEditorData data)
		{
			DocumentLocation newLocation = new DocumentLocation (0, 0);
			if (newLocation != data.Caret.Location) 
				data.Caret.Location = newLocation;
		}
	}
	
	public class CaretMoveToDocumentEnd : EditAction
	{
		public override void Run (TextEditorData data)
		{
			data.Caret.Offset = data.Document.Buffer.Length;
		}
	}
	
	public class SelectionSelectAll : CaretMoveLeft
	{
		public override void Run (TextEditorData data)
		{
			data.Caret.AutoScrollToCaret = false;
			data.Caret.PreserveSelection = true;
			data.SelectionStart = new SelectionMarker (data.Document.GetLine (0), 0);
			new CaretMoveToDocumentEnd ().Run (data);
			SelectionMoveLeft.EndSelection (data);
			data.Caret.AutoScrollToCaret = true;
		}
	}
	
	public class SelectionMoveLeft : CaretMoveLeft
	{
		public static void StartSelection (TextEditorData data)
		{
			data.Caret.PreserveSelection = true;
			if (data.SelectionStart == null) {
				data.SelectionStart = new SelectionMarker (data.Document.GetLine (data.Caret.Line), data.Caret.Column);
			}
		}
		
		public static void EndSelection (TextEditorData data)
		{
			data.SelectionEnd = new SelectionMarker (data.Document.GetLine (data.Caret.Line), data.Caret.Column);
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
			if (data.Caret.Line + pageIncrement / PageUpAction.LineHeight < data.Document.Splitter.LineCount)
				data.Caret.Line += pageIncrement / PageUpAction.LineHeight;
			else 
				data.Caret.Line = data.Document.Splitter.LineCount - 1;
		}
	}	
#endregion
	
	public class RemoveTab : EditAction
	{
		public static int RemoveTabInLine (Document document, LineSegment line)
		{
			if (line.Length == 0)
				return 0;
			char ch = document.Buffer.GetCharAt (line.Offset); 
			if (ch == '\t') {
				document.Buffer.Remove (line.Offset, 1);
				return 1;
			} else if (ch == ' ') {
				int removeCount = 0;
				for (int i = 0; i < TextEditorOptions.Options.IndentationSize;) {
					ch = document.Buffer.GetCharAt (line.Offset + i);
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
				document.Buffer.Remove (line.Offset, removeCount);
				return removeCount;
			}
			return 0;
		}
		
		public static void RemoveIndentSelection (TextEditorData data)
		{
			int startLineNr = data.IsSomethingSelected ? data.Document.Splitter.GetLineNumberForOffset (data.SelectionRange.Offset) : data.Caret.Line;
			int endLineNr   = data.IsSomethingSelected ? data.Document.Splitter.GetLineNumberForOffset (data.SelectionRange.EndOffset) : data.Caret.Line;
			data.Document.BeginAtomicUndo ();
			int first = -1;
			int last  = 0;
			foreach (LineSegment line in data.SelectedLines) {
				last = RemoveTabInLine (data.Document, line);
				if (first < 0)
					first = last;
			}
			if (data.IsSomethingSelected)
				data.SelectionStart.Column = System.Math.Max (0, data.SelectionStart.Column - first);
			if (!data.IsSomethingSelected || data.SelectionEnd.Column != 0) {
				if (data.IsSomethingSelected)
					data.SelectionEnd.Column = System.Math.Max (0, data.SelectionEnd.Column - last);
				data.Caret.PreserveSelection = true;
				data.Caret.Column = System.Math.Max (0, data.Caret.Column - last);
				data.Caret.PreserveSelection = false;
			}
			data.Document.EndAtomicUndo ();
			data.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			data.Document.CommitDocumentUpdate ();
		}
		
		public override void Run (TextEditorData data)
		{
			if (data.IsSomethingSelected && data.SelectionStart.Segment != data.SelectionEnd.Segment) {
				RemoveIndentSelection (data);
				return;
			} else {
				LineSegment line = data.Document.Splitter.Get (data.Caret.Line);
				int visibleColumn = 0;
				for (int i = 0; i < data.Caret.Column; ++i)
					visibleColumn += data.Document.Buffer.GetCharAt (line.Offset + i) == '\t' ? TextEditorOptions.Options.TabSize : 1;
				
				int newColumn = ((visibleColumn / TextEditorOptions.Options.IndentationSize) - 1) * TextEditorOptions.Options.IndentationSize;
				
				visibleColumn = 0;
				for (int i = 0; i < data.Caret.Column; ++i) {
					visibleColumn += data.Document.Buffer.GetCharAt (line.Offset + i) == '\t' ? TextEditorOptions.Options.TabSize : 1;
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
			int startLineNr = data.IsSomethingSelected ? data.Document.Splitter.GetLineNumberForOffset (data.SelectionRange.Offset) : data.Caret.Line;
			int endLineNr   = data.IsSomethingSelected ? data.Document.Splitter.GetLineNumberForOffset (data.SelectionRange.EndOffset) : data.Caret.Line;
			data.Document.BeginAtomicUndo ();
			foreach (LineSegment line in data.SelectedLines) {
				data.Document.Buffer.Insert (line.Offset, new StringBuilder(TextEditorOptions.Options.IndentationString));
			}
			if (data.IsSomethingSelected)
				data.SelectionStart.Column++;
			if (!data.IsSomethingSelected || data.SelectionEnd.Column != 0) {
				if (data.IsSomethingSelected)
					data.SelectionEnd.Column++;
				data.Caret.PreserveSelection = true;
				data.Caret.Column++;
				data.Caret.PreserveSelection = false;
			}
			data.Document.EndAtomicUndo ();
			data.Document.RequestUpdate (new MultipleLineUpdate (startLineNr, endLineNr));
			data.Document.CommitDocumentUpdate ();
		}
		
		public override void Run (TextEditorData data)
		{
			if (data.IsSomethingSelected && data.SelectionStart.Segment != data.SelectionEnd.Segment) {
				IndentSelection (data);
				return;
			}
			
			if (data.IsSomethingSelected) {
				DeleteAction.DeleteSelection (data);
			}
			
			data.Document.Buffer.Insert (data.Caret.Offset, new StringBuilder (TextEditorOptions.Options.IndentationString));
			data.Caret.Column ++;
		}
	}
	
	public class InsertNewLine : EditAction
	{
		public override void Run (TextEditorData data)
		{
			StringBuilder newLine = new StringBuilder ("\n");
/*			if (TextEditorOptions.Options.AutoIndent) {
				LineSegment line = data.Document.GetLine (data.Caret.Line);
				for (int i = 0; i < line.EditableLength; i++) {
					char ch = data.Document.Buffer.GetCharAt (line.Offset + i);
					if (!Char.IsWhiteSpace (ch))
						break;
					newLine.Append (ch);
				}
			}*/
			data.Document.Buffer.Insert (data.Caret.Offset, newLine);
			data.Caret.Column = newLine.Length - 1;
			data.Caret.Line++;
		}
	}
	
	public class BackspaceAction : EditAction
	{
		public override void Run (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				DeleteAction.DeleteSelection (data);
				return;
			}
			if (data.Caret.Offset == 0)
				return;
			LineSegment line = data.Document.Splitter.Get (data.Caret.Line);
			if (data.Caret.Offset == line.Offset) {
				LineSegment lineAbove = data.Document.Splitter.Get (data.Caret.Line - 1);
				data.Caret.Location = new DocumentLocation (data.Caret.Line - 1, lineAbove.EditableLength);
				data.Document.Buffer.Remove (lineAbove.EndOffset - lineAbove.DelimiterLength, lineAbove.DelimiterLength);
			} else {
				data.Document.Buffer.Remove (data.Caret.Offset - 1, 1);
				data.Caret.Column--;
			}
		}
	}
	
	public class DeleteAction : EditAction
	{
		public static void DeleteSelection (TextEditorData data)
		{
			ISegment selection = data.SelectionRange;
			data.Caret.Offset = selection.Offset;
			data.Document.Buffer.Remove (selection.Offset, selection.Length);
			data.ClearSelection ();
		}
		public override void Run (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				DeleteSelection (data);
				return;
			}
			if (data.Caret.Offset >= data.Document.Buffer.Length)
				return;
			LineSegment line = data.Document.Splitter.Get (data.Caret.Line);
			if (data.Caret.Column == line.EditableLength) {
				if (data.Caret.Line < data.Document.Splitter.LineCount) { 
					data.Document.Buffer.Remove (line.EndOffset - line.DelimiterLength, line.DelimiterLength);
					if (line.EndOffset == data.Document.Buffer.Length)
						line.DelimiterLength = 0;
				}
				data.Document.RequestUpdate (new LineToEndUpdate (data.Caret.Line));
			} else {
				data.Document.Buffer.Remove (data.Caret.Offset, 1); 
				data.Document.RequestUpdate (new LineUpdate (data.Caret.Line));
			}
			data.Document.CommitDocumentUpdate ();
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
		
		public static readonly Gdk.Atom CLIPBOARD_ATOM = Gdk.Atom.Intern ("CLIPBOARD", false);
		static readonly Gdk.Atom RTF_ATOM = Gdk.Atom.Intern ("text/rtf", false);
		
		public void SetData (SelectionData selection_data, uint info)
		{
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
			LineSegment line    = data.Document.Splitter.GetByOffset (selection.Offset);
			LineSegment endLine = data.Document.Splitter.GetByOffset (selection.EndOffset);
			
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
							char ch = data.Document.Buffer.GetCharAt (i);
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
		
		public void CopyData (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				text = data.Document.Buffer.GetTextAt (data.SelectionRange);
				rtf  = GenerateRtf (data);
			}
		}
		
		public override void Run (TextEditorData data)
		{
			if (data.IsSomethingSelected) {
				Clipboard clipboard = Clipboard.Get (CopyAction.CLIPBOARD_ATOM);
				CopyData (data);
				
				clipboard.SetWithData ((Gtk.TargetEntry[])TargetList, ClipboardGetFunc, ClipboardClearFunc);
			}
		}
	}
	
	public class PasteAction : EditAction
	{
		public override void Run (TextEditorData data)
		{
			Clipboard clipboard = Clipboard.Get (CopyAction.CLIPBOARD_ATOM);
			if (clipboard.WaitIsTextAvailable ()) {
				if (data.IsSomethingSelected) {
					DeleteAction.DeleteSelection (data);
				}
				StringBuilder sb = new StringBuilder (clipboard.WaitForText ());
				data.Document.Buffer.Insert (data.Caret.Offset, sb);
				int oldLine = data.Caret.Line;
				data.Caret.Offset += sb.Length;
				data.Document.RequestUpdate (oldLine != data.Caret.Line ? (DocumentUpdateRequest)new LineToEndUpdate (oldLine) : (DocumentUpdateRequest)new LineUpdate (oldLine));
			}
		}
	}
#endregion
	
	public class UndoAction : EditAction
	{
		public override void Run (TextEditorData data)
		{
			Console.WriteLine ("Todo: Undo");
		}
	}
	
	public class RedoAction : EditAction
	{
		public override void Run (TextEditorData data)
		{
			Console.WriteLine ("Todo: Redo");
		}
	}
#region Bookmarks
	public class GotoNextBookmark : EditAction
	{
		int GetNextOffset (Document document, int lineNumber)
		{
			LineSegment startLine = document.Splitter.Get (lineNumber);
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
			LineSegment startLine = document.Splitter.Get (lineNumber);
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
				offset = GetPrevOffset (data.Document, data.Document.Splitter.LineCount - 1);
			if (offset >= 0)
				data.Caret.Offset = offset;
		}
	}
	
	public class ClearAllBookmarks : EditAction
	{
		public override void Run (TextEditorData data)
		{
			bool redraw = false;
			foreach (LineSegment line in data.Document.Splitter.Lines) {
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