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

namespace Mono.TextEditor
{
	
	public class SelectionActions
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
				data.MainSelection = new Selection (data.Caret.Location, data.Caret.Location);
			}
		}
		
		public static void EndSelection (TextEditorData data)
		{
			data.ExtendSelectionTo (data.Caret.Offset);
			data.Caret.PreserveSelection = false;
		}
		
		public static void Select (TextEditorData data, Action<TextEditorData> caretMoveAction)
		{
			StartSelection (data);
			caretMoveAction (data);
			EndSelection (data);
		}

		public static void StartLineSelection (TextEditorData data)
		{
			StartSelection (data);
		}

		public static void EndLineSelection (TextEditorData data)
		{
			if (null != data.SelectionRange) {
				if (data.Caret.Location < data.MainSelection.Anchor) {
					data.SetSelectLines (data.Caret.Line, data.MainSelection.Anchor.Line);
				} else {
					data.SetSelectLines (data.MainSelection.Anchor.Line, data.Caret.Line);
				}
				
			} else {
				data.SetSelectLines (data.Caret.Line, data.Caret.Line);
			}
			data.Caret.PreserveSelection = false;
		}

		public static void SelectAll (TextEditorData data)
		{
			data.Caret.AutoScrollToCaret = false;
			data.Caret.PreserveSelection = true;
			CaretMoveActions.ToDocumentEnd (data);
			data.MainSelection = new Selection (new DocumentLocation (0, 0),
			                                    data.LogicalToVisualLocation (data.Caret.Location));
			data.Caret.PreserveSelection = false;
			data.Caret.AutoScrollToCaret = true;
		}
		
		
		public static void MoveLeft (TextEditorData data)
		{
			Select (data, CaretMoveActions.Left);
		}
		
		public static void MovePreviousWord (TextEditorData data)
		{
			Select (data, CaretMoveActions.PreviousWord);
		}
		
		public static void MoveRight (TextEditorData data)
		{
			Select (data, CaretMoveActions.Right);
		}
		
		public static void MoveNextWord (TextEditorData data)
		{
			Select (data, CaretMoveActions.NextWord);
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
	}
}
