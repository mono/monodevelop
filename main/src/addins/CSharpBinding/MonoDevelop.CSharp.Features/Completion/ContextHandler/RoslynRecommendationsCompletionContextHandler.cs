//
// RoslynRecommendationsCompletionContextHandler.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{

	//	class CompletionEngineCache
	//	{
	//		public List<INamespace>  namespaces;
	//		public ICompletionData[] importCompletion;
	//	}

	class RoslynRecommendationsCompletionContextHandler : CompletionContextHandler
	{
		public override bool IsTriggerCharacter (SourceText text, int position)
		{
			var ch = text [position];
			return ch == '.' || 
				ch == ' ' && position >= 1 && !char.IsWhiteSpace (text [position - 1]) ||
				ch == '#' || // pre processor directives 
				ch == '>' && position >= 1 && text [position - 1] == '-' || // pointer member access
				ch == ':' && position >= 1 && text [position - 1] == ':' || // alias name
				IsStartingNewWord (text, position);
		}

		bool IsException (ITypeSymbol type)
		{
			if (type == null)
				return false;
			if (type.Name == "Exception" && type.ContainingNamespace.Name == "System")
				return true;
			return IsException (type.BaseType);
		}

		protected override async Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var semanticModel = ctx.SemanticModel;
			var result = new List<CompletionData> ();
			if (info.TriggerCharacter == ' ') {
				var newExpression = ObjectCreationContextHandler.GetObjectCreationNewExpression (ctx.SyntaxTree, completionContext.Position, cancellationToken);
				if (newExpression == null && info.CompletionTriggerReason == CompletionTriggerReason.CharTyped  && !ctx.LeftToken.IsKind (SyntaxKind.EqualsToken) && !ctx.LeftToken.IsKind (SyntaxKind.EqualsEqualsToken))
					return Enumerable.Empty<CompletionData> ();

				completionResult.AutoCompleteEmptyMatch = false;
			}

			var parent = ctx.TargetToken.Parent;
			bool isInAttribute = ctx.CSharpSyntaxContext.IsAttributeNameContext;
			bool isInBaseList = parent != null && parent.IsKind (SyntaxKind.BaseList);
			bool isInUsingDirective = parent != null && parent.Parent != null && parent.Parent.IsKind (SyntaxKind.UsingDirective) && !parent.IsKind (SyntaxKind.QualifiedName);
			var isInQuery = ctx.CSharpSyntaxContext.IsInQuery;
			var completionDataLookup = new Dictionary<Tuple<string, SymbolKind>, ISymbolCompletionData> ();
			bool isInCatchTypeExpression = parent != null && parent.IsKind (SyntaxKind.CatchDeclaration) || 
			                               parent.IsKind (SyntaxKind.QualifiedName) && parent.Parent != null && parent.Parent.IsKind (SyntaxKind.CatchDeclaration);
			Action<ISymbolCompletionData> addData = d => {
				var key = Tuple.Create (d.DisplayText, d.Symbol.Kind);
				ISymbolCompletionData data;
				if (completionDataLookup.TryGetValue (key, out data)) {
					data.AddOverload (d);
					return;
				}
				completionDataLookup.Add (key, d);
				result.Add (d);
			};

			var completionCategoryLookup = new Dictionary<string, CompletionCategory> ();
			foreach (var symbol in await Recommender.GetRecommendedSymbolsAtPositionAsync (semanticModel, completionContext.Position, engine.Workspace, null, cancellationToken)) {
				if (symbol.Kind == SymbolKind.NamedType) {
					if (isInAttribute) {
						var type = (ITypeSymbol)symbol;
						if (type.IsAttribute ()) {
							const string attributeSuffix = "Attribute";
							var v = type.Name.EndsWith (attributeSuffix, StringComparison.Ordinal)
								? type.Name.Substring (0, type.Name.Length - attributeSuffix.Length)
								: type.Name;

							var needsEscaping = SyntaxFacts.GetKeywordKind(v) != SyntaxKind.None;
							needsEscaping = needsEscaping || (isInQuery && SyntaxFacts.IsQueryContextualKeyword(SyntaxFacts.GetContextualKeywordKind(v)));
							if (!needsEscaping) {
								addData (engine.Factory.CreateSymbolCompletionData (this, symbol, v));
								continue;
							}
						}
					}
					if (isInBaseList) {
						var type = (ITypeSymbol)symbol;
						if (type.IsSealed || type.IsStatic)
							continue;
					}
					if (isInCatchTypeExpression) {
						var type = (ITypeSymbol)symbol;
						if (!IsException (type))
							continue;
					}
				}

				if (isInUsingDirective && symbol.Kind != SymbolKind.Namespace)
					continue;

				var newData = engine.Factory.CreateSymbolCompletionData (this, symbol, symbol.Name.EscapeIdentifier (isInQuery));
				ISymbol categorySymbol;
				var method = symbol as IMethodSymbol;
				if (method != null) {
					if (method.IsReducedExtension ()) {
						categorySymbol = method.ReceiverType;
					} else {
						categorySymbol = (ISymbol)symbol.ContainingType;
					}
				} else {
					categorySymbol = (ISymbol)symbol.ContainingType ?? symbol.ContainingNamespace;
				}
				if (categorySymbol != null) {
					CompletionCategory category;
					var key = categorySymbol.ToDisplayString ();
					if (!completionCategoryLookup.TryGetValue (key, out category)) {
						completionCategoryLookup [key] = category = engine.Factory.CreateCompletionDataCategory (categorySymbol);
					}
					newData.CompletionCategory = category;
				}
				addData (newData);
			}
			return (IEnumerable<CompletionData>)result;
		}

		protected override async Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
		{
			bool? result = await IsTriggerOnDotAsync(document, characterPosition, cancellationToken).ConfigureAwait(false);
			if (result.HasValue)
			{
				return result.Value;
			}

			return true;
		}

		private async Task<bool?> IsTriggerOnDotAsync(Document document, int characterPosition, CancellationToken cancellationToken)
		{
			var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
			if (text[characterPosition] != '.')
			{
				return null;
			}

			// don't want to trigger after a number.  All other cases after dot are ok.
			var tree = await document.GetCSharpSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var token = tree.FindToken(characterPosition);
			if (token.Kind() == SyntaxKind.DotToken)
			{
				token = token.GetPreviousToken();
			}

			return token.Kind() != SyntaxKind.NumericLiteralToken;
		}
	}
}