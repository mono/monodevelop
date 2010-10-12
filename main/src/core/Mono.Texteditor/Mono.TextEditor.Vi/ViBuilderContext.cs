// 
// ViBuilderContext.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using Gdk;
using System.Collections.Generic;
using System.Text;
using Mono.TextEditor;
using System.Linq;

namespace Mono.TextEditor.Vi
{
	
	/// <summary>
	/// Returns true if it handled the keystroke.
	/// </summary>
	public delegate bool ViBuilder (ViBuilderContext ctx);
	
	public class ViBuilderContext
	{
		ViBuilderContext ()
		{
			Keys = new List<ViKey> ();
			Multiplier = 1;
		}
		
		public void Build (ViEditor editor, Key key, char ch, ModifierType modifiers)
		{
			var k = ch == '\0'? new ViKey (modifiers, key) : new ViKey (modifiers, ch);
			Keys.Add (k);
			if (!Builder (this)) {
				Error = "Unknown command";
			}
		}
		
		public string Error { get; set; }
		public string Message { get; set; }
		public IList<ViKey> Keys { get; private set; }
		public int Multiplier { get; set; }
		public char Register { get; set; }
		
		Action<ViEditor> action;
		
		public Action<ViEditor> Action {
			get { return action; }
			set {
				if (action != null)
					throw new InvalidOperationException ("Action already set");
				
				//FALLBACK for builders that don't handler multipliers directly
				//we cap these at 100, to reduce the length of time MD could be unresponsive
				if (Multiplier > 1) {
					action = (ViEditor v) => {
						for (int i = 0; i < System.Math.Min (100, Multiplier); i++)
							value (v);
					};
				} else {
					action = value;
				}
			}
		}
		
		public ViBuilder builder;
		
		public ViBuilder Builder {
			get { return builder; }
			set {
				if (value == null)
					throw new ArgumentException ("builder cannot be null");
				if (action != null)
					throw new InvalidOperationException ("builder cannot be set after action has been set");
				builder = value;
			}
		}
		
		public ViKey LastKey {
			get { return Keys[Keys.Count - 1]; }
		}
		
		public static ViBuilderContext Create ()
		{
			return new ViBuilderContext () {
				Builder = normalBuilder
			};
		}
		
		static ViBuilderContext ()
		{
			normalBuilder = 
				ViBuilders.RegisterBuilder (
					ViBuilders.MultiplierBuilder (
						ViBuilders.First (normalActions.Builder, motions.Builder)));
		}
		
		static ViBuilder normalBuilder;
		
		static ViCommandMap normalActions = new ViCommandMap () {
			{ 'J', ViActions.Join },
			{ 'z', new ViCommandMap () {
				{ 'A', FoldActions.ToggleFoldRecursive },
				{ 'C', FoldActions.CloseFoldRecursive },
				{ 'M', FoldActions.CloseAllFolds },
				{ 'O', FoldActions.OpenFoldRecursive },
				{ 'R', FoldActions.OpenAllFolds },
				{ 'a', FoldActions.ToggleFold },
				{ 'c', FoldActions.CloseFold },
				{ 'o', FoldActions.OpenFold },
			}},
			{ 'g', new ViCommandMap () {
				{ 'g', CaretMoveActions.LineStart },
			}},
			{ 'r', ViBuilders.ReplaceChar },
			{ '~', ViActions.ToggleCase },
			{ 'm', ViBuilders.Mark },
			{ 'M', ViEditorActions.CaretToScreenCenter },
			{ 'H', ViEditorActions.CaretToScreenTop },
			{ 'L', ViEditorActions.CaretToScreenBottom },
			{ 'u', MiscActions.Undo },
			{ new ViKey (ModifierType.ControlMask, Key.Up),       ScrollActions.Up },
			{ new ViKey (ModifierType.ControlMask, Key.KP_Up),    ScrollActions.Up },
			{ new ViKey (ModifierType.ControlMask, Key.Down),     ScrollActions.Down },
			{ new ViKey (ModifierType.ControlMask, Key.KP_Down),  ScrollActions.Down },
		};
		
		static ViCommandMap motions = new ViCommandMap () {
			{ '`', ViBuilders.GoToMark },
			{ 'h', ViActions.Left },
			{ 'b', CaretMoveActions.PreviousSubword },
			{ 'B', CaretMoveActions.PreviousWord },
			{ 'l', ViActions.Right },
			{ 'w', CaretMoveActions.NextSubword },
			{ 'W', CaretMoveActions.NextWord },
			{ 'k', ViActions.Up },
			{ 'j', ViActions.Down },
			{ '%', MiscActions.GotoMatchingBracket },
			{ '0', CaretMoveActions.LineStart },
			{ '^', CaretMoveActions.LineFirstNonWhitespace },
			{ '_', CaretMoveActions.LineFirstNonWhitespace },
			{ '$', ViActions.LineEnd },
			{ 'G', CaretMoveActions.ToDocumentEnd },
			{ '{', ViActions.MoveToPreviousEmptyLine },
			{ '}', ViActions.MoveToNextEmptyLine },
			{ Key.Left,         ViActions.Left },
			{ Key.KP_Left,      ViActions.Left },
			{ Key.Right,        ViActions.Right },
			{ Key.KP_Right,     ViActions.Right },
			{ Key.Up,           ViActions.Up },
			{ Key.KP_Up,        ViActions.Up },
			{ Key.Down,         ViActions.Down },
			{ Key.KP_Down,      ViActions.Down },
			{ Key.KP_Home,      CaretMoveActions.LineHome },
			{ Key.Home,         CaretMoveActions.LineHome },
			{ Key.KP_End,       ViActions.LineEnd },
			{ Key.End,          ViActions.LineEnd },
			{ Key.Page_Up,      CaretMoveActions.PageUp },
			{ Key.KP_Page_Up,   CaretMoveActions.PageUp },
			{ Key.Page_Down,    CaretMoveActions.PageDown },
			{ Key.KP_Page_Down, CaretMoveActions.PageDown },
			{ new ViKey (ModifierType.ControlMask, Key.Left),     CaretMoveActions.PreviousWord },
			{ new ViKey (ModifierType.ControlMask, Key.KP_Left),  CaretMoveActions.PreviousWord },
			{ new ViKey (ModifierType.ControlMask, Key.Right),    CaretMoveActions.NextWord },
			{ new ViKey (ModifierType.ControlMask, Key.KP_Right), CaretMoveActions.NextWord },
			{ new ViKey (ModifierType.ControlMask, Key.Home),     CaretMoveActions.ToDocumentStart },
			{ new ViKey (ModifierType.ControlMask, Key.KP_Home),  CaretMoveActions.ToDocumentStart },
			{ new ViKey (ModifierType.ControlMask, Key.End),      CaretMoveActions.ToDocumentEnd },
			{ new ViKey (ModifierType.ControlMask, Key.KP_End),   CaretMoveActions.ToDocumentEnd },
			{ new ViKey (ModifierType.ControlMask, 'u'),  CaretMoveActions.PageUp },
			{ new ViKey (ModifierType.ControlMask, 'd'),  CaretMoveActions.PageDown },
		};
		
		static ViCommandMap insertActions = new ViCommandMap () {
			{ Key.Tab,       MiscActions.InsertTab },
			{ Key.Return,    MiscActions.InsertNewLine },
			{ Key.KP_Enter,  MiscActions.InsertNewLine },
			{ Key.BackSpace, DeleteActions.Backspace },
			{ Key.Delete,    DeleteActions.Delete },
			{ Key.KP_Delete, DeleteActions.Delete },
			{ Key.Insert,    MiscActions.SwitchCaretMode },
			{ new ViKey (ModifierType.ControlMask, Key.BackSpace), DeleteActions.PreviousWord },
			{ new ViKey (ModifierType.ControlMask, Key.Delete),    DeleteActions.NextWord },
			{ new ViKey (ModifierType.ControlMask, Key.KP_Delete), DeleteActions.NextWord },
			{ new ViKey (ModifierType.ShiftMask,   Key.Tab),       MiscActions.RemoveTab },
			{ new ViKey (ModifierType.ShiftMask,   Key.BackSpace),       DeleteActions.Backspace },
		};
	}
}

