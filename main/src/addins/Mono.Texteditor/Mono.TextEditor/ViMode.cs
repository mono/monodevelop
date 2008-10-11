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

namespace Mono.TextEditor
{
	
	
	public class ViMode : EditMode
	{
		State state;
		string status;
		StringBuilder commandBuffer = new StringBuilder ();
		
		Dictionary<int, Action<TextEditorData>> navMaps = new Dictionary<int, Action<TextEditorData>> ();
		Dictionary<int, Action<TextEditorData>> insertModeMaps = new Dictionary<int, Action<TextEditorData>> ();
		Dictionary<int, Action<TextEditorData>> commandMaps = new Dictionary<int, Action<TextEditorData>> ();
		
		public ViMode ()
		{
			InitNavMaps ();
			InitInsertMaps ();
			InitCommandMaps ();
		}
		
		void InitNavMaps ()
		{
			Action<TextEditorData> action;
			
			// ==== Left ====
			
			action = CaretMoveActions.Left;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Left), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Left), action);
			navMaps.Add (GetKeyCode (Gdk.Key.h), action);
			
			action = CaretMoveActions.PreviousWord;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.b), action);
			
			// ==== Right ====
			
			action = CaretMoveActions.Right;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Right), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Right), action);
			navMaps.Add (GetKeyCode (Gdk.Key.l), action);
			
			action = CaretMoveActions.NextWord;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.w), action);
			
			// ==== Up ====
			
			action = CaretMoveActions.Up;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Up), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Up), action);
			navMaps.Add (GetKeyCode (Gdk.Key.k), action);
			
			action = ScrollActions.Up;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ControlMask), action);

			// combination usually bound at IDE level
			action = CaretMoveActions.PageUp;
			navMaps.Add (GetKeyCode (Gdk.Key.u, Gdk.ModifierType.ControlMask), action);
			
			// ==== Down ====
			
			action = CaretMoveActions.Down;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Down), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Down), action);
			navMaps.Add (GetKeyCode (Gdk.Key.j), action);
			
			action = ScrollActions.Down;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ControlMask), action);

			// combination usually bound at IDE level
			action = CaretMoveActions.PageDown;
			navMaps.Add (GetKeyCode (Gdk.Key.d, Gdk.ModifierType.ControlMask), action);

			// ==== Editing ====

			action = MiscActions.GotoMatchingBracket;
			navMaps.Add (GetKeyCode (Gdk.Key.percent, Gdk.ModifierType.ShiftMask), action);
			
			// === Home ===
			
			//not strictly vi, but more useful IMO
			action = CaretMoveActions.LineHome;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Home), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Home), action);
			
			action = CaretMoveActions.ToDocumentStart;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ControlMask), action);
			
			action = CaretMoveActions.LineStart;
			navMaps.Add (GetKeyCode (Gdk.Key.Key_0), action);
			
			action = CaretMoveActions.LineFirstNonWhitespace;
			navMaps.Add (GetKeyCode (Gdk.Key.asciicircum, Gdk.ModifierType.ShiftMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.underscore, Gdk.ModifierType.ShiftMask), action);
			
			// ==== End ====
			
			action = CaretMoveActions.LineEnd;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_End), action);
			navMaps.Add (GetKeyCode (Gdk.Key.End), action);
			navMaps.Add (GetKeyCode (Gdk.Key.dollar, Gdk.ModifierType.ShiftMask), action);
			
			action = CaretMoveActions.ToDocumentEnd;
			navMaps.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.G, Gdk.ModifierType.ShiftMask), action);
		}
		
		void InitInsertMaps ()
		{
			Action<TextEditorData> action;
			
			// ==== Maps for insert mode ====
			
			insertModeMaps.Add (GetKeyCode (Gdk.Key.Tab), MiscActions.InsertTab);
			insertModeMaps.Add (GetKeyCode (Gdk.Key.ISO_Left_Tab, Gdk.ModifierType.ShiftMask), MiscActions.RemoveTab);
			
			action = MiscActions.InsertNewLine;
			insertModeMaps.Add (GetKeyCode (Gdk.Key.Return), action);
			insertModeMaps.Add (GetKeyCode (Gdk.Key.KP_Enter), action);
			
			action = DeleteActions.Backspace;
			insertModeMaps.Add (GetKeyCode (Gdk.Key.BackSpace), action);
			insertModeMaps.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ShiftMask), action);
			
			insertModeMaps.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ControlMask), DeleteActions.PreviousWord);
			
			action = DeleteActions.Delete;
			insertModeMaps.Add (GetKeyCode (Gdk.Key.KP_Delete), action);
			insertModeMaps.Add (GetKeyCode (Gdk.Key.Delete), action);
			
			action = DeleteActions.NextWord;
			insertModeMaps.Add (GetKeyCode (Gdk.Key.KP_Delete, Gdk.ModifierType.ControlMask), action);
			insertModeMaps.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ControlMask), action);
		}
		
		void InitCommandMaps ()
		{
			Action<TextEditorData> action;
			
			action = MiscActions.Undo;
			commandMaps.Add (GetKeyCode (Gdk.Key.u), action);
		}
		
		public string Status {
			get { return status; }
			private set {
				status = value;
				OnStatusChanged ();
			}
		}
		
		protected virtual void OnStatusChanged ()
		{
			if (StatusChanged != null)
				StatusChanged (this, EventArgs.Empty);
		}
		
		protected virtual string RunCommand (string command)
		{
			return "Command not recognised";
		}
		
		public event EventHandler StatusChanged;
		
		public override bool WantsToPreemptIM {
			get {
				return state != State.Insert && state != State.Replace;
			}
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.Escape || (key == Gdk.Key.c && (modifier & Gdk.ModifierType.ControlMask) != null)) {
				Data.ClearSelection ();
				if (!Caret.IsInInsertMode)
					Caret.IsInInsertMode = true;
				state = State.Normal;
				commandBuffer.Length = 0;
				Status = string.Empty;
				return;
			}
			
			int keyCode;
			Action<TextEditorData> action;
			
			switch (state) {
			case State.Normal:
				switch (key) {
				case Gdk.Key.colon:
					state = State.Command;
					commandBuffer.Append (":");
					Status = commandBuffer.ToString ();
					return;
					
				case Gdk.Key.i:
				case Gdk.Key.a:
					Status = "-- INSERT --";
					state = State.Insert;
					return;
					
				case Gdk.Key.R:
					Caret.IsInInsertMode = false;
					Status = "-- REPLACE --";
					state = State.Replace;
					return;
					
				case Gdk.Key.v:
					Status = "-- VISUAL --";
					state = State.Visual;
					return;
				}
				
				keyCode = GetKeyCode (key, modifier);
				if (navMaps.TryGetValue (keyCode, out action))
					RunAction (action);
				else if (commandMaps.TryGetValue (keyCode, out action))
					RunAction (action);
				
				return;
				
			case State.Insert:
			case State.Replace:
				keyCode = GetKeyCode (key, modifier);
				if (insertModeMaps.TryGetValue (keyCode, out action))
				    RunAction (action);
				else if (unicodeKey != 0)
					InsertCharacter (unicodeKey);
				else if (navMaps.TryGetValue (keyCode, out action)) {
					RunAction (action);
				}
				return;
				
			case State.Visual:
				keyCode = GetKeyCode (key, modifier);
				if (navMaps.TryGetValue (keyCode, out action)) {
					SelectionActions.Select (Data, action);
				}
				return;
				
			case State.Command:
				if (key == Gdk.Key.Return || key == Gdk.Key.KP_Enter) {
					Status = RunCommand (commandBuffer.ToString ());
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
			Visual,
			VisualLine,
			Insert,
			Replace
		}
	}
}
