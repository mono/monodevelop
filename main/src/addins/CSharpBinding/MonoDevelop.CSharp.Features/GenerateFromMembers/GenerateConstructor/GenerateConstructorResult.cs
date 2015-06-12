// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;
using ICSharpCode.NRefactory6.CSharp.Refactoring;

namespace ICSharpCode.NRefactory6.CSharp.GenerateFromMembers.GenerateConstructor
{
	public class GenerateConstructorResult : AbstractCodeRefactoringResult
	{
		public static readonly GenerateConstructorResult Failure = new GenerateConstructorResult(null);

		public GenerateConstructorResult(CodeRefactoring codeRefactoring)
			: base(codeRefactoring)
		{
		}
	}
}
