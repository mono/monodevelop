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

namespace Mono.MHex
{
	public class SimpleEditMode : EditMode
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
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left), CaretMoveActions.Left);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left), CaretMoveActions.Left);
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ControlMask), CaretMoveActions.Left);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ControlMask), CaretMoveActions.Left);
			
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask), SelectionActions.MoveLeft);
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask), SelectionActions.MoveLeft);
			
			// ==== Right ====
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right), CaretMoveActions.Right);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right), CaretMoveActions.Right);
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ControlMask), CaretMoveActions.Right);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ControlMask), CaretMoveActions.Right);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask), SelectionActions.MoveRight);
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask), SelectionActions.MoveRight);
			
			// ==== Up ====
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up), CaretMoveActions.Up);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up), CaretMoveActions.Up);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ControlMask), ScrollActions.Up);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ControlMask), ScrollActions.Up);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ShiftMask), SelectionActions.MoveUp);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask), SelectionActions.MoveUp);
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), SelectionActions.MoveUp);
			
			// ==== Down ====
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down), CaretMoveActions.Down);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down), CaretMoveActions.Down);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ControlMask), ScrollActions.Down);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ControlMask), ScrollActions.Down);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ShiftMask), SelectionActions.MoveDown);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask), SelectionActions.MoveDown);
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), SelectionActions.MoveDown);
			
			// ==== Deletion, insertion ====
		
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert), MiscActions.SwitchCaretMode);
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert), MiscActions.SwitchCaretMode);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Tab), CaretMoveActions.SwitchSide);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace), DeleteActions.Backspace);
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ShiftMask), DeleteActions.Backspace);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete), DeleteActions.Delete);
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete), DeleteActions.Delete);
		}
		
		void InitDefaultBindings ()
		{
			// === Home ===
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home), CaretMoveActions.LineHome);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home), CaretMoveActions.LineHome);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ShiftMask), SelectionActions.MoveLineHome);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask), SelectionActions.MoveLineHome);

			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ControlMask), CaretMoveActions.ToDocumentStart);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ControlMask), CaretMoveActions.ToDocumentStart);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), SelectionActions.MoveToDocumentStart);
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), SelectionActions.MoveToDocumentStart);
			
			// ==== End ====
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End), CaretMoveActions.LineEnd);
			keyBindings.Add (GetKeyCode (Gdk.Key.End), CaretMoveActions.LineEnd);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ShiftMask), SelectionActions.MoveLineEnd);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask), SelectionActions.MoveLineEnd);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ControlMask), CaretMoveActions.ToDocumentEnd);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ControlMask), CaretMoveActions.ToDocumentEnd);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), SelectionActions.MoveToDocumentEnd);
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), SelectionActions.MoveToDocumentEnd);
			
			// ==== Cut, copy, paste ===
			/*
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
			*/
			// ==== Page up/down ====
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up), CaretMoveActions.PageUp);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up), CaretMoveActions.PageUp);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up, Gdk.ModifierType.ShiftMask), SelectionActions.MovePageUp);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up, Gdk.ModifierType.ShiftMask), SelectionActions.MovePageUp);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down), CaretMoveActions.PageDown);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down), CaretMoveActions.PageDown);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down, Gdk.ModifierType.ShiftMask), SelectionActions.MovePageDown);
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down, Gdk.ModifierType.ShiftMask), SelectionActions.MovePageDown);
			
			// ==== Misc ====
			
			keyBindings.Add (GetKeyCode (Gdk.Key.a, Gdk.ModifierType.ControlMask), SelectionActions.SelectAll);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.ControlMask), MiscActions.Undo);
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask), MiscActions.Redo);
			
			keyBindings.Add (GetKeyCode (Gdk.Key.F2), BookmarkActions.GotoNext);
			keyBindings.Add (GetKeyCode (Gdk.Key.F2, Gdk.ModifierType.ControlMask), BookmarkActions.Toggle);
			keyBindings.Add (GetKeyCode (Gdk.Key.F2, Gdk.ModifierType.ShiftMask), BookmarkActions.GotoPrevious);
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeChar, Gdk.ModifierType modifier)
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
				string hex = "01234567890ABCDEF";
				int idx = hex.IndexOf (char.ToUpper (ch));
				if (idx >= 0) {
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
		
		static int GetKeyCode (Gdk.Key key)
		{
			return GetKeyCode (key, Gdk.ModifierType.None);
		}
		static int GetKeyCode (Gdk.Key key, Gdk.ModifierType modifier)
		{
			uint m =       (uint)(((modifier & Gdk.ModifierType.ControlMask) != 0)? 1 : 0);
			m = (m << 1) | (uint)(((modifier & Gdk.ModifierType.ShiftMask)   != 0)? 1 : 0);
			m = (m << 1) | (uint)(((modifier & Gdk.ModifierType.Mod1Mask)    != 0)? 1 : 0);
			
			return (int)key | (int)(m << 16);
		}
	}
}
