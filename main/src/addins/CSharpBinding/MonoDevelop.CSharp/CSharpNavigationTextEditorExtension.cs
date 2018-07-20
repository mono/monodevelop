//
// CSharpNavigationTextEditorExtension.cs
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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using System.Linq;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp
{
	class CSharpNavigationTextEditorExtension : AbstractNavigationExtension
	{
		static List<NavigationSegment> emptyList = new List<NavigationSegment> ();

		protected override async Task<IEnumerable<NavigationSegment>> RequestLinksAsync (int offset, int length, CancellationToken token)
		{
			var analysisDocument = DocumentContext.AnalysisDocument;
			if (analysisDocument == null)
				return Enumerable.Empty<NavigationSegment> ();
			var model = await analysisDocument.GetSemanticModelAsync(token);
			if (model == null)
				return Enumerable.Empty<NavigationSegment> ();
			return await Task.Run (async delegate {
				try {
					var visitor = new NavigationVisitor (DocumentContext, model, new TextSpan (offset, length), token);
					visitor.Visit (await model.SyntaxTree.GetRootAsync (token).ConfigureAwait (false));
					return (IEnumerable<NavigationSegment>)visitor.result;
				} catch (OperationCanceledException) {
					return (IEnumerable<NavigationSegment>)emptyList;
				}
			});
		}

		class NavigationVisitor : CSharpSyntaxWalker
		{
			SemanticModel model;
			internal List<NavigationSegment> result = new List<NavigationSegment> ();

			TextSpan region;
			DocumentContext documentContext;
			CancellationToken token;

			public NavigationVisitor (DocumentContext documentContext, SemanticModel model, TextSpan region, CancellationToken token)
			{
				this.documentContext = documentContext;
				this.model = model;
				this.region = region;
				this.token = token;
			}

			public override void VisitCompilationUnit (CompilationUnitSyntax node)
			{
				var startNode = node.DescendantNodesAndSelf (n => region.Start <= n.SpanStart).FirstOrDefault ();
				if (startNode == node || startNode == null) {
					base.VisitCompilationUnit (node);
				} else {
					this.Visit (startNode);
				}
			}

			public override void Visit (SyntaxNode node)
			{
				if (node.Span.End < region.Start)
					return;
				if (node.Span.Start > region.End)
					return;
				base.Visit(node);
			}

			public override void VisitIdentifierName (IdentifierNameSyntax node)
			{
				var info = model.GetSymbolInfo (node);
				if (IsNavigatable (info)) {
					result.Add (new NavigationSegment (node.Span.Start, node.Span.Length, delegate { 
						GLib.Timeout.Add (50, delegate {
							RefactoringService.RoslynJumpToDeclaration (info.Symbol, documentContext.Project);
							return false;
						});
					}));
				}
			}

			static bool IsNavigatable (SymbolInfo info)
			{
				return info.Symbol != null && info.Symbol.Kind != SymbolKind.Namespace;
			}
			 
			public override void VisitBlock (BlockSyntax node)
			{
				token.ThrowIfCancellationRequested ();
				base.VisitBlock (node);
			}
		}
	}
}

