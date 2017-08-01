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
using System.Threading;
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
{
	[ExportCompletionProvider ("CastCompletionProvider", LanguageNames.CSharp)]
	class CastCompletionProvider : CommonCompletionProvider
	{
		public override async Task ProvideCompletionsAsync (Microsoft.CodeAnalysis.Completion.CompletionContext context)
		{
			var document = context.Document;
			var position = context.Position;
			var cancellationToken = context.CancellationToken;

			var model = await document.GetSemanticModelForSpanAsync (new TextSpan (position, 0), cancellationToken).ConfigureAwait (false);

			var workspace = document.Project.Solution.Workspace;
			var ctx = CSharpSyntaxContext.CreateContext (workspace, model, position, cancellationToken);

			var syntaxTree = ctx.SyntaxTree;
			if (syntaxTree.IsInNonUserCode (position, cancellationToken) ||
				syntaxTree.IsPreProcessorDirectiveContext (position, cancellationToken))
				return;
			if (!syntaxTree.IsRightOfDotOrArrowOrColonColon (position, cancellationToken))
				return;
			var ma = ctx.LeftToken.Parent as MemberAccessExpressionSyntax;
			if (ma == null)
				return;

			var symbolInfo = model.GetSymbolInfo (ma.Expression);
			if (symbolInfo.Symbol == null)
				return;

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
								Analyze (context, model, ma.Expression, type, model.GetTypeInfo (isExpr.Left).Type, within, addedSymbols, cancellationToken);
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
								Analyze (context, model, ma.Expression, type, model.GetTypeInfo (isExpr.Left).Type, within, addedSymbols, cancellationToken);
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
								Analyze (context, model, ma.Expression, type, model.GetTypeInfo (isExpr.Left).Type, within, addedSymbols, cancellationToken);
							}
						}
					}
				}

			loop: ancestor = ancestor.Parent;
			}
		}

		protected override Task<TextChange?> GetTextChangeAsync (CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
		{
			var node = selectedItem.Properties ["NodeString"];
			return Task.FromResult<TextChange?> (new TextChange (new TextSpan (selectedItem.Span.Start - node.Length - 1, selectedItem.Span.Length + node.Length + 1), "((" + selectedItem.Properties ["CastTypeString"] + ")" + node + ")." + selectedItem.DisplayText));
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

		void Analyze (Microsoft.CodeAnalysis.Completion.CompletionContext context, SemanticModel model, SyntaxNode node, ITypeSymbol type, ITypeSymbol stopAt, ISymbol within, HashSet<string> addedSymbols, CancellationToken cancellationToken)
		{
			var startType = type;
			var typeString = CSharpAmbience.SafeMinimalDisplayString (type, model, context.CompletionListSpan.Start, Ambience.LabelFormat);
			var pDict = ImmutableDictionary<string, string>.Empty;
			if (typeString != null)
				pDict = pDict.Add ("CastTypeString", typeString);
			pDict = pDict.Add ("NodeString", node.ToString ());

			while (type != null && type.SpecialType != SpecialType.System_Object && type != stopAt) {
				foreach (var member in type.GetMembers ()) {
					cancellationToken.ThrowIfCancellationRequested ();
					if (member.IsImplicitlyDeclared || member.IsStatic)
						continue;
					if (member.IsOrdinaryMethod () || member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Property) {
						if (member.IsAccessibleWithin (within)) {
							var completionData = CompletionItem.Create (member.Name, properties: pDict);
							if (addedSymbols.Contains (completionData.DisplayText))
								continue;
							addedSymbols.Add (completionData.DisplayText);
							context.AddItem (completionData);
						}
					}
				}
				type = type.BaseType;
			}
		}
	}
}

