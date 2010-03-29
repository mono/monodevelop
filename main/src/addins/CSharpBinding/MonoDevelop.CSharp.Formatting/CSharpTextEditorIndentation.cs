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
using MonoDevelop.Ide.CodeCompletion;

using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using Mono.TextEditor;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.CSharp.Completion;

namespace MonoDevelop.CSharp.Formatting
{
	public class CSharpTextEditorIndentation : TextEditorExtension
	{
		DocumentStateTracker<CSharpIndentEngine> stateTracker;
		int cursorPositionBeforeKeyPress;
		TextEditorData textEditorData;
		
		static CSharpTextEditorIndentation ()
		{
			CompletionWindowManager.WordCompleted += delegate(object sender, CodeCompletionContextEventArgs e) {
				IExtensibleTextEditor editor = e.Widget as IExtensibleTextEditor;
				if (editor == null)
					return;
				ITextEditorExtension textEditorExtension = editor.Extension;
				while (textEditorExtension != null && !(textEditorExtension is CSharpTextEditorIndentation)) {
					textEditorExtension = textEditorExtension.Next;
				}
				CSharpTextEditorIndentation extension = textEditorExtension as CSharpTextEditorIndentation;
				if (extension == null)
					return;
				extension.stateTracker.UpdateEngine ();
				if (extension.stateTracker.Engine.NeedsReindent)
					extension.DoReSmartIndent ();
			};
		}
		
		public CSharpTextEditorIndentation ()
		{
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			InitTracker ();
			
			Mono.TextEditor.ITextEditorDataProvider view = base.Document.GetContent <Mono.TextEditor.ITextEditorDataProvider> ();
			
			if (view != null) {
				textEditorData = view.GetTextEditorData ();
				textEditorData.VirtualSpaceManager = new IndentVirtualSpaceManager (view.GetTextEditorData (), new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (), Editor));
				textEditorData.Caret.AllowCaretBehindLineEnd = true;
				textEditorData.Paste += TextEditorDataPaste;
			//	textEditorData.Document.TextReplaced += TextCut;
			}
		}

/*		void TextCut (object sender, ReplaceEventArgs e)
		{
			if (!string.IsNullOrEmpty (e.Value) || e.Count == 0)
				return;
			RunFormatterAt (e.Offset);
		}*/
		
		void RunFormatterAt (int offset)
		{
			if (PropertyService.Get ("OnTheFlyFormatting", false) && textEditorData != null && Document != null) {
				//	textEditorData.Document.TextReplaced -= TextCut;
				ProjectDom dom = ProjectDomService.GetProjectDom (Document.Project);
				if (dom == null)
					dom = ProjectDomService.GetFileDom (Document.FileName);
				if (dom == null)
					return;
				DocumentLocation loc = textEditorData.Document.OffsetToLocation (offset);
				DomLocation location = new DomLocation (loc.Line, loc.Column);
				CSharpFormatter.Format (textEditorData, dom, Document.CompilationUnit, location);
				//	textEditorData.Document.TextReplaced += TextCut;
			}
		}
		void TextEditorDataPaste (int insertionOffset, string text)
		{
			if (string.IsNullOrEmpty (text) || text.Length < 2)
				return;
			RunFormatterAt (insertionOffset);
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
		
		public bool DoInsertTemplate ()
		{
			string word = CodeTemplate.GetWordBeforeCaret (Editor);
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplates (CSharpFormatter.MimeType)) {
				if (template.Shortcut == word) 
					return true;
			}
			return false;
		}
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			cursorPositionBeforeKeyPress = Editor.CursorPosition;
			bool isSomethingSelected = Editor.SelectionEndPosition - Editor.SelectionStartPosition > 0;
			
			if (key == Gdk.Key.Tab && TextEditorProperties.TabIsReindent && !(textEditorData.CurrentMode is TextLinkEditMode) && !DoInsertTemplate () && !isSomethingSelected) {
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
					stateTracker.UpdateEngine ();
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
				DoPreInsertionSmartIndent (key);
				bool retval = base.KeyPress (key, keyChar, modifier);

				//handle inserted characters
				if (Editor.CursorPosition <= 0 || Editor.SelectionStartPosition < Editor.SelectionEndPosition)
					return retval;
				
				char lastCharInserted = TranslateKeyCharForIndenter (key, keyChar, Editor.GetCharAt (Editor.CursorPosition - 1));
				if (lastCharInserted == '\0')
					return retval;
				stateTracker.UpdateEngine ();
				
				bool reIndent = false;
				if (!(oldLine == Editor.CursorLine && lastCharInserted == '\n') && (oldBufLen != Editor.TextLength || lastCharInserted != '\0'))
					DoPostInsertionSmartIndent (lastCharInserted, hadSelection, out reIndent);

				//reindent the line after the insertion, if needed
				//N.B. if the engine says we need to reindent, make sure that it's because a char was 
				//inserted rather than just updating the stack due to moving around
				stateTracker.UpdateEngine ();
				bool automaticReindent = (stateTracker.Engine.NeedsReindent && lastCharInserted != '\0');
				if (reIndent || automaticReindent)
					DoReSmartIndent ();

				if (lastCharInserted == '\n') {
					RunFormatter ();
					stateTracker.UpdateEngine ();
//					DoReSmartIndent ();
				}

				stateTracker.UpdateEngine ();

				return retval;
			}

			if (TextEditorProperties.IndentStyle == IndentStyle.Auto && TextEditorProperties.TabIsReindent && key == Gdk.Key.Tab) {
				bool retval = base.KeyPress (key, keyChar, modifier);
				DoReSmartIndent ();
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
		
		
		// removes "\s*\+\s*" patterns (used for special behaviour inside strings)
		void HandleStringConcatinationDeletion (int start, int end)
		{
			if (start < 0 || end >= Editor.TextLength)
				return;
			char ch = Editor.GetCharAt (start);
			if (ch == '"') {
				int sgn = Math.Sign (end - start);
				bool foundPlus = false;
				for (int i = start + sgn; i != end && i >= 0 && i < Editor.TextLength; i += sgn) {
					ch = Editor.GetCharAt (i);
					if (Char.IsWhiteSpace (ch))
						continue;
					if (ch == '+') {
						if (foundPlus)
							break;
						foundPlus = true;
					} else if (ch == '"') {
						if (!foundPlus)
							break;
						if (sgn < 0) {
							Editor.DeleteText (i, start - i);
							Editor.CursorPosition = i + 1;
						} else {
							Editor.DeleteText (start + sgn, i - start);
							Editor.CursorPosition = start;
						}
						break;
					} else {
						break;
					}
				}
			}
		}
		void DoPreInsertionSmartIndent (Gdk.Key key)
		{
			switch (key) {
			case Gdk.Key.BackSpace:
				stateTracker.UpdateEngine ();
				HandleStringConcatinationDeletion (Editor.CursorPosition - 1, 0);
				break;
			case Gdk.Key.Delete:
				stateTracker.UpdateEngine ();
				HandleStringConcatinationDeletion (Editor.CursorPosition, Editor.TextLength);
				break;
			}
		}
		
		//special handling for certain characters just inserted , for comments etc
		void DoPostInsertionSmartIndent (char charInserted, bool hadSelection, out bool reIndent)
		{
			stateTracker.UpdateEngine ();
			reIndent = false;
			int cursor = Editor.CursorPosition;
			switch (charInserted) {
			case ';':
			case '}':
				RunFormatter ();
				break;
			case '\n':
				if (stateTracker.Engine.LineNumber > 0) {
					string previousLine = Editor.GetLineText (stateTracker.Engine.LineNumber - 1);
					string trimmedPreviousLine = previousLine.TrimStart ();
					//xml doc comments
					//check previous line was a doc comment
					//check there's a following line?
					if (trimmedPreviousLine.StartsWith ("/// ") && Editor.GetPositionFromLineColumn (stateTracker.Engine.LineNumber + 1, 1) > -1)					/*  && cursor > 0 && Editor.GetCharAt (cursor - 1) == '\n'*/ {
						//check that the newline command actually inserted a newline
						string nextLine = Editor.GetLineText (stateTracker.Engine.LineNumber + 1);
						if (trimmedPreviousLine.Length > "/// ".Length || nextLine.TrimStart ().StartsWith ("/// ")) {
							Editor.InsertText (cursor, 							/*GetLineWhiteSpace (previousLine) + */"/// ");
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
						Editor.InsertText (cursor, 						/*GetLineWhiteSpace (previousLine) +*/commentPrefix);
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
				//newline always reindents unless it's had special handling
				reIndent = true;
				break;
			}
		}
		void RunFormatter ()
		{
			if (PropertyService.Get ("OnTheFlyFormatting", false) && textEditorData != null) {
				textEditorData.Paste -= TextEditorDataPaste;
				//		textEditorData.Document.TextReplaced -= TextCut;
				ProjectDom dom = ProjectDomService.GetProjectDom (Document.Project);
				if (dom == null)
					dom = ProjectDomService.GetFileDom (Document.FileName);
				
				DomLocation location = new DomLocation (textEditorData.Caret.Location.Line, textEditorData.Caret.Location.Column);
				CSharpFormatter.Format (textEditorData, dom, Document.CompilationUnit, location);
//				OnTheFlyFormatter.Format (textEditorData, dom, location);

				//		textEditorData.Document.TextReplaced += TextCut;
				textEditorData.Paste += TextEditorDataPaste;
			}
		}
		
		//does re-indenting and cursor positioning
		void DoReSmartIndent ()
		{
			string newIndent = string.Empty;
			int cursor = Editor.CursorPosition;
			// Get context to the end of the line w/o changing the main engine's state
			CSharpIndentEngine ctx = (CSharpIndentEngine)stateTracker.Engine.Clone ();
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
			int offset = cursor > pos + curIndent.Length ? cursor - (pos + curIndent.Length) : 0;
			if (!stateTracker.Engine.LineBeganInsideMultiLineComment || (nlwsp < line.Length && line[nlwsp] == '*')) {
				// Possibly replace the indent
				newIndent = ctx.ThisLineIndent;
				int newIndentLength = newIndent.Length;
				if (newIndent != curIndent) {
					if (textEditorData != null) {
						textEditorData.Remove (pos, nlwsp);
					} else {
						Editor.DeleteText (pos, nlwsp);
					}
					if (CompletionWindowManager.IsVisible) {
						if (pos < CompletionWindowManager.CodeCompletionContext.TriggerOffset)
							CompletionWindowManager.CodeCompletionContext.TriggerOffset -= nlwsp;
					}
					
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
