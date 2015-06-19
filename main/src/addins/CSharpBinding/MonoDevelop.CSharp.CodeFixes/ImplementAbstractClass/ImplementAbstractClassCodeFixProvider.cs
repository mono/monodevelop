// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.Features.ImplementAbstractClass;
using MonoDevelop.Core;
using RefactoringEssentials;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp.CodeFixes.ImplementAbstractClass
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementAbstractClass), Shared]
	[ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateType)]
	internal class ImplementAbstractClassCodeFixProvider : CodeFixProvider
	{
		private const string CS0534 = "CS0534"; // 'Program' does not implement inherited abstract member 'Foo.bar()'

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS0534); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var token = root.FindToken(context.Span.Start);
			if (!token.Span.IntersectsWith(context.Span))
			{
				return;
			}

			var classNode = token.Parent as ClassDeclarationSyntax;
			if (classNode == null)
			{
				return;
			}

			var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
			if (model.IsFromGeneratedCode (context.CancellationToken))
				return;
			
			foreach (var baseTypeSyntax in classNode.BaseList.Types)
			{
				var node = baseTypeSyntax.Type;

				if (service.CanImplementAbstractClass(
					context.Document,
					model,
					node,
					context.CancellationToken))
				{
					var title = GettextCatalog.GetString ("Implement Abstract Class");
					var abstractType = model.GetTypeInfo(node, context.CancellationToken).Type;
					var id = GetCodeActionId(abstractType.ContainingAssembly.Name, abstractType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
					context.RegisterCodeFix(
						new DocumentChangeAction(node.Span, DiagnosticSeverity.Error, title,
							(c) => ImplementAbstractClassAsync(context.Document, node, c)),
						context.Diagnostics);
					return;
				}
			}
		}

		// internal for testing purposes.
		internal static string GetCodeActionId(string assemblyName, string abstractTypeFullyQualifiedName)
		{
			return "ImplementAbstractClass;" +
				assemblyName + ";" +
				abstractTypeFullyQualifiedName;
		}
		static CSharpImplementAbstractClassService service = new CSharpImplementAbstractClassService ();

		private async Task<Document> ImplementAbstractClassAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			return await service.ImplementAbstractClassAsync(
				document,
				await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false),
				node,
				cancellationToken).ConfigureAwait(false);
		}

	}
}
