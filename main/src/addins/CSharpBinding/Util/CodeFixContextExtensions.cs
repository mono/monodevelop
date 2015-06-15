// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;


namespace ICSharpCode.NRefactory6.CSharp
{
	public static class CodeFixContextExtensions
	{
		/// <summary>
		/// Use this helper to register multiple fixes (<paramref name="actions"/>) each of which addresses / fixes the same supplied <paramref name="diagnostic"/>.
		/// </summary>
		public static void RegisterFixes(this CodeFixContext context, IEnumerable<CodeAction> actions, Diagnostic diagnostic)
		{
			foreach (var action in actions)
			{
				context.RegisterCodeFix(action, diagnostic);
			}
		}

		/// <summary>
		/// Use this helper to register multiple fixes (<paramref name="actions"/>) each of which addresses / fixes the same set of supplied <paramref name="diagnostics"/>.
		/// </summary>
		public static void RegisterFixes(this CodeFixContext context, IEnumerable<CodeAction> actions, ImmutableArray<Diagnostic> diagnostics)
		{
			foreach (var action in actions)
			{
				context.RegisterCodeFix(action, diagnostics);
			}
		}
	}

}

