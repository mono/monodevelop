// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Features.IntroduceVariable
{
	public class IntroduceVariableResult
	{
		public static readonly IntroduceVariableResult Failure = new IntroduceVariableResult(null);

		private readonly CodeRefactoring _codeRefactoring;

		public IntroduceVariableResult(CodeRefactoring codeRefactoring)
		{
			_codeRefactoring = codeRefactoring;
		}

		public bool ContainsChanges
		{
			get
			{
				return _codeRefactoring != null;
			}
		}

		public CodeRefactoring GetCodeRefactoring(CancellationToken cancellationToken)
		{
			return _codeRefactoring;
		}
	}
}
