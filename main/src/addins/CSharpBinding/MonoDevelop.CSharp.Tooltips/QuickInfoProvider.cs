//
// QuickInfoProvider.cs
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
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;
using MonoDevelop.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using Gtk;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.SourceEditor
{
	static class QuickInfoProvider
	{
		public static async Task<TooltipInformation> GetQuickInfoAsync(TextEditor editor, DocumentContext ctx, ISymbol symbol, CancellationToken cancellationToken = default(CancellationToken))
		{
			var tooltipInfo = new TooltipInformation ();

			var model = await ctx.AnalysisDocument.GetSemanticModelAsync ();
			var descriptionService = ctx.RoslynWorkspace.Services.GetLanguageServices (model.Language).GetService<ISymbolDisplayService> ();

			var sections = await descriptionService.ToDescriptionGroupsAsync (ctx.RoslynWorkspace, model, editor.CaretOffset, new [] { symbol }.AsImmutable (), default (CancellationToken)).ConfigureAwait (false);

			ImmutableArray<TaggedText> parts;

			var sb = new StringBuilder ();

			var theme = editor.Options.GetEditorTheme ();

			if (sections.TryGetValue (SymbolDescriptionGroups.MainDescription, out parts)) {
				TaggedTextUtil.AppendTaggedText (sb, theme, parts);
			}

			// if generating quick info for an attribute, bind to the class instead of the constructor
			if (symbol.ContainingType?.IsAttribute () == true) {
				symbol = symbol.ContainingType;
			}

			var formatter = ctx.RoslynWorkspace.Services.GetLanguageServices (model.Language).GetService<IDocumentationCommentFormattingService> ();
			var documentation = symbol.GetDocumentationParts (model, editor.CaretOffset, formatter, cancellationToken);
			sb.Append ("<span font='" + FontService.SansFontName + "' size='small'>");

			if (documentation != null) {
				sb.AppendLine ();
				sb.AppendLine ();
				TaggedTextUtil.AppendTaggedText (sb, theme, documentation);
			}

			if (sections.TryGetValue (SymbolDescriptionGroups.AnonymousTypes, out parts)) {
				if (!parts.IsDefaultOrEmpty) {
					sb.AppendLine ();
					TaggedTextUtil.AppendTaggedText (sb, theme, parts);
				}
			}

			if (sections.TryGetValue (SymbolDescriptionGroups.AwaitableUsageText, out parts)) {
				if (!parts.IsDefaultOrEmpty) {
					sb.AppendLine ();
					TaggedTextUtil.AppendTaggedText (sb, theme, parts);
				}
			}


			if (sections.TryGetValue (SymbolDescriptionGroups.Exceptions, out parts)) {
				if (!parts.IsDefaultOrEmpty) {
					sb.AppendLine ();
					TaggedTextUtil.AppendTaggedText (sb, theme, parts);
				}
			}
			sb.Append ("</span>");

			tooltipInfo.SignatureMarkup = sb.ToString ();
			return tooltipInfo;
		}
	}
}