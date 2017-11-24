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

namespace MonoDevelop.CSharp.Highlighting
{
	class UsageData
	{
		public RefactoringSymbolInfo SymbolInfo;
		public Document Document;
		public int Offset;

		public ISymbol Symbol {
			get { return SymbolInfo != null ? SymbolInfo.Symbol ?? SymbolInfo.DeclaredSymbol : null; }
		}
	}

	class HighlightUsagesExtension : AbstractUsagesExtension<UsageData>
	{
		static IHighlighter [] highlighters;

		static HighlightUsagesExtension ()
		{
			try {
				highlighters = typeof (HighlightUsagesExtension).Assembly
					.GetTypes ()
					.Where (t => !t.IsAbstract && typeof (IHighlighter).IsAssignableFrom (t))
					.Select (Activator.CreateInstance)
					.Cast<IHighlighter> ()
					.ToArray ();
			} catch (Exception e) {
				LoggingService.LogError ("Error while loading highlighters.", e);
				highlighters = Array.Empty<IHighlighter> ();
			}
		}
		protected override void Initialize ()
		{
			base.Initialize ();
			Editor.SetSelectionSurroundingProvider (new CSharpSelectionSurroundingProvider (Editor, DocumentContext));
			fallbackHighlighting = Editor.SyntaxHighlighting;
			UpdateHighlighting ();
			DocumentContext.AnalysisDocumentChanged += delegate {
				Runtime.RunInMainThread (delegate {
					UpdateHighlighting ();
				});
			};
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
			Editor.SyntaxHighlighting = fallbackHighlighting;
			base.Dispose ();
		}

		protected async override Task<UsageData> ResolveAsync (CancellationToken token)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return new UsageData ();
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return new UsageData ();

			var symbolInfo = await RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor, token);
			if (symbolInfo.Symbol == null && symbolInfo.DeclaredSymbol == null)
				return new UsageData {
					Document = analysisDocument,
					Offset = doc.Editor.CaretOffset
				};

			if (symbolInfo.Symbol != null && !symbolInfo.Node.IsKind (SyntaxKind.IdentifierName) && !symbolInfo.Node.IsKind (SyntaxKind.GenericName))
				return new UsageData ();

			return new UsageData {
				Document = analysisDocument,
				SymbolInfo = symbolInfo,
				Offset = doc.Editor.CaretOffset
			};
		}

		protected override async Task<IEnumerable<MemberReference>> GetReferencesAsync (UsageData resolveResult, CancellationToken token)
		{
			var result = new List<MemberReference> ();
			if (resolveResult.Symbol == null) {
				if (resolveResult.Document == null)
					return result;
				var root = await resolveResult.Document.GetSyntaxRootAsync (token).ConfigureAwait (false);
				var doc2 = resolveResult.Document;
				var offset = resolveResult.Offset;
				if (!root.Span.Contains (offset))
					return result;
				foreach (var highlighter in highlighters) {
					try {
						foreach (var span in highlighter.GetHighlights (root, offset, token)) {
							result.Add (new MemberReference (span, doc2.FilePath, span.Start, span.Length) {
								ReferenceUsageType = ReferenceUsageType.Keyword
							});
						}
					} catch (Exception e) {
						LoggingService.LogError ("Highlighter " + highlighter + " threw exception.", e);
					}
				}
				return result;
			}

			var doc = resolveResult.Document;
			var documents = ImmutableHashSet.Create (doc);

			foreach (var symbol in await CSharpFindReferencesProvider.GatherSymbols (resolveResult.Symbol, resolveResult.Document.Project.Solution, token)) {
				foreach (var loc in symbol.Locations) {
					if (loc.IsInSource && loc.SourceTree.FilePath == doc.FilePath)
						result.Add (new MemberReference (symbol, doc.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length) {
							ReferenceUsageType = ReferenceUsageType.Declaration
						});
				}

				foreach (var mref in await SymbolFinder.FindReferencesAsync (symbol, DocumentContext.AnalysisDocument.Project.Solution, documents, token)) {
					foreach (var loc in mref.Locations) {
						Microsoft.CodeAnalysis.Text.TextSpan span = loc.Location.SourceSpan;
						var root = loc.Location.SourceTree.GetRoot ();
						var node = root.FindNode (loc.Location.SourceSpan);
						var trivia = root.FindTrivia (loc.Location.SourceSpan.Start);
						if (!trivia.IsKind (SyntaxKind.SingleLineDocumentationCommentTrivia)) {
							span = node.Span;
						}

						if (span.Start != loc.Location.SourceSpan.Start) {
							span = loc.Location.SourceSpan;
						}
						result.Add (new MemberReference (symbol, doc.FilePath, span.Start, span.Length) {
							ReferenceUsageType = GetUsage (node)
						});
					}
				}

				foreach (var loc in await GetAdditionalReferencesAsync (doc, symbol, token)) {
					result.Add (new MemberReference (symbol, doc.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length) {
						ReferenceUsageType = ReferenceUsageType.Write
					});
				}
			}

			return result;
		}

		async Task<IEnumerable<Location>> GetAdditionalReferencesAsync (Document document, ISymbol symbol, CancellationToken cancellationToken)
		{
			// The FindRefs engine won't find references through 'var' for performance reasons.
			// Also, they are not needed for things like rename/sig change, and the normal find refs
			// feature.  However, we would lke the results to be highlighted to get a good experience
			// while editing (especially since highlighting may have been invoked off of 'var' in
			// the first place).
			//
			// So we look for the references through 'var' directly in this file and add them to the
			// results found by the engine.
			List<Location> results = null;

			if (symbol is INamedTypeSymbol && symbol.Name != "var") {
				var originalSymbol = symbol.OriginalDefinition;
				var root = await document.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);

				var descendents = root.DescendantNodes ();
				var semanticModel = default (SemanticModel);

				foreach (var type in descendents.OfType<IdentifierNameSyntax> ()) {
					cancellationToken.ThrowIfCancellationRequested ();

					if (type.IsVar) {
						if (semanticModel == null) {
							semanticModel = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
						}

						var boundSymbol = semanticModel.GetSymbolInfo (type, cancellationToken).Symbol;
						boundSymbol = boundSymbol == null ? null : boundSymbol.OriginalDefinition;

						if (originalSymbol.Equals (boundSymbol)) {
							if (results == null) {
								results = new List<Location> ();
							}

							results.Add (type.GetLocation ());
						}
					}
				}
			}

			return results ?? SpecializedCollections.EmptyEnumerable<Location> ();
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

