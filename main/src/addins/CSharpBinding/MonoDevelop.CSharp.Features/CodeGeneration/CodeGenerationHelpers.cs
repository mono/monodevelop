// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.CodeGeneration
{
	#if NR6
	public
	#endif
	static class CodeGenerationHelpers
	{
		public static SyntaxNode GenerateThrowStatement(
			SyntaxGenerator factory,
			SemanticDocument document,
			string exceptionMetadataName,
			CancellationToken cancellationToken)
		{
			var compilation = document.SemanticModel.Compilation;
			var exceptionType = compilation.GetTypeByMetadataName(exceptionMetadataName);

			// If we can't find the Exception, we obviously can't generate anything.
			if (exceptionType == null)
			{
				return null;
			}

			var exceptionCreationExpression = factory.ObjectCreationExpression(
				exceptionType,
				SpecializedCollections.EmptyList<SyntaxNode>());

			return factory.ThrowStatement(exceptionCreationExpression);
		}

	}
}
