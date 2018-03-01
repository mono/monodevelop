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
using MonoDevelop.Ide.CodeTemplates;
using System.Linq;
using System.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Projects;
using MonoDevelop.Core.Text;
using ICSharpCode.NRefactory6.CSharp;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Formatting
{
	class CSharpTextEditorIndentation : TextEditorExtension
	{
		internal ICSharpCode.NRefactory6.CSharp.CacheIndentEngine stateTracker;
		int cursorPositionBeforeKeyPress;

		readonly static IEnumerable<string> types = DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);

		CSharpFormattingPolicy Policy {
			get {
				return DocumentContext.GetPolicy<CSharpFormattingPolicy> (types);
			}
		}

		TextStylePolicy TextStylePolicy {
			get {
				return DocumentContext.GetPolicy<TextStylePolicy> (types);
			}
		}

		char lastCharInserted;

		static CSharpTextEditorIndentation ()
		{
			CompletionWindowManager.WordCompleted += delegate (object sender, CodeCompletionContextEventArgs e) {
				var editor = e.Widget as IServiceProvider;
				if (editor == null)
					return;
				var extension = editor.GetService (typeof (CSharpTextEditorIndentation)) as CSharpTextEditorIndentation;
				if (extension == null)
					return;
				extension.SafeUpdateIndentEngine (extension.Editor.CaretOffset);
				if (extension.stateTracker.NeedsReindent)
					extension.DoReSmartIndent ();
			};

			RefactoringService.OptionSetCreation = (editor, ctx) => {
				var policy = ctx.Project.Policies.Get<CSharpFormattingPolicy> (types);
				return policy.CreateOptions (editor.Options);
			};
		}

		internal void SafeUpdateIndentEngine (int offset)
		{
			try {
				stateTracker.Update (Editor, offset);
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
			if (!OnTheFlyFormatting || Editor == null || Editor.EditMode != EditMode.Edit)
				return;
			var offset = Editor.LocationToOffset (location);
			OnTheFlyFormatter.Format (Editor, DocumentContext, offset, offset, optionSet: optionSet);
		}

		protected override void Initialize ()
		{
			base.Initialize ();

			if (Editor != null) {
				Editor.OptionsChanged += HandleTextOptionsChanged;
				HandleTextOptionsChanged (this, EventArgs.Empty);
				Editor.TextChanging += HandleTextReplacing;
				Editor.TextChanged += HandleTextReplaced;
			}
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ActiveConfigurationChanged += HandleTextOptionsChanged;
			CompletionWindowManager.WindowClosed += CompletionWindowManager_WindowClosed;
		}

		bool indentationDisabled;

		public static IEnumerable<string> GetDefinedSymbols (MonoDevelop.Projects.Project project)
		{
			var workspace = IdeApp.Workspace;
			if (workspace == null || project == null)
				yield break;
			var configuration = project.GetConfiguration (workspace.ActiveConfiguration) as DotNetProjectConfiguration;
			if (configuration != null) {
				foreach (string s in configuration.GetDefineSymbols ())
					yield return s;
				// Workaround for mcs defined symbol
				if (configuration.TargetRuntime.RuntimeId == "Mono")
					yield return "__MonoCS__";
			}
		}

		void HandleTextOptionsChanged (object sender, EventArgs e)
		{
			//var options = Editor.CreateNRefactoryTextEditorOptions ();
			optionSet = Policy.CreateOptions (Editor.Options);
			//options.IndentBlankLines = true;
			ICSharpCode.NRefactory6.CSharp.IStateMachineIndentEngine indentEngine;
			try {
				var csharpIndentEngine = new ICSharpCode.NRefactory6.CSharp.CSharpIndentEngine (optionSet);
				//csharpIndentEngine.EnableCustomIndentLevels = true;
				foreach (var symbol in GetDefinedSymbols (DocumentContext.Project)) {
					csharpIndentEngine.DefineSymbol (symbol);
				}
				indentEngine = csharpIndentEngine;
			} catch (Exception ex) {
				LoggingService.LogError ("Error while creating the c# indentation engine", ex);
				indentEngine = new ICSharpCode.NRefactory6.CSharp.NullIStateMachineIndentEngine ();
			}
			stateTracker = new ICSharpCode.NRefactory6.CSharp.CacheIndentEngine (indentEngine);
			if (DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.Auto) {
				Editor.IndentationTracker = null;
			} else {
				Editor.IndentationTracker = new IndentVirtualSpaceManager (Editor, stateTracker);
			}

			indentationDisabled = DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.Auto || DefaultSourceEditorOptions.Instance.IndentStyle == IndentStyle.None;
			if (indentationDisabled) {
				Editor.SetTextPasteHandler (null);
			} else {
				Editor.SetTextPasteHandler (new CSharpTextPasteHandler (this, stateTracker, optionSet));
			}
		}

		public override void Dispose ()
		{
			if (Editor != null) {
				Editor.SetTextPasteHandler (null);
				Editor.OptionsChanged -= HandleTextOptionsChanged;
				Editor.IndentationTracker  = null;
				Editor.TextChanging -= HandleTextReplacing;
				Editor.TextChanged -= HandleTextReplaced;
			}
			IdeApp.Workspace.ActiveConfigurationChanged -= HandleTextOptionsChanged;
			CompletionWindowManager.WindowClosed -= CompletionWindowManager_WindowClosed;

			stateTracker = null;
			base.Dispose ();
		}

		bool? wasInVerbatimString;

		void HandleTextReplaced (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			for (int i = 0; i < e.TextChanges.Count; ++i) {
				var change = e.TextChanges[i];
				stateTracker.ResetEngineToPosition (Editor, change.NewOffset);
				if (wasInVerbatimString == null)
					return;
				if (change.RemovalLength != 1 /*|| textEditorData.Document.CurrentAtomicUndoOperationType == OperationType.Format*/)
					return;
				SafeUpdateIndentEngine (Math.Min (Editor.Length, change.NewOffset + change.InsertionLength + 1));
				if (wasInVerbatimString == true && !stateTracker.IsInsideVerbatimString) {
					Editor.TextChanging -= HandleTextReplacing;
					Editor.TextChanged -= HandleTextReplaced;
					ConvertVerbatimStringToNormal (Editor, change.NewOffset + change.InsertionLength + 1);
					Editor.TextChanging += HandleTextReplacing;
					Editor.TextChanged += HandleTextReplaced;
				}
			}
		}

		void HandleTextReplacing (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			wasInVerbatimString = null;
			for (int i = 0; i < e.TextChanges.Count; ++i) {
				var change = e.TextChanges[i];
				var o = change.Offset + change.RemovalLength;
				if (o < 0 || o + 1 > Editor.Length || change.RemovalLength != 1/* || textEditorData.Document.IsInUndo*/) {
					continue;
				}
				if (Editor.GetCharAt (o) != '"')
					continue;
				SafeUpdateIndentEngine (o + 1);
				wasInVerbatimString = stateTracker.IsInsideVerbatimString;
			}
		}

		internal static string ConvertToStringLiteral (string text)
		{
			var result = StringBuilderCache.Allocate ();
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
			return StringBuilderCache.ReturnAndFree (result);
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
			textEditorData.ReplaceText (offset, endOffset - offset, newText);
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
			textEditorData.ReplaceText (offset, endOffset - offset, newText);
		}

		public bool DoInsertTemplate ()
		{
			string word = CodeTemplate.GetTemplateShortcutBeforeCaret (Editor);
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplatesAsync (Editor).WaitAndGetResult (CancellationToken.None)) {
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
					if (!string.IsNullOrEmpty (tag) && ICSharpCode.NRefactory.CSharp.Completion.CSharpCompletionEngine.CommentTags.Any (t => t == tag)) {
						var caretOffset = Editor.CaretOffset;
						Editor.InsertText (caretOffset, "</" + tag + ">");
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
					Editor.InsertText (cursor, "\t");
				}
				// textEditorData.Document.CommitLineUpdate (textEditorData.CaretLine);
			}
			else if (cursor >= 1) {
				if (Editor.CaretColumn > 1) {
					int delta = cursor - cursorPositionBeforeKeyPress;
					if (delta < 2 && delta > 0) {
						Editor.RemoveText (cursor - delta, delta);
						Editor.CaretOffset = cursor - delta;
						// textEditorData.Document.CommitLineUpdate (textEditorData.CaretLine);
					}
				}
				SafeUpdateIndentEngine (Editor.CaretOffset);
				DoReSmartIndent ();
			}
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			completionWindowWasVisible = CompletionWindowManager.IsVisible;
			cursorPositionBeforeKeyPress = Editor.CaretOffset;
			bool isSomethingSelected = Editor.IsSomethingSelected;
			if (descriptor.SpecialKey == SpecialKey.BackSpace && Editor.CaretOffset == lastInsertedSemicolon) {
				EditActions.Undo (Editor);
				lastInsertedSemicolon = -1;
				return false;
			}

			lastInsertedSemicolon = -1;
			if (descriptor.KeyChar == ';' && Editor.EditMode == EditMode.Edit && !DoInsertTemplate () && !isSomethingSelected && PropertyService.Get (
				    "SmartSemicolonPlacement",
				    false
			    ) && !(stateTracker.IsInsideComment || stateTracker.IsInsideString)) {
				bool retval = base.KeyPress (descriptor);
				var curLine = Editor.GetLine (Editor.CaretLine);
				string text = Editor.GetTextAt (curLine);
				if (!(text.EndsWith (";", StringComparison.Ordinal) || text.Trim ().StartsWith ("for", StringComparison.Ordinal))) {
					int guessedOffset;

					if (GuessSemicolonInsertionOffset (Editor, curLine, Editor.CaretOffset, out guessedOffset)) {
						using (var undo = Editor.OpenUndoGroup ()) {
							Editor.RemoveText (Editor.CaretOffset - 1, 1);
							Editor.CaretOffset = guessedOffset;
							lastInsertedSemicolon = Editor.CaretOffset + 1;
							retval = base.KeyPress (descriptor);
						}
					}
				}
				using (var undo = Editor.OpenUndoGroup ()) {
					if (OnTheFlyFormatting && Editor != null && Editor.EditMode == EditMode.Edit) {
						OnTheFlyFormatter.FormatStatmentAt (Editor, DocumentContext, Editor.CaretLocation, optionSet: optionSet);
					}
				}
				return retval;
			}

			if (descriptor.SpecialKey == SpecialKey.Tab && descriptor.ModifierKeys == ModifierKeys.None && !CompletionWindowManager.IsVisible) {
				SafeUpdateIndentEngine (Editor.CaretOffset);
				if (stateTracker.IsInsideStringLiteral && !Editor.IsSomethingSelected) {
					var lexer = new ICSharpCode.NRefactory.CSharp.Completion.CSharpCompletionEngineBase.MiniLexer (Editor.GetTextAt (0, Editor.CaretOffset));
					lexer.Parse ();
					if (lexer.IsInString) {
						Editor.InsertAtCaret ("\\t");
						return false;
					}
				}
			}


			if (descriptor.SpecialKey == SpecialKey.Tab && DefaultSourceEditorOptions.Instance.TabIsReindent && !CompletionWindowManager.IsVisible && Editor.EditMode == EditMode.Edit && !DoInsertTemplate () && !isSomethingSelected) {
				ReindentOnTab ();

				return false;
			}

			SafeUpdateIndentEngine (Editor.CaretOffset);
			if (!stateTracker.IsInsideOrdinaryCommentOrString) {
				if (descriptor.KeyChar == '@') {
					var retval = base.KeyPress (descriptor);
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
					DoPreInsertionSmartIndent (descriptor.SpecialKey);
				}
				wasInStringLiteral = stateTracker.IsInsideStringLiteral;


				bool returnBetweenBraces =
					descriptor.SpecialKey == SpecialKey.Return &&
					descriptor.ModifierKeys == ModifierKeys.None &&
					          Editor.CaretOffset > 0 && Editor.CaretOffset < Editor.Length &&
					          Editor.GetCharAt (Editor.CaretOffset - 1) == '{' && Editor.GetCharAt (Editor.CaretOffset) == '}' && !stateTracker.IsInsideOrdinaryCommentOrString;

				bool automaticReindent;
				// need to be outside of an undo group - otherwise it interferes with other text editor extension
				// esp. the documentation insertion undo steps.
				retval = base.KeyPress (descriptor);


				if (descriptor.KeyChar == '/' && stateTracker.IsInsideMultiLineComment) {
					if (Editor.CaretOffset - 3 >= 0 && Editor.GetCharAt (Editor.CaretOffset - 3) == '*' && Editor.GetCharAt (Editor.CaretOffset - 2) == ' ') {
						using (var undo = Editor.OpenUndoGroup ()) {
							Editor.RemoveText (Editor.CaretOffset - 2, 1);
						}
					}
				}

				//handle inserted characters
				if (Editor.CaretOffset <= 0 || Editor.IsSomethingSelected)
					return retval;

				lastCharInserted = TranslateKeyCharForIndenter (descriptor.SpecialKey, descriptor.KeyChar, Editor.GetCharAt (Editor.CaretOffset - 1));
				if (lastCharInserted == '\0')
					return retval;
				using (var undo = Editor.OpenUndoGroup ()) {

					if (returnBetweenBraces) {
						var oldOffset = Editor.CaretOffset;
						Editor.InsertAtCaret (Editor.EolMarker);
						DoReSmartIndent ();
						Editor.CaretOffset = oldOffset;
					}

					SafeUpdateIndentEngine (Editor.CaretOffset);

					if (descriptor.SpecialKey == SpecialKey.Return && descriptor.ModifierKeys == ModifierKeys.Control) {
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
					if (descriptor.SpecialKey == SpecialKey.Return && (reIndent || automaticReindent)) {
						if (Editor.Options.IndentStyle == IndentStyle.Virtual) {
							if (Editor.GetLine (Editor.CaretLine).Length == 0)
								Editor.CaretColumn = Editor.GetVirtualIndentationColumn (Editor.CaretLine);
						} else {
							DoReSmartIndent ();
						}
					}
				}

				const string reindentChars = ";){}";
				if (reIndent || descriptor.SpecialKey != SpecialKey.Return && descriptor.SpecialKey != SpecialKey.Tab && automaticReindent && reindentChars.Contains (descriptor.KeyChar)) {
					using (var undo = Editor.OpenUndoGroup ()) {
						DoReSmartIndent ();
					}
				}

				HandleOnTheFlyFormatting (descriptor);
				SafeUpdateIndentEngine (Editor.CaretOffset);
				lastCharInserted = '\0';
				CheckXmlCommentCloseTag (descriptor.KeyChar);
				return retval;
			}

			if (Editor.Options.IndentStyle == IndentStyle.Auto && DefaultSourceEditorOptions.Instance.TabIsReindent && descriptor.SpecialKey == SpecialKey.Tab) {
				bool retval = base.KeyPress (descriptor);
				DoReSmartIndent ();
				CheckXmlCommentCloseTag (descriptor.KeyChar);
				return retval;
			}

			//pass through to the base class, which actually inserts the character
			//and calls HandleCodeCompletion etc to handles completion
			var result = base.KeyPress (descriptor);

			if (!indentationDisabled && (descriptor.SpecialKey == SpecialKey.Return)) {
				DoReSmartIndent ();
			}

			CheckXmlCommentCloseTag (descriptor.KeyChar);

			HandleOnTheFlyFormatting (descriptor);

			return result;
		}

		void HandleOnTheFlyFormatting (KeyDescriptor descriptor)
		{
			if (descriptor.KeyChar == '{')
				return;
			SafeUpdateIndentEngine (Editor.CaretOffset);
			bool skipFormatting = stateTracker.IsInsideOrdinaryCommentOrString;
			if (!skipFormatting && !(stateTracker.IsInsideComment || stateTracker.IsInsideString)) {
				if (DocumentContext.ParsedDocument == null || DocumentContext.ParsedDocument.GetAst<SemanticModel> () == null)
					return;
				var document = DocumentContext.AnalysisDocument;
				if (document == null)
					return;
				if (!skipFormatting && service.SupportsFormattingOnTypedCharacter (document, descriptor.KeyChar)) {
					var caretPosition = Editor.CaretOffset;
					var token = CSharpEditorFormattingService.GetTokenBeforeTheCaretAsync (document, caretPosition, default(CancellationToken)).Result;
					if (token.IsMissing || !service.ValidSingleOrMultiCharactersTokenKind (descriptor.KeyChar, token.Kind ()) || token.IsKind (SyntaxKind.EndOfFileToken) || token.IsKind (SyntaxKind.None))
						return;
					if (CSharpEditorFormattingService.TokenShouldNotFormatOnTypeChar (token))
						return;
					using (var undo = Editor.OpenUndoGroup ()) {
						if (OnTheFlyFormatting && Editor != null && Editor.EditMode == EditMode.Edit) {
							var oldVersion = Editor.Version;
							OnTheFlyFormatter.FormatStatmentAt (Editor, DocumentContext, Editor.CaretLocation, optionSet: optionSet);
							if (oldVersion.CompareAge (Editor.Version) != 0)
								CompletionWindowManager.HideWindow ();
						}
					}
				}
			}
			if (OnTheFlyFormatting && descriptor.SpecialKey == SpecialKey.Return) {
				try {
					FormatOnReturn ();
				} catch (Exception e) {
					LoggingService.LogError ("Exception while formatting", e);
				}
			}
		}

		async void CompletionWindowManager_WindowClosed (object sender, EventArgs e)
		{
			var document = DocumentContext.AnalysisDocument;
			if (document == null)
				return;
			var caretPosition = Editor.CaretOffset;
			var token = await CSharpEditorFormattingService.GetTokenBeforeTheCaretAsync (document, caretPosition, default (CancellationToken)).ConfigureAwait (false);
			if (token.IsMissing || !token.Parent.IsKind (SyntaxKind.ElseDirectiveTrivia))
				return;
			var tokenRange = Microsoft.CodeAnalysis.CSharp.Utilities.FormattingRangeHelper.FindAppropriateRange (token);
			if (tokenRange == null || !tokenRange.HasValue || tokenRange.Value.Item1.Equals (tokenRange.Value.Item2))
				return;

			var value = tokenRange.Value;
			using (var undo = Editor.OpenUndoGroup ()) {
				OnTheFlyFormatter.Format (Editor, DocumentContext, value.Item1.SpanStart, value.Item2.Span.End, optionSet: optionSet);
			}
		}

		async void FormatOnReturn (CancellationToken cancellationToken = default (CancellationToken))
		{
			var document = DocumentContext.AnalysisDocument;
			if (document == null)
				return;
			var caretPosition = Editor.CaretOffset;
			var token = await CSharpEditorFormattingService.GetTokenBeforeTheCaretAsync (document, caretPosition, cancellationToken).ConfigureAwait (false);
			if (token.IsMissing)
				return;

			string text = null;
			if (service.IsInvalidToken (token, ref text))
				return;
			// Check to see if the token is ')' and also the parent is a using statement. If not, bail
			if (CSharpEditorFormattingService.TokenShouldNotFormatOnReturn (token))
				return;
			var tokenRange = Microsoft.CodeAnalysis.CSharp.Utilities.FormattingRangeHelper.FindAppropriateRange (token);
			if (tokenRange == null || !tokenRange.HasValue || tokenRange.Value.Item1.Equals (tokenRange.Value.Item2))
				return;
			var value = tokenRange.Value;
			using (var undo = Editor.OpenUndoGroup ()) {
				OnTheFlyFormatter.Format (Editor, DocumentContext, value.Item1.SpanStart, value.Item2.Span.End, optionSet: optionSet);
			}
		}

		CSharpEditorFormattingService service = new CSharpEditorFormattingService ();

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

			var offset = curLine.Offset;
			string lineText = data.GetTextAt (caretOffset, max - caretOffset);
			var lexer = new ICSharpCode.NRefactory.CSharp.Completion.CSharpCompletionEngineBase.MiniLexer (lineText);
			lexer.Parse ((ch, i) => {
				if (lexer.IsInSingleComment || lexer.IsInMultiLineComment)
					return true;
				if (ch == '}' && lexer.IsFistNonWs && !IsSemicolonalreadyPlaced (data, caretOffset)) {
					lastNonWsChar = ';';
					return true;
				}
				if (!char.IsWhiteSpace (ch)) {
					lastNonWsOffset = caretOffset + i;
					lastNonWsChar = ch;
				}
				return false;
			});
			// if the line ends with ';' the line end is not the correct place for a new semicolon.
			if (lastNonWsChar == ';')
				return false;
			outOffset = lastNonWsOffset;
			return true;
		}

		static char TranslateKeyCharForIndenter (SpecialKey key, char keyChar, char docChar)
		{
			switch (key) {
			case SpecialKey.Return:
				return '\n';
			case SpecialKey.Tab:
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
							Editor.RemoveText (max, start - max);
							Editor.CaretOffset = max + 1;
						} else {
							Editor.RemoveText (start + sgn, max - start);
							Editor.CaretOffset = start;
						}
						break;
					} else {
						break;
					}
				}
			}
		}

		void DoPreInsertionSmartIndent (SpecialKey key)
		{
			switch (key) {
			case SpecialKey.BackSpace:
				SafeUpdateIndentEngine (Editor.CaretOffset);
				HandleStringConcatinationDeletion (Editor.CaretOffset - 1, 0);
				break;
			case SpecialKey.Delete:
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
			case ':':
			case ';':
				reIndent = true;
				break;
			case '\n':
				if (completionWindowWasVisible) // \n is handled by an open completion window
					return;
				if (FixLineStart (Editor, stateTracker, Editor.OffsetToLineNumber (stateTracker.Offset)))
					return;
				//newline always reindents unless it's had special handling
				reIndent = true;
				break;
			}
		}

		internal bool wasInStringLiteral;
		OptionSet optionSet;
		bool completionWindowWasVisible;

		public bool FixLineStart (TextEditor textEditorData, ICSharpCode.NRefactory6.CSharp.IStateMachineIndentEngine stateTracker, int lineNumber)
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
						textEditorData.InsertText (insertionPoint, "/// ");
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
					textEditorData.ReplaceText (line.Offset, indentSize, insertedText);
					textEditorData.CaretOffset = line.Offset + insertedText.Length;
					return true;
				} else if (wasInStringLiteral) {
					var lexer = new ICSharpCode.NRefactory.CSharp.Completion.CSharpCompletionEngineBase.MiniLexer (textEditorData.GetTextAt (0, prevLine.EndOffset).TrimEnd ());
					lexer.Parse ();
					if (!lexer.IsInString)
						return false;
					textEditorData.EnsureCaretIsNotVirtual ();
					var insertedText = "\" +";
					textEditorData.InsertText (prevLine.Offset + prevLine.Length, insertedText);
					var lineOffset = line.Offset + insertedText.Length;
					int indentSize = textEditorData.CaretOffset - lineOffset;
					insertedText = prevLine.GetIndentation (textEditorData) + (trimmedPreviousLine.StartsWith ("\"", StringComparison.Ordinal) ? "" : "\t") + "\"";
					textEditorData.ReplaceText (lineOffset, indentSize, insertedText);
					return true;
				}
			}
			return false;
		}
		//does re-indenting and cursor positioning
		internal void DoReSmartIndent ()
		{
			DoReSmartIndent (Editor.CaretOffset);
		}

		internal void DoReSmartIndent (int cursor)
		{
			SafeUpdateIndentEngine (cursor);
			if (stateTracker.LineBeganInsideVerbatimString || stateTracker.LineBeganInsideMultiLineComment || stateTracker.IsInsidePreprocessorDirective)
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
					Editor.ReplaceText (pos, nlwsp, newIndent);
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
