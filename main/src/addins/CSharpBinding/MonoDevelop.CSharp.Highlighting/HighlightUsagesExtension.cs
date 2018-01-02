// 
// HighlightUsagesExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ICSharpCode.NRefactory6.CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Refactoring;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.Ide.Editor.Highlighting;
using Microsoft.CodeAnalysis.DocumentHighlighting;

namespace MonoDevelop.CSharp.Highlighting
{
	class HighlightUsagesExtension : AbstractUsagesExtension<ImmutableArray<DocumentHighlights>>
	{
		IDocumentHighlightsService highlightsService; 

		protected override void Initialize ()
		{
			base.Initialize ();
			highlightsService = DocumentContext.RoslynWorkspace.Services.GetLanguageServices (LanguageNames.CSharp).GetService<IDocumentHighlightsService> ();

			Editor.SetSelectionSurroundingProvider (new CSharpSelectionSurroundingProvider (Editor, DocumentContext));
			fallbackHighlighting = Editor.SyntaxHighlighting;
			UpdateHighlighting ();
			DocumentContext.AnalysisDocumentChanged += HandleAnalysisDocumentChanged;
		}

		void HandleAnalysisDocumentChanged (object sender, EventArgs args)
		{
			Runtime.RunInMainThread (delegate {
				UpdateHighlighting ();
			});
		}

		ISyntaxHighlighting fallbackHighlighting;
		void UpdateHighlighting ()
		{
			if (DocumentContext?.AnalysisDocument == null) {
				if (Editor.SyntaxHighlighting != fallbackHighlighting)
					Editor.SyntaxHighlighting = fallbackHighlighting;
				return;
			}
			var old = Editor.SyntaxHighlighting as RoslynClassificationHighlighting;
			if (old == null || old.DocumentId != DocumentContext.AnalysisDocument.Id) {
				Editor.SyntaxHighlighting = new RoslynClassificationHighlighting ((MonoDevelopWorkspace)DocumentContext.RoslynWorkspace,
																				  DocumentContext.AnalysisDocument.Id, "source.cs");
			}
		}

		public override void Dispose ()
		{
			DocumentContext.AnalysisDocumentChanged -= HandleAnalysisDocumentChanged;
			Editor.SyntaxHighlighting = fallbackHighlighting;
			base.Dispose ();
		}

		protected async override Task<ImmutableArray<DocumentHighlights>> ResolveAsync (CancellationToken token)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return ImmutableArray<DocumentHighlights>.Empty;
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return ImmutableArray<DocumentHighlights>.Empty;

			return await highlightsService.GetDocumentHighlightsAsync (analysisDocument, doc.Editor.CaretOffset, ImmutableHashSet<Document>.Empty.Add (analysisDocument), token);
		}

		protected override Task<IEnumerable<MemberReference>> GetReferencesAsync (ImmutableArray<DocumentHighlights> resolveResult, CancellationToken token)
		{
			var result = new List<MemberReference> ();
			foreach (var highlight in resolveResult) {
				foreach (var span in highlight.HighlightSpans) {
					result.Add (new MemberReference (highlight, highlight.Document.FilePath, span.TextSpan.Start, span.TextSpan.Length) {
						ReferenceUsageType = ConvertKind (span.Kind)
					});
				}
			}
			return Task.FromResult((IEnumerable<MemberReference>)result);
		}

		static ReferenceUsageType ConvertKind (HighlightSpanKind kind)
		{
			switch (kind) {
			case HighlightSpanKind.Definition:
				return ReferenceUsageType.Declaration;
			case HighlightSpanKind.Reference:
				return ReferenceUsageType.Read;
			case HighlightSpanKind.WrittenReference:
				return ReferenceUsageType.ReadWrite;
			default:
				return ReferenceUsageType.Unknown;
			}
		}

		internal static ReferenceUsageType GetUsage (SyntaxNode node)
		{
			if (node == null)
				return ReferenceUsageType.Read;

			var parent = node.AncestorsAndSelf ().OfType<ExpressionSyntax> ().FirstOrDefault ();
			if (parent == null)
				return ReferenceUsageType.Read;
			if (parent.IsOnlyWrittenTo ())
				return ReferenceUsageType.Write;
			if (parent.IsWrittenTo ())
				return ReferenceUsageType.ReadWrite;
			return ReferenceUsageType.Read;
		}
	}
}