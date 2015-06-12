// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings.EncapsulateField
{
	public class EncapsulateFieldResult
	{
		private readonly Func<CancellationToken, Task<AbstractEncapsulateFieldService.Result>> _resultGetter;

		public EncapsulateFieldResult(Func<CancellationToken, Task<AbstractEncapsulateFieldService.Result>> resultGetter)
		{
			_resultGetter = resultGetter;
		}

		public async Task<string> GetNameAsync(CancellationToken cancellationToken)
		{
			var result = await _resultGetter(cancellationToken).ConfigureAwait(false);
			return result.Name;
		}
//
//		public async Task<Glyph> GetGlyphAsync(CancellationToken cancellationToken)
//		{
//			var result = await _resultGetter(cancellationToken).ConfigureAwait(false);
//			return result.Glyph;
//		}

		public async Task<Solution> GetSolutionAsync(CancellationToken cancellationToken)
		{
			var result = await _resultGetter(cancellationToken).ConfigureAwait(false);
			return result.Solution;
		}
	}
}
