// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.CSharp.Navigation;

namespace ICSharpCode.NRefactory6.CSharp.Features.GotoDefinition
{
	static class GoToDefinitionService
	{
		/// <summary>
		/// Navigate to the first source location of a given symbol.
		/// bool TryNavigateToSymbol(ISymbol symbol, Project project, bool usePreviewTab = false);
		/// </summary>
		public static Func<ISymbol, Project, bool, bool> TryNavigateToSymbol = delegate {
			return true;
		};

		/// <summary>
		/// Navigates to the given position in the specified document, opening it if necessary.
		/// bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool usePreviewTab = false);
		/// </summary>
		public static Func<Workspace, DocumentId, TextSpan, bool, bool> TryNavigateToSpan = delegate {
			return true;
		};

		/// <summary>
		/// Determines whether it is possible to navigate to the given position in the specified document.
		/// bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan);
		/// </summary>
		public static Func<Workspace, DocumentId, TextSpan, bool> CanNavigateToSpan = delegate {
			return true;
		};

		public static Action<IEnumerable<Tuple<Solution, ISymbol, Location>>> DisplayMultiple = delegate {
		};

		/// <summary>
		/// bool TrySymbolNavigationNotify(ISymbol symbol, Solution solution);
		/// </summary>
		/// <returns>True if the navigation was handled, indicating that the caller should not 
		/// perform the navigation.
		/// 
		/// </returns>
		public static Func<ISymbol, Solution, bool> TrySymbolNavigationNotify = delegate {
			return false;
		};

		static ISymbol FindRelatedExplicitlyDeclaredSymbol (ISymbol symbol, Compilation compilation)
		{
			return symbol;
		}

		public static async Task<ISymbol> FindSymbolAsync (Document document, int position, CancellationToken cancellationToken)
		{
			var workspace = document.Project.Solution.Workspace;

			var semanticModel = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			//var symbol = SymbolFinder.FindSymbolAtPosition(semanticModel, position, workspace, bindLiteralsToUnderlyingType: true, cancellationToken: cancellationToken);
			var symbol = await SymbolFinder.FindSymbolAtPositionAsync (semanticModel, position, workspace, cancellationToken: cancellationToken).ConfigureAwait (false);

			return FindRelatedExplicitlyDeclaredSymbol (symbol, semanticModel.Compilation);
		}

		//		public async Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
		//		{
		//			var symbol = await FindSymbolAsync(document, position, cancellationToken).ConfigureAwait(false);
		//
		//			// realize the list here so that the consumer await'ing the result doesn't lazily cause
		//			// them to be created on an inappropriate thread.
		//			return NavigableItemFactory.GetItemsfromPreferredSourceLocations(document.Project.Solution, symbol).ToList();
		//		}

		public static bool TryGoToDefinition (Document document, int position, CancellationToken cancellationToken)
		{
			var metadata = Counters.CreateNavigateToMetadata ("Declaration");

			using (var timer = Counters.NavigateTo.BeginTiming (metadata)) {
				try {
					bool result = TryGoToDefinitionInternal (document, position, cancellationToken);
					Counters.UpdateNavigateResult (metadata, result);
					return result;
				} finally {
					Counters.UpdateUserCancellation (metadata, cancellationToken);
				}
			}
		}

		static bool TryGoToDefinitionInternal (Document document, int position, CancellationToken cancellationToken)
		{
			var symbol = FindSymbolAsync (document, position, cancellationToken).Result;

			if (symbol != null) {
				var containingTypeSymbol = GetContainingTypeSymbol (position, document, cancellationToken);

				if (GoToDefinitionHelpers.TryGoToDefinition (symbol, document.Project, containingTypeSymbol, throwOnHiddenDefinition: true, cancellationToken: cancellationToken)) {
					return true;
				}
			}

			return false;
		}

		private static ITypeSymbol GetContainingTypeSymbol (int caretPosition, Document document, CancellationToken cancellationToken)
		{
			var syntaxRoot = document.GetSyntaxRootAsync (cancellationToken).Result;
			var containingTypeDeclaration = CSharpSyntaxFactsService.Instance.GetContainingTypeDeclaration (syntaxRoot, caretPosition);

			if (containingTypeDeclaration != null) {
				var semanticModel = document.GetSemanticModelAsync (cancellationToken).Result;
				return semanticModel.GetDeclaredSymbol (containingTypeDeclaration, cancellationToken) as ITypeSymbol;
			}

			return null;
		}
	}
}
