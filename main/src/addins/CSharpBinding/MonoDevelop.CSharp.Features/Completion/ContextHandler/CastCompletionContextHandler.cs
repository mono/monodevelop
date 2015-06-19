//
// CastCompletionContextHandler.cs
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
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class CastCompletionContextHandler : CompletionContextHandler
	{
		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, CancellationToken cancellationToken)
		{
			var position = completionContext.Position;
			var document = completionContext.Document;
			var ctx = await completionContext.GetSyntaxContextAsync (engine.Workspace, cancellationToken).ConfigureAwait (false);
			var syntaxTree = ctx.SyntaxTree;
			if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
				syntaxTree.IsPreProcessorDirectiveContext(position, cancellationToken))
				return Enumerable.Empty<CompletionData> ();
			if (!syntaxTree.IsRightOfDotOrArrowOrColonColon(position, cancellationToken))
				return Enumerable.Empty<CompletionData> ();
			var ma = ctx.LeftToken.Parent as MemberAccessExpressionSyntax;
			if (ma == null)
				return Enumerable.Empty<CompletionData> ();

			var model = ctx.CSharpSyntaxContext.SemanticModel;

			var symbolInfo = model.GetSymbolInfo (ma.Expression);
			if (symbolInfo.Symbol == null)
				return Enumerable.Empty<CompletionData> ();

			var list = new List<CompletionData> ();
			var within = model.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
			var addedSymbols = new HashSet<string> ();
			foreach (var ifStmSyntax in ma.Expression.AncestorsAndSelf ().OfType<IfStatementSyntax> ()) {
				var condition = ifStmSyntax.Condition.SkipParens ();
				if (condition == null || !condition.IsKind (SyntaxKind.IsExpression))
					continue;
				var isExpr = ((BinaryExpressionSyntax)condition);
				var leftSymbol = model.GetSymbolInfo (isExpr.Left);

				if (leftSymbol.Symbol == symbolInfo.Symbol) {
					var type = model.GetTypeInfo (isExpr.Right).Type;
					if (type != null) {
						Analyze (engine, ma.Expression, type, within, list, addedSymbols, cancellationToken);
					}
				}
			}

			return list;
		}

		void Analyze (CompletionEngine engine, SyntaxNode node, ITypeSymbol type, ISymbol within, List<CompletionData> list, HashSet<string> addedSymbols, CancellationToken cancellationToken)
		{
			var startType = type;

			while (type.SpecialType != SpecialType.System_Object) {
				foreach (var member in type.GetMembers ()) {
					cancellationToken.ThrowIfCancellationRequested ();
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
	}
}

