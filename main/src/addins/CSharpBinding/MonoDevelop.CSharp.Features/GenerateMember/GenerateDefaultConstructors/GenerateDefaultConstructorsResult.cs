// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;
using ICSharpCode.NRefactory6.CSharp.Refactoring;

namespace ICSharpCode.NRefactory6.CSharp.GenerateMember.GenerateDefaultConstructors
{
	public class GenerateDefaultConstructorsResult : AbstractCodeRefactoringResult
	{
		public static readonly GenerateDefaultConstructorsResult Failure = new GenerateDefaultConstructorsResult(null);

		internal GenerateDefaultConstructorsResult(CodeRefactoring codeRefactoring)
			: base(codeRefactoring)
		{
		}
	}
}
