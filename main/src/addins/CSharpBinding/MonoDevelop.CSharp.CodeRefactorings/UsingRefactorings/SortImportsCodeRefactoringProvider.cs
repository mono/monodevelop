//
// SortImportsCodeRefactoringProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Refactoring;

namespace MonoDevelop.CSharp.CodeRefactorings.IntroduceVariable
{
	[ExportCodeRefactoringProvider (LanguageNames.CSharp, Name = "Sort Imports Code Action Provider"), Shared]
	class SortImportsCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync (CodeRefactoringContext context)
		{
			var document = context.Document;
			var textSpan = context.Span;
			var cancellationToken = context.CancellationToken;

			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles) {
				return;
			}

			if (document.IsGeneratedCode(cancellationToken)) {
				return;
			}

			var model = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var root = await document.GetCSharpSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
			if (textSpan.Start >= root.FullSpan.Length)
				return;
			var token = root.FindToken (textSpan.Start);

			if (!token.Span.Contains (textSpan)) {
				return;
			}

			var node = token.Parent.AncestorsAndSelf ().FirstOrDefault (n => n.IsKind (SyntaxKind.UsingDirective) || n.IsParentKind (SyntaxKind.ExternAliasDirective));
			if (node == null) {
				return;
			}

			context.RegisterRefactoring (
				new CodeAction.DocumentChangeAction (
					GettextCatalog.GetString ("Sort usings"),
					(t) => OrganizeImportsCommandHandler.SortUsingsAsync (document, t)));
		}
	}

	[ExportCodeRefactoringProvider (LanguageNames.CSharp, Name = "Sort And Remove Imports Code Action Provider"), Shared]
	class SortAndRemoveImportsCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync (CodeRefactoringContext context)
		{
			var document = context.Document;
			var textSpan = context.Span;
			var cancellationToken = context.CancellationToken;

			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles) {
				return;
			}

			if (document.IsGeneratedCode (cancellationToken)) {
				return;
			}

			var model = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var root = await document.GetCSharpSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
			if (textSpan.Start >= root.FullSpan.Length)
				return;
			var token = root.FindToken (textSpan.Start);
			if (!token.Span.Contains (textSpan)) {
				return;
			}

			var node = token.Parent.AncestorsAndSelf ().FirstOrDefault (n => n.IsKind (SyntaxKind.UsingDirective) || n.IsParentKind (SyntaxKind.ExternAliasDirective));
			if (node == null) {
				return;
			}

			context.RegisterRefactoring (
				new CodeAction.DocumentChangeAction (
					GettextCatalog.GetString ("Sort and remove usings"),
					(t) => SortAndRemoveImportsCommandHandler.SortAndRemoveAsync (document, t)));
		}
	}

}

