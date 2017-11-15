//
// RegexContextHandler.cs
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
{
	[ExportCompletionProvider ("RegexCompletionProvider", LanguageNames.CSharp)]
	class RegexCompletionProvider : CommonCompletionProvider
	{
		internal static bool IsRegexMatchMethod (SymbolInfo symbolInfo)
		{
			var symbol = symbolInfo.Symbol;
			if (symbol == null)
				return false;
			return IsRegexType (symbol.ContainingType) && symbol.IsStatic && (symbol.Name == "IsMatch" || symbol.Name == "Match" || symbol.Name == "Matches");
		}

		internal static bool IsRegexConstructor (SymbolInfo symbolInfo)
		{
			return symbolInfo.Symbol?.ContainingType is INamedTypeSymbol && IsRegexType (symbolInfo.Symbol.ContainingType);
		}

		internal static bool IsRegexType (INamedTypeSymbol containingType)
		{
			return containingType != null && containingType.Name == "Regex" && containingType.ContainingNamespace.GetFullName () == "System.Text.RegularExpressions";
		}

		public override bool ShouldTriggerCompletion (SourceText text, int position, CompletionTrigger trigger, Microsoft.CodeAnalysis.Options.OptionSet options)
		{
			return trigger.Character == '\\';
		}

		public override async Task ProvideCompletionsAsync (Microsoft.CodeAnalysis.Completion.CompletionContext context)
		{
			var document = context.Document;
			var position = context.Position;
			var cancellationToken = context.CancellationToken;

			var semanticModel = await document.GetSemanticModelForSpanAsync (new TextSpan (position, 0), cancellationToken).ConfigureAwait (false);

			var workspace = document.Project.Solution.Workspace;
			var ctx = CSharpSyntaxContext.CreateContext (workspace, semanticModel, position, cancellationToken);
			if (context.Trigger.Character == '\\') {
				if (ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.Parent != null &&
				ctx.TargetToken.Parent.Parent.IsKind (SyntaxKind.Argument)) {
					var argument = ctx.TargetToken.Parent.Parent as ArgumentSyntax;

					var symbolInfo = semanticModel.GetSymbolInfo (ctx.TargetToken.Parent.Parent.Parent.Parent);
					if (symbolInfo.Symbol == null)
						return;

					if (IsRegexMatchMethod (symbolInfo)) {
						if (((ArgumentListSyntax)argument.Parent).Arguments [1] != argument)
							return;
						AddFormatCompletionData (context, argument.Expression.ToString () [0] == '@');
						return;
					}
					if (IsRegexConstructor (symbolInfo)) {
						if (((ArgumentListSyntax)argument.Parent).Arguments [0] != argument)
							return;
						AddFormatCompletionData (context, argument.Expression.ToString () [0] == '@');
						return;
					}
				}
			} else {
				var ma = ctx.TargetToken.Parent as MemberAccessExpressionSyntax;
				if (ma != null) {
					var symbolInfo = semanticModel.GetSymbolInfo (ma.Expression);
					var typeInfo = semanticModel.GetTypeInfo (ma.Expression);
					var type = typeInfo.Type;
					if (type != null && type.Name == "Match" && type.ContainingNamespace.GetFullName () == "System.Text.RegularExpressions") {
						foreach (var grp in GetGroups (ctx, symbolInfo.Symbol)) {
							context.AddItem (FormatItemCompletionProvider.CreateCompletionItem ("Groups[\"" + grp + "\"]", null, null));
						}
					}
				}
			}
		}

		IEnumerable<string> GetGroups (CSharpSyntaxContext ctx, ISymbol symbol)
		{
			var root = ctx.SyntaxTree.GetRoot ();
			foreach (var decl in symbol.DeclaringSyntaxReferences) {
				Optional<object> val = null;

				var node = root.FindNode (decl.Span) as VariableDeclaratorSyntax;
				if (node == null)
					continue;
				var invocation = node.Initializer.Value as InvocationExpressionSyntax;
				var invocationSymbol = ctx.SemanticModel.GetSymbolInfo (invocation).Symbol;
				if (invocationSymbol.Name == "Match" && IsRegexType (invocationSymbol.ContainingType)) {
					if (invocation.ArgumentList.Arguments.Count == 1) {
						var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
						if (memberAccess == null)
							continue;
						var target = ctx.SemanticModel.GetSymbolInfo (memberAccess.Expression).Symbol;
						if (target.DeclaringSyntaxReferences.Length == 0)
							continue;
						var targetNode = root.FindNode (target.DeclaringSyntaxReferences.First ().Span) as VariableDeclaratorSyntax;
						if (targetNode == null)
							continue;
						var objectCreation = targetNode.Initializer.Value as ObjectCreationExpressionSyntax;
						if (objectCreation == null)
							continue;
						var targetNodeSymbol = ctx.SemanticModel.GetSymbolInfo (objectCreation).Symbol;
						if (IsRegexType (targetNodeSymbol.ContainingType)) {
							if (objectCreation.ArgumentList.Arguments.Count < 1)
								continue;
							val = ctx.SemanticModel.GetConstantValue (objectCreation.ArgumentList.Arguments [0].Expression);
						}
					} else {
						if (invocation.ArgumentList.Arguments.Count < 2)
							continue;
						val = ctx.SemanticModel.GetConstantValue (invocation.ArgumentList.Arguments [1].Expression);
					}

					if (!val.HasValue)
						continue;
					var str = val.Value.ToString ();
					int idx = -1;
					while ((idx = str.IndexOf ("(?<", idx + 1, StringComparison.Ordinal)) >= 0) {
						var closingIndex = str.IndexOf (">", idx, StringComparison.Ordinal);
						if (closingIndex >= idx) {
							yield return str.Substring (idx + 3, closingIndex - idx - 3);
							idx = closingIndex - 1;
						}
					}
				}
			}
		}

		static readonly CompletionItem [] formatItems =  {
			FormatItemCompletionProvider.CreateCompletionItem("d", "Digit character", null),
			FormatItemCompletionProvider.CreateCompletionItem("D", "Non-digit character", null),
			FormatItemCompletionProvider.CreateCompletionItem("b", "Word boundary", null),
			FormatItemCompletionProvider.CreateCompletionItem("B", "Non-word boundary", null),
			FormatItemCompletionProvider.CreateCompletionItem("w", "Word character", null),
			FormatItemCompletionProvider.CreateCompletionItem("W", "Non-word character", null),
			FormatItemCompletionProvider.CreateCompletionItem("s", "White-space character", null),
			FormatItemCompletionProvider.CreateCompletionItem("S", "Non-white-space character", null),
			FormatItemCompletionProvider.CreateCompletionItem("A", "Start boundary", null),
			FormatItemCompletionProvider.CreateCompletionItem("Z", "End boundary", null),
			FormatItemCompletionProvider.CreateCompletionItem("k<name>", "Named backreference", null),
			FormatItemCompletionProvider.CreateCompletionItem("P{name}", "Negative unicode category or unicode block", null),
			FormatItemCompletionProvider.CreateCompletionItem("p{name}", "Unicode category or unicode block", null)
		};

		static readonly CompletionItem [] verbatimFormatItems =  {
			FormatItemCompletionProvider.CreateCompletionItem("\\d", "Digit character", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\D", "Non-digit character", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\b", "Word boundary", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\B", "Non-word boundary", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\w", "Word character", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\W", "Non-word character", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\s", "White-space character", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\S", "Non-white-space character", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\A", "Start boundary", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\Z", "End boundary", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\k<name>", "Named backreference", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\P{name}", "Negative unicode category or unicode block", null),
			FormatItemCompletionProvider.CreateCompletionItem("\\p{name}", "Unicode category or unicode block", null)
		};

		void AddFormatCompletionData (Microsoft.CodeAnalysis.Completion.CompletionContext context, bool isVerbatimString)
		{
			context.AddItems (isVerbatimString ? verbatimFormatItems : formatItems);
		}
	}
}