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
		Dictionary<int, EditAction> keyBindings = new Dictionary<int, EditAction> ();
		public Dictionary<int, EditAction> KeyBindings { get { return keyBindings; } }
		
		public SimpleEditMode ()
		{
			EditAction action;
			
			// ==== Left ====
			
			action = new CaretMoveLeft ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left), action);
			
			action = new SelectionMoveLeft ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask), action);
			
			action = new CaretMovePrevWord ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ControlMask), action);
			
			action = new SelectionMovePrevWord ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== Right ====
			
			action = new CaretMoveRight ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right), action);
			
			action = new SelectionMoveRight ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask), action);
			
			action = new CaretMoveNextWord ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ControlMask), action);
			
			action = new SelectionMoveNextWord ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== Up ====
			
			action = new CaretMoveUp ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up), action);
			
			action = new ScrollUpAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ControlMask), action);
			
			action = new SelectionMoveUp ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== Down ====
			
			action = new CaretMoveDown ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down), action);
			
			action = new ScrollDownAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ControlMask), action);
			
			action = new SelectionMoveDown ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// === Home ===
			
			action = new CaretMoveHome ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home), action);
			
			action = new SelectionMoveHome ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask), action);
			
			action = new CaretMoveToDocumentStart ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ControlMask), action);
			
			action = new SelectionMoveToDocumentStart ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== End ====
			
			action = new CaretMoveEnd ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End), action);
			
			action = new SelectionMoveEnd ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask), action);
			
			action = new CaretMoveToDocumentEnd ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ControlMask), action);
			
			action = new SelectionMoveToDocumentEnd ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			// ==== Deletion, insertion ====
			
			action = new SwitchCaretModeAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert), action);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Tab), new InsertTab ());
			keyBindings.Add (GetKeyCode (Gdk.Key.ISO_Left_Tab, Gdk.ModifierType.ShiftMask), new RemoveTab ());
			
			action = new InsertNewLine ();
			keyBindings.Add (GetKeyCode (Gdk.Key.Return), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Enter), action);
			
			action = new BackspaceAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ShiftMask), action);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ControlMask), new DeletePrevWord ());
			
			action = new DeleteAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete), action);
			
			action = new DeleteNextWord ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ControlMask), action);
			
			// ==== Cut, copy, paste ===
			
			action = new CutAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.x, Gdk.ModifierType.ControlMask), action);
			
			action = new CopyAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.c, Gdk.ModifierType.ControlMask), action);
			
			action = new PasteAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.v, Gdk.ModifierType.ControlMask), action);
			
			// ==== Page up/down ====
			
			action = new PageUpAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up), action);
			
			action = new SelectionPageUpAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up, Gdk.ModifierType.ShiftMask), action);
			
			action = new PageDownAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down), action);
			
			action = new SelectionPageDownAction ();
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down, Gdk.ModifierType.ShiftMask), action);
			
			// ==== Misc ====
			
			keyBindings.Add (GetKeyCode (Gdk.Key.a, Gdk.ModifierType.ControlMask), new SelectionSelectAll ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.d, Gdk.ModifierType.ControlMask), new DeleteCaretLine ());
			keyBindings.Add (GetKeyCode (Gdk.Key.D, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new DeleteCaretLineToEnd ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.ControlMask), new UndoAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask), new RedoAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.F2), new GotoNextBookmark ());
			keyBindings.Add (GetKeyCode (Gdk.Key.F2, Gdk.ModifierType.ShiftMask), new GotoPrevBookmark ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.b, Gdk.ModifierType.ControlMask), new GotoMatchingBracket ());
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
