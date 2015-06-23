//
// CSharpFullyQualifyCodeFixProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeActions;
using System;
using RefactoringEssentials;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.CodeFixes.FullyQualify
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = "Fully Qualify")]
	[ExtensionOrder(After = PredefinedCodeFixProviderNames.AddUsingOrImport)]
	class CSharpFullyQualifyCodeFixProvider : CodeFixProvider
	{
		/// <summary>
		/// name does not exist in context
		/// </summary>
		private const string CS0103 = "CS0103";

		/// <summary>
		/// 'reference' is an ambiguous reference between 'identifier' and 'identifier'
		/// </summary>
		private const string CS0104 = "CS0104";

		/// <summary>
		/// type or namespace could not be found
		/// </summary>
		private const string CS0246 = "CS0246";

		/// <summary>
		/// wrong number of type args
		/// </summary>
		private const string CS0305 = "CS0305";

		/// <summary>
		/// The non-generic type 'A' cannot be used with type arguments
		/// </summary>
		private const string CS0308 = "CS0308";

		public override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS0103, CS0104, CS0246, CS0305, CS0308); }
		}

		protected bool IgnoreCase
		{
			get { return false; }
		}

		protected bool CanFullyQualify(Diagnostic diagnostic, ref SyntaxNode node)
		{
			var simpleName = node as SimpleNameSyntax;
			if (simpleName == null)
			{
				return false;
			}

			if (!simpleName.LooksLikeStandaloneTypeName())
			{
				return false;
			}

			if (!simpleName.CanBeReplacedWithAnyName())
			{
				return false;
			}

			return true;
		}

		protected SyntaxNode ReplaceNode(SyntaxNode node, string containerName, CancellationToken cancellationToken)
		{
			var simpleName = (SimpleNameSyntax)node;

			var leadingTrivia = simpleName.GetLeadingTrivia();
			var newName = simpleName.WithLeadingTrivia(SyntaxTriviaList.Empty);

			var qualifiedName = SyntaxFactory.QualifiedName(
				SyntaxFactory.ParseName(containerName), newName);

			qualifiedName = qualifiedName.WithLeadingTrivia(leadingTrivia);
			qualifiedName = qualifiedName.WithAdditionalAnnotations(Formatter.Annotation);

			var syntaxTree = simpleName.SyntaxTree;
			return syntaxTree.GetRoot(cancellationToken).ReplaceNode((NameSyntax)simpleName, qualifiedName);
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var cancellationToken = context.CancellationToken;

			var project = document.Project;
			var diagnostic = diagnostics.First();
			var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait (false);
			if (model.IsFromGeneratedCode (context.CancellationToken))
				return;
			
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var node = root.FindToken(span.Start).GetAncestors<SyntaxNode>().First(n => n.Span.Contains(span));

			// Has to be a simple identifier or generic name.
			if (node != null && CanFullyQualify(diagnostic, ref node))
			{
				var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

				var matchingTypes = await this.GetMatchingTypesAsync(project, semanticModel, node, cancellationToken).ConfigureAwait(false);
				var matchingNamespaces = await this.GetMatchingNamespacesAsync(project, semanticModel, node, cancellationToken).ConfigureAwait(false);

				if (matchingTypes != null || matchingNamespaces != null)
				{
					matchingTypes = matchingTypes ?? SpecializedCollections.EmptyEnumerable<ISymbol>();
					matchingNamespaces = matchingNamespaces ?? SpecializedCollections.EmptyEnumerable<ISymbol>();

					var matchingTypeContainers = FilterAndSort(GetContainers(matchingTypes, semanticModel.Compilation));
					var matchingNamespaceContainers = FilterAndSort(GetContainers(matchingNamespaces, semanticModel.Compilation));

					var proposedContainers =
						matchingTypeContainers.Concat(matchingNamespaceContainers)
							.Distinct()
							.Take(8);

					foreach (var container in proposedContainers)
					{
						var containerName = container.ToMinimalDisplayString(semanticModel, node.SpanStart);

						string name;
						int arity;
						node.GetNameAndArityOfSimpleName(out name, out arity);

						// Actual member name might differ by case.
						string memberName;
						if (this.IgnoreCase)
						{
							var member = container.GetMembers(name).FirstOrDefault();
							memberName = member != null ? member.Name : name;
						}
						else
						{
							memberName = name;
						}

						var codeAction = new DocumentChangeAction(
							node.Span,
							DiagnosticSeverity.Info,
							string.Format(GettextCatalog.GetString ("Change '{0}' to '{1}.{2}'"), name, containerName, memberName),
							(c) =>
							{
								var newRoot = this.ReplaceNode(node, containerName, c);
								return Task.FromResult(document.WithSyntaxRoot(newRoot));
							});

						context.RegisterCodeFix(codeAction, diagnostic);
					}
				}
			}
		}

		internal async Task<IEnumerable<ISymbol>> GetMatchingTypesAsync(
			Microsoft.CodeAnalysis.Project project, SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			// Can't be on the right hand side of binary expression (like 'dot').
			cancellationToken.ThrowIfCancellationRequested();
			string name;
			int arity;
			node.GetNameAndArityOfSimpleName(out name, out arity);

			var symbols = await SymbolFinder.FindDeclarationsAsync(project, name, this.IgnoreCase, SymbolFilter.Type, cancellationToken).ConfigureAwait(false);

			// also lookup type symbols with the "Attribute" suffix.
			var inAttributeContext = node.IsAttributeName();
			if (inAttributeContext)
			{
				symbols = symbols.Concat(
					await SymbolFinder.FindDeclarationsAsync(project, name + "Attribute", this.IgnoreCase, SymbolFilter.Type, cancellationToken).ConfigureAwait(false));
			}

			var accessibleTypeSymbols = symbols
				.OfType<INamedTypeSymbol>()
				.Where(s => (arity == 0 || s.GetArity() == arity)
					&& s.IsAccessibleWithin(semanticModel.Compilation.Assembly)
					&& (!inAttributeContext || s.IsAttribute())
					&& HasValidContainer(s))
				.ToList();

			return accessibleTypeSymbols;
		}

		private static bool HasValidContainer(ISymbol symbol)
		{
			var container = symbol.ContainingSymbol;
			return container is INamespaceSymbol ||
				(container is INamedTypeSymbol && !((INamedTypeSymbol)container).IsGenericType);
		}

		internal async Task<IEnumerable<ISymbol>> GetMatchingNamespacesAsync(
			Microsoft.CodeAnalysis.Project project,
			SemanticModel semanticModel,
			SyntaxNode simpleName,
			CancellationToken cancellationToken)
		{
			if (simpleName.IsAttributeName())
			{
				return null;
			}

			string name;
			int arity;
			simpleName.GetNameAndArityOfSimpleName(out name, out arity);
			if (cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			var symbols = await SymbolFinder.FindDeclarationsAsync(project, name, this.IgnoreCase, SymbolFilter.Namespace, cancellationToken).ConfigureAwait(false);

			var namespaces = symbols
				.OfType<INamespaceSymbol>()
				.Where(n => !n.IsGlobalNamespace &&
					HasAccessibleTypes(n, semanticModel, cancellationToken));

			return namespaces;
		}

		private bool HasAccessibleTypes(INamespaceSymbol @namespace, SemanticModel model, CancellationToken cancellationToken)
		{
			return Enumerable.Any(@namespace.GetAllTypes(cancellationToken), t => t.IsAccessibleWithin(model.Compilation.Assembly));
		}

		private static IEnumerable<INamespaceOrTypeSymbol> GetContainers(IEnumerable<ISymbol> symbols, Compilation compilation)
		{
			foreach (var symbol in symbols)
			{
				var containingSymbol = symbol.ContainingSymbol as INamespaceOrTypeSymbol;
				if (containingSymbol is INamespaceSymbol)
				{
					containingSymbol = compilation.GetCompilationNamespace((INamespaceSymbol)containingSymbol);
				}

				if (containingSymbol != null)
				{
					yield return containingSymbol;
				}
			}
		}

		private IEnumerable<INamespaceOrTypeSymbol> FilterAndSort(IEnumerable<INamespaceOrTypeSymbol> symbols)
		{
			symbols = symbols ?? SpecializedCollections.EmptyList<INamespaceOrTypeSymbol>();
			var list = symbols.Distinct ().Where<INamespaceOrTypeSymbol> (n => n is INamedTypeSymbol || !((INamespaceSymbol)n).IsGlobalNamespace).ToList ();
			list.Sort (this.Compare);
			return list;
		}

		private static readonly ConditionalWeakTable<INamespaceOrTypeSymbol, IList<string>> s_symbolToNameMap =
			new ConditionalWeakTable<INamespaceOrTypeSymbol, IList<string>>();
		private static readonly ConditionalWeakTable<INamespaceOrTypeSymbol, IList<string>>.CreateValueCallback s_getNameParts = GetNameParts;

		private static IList<string> GetNameParts(INamespaceOrTypeSymbol symbol)
		{
			return symbol.ToDisplayString(MonoDevelop.Ide.TypeSystem.Ambience.NameFormat).Split('.');
		}

		private int Compare(INamespaceOrTypeSymbol n1, INamespaceOrTypeSymbol n2)
		{
			if (n1 is INamedTypeSymbol && n2 is INamespaceSymbol)
			{
				return -1;
			}
			else if (n1 is INamespaceSymbol && n2 is INamedTypeSymbol)
			{
				return 1;
			}

			var names1 = s_symbolToNameMap.GetValue(n1, GetNameParts);
			var names2 = s_symbolToNameMap.GetValue(n2, GetNameParts);

			for (var i = 0; i < Math.Min(names1.Count, names2.Count); i++)
			{
				var comp = names1[i].CompareTo(names2[i]);
				if (comp != 0)
				{
					return comp;
				}
			}

			return names1.Count - names2.Count;
		}
	}
}