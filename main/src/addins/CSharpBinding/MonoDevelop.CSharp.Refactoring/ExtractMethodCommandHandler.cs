//
// ExtractMethodCommandHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Refactoring
{
	class ExtractMethodCommandHandler : CommandHandler
	{
		public static async Task<bool> IsValid (MonoDevelop.Ide.Gui.Document doc, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (doc == null)
				return false;
			if (doc.Editor == null || !doc.Editor.IsSomethingSelected)
				return false;
			var ad = doc.AnalysisDocument;
			if (ad == null)
				return false;
			var selectionRange = doc.Editor.SelectionRange;
			try {
				var selection = new CSharpSelectionValidator (await SemanticDocument.CreateAsync (ad, cancellationToken).ConfigureAwait (false), new TextSpan (selectionRange.Offset, selectionRange.Length), doc.GetOptionSet ());
				var result = await selection.GetValidSelectionAsync (cancellationToken).ConfigureAwait (false);
				return result.ContainsValidContext;
			} catch (Exception) {
				return false;
			}
		}

		protected override void Update (CommandInfo info)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			info.Enabled = doc != null && doc.ParsedDocument != null && doc.ParsedDocument.GetAst<SemanticModel> () != null;
		}

		public async static Task Run (MonoDevelop.Ide.Gui.Document doc)
		{
			if (!doc.Editor.IsSomethingSelected)
				return;
			var ad = doc.AnalysisDocument;
			if (ad == null || !await IsValid (doc))
				return;
			try {
				var selectionRange = doc.Editor.SelectionRange;
				var token = default (CancellationToken);
				var selection = new CSharpSelectionValidator (await SemanticDocument.CreateAsync (ad, token).ConfigureAwait (false), new TextSpan (selectionRange.Offset, selectionRange.Length), doc.GetOptionSet ());
				var result = await selection.GetValidSelectionAsync (token).ConfigureAwait (false);
				if (!result.ContainsValidContext)
					return;
				var extractor = new CSharpMethodExtractor ((CSharpSelectionResult)result);
				var extractionResult = await extractor.ExtractMethodAsync (token).ConfigureAwait (false);
				var changes = await extractionResult.Document.GetTextChangesAsync (ad, token);
				using (var undo = doc.Editor.OpenUndoGroup ()) {
					foreach (var change in changes.OrderByDescending (ts => ts.Span.Start)) {
						doc.Editor.ReplaceText (change.Span.Start, change.Span.Length, change.NewText);
					}
					// hack to remove the redundant private modifier.
					if (doc.Editor.GetTextAt (extractionResult.MethodDeclarationNode.SpanStart, "private ".Length) == "private ") {
						doc.Editor.RemoveText (extractionResult.MethodDeclarationNode.SpanStart, "private ".Length);
					}
				}
				await doc.UpdateParseDocument ();
				var info = RefactoringSymbolInfo.GetSymbolInfoAsync (doc, extractionResult.InvocationNameToken.Span.Start).Result;
				var sym = info.DeclaredSymbol ?? info.Symbol;
				if (sym != null)
					await new MonoDevelop.Refactoring.Rename.RenameRefactoring ().Rename (sym);
			} catch (Exception e) {
				LoggingService.LogError ("Error while extracting method", e);
			}
		}

		protected async override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			await Run (doc);
		}
	}
}

