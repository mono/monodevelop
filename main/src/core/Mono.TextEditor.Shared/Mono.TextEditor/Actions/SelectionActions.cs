// 
// SelectionActions.cs
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
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	static class SelectionActions
	{
		public static Action<TextEditorData> FromMoveAction (Action<TextEditorData> moveAction)
		{
			return delegate (TextEditorData data) {
				StartSelection (data);
				moveAction (data);
				EndSelection (data);
			};
		}

		public static Action<TextEditorData> LineActionFromMoveAction (Action<TextEditorData> moveAction)
		{
			return delegate (TextEditorData data) {
				StartLineSelection (data);
				moveAction (data);
				EndLineSelection (data);
			};
		}
		
		public static void StartSelection (TextEditorData data)
		{
			data.Caret.PreserveSelection = true;
			if (!data.IsSomethingSelected) {
				data.MainSelection = new MonoDevelop.Ide.Editor.Selection (data.Caret.Location, data.Caret.Location);
			}
			data.Caret.AutoScrollToCaret = false;
		}
		
		public static void EndSelection (TextEditorData data)
		{
			data.ExtendSelectionTo (data.Caret.Location);
			data.Caret.AutoScrollToCaret = true;
			data.Caret.PreserveSelection = false;
		}
		
		public static void Select (TextEditorData data, Action<TextEditorData> caretMoveAction)
		{
			data?.Parent?.CommitPreedit ();

			using (var undoGroup = data.OpenUndoGroup ()) {
				PositionChangedHandler handler = new PositionChangedHandler (data);
				data.Caret.PositionChanged += handler.DataCaretPositionChanged;

				StartSelection (data);
				caretMoveAction (data);
				data.Caret.PositionChanged -= handler.DataCaretPositionChanged;
				data.Caret.AutoScrollToCaret = true;
				data.Caret.PreserveSelection = false;
				data.ScrollToCaret ();
			}
		}

		class PositionChangedHandler
		{
			TextEditorData data;
			
			public PositionChangedHandler (TextEditorData data)
			{
				this.data = data;
			}
			
			public void DataCaretPositionChanged (object sender, DocumentLocationEventArgs e)
			{
				data.ExtendSelectionTo (data.Caret.Location);
			}
		}

		public static void StartLineSelection (TextEditorData data)
		{
			data.Caret.PreserveSelection = true;
			if (!data.IsSomethingSelected) {
				data.MainSelection = new MonoDevelop.Ide.Editor.Selection (new DocumentLocation (data.Caret.Line, DocumentLocation.MinColumn), new DocumentLocation (data.Caret.Line, DocumentLocation.MinColumn));
			}
		}

		public static void EndLineSelection (TextEditorData data)
		{
			int fromLine = data.MainSelection.Anchor.Line;
			int toLine = data.Caret.Line;
			var toSegment = data.Document.GetLine (toLine);
			
			//flip the anchor if pivoting around the origin line
			if (fromLine == toLine + 1) {
				if ((fromLine - data.MainSelection.Lead.Line) != 2) {
					var fromSegment = data.Document.GetLine (fromLine);
					data.SetSelection (fromSegment.EndOffsetIncludingDelimiter, toSegment.Offset);
				} else {
					data.SetSelection (toSegment.Offset, toSegment.EndOffsetIncludingDelimiter);
				}
			}
			//else just extend the selection else
			{
				int toOffset = (toLine < fromLine) ? toSegment.Offset : toSegment.EndOffsetIncludingDelimiter;
				data.ExtendSelectionTo (toOffset);
			}
			data.Caret.PreserveSelection = false;
		}

		public static void SelectAll (TextEditorData data)
		{
			data.Caret.PreserveSelection = true;
			data.Caret.AutoScrollToCaret = false;
			data.Caret.Offset = data.Length;
			data.Caret.AutoScrollToCaret = true;
			data.MainSelection = new MonoDevelop.Ide.Editor.Selection (new DocumentLocation (DocumentLocation.MinLine, DocumentLocation.MinColumn), data.OffsetToLocation (data.Length));
			data.Caret.PreserveSelection = false;
		}

		public static void MoveLeft (TextEditorData data)
		{
			Select (data, CaretMoveActions.Left);
		}
		
		public static void MovePreviousWord (TextEditorData data)
		{
			Select (data, CaretMoveActions.PreviousWord);
		}
		
		public static void MovePreviousSubword (TextEditorData data)
		{
			Select (data, CaretMoveActions.PreviousSubword);
		}
		
		public static void MoveRight (TextEditorData data)
		{
			Select (data, CaretMoveActions.Right);
		}
		
		public static void MoveNextWord (TextEditorData data)
		{
			Select (data, CaretMoveActions.NextWord);
		}
		
		public static void MoveNextSubword (TextEditorData data)
		{
			Select (data, CaretMoveActions.NextSubword);
		}
		
		public static void MoveUp (TextEditorData data)
		{
			Select (data, CaretMoveActions.Up);
		}
		
		public static void MoveDown (TextEditorData data)
		{
			Select (data, CaretMoveActions.Down);
		}
		
		public static void MoveLineHome (TextEditorData data)
		{
			Select (data, CaretMoveActions.LineHome);
		}
		
		public static void MoveLineEnd (TextEditorData data)
		{
			Select (data, CaretMoveActions.LineEnd);
		}
		
		public static void MoveToDocumentStart (TextEditorData data)
		{
			Select (data, CaretMoveActions.ToDocumentStart);
		}
		
		public static void MoveToDocumentEnd (TextEditorData data)
		{
			Select (data, CaretMoveActions.ToDocumentEnd);
		}
		
		public static void MovePageUp (TextEditorData data)
		{
			Select (data, CaretMoveActions.PageUp);
		}
		
		public static void MovePageDown (TextEditorData data)
		{
			Select (data, CaretMoveActions.PageDown);
		}
		
		public static void MoveUpLineStart (TextEditorData data)
		{
			Select (data, CaretMoveActions.UpLineStart);
		}
		
		public static void MoveDownLineEnd (TextEditorData data)
		{
			Select (data, CaretMoveActions.DownLineEnd);
		}

		public static void ExpandSelectionToLine (TextEditorData data)
		{
			using (var undoGroup = data.OpenUndoGroup ()) {
				var curLineSegment = data.GetLine (data.Caret.Line).SegmentIncludingDelimiter;
				var range = data.SelectionRange;
				var selection = TextSegment.FromBounds (
					               System.Math.Min (range.Offset, curLineSegment.Offset),
					               System.Math.Max (range.EndOffset, curLineSegment.EndOffset));
				data.Caret.PreserveSelection = true;
				data.Caret.Offset = selection.EndOffset;
				data.Caret.PreserveSelection = false;
				data.SelectionRange = selection;
			}
		}

		public static void ClearSelection (TextEditorData data)
		{
			data.ClearSelection ();
		}
	}
}
