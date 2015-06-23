// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings.EncapsulateField
{
	public abstract class AbstractEncapsulateFieldRefactoringProvider : CodeRefactoringProvider
	{
		public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var service = context.Document.GetLanguageService<AbstractEncapsulateFieldService>();
			var actions = await service.GetEncapsulateFieldCodeActionsAsync(context.Document, context.Span, context.CancellationToken).ConfigureAwait(false);
			context.RegisterRefactorings(actions);
		}
	}
}
