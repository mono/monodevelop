//
// ExplicitInterfaceContextHandler.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class ExplicitInterfaceContextHandler : CompletionContextHandler
	{
		public override bool IsTriggerCharacter(SourceText text, int position)
		{
			return text[position] == '.';
		}

		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var position = completionContext.Position;
			var document = completionContext.Document;
			var span = new TextSpan(position, 0);
			var semanticModel = await document.GetCSharpSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
			var syntaxTree = semanticModel.SyntaxTree;
			//	var ctx = await completionContext.GetSyntaxContextAsync (engine.Workspace, cancellationToken).ConfigureAwait (false);

			if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
				syntaxTree.IsPreProcessorDirectiveContext(position, cancellationToken))
			{
				return Enumerable.Empty<CompletionData> ();
			}

			if (!syntaxTree.IsRightOfDotOrArrowOrColonColon(position, cancellationToken))
			{
				return Enumerable.Empty<CompletionData> ();
			}

			var node = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
				.GetPreviousTokenIfTouchingWord(position)
				.Parent;

			if (node.Kind() == SyntaxKind.ExplicitInterfaceSpecifier)
			{
				return await GetCompletionsOffOfExplicitInterfaceAsync(
					engine, document, semanticModel, position, ((ExplicitInterfaceSpecifierSyntax)node).Name, cancellationToken).ConfigureAwait(false);
			}

			return Enumerable.Empty<CompletionData> ();
		}

		private Task<IEnumerable<CompletionData>> GetCompletionsOffOfExplicitInterfaceAsync(
			CompletionEngine engine, Document document, SemanticModel semanticModel, int position, NameSyntax name, CancellationToken cancellationToken)
		{
			// Bind the interface name which is to the left of the dot
			var syntaxTree = semanticModel.SyntaxTree;
			var nameBinding = semanticModel.GetSymbolInfo(name, cancellationToken);
			// var context = CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);

			var symbol = nameBinding.Symbol as ITypeSymbol;
			if (symbol == null || symbol.TypeKind != TypeKind.Interface)
			{
				return Task.FromResult (Enumerable.Empty<CompletionData> ());
			}

			var members = semanticModel.LookupSymbols (
	              position: name.SpanStart,
	              container: symbol)
				.Where (s => !s.IsStatic);
			//	.FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);

			// We're going to create a entry for each one, including the signature
			var completions = new List<CompletionData>();

//			var signatureDisplayFormat =
//				new SymbolDisplayFormat(
//					genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
//					memberOptions:
//					SymbolDisplayMemberOptions.IncludeParameters,
//					parameterOptions:
//					SymbolDisplayParameterOptions.IncludeName |
//					SymbolDisplayParameterOptions.IncludeType |
//					SymbolDisplayParameterOptions.IncludeParamsRefOut,
//					miscellaneousOptions:
//					SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
//					SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

			var namePosition = name.SpanStart;

			// var text = await context.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
			// var textChangeSpan = GetTextChangeSpan(text, context.Position);

			foreach (var member in members)
			{
				// var displayString = member.ToMinimalDisplayString(semanticModel, namePosition, signatureDisplayFormat);
				// var memberCopied = member;
				// var insertionText = displayString;

				completions.Add(engine.Factory.CreateSymbolCompletionData (this, member)

					/*new SymbolCompletionItem(
					this,
					displayString,
					insertionText: insertionText,
					filterSpan: textChangeSpan,
					position: position,
					symbols: new List<ISymbol> { member },
					context: context) */);
			}

			return Task.FromResult ((IEnumerable<CompletionData>)completions);
		}

//		public override TextChange GetTextChange(CompletionItem selectedItem, char? ch = default(char), string textTypedSoFar = null)
//		{
//			if (ch.HasValue && ch.Value == '(')
//			{
//				return new TextChange(selectedItem.FilterSpan, ((SymbolCompletionItem)selectedItem).Symbols[0].Name);
//			}
//
//			return new TextChange(selectedItem.FilterSpan, selectedItem.DisplayText);
//		}

	}
}

