//
// EventSenderCompletionProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
/*using System;
namespace CompletionProvider
{
	class EventSenderCompletionProvider : CompletionContextHandler
	{
		protected override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var position = completionContext.Position;
			var document = completionContext.Document;
			var syntaxTree = ctx.SyntaxTree;
			if (syntaxTree.IsInNonUserCode (position, cancellationToken) ||
				syntaxTree.IsPreProcessorDirectiveContext (position, cancellationToken))
				return Task.FromResult (Enumerable.Empty<CompletionData> ());
			if (!syntaxTree.IsRightOfDotOrArrowOrColonColon (position, cancellationToken))
				return Task.FromResult (Enumerable.Empty<CompletionData> ());
			var ma = ctx.LeftToken.Parent as MemberAccessExpressionSyntax;
			if (ma == null)
				return Task.FromResult (Enumerable.Empty<CompletionData> ());

			var model = ctx.CSharpSyntaxContext.SemanticModel;

			var symbolInfo = model.GetSymbolInfo (ma.Expression);
			if (symbolInfo.Symbol == null || symbolInfo.Symbol.Kind != SymbolKind.Parameter)
				return Task.FromResult (Enumerable.Empty<CompletionData> ());
			var list = new List<CompletionData> ();
			var within = model.GetEnclosingNamedTypeOrAssembly (position, cancellationToken);
			var addedSymbols = new HashSet<string> ();

			foreach (var ano in ma.AncestorsAndSelf ().OfType<AnonymousMethodExpressionSyntax> ()) {
				Analyze (engine, model, ma.Expression, within, list, ano.ParameterList, symbolInfo.Symbol, addedSymbols, cancellationToken);
			}

			foreach (var ano in ma.AncestorsAndSelf ().OfType<ParenthesizedLambdaExpressionSyntax> ()) {
				Analyze (engine, model, ma.Expression, within, list, ano.ParameterList, symbolInfo.Symbol, addedSymbols, cancellationToken);
			}

			return Task.FromResult ((IEnumerable<CompletionData>)list);
		}

		void Analyze (CompletionEngine engine, SemanticModel model, SyntaxNode node, ISymbol within, List<CompletionData> list, ParameterListSyntax parameterList, ISymbol symbol, HashSet<string> addedSymbols, CancellationToken cancellationToken)
		{
			var type = CheckParameterList (model, parameterList, symbol, cancellationToken);
			if (type == null)
				return;
			var startType = type;

			while (type.SpecialType != SpecialType.System_Object) {
				foreach (var member in type.GetMembers ()) {
					if (member.IsImplicitlyDeclared || member.IsStatic)
						continue;
					if (member.IsOrdinaryMethod () || member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Property) {
						if (member.IsAccessibleWithin (within)) {
							var completionData = engine.Factory.CreateCastCompletionData (this, member, node, startType);
							if (addedSymbols.Contains (completionData.DisplayText))
								continue;
							addedSymbols.Add (completionData.DisplayText);
							list.Add (completionData);
						}
					}
				}

				type = type.BaseType;
			}
		}

		static ITypeSymbol CheckParameterList (SemanticModel model, ParameterListSyntax listSyntax, ISymbol parameter, CancellationToken cancellationToken)
		{
			var param = listSyntax?.Parameters.FirstOrDefault ();
			if (param == null)
				return null;
			var declared = model.GetDeclaredSymbol (param, cancellationToken);
			if (declared != parameter)
				return null;
			var assignmentExpr = listSyntax.Parent.Parent as AssignmentExpressionSyntax;
			if (assignmentExpr == null || !assignmentExpr.IsKind (SyntaxKind.AddAssignmentExpression))
				return null;
			var left = assignmentExpr.Left as MemberAccessExpressionSyntax;
			if (left == null)
				return null;
			var symbolInfo = model.GetSymbolInfo (left.Expression);
			if (symbolInfo.Symbol == null || symbolInfo.Symbol is ITypeSymbol)
				return null;
			return model.GetTypeInfo (left.Expression).Type;
		}
	}
}

*/