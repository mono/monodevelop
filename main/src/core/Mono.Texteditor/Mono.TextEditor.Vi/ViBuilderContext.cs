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
		readonly ViEditor editor;
		readonly List<ViKey> keys = new List<ViKey> ();
		
		ViBuilderContext (ViEditor editor)
		{
			this.editor = editor;
			Multiplier = 1;
		}
		
		public void ProcessKey (Key key, char ch, ModifierType modifiers)
		{
			var k = ch == '\0'? new ViKey (modifiers, key) : new ViKey (modifiers, ch);
			Keys.Add (k);
			if (!Builder (this)) {
				SetError ("Unknown command");
			}
		}
		
		public void SetError (string error)
		{
			Completed = Error = true;
			Message = error;
		}
		
		public string Message { get; set; }
		public int Multiplier { get; set; }
		public char Register { get; set; }
		protected ViEditor Editor { get { return editor; } }
		public List<ViKey> Keys { get { return keys; } }
		public bool Completed { get; private set; }
		public bool Error { get; private set; }
		
		//copied from EditMode.cs, with the undo stack handling code removed
		public void InsertChar (char ch)
		{
			var data = Editor.Data;
			var doc = Editor.Document;
			var caret = Editor.Data.Caret;
			
			if (!data.CanEdit (data.Caret.Line))
				return;
			
			if (Editor.Editor != null)
				Editor.Editor.HideMouseCursor ();
			
			if (data.IsSomethingSelected && data.MainSelection.SelectionMode == SelectionMode.Block) {
				data.Caret.PreserveSelection = true;
				if (!data.MainSelection.IsDirty) {
					data.DeleteSelectedText (false);
					data.MainSelection.IsDirty = true;
				}
			} else {
				data.DeleteSelectedText ();
			}
			
			if (!char.IsControl (ch) && data.CanEdit (caret.Line)) {
				LineSegment line = doc.GetLine (caret.Line);
				if (caret.IsInInsertMode || caret.Column >= line.EditableLength + 1) {
					string text = caret.Column > line.EditableLength + 1 ? data.GetVirtualSpaces (caret.Line, caret.Column) + ch.ToString () : ch.ToString ();
					if (data.IsSomethingSelected && data.MainSelection.SelectionMode == SelectionMode.Block) {
						int length = 0;
						for (int lineNumber = data.MainSelection.MinLine; lineNumber <= data.MainSelection.MaxLine; lineNumber++) {
							length = data.Insert (doc.GetLine (lineNumber).Offset + caret.Column, text);
						}
						caret.Column += length - 1;
						data.MainSelection.Lead = new DocumentLocation (data.MainSelection.Lead.Line, caret.Column + 1);
						data.MainSelection.IsDirty = true;
						doc.CommitMultipleLineUpdate (data.MainSelection.MinLine, data.MainSelection.MaxLine);
					} else {
						int length = data.Insert (caret.Offset, text);
						caret.Column += length - 1;
					}
				} else {
					int length = data.Replace (caret.Offset, 1, ch.ToString ());
					if (length > 1)
						caret.Offset += length - 1;
				}
				caret.Column++;
			}
			if (data.IsSomethingSelected && data.MainSelection.SelectionMode == SelectionMode.Block)
				data.Caret.PreserveSelection = false;
		}
		
		public void RunAction (Action<ViEditor> action)
		{
			Completed = true;
			
			//FALLBACK for builders that don't handler multipliers directly
			//we cap these at 100, to reduce the length of time MD could be unresponsive
			if (Multiplier > 1) {
				for (int i = 0; i < System.Math.Min (100, Multiplier); i++)
					action (editor);
			} else {
				action (editor);
			}
		}
		
		public ViEditorMode Mode { get { return Editor.Mode; } }
		
		//HACK: this is really inelegant
		public void SuppressCompleted ()
		{
			Completed = false;
		}
		
		private ViBuilder _builder;
		
		public ViBuilder Builder {
			get { return _builder; }
			set {
				if (_builder == value)
					return;
				if (value == null)
					throw new ArgumentException ("builder cannot be null");
				if (Completed)
					throw new InvalidOperationException ("builder cannot be set after context is completed");
				_builder = value;
			}
		}
		
		public ViKey LastKey {
			get { return Keys[Keys.Count - 1]; }
		}
		
		public static ViBuilderContext Create (ViEditor editor)
		{
			return new ViBuilderContext (editor) {
				Builder = normalBuilder
			};
		}
		
		static ViBuilderContext ()
		{
			normalBuilder = 
				ViBuilders.RegisterBuilder (
					ViBuilders.MultiplierBuilder (
						ViBuilders.First (normalActions.Builder, motions.Builder, nonCharMotions.Builder)));
			insertActions = ViBuilders.First (nonCharMotions.Builder, insertEditActions.Builder);
		}
		
		static ViBuilder normalBuilder, insertActions;
		
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
				{ 'g', CaretMoveActions.ToDocumentStart },
			}},
			{ 'r', ViBuilders.ReplaceChar },
			{ '~', ViActions.ToggleCase },
			{ 'm', ViBuilders.Mark },
			{ 'M', ViEditorActions.CaretToScreenCenter },
			{ 'H', ViEditorActions.CaretToScreenTop },
			{ 'L', ViEditorActions.CaretToScreenBottom },
			{ 'u', MiscActions.Undo },
			{ 'i', Insert, true },
			{ 'R', Replace, true },
			{ 'o', Open, true },
			{ 'O', OpenAbove, true },
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
		};
		
		static ViCommandMap nonCharMotions = new ViCommandMap () {
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
		
		static ViCommandMap insertEditActions = new ViCommandMap () {
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
		
		static bool Insert (ViBuilderContext ctx)
		{
			ctx.RunAction ((ViEditor e) => e.SetMode (ViEditorMode.Insert));
			ctx.SuppressCompleted ();
			
			ctx.Builder = ViBuilders.InsertBuilder (insertActions);
			return true;
		}
		
		static bool Replace (ViBuilderContext ctx)
		{
			ctx.RunAction ((ViEditor e) => e.SetMode (ViEditorMode.Replace));
			ctx.SuppressCompleted ();
			
			ctx.Builder = ViBuilders.InsertBuilder (insertActions);
			return true;
		}
		
		static bool Open (ViBuilderContext ctx)
		{
			ctx.RunAction ((ViEditor e) => MiscActions.InsertNewLineAtEnd (e.Data));
			return Insert (ctx);
		}
		
		static bool OpenAbove (ViBuilderContext ctx)
		{
			// FIXME: this doesn't work correctly on the first line
			ctx.RunAction ((ViEditor e) => ViActions.Up (e.Data));
			return Open (ctx);
		}
	}
}

