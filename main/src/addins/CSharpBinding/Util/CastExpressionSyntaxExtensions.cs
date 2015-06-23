// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class CastExpressionSyntaxExtensions
	{
		readonly static MethodInfo isUnnecessaryCastMethod;

		static CastExpressionSyntaxExtensions ()
		{
			var typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CSharp.Extensions.CastExpressionSyntaxExtensions" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			isUnnecessaryCastMethod = typeInfo.GetMethod ("IsUnnecessaryCast", BindingFlags.Static | BindingFlags.Public);
		}

		public static bool IsUnnecessaryCast(this CastExpressionSyntax cast, SemanticModel semanticModel, CancellationToken cancellationToken)
		{
			try {
				return (bool)isUnnecessaryCastMethod.Invoke (null, new object[] { cast, semanticModel, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return false;
			}
		}
	}
}
