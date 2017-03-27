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

		public override string FormatPlainText (int offset, string text, byte[] copyData)
		{
			return engine.FormatPlainText (indent.Editor, offset, text, copyData);
		}

		public override byte[] GetCopyData (int offset, int length)
		{
			return engine.GetCopyData (indent.Editor, new TextSpan (offset, length));
		}

		public override async Task PostFomatPastedText (int insertionOffset, int insertedChars)
		{
			if (indent.Editor.Options.IndentStyle == IndentStyle.None ||
				indent.Editor.Options.IndentStyle == IndentStyle.Auto)
				return;
			if (DefaultSourceEditorOptions.Instance.OnTheFlyFormatting) {
				var tree = await indent.DocumentContext.AnalysisDocument.GetSyntaxTreeAsync ();
				var startLine = indent.Editor.GetLineByOffset (insertionOffset);
				var endLine = indent.Editor.GetLineByOffset (insertionOffset + insertedChars);
				int lineStartOffset = startLine.Offset != endLine.Offset ? startLine.Offset : insertionOffset;
				int formatCharsCount = insertedChars + (insertionOffset - lineStartOffset);
				var policy = indent.DocumentContext.GetFormattingPolicy ();
				var textPolicy = indent.DocumentContext.Project.Policies.Get<Ide.Gui.Content.TextStylePolicy> (indent.Editor.MimeType);
				var optionSet = policy.CreateOptions (textPolicy);
				var span = new TextSpan (lineStartOffset, formatCharsCount);
				var doc = await Formatter.FormatAsync (indent.DocumentContext.AnalysisDocument, span, optionSet);

				OnTheFlyFormatter.ApplyNewTree (indent.Editor, lineStartOffset, true, span, tree, await doc.GetSyntaxTreeAsync ());
				return;
			}
			// Just correct the start line of the paste operation - the text is already indented.
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

	}
}

