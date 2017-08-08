//
// CSharpCodeLensProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xwt;
using Cairo;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.CSharp.Refactoring
{
	class CSharpCodeLens : CodeLens
	{
		readonly ICodeLensDisplayInfoService displayService;
		readonly ICodeLensReferencesService referencesService;
		readonly ISymbol symbol;
		readonly SyntaxNode displayNode;
		readonly Solution solution;
		readonly DocumentId id;

		public override Core.Text.TextSegment CodeLensSpan {
			get {
				return new Core.Text.TextSegment (displayNode.Span.Start, displayNode.Span.Length);
			}
		}

		public CSharpCodeLens (Solution solution, DocumentId id, ICodeLensDisplayInfoService displayService, ICodeLensReferencesService referencesService, ISymbol symbol, SyntaxNode displayNode)
		{
			this.id = id;
			this.solution = solution;
			this.displayService = displayService;
			this.referencesService = referencesService;
			this.symbol = symbol;
			this.displayNode = displayNode;
		}

		Size size;
		ReferenceCount count;
		public override Size Size => size;

		public async Task Update (CancellationToken token)
		{
			count = await referencesService.GetReferenceCountAsync (solution, id, displayNode, 1000, token);
		}

		public override void Draw (CodeLansDrawingParameters drawingParameters)
		{
			var p = drawingParameters as GtkCodeLansDrawingParameters;
			if (count == null || p == null)
				return;
			p.Context.Save ();
			var theme = p.Editor.Options.GetEditorTheme ();
			theme.TryGetColor (EditorThemeColors.FoldLine, out Components.HslColor color);
			p.Context.SetSourceColor (color);
			p.Context.Translate (p.X, p.Y);
			p.Layout.FontDescription = FontService.SansFont;
			string text;
			if (count.IsCapped) {
				text = GettextCatalog.GetString (">{0} references", count.Count);
			} else {
				text = GettextCatalog.GetString ("{0} references", count.Count);
			}
			p.Layout.SetMarkup ("<span size='small'>" + text + "</span>");
			p.Context.ShowLayout (p.Layout);
			p.Context.Restore ();
			p.Layout.GetPixelSize (out int pw, out int ph);
			size = new Size (pw, ph);
		}
	}

	class CSharpCodeLensProvider : CodeLensProvider
	{
		public override async Task<IEnumerable<CodeLens>> GetLenses (TextEditor editor, DocumentContext ctx, CancellationToken token)
		{
			var languageService = ctx.RoslynWorkspace.Services.GetLanguageServices (LanguageNames.CSharp);
			if (languageService == null)
				return Enumerable.Empty<CodeLens> ();
			var displayService = languageService.GetService<ICodeLensDisplayInfoService> ();
			var referencesService = ctx.RoslynWorkspace.Services.GetService<ICodeLensReferencesService> ();
			if (displayService == null || referencesService == null)
				return Enumerable.Empty<CodeLens> ();
			var model = await ctx.AnalysisDocument.GetSemanticModelAsync (token).ConfigureAwait (false);
			var root = await model.SyntaxTree.GetRootAsync (token).ConfigureAwait (false);
			var result = new List<CodeLens> ();
			foreach (var node in model.SyntaxTree.GetRoot ().DescendantNodesAndSelf ()) {
				var symbol = model.GetDeclaredSymbol (node);
				if (symbol == null || symbol.Kind == SymbolKind.Namespace)
					continue;
				var displayNode = displayService.GetDisplayNode (node);
				var newLens = new CSharpCodeLens (ctx.AnalysisDocument.Project.Solution, ctx.AnalysisDocument.Id, displayService, referencesService, symbol, displayNode);
				await newLens.Update (token);
				result.Add (newLens);
			}

			return result;
		}
	}
}
