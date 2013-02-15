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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects;
using MonoDevelop.Ide.CodeCompletion;

using MonoDevelop.CSharp.Formatting;
using Mono.TextEditor;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.SourceEditor;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.Editor;
using System.Linq;

namespace MonoDevelop.CSharp.Formatting
{
	public class CSharpTextEditorIndentation : TextEditorExtension
	{
		DocumentStateTracker<CSharpIndentEngine> stateTracker;
		int cursorPositionBeforeKeyPress;
		TextEditorData textEditorData {
			get {
				return document.Editor;
			}
		}

		IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);

		CSharpFormattingPolicy Policy {
			get {
				if (Document != null && Document.Project != null && Document.Project.Policies != null) {
					return base.Document.Project.Policies.Get<CSharpFormattingPolicy> (types);
				}
				return MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			}
		}

		TextStylePolicy TextStylePolicy {
			get {
				if (Document != null && Document.Project != null && Document.Project.Policies != null) {
					return base.Document.Project.Policies.Get<TextStylePolicy> (types);
				}
				return MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> (types);
			}
		}

		char lastCharInserted;

		static CSharpTextEditorIndentation ()
		{
			CompletionWindowManager.WordCompleted += delegate(object sender,CodeCompletionContextEventArgs e) {
				var editor = e.Widget as IExtensibleTextEditor;
				if (editor == null)
					return;
				var textEditorExtension = editor.Extension;
				while (textEditorExtension != null && !(textEditorExtension is CSharpTextEditorIndentation)) {
					textEditorExtension = textEditorExtension.Next;
				}
				var extension = textEditorExtension as CSharpTextEditorIndentation;
				if (extension == null)
					return;
				extension.stateTracker.UpdateEngine ();
				if (extension.stateTracker.Engine.NeedsReindent)
					extension.DoReSmartIndent ();
			};
		}

		bool IsPreprocessorDirective (DocumentLine documentLine)
		{
			int o = documentLine.Offset;
			for (int i = 0; i < documentLine.Length; i++) {
				char ch = Editor.GetCharAt (o++);
				if (ch == '#')
					return true;
				if (!char.IsWhiteSpace (ch)) {
					return false;
				}
			}
			return false;
		}

		void HandleTextPaste (int insertionOffset, string text, int insertedChars)
		{
			if (document.Editor.Options.IndentStyle == IndentStyle.None ||
			    document.Editor.Options.IndentStyle == IndentStyle.Auto)
				return;
			var startLine = Editor.GetLineByOffset (insertionOffset);
			var endLine = Editor.GetLineByOffset (insertionOffset + insertedChars);

			using (var undo = Editor.OpenUndoGroup ()) {

//			// Trim blank spaces on text paste, see: Bug 511 - Trim blank spaces when copy-pasting
//			if (OnTheFlyFormatting) {
//				int i = insertionOffset + insertedChars;
//				bool foundNonWsFollowUp = false;
//
//				var line = Document.Editor.GetLineByOffset (i);
//				if (line != null) {
//					for (int j = 0; j < line.Offset + line.Length; j++) {
//						var ch = Document.Editor.GetCharAt (j);
//						if (ch != ' ' && ch != '\t') {
//							foundNonWsFollowUp = true;
//							break;
//						}
//					}
//				}
//
//				if (!foundNonWsFollowUp) {
//					while (i > insertionOffset) {
//						char ch = Document.Editor.GetCharAt (i - 1);
//						if (ch != ' ' && ch != '\t') 
//							break;
//						i--;
//					}
//					int delta = insertionOffset + insertedChars - i;
//					if (delta > 0) {
//						Editor.Caret.Offset -= delta;
//						Editor.Remove (insertionOffset + insertedChars - delta, delta);
//					}
//				}
//			}

				var curLine = startLine;


				while (true) {
					if (curLine == null)
						break;
					var curLineOffset = curLine.Offset;
					stateTracker.UpdateEngine (curLineOffset);
					if (!stateTracker.Engine.IsInsideOrdinaryCommentOrString) {
						// The Indent engine doesn't really handle pre processor directives very well.
						if (IsPreprocessorDirective (curLine)) {
							Editor.Replace (curLineOffset, curLine.Length, stateTracker.Engine.NewLineIndent + Editor.GetTextAt (curLine).TrimStart ());
						} else {
							int pos = curLineOffset;
							string curIndent = curLine.GetIndentation (textEditorData.Document);
							int nlwsp = curIndent.Length;


							if (!stateTracker.Engine.LineBeganInsideMultiLineComment || (nlwsp < curLine.LengthIncludingDelimiter && textEditorData.Document.GetCharAt (curLineOffset + nlwsp) == '*')) {
								// Possibly replace the indent
								stateTracker.UpdateEngine (curLineOffset + curLine.Length);
								string newIndent = stateTracker.Engine.ThisLineIndent;
								if (newIndent != curIndent) {
									if (CompletionWindowManager.IsVisible) {
										if (pos < CompletionWindowManager.CodeCompletionContext.TriggerOffset)
											CompletionWindowManager.CodeCompletionContext.TriggerOffset -= nlwsp;
									}
									textEditorData.Replace (pos, nlwsp, newIndent);
									textEditorData.Document.CommitLineUpdate (textEditorData.Caret.Line);
								}
							}
						}
					}

					if (curLine == endLine)
						break;
					curLine = curLine.NextLine;
				}
				textEditorData.FixVirtualIndentation ();
			}
		} 

		public static bool OnTheFlyFormatting {
			get {
				return PropertyService.Get ("OnTheFlyFormatting", true);
			}
			set {
				PropertyService.Set ("OnTheFlyFormatting", value);
			}
		}
		
		void RunFormatter (DocumentLocation location)
		{
			if (OnTheFlyFormatting && textEditorData != null && !(textEditorData.CurrentMode is TextLinkEditMode) && !(textEditorData.CurrentMode is InsertionCursorEditMode)) {
				OnTheFlyFormatter.Format (Document, location);
			}
		}

		public override void Initialize ()
		{
			base.Initialize ();


			if (textEditorData != null) {
				textEditorData.Options.Changed += delegate {
					textEditorData.IndentationTracker = new IndentVirtualSpaceManager (
						textEditorData,
						new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (Policy, TextStylePolicy), textEditorData)
					);
				};
				textEditorData.IndentationTracker = new IndentVirtualSpaceManager (
					textEditorData,
					new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (Policy, TextStylePolicy), textEditorData)
				);
			}

			InitTracker ();
			Document.Editor.Paste += HandleTextPaste;
		}

		/*		void TextCut (object sender, ReplaceEventArgs e)
		{
			if (!string.IsNullOrEmpty (e.Value) || e.Count == 0)
				return;
			RunFormatterAt (e.Offset);
		}*/


		#region Sharing the tracker

		void InitTracker ()
		{
			stateTracker = new DocumentStateTracker<CSharpIndentEngine> (new CSharpIndentEngine (Policy, TextStylePolicy), textEditorData);
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

		void CheckXmlCommentCloseTag (char keyChar)
		{
			if (keyChar == '>' && stateTracker.Engine.IsInsideDocLineComment) {
				var location = Editor.Caret.Location;
				string lineText = Editor.GetLineText (Editor.Caret.Line);
				int startIndex = Math.Min (location.Column - 1, lineText.Length - 1);
				while (startIndex >= 0 && lineText [startIndex] != '<') {
					--startIndex;
					if (lineText [startIndex] == '/') {
						// already closed.
						startIndex = -1;
						break;
					}
				}
				if (startIndex >= 0) {
					int endIndex = startIndex;
					while (endIndex <= location.Column && endIndex < lineText.Length && !Char.IsWhiteSpace (lineText [endIndex])) {
						endIndex++;
					}
					string tag = endIndex - startIndex - 1 > 0 ? lineText.Substring (startIndex + 1, endIndex - startIndex - 2) : null;
					if (!string.IsNullOrEmpty (tag) && CSharpCompletionEngine.CommentTags.Any (t => t == tag)) {
						Editor.Document.Insert (Editor.Caret.Offset, "</" + tag + ">", AnchorMovementType.BeforeInsertion);
					}
				}
			}
		}

		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool skipFormatting = StateTracker.Engine.IsInsideOrdinaryCommentOrString ||
					StateTracker.Engine.IsInsidePreprocessorDirective;

			cursorPositionBeforeKeyPress = textEditorData.Caret.Offset;
			bool isSomethingSelected = textEditorData.IsSomethingSelected;
			if (key == Gdk.Key.BackSpace && textEditorData.Caret.Offset == lastInsertedSemicolon) {
				textEditorData.Document.Undo ();
				lastInsertedSemicolon = -1;
				return false;
			}
			lastInsertedSemicolon = -1;
			if (keyChar == ';' && !(textEditorData.CurrentMode is TextLinkEditMode) && !DoInsertTemplate () && !isSomethingSelected && PropertyService.Get (
				"SmartSemicolonPlacement",
				false
			)) {
				bool retval = base.KeyPress (key, keyChar, modifier);
				DocumentLine curLine = textEditorData.Document.GetLine (textEditorData.Caret.Line);
				string text = textEditorData.Document.GetTextAt (curLine);
				if (!(text.EndsWith (";", StringComparison.Ordinal) || text.Trim ().StartsWith ("for", StringComparison.Ordinal))) {
					int guessedOffset;

					if (GuessSemicolonInsertionOffset (textEditorData, curLine, textEditorData.Caret.Offset, out guessedOffset)) {
						using (var undo = textEditorData.OpenUndoGroup ()) {
							textEditorData.Remove (textEditorData.Caret.Offset - 1, 1);
							textEditorData.Caret.Offset = guessedOffset;
							lastInsertedSemicolon = textEditorData.Caret.Offset + 1;
							retval = base.KeyPress (key, keyChar, modifier);
						}
					}
				}
				using (var undo = textEditorData.OpenUndoGroup ()) {
					if (OnTheFlyFormatting && textEditorData != null && !(textEditorData.CurrentMode is TextLinkEditMode) && !(textEditorData.CurrentMode is InsertionCursorEditMode)) {
						OnTheFlyFormatter.FormatStatmentAt (Document, textEditorData.Caret.Location);
					}
				}
				return retval;
			}
			
			if (key == Gdk.Key.Tab) {
				stateTracker.UpdateEngine ();
				if (stateTracker.Engine.IsInsideStringLiteral && !textEditorData.IsSomethingSelected) {
					var lexer = new CSharpCompletionEngineBase.MiniLexer (textEditorData.Document.GetTextAt (0, textEditorData.Caret.Offset));
					lexer.Parse ();
					if (lexer.IsInString) {
						textEditorData.InsertAtCaret ("\\t");
						return false;
					}
				}
			}


			if (key == Gdk.Key.Tab && DefaultSourceEditorOptions.Instance.TabIsReindent && !CompletionWindowManager.IsVisible && !(textEditorData.CurrentMode is TextLinkEditMode) && !DoInsertTemplate () && !isSomethingSelected) {
				int cursor = textEditorData.Caret.Offset;
				if (stateTracker.Engine.IsInsideVerbatimString && cursor > 0 && cursor < textEditorData.Document.TextLength && textEditorData.GetCharAt (cursor - 1) == '"')
					stateTracker.UpdateEngine (cursor + 1);

				if (stateTracker.Engine.IsInsideVerbatimString) {
					// insert normal tab inside @" ... "
					if (textEditorData.IsSomethingSelected) {
						textEditorData.SelectedText = "\t";
					} else {
						textEditorData.Insert (cursor, "\t");
					}
					textEditorData.Document.CommitLineUpdate (textEditorData.Caret.Line);
				} else if (cursor >= 1) {
					if (textEditorData.Caret.Column > 1) {
						int delta = cursor - cursorPositionBeforeKeyPress;
						if (delta < 2 && delta > 0) {
							textEditorData.Remove (cursor - delta, delta);
							textEditorData.Caret.Offset = cursor - delta;
							textEditorData.Document.CommitLineUpdate (textEditorData.Caret.Line);
						}
					}
					stateTracker.UpdateEngine ();
					DoReSmartIndent ();
				}
				return false;
			}

			//do the smart indent
			if (textEditorData.Options.IndentStyle == IndentStyle.Smart || textEditorData.Options.IndentStyle == IndentStyle.Virtual) {
				bool retval;
				//capture some of the current state
				int oldBufLen = textEditorData.Length;
				int oldLine = textEditorData.Caret.Line + 1;
				bool hadSelection = textEditorData.IsSomethingSelected;
				bool reIndent = false;

				//pass through to the base class, which actually inserts the character
				//and calls HandleCodeCompletion etc to handles completion
				using (var undo = textEditorData.OpenUndoGroup ()) {
					DoPreInsertionSmartIndent (key);
				}

				bool automaticReindent;
				// need to be outside of an undo group - otherwise it interferes with other text editor extension
				// esp. the documentation insertion undo steps.
				retval = base.KeyPress (key, keyChar, modifier);
				//handle inserted characters
				if (textEditorData.Caret.Offset <= 0 || textEditorData.IsSomethingSelected)
					return retval;
				
				lastCharInserted = TranslateKeyCharForIndenter (key, keyChar, textEditorData.GetCharAt (textEditorData.Caret.Offset - 1));
				if (lastCharInserted == '\0')
					return retval;

				using (var undo = textEditorData.OpenUndoGroup ()) {
					stateTracker.UpdateEngine ();

					if (key == Gdk.Key.Return && modifier == Gdk.ModifierType.ControlMask) {
						FixLineStart (textEditorData, stateTracker, textEditorData.Caret.Line + 1);
					} else {
						if (!(oldLine == textEditorData.Caret.Line + 1 && lastCharInserted == '\n') && (oldBufLen != textEditorData.Length || lastCharInserted != '\0'))
							DoPostInsertionSmartIndent (lastCharInserted, hadSelection, out reIndent);
					}
					//reindent the line after the insertion, if needed
					//N.B. if the engine says we need to reindent, make sure that it's because a char was 
					//inserted rather than just updating the stack due to moving around

					stateTracker.UpdateEngine ();
					automaticReindent = (stateTracker.Engine.NeedsReindent && lastCharInserted != '\0');
					if (key == Gdk.Key.Return && (reIndent || automaticReindent))
						DoReSmartIndent ();
				}

				if (key != Gdk.Key.Return && (reIndent || automaticReindent)) {
					using (var undo = textEditorData.OpenUndoGroup ()) {
						DoReSmartIndent ();
					}
				}
				if (!skipFormatting) {
					if (keyChar == ';' || keyChar == '}') {
						using (var undo = textEditorData.OpenUndoGroup ()) {
							if (OnTheFlyFormatting && textEditorData != null && !(textEditorData.CurrentMode is TextLinkEditMode) && !(textEditorData.CurrentMode is InsertionCursorEditMode)) {
								OnTheFlyFormatter.FormatStatmentAt (Document, textEditorData.Caret.Location);
							}
						}
					}
				}

				stateTracker.UpdateEngine ();
				lastCharInserted = '\0';
				CheckXmlCommentCloseTag (keyChar);
				return retval;
			}

			if (textEditorData.Options.IndentStyle == IndentStyle.Auto && DefaultSourceEditorOptions.Instance.TabIsReindent && key == Gdk.Key.Tab) {
				bool retval = base.KeyPress (key, keyChar, modifier);
				DoReSmartIndent ();
				CheckXmlCommentCloseTag (keyChar);
				return retval;
			}

			//pass through to the base class, which actually inserts the character
			//and calls HandleCodeCompletion etc to handles completion
			var result = base.KeyPress (key, keyChar, modifier);

			CheckXmlCommentCloseTag (keyChar);

			if (!skipFormatting && keyChar == '}')
				RunFormatter (new DocumentLocation (textEditorData.Caret.Location.Line, textEditorData.Caret.Location.Column));
			return result;
		}

		static bool IsSemicolonalreadyPlaced (TextEditorData data, int caretOffset)
		{
			for (int pos2 = caretOffset - 1; pos2 --> 0;) {
				var ch2 = data.Document.GetCharAt (pos2);
				if (ch2 == ';') {
					return true;
				}
				if (!char.IsWhiteSpace (ch2))
					return false;
			}
			return false;
		}

		public static bool GuessSemicolonInsertionOffset (TextEditorData data, IDocumentLine curLine, int caretOffset, out int outOffset)
		{
			int lastNonWsOffset = caretOffset;
			char lastNonWsChar = '\0';
			outOffset = caretOffset;
			int max = curLine.EndOffset;
	//		if (caretOffset - 2 >= curLine.Offset && data.Document.GetCharAt (caretOffset - 2) == ')' && !IsSemicolonalreadyPlaced (data, caretOffset))
	//			return false;

			int end = caretOffset;
			while (end > 1 && char.IsWhiteSpace (data.GetCharAt (end)))
				end--;
			int end2 = end;
			while (end2 > 1 && char.IsLetter(data.GetCharAt (end2 - 1)))
				end2--;
			if (end != end2) {
				string token = data.GetTextBetween (end2, end + 1);
				// guess property context
				if (token == "get" || token == "set")
					return false;
			}

			bool isInString = false , isInChar= false , isVerbatimString= false;
			bool isInLineComment = false , isInBlockComment= false;
			bool firstChar = true;
			for (int pos = caretOffset; pos < max; pos++) {
				if (pos == caretOffset) {
					if (isInString || isInChar || isVerbatimString || isInLineComment || isInBlockComment) {
						outOffset = pos;
						return true;
					}
				}
				char ch = data.Document.GetCharAt (pos);
				switch (ch) {
				case '}':
					if (firstChar && !IsSemicolonalreadyPlaced (data, caretOffset))
						return false;
					break;
				case '/':
					if (isInBlockComment) {
						if (pos > 0 && data.Document.GetCharAt (pos - 1) == '*') 
							isInBlockComment = false;
					} else if (!isInString && !isInChar && pos + 1 < max) {
						char nextChar = data.Document.GetCharAt (pos + 1);
						if (nextChar == '/') {
							outOffset = lastNonWsOffset;
							return true;
						}
						if (!isInLineComment && nextChar == '*') {
							outOffset = lastNonWsOffset;
							return true;
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
				if (!char.IsWhiteSpace (ch)) {
					firstChar = false;
					lastNonWsOffset = pos;
					lastNonWsChar = ch;
				}
			}
			// if the line ends with ';' the line end is not the correct place for a new semicolon.
			if (lastNonWsChar == ';')
				return false;
			outOffset = lastNonWsOffset;
			return true;
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
			if (start < 0 || end >= textEditorData.Length || textEditorData.IsSomethingSelected)
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
			case '}':
			case ';':
				reIndent = true;
				break;
			case '\n':
				if (FixLineStart (textEditorData, stateTracker, stateTracker.Engine.LineNumber)) 
					return;
				//newline always reindents unless it's had special handling
				reIndent = true;
				break;
			}
		}

		public static bool FixLineStart (TextEditorData textEditorData, DocumentStateTracker<CSharpIndentEngine> stateTracker, int lineNumber)
		{
			if (lineNumber > DocumentLocation.MinLine) {
				DocumentLine line = textEditorData.Document.GetLine (lineNumber);
				if (line == null)
					return false;

				DocumentLine prevLine = textEditorData.Document.GetLine (lineNumber - 1);
				if (prevLine == null)
					return false;
				string trimmedPreviousLine = textEditorData.Document.GetTextAt (prevLine).TrimStart ();

				//xml doc comments
				//check previous line was a doc comment
				//check there's a following line?
				if (trimmedPreviousLine.StartsWith ("///", StringComparison.Ordinal)) {
					if (textEditorData.GetTextAt (line.Offset, line.Length).TrimStart ().StartsWith ("///", StringComparison.Ordinal))
						return false;
					//check that the newline command actually inserted a newline
					textEditorData.EnsureCaretIsNotVirtual ();
					string nextLine = textEditorData.Document.GetTextAt (textEditorData.Document.GetLine (lineNumber + 1)).TrimStart ();

					if (trimmedPreviousLine.Length > "///".Length || nextLine.StartsWith ("///", StringComparison.Ordinal)) {
						var insertionPoint = line.Offset + line.GetIndentation (textEditorData.Document).Length;
						textEditorData.Insert (insertionPoint, "/// ");
						return true;
					}
					//multi-line comments
				} else if (stateTracker.Engine.IsInsideMultiLineComment) {
					if (textEditorData.GetTextAt (line.Offset, line.Length).TrimStart ().StartsWith ("*", StringComparison.Ordinal))
						return false;
					textEditorData.EnsureCaretIsNotVirtual ();
					string commentPrefix = string.Empty;
					if (trimmedPreviousLine.StartsWith ("* ", StringComparison.Ordinal)) {
						commentPrefix = "* ";
					} else if (trimmedPreviousLine.StartsWith ("/**", StringComparison.Ordinal) || trimmedPreviousLine.StartsWith ("/*", StringComparison.Ordinal)) {
						commentPrefix = " * ";
					} else if (trimmedPreviousLine.StartsWith ("*", StringComparison.Ordinal)) {
						commentPrefix = "*";
					}

					int indentSize = line.GetIndentation (textEditorData.Document).Length;
					var insertedText = prevLine.GetIndentation (textEditorData.Document) + commentPrefix;
					textEditorData.Replace (line.Offset, indentSize, insertedText);
					textEditorData.Caret.Offset = line.Offset + insertedText.Length;
					return true;
				} else if (stateTracker.Engine.IsInsideStringLiteral) {
					var lexer = new CSharpCompletionEngineBase.MiniLexer (textEditorData.Document.GetTextAt (0, prevLine.EndOffset));
					lexer.Parse ();
					if (!lexer.IsInString)
						return false;
					textEditorData.EnsureCaretIsNotVirtual ();
					textEditorData.Insert (prevLine.Offset + prevLine.Length, "\" +");

					int indentSize = line.GetIndentation (textEditorData.Document).Length;
					var insertedText = prevLine.GetIndentation (textEditorData.Document) + (trimmedPreviousLine.StartsWith ("\"", StringComparison.Ordinal) ? "" : "\t") + "\"";
					textEditorData.Replace (line.Offset, indentSize, insertedText);
					return true;
				}
			}
			return false;
		}

		//does re-indenting and cursor positioning
		void DoReSmartIndent ()
		{
			DoReSmartIndent (textEditorData.Caret.Offset);
		}

		void DoReSmartIndent (int cursor)
		{
			if (stateTracker.Engine.LineBeganInsideVerbatimString || stateTracker.Engine.LineBeganInsideMultiLineComment)
				return;
			DocumentLine line = textEditorData.Document.GetLineByOffset (cursor);
//			stateTracker.UpdateEngine (line.Offset);
			// Get context to the end of the line w/o changing the main engine's state
			var ctx = (CSharpIndentEngine)stateTracker.Engine.Clone ();
			for (int max = cursor; max < line.EndOffset; max++) {
				ctx.Push (textEditorData.Document.GetCharAt (max));
			}
			int pos = line.Offset;
			string curIndent = line.GetIndentation (textEditorData.Document);
			int nlwsp = curIndent.Length;
			int offset = cursor > pos + nlwsp ? cursor - (pos + nlwsp) : 0;
			if (!stateTracker.Engine.LineBeganInsideMultiLineComment || (nlwsp < line.LengthIncludingDelimiter && textEditorData.Document.GetCharAt (line.Offset + nlwsp) == '*')) {
				// Possibly replace the indent
				string newIndent = ctx.ThisLineIndent;
				int newIndentLength = newIndent.Length;
				if (newIndent != curIndent) {
					if (CompletionWindowManager.IsVisible) {
						if (pos < CompletionWindowManager.CodeCompletionContext.TriggerOffset)
							CompletionWindowManager.CodeCompletionContext.TriggerOffset -= nlwsp;
					}

					newIndentLength = textEditorData.Replace (pos, nlwsp, newIndent);
					textEditorData.Document.CommitLineUpdate (textEditorData.Caret.Line);
					// Engine state is now invalid
					stateTracker.ResetEngineToPosition (pos);
					CompletionWindowManager.HideWindow ();
				}
				pos += newIndentLength;
			} else {
				pos += curIndent.Length;
			}

			pos += offset;

			textEditorData.FixVirtualIndentation ();
		}

		/*
		[MonoDevelop.Components.Commands.CommandHandler (MonoDevelop.Ide.CodeFormatting.CodeFormattingCommands.FormatBuffer)]
		public void FormatBuffer ()
		{
			Console.WriteLine ("format buffer!");
			ITypeResolveContext dom = TypeSystemService.GetProjectDom (Document.Project);
			OnTheFlyFormatter.Format (this.textEditorData, dom);
		}*/
	}
}
