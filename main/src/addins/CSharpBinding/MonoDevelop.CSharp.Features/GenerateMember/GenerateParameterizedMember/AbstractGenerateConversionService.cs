// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.GenerateMember.GenerateParameterizedMember
{
	public abstract partial class AbstractGenerateConversionService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
	AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
		where TService : AbstractGenerateConversionService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
		where TSimpleNameSyntax : TExpressionSyntax
		where TExpressionSyntax : SyntaxNode
		where TInvocationExpressionSyntax : TExpressionSyntax
	{
		protected abstract bool IsImplicitConversionGeneration(SyntaxNode node);
		protected abstract bool IsExplicitConversionGeneration(SyntaxNode node);
		protected abstract bool TryInitializeImplicitConversionState(SemanticDocument document, SyntaxNode expression, ISet<TypeKind> classInterfaceModuleStructTypes, CancellationToken cancellationToken, out SyntaxToken identifierToken, out IMethodSymbol methodSymbol, out INamedTypeSymbol typeToGenerateIn);
		protected abstract bool TryInitializeExplicitConversionState(SemanticDocument document, SyntaxNode expression, ISet<TypeKind> classInterfaceModuleStructTypes, CancellationToken cancellationToken, out SyntaxToken identifierToken, out IMethodSymbol methodSymbol, out INamedTypeSymbol typeToGenerateIn);

		public async Task<IEnumerable<CodeAction>> GenerateConversionAsync(
			Document document,
			SyntaxNode node,
			CancellationToken cancellationToken)
		{
			var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
			var state = await State.GenerateConversionStateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
			if (state == null)
			{
				return SpecializedCollections.EmptyEnumerable<CodeAction>();
			}

			return GetActions(document, state, cancellationToken);
		}

		protected new class State : AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>.State
		{
			public static async Task<State> GenerateConversionStateAsync(
				TService service,
				SemanticDocument document,
				SyntaxNode interfaceNode,
				CancellationToken cancellationToken)
			{
				var state = new State();
				if (!await state.TryInitializeConversionAsync(service, document, interfaceNode, cancellationToken).ConfigureAwait(false))
				{
					return null;
				}

				return state;
			}

			private Task<bool> TryInitializeConversionAsync(
				TService service,
				SemanticDocument document,
				SyntaxNode node,
				CancellationToken cancellationToken)
			{
				if (service.IsImplicitConversionGeneration(node))
				{
					if (!TryInitializeImplicitConversion(service, document, node, cancellationToken))
					{
						return Task.FromResult (false);
					}
				}
				else if (service.IsExplicitConversionGeneration(node))
				{
					if (!TryInitializeExplicitConversion(service, document, node, cancellationToken))
					{
						return Task.FromResult (false);
					}
				}

				return TryFinishInitializingState(service, document, cancellationToken);
			}

			private bool TryInitializeExplicitConversion(TService service, SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
			{
				MethodKind = MethodKind.Conversion;
				SyntaxToken identifierToken;
				IMethodSymbol methodSymbol;
				INamedTypeSymbol typeToGenerateIn;
				if (!service.TryInitializeExplicitConversionState(
					document, node, ClassInterfaceModuleStructTypes, cancellationToken,
					out identifierToken, out methodSymbol, out typeToGenerateIn))
				{
					return false;
				}

				this.ContainingType = document.SemanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
				if (ContainingType == null)
				{
					return false;
				}

				this.IdentifierToken = identifierToken;
				this.TypeToGenerateIn = typeToGenerateIn;
				this.SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);
				this.location = node.GetLocation();
				this.MethodGenerationKind = MethodGenerationKind.ExplicitConversion;
				return true;
			}

			private bool TryInitializeImplicitConversion(TService service, SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
			{
				MethodKind = MethodKind.Conversion;
				SyntaxToken identifierToken;
				IMethodSymbol methodSymbol;
				INamedTypeSymbol typeToGenerateIn;
				if (!service.TryInitializeImplicitConversionState(
					document, node, ClassInterfaceModuleStructTypes, cancellationToken,
					out identifierToken, out methodSymbol, out typeToGenerateIn))
				{
					return false;
				}

				this.ContainingType = document.SemanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
				if (ContainingType == null)
				{
					return false;
				}

				this.IdentifierToken = identifierToken;
				this.TypeToGenerateIn = typeToGenerateIn;
				this.SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);
				this.MethodGenerationKind = MethodGenerationKind.ImplicitConversion;
				return true;
			}
		}
	}
}
