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
		CSharpFormattingPolicy policy;
		
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
			IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			
			IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			if (base.Document.Project != null && base.Document.Project.Policies != null)
				policy = base.Document.Project.Policies.Get<CSharpFormattingPolicy> (types);
			
			textEditorData = Document.Editor;
			if (textEditorData != null) {
				textEditorData.VirtualSpaceManager = new IndentVirtualSpaceManager (textEditorData, new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (policy), textEditorData));
				textEditorData.Caret.AllowCaretBehindLineEnd = true;
				textEditorData.Paste += TextEditorDataPaste;
			}
			
			InitTracker ();
			
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
				ProjectDom dom = Document.Dom;
				DocumentLocation loc = textEditorData.Document.OffsetToLocation (offset);
				DomLocation location = new DomLocation (loc.Line, loc.Column);
			//	CSharpFormatter.Format (textEditorData, dom, Document.CompilationUnit, location);
				OnTheFlyFormatter.Format (textEditorData, dom, location);
				//	textEditorData.Document.TextReplaced += TextCut;
			}
		}
		void TextEditorDataPaste (int insertionOffset, string text)
		{
//			if (string.IsNullOrEmpty (text) || text.Length < 2)
//				return;
//			RunFormatterAt (insertionOffset);
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
				if (column == indent.Length + 1)
					return data.FormatString (0, indent);
				return "";
			}
			
			string GetIndent (int lineNumber, int column)
			{
				stateTracker.UpdateEngine (data.Document.LocationToOffset (lineNumber, column));
				return stateTracker.Engine.NewLineIndent;
			}
			
			public int GetNextVirtualColumn (int lineNumber, int column)
			{
				if (column == DocumentLocation.MinColumn) {
					int result = GetIndent (lineNumber, column).Length + 1;
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
				stateTracker = new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (policy), textEditorData);
			}
		}
		
		internal DocumentStateTracker<CSharpIndentEngine> StateTracker { get { return stateTracker; } }
		
		#endregion
		
		public bool DoInsertTemplate ()
		{
			string word = CodeTemplate.GetWordBeforeCaret (textEditorData);
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplates (CSharpFormatter.MimeType)) {
				if (template.Shortcut == word) 
					return true;
			}
			return false;
		}
		int lastInsertedSemicolon = -1;
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			cursorPositionBeforeKeyPress = textEditorData.Caret.Offset;
			bool isSomethingSelected = textEditorData.IsSomethingSelected;
			if (key == Gdk.Key.BackSpace && textEditorData.Caret.Offset == lastInsertedSemicolon) {
				textEditorData.Document.Undo ();
				lastInsertedSemicolon = -1;
				return false;
			}
			lastInsertedSemicolon = -1;
			if (keyChar == ';' && !(textEditorData.CurrentMode is TextLinkEditMode) && !DoInsertTemplate () && !isSomethingSelected && PropertyService.Get ("SmartSemicolonPlacement", false)) {
				bool retval = base.KeyPress (key, keyChar, modifier);
				LineSegment curLine = textEditorData.Document.GetLine (textEditorData.Caret.Line);
				string text = textEditorData.Document.GetTextAt (curLine);
				if (text.EndsWith (";") || text.Trim ().StartsWith ("for"))
					return retval;
				
				int guessedOffset = GuessSemicolonInsertionOffset (textEditorData, curLine);
				if (guessedOffset != textEditorData.Caret.Offset) {
					textEditorData.Document.EndAtomicUndo ();
					textEditorData.Document.BeginAtomicUndo ();
					textEditorData.Remove (textEditorData.Caret.Offset - 1, 1);
					textEditorData.Caret.Offset = guessedOffset;
					lastInsertedSemicolon = textEditorData.Caret.Offset + 1;
					retval = base.KeyPress (key, keyChar, modifier);
				}
				return retval;
			}
			
			if (key == Gdk.Key.Tab && TextEditorProperties.TabIsReindent && !(textEditorData.CurrentMode is TextLinkEditMode) && !DoInsertTemplate () && !isSomethingSelected) {
				int cursor = textEditorData.Caret.Offset;
				
				if (TextEditorProperties.TabIsReindent && stateTracker.Engine.IsInsideVerbatimString) {
					// insert normal tab inside @" ... "
					if (textEditorData.IsSomethingSelected) {
						textEditorData.SelectedText = "\t";
					} else {
						textEditorData.Insert (cursor, "\t");
					}
				} else if (TextEditorProperties.TabIsReindent && cursor >= 1) {
					if (textEditorData.Caret.Column > 1) {
						int delta = cursor - this.cursorPositionBeforeKeyPress;
						if (delta < 2 && delta > 0) {
							textEditorData.Remove (cursor - delta, delta);
							textEditorData.Caret.Offset = cursor - delta;
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
				int oldBufLen = textEditorData.Length;
				int oldLine = textEditorData.Caret.Line + 1;
				bool hadSelection = textEditorData.IsSomethingSelected;

				//pass through to the base class, which actually inserts the character
				//and calls HandleCodeCompletion etc to handles completion
				DoPreInsertionSmartIndent (key);
				bool retval = base.KeyPress (key, keyChar, modifier);

				//handle inserted characters
				if (textEditorData.Caret.Offset <= 0 || textEditorData.IsSomethingSelected)
					return retval;
				
				char lastCharInserted = TranslateKeyCharForIndenter (key, keyChar, textEditorData.GetCharAt (textEditorData.Caret.Offset - 1));
				if (lastCharInserted == '\0')
					return retval;
				stateTracker.UpdateEngine ();
				
				bool reIndent = false;
				if (key == Gdk.Key.Return && modifier == Gdk.ModifierType.ControlMask) {
					FixLineStart (textEditorData.Caret.Line + 1);
				} else {
					if (!(oldLine == textEditorData.Caret.Line + 1 && lastCharInserted == '\n') && (oldBufLen != textEditorData.Length || lastCharInserted != '\0'))
						DoPostInsertionSmartIndent (lastCharInserted, hadSelection, out reIndent);
				}
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

		static int GuessSemicolonInsertionOffset (TextEditorData data, LineSegment curLine)
		{
			int offset = data.Caret.Offset;
			int lastNonWsOffset = offset;
			
			int max = curLine.Offset + curLine.EditableLength;
			
			// if the line ends with ';' the line end is not the correct place for a new semicolon.
			if (curLine.EditableLength > 0 && data.Document.GetCharAt (max - 1) == ';')
				return offset;
			
			bool isInString = false, isInChar = false, isVerbatimString = false;
			bool isInLineComment  = false, isInBlockComment = false;
			for (int pos = offset; pos < max; pos++) {
				char ch = data.Document.GetCharAt (pos);
				switch (ch) {
				case '/':
					if (isInBlockComment) {
						if (pos > 0 && data.Document.GetCharAt (pos - 1) == '*') 
							isInBlockComment = false;
					} else  if (!isInString && !isInChar && pos + 1 < max) {
						char nextChar = data.Document.GetCharAt (pos + 1);
						if (nextChar == '/') {
							isInLineComment = true;
							return lastNonWsOffset;
						}
						if (!isInLineComment && nextChar == '*') {
							isInBlockComment = true;
							return lastNonWsOffset;
						}
					}
					break;
				case '\\':
					if (isInChar || (isInString && !isVerbatimString))
						pos++;
					break;
				case '@':
					if (!(isInString || isInChar || isInLineComment || isInBlockComment) && pos + 1 < max && data.Document.GetCharAt (pos + 1) == '"') {
						isInString = true;
						isVerbatimString = true;
						pos++;
					}
					break;
				case '"':
					if (!(isInChar || isInLineComment || isInBlockComment)) {
						if (isInString && isVerbatimString && pos + 1 < max && data.Document.GetCharAt (pos + 1) == '"') {
							pos++;
						} else {
							isInString = !isInString;
							isVerbatimString = false;
						}
					}
					break;
				case '\'':
					if (!(isInString || isInLineComment || isInBlockComment)) 
						isInChar = !isInChar;
					break;
				}
				if (!char.IsWhiteSpace (ch))
					lastNonWsOffset = pos;
			}
			
			return lastNonWsOffset;
			
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
			if (start < 0 || end >= textEditorData.Length)
				return;
			char ch = textEditorData.GetCharAt (start);
			if (ch == '"') {
				int sgn = Math.Sign (end - start);
				bool foundPlus = false;
				for (int max = start + sgn; max != end && max >= 0 && max < textEditorData.Length; max += sgn) {
					ch = textEditorData.GetCharAt (max);
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
							textEditorData.Remove (max, start - max);
							textEditorData.Caret.Offset = max + 1;
						} else {
							textEditorData.Remove (start + sgn, max - start);
							textEditorData.Caret.Offset = start;
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
				HandleStringConcatinationDeletion (textEditorData.Caret.Offset - 1, 0);
				break;
			case Gdk.Key.Delete:
				stateTracker.UpdateEngine ();
				HandleStringConcatinationDeletion (textEditorData.Caret.Offset, textEditorData.Length);
				break;
			}
		}
		
		//special handling for certain characters just inserted , for comments etc
		void DoPostInsertionSmartIndent (char charInserted, bool hadSelection, out bool reIndent)
		{
			stateTracker.UpdateEngine ();
			reIndent = false;
			switch (charInserted) {
			case ';':
			case '}':
				RunFormatter ();
				break;
			case '\n':
				if (FixLineStart (stateTracker.Engine.LineNumber)) 
					return;
				//newline always reindents unless it's had special handling
				reIndent = true;
				break;
			}
		}
		
		bool FixLineStart (int lineNumber)
		{
			if (lineNumber > DocumentLocation.MinLine) {
				LineSegment line = textEditorData.Document.GetLine (lineNumber);
				int insertionPoint = line.Offset + line.GetIndentation (textEditorData.Document).Length;
				
				LineSegment prevLine = textEditorData.Document.GetLine (lineNumber - 1);
				string trimmedPreviousLine = textEditorData.Document.GetTextAt (prevLine).TrimStart ();
				
				//xml doc comments
				//check previous line was a doc comment
				//check there's a following line?
				if (trimmedPreviousLine.StartsWith ("/// ") && lineNumber + 1 < textEditorData.Document.LineCount) {
					//check that the newline command actually inserted a newline
					string nextLine = textEditorData.Document.GetTextAt (textEditorData.Document.GetLine (lineNumber + 1)).TrimStart ();
					if (trimmedPreviousLine.Length > "/// ".Length || nextLine.StartsWith ("/// ")) {
						textEditorData.Insert (insertionPoint, "/// ");
						if (textEditorData.Caret.Offset >= insertionPoint)
							textEditorData.Caret.Offset += "/// ".Length;
						return true;
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
					textEditorData.Insert (insertionPoint, commentPrefix);
					if (textEditorData.Caret.Offset >= insertionPoint)
						textEditorData.Caret.Offset += commentPrefix.Length;
					return true;
				} else if (stateTracker.Engine.IsInsideStringLiteral) {
					textEditorData.Insert (prevLine.Offset + prevLine.EditableLength, "\" +");
					
					if (!trimmedPreviousLine.StartsWith ("\"")) {
						int offset = insertionPoint++ + 3; 
						textEditorData.Insert (offset, "\t");
						if (textEditorData.Caret.Offset >= offset)
							textEditorData.Caret.Offset ++;
					}
					textEditorData.Insert (insertionPoint + 3, "\"");
					if (textEditorData.Caret.Offset >= insertionPoint + 3)
						textEditorData.Caret.Offset += "\"".Length;
					
					return true;
				}
			}
			return false;
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
//				CSharpFormatter.Format (textEditorData, dom, Document.CompilationUnit, location);
				OnTheFlyFormatter.Format (textEditorData, dom, location);

				//		textEditorData.Document.TextReplaced += TextCut;
				textEditorData.Paste += TextEditorDataPaste;
			}
		}
		
		//does re-indenting and cursor positioning
		void DoReSmartIndent ()
		{
			string newIndent = string.Empty;
			int cursor = textEditorData.Caret.Offset;
			
			// Get context to the end of the line w/o changing the main engine's state
			CSharpIndentEngine ctx = (CSharpIndentEngine)stateTracker.Engine.Clone ();
			LineSegment line = textEditorData.Document.GetLine (textEditorData.Caret.Line);

			for (int max = line.Offset; max < line.Offset + line.EditableLength; max++) {
				ctx.Push (textEditorData.Document.GetCharAt (max));
			}
			
			int pos = line.Offset;
			
			string curIndent = line.GetIndentation (textEditorData.Document);
			
			int nlwsp = curIndent.Length;
			int offset = cursor > pos + nlwsp ? cursor - (pos + nlwsp) : 0;
			if (!stateTracker.Engine.LineBeganInsideMultiLineComment || (nlwsp < line.Length && textEditorData.Document.GetCharAt (line.Offset + nlwsp) == '*')) {
				// Possibly replace the indent
				newIndent = ctx.ThisLineIndent;
				int newIndentLength = newIndent.Length;
				if (newIndent != curIndent) {
					if (CompletionWindowManager.IsVisible) {
						if (pos < CompletionWindowManager.CodeCompletionContext.TriggerOffset)
							CompletionWindowManager.CodeCompletionContext.TriggerOffset -= nlwsp;
					}
					newIndentLength = textEditorData.Replace (pos, nlwsp, newIndent);
					// Engine state is now invalid
					stateTracker.ResetEngineToPosition (pos);
				}
				pos += newIndentLength;
			} else {
				pos += curIndent.Length;
			}
			
			pos += offset;
			
			textEditorData.Caret.Offset = pos;
		}
		
		/*
		[MonoDevelop.Components.Commands.CommandHandler (MonoDevelop.Ide.CodeFormatting.CodeFormattingCommands.FormatBuffer)]
		public void FormatBuffer ()
		{
			Console.WriteLine ("format buffer!");
			ProjectDom dom = ProjectDomService.GetProjectDom (Document.Project);
			OnTheFlyFormatter.Format (this.textEditorData, dom);
		}*/
	}
}
