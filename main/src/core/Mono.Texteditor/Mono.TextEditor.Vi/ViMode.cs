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
		bool searchBackward;
		static string lastPattern;
		static string lastReplacement;
		State state;

		StringBuilder commandBuffer = new StringBuilder ();
		
		public virtual string Status { get; protected set; }
		
		protected virtual string RunExCommand (string command)
		{
			switch (command[0]) {
			case ':':
				if (2 > command.Length)
					break;
					
				int line;
				if (int.TryParse (command.Substring (1), out line)) {
					if (line < 0 || line > Data.Document.LineCount) {
						return "Invalid line number.";
					} else if (line == 0) {
						RunAction (CaretMoveActions.ToDocumentStart);
						return "Jumped to beginning of document.";
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
					
				case '$':
					if (command.Length == 2) {
						RunAction (CaretMoveActions.ToDocumentEnd);
						return "Jumped to end of document.";
					}
					break;	
				}
				break;

			case '?':
			case '/':
				searchBackward = ('?' == command[0]);
				if (1 < command.Length) {
					Editor.HighlightSearchPattern = true;
					Editor.SearchEngine = new RegexSearchEngine ();
					Editor.SearchPattern = command.Substring (1);
				}
				return Search ();
			}

			return "Command not recognised";
		}
		
		public override bool WantsToPreemptIM {
			get {
				return state != State.Insert && state != State.Replace;
			}
		}
		
		protected override void SelectionChanged ()
		{
			if (Data.IsSomethingSelected) {
				state = ViEditMode.State.Visual;
				Status = "-- VISUAL --";
			} else if (state == State.Visual && !Data.IsSomethingSelected) {
				Reset ("");
			}
		}
		
		void CheckVisualMode ()
		{
			if (state == ViEditMode.State.Visual || state == ViEditMode.State.Visual) {
				if (!Data.IsSomethingSelected)
					state = ViEditMode.State.Normal;
			} else {
				if (Data.IsSomethingSelected) {
					state = ViEditMode.State.Visual;
					Status = "-- VISUAL --";
				}
			}
		}
		
		void ResetEditorState (TextEditorData data)
		{
			if (data == null)
				return;
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
		
		protected virtual Action<TextEditorData> GetInsertAction (Gdk.Key key, Gdk.ModifierType modifier)
		{
			return ViActionMaps.GetInsertKeyAction (key, modifier) ??
				ViActionMaps.GetDirectionKeyAction (key, modifier);
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.Escape || (key == Gdk.Key.c && (modifier & Gdk.ModifierType.ControlMask) != 0)) {
				Reset (string.Empty);
				return;
			}
			
			Action<TextEditorData> action = null;
			
			switch (state) {
			case State.Normal:
				if (((modifier & (Gdk.ModifierType.ControlMask)) == 0)) {
					switch ((char)unicodeKey) {
					case '?':
					case '/':
					case ':':
						state = State.Command;
						commandBuffer.Append ((char)unicodeKey);
						Status = commandBuffer.ToString ();
						return;
					
					case 'A':
						RunAction (CaretMoveActions.LineEnd);
						goto case 'i';
						
					case 'I':
						RunAction (CaretMoveActions.LineFirstNonWhitespace);
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
						if (Data.MainSelection == null)
							Data.MainSelection = new Selection (Caret.Location, Caret.Location);
						Data.MainSelection.Anchor = Caret.Location;
						state = State.Visual;
						return;
						
					case 'd':
						Status = "d";
						state = State.Delete;
						return;
						
					case 'y':
						Status = "y";
						state = State.Yank;
						return;

					case 'Y':
						state = State.Yank;
						HandleKeypress (Gdk.Key.y, (int)'y', Gdk.ModifierType.None);
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
						if (!Data.IsSomethingSelected)
							RunActions (SelectionActions.FromMoveAction (CaretMoveActions.Right), ClipboardActions.Cut);
						else
							RunAction (ClipboardActions.Cut);
						return;
						
					case 'X':
						Status = string.Empty;
						if (!Data.IsSomethingSelected && 0 < Caret.Offset)
							RunActions (SelectionActions.FromMoveAction (CaretMoveActions.Left), ClipboardActions.Cut);
						else
							RunAction (ClipboardActions.Cut);
						return;
						
					case 'D':
						RunActions (SelectionActions.FromMoveAction (CaretMoveActions.LineEnd), ClipboardActions.Cut);
						return;
						
					case 'C':
						RunActions (SelectionActions.FromMoveAction (CaretMoveActions.LineEnd), ClipboardActions.Cut);
						goto case 'i';
						
					case '>':
						Status = ">";
						state = State.Indent;
						return;
						
					case '<':
						Status = "<";
						state = State.Unindent;
						return;
					case 'n':
						Search ();
						return;
					case 'N':
						searchBackward = !searchBackward;
						Search ();
						searchBackward = !searchBackward;
						return;
					case 'p':
						PasteAfter (false);
						return;
					case 'P':
						PasteBefore (false);
						return;
					case 's':
						if (!Data.IsSomethingSelected)
							RunAction (SelectionActions.FromMoveAction (CaretMoveActions.Right));
						RunAction (ClipboardActions.Cut);
						goto case 'i';
					case 'S':
						if (!Data.IsSomethingSelected)
							RunAction (SelectionActions.LineActionFromMoveAction (CaretMoveActions.LineEnd));
						else Data.SetSelectLines (Data.MainSelection.Anchor.Line, Data.Caret.Line);
						RunAction (ClipboardActions.Cut);
						goto case 'i';
						
					case 'g':
						Status = "g";
						state = State.G;
						return;
						
					case 'H':
						Caret.Line = System.Math.Max (0, Editor.VisualToDocumentLocation (0, Editor.LineHeight - 1).Line);
						return;
					case 'J':
						RunAction (ViActions.Join);
						return;
					case 'L':
						int line = Editor.VisualToDocumentLocation (0, Editor.Allocation.Height - Editor.LineHeight * 2 - 2).Line;
						if (line < 0)
							line = Document.LineCount - 1;
						Caret.Line = line;
						return;
					case 'M':
						line = Editor.VisualToDocumentLocation (0, Editor.Allocation.Height/2).Line;
						if (line < 0)
							line = Document.LineCount - 1;
						Caret.Line = line;
						return;
						
					case '~':
						RunAction (ViActions.ToggleCase);
						return;
						
					case 'z':
						Status = "z";
						state = State.Fold;
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
				
				//undo/redo may leave MD with a selection mode without activating visual mode
				CheckVisualMode ();
				return;
				
			case State.Delete:
				if (((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0 
				     && key == Gdk.Key.d))
				{
					action = SelectionActions.LineActionFromMoveAction (CaretMoveActions.LineEnd);
				} else {
					action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
					if (action == null)
						action = ViActionMaps.GetDirectionKeyAction (key, modifier);
					if (action != null)
						action = SelectionActions.FromMoveAction (action);
				}
				
				if (action != null) {
					RunActions (action, ClipboardActions.Cut);
					Reset ("");
				} else {
					Reset ("Unrecognised motion");
				}
				
				return;

			case State.Yank:
				int offset = Caret.Offset;
				bool lineyank = false;
				
				if (((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0 
				     && key == Gdk.Key.y))
				{
					action = SelectionActions.LineActionFromMoveAction (CaretMoveActions.LineEnd);
					lineyank = true;
				} else {
					action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
					if (action == null)
						action = ViActionMaps.GetDirectionKeyAction (key, modifier);
					if (action != null)
						action = SelectionActions.FromMoveAction (action);
				}
				
				if (action != null) {
					RunAction (action);
					if (Data.IsSomethingSelected && !lineyank)
						offset = Data.SelectionRange.Offset;
					RunAction (ClipboardActions.Copy);
					Reset (string.Empty);
				} else {
					Reset ("Unrecognised motion");
				}
				Caret.Offset = offset;
				
				return;
				
			case State.Change:
				//copied from delete action
				if (((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask)) == 0 
				     && key == Gdk.Key.c))
				{
					action = SelectionActions.LineActionFromMoveAction (CaretMoveActions.LineEnd);
				} else {
					action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
					if (action == null)
						action = ViActionMaps.GetDirectionKeyAction (key, modifier);
					if (action != null)
						action = SelectionActions.FromMoveAction (action);
				}
				
				if (action != null) {
					RunActions (action, ClipboardActions.Cut);
					Reset ("-- INSERT --");
					state = State.Insert;
				} else {
					Reset ("Unrecognised motion");
				}
				
				return;
				
			case State.Insert:
			case State.Replace:
				action = GetInsertAction (key, modifier);
				
				
				if (action != null)
					RunAction (action);
				else if (unicodeKey != 0)
					InsertCharacter (unicodeKey);
				
				return;

			case State.VisualLine:
				switch ((char)unicodeKey) {
				case 'p':
					PasteAfter (true);
					return;
				case 'P':
					PasteBefore (true);
					return;
				}
				action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (action == null) {
					action = ViActionMaps.GetDirectionKeyAction (key, modifier);
				}
				if (action == null) {
					action = ViActionMaps.GetCommandCharAction ((char)unicodeKey);
				}
				if (action != null) {
					RunAction (SelectionActions.LineActionFromMoveAction (action));
					return;
				}

				ApplyActionToSelection (modifier, unicodeKey);
				return;

			case State.Visual:
				switch ((char)unicodeKey) {
				case 'p':
					PasteAfter (false);
					return;
				case 'P':
					PasteBefore (false);
					return;
				}
				action = ViActionMaps.GetNavCharAction ((char)unicodeKey);
				if (action == null) {
					action = ViActionMaps.GetDirectionKeyAction (key, modifier);
				}
				if (action == null) {
					action = ViActionMaps.GetCommandCharAction ((char)unicodeKey);
				}
				if (action != null) {
					RunAction (SelectionActions.FromMoveAction (action));
					return;
				}

				ApplyActionToSelection (modifier, unicodeKey);
				return;
				
			case State.Command:
				switch (key) {
				case Gdk.Key.Return:
				case Gdk.Key.KP_Enter:
					Status = RunExCommand (commandBuffer.ToString ());
					commandBuffer.Length = 0;
					state = State.Normal;
					break;
				case Gdk.Key.BackSpace:
				case Gdk.Key.Delete:
				case Gdk.Key.KP_Delete:
					if (0 < commandBuffer.Length) {
						commandBuffer.Remove (commandBuffer.Length-1, 1);
						Status = commandBuffer.ToString ();
						if (0 == commandBuffer.Length)
							Reset (Status);
					}
					break;
				default:
					if(unicodeKey != 0) {
						commandBuffer.Append ((char)unicodeKey);
						Status = commandBuffer.ToString ();
					}
					break;
				}
				return;
				
			case State.WriteChar:
				if (unicodeKey != 0) {
					int   roffset = Data.SelectionRange.Offset;
					InsertCharacter ((char) unicodeKey);
					Caret.Offset = roffset;
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
					RunActions (SelectionActions.FromMoveAction (action), MiscActions.IndentSelection);
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
					RunActions (SelectionActions.FromMoveAction (action), MiscActions.RemoveIndentSelection);
					Reset ("");
				} else {
					Reset ("Unrecognised motion");
				}
				return;

			case State.G:
				if (((modifier & (Gdk.ModifierType.ControlMask)) == 0)) {
					switch ((char)unicodeKey) {
					case 'g':
						Caret.Offset = 0;
						Reset ("");
						return;
					}
				}
				Reset ("Unknown command");
				return;
			case State.Fold:
				if (((modifier & (Gdk.ModifierType.ControlMask)) == 0)) {
					switch ((char)unicodeKey) {
						case 'A':
						// Recursive fold toggle
							action = FoldActions.ToggleFoldRecursive;
							break;
						case 'C':
						// Recursive fold close
							action = FoldActions.CloseFoldRecursive;
							break;
						case 'M':
						// Close all folds
							action = FoldActions.CloseAllFolds;
							break;
						case 'O':
						// Recursive fold open
							action = FoldActions.OpenFoldRecursive;
							break;
						case 'R':
						// Expand all folds
							action = FoldActions.OpenAllFolds;
							break;
						case 'a':
						// Fold toggle
							action = FoldActions.ToggleFold;
							break;
						case 'c':
						// Fold close
							action = FoldActions.CloseFold;
							break;
						case 'o':
						// Fold open
							action = FoldActions.OpenFold;
							break;
						default:
							Reset ("Unknown command");
							break;
					}
					
					if (null != action) {
						RunAction (action);
						Reset (string.Empty);
					}
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
				Editor.GetTextEditorData ().Replace (segment.Offset, line.Length, newline);
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
				case 'x':
				case 'd':
					RunAction (ClipboardActions.Cut);
					Reset ("Deleted selection");
					return;
				case 'y':
					int offset = Data.SelectionRange.Offset;
					RunAction (ClipboardActions.Copy);
					Reset ("Yanked selection");
					Caret.Offset = offset;
					return;
				case 's':
				case 'c':
					RunAction (ClipboardActions.Cut);
					state = State.Insert;
					Status = "-- INSERT --";
					return;
				case 'S':
					Data.SetSelectLines (Data.MainSelection.Anchor.Line, Data.Caret.Line);
					goto case 'c';
					
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
				case 'J':
					RunAction (ViActions.Join);
					Reset ("");
					return;
					
				case '~':
					RunAction (ViActions.ToggleCase);
					Reset ("");
					return;
				}
			}
		}

		private string Search()
		{
			SearchResult result = searchBackward?
				Editor.SearchBackward (Caret.Offset):
				Editor.SearchForward (Caret.Offset+1);
			Editor.HighlightSearchPattern = (null != result);
			if (null == result) 
				return string.Format ("Pattern not found: '{0}'", Editor.SearchPattern);
			else Caret.Offset = result.Offset;
		
			return string.Empty;
		}

		/// <summary>
		/// Pastes the selection after the caret,
		/// or replacing an existing selection.
		/// </summary>
		private void PasteAfter (bool linemode)
		{
			TextEditorData data = Data;
			
			Gtk.Clipboard.Get (ClipboardActions.CopyOperation.CLIPBOARD_ATOM).RequestText 
				(delegate (Gtk.Clipboard cb, string contents) {
				if (contents == null)
					return;
				if (contents.EndsWith ("\r") || contents.EndsWith ("\n")) {
					// Line mode paste
					if (data.IsSomethingSelected) {
						// Replace selection
						RunAction (ClipboardActions.Cut);
						data.InsertAtCaret (data.EolMarker);
						int offset = data.Caret.Offset;
						data.InsertAtCaret (contents);
						if (linemode) {
							// Existing selection was also in line mode
							data.Caret.Offset = offset;
							RunAction (DeleteActions.FromMoveAction (CaretMoveActions.Left));
						}
						RunAction (CaretMoveActions.LineStart);
					} else {
						// Paste on new line
						RunAction (ViActions.NewLineBelow);
						RunAction (DeleteActions.FromMoveAction (CaretMoveActions.LineStart));
						data.InsertAtCaret (contents);
						RunAction (DeleteActions.FromMoveAction (CaretMoveActions.Left));
						RunAction (CaretMoveActions.LineStart);
					}
				} else {
					// Inline paste
					if (data.IsSomethingSelected) 
						RunAction (ClipboardActions.Cut);
					else if (data.Caret.Offset < data.Document.GetLine (data.Caret.Line).EndOffset)
						RunAction (CaretMoveActions.Right);
					int offset = data.Caret.Offset;
					data.InsertAtCaret (contents);
					data.Caret.Offset = offset;
				}
				Reset (string.Empty);
			});
		}

		/// <summary>
		/// Pastes the selection before the caret,
		/// or replacing an existing selection.
		/// </summary>
		private void PasteBefore (bool linemode)
		{
			TextEditorData data = Data;
			
			Gtk.Clipboard.Get (ClipboardActions.CopyOperation.CLIPBOARD_ATOM).RequestText 
				(delegate (Gtk.Clipboard cb, string contents) {
				if (contents == null)
					return;
				if (contents.EndsWith ("\r") || contents.EndsWith ("\n")) {
					// Line mode paste
					if (data.IsSomethingSelected) {
						// Replace selection
						RunAction (ClipboardActions.Cut);
						data.InsertAtCaret (data.EolMarker);
						int offset = data.Caret.Offset;
						data.InsertAtCaret (contents);
						if (linemode) {
							// Existing selection was also in line mode
							data.Caret.Offset = offset;
							RunAction (DeleteActions.FromMoveAction (CaretMoveActions.Left));
						}
						RunAction (CaretMoveActions.LineStart);
					} else {
						// Paste on new line
						RunAction (ViActions.NewLineAbove);
						RunAction (DeleteActions.FromMoveAction (CaretMoveActions.LineStart));
						data.InsertAtCaret (contents);
						RunAction (DeleteActions.FromMoveAction (CaretMoveActions.Left));
						RunAction (CaretMoveActions.LineStart);
					}
				} else {
					// Inline paste
					if (data.IsSomethingSelected) 
						RunAction (ClipboardActions.Cut);
					else if (Caret.Offset > data.Document.GetLine (Caret.Line).Offset)
						RunAction (CaretMoveActions.Left);
					int offset = Caret.Offset;
					data.InsertAtCaret (contents);
					Caret.Offset = offset;
				}
				Reset (string.Empty);
			});
		}

		enum State {
			Normal = 0,
			Command,
			Delete,
			Yank,
			Visual,
			VisualLine,
			Insert,
			Replace,
			WriteChar,
			Change,
			Indent,
			Unindent,
			G,
			Fold
		}
	}
}
