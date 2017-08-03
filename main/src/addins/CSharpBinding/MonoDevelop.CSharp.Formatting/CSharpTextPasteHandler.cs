//
// TextPasteIndentEngine.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.CodeCompletion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Roslyn.Utilities;
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace MonoDevelop.CSharp.Formatting
{
	class CSharpTextPasteHandler : TextPasteHandler
	{
		readonly ICSharpCode.NRefactory6.CSharp.ITextPasteHandler engine;
		readonly CSharpTextEditorIndentation indent;

		public CSharpTextPasteHandler (CSharpTextEditorIndentation indent, ICSharpCode.NRefactory6.CSharp.IStateMachineIndentEngine decoratedEngine, OptionSet formattingOptions)
		{
			this.engine = new ICSharpCode.NRefactory6.CSharp.TextPasteIndentEngine (decoratedEngine, formattingOptions);
			this.indent = indent;
		}

		public override string FormatPlainText (int insertionOffset, string text, byte [] copyData)
		{
			var result = engine.FormatPlainText (indent.Editor, insertionOffset, text, copyData);

			if (DefaultSourceEditorOptions.Instance.OnTheFlyFormatting) {
				var tree = indent.DocumentContext.AnalysisDocument.GetSyntaxTreeAsync ().WaitAndGetResult (default (CancellationToken));
				tree = tree.WithChangedText (tree.GetText ().WithChanges (new TextChange (new TextSpan (insertionOffset, 0), text)));

				var insertedChars = text.Length;
				var startLine = indent.Editor.GetLineByOffset (insertionOffset);

				var policy = indent.DocumentContext.GetFormattingPolicy ();
				var textPolicy = indent.DocumentContext.Project.Policies.Get<Ide.Gui.Content.TextStylePolicy> (indent.Editor.MimeType);
				var optionSet = policy.CreateOptions (textPolicy);
				var span = new TextSpan (insertionOffset, insertedChars);

				var rules = new List<IFormattingRule> { new PasteFormattingRule () };
				rules.AddRange (Formatter.GetDefaultFormattingRules (indent.DocumentContext.AnalysisDocument));

				var root = tree.GetRoot ();
				var changes = Formatter.GetFormattedTextChanges (root, SpecializedCollections.SingletonEnumerable (span), indent.DocumentContext.RoslynWorkspace, optionSet, rules, default (CancellationToken));
				var doc = TextEditorFactory.CreateNewDocument ();
				doc.Text = text;
				doc.ApplyTextChanges (changes.Where (c => c.Span.Start - insertionOffset < text.Length && c.Span.Start - insertionOffset >= 0).Select (delegate (TextChange c) { 
					return new TextChange (new TextSpan (c.Span.Start - insertionOffset, c.Span.Length), c.NewText); 
				}));
				return doc.Text;
			}

			return result;
		}

		public override byte [] GetCopyData (int offset, int length)
		{
			return engine.GetCopyData (indent.Editor, new TextSpan (offset, length));
		}

		public override async Task PostFomatPastedText (int insertionOffset, int insertedChars)
		{
			if (indent.Editor.Options.IndentStyle == IndentStyle.None ||
				indent.Editor.Options.IndentStyle == IndentStyle.Auto)
				return;
			// Just correct the start line of the paste operation - the text is already Formatted.
			var curLine = indent.Editor.GetLineByOffset (insertionOffset);
			var curLineOffset = curLine.Offset;
			indent.SafeUpdateIndentEngine (curLineOffset);
			if (!indent.stateTracker.IsInsideOrdinaryCommentOrString) {
				int pos = curLineOffset;
				string curIndent = curLine.GetIndentation (indent.Editor);
				int nlwsp = curIndent.Length;
				if (!indent.stateTracker.LineBeganInsideMultiLineComment || (nlwsp < curLine.LengthIncludingDelimiter && indent.Editor.GetCharAt (curLineOffset + nlwsp) == '*')) {
					// Possibly replace the indent
					indent.SafeUpdateIndentEngine (curLineOffset + curLine.Length);
					string newIndent = indent.stateTracker.ThisLineIndent;
					if (newIndent != curIndent) {
						if (CompletionWindowManager.IsVisible) {
							if (pos < CompletionWindowManager.CodeCompletionContext.TriggerOffset)
								CompletionWindowManager.CodeCompletionContext.TriggerOffset -= nlwsp;
						}
						indent.Editor.ReplaceText (pos, nlwsp, newIndent);
						//						textEditorData.Document.CommitLineUpdate (textEditorData.CaretLine);
					}
				}
			}
			indent.Editor.FixVirtualIndentation ();

		}

		class PasteFormattingRule : AbstractFormattingRule
		{
			public override AdjustNewLinesOperation GetAdjustNewLinesOperation (SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
			{
				if (currentToken.Parent != null) {
					var currentTokenParentParent = currentToken.Parent.Parent;
					if (currentToken.Kind () == SyntaxKind.OpenBraceToken && currentTokenParentParent != null &&
						(currentTokenParentParent.Kind () == SyntaxKind.SimpleLambdaExpression ||
						 currentTokenParentParent.Kind () == SyntaxKind.ParenthesizedLambdaExpression ||
						 currentTokenParentParent.Kind () == SyntaxKind.AnonymousMethodExpression)) {
						return FormattingOperations.CreateAdjustNewLinesOperation (0, AdjustNewLinesOption.PreserveLines);
					}
				}

				return nextOperation.Invoke ();
			}
		}
	}
}

