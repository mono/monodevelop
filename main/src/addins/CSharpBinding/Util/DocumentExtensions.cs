// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.SemanticModelWorkspaceService;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static partial class DocumentExtensions
	{
		public static TLanguageService GetLanguageService<TLanguageService>(this Document document) where TLanguageService : class, ILanguageService
		{
			return document.Project.LanguageServices.GetService<TLanguageService>();
		}

		public static bool IsOpen(this Document document)
		{
			var workspace = document.Project.Solution.Workspace as Workspace;
			return workspace != null && workspace.IsDocumentOpen(document.Id);
		}

		/// <summary>
		/// this will return either regular semantic model or speculative semantic based on context. 
		/// any feature that is involved in typing or run on UI thread should use this to take advantage of speculative semantic model 
		/// whenever possible automatically.
		/// 
		/// when using this API, semantic model should only be used to ask node inside of the given span. 
		/// otherwise, it might throw if semantic model returned by this API is a speculative semantic model.
		/// 
		/// also, symbols from the semantic model returned by this API might have out of date location information. 
		/// if exact location (not relative location) is needed from symbol, regular GetSemanticModel should be used.
		/// </summary>
		public static async Task<SemanticModel> GetSemanticModelForSpanAsync(this Document document, TextSpan span, CancellationToken cancellationToken)
		{
//			var syntaxFactService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
//			var semanticModelService = document.Project.Solution.Workspace.Services.GetService<ISemanticModelService>();
//			if (semanticModelService == null || syntaxFactService == null)
//			{
				return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
//			}
//
//			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
//			var token = root.FindToken(span.Start);
//			if (token.Parent == null)
//			{
//				return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
//			}
//
//			var node = token.Parent.AncestorsAndSelf().FirstOrDefault(a => a.FullSpan.Contains(span));
//			return await GetSemanticModelForNodeAsync(semanticModelService, syntaxFactService, document, node, span, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// this will return either regular semantic model or speculative semantic based on context. 
		/// any feature that is involved in typing or run on UI thread should use this to take advantage of speculative semantic model 
		/// whenever possible automatically.
		/// 
		/// when using this API, semantic model should only be used to ask node inside of the given node except ones that belong to 
		/// member signature. otherwise, it might throw if semantic model returned by this API is a speculative semantic model.
		/// 
		/// also, symbols from the semantic model returned by this API might have out of date location information. 
		/// if exact location (not relative location) is needed from symbol, regular GetSemanticModel should be used.
		/// </summary>
		public static Task<SemanticModel> GetSemanticModelForNodeAsync(this Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
//			var syntaxFactService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
//			var semanticModelService = document.Project.Solution.Workspace.Services.GetService<ISemanticModelService>();
//			if (semanticModelService == null || syntaxFactService == null || node == null)
//			{
				return document.GetSemanticModelAsync(cancellationToken);
//			}
//
//			return GetSemanticModelForNodeAsync(semanticModelService, syntaxFactService, document, node, node.FullSpan, cancellationToken);
		}

		public static async Task<IEnumerable<T>> GetUnionResultsFromDocumentAndLinks<T>(
			this Document document,
			IEqualityComparer<T> comparer,
			Func<Document, CancellationToken, Task<IEnumerable<T>>> getItemsWorker,
			CancellationToken cancellationToken)
		{
			var linkedDocumentIds = document.GetLinkedDocumentIds();
			var itemsForCurrentContext = await getItemsWorker(document, cancellationToken).ConfigureAwait(false) ?? SpecializedCollections.EmptyEnumerable<T>();
			if (!linkedDocumentIds.Any())
			{
				return itemsForCurrentContext;
			}

			ISet<T> totalItems = itemsForCurrentContext.ToSet(comparer);
			foreach (var linkedDocumentId in linkedDocumentIds)
			{
				var linkedDocument = document.Project.Solution.GetDocument(linkedDocumentId);
				var items = await getItemsWorker(linkedDocument, cancellationToken).ConfigureAwait(false);
				if (items != null)
				{
					foreach (var item in items)
						totalItems.Add (item);
				}
			}

			return totalItems;
		}

		public static bool ShouldHideAdvancedMembers(this Document document)
		{
			return false;
//			var service = document.Project.Solution.Workspace.Services.GetService<IOptionService>();
//			return service.GetOption(CompletionOptions.HideAdvancedMembers, document.Project.Language);
		}

		public static async Task<Document> ReplaceNodeAsync<TNode>(this Document document, TNode oldNode, TNode newNode, CancellationToken cancellationToken) where TNode : SyntaxNode
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var newRoot = root.ReplaceNode(oldNode, newNode);
			return document.WithSyntaxRoot(newRoot);
		}

		public static async Task<Document> ReplaceNodesAsync(this Document document,
			IEnumerable<SyntaxNode> nodes,
			Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode,
			CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var newRoot = root.ReplaceNodes(nodes, computeReplacementNode);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
