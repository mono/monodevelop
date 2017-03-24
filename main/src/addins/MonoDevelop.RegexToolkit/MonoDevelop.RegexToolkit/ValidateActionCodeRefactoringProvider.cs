//
// ValidateActionCodeRefactoringProvider.cs
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
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using MonoDevelop.Core;
using MonoDevelop.RegexToolkit;

namespace MonoDevelop.RegexToolkit.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "Validate regular expression")]
	public class ValidateActionCodeRefactoringProvider : CodeRefactoringProvider
	{
		public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (!span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;

			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			//if (model.IsFromGeneratedCode(cancellationToken))
			//	return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			if (!root.Span.Contains (span))
				return;
			var node = root.FindNode(span, false, true);
			if (!node.IsKind(SyntaxKind.StringLiteralExpression))
				return;
			var argument = node.Parent as ArgumentSyntax;
			if (argument == null)
				return;
			var list = argument.Parent as ArgumentListSyntax;

			var invocation = argument.Parent?.Parent as InvocationExpressionSyntax;
			if (invocation != null) {
				var info = model.GetSymbolInfo (invocation);
				if (!IsRegexMatchMethod (info) || list.Arguments [1] != argument)
					return;
			}

			var oce = argument.Parent?.Parent as ObjectCreationExpressionSyntax;
			if (oce != null) {
				var info = model.GetSymbolInfo (oce);
				if (info.Symbol == null || !IsRegexType (info.Symbol.ContainingType) || list.Arguments [0] != argument)
					return;
			}
			 
			var regex = model.GetConstantValue (node);
			if (!regex.HasValue)
				return;
			
			context.RegisterRefactoring (CodeAction.Create(
				GettextCatalog.GetString("Validate regular expression"),
				t2 => {
					ShowRegexToolkitHandler.RunRegexWindow ().Regex = regex.Value.ToString ();
					return Task.FromResult(document);
				}
			));
		}

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
			return containingType != null && containingType.Name == "Regex" && containingType.ContainingNamespace.ToDisplayString () == "System.Text.RegularExpressions";
		}
	}
}
