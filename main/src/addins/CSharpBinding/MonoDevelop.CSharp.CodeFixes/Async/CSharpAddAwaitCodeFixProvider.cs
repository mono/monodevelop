// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.CodeFixes
{
	internal abstract partial class AbstractAddAsyncCodeFixProvider : AbstractAddAsyncAwaitCodeFixProvider
	{
		protected const string SystemThreadingTasksTask = "System.Threading.Tasks.Task";
		protected const string SystemThreadingTasksTaskT = "System.Threading.Tasks.Task`1";
		protected abstract SyntaxNode AddAsyncKeyword(SyntaxNode methodNode);
		protected abstract SyntaxNode AddAsyncKeywordAndTaskReturnType(SyntaxNode methodNode, ITypeSymbol existingReturnType, INamedTypeSymbol taskTypeSymbol);
		protected abstract bool DoesConversionExist(Compilation compilation, ITypeSymbol source, ITypeSymbol destination);

		protected async Task<SyntaxNode> ConvertMethodToAsync(Document document, SemanticModel semanticModel, SyntaxNode methodNode, CancellationToken cancellationToken)
		{
			var methodSymbol = semanticModel.GetDeclaredSymbol(methodNode, cancellationToken) as IMethodSymbol;

			if (methodSymbol.ReturnsVoid)
			{
				return AddAsyncKeyword(methodNode);
			}

			var returnType = methodSymbol.ReturnType;
			var compilation = semanticModel.Compilation;

			var taskSymbol = compilation.GetTypeByMetadataName(SystemThreadingTasksTask);
			var genericTaskSymbol = compilation.GetTypeByMetadataName(SystemThreadingTasksTaskT);
			if (taskSymbol == null)
			{
				return null;
			}

			if (returnType is IErrorTypeSymbol)
			{
				// The return type of the method will not bind.  This could happen for a lot of reasons.
				// The type may not actually exist or the user could just be missing a using/import statement.
				// We're going to try and see if there are any known types that have the same name as 
				// our return type, and then check if those are convertible to Task.  If they are then
				// we assume the user just has a missing using.  If they are not, we wrap the return
				// type in a generic Task.
				var typeName = returnType.Name;

				var results = await SymbolFinder.FindDeclarationsAsync(
					document.Project, typeName, ignoreCase: false, filter: SymbolFilter.Type, cancellationToken: cancellationToken).ConfigureAwait(false);

				if (results.OfType<ITypeSymbol>().Any(s => DoesConversionExist(compilation, s, taskSymbol)))
				{
					return AddAsyncKeyword(methodNode);
				}

				return AddAsyncKeywordAndTaskReturnType(methodNode, returnType, genericTaskSymbol);
			}

			if (DoesConversionExist(compilation, returnType, taskSymbol))
			{
				return AddAsyncKeyword(methodNode);
			}

			return AddAsyncKeywordAndTaskReturnType(methodNode, returnType, genericTaskSymbol);
		}
	}


	[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddAwait), Shared]
	internal class CSharpAddAwaitCodeFixProvider : AbstractAddAsyncAwaitCodeFixProvider
	{
		/// <summary>
		/// Since this is an async method, the return expression must be of type 'blah' rather than 'baz'
		/// </summary>
		private const string CS4014 = "CS4014";

		/// <summary>
		/// Because this call is not awaited, execution of the current method continues before the call is completed.
		/// </summary>
		private const string CS4016 = "CS4016";

		public override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS4014, CS4016); }
		}

		protected override string GetDescription(Diagnostic diagnostic, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
		{
			return GettextCatalog.GetString ("Insert 'await'");
		}

		protected override Task<SyntaxNode> GetNewRoot(SyntaxNode root, SyntaxNode oldNode, SemanticModel semanticModel, Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
		{
			var expression = oldNode as ExpressionSyntax;

			switch (diagnostic.Id)
			{
				case CS4014:
					if (expression == null)
					{
						return Task.FromResult<SyntaxNode>(null);
					}

				return Task.FromResult(root.ReplaceNode(oldNode, ConvertToAwaitExpression(expression)));
				case CS4016:
					if (expression == null)
					{
					return Task.FromResult (default (SyntaxNode));
					}

					if (!IsCorrectReturnType(expression, semanticModel))
					{
					return Task.FromResult (default (SyntaxNode));
					}

				return Task.FromResult(root.ReplaceNode(oldNode, ConvertToAwaitExpression(expression)));
				default:
				return Task.FromResult (default (SyntaxNode));
			}
		}

		private bool IsCorrectReturnType(ExpressionSyntax expression, SemanticModel semanticModel)
		{
			INamedTypeSymbol taskType = null;
			INamedTypeSymbol returnType = null;
			return TryGetTypes(expression, semanticModel, out taskType, out returnType) &&
				semanticModel.Compilation.ClassifyConversion(taskType, returnType).Exists;
		}

		private static ExpressionSyntax ConvertToAwaitExpression(ExpressionSyntax expression)
		{
			return SyntaxFactory.AwaitExpression(expression)
				                .WithAdditionalAnnotations(Formatter.Annotation);
		}
	}
}
