// 
// ViBuilders.cs
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
using System.Linq;
using Gdk;
using System.Text;

namespace Mono.TextEditor.Vi
{
	class ViBuilders
	{
		public static bool Mark (ViBuilderContext ctx)
		{
			char c = ctx.LastKey.Char;
			if (!char.IsLetterOrDigit (c)) {
				ctx.SetError ("Invalid Mark");
				return true;
			}
			
			ctx.RunAction ((ViEditor ed) => {
				ViMark mark;
				if (!ed.Marks.TryGetValue (c, out mark))
					ed.Marks [c] = mark = new ViMark (c);
				mark.SaveMark (ed.Data);
			});
			return true;
		}
		
		public static ViBuilder InsertBuilder (ViBuilder preInsertActions)
		{	
			var lastInserted = new StringBuilder ();
			bool inUndoTx = false;
			return (ViBuilderContext ctx) => {
				var l = ctx.LastKey;
				bool noModifiers = l.Modifiers == ModifierType.None;
				
				ctx.Message = ctx.Mode == ViEditorMode.Replace? "-- REPLACE --" : "-- INSERT --";
				
				if ((noModifiers && l.Key == Key.Escape) || (l.Char == 'c' && (l.Modifiers & ModifierType.ControlMask) != 0)) {
					ctx.RunAction ((ViEditor ed) => {
						if (inUndoTx)
						{
							ed.Document.EndAtomicUndo ();
							inUndoTx = false;
						}
						ed.LastInsertedText = lastInserted.ToString ();
						ed.SetMode (ViEditorMode.Normal);
					});
					return true;
				}
				
				//keypad motions etc
				if (preInsertActions (ctx)) {
					if (inUndoTx)
						ctx.RunAction ( (ed) => ed.Document.EndAtomicUndo () );
					inUndoTx = false;
					ctx.SuppressCompleted ();
					lastInserted.Length = 0;
					return true;
				}
				
				if (l.Char != '\0' && noModifiers) {
					if (!inUndoTx)
						ctx.RunAction ( (ed) => ed.Document.BeginAtomicUndo () );
					inUndoTx = true;
					ctx.SuppressCompleted ();
					lastInserted.Append (l.Char);
					ctx.InsertChar (l.Char);
					return true;
				}
				
				return false;
			};
		}
		
		public static bool GoToMark (ViBuilderContext ctx)
		{
			char c = ctx.LastKey.Char;
			if (!char.IsLetterOrDigit (c)) {
				ctx.SetError ("Invalid Mark");
				return true;
			}
			
			ctx.RunAction ((ViEditor ed) => {
				ViMark mark;
				if (ed.Marks.TryGetValue (c, out mark))
					mark.LoadMark (ed.Data);
				else
					ed.Reset ("Unknown Mark");
			});
			return true;
		}
		
		
		public static bool ReplaceChar (ViBuilderContext ctx)
		{
			if (ctx.LastKey.Char != '\0')
				ctx.RunAction ((ViEditor ed) => ed.Data.Replace (ed.Data.Caret.Offset, 1, ctx.LastKey.Char.ToString ()));
			else
				ctx.SetError ("Expecting a character");
			return true;
		}
		
		static void StartRegisterBuilder (ViBuilderContext ctx, ViBuilder nextBuilder)
		{
			if (ctx.Register != '\0') {
				ctx.SetError ("Register already set");
				return;
			}
			ctx.Builder = (ViBuilderContext x) => {
				char c = x.LastKey.Char;
				if (!ViEditor.IsValidRegister (c)) {
					x.SetError ("Invalid register");
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
}
