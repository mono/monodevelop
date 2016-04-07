// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.Features.IntroduceVariable;
using ICSharpCode.NRefactory6.CSharp;
using RefactoringEssentials;

namespace MonoDevelop.CSharp.CodeRefactorings.IntroduceVariable
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.IntroduceVariable), Shared]
	class IntroduceVariableCodeRefactoringProvider : CodeRefactoringProvider
	{
		static readonly CSharpIntroduceVariableService service = new CSharpIntroduceVariableService ();

		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var textSpan = context.Span;
			var cancellationToken = context.CancellationToken;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
			{
				return;
			}
			var model = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			if (model.IsFromGeneratedCode (cancellationToken))
				return;
			var result = await service.IntroduceVariableAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

			if (!result.ContainsChanges)
			{
				return;
			}

			var actions = result.GetCodeRefactoring(cancellationToken).Actions;
			context.RegisterRefactorings(actions);
		}
	}
}
