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
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.Completion.Providers;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Completion.Provider
{
	[ExportCompletionProvider ("EventSenderCompletionProvider", LanguageNames.CSharp)]
	class EventSenderCompletionProvider : CommonCompletionProvider
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
			if (symbolInfo.Symbol == null || symbolInfo.Symbol.Kind != SymbolKind.Parameter)
				return;
			var within = model.GetEnclosingNamedTypeOrAssembly (position, cancellationToken);
			var addedSymbols = new HashSet<string> ();

			foreach (var ano in ma.AncestorsAndSelf ().OfType<AnonymousMethodExpressionSyntax> ()) {
				Analyze (context, model, ma.Expression, within, ano.ParameterList, symbolInfo.Symbol, addedSymbols, cancellationToken);
			}

			foreach (var ano in ma.AncestorsAndSelf ().OfType<ParenthesizedLambdaExpressionSyntax> ()) {
				Analyze (context, model, ma.Expression, within, ano.ParameterList, symbolInfo.Symbol, addedSymbols, cancellationToken);
			}
		}

		protected override Task<TextChange?> GetTextChangeAsync (CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
		{
			var node = selectedItem.Properties ["NodeString"];
			return Task.FromResult<TextChange?> (new TextChange (new TextSpan (selectedItem.Span.Start - node.Length - 1, selectedItem.Span.Length + node.Length + 1), "((" + selectedItem.Properties ["CastTypeString"] + ")" + node + ")." + selectedItem.DisplayText));
		}

		void Analyze (CompletionContext context, SemanticModel model, SyntaxNode node, ISymbol within, ParameterListSyntax parameterList, ISymbol symbol, HashSet<string> addedSymbols, CancellationToken cancellationToken)
		{
			var type = CheckParameterList (model, parameterList, symbol, cancellationToken);
			if (type == null)
				return;
			var startType = type;

			var typeString = CSharpAmbience.SafeMinimalDisplayString (type, model, context.CompletionListSpan.Start, Ambience.LabelFormat);
			var pDict = ImmutableDictionary<string, string>.Empty;
			if (typeString != null)
				pDict = pDict.Add ("CastTypeString", typeString);
			pDict = pDict.Add ("DescriptionMarkup", "- <span foreground=\"darkgray\" size='small'>" + GettextCatalog.GetString ("Cast to '{0}'", type.Name) + "</span>");
			pDict = pDict.Add ("NodeString", node.ToString ());

			while (type.SpecialType != SpecialType.System_Object) {
				foreach (var member in type.GetMembers ()) {
					if (member.IsImplicitlyDeclared || member.IsStatic)
						continue;
					if (member.IsOrdinaryMethod () || member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Property) {
						if (member.IsAccessibleWithin (within)) {

							var completionData = SymbolCompletionItem.CreateWithSymbolId (
								member.Name,
								new [] { member },
								CompletionItemRules.Default,
								context.Position,
								properties: pDict
							);

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

		protected override async Task<CompletionDescription> GetDescriptionWorkerAsync (Document document, CompletionItem item, CancellationToken cancellationToken)
		{
			return await SymbolCompletionItem.GetDescriptionAsync (item, document, cancellationToken).ConfigureAwait (false);
		}
	}
}