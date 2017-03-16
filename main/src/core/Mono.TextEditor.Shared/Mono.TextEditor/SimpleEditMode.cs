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
using Gdk;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace Mono.TextEditor
{
	class SimpleEditMode : EditMode
	{
		Dictionary<int, Action<TextEditorData>> keyBindings = new Dictionary<int, Action<TextEditorData>> ();
		public Dictionary<int, Action<TextEditorData>> KeyBindings { get { return keyBindings; } }
		
		public SimpleEditMode ()
		{
			if (Platform.IsMac)
				InitMacBindings ();
			else
				InitDefaultBindings ();
		}
		
		void InitCommonBindings ()
		{
			Action<TextEditorData> action;
			
			Gdk.ModifierType wordModifier = Platform.IsMac? Gdk.ModifierType.Mod1Mask : Gdk.ModifierType.ControlMask;
			Gdk.ModifierType subwordModifier = Platform.IsMac? Gdk.ModifierType.ControlMask : Gdk.ModifierType.Mod1Mask;
						
			// ==== Left ====
			
			action = CaretMoveActions.Left;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left), action);
			
			action = SelectionActions.MoveLeft;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask), action);
			
			action = CaretMoveActions.PreviousWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, wordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, wordModifier), action);
			
			action = SelectionActions.MovePreviousWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask | wordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask | wordModifier), action);
			
			// ==== Right ====
			
			action = CaretMoveActions.Right;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right), action);
			
			action = SelectionActions.MoveRight;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask), action);
			
			action = CaretMoveActions.NextWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, wordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, wordModifier), action);
			
			action = SelectionActions.MoveNextWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask | wordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask | wordModifier), action);
			
			// ==== Up ====
			
			action = CaretMoveActions.Up;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up), action);
			
			action = ScrollActions.Up;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ControlMask), action);
			
			// ==== Down ====
			
			action = CaretMoveActions.Down;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down), action);
			
			action = ScrollActions.Down;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ControlMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ControlMask), action);
			
			// ==== Deletion, insertion ====
			
			action = MiscActions.SwitchCaretMode;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert), action);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Tab), MiscActions.InsertTab);
			keyBindings.Add (GetKeyCode (Gdk.Key.Tab, Gdk.ModifierType.ShiftMask), MiscActions.RemoveTab);
			
			action = MiscActions.InsertNewLine;
			keyBindings.Add (GetKeyCode (Gdk.Key.Return), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Enter), action);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Return, Gdk.ModifierType.ControlMask), MiscActions.InsertNewLinePreserveCaretPosition);
			keyBindings.Add (GetKeyCode (Gdk.Key.Return, Gdk.ModifierType.ShiftMask), MiscActions.InsertNewLineAtEnd);
			
			action = DeleteActions.Backspace;
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ShiftMask), action);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, wordModifier), DeleteActions.PreviousWord);
			
			action = DeleteActions.Delete;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete), action);
			
			action = DeleteActions.NextWord;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete, wordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, wordModifier), action);
			
			
			// == subword motions ==
			action = CaretMoveActions.PreviousSubword;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, subwordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, subwordModifier), action);
			
			action = SelectionActions.MovePreviousSubword;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask | subwordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask | subwordModifier), action);
			
			action = CaretMoveActions.NextSubword;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, subwordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, subwordModifier), action);
			
			action = SelectionActions.MoveNextSubword;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask | subwordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask | subwordModifier), action);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, subwordModifier), DeleteActions.PreviousSubword);

			action = DeleteActions.NextSubword;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete, subwordModifier), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, subwordModifier), action);
		}
		
		void InitDefaultBindings ()
		{
			InitCommonBindings ();
			
			Action<TextEditorData> action;
			
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
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Escape), SelectionActions.ClearSelection);

			//Non-mac selection actions
			
			action = SelectionActions.MoveDown;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
			
			action = SelectionActions.MoveUp;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), action);
		}
		
		void InitMacBindings ()
		{
			InitCommonBindings ();
			
			Action<TextEditorData> action;
			
			// Up/down
			
			action = CaretMoveActions.UpLineStart;
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.Mod1Mask), action);
			
			action = CaretMoveActions.DownLineEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.Mod1Mask), action);
			
			action = SelectionActions.MoveUpLineStart;
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.ShiftMask), action);
			
			action = SelectionActions.MoveDownLineEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.ShiftMask), action);
				
			// === Home ===
			
			action = CaretMoveActions.LineHome;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.MetaMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.a, Gdk.ModifierType.ControlMask), action); //emacs
			keyBindings.Add (GetKeyCode (Gdk.Key.a, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask), SelectionActions.MoveLineHome);

			action = SelectionActions.MoveLineHome;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.MetaMask | Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.MetaMask | Gdk.ModifierType.ShiftMask), action);

			action = CaretMoveActions.ToDocumentStart;
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.MetaMask), action);

			action = SelectionActions.MoveToDocumentStart;
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.MetaMask | Gdk.ModifierType.ShiftMask), action);

			// ==== End ====
			
			action = CaretMoveActions.LineEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.MetaMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.e, Gdk.ModifierType.ControlMask), action); //emacs
			keyBindings.Add (GetKeyCode (Gdk.Key.e, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask), SelectionActions.MoveLineEnd);
			
			
			action = SelectionActions.MoveLineEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.MetaMask | Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.MetaMask | Gdk.ModifierType.ShiftMask), action);

			action = CaretMoveActions.ToDocumentEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.MetaMask), action);

			action = SelectionActions.MoveToDocumentEnd;
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.MetaMask | Gdk.ModifierType.ShiftMask), action);

			// ==== Cut, copy, paste ===
			
			action = ClipboardActions.Cut;
			keyBindings.Add (GetKeyCode (Gdk.Key.x, Gdk.ModifierType.MetaMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.w, Gdk.ModifierType.ControlMask), action); //emacs
			
			action = ClipboardActions.Copy;
			keyBindings.Add (GetKeyCode (Gdk.Key.c, Gdk.ModifierType.MetaMask), action);
			
			action = ClipboardActions.Paste;
			keyBindings.Add (GetKeyCode (Gdk.Key.v, Gdk.ModifierType.MetaMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.y, Gdk.ModifierType.ControlMask), action); //emacs
			
			// ==== Page up/down ====
			
			action = ScrollActions.PageDown;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down), action);
			
			action = ScrollActions.PageUp;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up), action);
			
			action = CaretMoveActions.PageDown;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down, Gdk.ModifierType.Mod1Mask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down, Gdk.ModifierType.Mod1Mask), action);
			
			action = CaretMoveActions.PageUp;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up, Gdk.ModifierType.Mod1Mask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up, Gdk.ModifierType.Mod1Mask), action);
			
			action = SelectionActions.MovePageUp;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up, Gdk.ModifierType.ShiftMask), action);
			
			action = SelectionActions.MovePageDown;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down, Gdk.ModifierType.ShiftMask), action);
			
			// ==== Misc ====
			
			keyBindings.Add (GetKeyCode (Gdk.Key.a, Gdk.ModifierType.MetaMask), SelectionActions.SelectAll);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.MetaMask), MiscActions.Undo);
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.MetaMask | Gdk.ModifierType.ShiftMask), MiscActions.Redo);

			keyBindings.Add (GetKeyCode (Key.BackSpace, ModifierType.Mod1Mask | ModifierType.ShiftMask), DeleteActions.PreviousSubword);
			keyBindings.Add (GetKeyCode (Key.Delete, ModifierType.Mod1Mask | ModifierType.ShiftMask), DeleteActions.NextSubword);

			// selection actions
			
			action = SelectionActions.MoveDown;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask), action);
			
			action = SelectionActions.MoveUp;
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ShiftMask), action);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask), action);
			
			// extra emacs stuff
			keyBindings.Add (GetKeyCode (Gdk.Key.f, Gdk.ModifierType.ControlMask), CaretMoveActions.Right);
			keyBindings.Add (GetKeyCode (Gdk.Key.b, Gdk.ModifierType.ControlMask), CaretMoveActions.Left);
			keyBindings.Add (GetKeyCode (Gdk.Key.p, Gdk.ModifierType.ControlMask), CaretMoveActions.Up);
			keyBindings.Add (GetKeyCode (Gdk.Key.p, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask), SelectionActions.MoveUp);
			keyBindings.Add (GetKeyCode (Gdk.Key.n, Gdk.ModifierType.ControlMask), CaretMoveActions.Down);
			keyBindings.Add (GetKeyCode (Gdk.Key.n, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask), SelectionActions.MoveDown);
			keyBindings.Add (GetKeyCode (Gdk.Key.h, Gdk.ModifierType.ControlMask), DeleteActions.Backspace);
			keyBindings.Add (GetKeyCode (Gdk.Key.d, Gdk.ModifierType.ControlMask), DeleteActions.Delete);
			keyBindings.Add (GetKeyCode (Gdk.Key.o, Gdk.ModifierType.ControlMask), MiscActions.InsertNewLinePreserveCaretPosition);
		}
		
		public void AddBinding (Gdk.Key key, Action<TextEditorData> action, bool force = false)
		{
			var code = GetKeyCode (key);
			if (force) {
				if (keyBindings.ContainsKey (code)) {
					keyBindings [code] = action;
					return;
				}
			}
			keyBindings.Add (code, action);
		}

		public override void SelectValidShortcut (KeyboardShortcut[] accels, out Gdk.Key key, out ModifierType mod)
		{
			foreach (var accel in accels) {
				int keyCode = GetKeyCode (accel.Key, accel.Modifier);
				if (keyBindings.ContainsKey (keyCode)) {
					key = accel.Key;
					mod = accel.Modifier;
					return;
				}
			}
			key = accels [0].Key;
			mod = accels [0].Modifier;
		}

		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			int keyCode = GetKeyCode (key, modifier);
			if (keyBindings.ContainsKey (keyCode)) {
				RunAction (keyBindings [keyCode]);
			} else if (unicodeKey != 0 && modifier == Gdk.ModifierType.None) {
				InsertCharacter (unicodeKey);
			}
		}
	}
}
