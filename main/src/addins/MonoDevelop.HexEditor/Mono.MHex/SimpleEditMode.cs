// 
// SimpleEditMode.cs
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
using System.Collections.Generic;
using Xwt;

namespace Mono.MHex
{
	class SimpleEditMode : EditMode
	{
		Dictionary<int, Action<HexEditorData>> keyBindings = new Dictionary<int, Action<HexEditorData>> ();
		
		
		public SimpleEditMode ()
		{
			InitCommonBindings ();
			InitDefaultBindings ();
			
		}
		
		void InitCommonBindings ()
		{
			// ==== Left ====
			
			keyBindings.Add (GetKeyCode (Key.NumPadLeft), CaretMoveActions.Left);
			keyBindings.Add (GetKeyCode (Key.Left), CaretMoveActions.Left);
			keyBindings.Add (GetKeyCode (Key.NumPadLeft, ModifierKeys.Control), CaretMoveActions.Left);
			keyBindings.Add (GetKeyCode (Key.Left, ModifierKeys.Control), CaretMoveActions.Left);
			
			
			keyBindings.Add (GetKeyCode (Key.NumPadLeft, ModifierKeys.Shift), SelectionActions.MoveLeft);
			keyBindings.Add (GetKeyCode (Key.Left, ModifierKeys.Shift), SelectionActions.MoveLeft);
			
			// ==== Right ====
			
			keyBindings.Add (GetKeyCode (Key.NumPadRight), CaretMoveActions.Right);
			keyBindings.Add (GetKeyCode (Key.Right), CaretMoveActions.Right);
			keyBindings.Add (GetKeyCode (Key.NumPadRight, ModifierKeys.Control), CaretMoveActions.Right);
			keyBindings.Add (GetKeyCode (Key.Right, ModifierKeys.Control), CaretMoveActions.Right);
			
			keyBindings.Add (GetKeyCode (Key.NumPadRight, ModifierKeys.Shift), SelectionActions.MoveRight);
			keyBindings.Add (GetKeyCode (Key.Right, ModifierKeys.Shift), SelectionActions.MoveRight);
			
			// ==== Up ====
			
			keyBindings.Add (GetKeyCode (Key.NumPadUp), CaretMoveActions.Up);
			keyBindings.Add (GetKeyCode (Key.Up), CaretMoveActions.Up);
			
			keyBindings.Add (GetKeyCode (Key.NumPadUp, ModifierKeys.Control), ScrollActions.Up);
			keyBindings.Add (GetKeyCode (Key.Up, ModifierKeys.Control), ScrollActions.Up);
			
			keyBindings.Add (GetKeyCode (Key.NumPadUp, ModifierKeys.Shift), SelectionActions.MoveUp);
			keyBindings.Add (GetKeyCode (Key.Up, ModifierKeys.Shift), SelectionActions.MoveUp);
			keyBindings.Add (GetKeyCode (Key.Up, ModifierKeys.Shift | ModifierKeys.Control), SelectionActions.MoveUp);
			
			// ==== Down ====
			
			keyBindings.Add (GetKeyCode (Key.NumPadDown), CaretMoveActions.Down);
			keyBindings.Add (GetKeyCode (Key.Down), CaretMoveActions.Down);
			
			keyBindings.Add (GetKeyCode (Key.NumPadDown, ModifierKeys.Control), ScrollActions.Down);
			keyBindings.Add (GetKeyCode (Key.Down, ModifierKeys.Control), ScrollActions.Down);
			
			keyBindings.Add (GetKeyCode (Key.NumPadDown, ModifierKeys.Shift), SelectionActions.MoveDown);
			keyBindings.Add (GetKeyCode (Key.Down, ModifierKeys.Shift), SelectionActions.MoveDown);
			keyBindings.Add (GetKeyCode (Key.Down, ModifierKeys.Shift | ModifierKeys.Control), SelectionActions.MoveDown);
			
			// ==== Deletion, insertion ====
		
			keyBindings.Add (GetKeyCode (Key.NumPadInsert), MiscActions.SwitchCaretMode);
			keyBindings.Add (GetKeyCode (Key.Insert), MiscActions.SwitchCaretMode);
			
			keyBindings.Add (GetKeyCode (Key.Tab), CaretMoveActions.SwitchSide);
			
			keyBindings.Add (GetKeyCode (Key.BackSpace), DeleteActions.Backspace);
			keyBindings.Add (GetKeyCode (Key.BackSpace, ModifierKeys.Shift), DeleteActions.Backspace);
			
			keyBindings.Add (GetKeyCode (Key.NumPadDelete), DeleteActions.Delete);
			keyBindings.Add (GetKeyCode (Key.Delete), DeleteActions.Delete);
		}
		
		void InitDefaultBindings ()
		{
			// === Home ===
			
			keyBindings.Add (GetKeyCode (Key.NumPadHome), CaretMoveActions.LineHome);
			keyBindings.Add (GetKeyCode (Key.Home), CaretMoveActions.LineHome);
			
			keyBindings.Add (GetKeyCode (Key.NumPadHome, ModifierKeys.Shift), SelectionActions.MoveLineHome);
			keyBindings.Add (GetKeyCode (Key.Home, ModifierKeys.Shift), SelectionActions.MoveLineHome);

			keyBindings.Add (GetKeyCode (Key.NumPadHome, ModifierKeys.Control), CaretMoveActions.ToDocumentStart);
			keyBindings.Add (GetKeyCode (Key.Home, ModifierKeys.Control), CaretMoveActions.ToDocumentStart);
			
			keyBindings.Add (GetKeyCode (Key.NumPadHome, ModifierKeys.Shift | ModifierKeys.Control), SelectionActions.MoveToDocumentStart);
			keyBindings.Add (GetKeyCode (Key.Home, ModifierKeys.Shift | ModifierKeys.Control), SelectionActions.MoveToDocumentStart);
			
			// ==== End ====
			
			keyBindings.Add (GetKeyCode (Key.NumPadEnd), CaretMoveActions.LineEnd);
			keyBindings.Add (GetKeyCode (Key.End), CaretMoveActions.LineEnd);
			
			keyBindings.Add (GetKeyCode (Key.NumPadEnd, ModifierKeys.Shift), SelectionActions.MoveLineEnd);
			keyBindings.Add (GetKeyCode (Key.End, ModifierKeys.Shift), SelectionActions.MoveLineEnd);
			
			keyBindings.Add (GetKeyCode (Key.NumPadEnd, ModifierKeys.Control), CaretMoveActions.ToDocumentEnd);
			keyBindings.Add (GetKeyCode (Key.End, ModifierKeys.Control), CaretMoveActions.ToDocumentEnd);
			
			keyBindings.Add (GetKeyCode (Key.NumPadEnd, ModifierKeys.Shift | ModifierKeys.Control), SelectionActions.MoveToDocumentEnd);
			keyBindings.Add (GetKeyCode (Key.End, ModifierKeys.Shift | ModifierKeys.Control), SelectionActions.MoveToDocumentEnd);
			
			// ==== Cut, copy, paste ===
			/*
			action = ClipboardActions.Cut;
			keyBindings.Add (GetKeyCode (Key.NumPadDelete, ModifierKeys.Shift), action);
			keyBindings.Add (GetKeyCode (Key.Delete, ModifierKeys.Shift), action);
			keyBindings.Add (GetKeyCode (Key.x, ModifierKeys.Control), action);
			
			action = ClipboardActions.Copy;
			keyBindings.Add (GetKeyCode (Key.NumPadInsert, ModifierKeys.Control), action);
			keyBindings.Add (GetKeyCode (Key.Insert, ModifierKeys.Control), action);
			keyBindings.Add (GetKeyCode (Key.c, ModifierKeys.Control), action);
			
			action = ClipboardActions.Paste;
			keyBindings.Add (GetKeyCode (Key.NumPadInsert, ModifierKeys.Shift), action);
			keyBindings.Add (GetKeyCode (Key.Insert, ModifierKeys.Shift), action);
			keyBindings.Add (GetKeyCode (Key.v, ModifierKeys.Control), action);
			*/
			// ==== Page up/down ====
			
//			keyBindings.Add (GetKeyCode (Key.NumPadPageUp), CaretMoveActions.PageUp);
			keyBindings.Add (GetKeyCode (Key.PageUp), CaretMoveActions.PageUp);
			
//			keyBindings.Add (GetKeyCode (Key.NumPadPage_Up, ModifierKeys.Shift), SelectionActions.MovePageUp);
			keyBindings.Add (GetKeyCode (Key.PageUp, ModifierKeys.Shift), SelectionActions.MovePageUp);
			
//			keyBindings.Add (GetKeyCode (Key.NumPadPage_Down), CaretMoveActions.PageDown);
			keyBindings.Add (GetKeyCode (Key.PageDown), CaretMoveActions.PageDown);
			
//			keyBindings.Add (GetKeyCode (Key.NumPadPage_Down, ModifierKeys.Shift), SelectionActions.MovePageDown);
			keyBindings.Add (GetKeyCode (Key.PageDown, ModifierKeys.Shift), SelectionActions.MovePageDown);
			
			// ==== Misc ====
			
			keyBindings.Add (GetKeyCode (Key.a, ModifierKeys.Control), SelectionActions.SelectAll);
			
			keyBindings.Add (GetKeyCode (Key.z, ModifierKeys.Control), MiscActions.Undo);
			keyBindings.Add (GetKeyCode (Key.z, ModifierKeys.Control | ModifierKeys.Shift), MiscActions.Redo);
			
			keyBindings.Add (GetKeyCode (Key.F2), BookmarkActions.GotoNext);
			keyBindings.Add (GetKeyCode (Key.F2, ModifierKeys.Control), BookmarkActions.Toggle);
			keyBindings.Add (GetKeyCode (Key.F2, ModifierKeys.Shift), BookmarkActions.GotoPrevious);
		}
		
		protected override void HandleKeypress (Key key, uint unicodeChar, ModifierKeys modifier)
		{
			int keyCode = GetKeyCode (key, modifier);
			if (keyBindings.ContainsKey (keyCode)) {
				keyBindings [keyCode] (HexEditorData);
				return;
			}
			
			InsertCharacter (unicodeChar);
		}
		
		void InsertCharacter (uint unicodeChar)
		{
			char ch = (char)unicodeChar;
			if (HexEditorData.IsSomethingSelected)
				HexEditorData.DeleteSelection ();
			if (HexEditorData.Caret.InTextEditor) {
				if ((char.IsLetterOrDigit (ch) || char.IsPunctuation (ch) || ch == ' ') && unicodeChar <= 255) {
					if (HexEditorData.Caret.IsInsertMode) {
						HexEditorData.Insert (HexEditorData.Caret.Offset, (byte)unicodeChar);
					} else {
						HexEditorData.Replace (HexEditorData.Caret.Offset, 1, (byte)unicodeChar);
					}
					Editor.Margins.ForEach (margin => margin.PurgeLayoutCache ());
					CaretMoveActions.Right (HexEditorData);
				}
			} else {
				string hex = "0123456789ABCDEF";
				int idx = hex.IndexOf (char.ToUpper (ch));
				if (idx >= 0) {
					if (HexEditorData.Caret.Offset >= HexEditorData.Length)
						HexEditorData.Insert (HexEditorData.Length, 0);
					if (HexEditorData.Caret.IsInsertMode && HexEditorData.Caret.SubPosition == 0) {
						HexEditorData.Insert (HexEditorData.Caret.Offset, (byte)(idx * 0x10));
					} else {
						byte cur = HexEditorData.GetByte (HexEditorData.Caret.Offset);
						int newByte = HexEditorData.Caret.SubPosition == 0 ? cur & 0xF | idx * 0x10 : cur & 0xF0 | idx;
						HexEditorData.Replace (HexEditorData.Caret.Offset, 1, (byte)newByte);
					}
					Editor.Margins.ForEach (margin => margin.PurgeLayoutCache ());
					CaretMoveActions.Right (HexEditorData);
				}
			}
		}
		
		static int GetKeyCode (Key key)
		{
			return GetKeyCode (key, ModifierKeys.None);
		}
		static int GetKeyCode (Key key, ModifierKeys modifier)
		{
			uint m =       (uint)(((modifier & ModifierKeys.Control) != 0)? 1 : 0);
			m = (m << 1) | (uint)(((modifier & ModifierKeys.Shift)   != 0)? 1 : 0);
			m = (m << 1) | (uint)(((modifier & ModifierKeys.Alt)     != 0)? 1 : 0);
			
			return (int)key | (int)(m << 16);
		}
	}
}
