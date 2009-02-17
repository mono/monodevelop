//
// SimpleEditMode.cs
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
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public class SimpleEditMode : EditMode
	{
		Dictionary<int, Action<TextEditorData>> keyBindings = new Dictionary<int, Action<TextEditorData>> ();
		public Dictionary<int, Action<TextEditorData>> KeyBindings { get { return keyBindings; } }
		
		public SimpleEditMode ()
		{
			Action<TextEditorData> action;
			
			// ==== Left ====
			
			action = CaretMoveActions.Left;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left), action);
			
			action = SelectionActions.MoveLeft;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask), action);
			
			action = CaretMoveActions.PreviousWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ControlMask), action);
			
			action = SelectionActions.MovePreviousWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== Right ====
			
			action = CaretMoveActions.Right;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right), action);
			
			action = SelectionActions.MoveRight;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask), action);
			
			action = CaretMoveActions.NextWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ControlMask), action);
			
			action = SelectionActions.MoveNextWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== Up ====
			
			action = CaretMoveActions.Up;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up), action);
			
			action = ScrollActions.Up;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ControlMask), action);
			
			action = SelectionActions.MoveUp;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== Down ====
			
			action = CaretMoveActions.Down;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down), action);
			
			action = ScrollActions.Down;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ControlMask), action);
			
			action = SelectionActions.MoveDown;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// === Home ===
			
			action = CaretMoveActions.LineHome;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home), action);
			
			action = SelectionActions.MoveLineHome;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask), action);
			
			action = CaretMoveActions.ToDocumentStart;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ControlMask), action);
			
			action = SelectionActions.MoveToDocumentStart;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== End ====
			
			action = CaretMoveActions.LineEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End), action);
			
			action = SelectionActions.MoveLineEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask), action);
			
			action = CaretMoveActions.ToDocumentEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ControlMask), action);
			
			action = SelectionActions.MoveToDocumentEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== Deletion, insertion ====
			
			action = MiscActions.SwitchCaretMode;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert), action);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Tab), MiscActions.InsertTab);
			keyBindings.Add (GetKeyCode (Gdk.Key.ISO_Left_Tab, Gdk.ModifierType.ShiftMask), MiscActions.RemoveTab);
			
			action = MiscActions.InsertNewLine;
			keyBindings.Add (GetKeyCode (Gdk.Key.Return), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Enter), action);
			
			action = DeleteActions.Backspace;
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ShiftMask), action);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ControlMask), DeleteActions.PreviousWord);
			
			action = DeleteActions.Delete;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete), action);
			
			action = DeleteActions.NextWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ControlMask), action);
			
			// ==== Cut, copy, paste ===
			
			action = ClipboardActions.Cut;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.x, Gdk.ModifierType.ControlMask), action);
			
			action = ClipboardActions.Copy;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.c, Gdk.ModifierType.ControlMask), action);
			
			action = ClipboardActions.Paste;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.v, Gdk.ModifierType.ControlMask), action);
			
			// ==== Page up/down ====
			
			action = CaretMoveActions.PageUp;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up), action);
			
			action = SelectionActions.MovePageUp;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up, Gdk.ModifierType.ShiftMask), action);
			
			action = CaretMoveActions.PageDown;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down), action);
			
			action = SelectionActions.MovePageDown;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down, Gdk.ModifierType.ShiftMask), action);
			
			// ==== Misc ====
			
			keyBindings.Add (GetKeyCode (Gdk.Key.a, Gdk.ModifierType.ControlMask), SelectionActions.SelectAll);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.d, Gdk.ModifierType.ControlMask), DeleteActions.CaretLine);
			keyBindings.Add (GetKeyCode (Gdk.Key.D, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), DeleteActions.CaretLineToEnd);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.ControlMask), MiscActions.Undo);
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask), MiscActions.Redo);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.F2), BookmarkActions.GotoNext);
			keyBindings.Add (GetKeyCode (Gdk.Key.F2, Gdk.ModifierType.ShiftMask), BookmarkActions.GotoPrevious);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.b, Gdk.ModifierType.ControlMask), MiscActions.GotoMatchingBracket);
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			int keyCode = GetKeyCode (key, modifier);
			
			if (keyBindings.ContainsKey (keyCode)) {
				RunAction (keyBindings [keyCode]);
			} else if (unicodeKey != 0) {
				InsertCharacter (unicodeKey);
			}
		}

	}
}
