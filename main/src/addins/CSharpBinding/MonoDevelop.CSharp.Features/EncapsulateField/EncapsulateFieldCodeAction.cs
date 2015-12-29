﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings.EncapsulateField
{
	class EncapsulateFieldCodeAction : CodeAction
	{
		private EncapsulateFieldResult _result;
		private string _title;

		public EncapsulateFieldCodeAction(EncapsulateFieldResult result, string title)
		{
			_result = result;
			_title = title;
		}

		public override string Title
		{
			get { return _title; }
		}

		protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
		{
			return _result.GetSolutionAsync(cancellationToken);
		}
	}
}
