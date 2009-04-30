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
			IEnumerable<FoldSegment> foldings = data.Document.GetEndFoldings (line);
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
			IEnumerable<FoldSegment> foldings = data.Document.GetStartFoldings (line);
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
			if (data.Caret.Column < line.EditableLength || data.Caret.AllowCaretBehindLineEnd) {
				if (data.Caret.Column >= line.EditableLength) {
					int nextColumn = data.GetNextVirtualColumn (data.Caret.Line, data.Caret.Column);
					if (data.Caret.Column != nextColumn) {
						data.Caret.Column = nextColumn;
					} else {
						data.Caret.Location = new DocumentLocation (data.Caret.Line + 1, 0);
						data.Caret.CheckCaretPosition ();
					}
				} else {
					data.Caret.Column++;
				}
			} else if (data.Caret.Line + 1 < data.Document.LineCount) {
				data.Caret.Location = new DocumentLocation (data.Caret.Line + 1, 0);
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
			if (data.Caret.Line > 0) {
				data.Caret.Line = data.Document.VisualToLogicalLine (data.Document.LogicalToVisualLine (data.Caret.Line) - 1);
			}
		}
		
		public static void Down (TextEditorData data)
		{
			if (data.Caret.Line < data.Document.LineCount - 1) {
				data.Caret.Line = data.Document.VisualToLogicalLine (data.Document.LogicalToVisualLine (data.Caret.Line) + 1);
			}
		}
		
		static int GetHomeMark (Document document, LineSegment line)
		{
			int result;
			for (result = 0; result < line.EditableLength; result++)
				if (!Char.IsWhiteSpace (document.GetCharAt (line.Offset + result)))
					return result;
			return result;
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
					newLocation.Column = data.Caret.Column == homeMark ? 0 : homeMark;
				} else {
					newLocation.Column = homeMark;
				}
			} else {
				newLocation.Column = 0;
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
			newLocation.Column = line.EditableLength;
			
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
			if (newLocation != data.Caret.Location) {
				data.Caret.Location = newLocation;
			} else if (data.Caret.AllowCaretBehindLineEnd) {
				int nextColumn = data.GetNextVirtualColumn (data.Caret.Line, data.Caret.Column);
				if (nextColumn != data.Caret.Column)
					data.Caret.Column = nextColumn;
			}
			
		}
		
		public static void ToDocumentStart (TextEditorData data)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			data.Caret.Location = new DocumentLocation (0, 0);
		}
		
		public static void ToDocumentEnd (TextEditorData data)
		{
			if (!data.Caret.PreserveSelection)
				data.ClearSelection ();
			data.Caret.Offset = data.Document.Length;
		}
		
		public static int LineHeight { get { return 16; } }
		
		public static void PageUp (TextEditorData data)
		{
			int pageIncrement =  LineHeight * ((int)(data.VAdjustment.PageIncrement / LineHeight) - 1);
			data.VAdjustment.Value = System.Math.Max (data.VAdjustment.Lower, data.VAdjustment.Value - pageIncrement); 
			
			int visualLine = data.Document.LogicalToVisualLine (data.Caret.Line);
			visualLine -= pageIncrement / LineHeight;
			int line = System.Math.Max (data.Document.VisualToLogicalLine (visualLine), 0);
			data.Caret.Line = line;
		}
		
		public static void PageDown (TextEditorData data)
		{
			int pageIncrement =  LineHeight * ((int)(data.VAdjustment.PageIncrement / LineHeight) - 1);
			if (data.VAdjustment.Value < data.VAdjustment.Upper - data.VAdjustment.PageIncrement)
				data.VAdjustment.Value = data.VAdjustment.Value + pageIncrement;
			
			int visualLine = data.Document.LogicalToVisualLine (data.Caret.Line);
			visualLine += pageIncrement / LineHeight;
			int line = System.Math.Min (data.Document.VisualToLogicalLine (visualLine), data.Document.LineCount - 1);
			data.Caret.Line = line;
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
