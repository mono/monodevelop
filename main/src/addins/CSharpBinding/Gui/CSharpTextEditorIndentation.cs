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
using Mono.TextEditor;


namespace MonoDevelop.CSharpBinding.Gui
{
	public class CSharpTextEditorIndentation : TextEditorExtension
	{
		DocumentStateTracker<CSharpIndentEngine> stateTracker;
		int cursorPositionBeforeKeyPress;
		TextEditorData textEditorData;
		
		public CSharpTextEditorIndentation ()
		{
			
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			InitTracker ();
			Mono.TextEditor.ITextEditorDataProvider view = this.Document.ActiveView as Mono.TextEditor.ITextEditorDataProvider;
			if (view != null) {
				textEditorData = view.GetTextEditorData ();
				textEditorData.VirtualSpaceManager = new IndentVirtualSpaceManager (view.GetTextEditorData (), new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (), Editor));
				textEditorData.Caret.AllowCaretBehindLineEnd = true;
				textEditorData.Paste += TextEditorDataPaste;
			}
		}

		void TextEditorDataPaste (int insertionOffset, string text)
		{
			if (string.IsNullOrEmpty (text) || text.Length < 2) 
				return; 
			
			if (PropertyService.Get ("OnTheFlyFormatting", false)) {
				ProjectDom dom = ProjectDomService.GetProjectDom (Document.Project);
				if (dom == null) 
					dom = ProjectDomService.GetFileDom (Document.FileName); 
				DocumentLocation loc = textEditorData.Document.OffsetToLocation (insertionOffset);
				DomLocation location = new DomLocation (loc.Line, loc.Column);
				CSharpFormatter formatter = new CSharpFormatter (textEditorData, dom, Document.CompilationUnit, Editor, location);
			}
		}

		class IndentVirtualSpaceManager : Mono.TextEditor.TextEditorData.IVirtualSpaceManager
		{
			Mono.TextEditor.TextEditorData data;
			DocumentStateTracker<CSharpIndentEngine> stateTracker;
			
			public IndentVirtualSpaceManager (Mono.TextEditor.TextEditorData data, DocumentStateTracker<CSharpIndentEngine> stateTracker)
			{
				this.data = data;
				this.stateTracker = stateTracker;
			}
					
			public string GetVirtualSpaces (int lineNumber, int column)
			{
				string indent = GetIndent (lineNumber, column);
				if (column == indent.Length)
					return indent;
				return "";
			}
			
			string GetIndent (int lineNumber, int column)
			{
				stateTracker.UpdateEngine (data.Document.LocationToOffset (lineNumber, column));
				return stateTracker.Engine.NewLineIndent;
			}
			
			public int GetNextVirtualColumn (int lineNumber, int column)
			{
				if (column == 0) {
					int result = GetIndent (lineNumber, column).Length;
					return result;
				}
				return column;
			}
		}
		
		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, IEditableTextBuffer editor)
		{
			return System.IO.Path.GetExtension (doc.Name) == ".cs";
		}
		
		#region Sharing the tracker
		
		void InitTracker ()
		{
			//if there's a CSharpTextEditorCompletion in the extension chain, we can reuse its stateTracker
			CSharpTextEditorCompletion c = this.Document.GetContent<CSharpTextEditorCompletion> ();
			if (c != null && c.StateTracker != null) {
				stateTracker = c.StateTracker;
			} else {
				stateTracker = new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (), Editor);
			}
		}
		
		internal DocumentStateTracker<CSharpIndentEngine> StateTracker { get { return stateTracker; } }
		
		#endregion
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			cursorPositionBeforeKeyPress = Editor.CursorPosition;
			if (key == Gdk.Key.Tab && TextEditorProperties.TabIsReindent) {
				int cursor = Editor.CursorPosition;
				if (TextEditorProperties.TabIsReindent && stateTracker.Engine.IsInsideVerbatimString) {
					// insert normal tab inside @" ... "
					if (Editor.SelectionEndPosition > 0) {
						Editor.SelectedText = "\t";
					} else {
						Editor.InsertText (cursor, "\t");
					}
				} else if (TextEditorProperties.TabIsReindent && cursor >= 1) {
					if (Editor.CursorColumn > 2) {
						int delta = cursor - this.cursorPositionBeforeKeyPress;
						if (delta < 2) {
							Editor.DeleteText (cursor - delta, delta);
							Editor.CursorPosition = cursor - delta;
						}
					}
					DoReSmartIndent ();
				}
				return false;
			}
			
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
			
			if (TextEditorProperties.IndentStyle == IndentStyle.Auto && TextEditorProperties.TabIsReindent && key == Gdk.Key.Tab) {
				DoReSmartIndent ();
				return false;
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
				//xml doc comments
				//check previous line was a doc comment
				//check there's a following line?
				if (trimmedPreviousLine.StartsWith ("/// ") && Editor.GetPositionFromLineColumn (stateTracker.Engine.LineNumber + 1, 1) > -1)				/*  && cursor > 0 && Editor.GetCharAt (cursor - 1) == '\n'*/ {
					//check that the newline command actually inserted a newline
					string nextLine = Editor.GetLineText (stateTracker.Engine.LineNumber + 1);
					if (trimmedPreviousLine.Length > "/// ".Length || nextLine.TrimStart ().StartsWith ("/// ")) {
						Editor.InsertText (cursor, 						/*GetLineWhiteSpace (previousLine) + */"/// ");
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
					Editor.InsertText (cursor, 					/*GetLineWhiteSpace (previousLine) +*/commentPrefix);
					return;
				} else if (stateTracker.Engine.IsInsideStringLiteral) {
					int lastLineEndPos = Editor.GetPositionFromLineColumn (stateTracker.Engine.LineNumber - 1, Editor.GetLineLength (stateTracker.Engine.LineNumber - 1) + 1);
					int cursorEndPos = cursor + 4;
					Editor.InsertText (lastLineEndPos, "\" +");
					if (!trimmedPreviousLine.StartsWith ("\"")) {
						Editor.InsertText (cursor++ + 3, "\t");
						cursorEndPos++;
					}
					Editor.InsertText (cursor + 3, "\"");
					Editor.CursorPosition = cursorEndPos;
					return;
				}
			}

			if (PropertyService.Get ("OnTheFlyFormatting", false)) {
				textEditorData.Paste -= TextEditorDataPaste;
				
				ProjectDom dom = ProjectDomService.GetProjectDom (Document.Project);
				if (dom == null) 
					dom = ProjectDomService.GetFileDom (Document.FileName); 
				
				DomLocation location = new DomLocation(Editor.CursorLine, Editor.CursorColumn);
				CSharpFormatter formatter = new CSharpFormatter(textEditorData, dom, Document.CompilationUnit, Editor, location);
				
				textEditorData.Paste += TextEditorDataPaste;
			}

			//newline always reindents unless it's had special handling
			reIndent = true;
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
				int newIndentLength = newIndent.Length;
				if (newIndent != curIndent) {
					Editor.DeleteText (pos, nlwsp);
					newIndentLength = Editor.InsertText (pos, newIndent);
					
					// Engine state is now invalid
					stateTracker.ResetEngineToPosition (pos);
				}
				
				pos += newIndentLength;
			} else {
				pos += curIndent.Length;
			}
			
			pos += offset;
			
			if (pos != Editor.CursorPosition) {
				Editor.CursorPosition = pos;
				Editor.Select (pos, pos);
			}
		}
	
	}
}
