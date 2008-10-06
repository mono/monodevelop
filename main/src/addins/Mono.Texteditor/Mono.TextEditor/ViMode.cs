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
using System.Collections.Generic;

namespace Mono.TextEditor
{
	
	
	public class ViMode : EditMode
	{
		State state;
		string status;
		
		Dictionary<int, EditAction> navMaps = new Dictionary<int, EditAction> ();
		Dictionary<int, EditAction> insertModeMaps = new Dictionary<int, EditAction> ();
		Dictionary<int, EditAction> commandMaps = new Dictionary<int, EditAction> ();
		
		public ViMode ()
		{
			InitNavMaps ();
			InitInsertMaps ();
			InitCommandMaps ();
		}
		
		void InitNavMaps ()
		{
			EditAction action;
			
			// ==== Left ====
			
			action = new CaretMoveLeft ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Left), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Left), action);
			navMaps.Add (GetKeyCode (Gdk.Key.h), action);
			
			action = new CaretMovePrevWord ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.b), action);
			
			// ==== Right ====
			
			action = new CaretMoveRight ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Right), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Right), action);
			navMaps.Add (GetKeyCode (Gdk.Key.l), action);
			
			action = new CaretMoveNextWord ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.w), action);
			
			// ==== Up ====
			
			action = new CaretMoveUp ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Up), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Up), action);
			navMaps.Add (GetKeyCode (Gdk.Key.k), action);
			
			action = new ScrollUpAction ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ControlMask), action);
			
			// ==== Down ====
			
			action = new CaretMoveDown ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Down), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Down), action);
			navMaps.Add (GetKeyCode (Gdk.Key.j), action);
			
			action = new ScrollDownAction ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ControlMask), action);
			
			// === Home ===
			
			//not strictly vi, but more useful IMO
			action = new CaretMoveHome ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Home), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Home), action);
			
			action = new CaretMoveToDocumentStart ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ControlMask), action);
			
			action = new CaretMoveLineStart ();
			navMaps.Add (GetKeyCode (Gdk.Key.Key_0), action);
			
			action = new CaretMoveFirstNonWhitespace ();
			navMaps.Add (GetKeyCode (Gdk.Key.asciicircum, Gdk.ModifierType.ShiftMask), action);
			
			// ==== End ====
			
			action = new CaretMoveEnd ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_End), action);
			navMaps.Add (GetKeyCode (Gdk.Key.End), action);
			navMaps.Add (GetKeyCode (Gdk.Key.dollar, Gdk.ModifierType.ShiftMask), action);
			
			action = new CaretMoveToDocumentEnd ();
			navMaps.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ControlMask), action);
			navMaps.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ControlMask), action);
		}
		
		void InitInsertMaps ()
		{
			EditAction action;
			
			// ==== Maps for insert mode ====
			
			insertModeMaps.Add (GetKeyCode (Gdk.Key.Tab), new InsertTab ());
			insertModeMaps.Add (GetKeyCode (Gdk.Key.ISO_Left_Tab, Gdk.ModifierType.ShiftMask), new RemoveTab ());
			
			action = new InsertNewLine ();
			insertModeMaps.Add (GetKeyCode (Gdk.Key.Return), action);
			insertModeMaps.Add (GetKeyCode (Gdk.Key.KP_Enter), action);
			
			action = new BackspaceAction ();
			insertModeMaps.Add (GetKeyCode (Gdk.Key.BackSpace), action);
			insertModeMaps.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ShiftMask), action);
			
			insertModeMaps.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ControlMask), new DeletePrevWord ());
			
			action = new DeleteAction ();
			insertModeMaps.Add (GetKeyCode (Gdk.Key.KP_Delete), action);
			insertModeMaps.Add (GetKeyCode (Gdk.Key.Delete), action);
			
			action = new DeleteNextWord ();
			insertModeMaps.Add (GetKeyCode (Gdk.Key.KP_Delete, Gdk.ModifierType.ControlMask), action);
			insertModeMaps.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ControlMask), action);
		}
		
		void InitCommandMaps ()
		{
			EditAction action;
			
			action = new UndoAction ();
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
		
		public event EventHandler StatusChanged;
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.Escape) {
				Data.ClearSelection ();
				if (!Caret.IsInInsertMode)
					Caret.IsInInsertMode = true;
				state = State.Normal;
				Status = string.Empty;
				return;
			}
			
			int keyCode;
			EditAction action;
			
			switch (state) {
			case State.Normal:
				switch (key) {
				case Gdk.Key.colon:
					state = State.Command;
					Status = ":";
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
					SelectionMoveLeft.StartSelection (Data);
					RunAction (action);
					SelectionMoveLeft.EndSelection (Data);
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
