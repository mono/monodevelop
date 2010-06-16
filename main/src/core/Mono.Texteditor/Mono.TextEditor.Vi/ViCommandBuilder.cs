// 
// ViCommandBuilder.cs
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
		}
		
		public void Build (ModifierType modifiers, Key key, char ch)
		{
			var k = ch == '\0'? new ViKey (modifiers, key) : new ViKey (modifiers, ch);
			Keys.Add (k);
			if (!Builder (this)) {
				Error = "Unknown command";
			}
		}
		
		public string Error { get; set; }
		public string Message { get; set; }
		public Action<ViEditor> Action { get; set; }
		public ViBuilder Builder { get; set; }
		public IList<ViKey> Keys { get; private set; }
		public int Multiplier { get; set; }
		public char Register { get; set; }
		
		public ViKey LastKey {
			get { return Keys[Keys.Count - 1]; }
		}
		
		public static ViBuilderContext CreateNormal ()
		{
			return new ViBuilderContext () {
				Builder = normalBuilder
			};
		}
		
		//WORKAROUND: we used to use a static initializer but that triggered a gmcs 2.6.4 bug
		//so instead use a lazy pattern
		static ViBuilder normalBuilder {
			get {
				return _normalBuilder ?? (_normalBuilder = 
					ViBuilders.RegisterBuilder (
						ViBuilders.MultiplierBuilder (
							ViBuilders.First (normalActions.Builder, motions.Builder))));
			}
		}
		static ViBuilder _normalBuilder;
		
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
	
	class ViCommandMap : IEnumerable<KeyValuePair<ViKey,ViBuilder>>
	{
		Dictionary<ViKey,ViBuilder> builders = new Dictionary<ViKey,ViBuilder> ();
		Dictionary<ViKey,Action<ViEditor>> actions  = new Dictionary<ViKey,Action<ViEditor>> ();
		
		public bool Builder (ViBuilderContext ctx)
		{
			Action<ViEditor> a;
			if (actions.TryGetValue (ctx.LastKey, out a)) {
				ctx.Action = a;
				return true;
			} else {
				ViBuilder b;
				if (builders.TryGetValue (ctx.LastKey, out b)) {
					ctx.Builder = b;
					return true;
				}
			}
			return false;
		}
		
		public void Add (ViKey key, ViCommandMap map)
		{
			Add (key, map.Builder);
		}
		
		public void Add (ViKey key, Action<ViEditor> action)
		{
			this.actions[key] = action;
		}
		
		public void Add (ViKey key, Action<TextEditorData> action)
		{
			this.actions[key] = (ViEditor ed) => action (ed.Data);
		}
		
		public void Add (ViKey key, ViBuilder builder)
		{
			this.builders[key] = builder;
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return builders.GetEnumerator ();
		}
		
		public IEnumerator<KeyValuePair<ViKey,ViBuilder>> GetEnumerator ()
		{
			return builders.GetEnumerator ();
		}
	}
	
	class ViBuilders
	{
		public static bool Mark (ViBuilderContext ctx)
		{
			char c = ctx.LastKey.Char;
			if (!char.IsLetterOrDigit (c)) {
				ctx.Error = "Invalid Mark";
				return true;
			}
			
			ctx.Action = (ViEditor ed) => {
				ViMark mark;
				if (!ed.Marks.TryGetValue (c, out mark))
					ed.Marks [c] = mark = new ViMark (c);
				mark.SaveMark (ed.Data);
			};
			return true;
		}
		
		public static bool GoToMark (ViBuilderContext ctx)
		{
			char c = ctx.LastKey.Char;
			if (!char.IsLetterOrDigit (c)) {
				ctx.Error = "Invalid Mark";
				return true;
			}
			
			ctx.Action = (ViEditor ed) => {
				ViMark mark;
				if (ed.Marks.TryGetValue (c, out mark))
					mark.LoadMark (ed.Data);
				else
					ed.Reset ("Unknown Mark");
			};
			return true;
		}
		
		
		public static bool ReplaceChar (ViBuilderContext ctx)
		{
			if (ctx.LastKey.Char != '\0')
				ctx.Action = (ViEditor ed) => ed.Data.Replace (ed.Data.Caret.Offset, 1, ctx.LastKey.Char.ToString ());
			else
				ctx.Error = "Expecting a character";
			return true;
		}
		
		static void StartRegisterBuilder (ViBuilderContext ctx, ViBuilder nextBuilder)
		{
			if (ctx.Register != '\0') {
				ctx.Error = "Register already set";
				return;
			}
			ctx.Builder = (ViBuilderContext x) => {
				char c = x.LastKey.Char;
				if (!ViEditor.IsValidRegister (c)) {
					x.Error = "Invalid register";
					return true;
				}
				x.Register = c;
				x.Builder = nextBuilder;
				return true;
			};
		}
		
		static void StartMultiplierBuilder (ViBuilderContext ctx, ViBuilder nextBuilder)
		{
			int factor = 1;
			int multiplier = 0;
			ctx.Builder = (ViBuilderContext x) => {
				int c = (int)x.LastKey.Char;
				if (c >= (int)'0' && c <= (int)'9') {
					int d = c - (int)'0';
					multiplier = multiplier * factor + d;
					factor *= 10;
					return true;
				} else {
					ctx.Multiplier *= multiplier;
					ctx.Builder = nextBuilder;
					return ctx.Builder (ctx);
				}
			};
			ctx.Builder (ctx);
		}
		
		public static ViBuilder MultiplierBuilder (ViBuilder nextBuilder)
		{
			return (ViBuilderContext ctx) => {
				var k = ctx.LastKey;
				if (char.IsDigit (k.Char)) {
					ViBuilders.StartMultiplierBuilder (ctx, nextBuilder);
					return true;
				} else {
					ctx.Builder = nextBuilder;
					return ctx.Builder (ctx);
				}
			};
		}
		
		public static ViBuilder RegisterBuilder (ViBuilder nextBuilder)
		{
			return (ViBuilderContext ctx) => {
				var k = ctx.LastKey;
				if (k.Char == '"') {
					ViBuilders.StartRegisterBuilder (ctx, nextBuilder);
					return true;
				} else {
					ctx.Builder = nextBuilder;
					return ctx.Builder (ctx);
				}
			};
		}
		
		public static ViBuilder First (params ViBuilder[] builders)
		{
			return (ViBuilderContext ctx) => builders.Any (b => b (ctx));
		}
	}
	
	class ViEditorActions
	{
		public static void CaretToScreenCenter (ViEditor ed)
		{
			var line = ed.Editor.VisualToDocumentLocation (0, ed.Editor.Allocation.Height/2).Line;
			if (line < 0)
				line = ed.Data.Document.LineCount - 1;
			ed.Data.Caret.Line = line;
		}
		
		public static void CaretToScreenBottom (ViEditor ed)
		{
			int line = ed.Editor.VisualToDocumentLocation (0, ed.Editor.Allocation.Height - ed.Editor.LineHeight * 2 - 2).Line;
			if (line < 0)
				line = ed.Data.Document.LineCount - 1;
			ed.Data.Caret.Line = line;
		}
		
		public static void CaretToScreenTop (ViEditor ed)
		{
			ed.Data.Caret.Line = System.Math.Max (0, ed.Editor.VisualToDocumentLocation (0, ed.Editor.LineHeight - 1).Line);
		}
	}
}

