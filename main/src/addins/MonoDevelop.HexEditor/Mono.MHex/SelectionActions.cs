// 
// SelectionActions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using Mono.MHex.Data;

namespace Mono.MHex
{
	static class SelectionActions
	{
		public static Action<HexEditorData> FromMoveAction (Action<HexEditorData> moveAction)
		{
			return delegate (HexEditorData data) {
				StartSelection (data);
				moveAction (data);
				EndSelection (data);
			};
		}

		public static Action<HexEditorData> LineActionFromMoveAction (Action<HexEditorData> moveAction)
		{
			return delegate (HexEditorData data) {
				StartLineSelection (data);
				moveAction (data);
				EndLineSelection (data);
			};
		}
		
		public static void StartSelection (HexEditorData data)
		{
			data.Caret.PreserveSelection = true;
			if (!data.IsSomethingSelected) {
				data.MainSelection = new Selection (data.Caret.Offset, data.Caret.Offset);
			}
//			data.Caret.AutoScrollToCaret = false;
		}
		
		public static void EndSelection (HexEditorData data)
		{
			data.ExtendSelectionTo (data.Caret.Offset);
//			data.Caret.AutoScrollToCaret = true;
			data.Caret.PreserveSelection = false;
		}
		
		public static void Select (HexEditorData data, Action<HexEditorData> caretMoveAction)
		{
			PositionChangedHandler handler = new PositionChangedHandler (data);
			data.Caret.OffsetChanged += handler.DataCaretPositionChanged;
			
			StartSelection (data);
			caretMoveAction (data);
			data.Caret.OffsetChanged -= handler.DataCaretPositionChanged;
			data.Caret.AutoScrollToCaret = true;
			data.Caret.PreserveSelection = false;
		}

		class PositionChangedHandler
		{
			HexEditorData data;
			
			public PositionChangedHandler (HexEditorData data)
			{
				this.data = data;
			}
			
			public void DataCaretPositionChanged (object sender, CaretLocationEventArgs e)
			{
				data.ExtendSelectionTo (data.Caret.Offset);
			}
		}

		public static void StartLineSelection (HexEditorData data)
		{
			data.Caret.PreserveSelection = true;
			if (!data.IsSomethingSelected)
				data.MainSelection = new Selection (data.Caret.Offset, data.Caret.Offset);
		}

		public static void EndLineSelection (HexEditorData data)
		{
			data.ExtendSelectionTo (data.Caret.Line * data.BytesInRow + data.BytesInRow);
			data.Caret.PreserveSelection = false;
		}

		public static void SelectAll (HexEditorData data)
		{
			data.Caret.PreserveSelection = true;
			CaretMoveActions.ToDocumentEnd (data);
			data.MainSelection = new Selection (0, data.Length);
			data.Caret.PreserveSelection = false;
		}
		
		
		public static void MoveLeft (HexEditorData data)
		{
			Select (data, CaretMoveActions.Left);
		}
		
		public static void MoveRight (HexEditorData data)
		{
			Select (data, CaretMoveActions.Right);
		}
		
		public static void MoveUp (HexEditorData data)
		{
			Select (data, CaretMoveActions.Up);
		}
		
		public static void MoveDown (HexEditorData data)
		{
			Select (data, CaretMoveActions.Down);
		}
		
		public static void MoveLineHome (HexEditorData data)
		{
			Select (data, CaretMoveActions.LineHome);
		}
		
		public static void MoveLineEnd (HexEditorData data)
		{
			Select (data, CaretMoveActions.LineEnd);
		}
		
		public static void MoveToDocumentStart (HexEditorData data)
		{
			Select (data, CaretMoveActions.ToDocumentStart);
		}
		
		public static void MoveToDocumentEnd (HexEditorData data)
		{
			Select (data, CaretMoveActions.ToDocumentEnd);
		}
		
		public static void MovePageUp (HexEditorData data)
		{
			Select (data, CaretMoveActions.PageUp);
		}
		
		public static void MovePageDown (HexEditorData data)
		{
			Select (data, CaretMoveActions.PageDown);
		}
		
	}
}
