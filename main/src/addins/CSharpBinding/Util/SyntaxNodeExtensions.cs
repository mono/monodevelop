//
// SyntaxNodeExtensions.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
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
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace ICSharpCode.NRefactory6.CSharp
{
	static partial class SyntaxNodeExtensions
	{
		public static IEnumerable<SyntaxNodeOrToken> DepthFirstTraversal(this SyntaxNode node)
		{
			return CommonSyntaxNodeOrTokenExtensions.DepthFirstTraversal(node);
		}

		public static IEnumerable<SyntaxNode> GetAncestors(this SyntaxNode node)
		{
			var current = node.Parent;

			while (current != null)
			{
				yield return current;

				current = current is IStructuredTriviaSyntax
					? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
					: current.Parent;
			}
		}

		public static IEnumerable<TNode> GetAncestors<TNode>(this SyntaxNode node)
			where TNode : SyntaxNode
		{
			var current = node.Parent;
			while (current != null)
			{
				if (current is TNode)
				{
					yield return (TNode)current;
				}

				current = current is IStructuredTriviaSyntax
					? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
					: current.Parent;
			}
		}

		public static TNode GetAncestor<TNode>(this SyntaxNode node)
			where TNode : SyntaxNode
		{
			if (node == null)
			{
				return default(TNode);
			}

			return node.GetAncestors<TNode>().FirstOrDefault();
		}

		public static TNode GetAncestorOrThis<TNode>(this SyntaxNode node)
			where TNode : SyntaxNode
		{
			if (node == null)
			{
				return default(TNode);
			}

			return node.GetAncestorsOrThis<TNode>().FirstOrDefault();
		}

		public static IEnumerable<TNode> GetAncestorsOrThis<TNode>(this SyntaxNode node)
			where TNode : SyntaxNode
		{
			var current = node;
			while (current != null)
			{
				if (current is TNode)
				{
					yield return (TNode)current;
				}

				current = current is IStructuredTriviaSyntax
					? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
					: current.Parent;
			}
		}

		public static bool HasAncestor<TNode>(this SyntaxNode node)
			where TNode : SyntaxNode
		{
			return node.GetAncestors<TNode>().Any();
		}

		public static IEnumerable<TSyntaxNode> Traverse<TSyntaxNode>(
			this SyntaxNode node, TextSpan searchSpan, Func<SyntaxNode, bool> predicate)
			where TSyntaxNode : SyntaxNode
		{
		//	Contract.ThrowIfNull(node);

			var nodes = new LinkedList<SyntaxNode>();
			nodes.AddFirst(node);

			while (nodes.Count > 0)
			{
				var currentNode = nodes.First.Value;
				nodes.RemoveFirst();

				if (currentNode != null && searchSpan.Contains(currentNode.FullSpan) && predicate(currentNode))
				{
					if (currentNode is TSyntaxNode)
					{
						yield return (TSyntaxNode)currentNode;
					}

					nodes.AddRangeAtHead(currentNode.ChildNodes());
				}
			}
		}

		public static bool CheckParent<T>(this SyntaxNode node, Func<T, bool> valueChecker) where T : SyntaxNode
		{
			if (node == null)
			{
				return false;
			}

			var parentNode = node.Parent as T;
			if (parentNode == null)
			{
				return false;
			}

			return valueChecker(parentNode);
		}

		/// <summary>
		/// Returns true if is a given token is a child token of of a certain type of parent node.
		/// </summary>
		/// <typeparam name="TParent">The type of the parent node.</typeparam>
		/// <param name="node">The node that we are testing.</param>
		/// <param name="childGetter">A function that, when given the parent node, returns the child token we are interested in.</param>
		public static bool IsChildNode<TParent>(this SyntaxNode node, Func<TParent, SyntaxNode> childGetter)
			where TParent : SyntaxNode
		{
			var ancestor = node.GetAncestor<TParent>();
			if (ancestor == null)
			{
				return false;
			}

			var ancestorNode = childGetter(ancestor);

			return node == ancestorNode;
		}

		/// <summary>
		/// Returns true if this node is found underneath the specified child in the given parent.
		/// </summary>
		public static bool IsFoundUnder<TParent>(this SyntaxNode node, Func<TParent, SyntaxNode> childGetter)
			where TParent : SyntaxNode
		{
			var ancestor = node.GetAncestor<TParent>();
			if (ancestor == null)
			{
				return false;
			}

			var child = childGetter(ancestor);

			// See if node passes through child on the way up to ancestor.
			return node.GetAncestorsOrThis<SyntaxNode>().Contains(child);
		}

		public static SyntaxNode GetCommonRoot(this SyntaxNode node1, SyntaxNode node2)
		{
			//Contract.ThrowIfTrue(node1.RawKind == 0 || node2.RawKind == 0);

			// find common starting node from two nodes.
			// as long as two nodes belong to same tree, there must be at least one common root (Ex, compilation unit)
			var ancestors = node1.GetAncestorsOrThis<SyntaxNode>();
			var set = new HashSet<SyntaxNode>(node2.GetAncestorsOrThis<SyntaxNode>());

			return ancestors.First(set.Contains);
		}

		public static int Width(this SyntaxNode node)
		{
			return node.Span.Length;
		}

		public static int FullWidth(this SyntaxNode node)
		{
			return node.FullSpan.Length;
		}

		public static SyntaxNode FindInnermostCommonNode(
			this IEnumerable<SyntaxNode> nodes,
			Func<SyntaxNode, bool> predicate)
		{
			IEnumerable<SyntaxNode> blocks = null;
			foreach (var node in nodes)
			{
				blocks = blocks == null
					? node.AncestorsAndSelf().Where(predicate)
					: blocks.Intersect(node.AncestorsAndSelf().Where(predicate));
			}

			return blocks == null ? null : blocks.First();
		}

		public static TSyntaxNode FindInnermostCommonNode<TSyntaxNode>(this IEnumerable<SyntaxNode> nodes)
			where TSyntaxNode : SyntaxNode
		{
			return (TSyntaxNode)nodes.FindInnermostCommonNode(n => n is TSyntaxNode);
		}

		/// <summary>
		/// create a new root node from the given root after adding annotations to the tokens
		/// 
		/// tokens should belong to the given root
		/// </summary>
		public static SyntaxNode AddAnnotations(this SyntaxNode root, IEnumerable<Tuple<SyntaxToken, SyntaxAnnotation>> pairs)
		{
//			Contract.ThrowIfNull(root);
//			Contract.ThrowIfNull(pairs);

			var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
			return root.ReplaceTokens(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
		}

		/// <summary>
		/// create a new root node from the given root after adding annotations to the nodes
		/// 
		/// nodes should belong to the given root
		/// </summary>
		public static SyntaxNode AddAnnotations(this SyntaxNode root, IEnumerable<Tuple<SyntaxNode, SyntaxAnnotation>> pairs)
		{
//			Contract.ThrowIfNull(root);
//			Contract.ThrowIfNull(pairs);

			var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
			return root.ReplaceNodes(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
		}

		public static TextSpan GetContainedSpan(this IEnumerable<SyntaxNode> nodes)
		{
//			Contract.ThrowIfNull(nodes);
//			Contract.ThrowIfFalse(nodes.Any());

			TextSpan fullSpan = nodes.First().Span;
			foreach (var node in nodes)
			{
				fullSpan = TextSpan.FromBounds(
					Math.Min(fullSpan.Start, node.SpanStart),
					Math.Max(fullSpan.End, node.Span.End));
			}

			return fullSpan;
		}

		public static IEnumerable<TextSpan> GetContiguousSpans(
			this IEnumerable<SyntaxNode> nodes, Func<SyntaxNode, SyntaxToken> getLastToken = null)
		{
			SyntaxNode lastNode = null;
			TextSpan? textSpan = null;
			foreach (var node in nodes)
			{
				if (lastNode == null)
				{
					textSpan = node.Span;
				}
				else
				{
					var lastToken = getLastToken == null
						? lastNode.GetLastToken()
						: getLastToken(lastNode);
					if (lastToken.GetNextToken(includeDirectives: true) == node.GetFirstToken())
					{
						// Expand the span
						textSpan = TextSpan.FromBounds(textSpan.Value.Start, node.Span.End);
					}
					else
					{
						// Return the last span, and start a new one
						yield return textSpan.Value;
						textSpan = node.Span;
					}
				}

				lastNode = node;
			}

			if (textSpan.HasValue)
			{
				yield return textSpan.Value;
			}
		}

		public static bool OverlapsHiddenPosition(this SyntaxNode node, CancellationToken cancellationToken)
		{
			return node.OverlapsHiddenPosition(node.Span, cancellationToken);
		}

		public static bool OverlapsHiddenPosition(this SyntaxNode node, TextSpan span, CancellationToken cancellationToken)
		{
			return node.SyntaxTree.OverlapsHiddenPosition(span, cancellationToken);
		}

		public static bool OverlapsHiddenPosition(this SyntaxNode declaration, SyntaxNode startNode, SyntaxNode endNode, CancellationToken cancellationToken)
		{
			var start = startNode.Span.End;
			var end = endNode.SpanStart;

			var textSpan = TextSpan.FromBounds(start, end);
			return declaration.OverlapsHiddenPosition(textSpan, cancellationToken);
		}

		public static IEnumerable<T> GetAnnotatedNodes<T>(this SyntaxNode node, SyntaxAnnotation syntaxAnnotation) where T : SyntaxNode
		{
			return node.GetAnnotatedNodesAndTokens(syntaxAnnotation).Select(n => n.AsNode()).OfType<T>();
		}

		/// <summary>
		/// Creates a new tree of nodes from the existing tree with the specified old nodes replaced with a newly computed nodes.
		/// </summary>
		/// <param name="root">The root of the tree that contains all the specified nodes.</param>
		/// <param name="nodes">The nodes from the tree to be replaced.</param>
		/// <param name="computeReplacementAsync">A function that computes a replacement node for
		/// the argument nodes. The first argument is one of the original specified nodes. The second argument is
		/// the same node possibly rewritten with replaced descendants.</param>
		/// <param name="cancellationToken"></param>
		public static Task<TRootNode> ReplaceNodesAsync<TRootNode>(
			this TRootNode root,
			IEnumerable<SyntaxNode> nodes,
			Func<SyntaxNode, SyntaxNode, CancellationToken, Task<SyntaxNode>> computeReplacementAsync,
			CancellationToken cancellationToken) where TRootNode : SyntaxNode
		{
			return root.ReplaceSyntaxAsync(
				nodes: nodes, computeReplacementNodeAsync: computeReplacementAsync,
				tokens: null, computeReplacementTokenAsync: null,
				trivia: null, computeReplacementTriviaAsync: null,
				cancellationToken: cancellationToken);
		}

//		/// <summary>
//		/// Creates a new tree of tokens from the existing tree with the specified old tokens replaced with a newly computed tokens.
//		/// </summary>
//		/// <param name="root">The root of the tree that contains all the specified tokens.</param>
//		/// <param name="tokens">The tokens from the tree to be replaced.</param>
//		/// <param name="computeReplacementAsync">A function that computes a replacement token for
//		/// the argument tokens. The first argument is one of the originally specified tokens. The second argument is
//		/// the same token possibly rewritten with replaced trivia.</param>
//		/// <param name="cancellationToken"></param>
//		public static Task<TRootNode> ReplaceTokensAsync<TRootNode>(
//			this TRootNode root,
//			IEnumerable<SyntaxToken> tokens,
//			Func<SyntaxToken, SyntaxToken, CancellationToken, Task<SyntaxToken>> computeReplacementAsync,
//			CancellationToken cancellationToken) where TRootNode : SyntaxNode
//		{
//			return root.ReplaceSyntaxAsync(
//				nodes: null, computeReplacementNodeAsync: null,
//				tokens: tokens, computeReplacementTokenAsync: computeReplacementAsync,
//				trivia: null, computeReplacementTriviaAsync: null,
//				cancellationToken: cancellationToken);
//		}
//
//		public static Task<TRoot> ReplaceTriviaAsync<TRoot>(
//			this TRoot root,
//			IEnumerable<SyntaxTrivia> trivia,
//			Func<SyntaxTrivia, SyntaxTrivia, CancellationToken, Task<SyntaxTrivia>> computeReplacementAsync,
//			CancellationToken cancellationToken) where TRoot : SyntaxNode
//		{
//			return root.ReplaceSyntaxAsync(
//				nodes: null, computeReplacementNodeAsync: null,
//				tokens: null, computeReplacementTokenAsync: null,
//				trivia: trivia, computeReplacementTriviaAsync: computeReplacementAsync,
//				cancellationToken: cancellationToken);
//		}

		public static async Task<TRoot> ReplaceSyntaxAsync<TRoot>(
			this TRoot root,
			IEnumerable<SyntaxNode> nodes,
			Func<SyntaxNode, SyntaxNode, CancellationToken, Task<SyntaxNode>> computeReplacementNodeAsync,
			IEnumerable<SyntaxToken> tokens,
			Func<SyntaxToken, SyntaxToken, CancellationToken, Task<SyntaxToken>> computeReplacementTokenAsync,
			IEnumerable<SyntaxTrivia> trivia,
			Func<SyntaxTrivia, SyntaxTrivia, CancellationToken, Task<SyntaxTrivia>> computeReplacementTriviaAsync,
			CancellationToken cancellationToken)
			where TRoot : SyntaxNode
		{
			// index all nodes, tokens and trivia by the full spans they cover
			var nodesToReplace = nodes != null ? nodes.ToDictionary(n => n.FullSpan) : new Dictionary<TextSpan, SyntaxNode>();
			var tokensToReplace = tokens != null ? tokens.ToDictionary(t => t.FullSpan) : new Dictionary<TextSpan, SyntaxToken>();
			var triviaToReplace = trivia != null ? trivia.ToDictionary(t => t.FullSpan) : new Dictionary<TextSpan, SyntaxTrivia>();

			var nodeReplacements = new Dictionary<SyntaxNode, SyntaxNode>();
			var tokenReplacements = new Dictionary<SyntaxToken, SyntaxToken>();
			var triviaReplacements = new Dictionary<SyntaxTrivia, SyntaxTrivia>();

			var retryAnnotations = new AnnotationTable<object>("RetryReplace");

			var spans = new List<TextSpan>(nodesToReplace.Count + tokensToReplace.Count + triviaToReplace.Count);
			spans.AddRange(nodesToReplace.Keys);
			spans.AddRange(tokensToReplace.Keys);
			spans.AddRange(triviaToReplace.Keys);

			while (spans.Count > 0)
			{
				// sort the spans of the items to be replaced so we can tell if any overlap
				spans.Sort((x, y) =>
					{
						// order by end offset, and then by length
						var d = x.End - y.End;

						if (d == 0)
						{
							d = x.Length - y.Length;
						}

						return d;
					});

				// compute replacements for all nodes that will go in the same batch
				// only spans that do not overlap go in the same batch.                
				TextSpan previous = default(TextSpan);
				foreach (var span in spans)
				{
					// only add to replacement map if we don't intersect with the previous node. This taken with the sort order
					// should ensure that parent nodes are not processed in the same batch as child nodes.
					if (previous == default(TextSpan) || !previous.IntersectsWith(span))
					{
						SyntaxNode currentNode;
						SyntaxToken currentToken;
						SyntaxTrivia currentTrivia;

						if (nodesToReplace.TryGetValue(span, out currentNode))
						{
							var original = (SyntaxNode)retryAnnotations.GetAnnotations(currentNode).SingleOrDefault() ?? currentNode;
							var newNode = await computeReplacementNodeAsync(original, currentNode, cancellationToken).ConfigureAwait(false);
							nodeReplacements[currentNode] = newNode;
						}
						else if (tokensToReplace.TryGetValue(span, out currentToken))
						{
							var original = (SyntaxToken)retryAnnotations.GetAnnotations(currentToken).SingleOrDefault();
							if (original == default(SyntaxToken))
							{
								original = currentToken;
							}

							var newToken = await computeReplacementTokenAsync(original, currentToken, cancellationToken).ConfigureAwait(false);
							tokenReplacements[currentToken] = newToken;
						}
						else if (triviaToReplace.TryGetValue(span, out currentTrivia))
						{
							var original = (SyntaxTrivia)retryAnnotations.GetAnnotations(currentTrivia).SingleOrDefault();
							if (original == default(SyntaxTrivia))
							{
								original = currentTrivia;
							}

							var newTrivia = await computeReplacementTriviaAsync(original, currentTrivia, cancellationToken).ConfigureAwait(false);
							triviaReplacements[currentTrivia] = newTrivia;
						}
					}

					previous = span;
				}

				bool retryNodes = false;
				bool retryTokens = false;
				bool retryTrivia = false;

				// replace nodes in batch
				// submit all nodes so we can annotate the ones we don't replace
				root = root.ReplaceSyntax(
					nodes: nodesToReplace.Values,
					computeReplacementNode: (original, rewritten) =>
					{
						SyntaxNode replaced;
						if (rewritten != original || !nodeReplacements.TryGetValue(original, out replaced))
						{
							// the subtree did change, or we didn't have a replacement for it in this batch
							// so we need to add an annotation so we can find this node again for the next batch.
							replaced = retryAnnotations.WithAdditionalAnnotations(rewritten, original);
							retryNodes = true;
						}

						return replaced;
					},
					tokens: tokensToReplace.Values,
					computeReplacementToken: (original, rewritten) =>
					{
						SyntaxToken replaced;
						if (rewritten != original || !tokenReplacements.TryGetValue(original, out replaced))
						{
							// the subtree did change, or we didn't have a replacement for it in this batch
							// so we need to add an annotation so we can find this node again for the next batch.
							replaced = retryAnnotations.WithAdditionalAnnotations(rewritten, original);
							retryTokens = true;
						}

						return replaced;
					},
					trivia: triviaToReplace.Values,
					computeReplacementTrivia: (original, rewritten) =>
					{
						SyntaxTrivia replaced;
						if (!triviaReplacements.TryGetValue(original, out replaced))
						{
							// the subtree did change, or we didn't have a replacement for it in this batch
							// so we need to add an annotation so we can find this node again for the next batch.
							replaced = retryAnnotations.WithAdditionalAnnotations(rewritten, original);
							retryTrivia = true;
						}

						return replaced;
					});

				nodesToReplace.Clear();
				tokensToReplace.Clear();
				triviaToReplace.Clear();
				spans.Clear();

				// prepare next batch out of all remaining annotated nodes
				if (retryNodes)
				{
					nodesToReplace = retryAnnotations.GetAnnotatedNodes(root).ToDictionary(n => n.FullSpan);
					spans.AddRange(nodesToReplace.Keys);
				}

				if (retryTokens)
				{
					tokensToReplace = retryAnnotations.GetAnnotatedTokens(root).ToDictionary(t => t.FullSpan);
					spans.AddRange(tokensToReplace.Keys);
				}

				if (retryTrivia)
				{
					triviaToReplace = retryAnnotations.GetAnnotatedTrivia(root).ToDictionary(t => t.FullSpan);
					spans.AddRange(triviaToReplace.Keys);
				}
			}

			return root;
		}


		public static bool IsKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2)
		{
			if (node == null)
			{
				return false;
			}

			var csharpKind = node.Kind();
			return csharpKind == kind1 || csharpKind == kind2;
		}

		public static bool IsKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
		{
			if (node == null)
			{
				return false;
			}

			var csharpKind = node.Kind();
			return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3;
		}

		public static bool IsKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4)
		{
			if (node == null)
			{
				return false;
			}

			var csharpKind = node.Kind();
			return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4;
		}

		public static bool IsKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5)
		{
			if (node == null)
			{
				return false;
			}

			var csharpKind = node.Kind();
			return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5;
		}

		/// <summary>
		/// Returns the list of using directives that affect <paramref name="node"/>. The list will be returned in
		/// top down order.  
		/// </summary>
		public static IEnumerable<UsingDirectiveSyntax> GetEnclosingUsingDirectives(this SyntaxNode node)
		{
			return node.GetAncestorOrThis<CompilationUnitSyntax>().Usings
				.Concat(node.GetAncestorsOrThis<NamespaceDeclarationSyntax>()
					.Reverse()
					.SelectMany(n => n.Usings));
		}

		public static bool IsUnsafeContext(this SyntaxNode node)
		{
			if (node.GetAncestor<UnsafeStatementSyntax>() != null)
			{
				return true;
			}

			return node.GetAncestors<MemberDeclarationSyntax>().Any(
				m => m.GetModifiers().Any(SyntaxKind.UnsafeKeyword));
		}

		public static bool IsInStaticContext(this SyntaxNode node)
		{
			// this/base calls are always static.
			if (node.FirstAncestorOrSelf<ConstructorInitializerSyntax>() != null)
			{
				return true;
			}

			var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
			if (memberDeclaration == null)
			{
				return false;
			}

			switch (memberDeclaration.Kind())
			{
			case SyntaxKind.MethodDeclaration:
			case SyntaxKind.ConstructorDeclaration:
			case SyntaxKind.PropertyDeclaration:
			case SyntaxKind.EventDeclaration:
			case SyntaxKind.IndexerDeclaration:
				return memberDeclaration.GetModifiers().Any(SyntaxKind.StaticKeyword);

			case SyntaxKind.FieldDeclaration:
				// Inside a field one can only access static members of a type.
				return true;

			case SyntaxKind.DestructorDeclaration:
				return false;
			}

			// Global statements are not a static context.
			if (node.FirstAncestorOrSelf<GlobalStatementSyntax>() != null)
			{
				return false;
			}

			// any other location is considered static
			return true;
		}

		public static NamespaceDeclarationSyntax GetInnermostNamespaceDeclarationWithUsings(this SyntaxNode contextNode)
		{
			var usingDirectiveAncsestor = contextNode.GetAncestor<UsingDirectiveSyntax>();
			if (usingDirectiveAncsestor == null)
			{
				return contextNode.GetAncestorsOrThis<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
			}
			else
			{
				// We are inside a using directive. In this case, we should find and return the first 'parent' namespace with usings.
				var containingNamespace = usingDirectiveAncsestor.GetAncestor<NamespaceDeclarationSyntax>();
				if (containingNamespace == null)
				{
					// We are inside a top level using directive (i.e. one that's directly in the compilation unit).
					return null;
				}
				else
				{
					return containingNamespace.GetAncestors<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
				}
			}
		}

		// Matches the following:
		//
		// (whitespace* newline)+ 
		private static readonly Matcher<SyntaxTrivia> s_oneOrMoreBlankLines;

		// Matches the following:
		// 
		// (whitespace* (single-comment|multi-comment) whitespace* newline)+ OneOrMoreBlankLines
		private static readonly Matcher<SyntaxTrivia> s_bannerMatcher;

		static SyntaxNodeExtensions()
		{
			var whitespace = Matcher.Repeat(Match(SyntaxKind.WhitespaceTrivia, "\\b"));
			var endOfLine = Match(SyntaxKind.EndOfLineTrivia, "\\n");
			var singleBlankLine = Matcher.Sequence(whitespace, endOfLine);

			var singleLineComment = Match(SyntaxKind.SingleLineCommentTrivia, "//");
			var multiLineComment = Match(SyntaxKind.MultiLineCommentTrivia, "/**/");
			var anyCommentMatcher = Matcher.Choice(singleLineComment, multiLineComment);

			var commentLine = Matcher.Sequence(whitespace, anyCommentMatcher, whitespace, endOfLine);

			s_oneOrMoreBlankLines = Matcher.OneOrMore(singleBlankLine);
			s_bannerMatcher =
				Matcher.Sequence(
					Matcher.OneOrMore(commentLine),
					s_oneOrMoreBlankLines);

			var typeInfo = Type.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.SyntaxNodeExtensions" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			containsInterleavedDirectiveMethod = typeInfo.GetMethod("ContainsInterleavedDirective", new[] { typeof(SyntaxNode), typeof (CancellationToken) });
		}

		private static Matcher<SyntaxTrivia> Match(SyntaxKind kind, string description)
		{
			return Matcher.Single<SyntaxTrivia>(t => t.Kind() == kind, description);
		}

		/// <summary>
		/// Returns all of the trivia to the left of this token up to the previous token (concatenates
		/// the previous token's trailing trivia and this token's leading trivia).
		/// </summary>
		public static IEnumerable<SyntaxTrivia> GetAllPrecedingTriviaToPreviousToken(this SyntaxToken token)
		{
			var prevToken = token.GetPreviousToken(includeSkipped: true);
			if (prevToken.Kind() == SyntaxKind.None)
			{
				return token.LeadingTrivia;
			}

			return prevToken.TrailingTrivia.Concat(token.LeadingTrivia);
		}

		public static bool IsBreakableConstruct(this SyntaxNode node)
		{
			switch (node.Kind())
			{
			case SyntaxKind.DoStatement:
			case SyntaxKind.WhileStatement:
			case SyntaxKind.SwitchStatement:
			case SyntaxKind.ForStatement:
			case SyntaxKind.ForEachStatement:
				return true;
			}

			return false;
		}

		public static bool IsContinuableConstruct(this SyntaxNode node)
		{
			switch (node.Kind())
			{
			case SyntaxKind.DoStatement:
			case SyntaxKind.WhileStatement:
			case SyntaxKind.ForStatement:
			case SyntaxKind.ForEachStatement:
				return true;
			}

			return false;
		}

		public static bool IsReturnableConstruct(this SyntaxNode node)
		{
			switch (node.Kind())
			{
			case SyntaxKind.AnonymousMethodExpression:
			case SyntaxKind.SimpleLambdaExpression:
			case SyntaxKind.ParenthesizedLambdaExpression:
			case SyntaxKind.MethodDeclaration:
			case SyntaxKind.ConstructorDeclaration:
			case SyntaxKind.DestructorDeclaration:
			case SyntaxKind.GetAccessorDeclaration:
			case SyntaxKind.SetAccessorDeclaration:
			case SyntaxKind.OperatorDeclaration:
			case SyntaxKind.AddAccessorDeclaration:
			case SyntaxKind.RemoveAccessorDeclaration:
				return true;
			}

			return false;
		}

		public static bool SpansPreprocessorDirective<TSyntaxNode>(
			this IEnumerable<TSyntaxNode> list)
			where TSyntaxNode : SyntaxNode
		{
			if (list == null || !list.Any())
			{
				return false;
			}

			var tokens = list.SelectMany(n => n.DescendantTokens());

			// todo: we need to dive into trivia here.
			return tokens.SpansPreprocessorDirective();
		}

		public static T WithPrependedLeadingTrivia<T>(
			this T node,
			params SyntaxTrivia[] trivia) where T : SyntaxNode
		{
			if (trivia.Length == 0)
			{
				return node;
			}

			return node.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
		}

		public static T WithPrependedLeadingTrivia<T>(
			this T node,
			SyntaxTriviaList trivia) where T : SyntaxNode
		{
			if (trivia.Count == 0)
			{
				return node;
			}

			return node.WithLeadingTrivia(trivia.Concat(node.GetLeadingTrivia()));
		}

		public static T WithPrependedLeadingTrivia<T>(
			this T node,
			IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
		{
			return node.WithPrependedLeadingTrivia(trivia.ToSyntaxTriviaList());
		}

		public static T WithAppendedTrailingTrivia<T>(
			this T node,
			params SyntaxTrivia[] trivia) where T : SyntaxNode
		{
			if (trivia.Length == 0)
			{
				return node;
			}

			return node.WithAppendedTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
		}

		public static T WithAppendedTrailingTrivia<T>(
			this T node,
			SyntaxTriviaList trivia) where T : SyntaxNode
		{
			if (trivia.Count == 0)
			{
				return node;
			}

			return node.WithTrailingTrivia(node.GetTrailingTrivia().Concat(trivia));
		}

		public static T WithAppendedTrailingTrivia<T>(
			this T node,
			IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
		{
			return node.WithAppendedTrailingTrivia(trivia.ToSyntaxTriviaList());
		}

		public static T With<T>(
			this T node,
			IEnumerable<SyntaxTrivia> leadingTrivia,
			IEnumerable<SyntaxTrivia> trailingTrivia) where T : SyntaxNode
		{
			return node.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
		}

		public static TNode ConvertToSingleLine<TNode>(this TNode node)
			where TNode : SyntaxNode
		{
			if (node == null)
			{
				return node;
			}

			var rewriter = new SingleLineRewriter();
			return (TNode)rewriter.Visit(node);
		}

		internal class SingleLineRewriter : CSharpSyntaxRewriter
        {
            private bool _lastTokenEndedInWhitespace;

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_lastTokenEndedInWhitespace)
                {
                    token = token.WithLeadingTrivia(Enumerable.Empty<SyntaxTrivia>());
                }
                else if (token.LeadingTrivia.Count > 0)
                {
                    token = token.WithLeadingTrivia(SyntaxFactory.Space);
                }

                if (token.TrailingTrivia.Count > 0)
                {
                    token = token.WithTrailingTrivia(SyntaxFactory.Space);
                    _lastTokenEndedInWhitespace = true;
                }
                else
                {
                    _lastTokenEndedInWhitespace = false;
                }

                return token;
            }
        }

		public static bool IsAnyArgumentList(this SyntaxNode node)
		{
			return node.IsKind(SyntaxKind.ArgumentList) ||
				node.IsKind(SyntaxKind.AttributeArgumentList) ||
				node.IsKind(SyntaxKind.BracketedArgumentList) ||
				node.IsKind(SyntaxKind.TypeArgumentList);
		}

		public static bool IsAnyLambda(this SyntaxNode node)
		{
			return
				node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) ||
				node.IsKind(SyntaxKind.SimpleLambdaExpression);
		}

		public static bool IsAnyLambdaOrAnonymousMethod(this SyntaxNode node)
		{
			return node.IsAnyLambda() || node.IsKind(SyntaxKind.AnonymousMethodExpression);
		}

		readonly static MethodInfo containsInterleavedDirectiveMethod;

		/// <summary>
		/// Returns true if the passed in node contains an interleaved pp directive.
		/// 
		/// i.e. The following returns false:
		/// 
		///   void Foo() {
		/// #if true
		/// #endif
		///   }
		/// 
		/// #if true
		///   void Foo() {
		///   }
		/// #endif
		/// 
		/// but these return true:
		/// 
		/// #if true
		///   void Foo() {
		/// #endif
		///   }
		/// 
		///   void Foo() {
		/// #if true
		///   }
		/// #endif
		/// 
		/// #if true
		///   void Foo() {
		/// #else
		///   }
		/// #endif
		/// 
		/// i.e. the method returns true if it contains a PP directive that belongs to a grouping
		/// constructs (like #if/#endif or #region/#endregion), but the grouping construct isn't
		/// entirely contained within the span of the node.
		/// </summary>
		public static bool ContainsInterleavedDirective(
			this SyntaxNode syntaxNode,
			CancellationToken cancellationToken)
		{
			return (bool)containsInterleavedDirectiveMethod.Invoke (null, new object[] { syntaxNode, cancellationToken });
		}


//		/// <summary>
//		/// Breaks up the list of provided nodes, based on how they are interspersed with pp
//		/// directives, into groups.  Within these groups nodes can be moved around safely, without
//		/// breaking any pp constructs.
//		/// </summary>
//		public static IList<IList<TSyntaxNode>> SplitNodesOnPreprocessorBoundaries<TSyntaxNode>(
//			this IEnumerable<TSyntaxNode> nodes,
//			CancellationToken cancellationToken)
//			where TSyntaxNode : SyntaxNode
//		{
//			var result = new List<IList<TSyntaxNode>>();
//
//			var currentGroup = new List<TSyntaxNode>();
//			foreach (var node in nodes)
//			{
//				var hasUnmatchedInteriorDirective = node.ContainsInterleavedDirective(cancellationToken);
//				var hasLeadingDirective = node.GetLeadingTrivia().Any(t => SyntaxFacts.IsPreprocessorDirective(t.Kind()));
//
//				if (hasUnmatchedInteriorDirective)
//				{
//					// we have a #if/#endif/#region/#endregion/#else/#elif in
//					// this node that belongs to a span of pp directives that
//					// is not entirely contained within the node.  i.e.:
//					//
//					//   void Foo() {
//					//      #if ...
//					//   }
//					//
//					// This node cannot be moved at all.  It is in a group that
//					// only contains itself (and thus can never be moved).
//
//					// add whatever group we've built up to now. And reset the 
//					// next group to empty.
//					result.Add(currentGroup);
//					currentGroup = new List<TSyntaxNode>();
//
//					result.Add(new List<TSyntaxNode> { node });
//				}
//				else if (hasLeadingDirective)
//				{
//					// We have a PP directive before us.  i.e.:
//					// 
//					//   #if ...
//					//      void Foo() {
//					//
//					// That means we start a new group that is contained between
//					// the above directive and the following directive.
//
//					// add whatever group we've built up to now. And reset the 
//					// next group to empty.
//					result.Add(currentGroup);
//					currentGroup = new List<TSyntaxNode>();
//
//					currentGroup.Add(node);
//				}
//				else
//				{
//					// simple case.  just add ourselves to the current group
//					currentGroup.Add(node);
//				}
//			}
//
//			// add the remainder of the final group.
//			result.Add(currentGroup);
//
//			// Now, filter out any empty groups.
//			result = result.Where(group => !group.IsEmpty()).ToList();
//			return result;
//		}
//
//		public static IEnumerable<SyntaxTrivia> GetLeadingBlankLines<TSyntaxNode>(
//			this TSyntaxNode node)
//			where TSyntaxNode : SyntaxNode
//		{
//			IEnumerable<SyntaxTrivia> blankLines;
//			node.GetNodeWithoutLeadingBlankLines(out blankLines);
//			return blankLines;
//		}
//
//		public static TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(
//			this TSyntaxNode node)
//			where TSyntaxNode : SyntaxNode
//		{
//			IEnumerable<SyntaxTrivia> blankLines;
//			return node.GetNodeWithoutLeadingBlankLines(out blankLines);
//		}
//
//		public static TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(
//			this TSyntaxNode node, out IEnumerable<SyntaxTrivia> strippedTrivia)
//			where TSyntaxNode : SyntaxNode
//		{
//			var leadingTriviaToKeep = new List<SyntaxTrivia>(node.GetLeadingTrivia());
//
//			var index = 0;
//			s_oneOrMoreBlankLines.TryMatch(leadingTriviaToKeep, ref index);
//
//			strippedTrivia = new List<SyntaxTrivia>(leadingTriviaToKeep.Take(index));
//
//			return node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index));
//		}

		public static IEnumerable<SyntaxTrivia> GetLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
			this TSyntaxNode node)
			where TSyntaxNode : SyntaxNode
		{
			IEnumerable<SyntaxTrivia> leadingTrivia;
			node.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(out leadingTrivia);
			return leadingTrivia;
		}

		public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
			this TSyntaxNode node)
			where TSyntaxNode : SyntaxNode
		{
			IEnumerable<SyntaxTrivia> strippedTrivia;
			return node.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(out strippedTrivia);
		}

		public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
			this TSyntaxNode node, out IEnumerable<SyntaxTrivia> strippedTrivia)
			where TSyntaxNode : SyntaxNode
		{
			var leadingTrivia = node.GetLeadingTrivia();

			// Rules for stripping trivia: 
			// 1) If there is a pp directive, then it (and all preceding trivia) *must* be stripped.
			//    This rule supersedes all other rules.
			// 2) If there is a doc comment, it cannot be stripped.  Even if there is a doc comment,
			//    followed by 5 new lines, then the doc comment still must stay with the node.  This
			//    rule does *not* supersede rule 1.
			// 3) Single line comments in a group (i.e. with no blank lines between them) belong to
			//    the node *iff* there is no blank line between it and the following trivia.

			List<SyntaxTrivia> leadingTriviaToStrip, leadingTriviaToKeep;

			int ppIndex = -1;
			for (int i = leadingTrivia.Count - 1; i >= 0; i--)
			{
				if (SyntaxFacts.IsPreprocessorDirective(leadingTrivia[i].Kind()))
				{
					ppIndex = i;
					break;
				}
			}

			if (ppIndex != -1)
			{
				// We have a pp directive.  it (and all all previous trivia) must be stripped.
				leadingTriviaToStrip = new List<SyntaxTrivia>(leadingTrivia.Take(ppIndex + 1));
				leadingTriviaToKeep = new List<SyntaxTrivia>(leadingTrivia.Skip(ppIndex + 1));
			}
			else
			{
				leadingTriviaToKeep = new List<SyntaxTrivia>(leadingTrivia);
				leadingTriviaToStrip = new List<SyntaxTrivia>();
			}

			// Now, consume as many banners as we can.
			var index = 0;
			while (
				s_oneOrMoreBlankLines.TryMatch(leadingTriviaToKeep, ref index) ||
				s_bannerMatcher.TryMatch(leadingTriviaToKeep, ref index))
			{
			}

			leadingTriviaToStrip.AddRange(leadingTriviaToKeep.Take(index));

			strippedTrivia = leadingTriviaToStrip;
			return node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index));
		}

		public static bool IsAnyAssignExpression(this SyntaxNode node)
		{
			return SyntaxFacts.IsAssignmentExpression(node.Kind());
		}

		public static bool IsCompoundAssignExpression(this SyntaxNode node)
		{
			switch (node.Kind())
			{
			case SyntaxKind.AddAssignmentExpression:
			case SyntaxKind.SubtractAssignmentExpression:
			case SyntaxKind.MultiplyAssignmentExpression:
			case SyntaxKind.DivideAssignmentExpression:
			case SyntaxKind.ModuloAssignmentExpression:
			case SyntaxKind.AndAssignmentExpression:
			case SyntaxKind.ExclusiveOrAssignmentExpression:
			case SyntaxKind.OrAssignmentExpression:
			case SyntaxKind.LeftShiftAssignmentExpression:
			case SyntaxKind.RightShiftAssignmentExpression:
				return true;
			}

			return false;
		}

		public static bool IsLeftSideOfAssignExpression(this SyntaxNode node)
		{
			return node.IsParentKind(SyntaxKind.SimpleAssignmentExpression) &&
				((AssignmentExpressionSyntax)node.Parent).Left == node;
		}

		public static bool IsLeftSideOfAnyAssignExpression(this SyntaxNode node)
		{
			return node.Parent.IsAnyAssignExpression() &&
				((AssignmentExpressionSyntax)node.Parent).Left == node;
		}

		public static bool IsRightSideOfAnyAssignExpression(this SyntaxNode node)
		{
			return node.Parent.IsAnyAssignExpression() &&
				((AssignmentExpressionSyntax)node.Parent).Right == node;
		}

		public static bool IsVariableDeclaratorValue(this SyntaxNode node)
		{
			return
				node.IsParentKind(SyntaxKind.EqualsValueClause) &&
				node.Parent.IsParentKind(SyntaxKind.VariableDeclarator) &&
				((EqualsValueClauseSyntax)node.Parent).Value == node;
		}

		public static BlockSyntax FindInnermostCommonBlock(this IEnumerable<SyntaxNode> nodes)
		{
			return nodes.FindInnermostCommonNode<BlockSyntax>();
		}

		public static IEnumerable<SyntaxNode> GetAncestorsOrThis(this SyntaxNode node, Func<SyntaxNode, bool> predicate)
		{
			var current = node;
			while (current != null)
			{
				if (predicate(current))
				{
					yield return current;
				}

				current = current.Parent;
			}
		}

		/// <summary>
		/// Look inside a trivia list for a skipped token that contains the given position.
		/// </summary>
		private static readonly Func<SyntaxTriviaList, int, SyntaxToken> s_findSkippedTokenForward =
			(l, p) => FindTokenHelper.FindSkippedTokenForward(GetSkippedTokens(l), p);

		private static IEnumerable<SyntaxToken> GetSkippedTokens(SyntaxTriviaList list)
		{
			return list.Where(trivia => trivia.RawKind == (int)SyntaxKind.SkippedTokensTrivia)
				.SelectMany(t => ((SkippedTokensTriviaSyntax)t.GetStructure()).Tokens);
		}

		/// <summary>
		/// If the position is inside of token, return that token; otherwise, return the token to the right.
		/// </summary>
		public static SyntaxToken FindTokenOnRightOfPosition(
			this SyntaxNode root,
			int position,
			bool includeSkipped = true,
			bool includeDirectives = false,
			bool includeDocumentationComments = false)
		{
			var skippedTokenFinder = includeSkipped ? s_findSkippedTokenForward : (Func<SyntaxTriviaList, int, SyntaxToken>)null;

			return FindTokenHelper.FindTokenOnRightOfPosition<CompilationUnitSyntax>(
				root, position, skippedTokenFinder, includeSkipped, includeDirectives, includeDocumentationComments);
		}

//		/// <summary>
//		/// If the position is inside of token, return that token; otherwise, return the token to the left.
//		/// </summary>
//		public static SyntaxToken FindTokenOnLeftOfPosition(
//			this SyntaxNode root,
//			int position,
//			bool includeSkipped = true,
//			bool includeDirectives = false,
//			bool includeDocumentationComments = false)
//		{
//			var skippedTokenFinder = includeSkipped ? s_findSkippedTokenBackward : (Func<SyntaxTriviaList, int, SyntaxToken>)null;
//
//			return FindTokenHelper.FindTokenOnLeftOfPosition<CompilationUnitSyntax>(
//				root, position, skippedTokenFinder, includeSkipped, includeDirectives, includeDocumentationComments);
//		}

		/// <summary>
		/// Returns child node or token that contains given position.
		/// </summary>
		/// <remarks>
		/// This is a copy of <see cref="SyntaxNode.ChildThatContainsPosition"/> that also returns the index of the child node.
		/// </remarks>
		internal static SyntaxNodeOrToken ChildThatContainsPosition(this SyntaxNode self, int position, out int childIndex)
		{
			var childList = self.ChildNodesAndTokens();

			int left = 0;
			int right = childList.Count - 1;

			while (left <= right)
			{
				int middle = left + ((right - left) / 2);
				SyntaxNodeOrToken node = childList.ElementAt(middle);

				var span = node.FullSpan;
				if (position < span.Start)
				{
					right = middle - 1;
				}
				else if (position >= span.End)
				{
					left = middle + 1;
				}
				else
				{
					childIndex = middle;
					return node;
				}
			}

			// we could check up front that index is within FullSpan,
			// but we wan to optimize for the common case where position is valid.
			Debug.Assert(!self.FullSpan.Contains(position), "Position is valid. How could we not find a child?");
			throw new ArgumentOutOfRangeException("position");
		}

		public static SyntaxNode GetParent(this SyntaxNode node)
		{
			return node != null ? node.Parent : null;
		}

		public static ValueTuple<SyntaxToken, SyntaxToken> GetBraces(this SyntaxNode node)
		{
			var namespaceNode = node as NamespaceDeclarationSyntax;
			if (namespaceNode != null)
			{
				return ValueTuple.Create(namespaceNode.OpenBraceToken, namespaceNode.CloseBraceToken);
			}

			var baseTypeNode = node as BaseTypeDeclarationSyntax;
			if (baseTypeNode != null)
			{
				return ValueTuple.Create(baseTypeNode.OpenBraceToken, baseTypeNode.CloseBraceToken);
			}

			var accessorListNode = node as AccessorListSyntax;
			if (accessorListNode != null)
			{
				return ValueTuple.Create(accessorListNode.OpenBraceToken, accessorListNode.CloseBraceToken);
			}

			var blockNode = node as BlockSyntax;
			if (blockNode != null)
			{
				return ValueTuple.Create(blockNode.OpenBraceToken, blockNode.CloseBraceToken);
			}

			var switchStatementNode = node as SwitchStatementSyntax;
			if (switchStatementNode != null)
			{
				return ValueTuple.Create(switchStatementNode.OpenBraceToken, switchStatementNode.CloseBraceToken);
			}

			var anonymousObjectCreationExpression = node as AnonymousObjectCreationExpressionSyntax;
			if (anonymousObjectCreationExpression != null)
			{
				return ValueTuple.Create(anonymousObjectCreationExpression.OpenBraceToken, anonymousObjectCreationExpression.CloseBraceToken);
			}

			var initializeExpressionNode = node as InitializerExpressionSyntax;
			if (initializeExpressionNode != null)
			{
				return ValueTuple.Create(initializeExpressionNode.OpenBraceToken, initializeExpressionNode.CloseBraceToken);
			}

			return new ValueTuple<SyntaxToken, SyntaxToken>();
		}

		public static ValueTuple<SyntaxToken, SyntaxToken> GetParentheses(this SyntaxNode node)
		{
			return node.TypeSwitch(
				(ParenthesizedExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(MakeRefExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(RefTypeExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(RefValueExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(CheckedExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(DefaultExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(TypeOfExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(SizeOfExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(ArgumentListSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(CastExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(WhileStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(DoStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(ForStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(ForEachStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(UsingStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(FixedStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(LockStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(IfStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(SwitchStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(CatchDeclarationSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(AttributeArgumentListSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(ConstructorConstraintSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(ParameterListSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
				(SyntaxNode n) => default(ValueTuple<SyntaxToken, SyntaxToken>));
		}

		public static ValueTuple<SyntaxToken, SyntaxToken> GetBrackets(this SyntaxNode node)
		{
			return node.TypeSwitch(
				(ArrayRankSpecifierSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
				(BracketedArgumentListSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
				(ImplicitArrayCreationExpressionSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
				(AttributeListSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
				(BracketedParameterListSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
				(SyntaxNode n) => default(ValueTuple<SyntaxToken, SyntaxToken>));
		}

		public static bool IsEmbeddedStatementOwner(this SyntaxNode node)
		{
			return node is IfStatementSyntax ||
				node is ElseClauseSyntax ||
				node is WhileStatementSyntax ||
				node is ForStatementSyntax ||
				node is ForEachStatementSyntax ||
				node is UsingStatementSyntax ||
				node is DoStatementSyntax;
		}

		public static StatementSyntax GetEmbeddedStatement(this SyntaxNode node)
		{
			return node.TypeSwitch(
				(IfStatementSyntax n) => n.Statement,
				(ElseClauseSyntax n) => n.Statement,
				(WhileStatementSyntax n) => n.Statement,
				(ForStatementSyntax n) => n.Statement,
				(ForEachStatementSyntax n) => n.Statement,
				(UsingStatementSyntax n) => n.Statement,
				(DoStatementSyntax n) => n.Statement,
				(SyntaxNode n) => null);
		}

		public static SyntaxTokenList GetModifiers(this SyntaxNode member)
		{
			if (member != null)
			{
				switch (member.Kind())
				{
				case SyntaxKind.EnumDeclaration:
					return ((EnumDeclarationSyntax)member).Modifiers;
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.InterfaceDeclaration:
				case SyntaxKind.StructDeclaration:
					return ((TypeDeclarationSyntax)member).Modifiers;
				case SyntaxKind.DelegateDeclaration:
					return ((DelegateDeclarationSyntax)member).Modifiers;
				case SyntaxKind.FieldDeclaration:
					return ((FieldDeclarationSyntax)member).Modifiers;
				case SyntaxKind.EventFieldDeclaration:
					return ((EventFieldDeclarationSyntax)member).Modifiers;
				case SyntaxKind.ConstructorDeclaration:
					return ((ConstructorDeclarationSyntax)member).Modifiers;
				case SyntaxKind.DestructorDeclaration:
					return ((DestructorDeclarationSyntax)member).Modifiers;
				case SyntaxKind.PropertyDeclaration:
					return ((PropertyDeclarationSyntax)member).Modifiers;
				case SyntaxKind.EventDeclaration:
					return ((EventDeclarationSyntax)member).Modifiers;
				case SyntaxKind.IndexerDeclaration:
					return ((IndexerDeclarationSyntax)member).Modifiers;
				case SyntaxKind.OperatorDeclaration:
					return ((OperatorDeclarationSyntax)member).Modifiers;
				case SyntaxKind.ConversionOperatorDeclaration:
					return ((ConversionOperatorDeclarationSyntax)member).Modifiers;
				case SyntaxKind.MethodDeclaration:
					return ((MethodDeclarationSyntax)member).Modifiers;
				case SyntaxKind.GetAccessorDeclaration:
				case SyntaxKind.SetAccessorDeclaration:
				case SyntaxKind.AddAccessorDeclaration:
				case SyntaxKind.RemoveAccessorDeclaration:
					return ((AccessorDeclarationSyntax)member).Modifiers;
				}
			}

			return default(SyntaxTokenList);
		}

		public static SyntaxNode WithModifiers(this SyntaxNode member, SyntaxTokenList modifiers)
		{
			if (member != null)
			{
				switch (member.Kind())
				{
				case SyntaxKind.EnumDeclaration:
					return ((EnumDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.InterfaceDeclaration:
				case SyntaxKind.StructDeclaration:
					return ((TypeDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.DelegateDeclaration:
					return ((DelegateDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.FieldDeclaration:
					return ((FieldDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.EventFieldDeclaration:
					return ((EventFieldDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.ConstructorDeclaration:
					return ((ConstructorDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.DestructorDeclaration:
					return ((DestructorDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.PropertyDeclaration:
					return ((PropertyDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.EventDeclaration:
					return ((EventDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.IndexerDeclaration:
					return ((IndexerDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.OperatorDeclaration:
					return ((OperatorDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.ConversionOperatorDeclaration:
					return ((ConversionOperatorDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.MethodDeclaration:
					return ((MethodDeclarationSyntax)member).WithModifiers(modifiers);
				case SyntaxKind.GetAccessorDeclaration:
				case SyntaxKind.SetAccessorDeclaration:
				case SyntaxKind.AddAccessorDeclaration:
				case SyntaxKind.RemoveAccessorDeclaration:
					return ((AccessorDeclarationSyntax)member).WithModifiers(modifiers);
				}
			}

			return null;
		}

		public static bool CheckTopLevel(this SyntaxNode node, TextSpan span)
		{
			var block = node as BlockSyntax;
			if (block != null)
			{
				return block.ContainsInBlockBody(span);
			}

			var field = node as FieldDeclarationSyntax;
			if (field != null)
			{
				foreach (var variable in field.Declaration.Variables)
				{
					if (variable.Initializer != null && variable.Initializer.Span.Contains(span))
					{
						return true;
					}
				}
			}

			var global = node as GlobalStatementSyntax;
			if (global != null)
			{
				return true;
			}

			var constructorInitializer = node as ConstructorInitializerSyntax;
			if (constructorInitializer != null)
			{
				return constructorInitializer.ContainsInArgument(span);
			}

			return false;
		}

		public static bool ContainsInArgument(this ConstructorInitializerSyntax initializer, TextSpan textSpan)
		{
			if (initializer == null)
			{
				return false;
			}

			return initializer.ArgumentList.Arguments.Any(a => a.Span.Contains(textSpan));
		}

		public static bool ContainsInBlockBody(this BlockSyntax block, TextSpan textSpan)
		{
			if (block == null)
			{
				return false;
			}

			var blockSpan = TextSpan.FromBounds(block.OpenBraceToken.Span.End, block.CloseBraceToken.SpanStart);
			return blockSpan.Contains(textSpan);
		}

		public static IEnumerable<MemberDeclarationSyntax> GetMembers(this SyntaxNode node)
		{
			var compilation = node as CompilationUnitSyntax;
			if (compilation != null)
			{
				return compilation.Members;
			}

			var @namespace = node as NamespaceDeclarationSyntax;
			if (@namespace != null)
			{
				return @namespace.Members;
			}

			var type = node as TypeDeclarationSyntax;
			if (type != null)
			{
				return type.Members;
			}

			var @enum = node as EnumDeclarationSyntax;
			if (@enum != null)
			{
				return @enum.Members;
			}

			return SpecializedCollections.EmptyEnumerable<MemberDeclarationSyntax>();
		}

		public static IEnumerable<SyntaxNode> GetBodies(this SyntaxNode node)
		{
			var constructor = node as ConstructorDeclarationSyntax;
			if (constructor != null)
			{
				var result = SpecializedCollections.SingletonEnumerable<SyntaxNode>(constructor.Body).WhereNotNull();
				var initializer = constructor.Initializer;
				if (initializer != null)
				{
					result = result.Concat(initializer.ArgumentList.Arguments.Select(a => (SyntaxNode)a.Expression).WhereNotNull());
				}

				return result;
			}

			var method = node as BaseMethodDeclarationSyntax;
			if (method != null)
			{
				return SpecializedCollections.SingletonEnumerable<SyntaxNode>(method.Body).WhereNotNull();
			}

			var property = node as BasePropertyDeclarationSyntax;
			if (property != null)
			{
				return property.AccessorList.Accessors.Select(a => a.Body).WhereNotNull();
			}

			var @enum = node as EnumMemberDeclarationSyntax;
			if (@enum != null)
			{
				if (@enum.EqualsValue != null)
				{
					return SpecializedCollections.SingletonEnumerable(@enum.EqualsValue.Value).WhereNotNull();
				}
			}

			var field = node as BaseFieldDeclarationSyntax;
			if (field != null)
			{
				return field.Declaration.Variables.Where(v => v.Initializer != null).Select(v => v.Initializer.Value).WhereNotNull();
			}

			return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
		}

		public static ConditionalAccessExpressionSyntax GetParentConditionalAccessExpression(this SyntaxNode node)
		{
			var parent = node.Parent;
			while (parent != null)
			{
				// Because the syntax for conditional access is right associate, we cannot
				// simply take the first ancestor ConditionalAccessExpression. Instead, we 
				// must walk upward until we find the ConditionalAccessExpression whose
				// OperatorToken appears left of the MemberBinding.
				if (parent.IsKind(SyntaxKind.ConditionalAccessExpression) &&
					((ConditionalAccessExpressionSyntax)parent).OperatorToken.Span.End <= node.SpanStart)
				{
					return (ConditionalAccessExpressionSyntax)parent;
				}

				parent = parent.Parent;
			}

			return null;
		}

		public static bool IsDelegateOrConstructorOrMethodParameterList(this SyntaxNode node)
		{
			if (!node.IsKind(SyntaxKind.ParameterList))
			{
				return false;
			}

			return
				node.IsParentKind(SyntaxKind.MethodDeclaration) ||
				node.IsParentKind(SyntaxKind.ConstructorDeclaration) ||
				node.IsParentKind(SyntaxKind.DelegateDeclaration);
		}

		public static ConditionalAccessExpressionSyntax GetInnerMostConditionalAccessExpression(this SyntaxNode node)
		{
			if (!(node is ConditionalAccessExpressionSyntax))
			{
				return null;
			}

			var result = (ConditionalAccessExpressionSyntax)node;
			while (result.WhenNotNull is ConditionalAccessExpressionSyntax)
			{
				result = (ConditionalAccessExpressionSyntax)result.WhenNotNull;
			}

			return result;
		}
	}
}
