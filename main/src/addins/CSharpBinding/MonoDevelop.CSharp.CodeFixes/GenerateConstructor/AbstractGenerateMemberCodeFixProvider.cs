//
// AbstractGenerateMemberCodeFixProvider.cs
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using ICSharpCode.NRefactory6.CSharp;
using RefactoringEssentials;

namespace MonoDevelop.CSharp.CodeFixes.GenerateConstructor
{
	internal abstract class AbstractGenerateMemberCodeFixProvider : CodeFixProvider
	{
		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			// NOTE(DustinCa): Not supported in REPL for now.
			if (context.Document.SourceCodeKind == SourceCodeKind.Interactive)
			{
				return;
			}
			var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait (false);
			if (model.IsFromGeneratedCode (context.CancellationToken))
				return;

			var root = await model.SyntaxTree.GetRootAsync (context.CancellationToken).ConfigureAwait (false);
			var names = GetTargetNodes(root, context.Span);
			foreach (var name in names)
			{
				var codeActions = await GetCodeActionsAsync(context.Document, name, context.CancellationToken).ConfigureAwait(false);
				if (codeActions == null)
				{
					continue;
				}
				foreach (var act in codeActions)
					context.RegisterCodeFix (act, context.Diagnostics);
				return;
			}
		}

		protected abstract Task<IEnumerable<CodeAction>> GetCodeActionsAsync(Document document, SyntaxNode node, CancellationToken cancellationToken);

		protected virtual SyntaxNode GetTargetNode(SyntaxNode node)
		{
			return node;
		}

		protected virtual bool IsCandidate(SyntaxNode node)
		{
			return false;
		}

		protected virtual IEnumerable<SyntaxNode> GetTargetNodes(SyntaxNode root, TextSpan span)
		{
			var token = root.FindToken(span.Start);
			if (!token.Span.IntersectsWith(span))
			{
				yield break;
			}

			var nodes = token.GetAncestors<SyntaxNode>().Where(IsCandidate);
			foreach (var node in nodes)
			{
				var name = GetTargetNode(node);

				if (name != null)
				{
					yield return name;
				}
			}
		}
	}
	
}
