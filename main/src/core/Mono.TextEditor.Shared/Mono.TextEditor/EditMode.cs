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
using Gdk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.Text;

namespace Mono.TextEditor
{
	abstract class EditMode
	{
		//NOTE: the behaviour of this class is actually stateless; these variables are used to make the API
		// friendlier for subclassers of this class
		protected TextEditorData textEditorData;
		protected MonoTextEditor editor;
	//	string status;
		
		public void InternalHandleKeypress (MonoTextEditor editor, TextEditorData data, Gdk.Key key, 
		                                      uint unicodeChar, Gdk.ModifierType modifier)
		{
			this.editor = editor; 
			this.textEditorData = data;
			
			HandleKeypress (key, unicodeChar, modifier);
			
			//make sure that nothing funny goes on when the mode should have finished
			this.textEditorData = null;
			this.editor = null;
		}
		
		internal virtual void InternalSelectionChanged (MonoTextEditor editor, TextEditorData data)
		{
			// only trigger SelectionChanged when event is a result of external stimuli, i.e. when 
			// not already running HandleKeypress
			if (this.editor == null) {
				this.editor = editor; 
				this.textEditorData = data;
				SelectionChanged ();
				this.textEditorData = null;
				this.editor = null;
			}
		}
		
		internal void InternalCaretPositionChanged (MonoTextEditor editor, TextEditorData data)
		{
			// only trigger CaretPositionChanged when event is a result of external stimuli, i.e. when 
			// not already running HandleKeypress
			if (this.editor == null) {
				this.editor = editor; 
				this.textEditorData = data;
				CaretPositionChanged ();
				this.textEditorData = null;
				this.editor = null;
			}
		}
		
		protected virtual void SelectionChanged ()
		{
		}
		
		protected virtual void CaretPositionChanged ()
		{
		}

		public virtual void SelectValidShortcut (KeyboardShortcut[] accels, out Gdk.Key key, out ModifierType mod)
		{
			key = accels [0].Key;
			mod = accels [0].Modifier;
		}

		protected CaretImpl Caret { get { return textEditorData.Caret; } }
		protected TextDocument Document { get { return textEditorData.Document; } }
		protected MonoTextEditor Editor { get { return editor; } }
		protected TextEditorData Data { get { return textEditorData; } }
		
		protected abstract void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier);
		
		public virtual bool WantsToPreemptIM {
			get { return false; }
		}

		bool IsSpecialKeyForSelection (uint unicodeKey)
		{
			string start, end;
			return textEditorData.SelectionSurroundingProvider.GetSelectionSurroundings (unicodeKey, out start, out end);
		}

		protected void InsertCharacter (uint unicodeKey)
		{
			if (!textEditorData.CanEdit (Data.Caret.Line))
				return;

			HideMouseCursor ();

			if (textEditorData.IsSomethingSelected && textEditorData.Options.EnableSelectionWrappingKeys && IsSpecialKeyForSelection (unicodeKey)) {
				textEditorData.SelectionSurroundingProvider.HandleSpecialSelectionKey (unicodeKey);
				return;
			}
			using (var undo = Document.OpenUndoGroup ()) {

				textEditorData.DeleteSelectedText (
					textEditorData.IsSomethingSelected ? textEditorData.MainSelection.SelectionMode != SelectionMode.Block : true);
				// Needs to be called after delete text, delete text handles virtual caret postitions itself,
				// but afterwards the virtual position may need to be restored.
				textEditorData.EnsureCaretIsNotVirtual ();

				char ch = (char)unicodeKey;
				if (!char.IsControl (ch) && textEditorData.CanEdit (Caret.Line)) {
					DocumentLine line = Document.GetLine (Caret.Line);
					if (Caret.IsInInsertMode || Caret.Column >= line.Length + 1) {
						string text = ch.ToString ();
						if (textEditorData.IsSomethingSelected && textEditorData.MainSelection.SelectionMode == SelectionMode.Block) {
							var visualInsertLocation = textEditorData.LogicalToVisualLocation (Caret.Location);
							var selection = textEditorData.MainSelection;
							Caret.PreserveSelection = true;
							var changes = new List<Microsoft.CodeAnalysis.Text.TextChange> ();
							for (int lineNumber = selection.MinLine; lineNumber <= selection.MaxLine; lineNumber++) {
								DocumentLine lineSegment = textEditorData.GetLine (lineNumber);
								int insertOffset = lineSegment.GetLogicalColumn (textEditorData, visualInsertLocation.Column) - 1;
								string textToInsert;
								if (lineSegment.Length < insertOffset) {
									int visualLastColumn = lineSegment.GetVisualColumn (textEditorData, lineSegment.Length + 1);
									int charsToInsert = visualInsertLocation.Column - visualLastColumn;
									int spaceCount = charsToInsert % editor.Options.TabSize;
									textToInsert = new string ('\t', (charsToInsert - spaceCount) / editor.Options.TabSize) + 
										new string (' ', spaceCount) + text;
									insertOffset = lineSegment.Length;
								} else {
									textToInsert = text;
								}
								changes.Add (new Microsoft.CodeAnalysis.Text.TextChange (new Microsoft.CodeAnalysis.Text.TextSpan (lineSegment.Offset + insertOffset, 0), textToInsert));
							}
							textEditorData.Document.ApplyTextChanges (changes);
							var visualColumn = textEditorData.GetLine (Caret.Location.Line).GetVisualColumn (textEditorData, Caret.Column);

							textEditorData.MainSelection = new MonoDevelop.Ide.Editor.Selection (
								new DocumentLocation (selection.Anchor.Line, textEditorData.GetLine (selection.Anchor.Line).GetLogicalColumn (textEditorData, visualColumn)),
								new DocumentLocation (selection.Lead.Line, textEditorData.GetLine (selection.Lead.Line).GetLogicalColumn (textEditorData, visualColumn)),
								SelectionMode.Block
								);
							Document.CommitMultipleLineUpdate (textEditorData.MainSelection.MinLine, textEditorData.MainSelection.MaxLine);
						} else {
							textEditorData.Insert (Caret.Offset, text);
						}
					} else {
						textEditorData.Replace (Caret.Offset, 1, ch.ToString ());
					}
					// That causes unnecessary redraws:
					//				bool autoScroll = Caret.AutoScrollToCaret;
//					Caret.Column++;
					if (Caret.PreserveSelection)
						Caret.PreserveSelection = false;
					//				Caret.AutoScrollToCaret = autoScroll;
					//				if (autoScroll)
					//					Editor.ScrollToCaret ();
					//				Document.RequestUpdate (new LineUpdate (Caret.Line));
					//				Document.CommitDocumentUpdate ();
				}
			}
			Document.OptimizeTypedUndo ();
		}
		
		internal void AddedToEditor (TextEditorData data)
		{
			OnAddedToEditor (data);
		}
		
		protected virtual void OnAddedToEditor (TextEditorData data)
		{
		}
		
		internal void RemovedFromEditor (TextEditorData data)
		{
			OnRemovedFromEditor (data);
		}
		
		protected virtual void OnRemovedFromEditor (TextEditorData data)
		{
		}
		
		protected void RunAction (Action<TextEditorData> action)
		{
			HideMouseCursor ();
			try {
				action (this.textEditorData);
			} catch (Exception e) {
				Console.WriteLine ("Error while executing action " + action.ToString () + " :" + e);
			}
		}
		
		protected void RunActions (params Action<TextEditorData>[] actions)
		{
			HideMouseCursor ();
			try {
				using (var undo = Document.OpenUndoGroup ()) {
					foreach (var action in actions)
						action (this.textEditorData);
				}
			} catch (Exception e) {
				var sb = new System.Text.StringBuilder ("Error while executing actions ");
				foreach (var action in actions)
					sb.AppendFormat (" {0}", action);
				Console.WriteLine (sb.ToString () + ": " + e);
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
		
		protected void HideMouseCursor ()
		{
			//should only be null during tests
			if (editor != null)
				editor.HideMouseCursor ();
		}

		#region TextAreaControl
		public virtual void AllocateTextArea (MonoTextEditor textEditor, TextArea textArea, Rectangle allocation)
		{
			if (textArea.Allocation != allocation)
				textArea.SizeAllocate (allocation);
		}
		#endregion
	}
}
