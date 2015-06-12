// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.ExtractMethod;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp.CodeRefactorings.ExtractMethod
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ExtractMethod), Shared]
	internal class ExtractMethodCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			// Don't bother if there isn't a selection
			var textSpan = context.Span;
			if (textSpan.IsEmpty)
			{
				return;
			}
			var document = context.Document;
			var cancellationToken = context.CancellationToken;

			var workspace = document.Project.Solution.Workspace;
			if (workspace.Kind == WorkspaceKind.MiscellaneousFiles)
			{
				return;
			}
			if (IdeApp.Workbench.ActiveDocument == null || IdeApp.Workbench.ActiveDocument.Editor == null)
				return;
			var activeInlineRenameSession = IdeApp.Workbench.ActiveDocument.Editor.EditMode != MonoDevelop.Ide.Editor.EditMode.Edit;
			if (activeInlineRenameSession)
			{
				return;
			}
			var model = await context.Document.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false);
			if (model.IsFromGeneratedCode (context.CancellationToken))
				return;
			
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			var action = await GetCodeActionAsync(document, textSpan, cancellationToken: cancellationToken).ConfigureAwait(false);
			if (action == null)
			{
				return;
			}

			context.RegisterRefactoring(action.Item1);
		}

		static CSharpExtractMethodService service = new CSharpExtractMethodService ();

		private async Task<Tuple<CodeAction, string>> GetCodeActionAsync(
			Document document,
			TextSpan textSpan,
			CancellationToken cancellationToken)
		{
			var options = document.Project.Solution.Workspace.Options;
			try {
				var result = await service.ExtractMethodAsync(
					document,
					textSpan,
					options,
					cancellationToken).ConfigureAwait(false);

				if (result.Succeeded || result.SucceededWithSuggestion)
				{
					var description = options.GetOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language) ?
						GettextCatalog.GetString ("Extract Method + Local") : GettextCatalog.GetString ("Extract Method");

					var codeAction = new DocumentChangeAction(textSpan, DiagnosticSeverity.Info, description, (c) => AddRenameAnnotationAsync(result.Document, result.InvocationNameToken, c));
					var methodBlock = result.MethodDeclarationNode;

					return Tuple.Create<CodeAction, string>(codeAction, methodBlock.ToString());
				}
			} catch (Exception) {
				// currently the extract method refactoring crashes often. Ignore the roslyn issues for now.
			}
			return null;
		}

		private async Task<Document> AddRenameAnnotationAsync(Document document, SyntaxToken invocationNameToken, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			var finalRoot = root.ReplaceToken(
				invocationNameToken,
				invocationNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()));

			return document.WithSyntaxRoot(finalRoot);
		}
	}
}