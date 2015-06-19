// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.Features.RemoveUnnecessaryImports;
using MonoDevelop.Core;
using RefactoringEssentials;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.CSharp.Diagnostics;

namespace MonoDevelop.CSharp.CodeFixes.RemoveUnusedUsings
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryImports), Shared]
	[ExtensionOrder(After = PredefinedCodeFixProviderNames.AddMissingReference)]
	internal class RemoveUnnecessaryUsingsCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		static readonly CSharpRemoveUnnecessaryImportsService service = new CSharpRemoveUnnecessaryImportsService();

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;

			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var newDocument = service.RemoveUnnecessaryImports(document, model, root, cancellationToken);
			if (newDocument == document || newDocument == null)
			{
				return;
			}

			context.RegisterCodeFix(
				new DocumentChangeAction(context.Diagnostics[0].Location.SourceSpan, DiagnosticSeverity.Warning,
					GettextCatalog.GetString ("Remove Unnecessary Usings"),
					(c) => Task.FromResult(newDocument)),
				context.Diagnostics);
		}

	}
}
