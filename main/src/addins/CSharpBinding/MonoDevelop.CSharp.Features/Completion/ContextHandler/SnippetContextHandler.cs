//
// SnippetContextHandler.cs
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


using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class SnippetContextHandler : CompletionContextHandler
	{
		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var document = completionContext.Document;
			var position = completionContext.Position;

			var syntaxTree = await document.GetCSharpSyntaxTreeAsync (cancellationToken).ConfigureAwait (false);

			if (syntaxTree.IsInNonUserCode (position, cancellationToken) ||
				syntaxTree.IsRightOfDotOrArrowOrColonColon (position, cancellationToken) ||
				syntaxTree.GetContainingTypeOrEnumDeclaration (position, cancellationToken) is EnumDeclarationSyntax) {
				return Enumerable.Empty<CompletionData> ();
			}

			// var span = new TextSpan(position, 0);
			//			var semanticModel = await document.GetCSharpSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
			if (syntaxTree.IsPreProcessorDirectiveContext (position, cancellationToken)) {
				var directive = syntaxTree.GetRoot (cancellationToken).FindTokenOnLeftOfPosition (position, includeDirectives: true).GetAncestor<DirectiveTriviaSyntax> ();
				if (directive.DirectiveNameToken.IsKind (
					SyntaxKind.IfKeyword,
					SyntaxKind.RegionKeyword,
					SyntaxKind.ElseKeyword,
					SyntaxKind.ElifKeyword,
					SyntaxKind.ErrorKeyword,
					SyntaxKind.LineKeyword,
					SyntaxKind.PragmaKeyword,
					SyntaxKind.EndIfKeyword,
					SyntaxKind.UndefKeyword,
					SyntaxKind.EndRegionKeyword,
					SyntaxKind.WarningKeyword)) {
					return Enumerable.Empty<CompletionData> ();
				}

				return await GetSnippetCompletionItemsAsync (cancellationToken).ConfigureAwait (false);
			}
			var tokenLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition (position, cancellationToken);

			if (syntaxTree.IsGlobalStatementContext (position, cancellationToken) ||
				syntaxTree.IsExpressionContext (position, tokenLeftOfPosition, true, cancellationToken) ||
				syntaxTree.IsStatementContext (position, tokenLeftOfPosition, cancellationToken) ||
				syntaxTree.IsTypeContext (position, cancellationToken) ||
				syntaxTree.IsTypeDeclarationContext (position, tokenLeftOfPosition, cancellationToken) ||
				syntaxTree.IsNamespaceContext (position, cancellationToken) ||
				syntaxTree.IsMemberDeclarationContext (position, tokenLeftOfPosition, cancellationToken) ||
				syntaxTree.IsLabelContext (position, cancellationToken)) {
				return await GetSnippetCompletionItemsAsync (cancellationToken).ConfigureAwait (false);
			}

			return Enumerable.Empty<CompletionData> ();
		}

		Task<IEnumerable<CompletionData>> GetSnippetCompletionItemsAsync (CancellationToken cancellationToken)
		{
			if (CompletionEngine.SnippetCallback == null)
				return Task.FromResult (Enumerable.Empty<CompletionData> ());
			return CompletionEngine.SnippetCallback (cancellationToken);
		}

	}
}

