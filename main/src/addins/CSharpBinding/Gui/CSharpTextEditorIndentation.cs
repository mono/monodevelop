//
// CSharpTextEditorIndentation.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.Diagnostics;
using System.Text;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui.Completion;

using CSharpBinding;
using CSharpBinding.FormattingStrategy;
using CSharpBinding.Parser;

namespace MonoDevelop.CSharpBinding.Gui
{
	public class CSharpTextEditorIndentation : TextEditorExtension
	{
		DocumentStateTracker<CSharpIndentEngine> stateTracker;
		int cursorPositionBeforeKeyPress;
		
		public CSharpTextEditorIndentation ()
		{
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			InitTracker ();
		}
		
		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, IEditableTextBuffer editor)
		{
			return System.IO.Path.GetExtension (doc.Title) == ".cs";
		}
		
		#region Sharing the tracker
		
		void InitTracker ()
		{
			//if there's a CSharpTextEditorCompletion in the extension chain, we can reuse its stateTracker
			CSharpTextEditorCompletion c = this.Document.GetContent<CSharpTextEditorCompletion> ();
			if (c != null && c.StateTracker != null) {
				stateTracker = c.StateTracker;
				System.Console.WriteLine("found it");
			} else {
				stateTracker = new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (), Editor);
			}
		}
		
		internal DocumentStateTracker<CSharpIndentEngine> StateTracker { get { return stateTracker; } }
		
		#endregion
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			cursorPositionBeforeKeyPress = Editor.CursorPosition;
			
			//do the smart indent
			if (TextEditorProperties.IndentStyle == IndentStyle.Smart) {
				//capture some of the current state
				int oldBufLen = Editor.TextLength;
				int oldLine = Editor.CursorLine;
				bool hadSelection = Editor.SelectionEndPosition != Editor.SelectionStartPosition;
				
				//pass through to the base class, which actually inserts the character
				//and calls HandleCodeCompletion etc to handles completion
				bool retval = base.KeyPress (key, keyChar, modifier);
				stateTracker.UpdateEngine ();
				
				//handle inserted characters
				if (Editor.CursorPosition <= 0)
					return retval;
				
				bool reIndent = false;
				char lastCharInserted = TranslateKeyCharForIndenter (key, keyChar, Editor.GetCharAt (Editor.CursorPosition - 1));
				if (!(oldLine == Editor.CursorLine && lastCharInserted == '\n') && (oldBufLen != Editor.TextLength || lastCharInserted != '\0'))
					DoPostInsertionSmartIndent (lastCharInserted, hadSelection, out reIndent);
				
				//reindent the line after the insertion, if needed
				//N.B. if the engine says we need to reindent, make sure that it's because a char was 
				//inserted rather than just updating the stack due to moving around
				stateTracker.UpdateEngine ();
				bool automaticReindent = (stateTracker.Engine.NeedsReindent && lastCharInserted != '\0');
				if (reIndent || automaticReindent)
					DoReSmartIndent ();
				
				stateTracker.UpdateEngine ();
				
				return retval;
			}
			
			//pass through to the base class, which actually inserts the character
			//and calls HandleCodeCompletion etc to handles completion
			return base.KeyPress (key, keyChar, modifier);
		}
		
		static char TranslateKeyCharForIndenter (Gdk.Key key, char keyChar, char docChar)
		{
			switch (key) {
			case Gdk.Key.Return:
			case Gdk.Key.KP_Enter:
				return '\n';
			case Gdk.Key.Tab:
				return '\t';
			default:
				if (docChar == keyChar)
					return keyChar;
				break;
			}
			return '\0';
		}
		
		//special handling for certain characters just inserted , for comments etc
		void DoPostInsertionSmartIndent (char charInserted, bool hadSelection, out bool reIndent)
		{
			stateTracker.UpdateEngine ();
			reIndent = false;
			int cursor = Editor.CursorPosition;
			
			switch (charInserted) {
			case '\n':
				if (stateTracker.Engine.LineNumber > 0) {
					string previousLine = Editor.GetLineText (stateTracker.Engine.LineNumber - 1);
					string trimmedPreviousLine = previousLine.TrimStart ();
					System.Console.WriteLine(trimmedPreviousLine);
					//xml doc comments
					if (trimmedPreviousLine.StartsWith ("/// ") //check previous line was a doc comment
					    && Editor.GetPositionFromLineColumn (stateTracker.Engine.LineNumber + 1, 1) > -1 //check there's a following line?
					   /*  && cursor > 0 && Editor.GetCharAt (cursor - 1) == '\n'*/) { //check that the newline command actually inserted a newline
						string nextLine = Editor.GetLineText (stateTracker.Engine.LineNumber + 1);
						if (trimmedPreviousLine.Length > "/// ".Length || nextLine.TrimStart ().StartsWith ("/// ")) {
						    Editor.InsertText (cursor, /*GetLineWhiteSpace (previousLine) + */"/// ");
							return;
						}
					//multi-line comments
					} else if (stateTracker.Engine.IsInsideMultiLineComment) {
					    string commentPrefix = string.Empty;
						if (trimmedPreviousLine.StartsWith ("* ")) {
							commentPrefix = "* ";
						} else if (trimmedPreviousLine.StartsWith ("/**") || trimmedPreviousLine.StartsWith ("/*")) {
							commentPrefix = " * ";
						} else if (trimmedPreviousLine.StartsWith ("*")) {
							commentPrefix = "*";
						}
						Editor.InsertText (cursor, /*GetLineWhiteSpace (previousLine) +*/ commentPrefix);
						return;
					}
				}
				//newline always reindents unless it's had special handling
				reIndent = true;
				break;
			case '\t':
				// Tab is a special case... depending on the context, the user may be
				// requesting a re-indent, tab-completing, or may just be wanting to
				// insert a literal tab.
				//
				// Tab is interpreted as a reindent command when it's neither at the end of a line nor in a verbatim string
				// and when a tab has just been inserted (i.e. not a template or an autocomplete command)
				if (TextEditorProperties.TabIsReindent &&
				    !stateTracker.Engine.IsInsideVerbatimString
				    && cursor >= 1 && Char.IsWhiteSpace (Editor.GetCharAt (cursor - 1)) //tab was actually inserted, or in a region of tabs
				    && !hadSelection //was just a cursor, not a block of selected text -- the text editor handles that specially
				    )
				{
					int delta = Editor.CursorPosition - this.cursorPositionBeforeKeyPress;
					Editor.DeleteText (cursor - delta, delta);
					reIndent = true;
				}
				break;
			}
		}
		
		//does re-indenting and cursor positioning
		void DoReSmartIndent ()
		{
			string newIndent = string.Empty;
			int cursor = Editor.CursorPosition;
			
			// Get context to the end of the line w/o changing the main engine's state
			CSharpIndentEngine ctx = (CSharpIndentEngine) stateTracker.Engine.Clone ();
			string line = Editor.GetLineText (ctx.LineNumber);
			for (int i = ctx.LineOffset; i < line.Length; i++) {
				ctx.Push (line[i]);
			}
			//System.Console.WriteLine("Re-indenting line '{0}'", line);
			
			// Measure the current indent
			int nlwsp = 0;
			while (nlwsp < line.Length && Char.IsWhiteSpace (line[nlwsp]))
				nlwsp++;
			
			int pos = Editor.GetPositionFromLineColumn (ctx.LineNumber, 1);
			string curIndent = line.Substring (0, nlwsp);
			int offset;
			
			if (cursor > pos + curIndent.Length)
				offset = cursor - (pos + curIndent.Length);
			else
				offset = 0;
			
			if (!stateTracker.Engine.LineBeganInsideMultiLineComment ||
			    (nlwsp < line.Length && line[nlwsp] == '*')) {
				// Possibly replace the indent
				newIndent = ctx.ThisLineIndent;
				
				if (newIndent != curIndent) {
					Editor.DeleteText (pos, nlwsp);
					Editor.InsertText (pos, newIndent);
					
					// Engine state is now invalid
					stateTracker.ResetEngineToPosition (pos);
				}
				
				pos += newIndent.Length;
			} else {
				pos += curIndent.Length;
			}
			
			pos += offset;
			if (pos != Editor.CursorPosition) {
				Editor.CursorPosition = pos;
				Editor.Select (pos, pos);
			}
		}
		
		static string GetLineWhiteSpace (string line)
		{
			int trimmedLength = line.TrimStart ().Length;
			return line.Substring (0, line.Length - trimmedLength);
		}
		
	}
}
