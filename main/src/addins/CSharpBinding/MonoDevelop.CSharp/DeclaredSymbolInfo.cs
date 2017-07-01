//
// DeclaredSymbolInfo.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp
{
	struct DeclaredSymbolInfoWrapper
	{
		internal DocumentId DocumentId { get; }

		public string FilePath { get; }

		public DeclaredSymbolInfo SymbolInfo { get; }

		public DeclaredSymbolInfoWrapper(SyntaxNode node, DocumentId documentId, DeclaredSymbolInfo wrapped)
			: this()
		{
			FilePath = node.SyntaxTree.FilePath;
			DocumentId = documentId;
			SymbolInfo = wrapped;
		}

		public async Task<ISymbol> GetSymbolAsync(Document document, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var node = root.FindNode(SymbolInfo.Span);
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
			return symbol;
		}
	}

	class DeclaredSymbolInfoResult : SearchResult
	{
		bool useFullName;

		DeclaredSymbolInfoWrapper type;

		public override SearchResultType SearchResultType { get { return SearchResultType.Type; } }

		public override string File {
			get { return type.FilePath; }
		}

		public override Xwt.Drawing.Image Icon {
			get {
				return ImageService.GetIcon (type.GetStockIconForSymbolInfo(), IconSize.Menu);
			}
		}

		public override int Offset {
			get { return type.SymbolInfo.Span.Start; }
		}

		public override int Length {
			get { return type.SymbolInfo.Span.Length; }
		}

		public override string PlainText {
			get {
				return type.SymbolInfo.Name;
			}
		}
		Document GetDocument (CancellationToken token)
		{
			var doc = type.DocumentId;
			if (doc == null) {
				var docId = TypeSystemService.GetDocuments (type.FilePath).FirstOrDefault ();
				if (docId == null)
					return null;
				return TypeSystemService.GetCodeAnalysisDocument (docId, token);
			}
			return TypeSystemService.GetCodeAnalysisDocument (type.DocumentId, token);
		}

		public override Task<TooltipInformation> GetTooltipInformation (CancellationToken token)
		{
			return Task.Run (async delegate {
				var doc = GetDocument (token);
				if (doc == null) {
					return null;
				}
				var symbol = await type.GetSymbolAsync (doc, token);
				return await Ambience.GetTooltip (token, symbol);
			});
		}

		public override string Description {
			get {
				string loc;
				//				if (type.TryGetSourceProject (out project)) {
				//					loc = GettextCatalog.GetString ("project {0}", project.Name);
				//				} else {
				loc = GettextCatalog.GetString ("file {0}", File);
				//				}

				switch (type.SymbolInfo.Kind) {
					case DeclaredSymbolInfoKind.Interface:
					return GettextCatalog.GetString ("interface ({0})", loc);
					case DeclaredSymbolInfoKind.Struct:
					return GettextCatalog.GetString ("struct ({0})", loc);
					case DeclaredSymbolInfoKind.Delegate:
					return GettextCatalog.GetString ("delegate ({0})", loc);
					case DeclaredSymbolInfoKind.Enum:
					return GettextCatalog.GetString ("enumeration ({0})", loc);
					case DeclaredSymbolInfoKind.Class:
					return GettextCatalog.GetString ("class ({0})", loc);

					case DeclaredSymbolInfoKind.Field:
					return GettextCatalog.GetString ("field ({0})", loc);
					case DeclaredSymbolInfoKind.Property:
					return GettextCatalog.GetString ("property ({0})", loc);
					case DeclaredSymbolInfoKind.Indexer:
					return GettextCatalog.GetString ("indexer ({0})", loc);
					case DeclaredSymbolInfoKind.Event:
					return GettextCatalog.GetString ("event ({0})", loc);
					case DeclaredSymbolInfoKind.Method:
					return GettextCatalog.GetString ("method ({0})", loc);
				}
				return GettextCatalog.GetString ("symbol ({0})", loc);
			}
		}

		public override string GetMarkupText (bool selected)
		{
			return HighlightMatch (useFullName ? type.SymbolInfo.FullyQualifiedContainerName : type.SymbolInfo.Name, match, selected);
		}

		public DeclaredSymbolInfoResult (string match, string matchedString, int rank, DeclaredSymbolInfoWrapper type, bool useFullName)  : base (match, matchedString, rank)
		{
			this.useFullName = useFullName;
			this.type = type;
		}

		public override bool CanActivate {
			get {
				var doc = GetDocument (default (CancellationToken));
				return doc != null;
			}
		}

		public override async void Activate ()
		{
			var token = default (CancellationToken);
			var doc = GetDocument (token);
			if (doc != null) {
				var symbol = await type.GetSymbolAsync (doc, token);
				var project = TypeSystemService.GetMonoProject (doc.Id);
				await RefactoringService.RoslynJumpToDeclaration (symbol, project);
			}
		}
	}
}
