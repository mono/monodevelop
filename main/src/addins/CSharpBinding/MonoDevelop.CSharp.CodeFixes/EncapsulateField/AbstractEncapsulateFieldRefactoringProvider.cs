// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings.EncapsulateField;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.EncapsulateField
{
	internal abstract class AbstractEncapsulateFieldRefactoringProvider : CodeRefactoringProvider
	{
		CSharpEncapsulateFieldService service = new CSharpEncapsulateFieldService ();

		public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var actions = await service.GetEncapsulateFieldCodeActionsAsync(context.Document, context.Span, context.CancellationToken).ConfigureAwait(false);
			foreach (var action in actions)
				context.RegisterRefactoring(action);
		}
	}
}
