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

namespace Mono.TextEditor
{
	
	
	public abstract class EditMode
	{
		//NOTE: the behaviour of this class is actually stateless; these variables are used to make the API
		// friendlier for implementors
		TextEditorData textEditorData;
		TextEditor editor;
		string status;
		
		internal void InternalHandleKeypress (TextEditor editor, TextEditorData data, Gdk.Key key, 
		                                      uint unicodeKey, Gdk.ModifierType modifier)
		{
			this.editor = editor; 
			this.textEditorData = data;
			
			HandleKeypress (key, unicodeKey, modifier);
			
			//make sure that nothing funny goes on when the mode should have finished
			this.textEditorData = null;
			this.editor = null;
		}
		
		protected Caret Caret { get { return textEditorData.Caret; } }
		protected Document Document { get { return textEditorData.Document; } }
		protected TextEditor Editor { get { return editor; } }
		protected TextEditorData Data { get { return textEditorData; } }
		
		protected abstract void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier);
		
		protected void InsertCharacter (uint unicodeKey)
		{
			Document.BeginAtomicUndo ();
			if (textEditorData.CanEditSelection)
				textEditorData.DeleteSelectedText ();
			char ch = (char)unicodeKey;
			if (!char.IsControl (ch) && textEditorData.CanEdit (Caret.Line)) {
				LineSegment line = Document.GetLine (Caret.Line);
				if (Caret.IsInInsertMode || Caret.Column >= line.EditableLength) {
					Document.Insert (Caret.Offset, ch.ToString());
				} else {
					Document.Replace (Caret.Offset, 1, ch.ToString());
				}
				bool autoScroll = Caret.AutoScrollToCaret;
				Caret.Column++;
				Caret.AutoScrollToCaret = autoScroll;
				if (autoScroll)
					Editor.ScrollToCaret ();
				Document.RequestUpdate (new LineUpdate (Caret.Line));
				Document.CommitDocumentUpdate ();
			}
			Document.EndAtomicUndo ();
			Document.OptimizeTypedUndo ();
		}
		
		protected void RunAction (EditAction action)
		{
			try {
				Document.BeginAtomicUndo ();
				action.Run (this.textEditorData);
				Document.EndAtomicUndo ();
			} catch (Exception e) {
				Console.WriteLine ("Error while executing action " + action.ToString () + " :" + e);
			}
		}
		
		public static int GetKeyCode (Gdk.Key key)
		{
			return (int)key;
		}
		
		public static int GetKeyCode (Gdk.Key key, Gdk.ModifierType modifier)
		{
			int m = ((int)modifier) & ((int)Gdk.ModifierType.ControlMask | (int)Gdk.ModifierType.ShiftMask);
			return (int)key | (int)m << 16;
		}
	}
}
