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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;

namespace MonoDevelop.CSharp.Formatting
{
	partial class CSharpTextPasteHandler : TextPasteHandler
	{
		readonly OptionSet optionSet;
		readonly CSharpTextEditorIndentation indent;

		public CSharpTextPasteHandler (CSharpTextEditorIndentation indent, OptionSet optionSet)
		{
			this.indent = indent;
			this.optionSet = optionSet;
		}

		public bool InUnitTestMode { get; internal set; }

		public override string FormatPlainText (int offset, string text, byte [] copyData)
		{
			if (indent.DocumentContext?.AnalysisDocument == null)
				return text;
			var sourceText = indent.Editor;
			var syntaxRoot = indent.DocumentContext.AnalysisDocument.GetSyntaxRootAsync ().WaitAndGetResult ();
			var token = syntaxRoot.FindTokenOnLeftOfPosition (offset);

			if (copyData != null && copyData.Length == 1) {
				var strategy = TextPasteUtils.Strategies [(PasteStrategy)copyData [0]];
				text = strategy.Decode (text);
			}
			if (CSharpSyntaxFactsService.Instance.IsVerbatimStringLiteral (token)) {
				int idx = text.IndexOf ('"');
				if (idx > 0 && !token.Text.EndsWith ("\"", System.StringComparison.Ordinal))
					return TextPasteUtils.VerbatimStringStrategy.Encode (text.Substring (0, idx)) + text.Substring (idx);
				return TextPasteUtils.VerbatimStringStrategy.Encode (text);
			}

			if (CSharpSyntaxFactsService.Instance.IsStringLiteral (token)) {
				int idx = text.IndexOf ('"');
				if (idx > 0 && !token.Text.EndsWith ("\"", System.StringComparison.Ordinal))
					return TextPasteUtils.StringLiteralStrategy.Encode (text.Substring (0, idx)) + text.Substring (idx);
				return TextPasteUtils.StringLiteralStrategy.Encode (text);
			}

			return text;
		}

		public override byte [] GetCopyData (int offset, int length)
		{
			if (indent.DocumentContext?.AnalysisDocument == null)
				return null;
			var syntaxRoot = indent.DocumentContext.AnalysisDocument.GetSyntaxRootAsync ().WaitAndGetResult ();
			var token = syntaxRoot.FindToken (offset);

			if (CSharpSyntaxFactsService.Instance.IsVerbatimStringLiteral (token))
				return new [] { (byte)PasteStrategy.VerbatimString };

			if (CSharpSyntaxFactsService.Instance.IsStringLiteral (token)) 
				return new [] { (byte)PasteStrategy.StringLiteral };

			return null;
		}

		public override async Task PostFomatPastedText (int offset, int length)
		{
			if (indent.Editor.Options.IndentStyle == IndentStyle.None ||
			  indent.Editor.Options.IndentStyle == IndentStyle.Auto)
				return;
			var doc = indent.DocumentContext.AnalysisDocument;
			var options = await doc.GetOptionsAsync ();
			if (!options.GetOption (FeatureOnOffOptions.FormatOnPaste, doc.Project.Language))
				return;

			var formattingService = doc.GetLanguageService<IEditorFormattingService> ();
			if (formattingService == null || !formattingService.SupportsFormatOnPaste)
				return;
			var text = await doc.GetTextAsync ();
			if (offset + length > text.Length) {
				LoggingService.LogError ($"CSharpTextPasteHandler.PostFormatPastedText out of range {offset}/{length} in a document of length {text.Length} (editor length {indent.Editor.Length}).");
				return;
			}
			var changes = await formattingService.GetFormattingChangesOnPasteAsync (doc, new TextSpan (offset, length), default (CancellationToken));
			if (changes == null)
				return;

			indent.Editor.ApplyTextChanges (changes);
			indent.Editor.FixVirtualIndentation ();
		}

	}
}
