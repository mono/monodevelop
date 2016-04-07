// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using RefactoringEssentials;
using MonoDevelop.CSharp.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.CodeFixes
{
	internal abstract partial class AbstractChangeToAsyncCodeFixProvider : AbstractAsyncCodeFix
	{
		protected abstract Task<string> GetDescription(Diagnostic diagnostic, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken);
		protected abstract Task<Tuple<SyntaxTree, SyntaxNode>> GetRootInOtherSyntaxTree(SyntaxNode node, SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken);

		protected override async Task<CodeAction> GetCodeFix(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

			var result = await GetRootInOtherSyntaxTree(node, semanticModel, diagnostic, cancellationToken).ConfigureAwait(false);
			if (result != null)
			{
				var syntaxTree = result.Item1;
				var newRoot = result.Item2;
				var otherDocument = document.Project.Solution.GetDocument(syntaxTree);
				return new DocumentChangeAction(node.Span, DiagnosticSeverity.Error,
					await this.GetDescription(diagnostic, node, semanticModel, cancellationToken).ConfigureAwait(false),
					token => Task.FromResult(otherDocument.WithSyntaxRoot(newRoot)));
			}

			return null;
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertToAsync), Shared]
	internal class CSharpConvertToAsyncMethodCodeFixProvider : AbstractChangeToAsyncCodeFixProvider
	{
		/// <summary>
		/// Cannot await void.
		/// </summary>
		private const string CS4008 = "CS4008";

		public override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS4008); }
		}

		protected override async Task<string> GetDescription(
			Diagnostic diagnostic,
			SyntaxNode node,
			SemanticModel semanticModel,
			CancellationToken cancellationToken)
		{
			var methodNode = await GetMethodDeclaration(node, semanticModel, cancellationToken).ConfigureAwait(false);
			return string.Format(GettextCatalog.GetString ("Make {0} return Task instead of void"), methodNode.WithBody(null));
		}

		protected override async Task<Tuple<SyntaxTree, SyntaxNode>> GetRootInOtherSyntaxTree(
			SyntaxNode node,
			SemanticModel semanticModel,
			Diagnostic diagnostic,
			CancellationToken cancellationToken)
		{
			var methodDeclaration = await GetMethodDeclaration(node, semanticModel, cancellationToken).ConfigureAwait(false);
			if (methodDeclaration == null)
			{
				return null;
			}

			var oldRoot = await methodDeclaration.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var newRoot = oldRoot.ReplaceNode(methodDeclaration, ConvertToAsyncFunction(methodDeclaration));
			return Tuple.Create(oldRoot.SyntaxTree, newRoot);
		}

		private async Task<MethodDeclarationSyntax> GetMethodDeclaration(
			SyntaxNode node,
			SemanticModel semanticModel,
			CancellationToken cancellationToken)
		{
			var invocationExpression = node.ChildNodes().FirstOrDefault(n => n.IsKind(SyntaxKind.InvocationExpression));
			var methodSymbol = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol as IMethodSymbol;
			if (methodSymbol == null)
			{
				return null;
			}

			var methodReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
			if (methodReference == null)
			{
				return null;
			}

			var methodDeclaration = (await methodReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false)) as MethodDeclarationSyntax;
			if (methodDeclaration == null)
			{
				return null;
			}

			if (!methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
			{
				return null;
			}

			return methodDeclaration;
		}

		private MethodDeclarationSyntax ConvertToAsyncFunction(MethodDeclarationSyntax methodDeclaration)
		{
			return methodDeclaration.WithReturnType(
				SyntaxFactory.ParseTypeName("Task")
				.WithLeadingTrivia(methodDeclaration.ReturnType.GetLeadingTrivia())
				.WithTrailingTrivia(methodDeclaration.ReturnType.GetTrailingTrivia()));
		}
	}
}
