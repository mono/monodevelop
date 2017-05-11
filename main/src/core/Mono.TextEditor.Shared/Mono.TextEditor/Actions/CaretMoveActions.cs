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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	static class CaretMoveActions
	{
		public static void Left (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				if (Platform.IsMac && data.IsSomethingSelected && !data.Caret.PreserveSelection) {
					data.Caret.Offset = System.Math.Min (data.SelectionAnchor, data.Caret.Offset);
					data.ClearSelection ();
					return;
				}
				
				if (data.Caret.Column > DocumentLocation.MinColumn) {
					DocumentLine line = data.Document.GetLine (data.Caret.Line);
					if (data.Caret.Column > line.Length + 1) {
						if (data.Caret.AllowCaretBehindLineEnd) {
							data.Caret.Column--;
						} else {
							data.Caret.Column = line.Length + 1;
						}
					} else {
						int offset = data.Caret.Offset - 1;
						bool foundFolding = false;
						foreach (var folding in data.Document.GetFoldingsFromOffset (offset).Where (f => f.IsCollapsed && f.Offset < offset)) {
							offset = System.Math.Min (offset, folding.Offset);
							foundFolding = true;
						}

						if (!foundFolding) {
							var layout = data.Parent?.TextViewMargin?.GetLayout (line);
							if (layout != null && data.Caret.Column < line.Length) {
								uint curIndex = 0, byteIndex = 0;
								int utf8ByteIndex = (int)layout.TranslateToUTF8Index ((uint)(offset - line.Offset), ref curIndex, ref byteIndex);
								layout.Layout.GetCursorPos (utf8ByteIndex, out var strong_pos, out var weak_pos);
								if (strong_pos.X != weak_pos.X) {
									offset--;
								}
							}
						}

						data.Caret.Offset = offset;
					}
				} else if (data.Caret.Line > DocumentLocation.MinLine) {
					DocumentLine prevLine = data.Document.GetLine (data.Caret.Line - 1);
					var nextLocation = new DocumentLocation (data.Caret.Line - 1, prevLine.Length + 1);
					if (data.HasIndentationTracker && data.Options.IndentStyle == IndentStyle.Virtual && nextLocation.Column == DocumentLocation.MinColumn)
						nextLocation = new DocumentLocation (data.Caret.Line - 1, data.GetVirtualIndentationColumn (nextLocation));
					data.Caret.Location = nextLocation;
				}
			}
		}
		
		public static void PreviousWord (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				data.Caret.Offset = data.FindPrevWordOffset (data.Caret.Offset);
			}
		}
		
		public static void PreviousSubword (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				data.Caret.Offset = data.FindPrevSubwordOffset (data.Caret.Offset);
			}
		}
		
		public static void Right (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				if (Platform.IsMac && data.IsSomethingSelected && !data.Caret.PreserveSelection) {
					data.Caret.Offset = System.Math.Max (data.SelectionAnchor, data.Caret.Offset);
					data.ClearSelection ();
					return;
				}
				
				DocumentLine line = data.Document.GetLine (data.Caret.Line);
				IEnumerable<FoldSegment > foldings = data.Document.GetStartFoldings (line);
				FoldSegment segment = null;
				foreach (FoldSegment folding in foldings) {
					if (folding.IsCollapsed && folding.Offset == data.Caret.Offset) {
						segment = folding;
						break;
					}
				}
				if (segment != null) {
					data.Caret.Offset = segment.EndOffset; 
					return;
				}

				if (data.Caret.Column >= line.Length + 1) {
					int nextColumn;
					if (data.HasIndentationTracker && data.Options.IndentStyle == IndentStyle.Virtual && data.Caret.Column == DocumentLocation.MinColumn) {
						nextColumn = data.GetVirtualIndentationColumn (data.Caret.Location);
					} else if (data.Caret.AllowCaretBehindLineEnd) {
						nextColumn = data.Caret.Column + 1;
					} else {
						nextColumn = line.Length + 1;
					}

					if (data.Caret.Column < nextColumn) {
						data.Caret.Column = nextColumn;
					} else {
						if (data.Caret.Line < data.LineCount)
							data.Caret.Location = new DocumentLocation (data.Caret.Line + 1, DocumentLocation.MinColumn);
					}
				} else {
					data.Caret.Column++;
					var layout = data.Parent?.TextViewMargin?.GetLayout (line);
					if (layout != null && data.Caret.Column < line.Length) {
						uint curIndex = 0, byteIndex = 0;
						int utf8ByteIndex = (int)layout.TranslateToUTF8Index ((uint)data.Caret.Column - 1, ref curIndex, ref byteIndex);
						layout.Layout.GetCursorPos (utf8ByteIndex, out var strong_pos, out var weak_pos);
						if (strong_pos.X != weak_pos.X) {
							data.Caret.Column++;
						}
					}
				}
			}
		}
		
		public static void NextWord (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				data.Caret.Offset = data.FindNextWordOffset (data.Caret.Offset);
			}
		}
		
		public static void NextSubword (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				data.Caret.Offset = data.FindNextSubwordOffset (data.Caret.Offset);
			}
		}
		
		public static void Up (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				int desiredColumn = data.Caret.DesiredColumn;

				//on Mac, when deselecting and moving up/down a line, column is always the column of the selection's start
				if (Platform.IsMac && data.IsSomethingSelected && !data.Caret.PreserveSelection) {
					int col = data.MainSelection.Anchor > data.MainSelection.Lead ? data.MainSelection.Lead.Column : data.MainSelection.Anchor.Column;
					int line = data.MainSelection.MinLine - 1;
					data.ClearSelection ();
					data.Caret.Location = (line >= DocumentLocation.MinLine) ? new DocumentLocation (line, col) : new DocumentLocation (DocumentLocation.MinLine, DocumentLocation.MinColumn);
					data.Caret.SetToDesiredColumn (desiredColumn);
					return;
				}

				if (data.Caret.Line > DocumentLocation.MinLine) {
					int visualLine = data.LogicalToVisualLine (data.Caret.Line);
					int line = data.VisualToLogicalLine (visualLine - 1);
					int offset = MoveCaretOutOfFolding (data, data.Document.LocationToOffset (line, data.Caret.Column), false);
					data.Caret.SetToOffsetWithDesiredColumn (offset);
				} else {
					ToDocumentStart (data);
				}
			}
		}

		static int MoveCaretOutOfFolding (TextEditorData data, int offset, bool moveToEnd = true)
		{
			IEnumerable<FoldSegment > foldings = data.Document.GetFoldingsFromOffset (offset);
			foreach (FoldSegment folding in foldings.Where (f => f.Offset < offset && offset < f.EndOffset)) {
				if (folding.IsCollapsed) {
					if (moveToEnd) {
						if (offset < folding.EndOffset)
							offset = folding.EndOffset;
					} else {
						if (offset > folding.Offset)
							offset = folding.Offset;
						
					}
				}
			}
			return offset;
		}

		public static bool IsFolded (TextEditorData data, int line, int column)
		{
			int offset = data.LocationToOffset (line, column);
			return data.Document.GetFoldingsFromOffset (offset).Any (f => f.isFolded && f.Offset < offset && offset < f.EndOffset);
		}
		
		public static void Down (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
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
					int nextLine = data.LogicalToVisualLine (data.Caret.Line) + 1;
					int line = data.VisualToLogicalLine (nextLine);
					int offset = MoveCaretOutOfFolding (data, data.LocationToOffset (line, data.Caret.Column), true);
					data.Caret.SetToOffsetWithDesiredColumn (offset);
				} else {
					ToDocumentEnd (data);
				}
			}
		}
		
		static int GetHomeMark (TextDocument document, DocumentLine line)
		{
			int result;
			for (result = 0; result < line.Length; result++)
				if (!Char.IsWhiteSpace (document.GetCharAt (line.Offset + result)))
					return result + 1;
			return result + 1;
		}
		
		internal static void InternalCaretMoveHome (TextEditorData data, bool firstNonWhitespace, bool hop)
		{
			using (var undo = data.OpenUndoGroup ()) {
				if (!data.Caret.PreserveSelection)
					data.ClearSelection ();

				DocumentLine line = data.Document.GetLine (data.Caret.Line);
				int newColumn;
				if (firstNonWhitespace) {

					int homeMark = GetHomeMark (data.Document, line);
					if (hop) {
						newColumn = data.Caret.Column == homeMark ? DocumentLocation.MinColumn : homeMark;
					} else {
						newColumn = homeMark;
					}
				} else {
					newColumn = DocumentLocation.MinColumn;
				}
				var newLocation = new DocumentLocation (data.Caret.Line, newColumn);
				// handle folding
				IEnumerable<FoldSegment> foldings = data.Document.GetEndFoldings (line);
				FoldSegment segment = null;
				foreach (FoldSegment folding in foldings) {
					if (folding.IsCollapsed && folding.Contains (data.Document.LocationToOffset (newLocation))) {
						segment = folding;
						break;
					}
				}
				if (segment != null)
					newLocation = data.Document.OffsetToLocation (segment.GetStartLine (data.Document).Offset); 

				if (newLocation != data.Caret.Location)
					data.Caret.Location = newLocation;
			}
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
			using (var undo = data.OpenUndoGroup ()) {
				if (!data.Caret.PreserveSelection)
					data.ClearSelection ();
				var line = data.Document.GetLine (data.Caret.Line);
				var newLocation = new DocumentLocation (data.Caret.Line, line.Length + 1);

				// handle folding
				IEnumerable<FoldSegment> foldings = data.Document.GetStartFoldings (line);
				FoldSegment segment = null;
				foreach (FoldSegment folding in foldings) {
					if (folding.IsCollapsed && folding.Contains (data.Document.LocationToOffset (newLocation))) {
						segment = folding;
						break;
					}
				}
				if (segment != null)
					newLocation = data.Document.OffsetToLocation (segment.EndOffset); 
				if (newLocation != data.Caret.Location)
					data.Caret.Location = newLocation;
			
				if (data.HasIndentationTracker && data.Options.IndentStyle == IndentStyle.Virtual) {
					int virtualIndentColumn = data.GetVirtualIndentationColumn (data.Caret.Location);
					if (virtualIndentColumn > data.Caret.Column)
						data.Caret.Column = virtualIndentColumn;
				}
			}
		}
		
		public static void ToDocumentStart (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				if (!data.Caret.PreserveSelection)
					data.ClearSelection ();
				data.Caret.Location = new DocumentLocation (DocumentLocation.MinLine, DocumentLocation.MinColumn);
			}
		}
		
		public static void ToDocumentEnd (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				if (!data.Caret.PreserveSelection)
					data.ClearSelection ();
				data.Caret.Offset = data.Document.Length;
			}
		}
				
		public static void PageUp (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				int pageLines = (int)((data.VAdjustment.PageSize + ((int)data.VAdjustment.Value % data.LineHeight)) / data.LineHeight);
				int visualLine = data.LogicalToVisualLine (data.Caret.Line);
				visualLine -= pageLines;
				int line = System.Math.Max (data.VisualToLogicalLine (visualLine), DocumentLocation.MinLine);
				int offset = data.LocationToOffset (line, data.Caret.Column);
				ScrollActions.PageUp (data);
				data.Caret.Offset = MoveCaretOutOfFolding (data, offset);
			}
		}
		
		public static void PageDown (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				int pageLines = (int)((data.VAdjustment.PageSize + ((int)data.VAdjustment.Value % data.LineHeight)) / data.LineHeight);
				int visualLine = data.LogicalToVisualLine (data.Caret.Line);
				visualLine += pageLines;
				int line = System.Math.Min (data.VisualToLogicalLine (visualLine), data.Document.LineCount);
				int offset = data.Document.LocationToOffset (line, data.Caret.Column);
				ScrollActions.PageDown (data);
				data.Caret.Offset = MoveCaretOutOfFolding (data, offset);
			}
		}
		
		public static void UpLineStart (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				Up (data);
				LineStart (data);
			}
		}
		
		public static void DownLineEnd (TextEditorData data)
		{
			using (var undo = data.OpenUndoGroup ()) {
				Down (data);
				LineEnd (data);
			}
		}
	}
}
