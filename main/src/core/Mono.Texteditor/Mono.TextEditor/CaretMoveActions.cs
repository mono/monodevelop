// 
// CaretMoveActions.cs
// 
// Author:
//   Mike Krüger <mkrueger@novell.com>
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Gtk;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public static class CaretMoveActions
	{
		public static void Left (TextEditorData data)
		{
			if (Platform.IsMac && data.IsSomethingSelected && !data.Caret.PreserveSelection) {
				data.Caret.Offset = System.Math.Min (data.SelectionAnchor, data.Caret.Offset);
				data.ClearSelection ();
				return;
			}
			
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			IEnumerable<FoldSegment > foldings = data.Document.GetEndFoldings (line);
			FoldSegment segment = null;
			foreach (FoldSegment folding in foldings) {
				if (folding.IsFolded && folding.EndColumn + 1 == data.Caret.Column) {
					segment = folding;
					break;
				}
			}
			if (segment != null) {
				data.Caret.Location = data.Document.OffsetToLocation (segment.StartLine.Offset + segment.Column - 1); 
				return;
			}
			
			if (data.Caret.Column > DocumentLocation.MinColumn) {
				if (data.Caret.Column > line.EditableLength + 1) {
					data.Caret.Column = line.EditableLength + 1;
				} else {
					data.Caret.Column--;
				}
			} else if (data.Caret.Line > DocumentLocation.MinLine) {
				LineSegment prevLine = data.Document.GetLine (data.Caret.Line - 1);
				data.Caret.Location = new DocumentLocation (data.Caret.Line - 1, prevLine.EditableLength + 1);
			}
		}
		
		public static void PreviousWord (TextEditorData data)
		{
			data.Caret.Offset = data.FindPrevWordOffset (data.Caret.Offset);
		}
		
		public static void PreviousSubword (TextEditorData data)
		{
			data.Caret.Offset = data.FindPrevSubwordOffset (data.Caret.Offset);
		}
		
		public static void Right (TextEditorData data)
		{
			if (Platform.IsMac && data.IsSomethingSelected && !data.Caret.PreserveSelection) {
				data.Caret.Offset = System.Math.Max (data.SelectionAnchor, data.Caret.Offset);
				data.ClearSelection ();
				return;
			}
			
			LineSegment line = data.Document.GetLine (data.Caret.Line);
			IEnumerable<FoldSegment > foldings = data.Document.GetStartFoldings (line);
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
			if (data.Caret.Column < line.EditableLength + 1 || data.Caret.AllowCaretBehindLineEnd) {
				if (data.Caret.Column >= line.EditableLength + 1) {
					int nextColumn = data.GetNextVirtualColumn (data.Caret.Line, data.Caret.Column);
					if (data.Caret.Column != nextColumn) {
						data.Caret.Column = nextColumn;
					} else {
						data.Caret.Location = new DocumentLocation (data.Caret.Line + 1, DocumentLocation.MinColumn);
						data.Caret.CheckCaretPosition ();
					}
				} else {
					data.Caret.Column++;
				}
			} else if (data.Caret.Line + 1 <= data.Document.LineCount) {
				data.Caret.Location = new DocumentLocation (data.Caret.Line + 1, DocumentLocation.MinColumn);
			}
		}
		
		public static void NextWord (TextEditorData data)
		{
			data.Caret.Offset = data.FindNextWordOffset (data.Caret.Offset);
		}
		
		public static void NextSubword (TextEditorData data)
		{
			data.Caret.Offset = data.FindNextSubwordOffset (data.Caret.Offset);
		}
		
		public static void Up (TextEditorData data)
		{
			int desiredColumn = data.Caret.DesiredColumn;
			
			//on Mac, when deselecting and moving up/down a line, column is always the column of the selection's start
			if (Platform.IsMac && data.IsSomethingSelected && !data.Caret.PreserveSelection) {
				int col = data.MainSelection.Anchor > data.MainSelection.Lead ? data.MainSelection.Lead.Column : data.MainSelection.Anchor.Column;
				int line = data.MainSelection.MinLine - 1;
				data.ClearSelection ();
				data.Caret.Location = (line >=  DocumentLocation.MinLine) ? new DocumentLocation (line, col) : new DocumentLocation (DocumentLocation.MinLine, DocumentLocation.MinColumn);
				data.Caret.SetToDesiredColumn (desiredColumn);
				return;
			}
			
			if (data.Caret.Line > DocumentLocation.MinLine) {
				int visualLine = data.Document.LogicalToVisualLine (data.Caret.Line);
				int line = data.Document.VisualToLogicalLine (visualLine - 1);
				int offset = data.Document.LocationToOffset (line, data.Caret.Column);
				data.Caret.Offset = MoveCaretOutOfFolding (data, offset);
				data.Caret.SetToDesiredColumn (desiredColumn);
			} else {
				ToDocumentStart (data);
			}
		}
		
		static int MoveCaretOutOfFolding (TextEditorData data, int offset)
		{
			IEnumerable<FoldSegment> foldings = data.Document.GetFoldingsFromOffset (offset);
			foreach (FoldSegment folding in foldings) {
				if (folding.IsFolded) {
					if (offset < folding.EndOffset)
						offset = folding.EndOffset;
				}
			}
			return offset;
		}
		
		public static void Down (TextEditorData data)
		{
			//on Mac, when deselecting and moving up/down a line, column is always the column of the selection's start
			if (Platform.IsMac && data.IsSomethingSelected && !data.Caret.PreserveSelection) {
				int col = data.MainSelection.Anchor > data.MainSelection.Lead ? data.MainSelection.Lead.Column : data.MainSelection.Anchor.Column;
				int line = data.MainSelection.MaxLine + 1;
				data.ClearSelection ();
				if (line <= data.Document.LineCount) {
					int offset = data.Document.LocationToOffset (line, col);
					data.Caret.SetToOffsetWithDesiredColumn (MoveCaretOutOfFolding (data, offset));
				} else {
					data.Caret.Offset = data.Document.Length;
				}
				return;
			}
			
			if (data.Caret.Line < data.Document.LineCount) {
				int nextLine = data.Document.LogicalToVisualLine (data.Caret.Line) + 1;
				int line = data.Document.VisualToLogicalLine (nextLine);
				int offset = data.Document.LocationToOffset (line, data.Caret.Column);
				data.Caret.SetToOffsetWithDesiredColumn (MoveCaretOutOfFolding (data, offset));
			} else {
				ToDocumentEnd (data);
			}
		}
		
		static int GetHomeMark (Document document, LineSegment line)
		{
			int result;
			for (result = 0; result < line.EditableLength; result++)
				if (!Char.IsWhiteSpace (document.GetCharAt (line.Offset + result)))
					return result + 1;
			return result + 1;
		}
		
		static void InternalCaretMoveHome (TextEditorData data, bool firstNonWhitespace, bool hop)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			DocumentLocation newLocation = data.Caret.Location;

			LineSegment line = data.Document.GetLine (data.Caret.Line);
			
			if (firstNonWhitespace) {

				int homeMark = GetHomeMark (data.Document, line);
				if (hop) {
					newLocation.Column = data.Caret.Column == homeMark ? DocumentLocation.MinColumn : homeMark;
				} else {
					newLocation.Column = homeMark;
				}
			} else {
				newLocation.Column = DocumentLocation.MinColumn;
			}
			
			// handle folding
			IEnumerable<FoldSegment> foldings = data.Document.GetEndFoldings (line);
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
		
		public static void LineHome (TextEditorData data)
		{
			InternalCaretMoveHome (data, true, true);
		}
		
		public static void LineStart (TextEditorData data)
		{
			InternalCaretMoveHome (data, false, false);
		}
		
		public static void LineFirstNonWhitespace (TextEditorData data)
		{
			InternalCaretMoveHome (data, true, false);
		}

		public static void LineEnd (TextEditorData data)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			DocumentLocation newLocation = data.Caret.Location;
			LineSegment line = data.Document.GetLine (data.Caret.Line);

			newLocation.Column = line.EditableLength + 1;
			
			// handle folding
			IEnumerable<FoldSegment> foldings = data.Document.GetStartFoldings (line);
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
			
			if (data.Caret.AllowCaretBehindLineEnd) {
				int nextColumn = data.GetNextVirtualColumn (data.Caret.Line, data.Caret.Column);
				if (nextColumn != data.Caret.Column)
					data.Caret.Column = nextColumn;
			}
			
		}
		
		public static void ToDocumentStart (TextEditorData data)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			data.Caret.Location = new DocumentLocation (DocumentLocation.MinLine, DocumentLocation.MinColumn);
		}
		
		public static void ToDocumentEnd (TextEditorData data)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			data.Caret.Offset = data.Document.Length;
		}
		
		public static double LineHeight { get; set; }
		
		public static void PageUp (TextEditorData data)
		{
			int pageLines = (int)((data.VAdjustment.PageSize + ((int)data.VAdjustment.Value % LineHeight)) / LineHeight);
			int visualLine = data.Document.LogicalToVisualLine (data.Caret.Line);
			visualLine -= pageLines;
			int line = System.Math.Max (data.Document.VisualToLogicalLine (visualLine), DocumentLocation.MinLine);
			int offset = data.Document.LocationToOffset (line, data.Caret.Column);
			ScrollActions.PageUp (data);
			data.Caret.Offset = MoveCaretOutOfFolding (data, offset);
		}
		
		public static void PageDown (TextEditorData data)
		{
			int pageLines = (int)((data.VAdjustment.PageSize + ((int)data.VAdjustment.Value % LineHeight)) / LineHeight);
			int visualLine = data.Document.LogicalToVisualLine (data.Caret.Line);
			visualLine += pageLines;
			
			int line = System.Math.Min (data.Document.VisualToLogicalLine (visualLine), data.Document.LineCount);
			int offset = data.Document.LocationToOffset (line, data.Caret.Column);
			ScrollActions.PageDown (data);
			data.Caret.Offset = MoveCaretOutOfFolding (data, offset);
		}
		
		public static void UpLineStart (TextEditorData data)
		{
			Up (data);
			LineStart (data);
		}
		
		public static void DownLineEnd (TextEditorData data)
		{
			Down (data);
			LineEnd (data);
		}
	}
}
