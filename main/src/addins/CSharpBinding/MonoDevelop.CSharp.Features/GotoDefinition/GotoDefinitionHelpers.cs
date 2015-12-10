// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.Features.GotoDefinition
{
	static class GoToDefinitionHelpers
	{
		
		public static bool TryGoToDefinition(
			ISymbol symbol,
			Project project,
			ITypeSymbol containingTypeSymbol,
			bool throwOnHiddenDefinition,
			CancellationToken cancellationToken)
		{
			var alias = symbol as IAliasSymbol;
			if (alias != null)
			{
				var ns = alias.Target as INamespaceSymbol;
				if (ns != null && ns.IsGlobalNamespace)
				{
					return false;
				}
			}

			// VB global import aliases have a synthesized SyntaxTree.
			// We can't go to the definition of the alias, so use the target type.

			var solution = project.Solution;
			if (symbol is IAliasSymbol &&
				GeneratedCodeRecognitionService.GetPreferredSourceLocations(solution, symbol).All(l => project.Solution.GetDocument(l.SourceTree) == null))
			{
				symbol = ((IAliasSymbol)symbol).Target;
			}

			var definition = SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).Result;
			cancellationToken.ThrowIfCancellationRequested();

			symbol = definition ?? symbol;

			if (TryThirdPartyNavigation(symbol, solution, containingTypeSymbol))
			{
				return true;
			}

			// If it is a partial method declaration with no body, choose to go to the implementation
			// that has a method body.
			if (symbol is IMethodSymbol)
			{
				symbol = ((IMethodSymbol)symbol).PartialImplementationPart ?? symbol;
			}

			var preferredSourceLocations = GeneratedCodeRecognitionService.GetPreferredSourceLocations(solution, symbol).ToArray();
			if (!preferredSourceLocations.Any())
			{
				// If there are no visible source locations, then tell the host about the symbol and 
				// allow it to navigate to it.  THis will either navigate to any non-visible source
				// locations, or it can appropriately deal with metadata symbols for hosts that can go 
				// to a metadata-as-source view.
				return GoToDefinitionService.TryNavigateToSymbol(symbol, project, true);
			}

			// If we have a single location, then just navigate to it.
			if (preferredSourceLocations.Length == 1)
			{
				var firstItem = preferredSourceLocations[0];
				var workspace = project.Solution.Workspace;
				if (GoToDefinitionService.CanNavigateToSpan(workspace, solution.GetDocument(firstItem.SourceTree).Id, firstItem.SourceSpan))
				{
					return GoToDefinitionService.TryNavigateToSpan(workspace, solution.GetDocument(firstItem.SourceTree).Id, firstItem.SourceSpan, true);
				}
				else
				{
					if (throwOnHiddenDefinition)
					{
						const int E_FAIL = -2147467259;
						throw new COMException("The definition of the object is hidden.", E_FAIL);
					}
					else
					{
						return false;
					}
				}
			}
			else
			{
				// We have multiple viable source locations, so ask the host what to do. Most hosts
				// will simply display the results to the user and allow them to choose where to 
				// go.
				GoToDefinitionService.DisplayMultiple (preferredSourceLocations.Select (location => Tuple.Create (solution, symbol, location)).ToList ());

				return false;
			}
		}

		private static bool TryThirdPartyNavigation(ISymbol symbol, Solution solution, ITypeSymbol containingTypeSymbol)
		{
			// Allow third parties to navigate to all symbols except types/constructors
			// if we are navigating from the corresponding type.

			if (containingTypeSymbol != null &&
				(symbol is ITypeSymbol || symbol.IsConstructor()))
			{
				var candidateTypeSymbol = symbol is ITypeSymbol
					? symbol
					: symbol.ContainingType;

				if (containingTypeSymbol == candidateTypeSymbol)
				{
					// We are navigating from the same type, so don't allow third parties to perform the navigation.
					// This ensures that if we navigate to a class from within that class, we'll stay in the same file
					// rather than navigate to, say, XAML.
					return false;
				}
			}

			// Notify of navigation so third parties can intercept the navigation
			return GoToDefinitionService.TrySymbolNavigationNotify(symbol, solution);
		}
	}
}
