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
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.SourceEditor;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.Editor;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory;
using MonoDevelop.Ide.Editor;
using Atk;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.CSharp.NRefactoryWrapper;

namespace MonoDevelop.CSharp.Formatting
{
	class CSharpTextEditorIndentation : TextEditorExtension
	{
		internal CacheIndentEngine stateTracker;
		int cursorPositionBeforeKeyPress;

		readonly IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);

		CSharpFormattingPolicy Policy {
			get {
				if (Document != null && Document.Project != null && Document.Project.Policies != null) {
					return Document.Project.Policies.Get<CSharpFormattingPolicy> (types);
				}
				return MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
			}
		}

		TextStylePolicy TextStylePolicy {
			get {
				if (Document != null && Document.Project != null && Document.Project.Policies != null) {
					return Document.Project.Policies.Get<TextStylePolicy> (types);
				}
				return MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> (types);
			}
		}

		char lastCharInserted;

		static CSharpTextEditorIndentation ()
		{
			CompletionWindowManager.WordCompleted += delegate(object sender, CodeCompletionContextEventArgs e) {
				var editor = e.Widget as IServiceProvider;
				if (editor == null)
					return;
				var extension = editor.GetService (typeof(CSharpTextEditorIndentation)) as CSharpTextEditorIndentation;
				if (extension == null)
					return;
				extension.SafeUpdateIndentEngine (extension.Editor.CaretOffset);
				if (extension.stateTracker.NeedsReindent)
					extension.DoReSmartIndent ();
			};
		}

		internal void SafeUpdateIndentEngine (int offset)
		{
			try {
				stateTracker.Update (offset);
			} catch (Exception e) {
				LoggingService.LogError ("Error while updating the indentation engine", e);
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

		void RunFormatter (MonoDevelop.Ide.Editor.DocumentLocation location)
		{

			if (OnTheFlyFormatting && Editor != null && Editor.EditMode == EditMode.Edit) {
				OnTheFlyFormatter.Format (Document, location);
			}
		}

		protected override void Initialize ()
		{
			base.Initialize ();


			if (Editor != null) {
				Editor.Options.Changed += HandleTextOptionsChanged;
				HandleTextOptionsChanged (this, EventArgs.Empty);
				Editor.TextChanging += HandleTextReplacing;
				Editor.TextChanged += HandleTextReplaced;
			}
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ActiveConfigurationChanged += HandleTextOptionsChanged;
		}

		bool indentationDisabled;

		void HandleTextOptionsChanged (object sender, EventArgs e)
		{
			var policy = Policy.CreateOptions ();
			var options = Editor.CreateNRefactoryTextEditorOptions ();
			options.IndentBlankLines = true;
			IStateMachineIndentEngine indentEngine;
			try {
				var csharpIndentEngine = new CSharpIndentEngine (new DocumentWrapper (Editor), options, policy);
				//csharpIndentEngine.EnableCustomIndentLevels = true;
				foreach (var symbol in MonoDevelop.CSharp.Highlighting.CSharpSyntaxMode.GetDefinedSymbols (Document.Project)) {
					csharpIndentEngine.DefineSymbol (symbol);
				}
				indentEngine = csharpIndentEngine;
			} catch (Exception ex) {
				LoggingService.LogError ("Error while creating the c# indentation engine", ex);
				indentEngine = new NullIStateMachineIndentEngine (new DocumentWrapper (Editor));
			}
			stateTracker = new CacheIndentEngine (indentEngine);
			if (DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.Auto) {
				((IInternalEditorExtensions)Editor).SetIndentationTracker (null);
			} else {
				((IInternalEditorExtensions)Editor).SetIndentationTracker (new IndentVirtualSpaceManager (Editor, stateTracker));
			}

			indentationDisabled = DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.Auto || DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.None;
			if (indentationDisabled) {
				((IInternalEditorExtensions)Editor).SetTextPasteHandler (null);
			} else {
				((IInternalEditorExtensions)Editor).SetTextPasteHandler (new CSharpTextPasteHandler (this, stateTracker, options, policy));
			}
		}

		public override void Dispose ()
		{
			if (Editor != null) {
				((IInternalEditorExtensions)Editor).SetTextPasteHandler (null);
				Editor.Options.Changed -= HandleTextOptionsChanged;
				((IInternalEditorExtensions)Editor).SetIndentationTracker (null);
				Editor.TextChanging -= HandleTextReplacing;
				Editor.TextChanged -= HandleTextReplaced;
			}
			IdeApp.Workspace.ActiveConfigurationChanged -= HandleTextOptionsChanged;
			stateTracker = null;
			base.Dispose ();
		}

		bool? wasInVerbatimString;

		void HandleTextReplaced (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			stateTracker.ResetEngineToPosition (e.Offset); 
			if (wasInVerbatimString == null)
				return;
			if (e.RemovalLength != 1 /*|| textEditorData.Document.CurrentAtomicUndoOperationType == OperationType.Format*/)
				return;
			SafeUpdateIndentEngine (Math.Min (Editor.Length, e.Offset + e.InsertionLength + 1));
			if (wasInVerbatimString == true && !stateTracker.IsInsideVerbatimString) {
				Editor.TextChanging -= HandleTextReplacing;
				Editor.TextChanged -= HandleTextReplaced;
				ConvertVerbatimStringToNormal (Editor, e.Offset + e.InsertionLength + 1);
				Editor.TextChanging += HandleTextReplacing;
				Editor.TextChanged += HandleTextReplaced;
			}
		}

		void HandleTextReplacing (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			wasInVerbatimString = null;
			var o = e.Offset + e.RemovalLength;
			if (o < 0 || o + 1 > Editor.Length || e.RemovalLength != 1/* || textEditorData.Document.IsInUndo*/) {
				return;
			}
			if (Editor.GetCharAt (o) != '"')
				return;
			SafeUpdateIndentEngine (o + 1);
			wasInVerbatimString = stateTracker.IsInsideVerbatimString;
		}

		internal static string ConvertToStringLiteral (string text)
		{
			var result = new StringBuilder ();
			foreach (var ch in text) {
				switch (ch) {
				case '\t':
					result.Append ("\\t");
					break;
				case '"':
					result.Append ("\\\"");
					break;
				case '\n':
					result.Append ("\\n");
					break;
				case '\r':
					result.Append ("\\r");
					break;
				case '\\':
					result.Append ("\\\\");
					break;
				default:
					result.Append (ch);
					break;
				}
			}
			return result.ToString ();
		}

		static void ConvertNormalToVerbatimString (ITextDocument textEditorData, int offset)
		{
			var endOffset = offset;
			while (endOffset < textEditorData.Length) {
				char ch = textEditorData.GetCharAt (endOffset);
				if (ch == '\\') {
					if (endOffset + 1 < textEditorData.Length && NewLine.IsNewLine (textEditorData.GetCharAt (endOffset + 1)))
						return;

					endOffset += 2;
					continue;
				}
				if (ch == '"')
					break;
				if (NewLine.IsNewLine (ch))
					return;
				endOffset++;
			}
			if (offset > endOffset || endOffset == textEditorData.Length)
				return;
			var plainText = TextPasteUtils.StringLiteralPasteStrategy.Instance.Decode (textEditorData.GetTextAt (offset, endOffset - offset));
			var newText = TextPasteUtils.VerbatimStringStrategy.Encode (plainText);
			textEditorData.Replace (offset, endOffset - offset, newText);
		}

		static void ConvertVerbatimStringToNormal (ITextDocument textEditorData, int offset)
		{
			var endOffset = offset;
			while (endOffset < textEditorData.Length) {
				char ch = textEditorData.GetCharAt (endOffset);
				if (ch == '"' && (endOffset + 1 < textEditorData.Length && textEditorData.GetCharAt (endOffset + 1) == '"')) {
					endOffset += 2;
					continue;
				}
				if (ch == '"') {
					break;
				}
				endOffset++;
			}
			var plainText = TextPasteUtils.VerbatimStringStrategy.Decode (textEditorData.GetTextAt (offset, endOffset - offset));
			var newText = TextPasteUtils.StringLiteralPasteStrategy.Instance.Encode (plainText);
			textEditorData.Replace (offset, endOffset - offset, newText);
		}

		internal IStateMachineIndentEngine StateTracker { get { return stateTracker; } }

		public bool DoInsertTemplate ()
		{
			string word = CodeTemplate.GetWordBeforeCaret (Editor);
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplates (CSharpFormatter.MimeType)) {
				if (template.Shortcut == word)
					return true;
			}
			return false;
		}

		int lastInsertedSemicolon = -1;

		void CheckXmlCommentCloseTag (char keyChar)
		{
			if (keyChar == '>' && stateTracker.IsInsideDocLineComment) {
				var location = Editor.CaretLocation;
				string lineText = Editor.GetLineText (Editor.CaretLine);
				int startIndex = Math.Min (location.Column - 2, lineText.Length - 1);
				while (startIndex >= 0 && lineText [startIndex] != '<') {
					--startIndex;
					if (lineText [startIndex] == '/') {
						// already closed.
						startIndex = -1;
						break;
					}
				}
				if (startIndex >= 0) {
					int endIndex = startIndex + 1;
					while (endIndex <= location.Column - 1 && endIndex < lineText.Length && Char.IsLetter (lineText [endIndex])) {
						endIndex++;
					}
					string tag = endIndex - startIndex > 0 ? lineText.Substring (startIndex + 1, endIndex - startIndex - 1) : null;
					if (!string.IsNullOrEmpty (tag) && CSharpCompletionEngine.CommentTags.Any (t => t == tag)) {
						var caretOffset = Editor.CaretOffset;
						Editor.Insert (caretOffset, "</" + tag + ">");
						Editor.CaretOffset = caretOffset;
					}
				}
			}
		}

		internal void ReindentOnTab ()
		{
			int cursor = Editor.CaretOffset;
			if (stateTracker.IsInsideVerbatimString && cursor > 0 && cursor < Editor.Length && Editor.GetCharAt (cursor - 1) == '"')
				SafeUpdateIndentEngine (cursor + 1);
			if (stateTracker.IsInsideVerbatimString) {
				// insert normal tab inside @" ... "
				if (Editor.IsSomethingSelected) {
					Editor.SelectedText = "\t";
				}
				else {
					Editor.Insert (cursor, "\t");
				}
				// textEditorData.Document.CommitLineUpdate (textEditorData.CaretLine);
			}
			else if (cursor >= 1) {
				if (Editor.CaretColumn > 1) {
					int delta = cursor - cursorPositionBeforeKeyPress;
					if (delta < 2 && delta > 0) {
						Editor.Remove (cursor - delta, delta);
						Editor.CaretOffset = cursor - delta;
						// textEditorData.Document.CommitLineUpdate (textEditorData.CaretLine);
					}
				}
				SafeUpdateIndentEngine (Editor.CaretOffset);
				DoReSmartIndent ();
			}
		}

		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool skipFormatting = StateTracker.IsInsideOrdinaryCommentOrString ||
			                      StateTracker.IsInsidePreprocessorDirective;
			cursorPositionBeforeKeyPress = Editor.CaretOffset;
			bool isSomethingSelected = Editor.IsSomethingSelected;
			if (key == Gdk.Key.BackSpace && Editor.CaretOffset == lastInsertedSemicolon) {
				Editor.Undo ();
				lastInsertedSemicolon = -1;
				return false;
			}
			lastInsertedSemicolon = -1;
			if (keyChar == ';' && Editor.EditMode == EditMode.Edit && !DoInsertTemplate () && !isSomethingSelected && PropertyService.Get (
				    "SmartSemicolonPlacement",
				    false
			    ) && !(stateTracker.IsInsideComment || stateTracker.IsInsideString)) {
				bool retval = base.KeyPress (key, keyChar, modifier);
				var curLine = Editor.GetLine (Editor.CaretLine);
				string text = Editor.GetTextAt (curLine);
				if (!(text.EndsWith (";", StringComparison.Ordinal) || text.Trim ().StartsWith ("for", StringComparison.Ordinal))) {
					int guessedOffset;

					if (GuessSemicolonInsertionOffset (Editor, curLine, Editor.CaretOffset, out guessedOffset)) {
						using (var undo = Editor.OpenUndoGroup ()) {
							Editor.Remove (Editor.CaretOffset - 1, 1);
							Editor.CaretOffset = guessedOffset;
							lastInsertedSemicolon = Editor.CaretOffset + 1;
							retval = base.KeyPress (key, keyChar, modifier);
						}
					}
				}
				using (var undo = Editor.OpenUndoGroup ()) {
					if (OnTheFlyFormatting && Editor != null && Editor.EditMode == EditMode.Edit) {
						OnTheFlyFormatter.FormatStatmentAt (Document, Editor.CaretLocation);
					}
				}
				return retval;
			}
			
			if (key == Gdk.Key.Tab) {
				SafeUpdateIndentEngine (Editor.CaretOffset);
				if (stateTracker.IsInsideStringLiteral && !Editor.IsSomethingSelected) {
					var lexer = new CSharpCompletionEngineBase.MiniLexer (Editor.GetTextAt (0, Editor.CaretOffset));
					lexer.Parse ();
					if (lexer.IsInString) {
						Editor.InsertAtCaret ("\\t");
						return false;
					}
				}
			}


			if (key == Gdk.Key.Tab && DefaultSourceEditorOptions.Instance.TabIsReindent && !CompletionWindowManager.IsVisible && Editor.EditMode == EditMode.Edit && !DoInsertTemplate () && !isSomethingSelected) {
				ReindentOnTab ();

				return false;
			}

			SafeUpdateIndentEngine (Editor.CaretOffset);
			if (!stateTracker.IsInsideOrdinaryCommentOrString) {
				if (keyChar == '@') {
					var retval = base.KeyPress (key, keyChar, modifier);
					int cursor = Editor.CaretOffset;
					if (cursor < Editor.Length && Editor.GetCharAt (cursor) == '"')
						ConvertNormalToVerbatimString (Editor, cursor + 1);
					return retval;
				}
			}


			//do the smart indent
			if (!indentationDisabled) {
				bool retval;
				//capture some of the current state
				int oldBufLen = Editor.Length;
				int oldLine = Editor.CaretLine + 1;
				bool reIndent = false;

				//pass through to the base class, which actually inserts the character
				//and calls HandleCodeCompletion etc to handles completion
				using (var undo = Editor.OpenUndoGroup ()) {
					DoPreInsertionSmartIndent (key);
				}
				wasInStringLiteral = stateTracker.IsInsideStringLiteral;
				bool automaticReindent;
				// need to be outside of an undo group - otherwise it interferes with other text editor extension
				// esp. the documentation insertion undo steps.
				retval = base.KeyPress (key, keyChar, modifier);
				//handle inserted characters
				if (Editor.CaretOffset <= 0 || Editor.IsSomethingSelected)
					return retval;
				
				lastCharInserted = TranslateKeyCharForIndenter (key, keyChar, Editor.GetCharAt (Editor.CaretOffset - 1));
				if (lastCharInserted == '\0')
					return retval;
				using (var undo = Editor.OpenUndoGroup ()) {
					SafeUpdateIndentEngine (Editor.CaretOffset);

					if (key == Gdk.Key.Return && modifier == Gdk.ModifierType.ControlMask) {
						FixLineStart (Editor, stateTracker, Editor.CaretLine + 1);
					} else {
						if (!(oldLine == Editor.CaretLine + 1 && lastCharInserted == '\n') && (oldBufLen != Editor.Length || lastCharInserted != '\0')) {
							DoPostInsertionSmartIndent (lastCharInserted, out reIndent);
						} else {
							reIndent = lastCharInserted == '\n';
						}
					}
					//reindent the line after the insertion, if needed
					//N.B. if the engine says we need to reindent, make sure that it's because a char was 
					//inserted rather than just updating the stack due to moving around

					SafeUpdateIndentEngine (Editor.CaretOffset);
					// Automatically reindent in text link mode will cause the mode exiting, therefore we need to prevent that.
					automaticReindent = (stateTracker.NeedsReindent && lastCharInserted != '\0') && Editor.EditMode == EditMode.Edit;
					if (key == Gdk.Key.Return && (reIndent || automaticReindent)) {
						if (Editor.Options.IndentStyle == IndentStyle.Virtual) {
							if (Editor.GetLine (Editor.CaretLine).Length == 0)
								Editor.CaretColumn = Editor.GetVirtualIndentationColumn (Editor.CaretLine);
						} else {
							DoReSmartIndent ();
						}
					}
				}

				const string reindentChars = ";){}";
				if (reIndent || key != Gdk.Key.Return && key != Gdk.Key.Tab && automaticReindent && reindentChars.Contains (keyChar)) {
					using (var undo = Editor.OpenUndoGroup ()) {
						DoReSmartIndent ();
					}
				}

				if (!skipFormatting && !(stateTracker.IsInsideComment || stateTracker.IsInsideString)) {
					if (keyChar == ';' || keyChar == '}') {
						using (var undo = Editor.OpenUndoGroup ()) {
							if (OnTheFlyFormatting && Editor != null && Editor.EditMode == EditMode.Edit) {
								OnTheFlyFormatter.FormatStatmentAt (Document, Editor.CaretLocation);
							}
						}
					}
				}

				SafeUpdateIndentEngine (Editor.CaretOffset);
				lastCharInserted = '\0';
				CheckXmlCommentCloseTag (keyChar);
				return retval;
			}

			if (Editor.Options.IndentStyle == IndentStyle.Auto && DefaultSourceEditorOptions.Instance.TabIsReindent && key == Gdk.Key.Tab) {
				bool retval = base.KeyPress (key, keyChar, modifier);
				DoReSmartIndent ();
				CheckXmlCommentCloseTag (keyChar);
				return retval;
			}

			//pass through to the base class, which actually inserts the character
			//and calls HandleCodeCompletion etc to handles completion
			var result = base.KeyPress (key, keyChar, modifier);

			if (!indentationDisabled && (key == Gdk.Key.Return || key == Gdk.Key.KP_Enter)) {
				DoReSmartIndent ();
			}

			CheckXmlCommentCloseTag (keyChar);

			if (!skipFormatting && keyChar == '}')
				RunFormatter (new MonoDevelop.Ide.Editor.DocumentLocation (Editor.CaretLine, Editor.CaretColumn));
			return result;
		}

		static bool IsSemicolonalreadyPlaced (IReadonlyTextDocument data, int caretOffset)
		{
			for (int pos2 = caretOffset - 1; pos2-- > 0;) {
				var ch2 = data.GetCharAt (pos2);
				if (ch2 == ';') {
					return true;
				}
				if (!char.IsWhiteSpace (ch2))
					return false;
			}
			return false;
		}

		public static bool GuessSemicolonInsertionOffset (IReadonlyTextDocument data, MonoDevelop.Core.Text.ISegment curLine, int caretOffset, out int outOffset)
		{
			int lastNonWsOffset = caretOffset;
			char lastNonWsChar = '\0';
			outOffset = caretOffset;
			int max = curLine.EndOffset;

			int end = caretOffset;
			while (end > 1 && char.IsWhiteSpace (data.GetCharAt (end)))
				end--;
			int end2 = end;
			while (end2 > 1 && char.IsLetter (data.GetCharAt (end2 - 1)))
				end2--;
			if (end != end2) {
				string token = data.GetTextBetween (end2, end + 1);
				// guess property context
				if (token == "get" || token == "set")
					return false;
			}

			bool isInString = false, isInChar = false, isVerbatimString = false;
			bool isInLineComment = false, isInBlockComment = false;
			bool firstChar = true;
			for (int pos = caretOffset; pos < max; pos++) {
				if (pos == caretOffset) {
					if (isInString || isInChar || isVerbatimString || isInLineComment || isInBlockComment) {
						outOffset = pos;
						return true;
					}
				}
				char ch = data.GetCharAt (pos);
				switch (ch) {
				case '}':
					if (firstChar && !IsSemicolonalreadyPlaced (data, caretOffset))
						return false;
					break;
				case '/':
					if (isInBlockComment) {
						isInBlockComment &= pos <= 0 || data.GetCharAt (pos - 1) != '*';
					} else if (!isInString && !isInChar && pos + 1 < max) {
						char nextChar = data.GetCharAt (pos + 1);
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
					if (!(isInString || isInChar || isInLineComment || isInBlockComment) && pos + 1 < max && data.GetCharAt (pos + 1) == '"') {
						isInString = true;
						isVerbatimString = true;
						pos++;
					}
					break;
				case '"':
					if (!(isInChar || isInLineComment || isInBlockComment)) {
						if (isInString && isVerbatimString && pos + 1 < max && data.GetCharAt (pos + 1) == '"') {
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
			if (start < 0 || end >= Editor.Length || Editor.IsSomethingSelected)
				return;
			char ch = Editor.GetCharAt (start);
			if (ch == '"') {
				int sgn = Math.Sign (end - start);
				bool foundPlus = false;
				for (int max = start + sgn; max != end && max >= 0 && max < Editor.Length; max += sgn) {
					ch = Editor.GetCharAt (max);
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
							Editor.Remove (max, start - max);
							Editor.CaretOffset = max + 1;
						} else {
							Editor.Remove (start + sgn, max - start);
							Editor.CaretOffset = start;
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
				SafeUpdateIndentEngine (Editor.CaretOffset);
				HandleStringConcatinationDeletion (Editor.CaretOffset - 1, 0);
				break;
			case Gdk.Key.Delete:
				SafeUpdateIndentEngine (Editor.CaretOffset);
				HandleStringConcatinationDeletion (Editor.CaretOffset, Editor.Length);
				break;
			}
		}
		//special handling for certain characters just inserted , for comments etc
		void DoPostInsertionSmartIndent (char charInserted, out bool reIndent)
		{
			SafeUpdateIndentEngine (Editor.CaretOffset);
			reIndent = false;
			switch (charInserted) {
			case '}':
			case ';':
				reIndent = true;
				break;
			case '\n':
				if (FixLineStart (Editor, stateTracker, stateTracker.Location.Line))
					return;
				//newline always reindents unless it's had special handling
				reIndent = true;
				break;
			}
		}

		internal bool wasInStringLiteral;

		public bool FixLineStart (TextEditor textEditorData, IStateMachineIndentEngine stateTracker, int lineNumber)
		{
			if (lineNumber > 1) {
				var line = textEditorData.GetLine (lineNumber);
				if (line == null)
					return false;

				var prevLine = textEditorData.GetLine (lineNumber - 1);
				if (prevLine == null)
					return false;
				string trimmedPreviousLine = textEditorData.GetTextAt (prevLine).TrimStart ();

				//xml doc comments
				//check previous line was a doc comment
				//check there's a following line?
				if (trimmedPreviousLine.StartsWith ("///", StringComparison.Ordinal)) {
					if (textEditorData.GetTextAt (line.Offset, line.Length).TrimStart ().StartsWith ("///", StringComparison.Ordinal))
						return false;
					//check that the newline command actually inserted a newline
					textEditorData.EnsureCaretIsNotVirtual ();
					var nextLineSegment = textEditorData.GetLine (lineNumber + 1);
					string nextLine = nextLineSegment != null ? textEditorData.GetTextAt (nextLineSegment).TrimStart () : "";

					if (trimmedPreviousLine.Length > "///".Length || nextLine.StartsWith ("///", StringComparison.Ordinal)) {
						var insertionPoint = textEditorData.CaretOffset;
						textEditorData.Insert (insertionPoint, "/// ");
						textEditorData.CaretOffset = insertionPoint + "/// ".Length;
						return true;
					}
					//multi-line comments
				} else if (stateTracker.IsInsideMultiLineComment) {
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

					int indentSize = line.GetIndentation (textEditorData).Length;
					var insertedText = prevLine.GetIndentation (textEditorData) + commentPrefix;
					textEditorData.Replace (line.Offset, indentSize, insertedText);
					textEditorData.CaretOffset = line.Offset + insertedText.Length;
					return true;
				} else if (wasInStringLiteral) {
					var lexer = new CSharpCompletionEngineBase.MiniLexer (textEditorData.GetTextAt (0, prevLine.EndOffset).TrimEnd ());
					lexer.Parse ();
					if (!lexer.IsInString)
						return false;
					textEditorData.EnsureCaretIsNotVirtual ();
					textEditorData.Insert (prevLine.Offset + prevLine.Length, "\" +");

					int indentSize = textEditorData.CaretOffset - line.Offset;
					var insertedText = prevLine.GetIndentation (textEditorData) + (trimmedPreviousLine.StartsWith ("\"", StringComparison.Ordinal) ? "" : "\t") + "\"";
					textEditorData.Replace (line.Offset, indentSize, insertedText);
					return true;
				}
			}
			return false;
		}
		//does re-indenting and cursor positioning
		void DoReSmartIndent ()
		{
			DoReSmartIndent (Editor.CaretOffset);
		}

		void DoReSmartIndent (int cursor)
		{
			SafeUpdateIndentEngine (cursor);
			if (stateTracker.LineBeganInsideVerbatimString || stateTracker.LineBeganInsideMultiLineComment)
				return;
			if (DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.Auto) {
				Editor.FixVirtualIndentation ();
				return;
			}
			var line = Editor.GetLineByOffset (cursor);

			// Get context to the end of the line w/o changing the main engine's state
			var curTracker = stateTracker.Clone ();
			try {
				for (int max = cursor; max < line.EndOffset; max++) {
					curTracker.Push (Editor.GetCharAt (max));
				}
			} catch (Exception e) {
				LoggingService.LogError ("Exception during indentation", e);
			}
			
			int pos = line.Offset;
			string curIndent = line.GetIndentation (Editor);
			int nlwsp = curIndent.Length;
			int offset = cursor > pos + nlwsp ? cursor - (pos + nlwsp) : 0;
			if (!stateTracker.LineBeganInsideMultiLineComment || (nlwsp < line.LengthIncludingDelimiter && Editor.GetCharAt (line.Offset + nlwsp) == '*')) {
				// Possibly replace the indent
				string newIndent = curTracker.ThisLineIndent;
				int newIndentLength = newIndent.Length;
				if (newIndent != curIndent) {
					if (CompletionWindowManager.IsVisible) {
						if (pos < CompletionWindowManager.CodeCompletionContext.TriggerOffset)
							CompletionWindowManager.CodeCompletionContext.TriggerOffset -= nlwsp;
					}
					newIndentLength = newIndent.Length;
					Editor.Replace (pos, nlwsp, newIndent);
					//textEditorData.CommitLineUpdate (textEditorData.CaretLine);
					CompletionWindowManager.HideWindow ();
				}
				pos += newIndentLength;
			} else {
				pos += curIndent.Length;
			}

			pos += offset;

			Editor.FixVirtualIndentation ();
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
