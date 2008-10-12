//
// ViMode.cs
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
using System.Text;
using System.Collections.Generic;

namespace Mono.TextEditor.Vi
{
	
	
	public class ViEditMode : EditMode
	{
		State state;
		string status;
		StringBuilder commandBuffer = new StringBuilder ();
		
		public virtual string Status { get; protected set; }
		
		protected virtual string RunExCommand (string command)
		{
			int line;
			if (int.TryParse (command.Substring (1), out line)) {
				if (line <= 0 || line > Data.Document.LineCount) {
					return "Invalid line number.";
				}
				
				Data.Caret.Line = line - 1;
				Editor.ScrollToCaret ();
				return string.Format ("Jumped to line {0}.", line);
			}
			
			return "Command not recognised";
		}
		
		public override bool WantsToPreemptIM {
			get {
				return state != State.Insert && state != State.Replace;
			}
		}
		
		void ResetEditorState (TextEditorData data)
		{
			data.ClearSelection ();
			if (!data.Caret.IsInInsertMode)
				data.Caret.IsInInsertMode = true;
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.Escape || (key == Gdk.Key.c && (modifier & Gdk.ModifierType.ControlMask) != 0)) {
				ResetEditorState (Data);
				state = State.Normal;
				commandBuffer.Length = 0;
				Status = string.Empty;
				return;
			}
			
			int keyCode;
			Action<TextEditorData> action;
			
			switch (state) {
			case State.Normal:
				if (((modifier & (Gdk.ModifierType.ControlMask)) == 0)) {
					switch ((char)unicodeKey) {
					case ':':
						state = State.Command;
						commandBuffer.Append (":");
						Status = commandBuffer.ToString ();
						return;
					
					case 'i':
					case 'a':
						Status = "-- INSERT --";
						state = State.Insert;
						return;
						
					case 'R':
						Caret.IsInInsertMode = false;
						Status = "-- REPLACE --";
						state = State.Replace;
						return;
						
					case 'v':
						Status = "-- VISUAL --";
						state = State.Visual;
						return;
						
					case 'd':
						Status = "d";
						state = State.Delete;
						return;
						
					case 'O':
						RunAction (ViActions.NewLineAbove);
						goto case 'i';
						
					case 'o':
						RunAction (ViActions.NewLineBelow);
						goto case 'i';
					}
				}
				
				action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (action == null)
					action = ViActionMaps.GetDirectionKeyAction (key, modifier);
				if (action == null)
					action = ViActionMaps.GetCommandCharAction ((char)unicodeKey);
				
				if (action != null)
					RunAction (action);
				
				return;
				
			case State.Delete:
				if (((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0 
				     && key == Gdk.Key.d))
				{
					action = DeleteActions.CaretLine;
				} else {
					action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
					if (action == null)
						action = ViActionMaps.GetDirectionKeyAction (key, modifier);
					if (action != null)
						action = DeleteActions.FromMoveAction (action);
				}
				
				if (action != null) {
					RunAction (action);
				}
				
				state = State.Normal;
				return;
				
			case State.Insert:
			case State.Replace:
				action = ViActionMaps.GetInsertKeyAction (key, modifier);
				if (action == null)
					action = ViActionMaps.GetDirectionKeyAction (key, modifier);
				
				if (action != null)
					RunAction (action);
				else if (unicodeKey != 0)
					InsertCharacter (unicodeKey);
				
				return;
				
			case State.Visual:
				action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (action == null)
					action = ViActionMaps.GetDirectionKeyAction (key, modifier);
				
				if (action != null)
					RunAction (SelectionActions.FromMoveAction (action));
				
				return;
				
			case State.Command:
				if (key == Gdk.Key.Return || key == Gdk.Key.KP_Enter) {
					Status = RunExCommand (commandBuffer.ToString ());
					commandBuffer.Length = 0;
					state = State.Normal;
				} else if (unicodeKey != 0) {
					commandBuffer.Append ((char)unicodeKey);
					Status = commandBuffer.ToString ();
				}
				return;
			}
		}
		
		enum State {
			Normal = 0,
			Command,
			Delete,
			Visual,
			VisualLine,
			Insert,
			Replace
		}
	}
}
