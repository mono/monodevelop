//
// SpeculativeTContextHandler.cs
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
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class SpeculativeTContextHandler : CompletionContextHandler
	{
		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var document = completionContext.Document;
			var position = completionContext.Position;

			return await document.GetUnionResultsFromDocumentAndLinks(
				UnionCompletionItemComparer.Instance,
				async (doc, ct) => await GetSpeculativeTCompletions(engine, doc, position, ct).ConfigureAwait(false),
				cancellationToken).ConfigureAwait(false);
		}

		private async Task<IEnumerable<CompletionData>> GetSpeculativeTCompletions(CompletionEngine engine, Document document, int position, CancellationToken cancellationToken)
		{
			var syntaxTree = await document.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
			if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
				syntaxTree.IsPreProcessorDirectiveContext(position, cancellationToken))
			{
				return Enumerable.Empty<CompletionData>();
			}

			// If we're in a generic type argument context, use the start of the generic type name
			// as the position for the rest of the context checks.
			int testPosition = position;
			var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

			var semanticModel = await document.GetCSharpSemanticModelForNodeAsync(leftToken.Parent, cancellationToken).ConfigureAwait(false);
			if (syntaxTree.IsGenericTypeArgumentContext(position, leftToken, cancellationToken, semanticModel))
			{
				// Walk out until we find the start of the partial written generic
				SyntaxToken nameToken;
				while (syntaxTree.IsInPartiallyWrittenGeneric(testPosition, cancellationToken, out nameToken))
				{
					testPosition = nameToken.SpanStart;
				}

				// If the user types Foo<T, automatic brace completion will insert the close brace
				// and the generic won't be "partially written".
				if (testPosition == position)
				{
					var typeArgumentList = leftToken.GetAncestor<TypeArgumentListSyntax>();
					if (typeArgumentList != null)
					{
						if (typeArgumentList.LessThanToken != default(SyntaxToken) && typeArgumentList.GreaterThanToken != default(SyntaxToken))
						{
							testPosition = typeArgumentList.LessThanToken.SpanStart;
						}
					}
				}
			}

			if ((!leftToken.GetPreviousTokenIfTouchingWord(position).IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword) &&
				syntaxTree.IsMemberDeclarationContext(testPosition, contextOpt: null, validModifiers: SyntaxKindSet.AllMemberModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken)) ||
				syntaxTree.IsGlobalMemberDeclarationContext(testPosition, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
				syntaxTree.IsGlobalStatementContext(testPosition, cancellationToken) ||
				syntaxTree.IsDelegateReturnTypeContext(testPosition, syntaxTree.FindTokenOnLeftOfPosition(testPosition, cancellationToken), cancellationToken))
			{
				const string T = "T";
				return new [] { engine.Factory.CreateGenericData (this, T, GenericDataType.Undefined) };
			}
			return Enumerable.Empty<CompletionData>();
		}
	}
}