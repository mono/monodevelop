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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Completion.Provider
{/*
	[ExportCompletionProvider ("CastCompletionProvider", LanguageNames.CSharp)]
	class CastCompletionProvider : CommonCompletionProvider
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
			if (symbolInfo.Symbol == null)
				return Task.FromResult (Enumerable.Empty<CompletionData> ());

			var list = new List<CompletionData> ();
			var within = model.GetEnclosingNamedTypeOrAssembly (position, cancellationToken);
			var addedSymbols = new HashSet<string> ();
			SyntaxNode ancestor = ma.Expression;
			while (ancestor != null) {
				// check parent if for direct type check
				var ifStmSyntax = ancestor as IfStatementSyntax;
				if (ifStmSyntax != null) {
					var condition = SkipParens (ifStmSyntax.Condition);
					if (condition != null && condition.IsKind (SyntaxKind.IsExpression)) {
						var isExpr = ((BinaryExpressionSyntax)condition);
						var leftSymbol = model.GetSymbolInfo (isExpr.Left);

						if (leftSymbol.Symbol == symbolInfo.Symbol) {
							var type = model.GetTypeInfo (isExpr.Right).Type;
							if (type != null) {
								Analyze (engine, ma.Expression, type, model.GetTypeInfo (isExpr.Left).Type, within, list, addedSymbols, cancellationToken);
							}
						}
					}
					// skip if else ... if else
					if (ancestor.Parent is ElseClauseSyntax) {
						while (ancestor is IfStatementSyntax || ancestor is ElseClauseSyntax)
							ancestor = ancestor.Parent;
						continue;
					}
					goto loop;
				}

				// check parent block if an if is there that checks the type
				var blockSyntax = ancestor as BlockSyntax;
				if (blockSyntax != null) {
					foreach (var ifStmt in blockSyntax.Statements.OfType<IfStatementSyntax> ()) {
						if (ifStmt.Span.End >= ma.Span.Start)
							break;
						var condition = SkipParens (ifStmt.Condition);
						bool wasNegated = false;
						if (condition.IsKind (SyntaxKind.LogicalNotExpression)) {
							condition = SkipParens (((PrefixUnaryExpressionSyntax)condition).Operand);
							wasNegated = true;
						}
						if (condition == null || !condition.IsKind (SyntaxKind.IsExpression))
							goto loop;
						var stmt = ifStmt.Statement;
						if (stmt is BlockSyntax) {
							stmt = ((BlockSyntax)stmt).Statements.LastOrDefault ();
						}
						if (!wasNegated ||
							stmt == null ||
							!stmt.IsKind (SyntaxKind.ReturnStatement) && !stmt.IsKind (SyntaxKind.ContinueStatement) && !stmt.IsKind (SyntaxKind.BreakStatement) && !stmt.IsKind (SyntaxKind.ThrowStatement))
							goto loop;

						var isExpr = ((BinaryExpressionSyntax)condition);
						var leftSymbol = model.GetSymbolInfo (isExpr.Left);
						if (leftSymbol.Symbol == symbolInfo.Symbol) {
							var type = model.GetTypeInfo (isExpr.Right).Type;
							if (type != null) {
								Analyze (engine, ma.Expression, type, model.GetTypeInfo (isExpr.Left).Type, within, list, addedSymbols, cancellationToken);
							}
						}
					}
				}

				var binOp = ancestor as BinaryExpressionSyntax;
				if (binOp != null && binOp.IsKind (SyntaxKind.LogicalAndExpression)) {
					if (SkipParens (binOp.Left).IsKind (SyntaxKind.IsExpression)) {
						var isExpr = (BinaryExpressionSyntax)SkipParens (binOp.Left);
						var leftSymbol = model.GetSymbolInfo (isExpr.Left);

						if (leftSymbol.Symbol == symbolInfo.Symbol) {
							var type = model.GetTypeInfo (isExpr.Right).Type;
							if (type != null) {
								Analyze (engine, ma.Expression, type, model.GetTypeInfo (isExpr.Left).Type, within, list, addedSymbols, cancellationToken);
							}
						}
					}
				}

			loop: ancestor = ancestor.Parent;
			}

			return Task.FromResult ((IEnumerable<CompletionData>)list);
		}

		static ExpressionSyntax SkipParens (ExpressionSyntax expression)
		{
			if (expression == null)
				return null;
			while (expression != null && expression.IsKind (SyntaxKind.ParenthesizedExpression)) {
				expression = ((ParenthesizedExpressionSyntax)expression).Expression;
			}
			return expression;
		}

		void Analyze (CompletionEngine engine, SyntaxNode node, ITypeSymbol type, ITypeSymbol stopAt, ISymbol within, List<CompletionData> list, HashSet<string> addedSymbols, CancellationToken cancellationToken)
		{
			var startType = type;

			while (type != null && type.SpecialType != SpecialType.System_Object && type != stopAt) {
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
	}*/
}

