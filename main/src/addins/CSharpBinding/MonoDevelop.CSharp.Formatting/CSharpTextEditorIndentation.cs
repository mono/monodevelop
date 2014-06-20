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

		internal TextEditor textEditorData {
			get {
				return document.Editor;
			}
		}

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
				extension.SafeUpdateIndentEngine (extension.textEditorData.CaretOffset);
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

		void RunFormatter (MonoDevelop.Core.Text.TextLocation location)
		{

			if (OnTheFlyFormatting && textEditorData != null && textEditorData.EditMode == EditMode.Edit) {
				OnTheFlyFormatter.Format (Document, location);
			}
		}

		public override void Initialize ()
		{
			base.Initialize ();


			if (textEditorData != null) {
				textEditorData.Options.Changed += HandleTextOptionsChanged;
				HandleTextOptionsChanged (this, EventArgs.Empty);
				textEditorData.TextChanging += HandleTextReplacing;
				textEditorData.TextChanged += HandleTextReplaced;
			}
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ActiveConfigurationChanged += HandleTextOptionsChanged;
		}


		void HandleTextOptionsChanged (object sender, EventArgs e)
		{
			var policy = Policy.CreateOptions ();
			var options = Editor.CreateNRefactoryTextEditorOptions ();
			options.IndentBlankLines = true;
			IStateMachineIndentEngine indentEngine;
			try {
				var csharpIndentEngine = new CSharpIndentEngine (new DocumentWrapper (textEditorData), options, policy);
				//csharpIndentEngine.EnableCustomIndentLevels = true;
				foreach (var symbol in MonoDevelop.CSharp.Highlighting.CSharpSyntaxMode.GetDefinedSymbols (document.Project)) {
					csharpIndentEngine.DefineSymbol (symbol);
				}
				indentEngine = csharpIndentEngine;
			} catch (Exception ex) {
				LoggingService.LogError ("Error while creating the c# indentation engine", ex);
				indentEngine = new NullIStateMachineIndentEngine (new DocumentWrapper (textEditorData));
			}
			stateTracker = new CacheIndentEngine (indentEngine);
			if (DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.Auto) {
				((IInternalEditorExtensions)textEditorData).SetIndentationTracker (null);
			} else {
				((IInternalEditorExtensions)textEditorData).SetIndentationTracker (new IndentVirtualSpaceManager (textEditorData, stateTracker));
			}

			if (textEditorData.Options.IndentStyle == IndentStyle.Auto || textEditorData.Options.IndentStyle == IndentStyle.None) {
				((IInternalEditorExtensions)textEditorData).SetTextPasteHandler (null);
			} else {
				((IInternalEditorExtensions)textEditorData).SetTextPasteHandler (new TextPasteHandler (this, stateTracker, options, policy));
			}
		}

		public override void Dispose ()
		{
			if (textEditorData != null) {
				((IInternalEditorExtensions)textEditorData).SetTextPasteHandler (null);
				textEditorData.Options.Changed -= HandleTextOptionsChanged;
				((IInternalEditorExtensions)textEditorData).SetIndentationTracker (null);
				textEditorData.TextChanging -= HandleTextReplacing;
				textEditorData.TextChanged -= HandleTextReplaced;
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
			SafeUpdateIndentEngine (Math.Min (textEditorData.Length, e.Offset + e.InsertionLength + 1));
			if (wasInVerbatimString == true && !stateTracker.IsInsideVerbatimString) {
				textEditorData.TextChanging -= HandleTextReplacing;
				textEditorData.TextChanged -= HandleTextReplaced;
				ConvertVerbatimStringToNormal (textEditorData, e.Offset + e.InsertionLength + 1);
				textEditorData.TextChanging += HandleTextReplacing;
				textEditorData.TextChanged += HandleTextReplaced;
			}
		}

		void HandleTextReplacing (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			wasInVerbatimString = null;
			var o = e.Offset + e.RemovalLength;
			if (o < 0 || o + 1 > textEditorData.Length || e.RemovalLength != 1/* || textEditorData.Document.IsInUndo*/) {
				return;
			}
			if (textEditorData[o] != '"')
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
				char ch = textEditorData[endOffset];
				if (ch == '\\') {
					if (endOffset + 1 < textEditorData.Length && NewLine.IsNewLine (textEditorData[endOffset + 1]))
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
				char ch = textEditorData[endOffset];
				if (ch == '"' && (endOffset + 1 < textEditorData.Length && textEditorData[endOffset + 1] == '"')) {
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
			int cursor = textEditorData.CaretOffset;
			if (stateTracker.IsInsideVerbatimString && cursor > 0 && cursor < textEditorData.Length && textEditorData[cursor - 1] == '"')
				SafeUpdateIndentEngine (cursor + 1);
			if (stateTracker.IsInsideVerbatimString) {
				// insert normal tab inside @" ... "
				if (textEditorData.IsSomethingSelected) {
					textEditorData.SelectedText = "\t";
				}
				else {
					textEditorData.Insert (cursor, "\t");
				}
				// textEditorData.Document.CommitLineUpdate (textEditorData.CaretLine);
			}
			else if (cursor >= 1) {
				if (textEditorData.CaretColumn > 1) {
					int delta = cursor - cursorPositionBeforeKeyPress;
					if (delta < 2 && delta > 0) {
						textEditorData.Remove (cursor - delta, delta);
						textEditorData.CaretOffset = cursor - delta;
						// textEditorData.Document.CommitLineUpdate (textEditorData.CaretLine);
					}
				}
				SafeUpdateIndentEngine (textEditorData.CaretOffset);
				DoReSmartIndent ();
			}
		}

		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool skipFormatting = StateTracker.IsInsideOrdinaryCommentOrString ||
			                      StateTracker.IsInsidePreprocessorDirective;

			cursorPositionBeforeKeyPress = textEditorData.CaretOffset;
			bool isSomethingSelected = textEditorData.IsSomethingSelected;
			if (key == Gdk.Key.BackSpace && textEditorData.CaretOffset == lastInsertedSemicolon) {
				textEditorData.Undo ();
				lastInsertedSemicolon = -1;
				return false;
			}
			lastInsertedSemicolon = -1;
			if (keyChar == ';' && textEditorData.EditMode == EditMode.Edit && !DoInsertTemplate () && !isSomethingSelected && PropertyService.Get (
				    "SmartSemicolonPlacement",
				    false
			    )) {
				bool retval = base.KeyPress (key, keyChar, modifier);
				var curLine = textEditorData.GetLine (textEditorData.CaretLine);
				string text = textEditorData.GetTextAt (curLine);
				if (!(text.EndsWith (";", StringComparison.Ordinal) || text.Trim ().StartsWith ("for", StringComparison.Ordinal))) {
					int guessedOffset;

					if (GuessSemicolonInsertionOffset (textEditorData, curLine, textEditorData.CaretOffset, out guessedOffset)) {
						using (var undo = textEditorData.OpenUndoGroup ()) {
							textEditorData.Remove (textEditorData.CaretOffset - 1, 1);
							textEditorData.CaretOffset = guessedOffset;
							lastInsertedSemicolon = textEditorData.CaretOffset + 1;
							retval = base.KeyPress (key, keyChar, modifier);
						}
					}
				}
				using (var undo = textEditorData.OpenUndoGroup ()) {
					if (OnTheFlyFormatting && textEditorData != null && textEditorData.EditMode == EditMode.Edit) {
						OnTheFlyFormatter.FormatStatmentAt (Document, textEditorData.CaretLocation);
					}
				}
				return retval;
			}
			
			if (key == Gdk.Key.Tab) {
				SafeUpdateIndentEngine (textEditorData.CaretOffset);
				if (stateTracker.IsInsideStringLiteral && !textEditorData.IsSomethingSelected) {
					var lexer = new CSharpCompletionEngineBase.MiniLexer (textEditorData.GetTextAt (0, textEditorData.CaretOffset));
					lexer.Parse ();
					if (lexer.IsInString) {
						textEditorData.InsertAtCaret ("\\t");
						return false;
					}
				}
			}


			if (key == Gdk.Key.Tab && DefaultSourceEditorOptions.Instance.TabIsReindent && !CompletionWindowManager.IsVisible && textEditorData.EditMode == EditMode.Edit && !DoInsertTemplate () && !isSomethingSelected) {
				ReindentOnTab ();

				return false;
			}

			SafeUpdateIndentEngine (textEditorData.CaretOffset);
			if (!stateTracker.IsInsideOrdinaryCommentOrString) {
				if (keyChar == '@') {
					var retval = base.KeyPress (key, keyChar, modifier);
					int cursor = textEditorData.CaretOffset;
					if (cursor < textEditorData.Length && textEditorData[cursor] == '"')
						ConvertNormalToVerbatimString (textEditorData, cursor + 1);
					return retval;
				}
			}


			//do the smart indent
			if (textEditorData.Options.IndentStyle == IndentStyle.Smart || textEditorData.Options.IndentStyle == IndentStyle.Virtual) {
				bool retval;
				//capture some of the current state
				int oldBufLen = textEditorData.Length;
				int oldLine = textEditorData.CaretLine + 1;
				bool reIndent = false;

				//pass through to the base class, which actually inserts the character
				//and calls HandleCodeCompletion etc to handles completion
				using (var undo = textEditorData.OpenUndoGroup ()) {
					DoPreInsertionSmartIndent (key);
				}
				wasInStringLiteral = stateTracker.IsInsideStringLiteral;
				bool automaticReindent;
				// need to be outside of an undo group - otherwise it interferes with other text editor extension
				// esp. the documentation insertion undo steps.
				retval = base.KeyPress (key, keyChar, modifier);
				//handle inserted characters
				if (textEditorData.CaretOffset <= 0 || textEditorData.IsSomethingSelected)
					return retval;
				
				lastCharInserted = TranslateKeyCharForIndenter (key, keyChar, textEditorData.GetCharAt (textEditorData.CaretOffset - 1));
				if (lastCharInserted == '\0')
					return retval;
				using (var undo = textEditorData.OpenUndoGroup ()) {
					SafeUpdateIndentEngine (textEditorData.CaretOffset);

					if (key == Gdk.Key.Return && modifier == Gdk.ModifierType.ControlMask) {
						FixLineStart (textEditorData, stateTracker, textEditorData.CaretLine + 1);
					} else {
						if (!(oldLine == textEditorData.CaretLine + 1 && lastCharInserted == '\n') && (oldBufLen != textEditorData.Length || lastCharInserted != '\0')) {
							DoPostInsertionSmartIndent (lastCharInserted, out reIndent);
						} else {
							reIndent = lastCharInserted == '\n';
						}
					}
					//reindent the line after the insertion, if needed
					//N.B. if the engine says we need to reindent, make sure that it's because a char was 
					//inserted rather than just updating the stack due to moving around

					SafeUpdateIndentEngine (textEditorData.CaretOffset);
					automaticReindent = (stateTracker.NeedsReindent && lastCharInserted != '\0');
					if (key == Gdk.Key.Return && (reIndent || automaticReindent)) {
						if (textEditorData.Options.IndentStyle == IndentStyle.Virtual) {
							if (textEditorData.GetLine (textEditorData.CaretLine).Length == 0)
								textEditorData.CaretColumn = textEditorData.GetVirtualIndentationColumn (textEditorData.CaretLine);
						} else {
							DoReSmartIndent ();
						}
					}
				}

				if (reIndent || key != Gdk.Key.Return && key != Gdk.Key.Tab && automaticReindent) {
					using (var undo = textEditorData.OpenUndoGroup ()) {
						DoReSmartIndent ();
					}
				}
				if (!skipFormatting) {
					if (keyChar == ';' || keyChar == '}') {
						using (var undo = textEditorData.OpenUndoGroup ()) {
							if (OnTheFlyFormatting && textEditorData != null && textEditorData.EditMode == EditMode.Edit) {
								OnTheFlyFormatter.FormatStatmentAt (Document, textEditorData.CaretLocation);
							}
						}
					}
				}

				SafeUpdateIndentEngine (textEditorData.CaretOffset);
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

			if (key == Gdk.Key.Return || key == Gdk.Key.KP_Enter) {
				DoReSmartIndent ();
			}

			CheckXmlCommentCloseTag (keyChar);

			if (!skipFormatting && keyChar == '}')
				RunFormatter (new MonoDevelop.Core.Text.TextLocation (textEditorData.CaretLine, textEditorData.CaretColumn));
			return result;
		}

		static bool IsSemicolonalreadyPlaced (IReadonlyTextDocument data, int caretOffset)
		{
			for (int pos2 = caretOffset - 1; pos2-- > 0;) {
				var ch2 = data[pos2];
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
			while (end > 1 && char.IsWhiteSpace (data[end]))
				end--;
			int end2 = end;
			while (end2 > 1 && char.IsLetter (data[end2 - 1]))
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
				char ch = data[pos];
				switch (ch) {
				case '}':
					if (firstChar && !IsSemicolonalreadyPlaced (data, caretOffset))
						return false;
					break;
				case '/':
					if (isInBlockComment) {
						isInBlockComment &= pos <= 0 || data[pos - 1] != '*';
					} else if (!isInString && !isInChar && pos + 1 < max) {
						char nextChar = data[pos + 1];
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
					if (!(isInString || isInChar || isInLineComment || isInBlockComment) && pos + 1 < max && data[pos + 1] == '"') {
						isInString = true;
						isVerbatimString = true;
						pos++;
					}
					break;
				case '"':
					if (!(isInChar || isInLineComment || isInBlockComment)) {
						if (isInString && isVerbatimString && pos + 1 < max && data[pos + 1] == '"') {
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
							textEditorData.CaretOffset = max + 1;
						} else {
							textEditorData.Remove (start + sgn, max - start);
							textEditorData.CaretOffset = start;
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
				SafeUpdateIndentEngine (textEditorData.CaretOffset);
				HandleStringConcatinationDeletion (textEditorData.CaretOffset - 1, 0);
				break;
			case Gdk.Key.Delete:
				SafeUpdateIndentEngine (textEditorData.CaretOffset);
				HandleStringConcatinationDeletion (textEditorData.CaretOffset, textEditorData.Length);
				break;
			}
		}
		//special handling for certain characters just inserted , for comments etc
		void DoPostInsertionSmartIndent (char charInserted, out bool reIndent)
		{
			SafeUpdateIndentEngine (textEditorData.CaretOffset);
			reIndent = false;
			switch (charInserted) {
			case '}':
			case ';':
				reIndent = true;
				break;
			case '\n':
				if (FixLineStart (textEditorData, stateTracker, stateTracker.Location.Line))
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
			DoReSmartIndent (textEditorData.CaretOffset);
		}

		void DoReSmartIndent (int cursor)
		{
			SafeUpdateIndentEngine (cursor);
			if (stateTracker.LineBeganInsideVerbatimString || stateTracker.LineBeganInsideMultiLineComment)
				return;
			if (DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.Auto) {
				textEditorData.FixVirtualIndentation ();
				return;
			}
			var line = textEditorData.GetLineByOffset (cursor);

			// Get context to the end of the line w/o changing the main engine's state
			var curTracker = stateTracker.Clone ();
			try {
				for (int max = cursor; max < line.EndOffset; max++) {
					curTracker.Push (textEditorData[max]);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Exception during indentation", e);
			}
			
			int pos = line.Offset;
			string curIndent = line.GetIndentation (textEditorData);
			int nlwsp = curIndent.Length;
			int offset = cursor > pos + nlwsp ? cursor - (pos + nlwsp) : 0;
			if (!stateTracker.LineBeganInsideMultiLineComment || (nlwsp < line.LengthIncludingDelimiter && textEditorData[line.Offset + nlwsp] == '*')) {
				// Possibly replace the indent
				string newIndent = curTracker.ThisLineIndent;
				int newIndentLength = newIndent.Length;
				if (newIndent != curIndent) {
					if (CompletionWindowManager.IsVisible) {
						if (pos < CompletionWindowManager.CodeCompletionContext.TriggerOffset)
							CompletionWindowManager.CodeCompletionContext.TriggerOffset -= nlwsp;
					}
					newIndentLength = newIndent.Length;
					textEditorData.Replace (pos, nlwsp, newIndent);
					//textEditorData.CommitLineUpdate (textEditorData.CaretLine);
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
