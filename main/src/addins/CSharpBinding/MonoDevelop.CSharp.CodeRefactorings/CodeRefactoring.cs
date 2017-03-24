// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace MonoDevelop.CSharp.CodeRefactorings
{
	/// <summary>
	/// Represents a set of transformations that can be applied to a piece of code.
	/// </summary>
	class CodeRefactoring  //: ICodeRefactoring
	{
		private readonly CodeRefactoringProvider _provider;
		private readonly IReadOnlyList<CodeAction> _actions;

		public CodeRefactoringProvider Provider
		{
			get { return _provider; }
		}

		/// <summary>
		/// List of possible actions that can be used to transform the code.
		/// </summary>
		public IEnumerable<CodeAction> Actions
		{
			get
			{
				return _actions;
			}
		}

		public CodeRefactoring(CodeRefactoringProvider provider, IEnumerable<CodeAction> actions)
		{
			_provider = provider;
			_actions = actions.ToImmutableArray();

			if (_actions.Count == 0)
			{
				throw new ArgumentException("Actions can not be empty", "actions");
			}
		}
	}
}
