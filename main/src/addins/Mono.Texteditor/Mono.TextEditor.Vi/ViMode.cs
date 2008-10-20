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
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Mono.TextEditor.Vi
{
	
	
	public class ViEditMode : EditMode
	{
		State state;
		string status;
		static string lastPattern;
		static string lastReplacement;

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

			switch (command[1]) {
			case 's':
				if (2 == command.Length) {
					if (null == lastPattern || null == lastReplacement)
						return "No stored pattern.";
						
					// Perform replacement with stored stuff
					command = string.Format (":s/{0}/{1}/", lastPattern, lastReplacement);
				}
	
				System.Text.RegularExpressions.Match match = Regex.Match (command, @"^:s(?<sep>.)(?<pattern>.+?)\k<sep>(?<replacement>.*?)(\k<sep>(?<trailer>i?))?$", RegexOptions.Compiled);
				if (!(match.Success && match.Groups["pattern"].Success && match.Groups["replacement"].Success))
					break;
	
				return RegexReplace (match);
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
		
		void Reset (string status)
		{
			ResetEditorState (Data);
			state = State.Normal;
			commandBuffer.Length = 0;
			Status = status;
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.Escape || (key == Gdk.Key.c && (modifier & Gdk.ModifierType.ControlMask) != 0)) {
				Reset (string.Empty);
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
					
					case 'A':
						RunAction (CaretMoveActions.LineEnd);
						goto case 'i';
						
					case 'I':
						RunAction (CaretMoveActions.LineStart);
						goto case 'i';
					
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

					case 'V':
						Status = "-- VISUAL LINE --";
						Data.SetSelectLines (Caret.Line, Caret.Line);
						state = State.VisualLine;
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
						
					case 'r':
						RunAction (SelectionActions.MoveRight);
						state = State.WriteChar;
						return;
						
					case 'c':
						Status = "c";
						state = State.Change;
						return;
						
					case 'x':
						Status = string.Empty;
						RunAction (DeleteActions.Delete);
						return;
						
					case 'X':
						Status = string.Empty;
						RunAction (DeleteActions.Backspace);
						return;
						
					case 'D':
						RunAction (DeleteActions.FromMoveAction (CaretMoveActions.LineEnd));
						return;
						
					case 'C':
						RunAction (DeleteActions.FromMoveAction (CaretMoveActions.LineEnd));
						goto case 'i';
						
					case '>':
						Status = ">";
						state = State.Indent;
						return;
						
					case '<':
						Status = "<";
						state = State.Unindent;
						return;
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
					Reset ("");
				} else {
					Reset ("Unrecognised motion");
				}
				
				return;
				
			case State.Change:
				//copied from delete action
				if (((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0 
				     && key == Gdk.Key.c))
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
					Reset ("--INSERT");
					state = State.Insert;
				} else {
					Reset ("Unrecognised motion");
				}
				
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

			case State.VisualLine:
				action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (action == null) {
					action = ViActionMaps.GetDirectionKeyAction (key, modifier);
				}
				if (action != null) {
					RunAction (SelectionActions.LineActionFromMoveAction (action));
					return;
				}

				ApplyActionToSelection (modifier, unicodeKey);
				return;

			case State.Visual:
				action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (action == null) {
					action = ViActionMaps.GetDirectionKeyAction (key, modifier);
				}
				if (action != null) {
					RunAction (SelectionActions.FromMoveAction (action));
					return;
				}

				ApplyActionToSelection (modifier, unicodeKey);
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
				
			case State.WriteChar:
				if (unicodeKey != 0) {
					InsertCharacter ((char) unicodeKey);
					Reset (string.Empty);
				} else {
					Reset ("Keystroke was not a character");
				}
				return;
				
			case State.Indent:
				if (((modifier & (Gdk.ModifierType.ControlMask)) == 0 && ((char)unicodeKey) == '>'))
				{
					RunAction (MiscActions.IndentSelection);
					Reset ("");
					return;
				}
				
				action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (action == null)
					action = ViActionMaps.GetDirectionKeyAction (key, modifier);
				
				if (action != null) {
					RunAction (SelectionActions.FromMoveAction (action));
					RunAction (MiscActions.IndentSelection);
					Reset ("");
				} else {
					Reset ("Unrecognised motion");
				}
				return;
				
			case State.Unindent:
				if (((modifier & (Gdk.ModifierType.ControlMask)) == 0 && ((char)unicodeKey) == '<'))
				{
					RunAction (MiscActions.RemoveIndentSelection);
					Reset ("");
					return;
				}
				
				action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (action == null)
					action = ViActionMaps.GetDirectionKeyAction (key, modifier);
				
				if (action != null) {
					RunAction (SelectionActions.FromMoveAction (action));
					RunAction (MiscActions.RemoveIndentSelection);
					Reset ("");
				} else {
					Reset ("Unrecognised motion");
				}
				return;
			}
		}

		/// <summary>
		/// Runs an in-place replacement on the selection or the current line
		/// using the "pattern", "replacement", and "trailer" groups of match.
		/// </summary>
		public string RegexReplace (System.Text.RegularExpressions.Match match)
		{
			string line = null;
			ISegment segment = null;

			if (Data.IsSomethingSelected) {
				// Operate on selection
				line = Data.SelectedText;
				segment = Data.SelectionRange;
			} else {
				// Operate on current line
				segment = Editor.Document.GetLine (Caret.Line);
				line = Editor.Document.GetTextBetween (segment.Offset, segment.EndOffset);
			}

			// Set regex options
			RegexOptions options = RegexOptions.Multiline;
			if (match.Groups["trailer"].Success && "i" == match.Groups["trailer"].Value)
				options |= RegexOptions.IgnoreCase;

			// Mogrify group backreferences to .net-style references
			string replacement = Regex.Replace (match.Groups["replacement"].Value, @"\\([0-9]+)", "$$$1", RegexOptions.Compiled);
			replacement = Regex.Replace (replacement, "&", "$$$0", RegexOptions.Compiled);

			try {
				string newline = Regex.Replace (line, match.Groups["pattern"].Value, replacement, options);
				Editor.Document.Replace (segment.Offset, line.Length, newline);
				if (Data.IsSomethingSelected)
					Data.ClearSelection ();
				lastPattern = match.Groups["pattern"].Value;
				lastReplacement = replacement; 
			} catch (ArgumentException ae) {
				return string.Format("Replacement error: {0}", ae.Message);
			}

			return "Performed replacement.";
		}

		public void ApplyActionToSelection (Gdk.ModifierType modifier, uint unicodeKey)
		{
			if (Data.IsSomethingSelected && (modifier & (Gdk.ModifierType.ControlMask)) == 0) {
				switch ((char)unicodeKey) {
				case 'd':
					RunAction (DeleteActions.DeleteSelection);
					Reset ("Deleted selection");
					return;
				case 'c':
					RunAction (DeleteActions.DeleteSelection);
					state = State.Insert;
					Status = "-- INSERT --";
					return;
					
				case '>':
					RunAction (MiscActions.IndentSelection);
					Reset ("");
					return;
					
				case '<':
					RunAction (MiscActions.RemoveIndentSelection);
					Reset ("");
					return;

				case ':':
					commandBuffer.Append (":");
					Status = commandBuffer.ToString ();
					state = State.Command;
					break;

				}
			}
		}

		enum State {
			Normal = 0,
			Command,
			Delete,
			Visual,
			VisualLine,
			Insert,
			Replace,
			WriteChar,
			Change,
			Indent,
			Unindent
		}
	}
}
