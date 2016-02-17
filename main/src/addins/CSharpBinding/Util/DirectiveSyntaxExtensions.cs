// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.NRefactory6.CSharp
{
	internal static partial class DirectiveSyntaxExtensions
	{
		readonly static MethodInfo getMatchingDirective;
		readonly static MethodInfo getMatchingConditionalDirectives;

		static DirectiveSyntaxExtensions()
		{
			var typeInfo = Type.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.DirectiveSyntaxExtensions" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			if (typeInfo == null)
				throw new InvalidOperationException ("DirectiveSyntaxExtensions not found.");
			getMatchingDirective = typeInfo.GetMethod("GetMatchingDirective", BindingFlags.NonPublic | BindingFlags.Static);
			if (getMatchingDirective == null)
				throw new InvalidOperationException ("GetMatchingDirective not found.");
			getMatchingConditionalDirectives = typeInfo.GetMethod("GetMatchingConditionalDirectives", BindingFlags.NonPublic | BindingFlags.Static);
			if (getMatchingDirective == null)
				throw new InvalidOperationException ("GetMatchingConditionalDirectives not found.");
		}



		internal static DirectiveTriviaSyntax GetMatchingDirective(this DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
		{
			try {
				return (DirectiveTriviaSyntax)getMatchingDirective.Invoke(null, new object[] { directive, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		internal static IReadOnlyList<DirectiveTriviaSyntax> GetMatchingConditionalDirectives(this DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
		{
			try {
				return (IReadOnlyList<DirectiveTriviaSyntax>)getMatchingConditionalDirectives.Invoke(null, new object[] { directive, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}
	}
}