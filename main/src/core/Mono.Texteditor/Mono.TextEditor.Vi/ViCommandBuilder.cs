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

namespace Mono.TextEditor.Vi
{
	public enum ViState
	{
		Normal,
		Visual,
		VisualLine
	}
	
	public class ViEditor
	{
		public TextEditor Editor { get; private set; }
		public TextEditorData Data { get; private set; }
		public ViEditorContext Context { get; private set; }
		
		public void Reset (string message)
		{
		}
	}
	
	public class ViBuilderContext
	{
		public string Error { get; set; }
		public string Message { get; set; }
		public Action<ViEditor> Action { get; set; }
		public Action<ViBuilderContext> Builder { get; set; }
		public IList<ViKey> Keys { get; set; }
		public int KeyIndex { get; set; }
		public int Multiplier { get; set; }
		public char Register { get; set; }
		
		public ViKey LastKey {
			get { return Keys[KeyIndex]; }
		}
		
		static Action<ViBuilderContext> normalBuilder = new ViCommandMap () {
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
			{ 'M', ViEditorActions.CenterCaret },
			{ '`', ViBuilders.GoToMark },
		}.Builder;
	}
	
	class ViCommandMap : IEnumerable<KeyValuePair<ViKey,Action<ViBuilderContext>>>
	{
		Dictionary<ViKey,Action<ViBuilderContext>> builders = new Dictionary<ViKey, Action<ViBuilderContext>> ();
		Dictionary<ViKey,Action<ViEditor>> actions  = new Dictionary<ViKey, Action<ViEditor>> ();
		
		public void Builder (ViBuilderContext ctx)
		{
			var k = ctx.LastKey;
			if (k.Char == '"') {
				ViBuilders.CreateSetRegisterBuilder (ctx, this.Builder);
				return;
			}
			
			Action<ViEditor> a;
			if (actions.TryGetValue (k, out a)) {
				ctx.Action = a;
			} else {
				Action<ViBuilderContext> b;
				if (builders.TryGetValue (k, out b)) {
					ctx.Builder = b;
				} else {
					ctx.Error = "Unknown command";
				}
			}
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
		
		public void Add (ViKey key, Action<ViBuilderContext> builder)
		{
			this.builders[key] = builder;
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return builders.GetEnumerator ();
		}
		
		public IEnumerator<KeyValuePair<ViKey,Action<ViBuilderContext>>> GetEnumerator ()
		{
			return builders.GetEnumerator ();
		}
	}
	
	class ViBuilders
	{
		public static void Mark (ViBuilderContext ctx)
		{
			char c = ctx.LastKey.Char;
			if (!char.IsLetterOrDigit (c)) {
				ctx.Error = "Invalid Mark";
				return;
			}
			
			ctx.Action = (ViEditor ed) => {
				ViMark mark;
				if (!ed.Context.Marks.TryGetValue (c, out mark))
					ed.Context.Marks [c] = mark = new ViMark (c);
				mark.SaveMark (ed.Data);
			};
		}
		
		public static void GoToMark (ViBuilderContext ctx)
		{
			char c = ctx.LastKey.Char;
			if (!char.IsLetterOrDigit (c)) {
				ctx.Error = "Invalid Mark";
				return;
			}
			
			ctx.Action = (ViEditor ed) => {
				ViMark mark;
				if (ed.Context.Marks.TryGetValue (c, out mark))
					mark.LoadMark (ed.Data);
				else
					ed.Reset ("Unknown Mark");
			};
		}
		
		
		public static void ReplaceChar (ViBuilderContext ctx)
		{
			if (ctx.LastKey.Char != '\0')
				ctx.Action = (ViEditor ed) => ed.Data.Replace (ed.Data.Caret.Offset, 1, ctx.LastKey.Char.ToString ());
			else
				ctx.Error = "Expecting a character";
		}
		
		public static void CreateSetRegisterBuilder (ViBuilderContext ctx, Action<ViBuilderContext> nextBuilder)
		{
			if (ctx.Register == '\0') {
				ctx.Error = "Register already set";
				return;
			}
			ctx.Builder = (ViBuilderContext x) => {
				char c = x.LastKey.Char;
				if (!ViEditorContext.IsValidRegister (c)) {
					x.Error = "Invalid register";
					return;
				}
				x.Register = c;
				x.Builder = nextBuilder;
			};
		}
	}
	
	class ViEditorActions
	{
		public static void CenterCaret (ViEditor ed)
		{
			var line = ed.Editor.VisualToDocumentLocation (0, ed.Editor.Allocation.Height/2).Line;
			if (line < 0)
				line = ed.Data.Document.LineCount - 1;
			ed.Data.Caret.Line = line;
		}
	}
}

