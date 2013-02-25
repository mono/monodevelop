// 
// ViActionMaps.cs
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

namespace Mono.TextEditor.Vi
{
	
	
	public static class ViActionMaps
	{
	
		public static Action<TextEditorData> GetEditObjectCharAction (char c, Motion motion)
		{
			if (motion == Motion.None) return GetEditObjectCharAction(c);

			switch (c) {
			case 'w':
				return ViActions.InnerWord;
			case ')':
			case '}':
			case ']':
			case '>':
				if (motion == Motion.Inner)
					return ViActions.InnerSymbol (c);
				else if (motion == Motion.Outer)
					return ViActions.OuterSymbol (c);
				else
					return null;
			case '"':
			case '\'':
			case '`':
				if (motion == Motion.Inner)
					return ViActions.InnerQuote (c);
				else if (motion == Motion.Outer)
					return ViActions.OuterQuote (c);
				else
					return null;
			default:
				return null;
			}
		}

		public static Action<TextEditorData> GetEditObjectCharAction (char c)
		{
			switch (c) {
			case 'W':
			case 'w':
				return ViActions.WordEnd;
			case 'B':
			case 'b':
				return ViActions.WordStart;
			}
			return GetNavCharAction (c);
		}
		
		public static Action<TextEditorData> GetNavCharAction (char c)
		{
			switch (c) {
			case 'h':
				return ViActions.Left;
			case 'b':
				return CaretMoveActions.PreviousSubword;
			case 'B':
				return CaretMoveActions.PreviousWord;
			case 'l':
				return ViActions.Right;
			case 'w':
				return CaretMoveActions.NextSubword;
			case 'W':
				return CaretMoveActions.NextWord;
			case 'k':
				return ViActions.Up;
			case 'j':
				return ViActions.Down;
			case '%':
				return MiscActions.GotoMatchingBracket;
			case '0':
				return CaretMoveActions.LineStart;
			case '^':
			case '_':
				return CaretMoveActions.LineFirstNonWhitespace;
			case '$':
				return ViActions.LineEnd;
			case 'G':
				return CaretMoveActions.ToDocumentEnd;
			case '{':
				return ViActions.MoveToPreviousEmptyLine;
			case '}':
				return ViActions.MoveToNextEmptyLine;
			}
			return null;
		}
		
		public static Action<TextEditorData> GetDirectionKeyAction (Gdk.Key key, Gdk.ModifierType modifier)
		{
			//
			// NO MODIFIERS
			//
			if ((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0) {
				switch (key) {
				case Gdk.Key.Left:
				case Gdk.Key.KP_Left:
					return ViActions.Left;
					
				case Gdk.Key.Right:
				case Gdk.Key.KP_Right:
					return ViActions.Right;
					
				case Gdk.Key.Up:
				case Gdk.Key.KP_Up:
					return ViActions.Up;
					
				case Gdk.Key.Down:
				case Gdk.Key.KP_Down:
					return ViActions.Down;
				
				//not strictly vi, but more useful IMO
				case Gdk.Key.KP_Home:
				case Gdk.Key.Home:
					return CaretMoveActions.LineHome;
					
				case Gdk.Key.KP_End:
				case Gdk.Key.End:
					return ViActions.LineEnd;

				case Gdk.Key.Page_Up:
				case Gdk.Key.KP_Page_Up:
					return CaretMoveActions.PageUp;

				case Gdk.Key.Page_Down:
				case Gdk.Key.KP_Page_Down:
					return CaretMoveActions.PageDown;
				}
			}
			//
			// === CONTROL ===
			//
			else if ((modifier & Gdk.ModifierType.ShiftMask) == 0
			         && (modifier & Gdk.ModifierType.ControlMask) != 0)
			{
				switch (key) {
				case Gdk.Key.Left:
				case Gdk.Key.KP_Left:
					return CaretMoveActions.PreviousWord;
					
				case Gdk.Key.Right:
				case Gdk.Key.KP_Right:
					return CaretMoveActions.NextWord;
					
				case Gdk.Key.Up:
				case Gdk.Key.KP_Up:
					return ScrollActions.Up;
					
				// usually bound at IDE level
				case Gdk.Key.u:
					return CaretMoveActions.PageUp;
					
				case Gdk.Key.Down:
				case Gdk.Key.KP_Down:
					return ScrollActions.Down;
					
				case Gdk.Key.d:
					return CaretMoveActions.PageDown;
				
				case Gdk.Key.KP_Home:
				case Gdk.Key.Home:
					return CaretMoveActions.ToDocumentStart;
					
				case Gdk.Key.KP_End:
				case Gdk.Key.End:
					return CaretMoveActions.ToDocumentEnd;
				}
			}
			return null;
		}
		
		public static Action<TextEditorData> GetInsertKeyAction (Gdk.Key key, Gdk.ModifierType modifier)
		{
			//
			// NO MODIFIERS
			//
			if ((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0) {
				switch (key) {
				case Gdk.Key.Tab:
					return MiscActions.InsertTab;
					
				case Gdk.Key.Return:
				case Gdk.Key.KP_Enter:
					return MiscActions.InsertNewLine;
					
				case Gdk.Key.BackSpace:
					return DeleteActions.Backspace;
					
				case Gdk.Key.Delete:
				case Gdk.Key.KP_Delete:
					return DeleteActions.Delete;
					
				case Gdk.Key.Insert:
					return MiscActions.SwitchCaretMode;
				}
			}
			//
			// CONTROL
			//
			else if ((modifier & Gdk.ModifierType.ControlMask) != 0
			         && (modifier & Gdk.ModifierType.ShiftMask) == 0)
			{
				switch (key) {
				case Gdk.Key.BackSpace:
					return DeleteActions.PreviousWord;
					
				case Gdk.Key.Delete:
				case Gdk.Key.KP_Delete:
					return DeleteActions.NextWord;
				}
			}
			//
			// SHIFT
			//
			else if ((modifier & Gdk.ModifierType.ControlMask) == 0
			         && (modifier & Gdk.ModifierType.ShiftMask) != 0)
			{
				switch (key) {
				case Gdk.Key.Tab:
					return MiscActions.RemoveTab;
					
				case Gdk.Key.BackSpace:
					return DeleteActions.Backspace;

				case Gdk.Key.Return:
				case Gdk.Key.KP_Enter:
					return MiscActions.InsertNewLine;
				}
			}
			return null;
		}
		
		public static Action<TextEditorData> GetCommandCharAction (char c)
		{
			switch (c) {
			case 'u':
				return MiscActions.Undo;
			}
			return null;
		}
	}
}
