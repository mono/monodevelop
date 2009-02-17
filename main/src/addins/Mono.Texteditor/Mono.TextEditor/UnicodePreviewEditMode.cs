// 
// UnicodePreviewEditMode.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public class UnicodePreviewEditMode : EditMode
	{
		Dictionary<int, Action<TextEditorData>> keyBindings = new Dictionary<int, Action<TextEditorData>> ();
		
		TextEditorData editor;
		EditMode oldEditMode;
		int initialOffset;
		int currentOffset;
		bool inInsertOperation = false;
		UnderlineMarker underlineMarker;
		LineSegment curLine;
		
		public UnicodePreviewEditMode ()
		{
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace), DeleteActions.Backspace);
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ShiftMask), DeleteActions.Backspace);
		}
		
		public void AttachUnicodeEditMode (TextEditorData editor)
		{
			this.editor = editor;
			oldEditMode = editor.CurrentMode;
			editor.CurrentMode = this;
			initialOffset = editor.Caret.Offset;
			curLine = editor.Document.GetLine (editor.Caret.Line);
			string style = "text";
			Chunk chunk = editor.Document.SyntaxMode.GetChunks (editor.Document, editor.ColorStyle, curLine, curLine.Offset, curLine.Length);
			while (chunk != null && chunk.Next != null) {
				if (chunk.Contains (initialOffset))
					break;
				chunk = chunk.Next;
			}
			if (chunk != null)
				style = chunk.Style;
			underlineMarker = new UnderlineMarker (style, editor.Caret.Column, editor.Caret.Column + 1);
			underlineMarker.Wave = false;
			
			curLine.AddMarker (underlineMarker);
			editor.Document.BeginAtomicUndo ();
			editor.InsertAtCaret ("u");
			currentOffset = editor.Caret.Offset;
			editor.Caret.PositionChanged += HandlePositionChanged; 
		}

		void HandlePositionChanged(object sender, DocumentLocationEventArgs e)
		{
			if (!inInsertOperation || editor.Caret.Offset == initialOffset)
				CancelUnicodeMode ();
		}
		
		uint GetCurrentUnicodeKey ()
		{
			if (editor.Caret.Offset - (initialOffset + 1) <= 0) 
				return 0;
			string text = editor.Document.GetTextBetween (initialOffset + 1, editor.Caret.Offset);
			
			return uint.Parse (text, System.Globalization.NumberStyles.HexNumber);
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.Escape) {
				CancelUnicodeMode ();
				return;
			}
			if (key == Gdk.Key.Return || key == Gdk.Key.space) {
				uint keyCode = GetCurrentUnicodeKey ();
				editor.Document.BeginAtomicUndo ();
				CancelUnicodeMode ();
				if (keyCode > 0) 
					editor.InsertAtCaret (((char)keyCode).ToString ());
				editor.Document.EndAtomicUndo ();
				
				return;
			}
			inInsertOperation = true;
			try {
				int keyCode = GetKeyCode (key, modifier);
				if (keyBindings.ContainsKey (keyCode)) {
					currentOffset = editor.Caret.Offset - 1; // only backspace is used
					RunAction (keyBindings [keyCode]);
				} else if (unicodeKey != 0 && editor.Caret.Offset - initialOffset  < 8) {
					char ch = Char.ToUpper ((char)unicodeKey);
					if (Char.IsDigit (ch) || ('A' <= ch && ch <= 'F')) {
						InsertCharacter (unicodeKey);
						currentOffset = editor.Caret.Offset;
					}
				}
				underlineMarker.EndCol = Caret.Column;
			} finally {
				inInsertOperation = false;
			}
		}
		
		void CancelUnicodeMode ()
		{
			editor.CurrentMode = oldEditMode;
		}
		
		public override void RemovedFromTextEditor ()
		{
			bool resetCaret = editor.Caret.Offset == currentOffset;
			editor.Caret.PositionChanged -= HandlePositionChanged; 
			curLine.RemoveMarker (underlineMarker);
			editor.Document.CommitLineUpdate (curLine);
			System.Console.WriteLine(currentOffset - initialOffset);
			editor.Remove (initialOffset, currentOffset - initialOffset);
			if (resetCaret)
				editor.Caret.Offset = initialOffset;
			editor.Document.EndAtomicUndo ();
		}
	}
}
