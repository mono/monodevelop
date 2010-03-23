//
// EditMode.cs
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
	public abstract class EditMode
	{
		//NOTE: the behaviour of this class is actually stateless; these variables are used to make the API
		// friendlier for implementors
		TextEditorData textEditorData;
	//	string status;
		
		internal void InternalHandleKeypress (TextEditor editor, TextEditorData data, Gdk.Key key, 
		                                      uint unicodeChar, Gdk.ModifierType modifier)
		{
			this.Editor = editor; 
			this.textEditorData = data;
			
			HandleKeypress (key, unicodeChar, modifier);
			
			//make sure that nothing funny goes on when the mode should have finished
			this.textEditorData = null;
			this.Editor = null;
		}
		
		internal virtual void InternalSelectionChanged (TextEditor editor, TextEditorData data)
		{
			// only trigger SelectionChanged when event is a result of external stimuli, i.e. when 
			// not already running HandleKeypress
			if (this.Editor == null) {
				this.Editor = editor; 
				this.textEditorData = data;
				SelectionChanged ();
				this.textEditorData = null;
				this.Editor = null;
			}
		}
		
		protected virtual void SelectionChanged ()
		{
		}
		
		protected Caret Caret { get { return textEditorData.Caret; } }
		protected Document Document { get { return textEditorData.Document; } }
		public TextEditor Editor { get; set;  }
		protected TextEditorData Data { get { return textEditorData; } }
		
		protected abstract void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier);
		
		public virtual bool WantsToPreemptIM {
			get { return false; }
		}
		
		protected void InsertCharacter (uint unicodeKey)
		{
			if (!Editor.GetTextEditorData ().CanEdit (Data.Caret.Line))
				return;
			Editor.HideMouseCursor ();
			Document.BeginAtomicUndo ();
			if (textEditorData.IsSomethingSelected && textEditorData.MainSelection.SelectionMode == SelectionMode.Block) {
				textEditorData.Caret.PreserveSelection = true;
				if (!textEditorData.MainSelection.IsDirty) {
					textEditorData.DeleteSelectedText (false);
					textEditorData.MainSelection.IsDirty = true;
				}
			} else {
				textEditorData.DeleteSelectedText ();
			}
			
			char ch = (char)unicodeKey;
			if (!char.IsControl (ch) && textEditorData.CanEdit (Caret.Line)) {
				LineSegment line = Document.GetLine (Caret.Line);
				if (Caret.IsInInsertMode || Caret.Column >= line.EditableLength) {
					string text = Caret.Column > line.EditableLength ? textEditorData.GetVirtualSpaces (Caret.Line, Caret.Column) + ch.ToString () : ch.ToString ();
					int offset = Caret.Offset;
					if (textEditorData.IsSomethingSelected && textEditorData.MainSelection.SelectionMode == SelectionMode.Block) {
						int length = 0;
						for (int lineNumber = textEditorData.MainSelection.MinLine; lineNumber <= textEditorData.MainSelection.MaxLine; lineNumber++) {
							length = textEditorData.Insert (textEditorData.Document.GetLine (lineNumber).Offset + Caret.Column, text);
						}
						Caret.Column += length - 1;
						textEditorData.MainSelection.Lead = new DocumentLocation (textEditorData.MainSelection.Lead.Line, Caret.Column + 1);
						textEditorData.MainSelection.IsDirty = true;
						Document.CommitMultipleLineUpdate (textEditorData.MainSelection.MinLine, textEditorData.MainSelection.MaxLine);
					} else {
						int length = textEditorData.Insert (Caret.Offset, text);
						Caret.Offset = offset + length - 1;
					}
				} else {
					int length = textEditorData.Replace (Caret.Offset, 1, ch.ToString ());
					if (length > 1)
						Caret.Offset += length - 1;
				}
				// That causes unnecessary redraws:
				//				bool autoScroll = Caret.AutoScrollToCaret;
				Caret.Column++;
				//				Caret.AutoScrollToCaret = autoScroll;
				//				if (autoScroll)
				//					Editor.ScrollToCaret ();
				//				Document.RequestUpdate (new LineUpdate (Caret.Line));
				//				Document.CommitDocumentUpdate ();
			}
			if (textEditorData.IsSomethingSelected && textEditorData.MainSelection.SelectionMode == SelectionMode.Block)
				textEditorData.Caret.PreserveSelection = false;
			Document.EndAtomicUndo ();
			Document.OptimizeTypedUndo ();
		}
		public virtual void RemovedFromTextEditor ()
		{
		}
		protected void RunAction (Action<TextEditorData> action)
		{
			Editor.HideMouseCursor ();
			try {
				Document.BeginAtomicUndo ();
				action (this.textEditorData);
				if (Document != null) // action may have closed the document.
					Document.EndAtomicUndo ();
			} catch (Exception e) {
				Console.WriteLine ("Error while executing action " + action.ToString () + " :" + e);
			}
		}
		
		protected void RunActions (Action<TextEditorData> action1, Action<TextEditorData> action2)
		{
			Editor.HideMouseCursor ();
			try {
				Document.BeginAtomicUndo ();
				action1 (this.textEditorData);
				action2 (this.textEditorData);
				Document.EndAtomicUndo ();
			} catch (Exception e) {
				Console.WriteLine ("Error while executing actions " + action1.ToString () + 
				                   " & " + action2.ToString () + ": " + e);
			}
		
		}
		static Dictionary<Gdk.Key, Gdk.Key> keyMappings = new Dictionary<Gdk.Key, Gdk.Key> ();
		static EditMode ()
		{
			for (char ch = 'a'; ch <= 'z'; ch++) {
				keyMappings[(Gdk.Key)ch] = (Gdk.Key)(ch -'a' + 'A');
			}
		}
		
		
		public virtual bool PreemptIM (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			return false;
		}
		
		public static int GetKeyCode (Gdk.Key key)
		{
			return (int)(keyMappings.ContainsKey (key) ? keyMappings[key] : key);
		}
		
		public static int GetKeyCode (Gdk.Key key, Gdk.ModifierType modifier)
		{
			uint m =       (uint)(((modifier & Gdk.ModifierType.ControlMask) != 0)? 1 : 0);
			m = (m << 1) | (uint)(((modifier & Gdk.ModifierType.ShiftMask)   != 0)? 1 : 0);
			m = (m << 1) | (uint)(((modifier & Gdk.ModifierType.MetaMask)    != 0)? 1 : 0);
			m = (m << 1) | (uint)(((modifier & Gdk.ModifierType.Mod1Mask)    != 0)? 1 : 0);
			m = (m << 1) | (uint)(((modifier & Gdk.ModifierType.SuperMask)   != 0)? 1 : 0);
			
			return GetKeyCode (key) | (int)(m << 16);
		}
		
	}
}
