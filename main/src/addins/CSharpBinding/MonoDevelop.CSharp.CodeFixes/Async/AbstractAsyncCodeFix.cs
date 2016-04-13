//
// AbstractAsyncCodeFix.cs
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

using System.Collections.Immutable;
using System.Linq;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CodeActions;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.CSharp.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RefactoringEssentials;

namespace MonoDevelop.CSharp.CodeFixes
{

	internal abstract partial class AbstractAsyncCodeFix : CodeFixProvider
	{
		protected abstract Task<CodeAction> GetCodeFix(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostic, CancellationToken cancellationToken);

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait (false);
			if (model.IsFromGeneratedCode (context.CancellationToken))
				return;
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			SyntaxNode node;
			if (!TryGetNode(root, context.Span, out node))
			{
				return;
			}

			var diagnostic = context.Diagnostics.FirstOrDefault();

			var codeAction = await GetCodeFix(root, node, context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);

			if (codeAction != null)
			{
				context.RegisterCodeFix(codeAction, diagnostic);
			}
		}

		private bool TryGetNode(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span, out SyntaxNode node)
		{
			node = null;
			var ancestors = root.FindToken(span.Start).GetAncestors<SyntaxNode>();
			if (!ancestors.Any())
			{
				return false;
			}

			node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root);
			return node != null;
		}
	}
	
}
