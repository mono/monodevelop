// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp.GenerateMember.GenerateParameterizedMember
{
	public abstract partial class AbstractGenerateMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
	AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
		where TService : AbstractGenerateMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
		where TSimpleNameSyntax : TExpressionSyntax
		where TExpressionSyntax : SyntaxNode
		where TInvocationExpressionSyntax : TExpressionSyntax
	{
		protected abstract bool IsSimpleNameGeneration(SyntaxNode node);
		protected abstract bool IsExplicitInterfaceGeneration(SyntaxNode node);
		protected abstract bool TryInitializeExplicitInterfaceState(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken, out SyntaxToken identifierToken, out IMethodSymbol methodSymbol, out INamedTypeSymbol typeToGenerateIn);
		protected abstract bool TryInitializeSimpleNameState(SemanticDocument document, TSimpleNameSyntax simpleName, CancellationToken cancellationToken, out SyntaxToken identifierToken, out TExpressionSyntax simpleNameOrMemberAccessExpression, out TInvocationExpressionSyntax invocationExpressionOpt, out bool isInConditionalExpression);
		protected abstract ITypeSymbol CanGenerateMethodForSimpleNameOrMemberAccessExpression(SemanticModel semanticModel, TExpressionSyntax expresion, CancellationToken cancellationToken);

		public async Task<IEnumerable<CodeAction>> GenerateMethodAsync(
			Document document,
			SyntaxNode node,
			CancellationToken cancellationToken)
		{
			var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
			var state = await State.GenerateMethodStateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
			if (state == null)
			{
				return SpecializedCollections.EmptyEnumerable<CodeAction>();
			}

			return GetActions(document, state, cancellationToken);
		}

		internal protected new class State : AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>.State
		{
			public static async Task<State> GenerateMethodStateAsync(
				TService service,
				SemanticDocument document,
				SyntaxNode interfaceNode,
				CancellationToken cancellationToken)
			{
				var state = new State();
				if (!await state.TryInitializeMethodAsync(service, document, interfaceNode, cancellationToken).ConfigureAwait(false))
				{
					return null;
				}

				return state;
			}

			private Task<bool> TryInitializeMethodAsync(
				TService service,
				SemanticDocument document,
				SyntaxNode node,
				CancellationToken cancellationToken)
			{
				// Cases that we deal with currently:
				//
				// 1) expr.Foo
				// 2) expr->Foo
				// 3) Foo
				// 4) expr.Foo()
				// 5) expr->Foo()
				// 6) Foo()
				// 7) ReturnType Explicit.Interface.Foo()
				//
				// In the first 3 invocationExpressionOpt will be null and we'll have to infer a
				// delegate type in order to figure out the right method signature to generate. In
				// the next 3 invocationExpressionOpt will be non null and will be used to figure
				// out the types/name of the parameters to generate. In the last one, we're going to
				// generate into an interface.
				if (service.IsExplicitInterfaceGeneration(node))
				{
					if (!TryInitializeExplicitInterface(service, document, node, cancellationToken))
					{
						return Task.FromResult (false);
					}
				}
				else if (service.IsSimpleNameGeneration(node))
				{
					if (!TryInitializeSimpleName(service, document, (TSimpleNameSyntax)node, cancellationToken))
					{
						return Task.FromResult (false);
					}
				}

				return TryFinishInitializingState(service, document, cancellationToken);
			}

			private bool TryInitializeExplicitInterface(
				TService service,
				SemanticDocument document,
				SyntaxNode methodDeclaration,
				CancellationToken cancellationToken)
			{
				MethodKind = MethodKind.Ordinary;
				SyntaxToken identifierToken;
				IMethodSymbol methodSymbol;
				INamedTypeSymbol typeToGenerateIn;
				if (!service.TryInitializeExplicitInterfaceState(
					document, methodDeclaration, cancellationToken,
					out identifierToken, out methodSymbol, out typeToGenerateIn))
				{
					return false;
				}

				if (methodSymbol.ExplicitInterfaceImplementations.Any())
				{
					return false;
				}

				this.IdentifierToken = identifierToken;
				this.TypeToGenerateIn = typeToGenerateIn;

				cancellationToken.ThrowIfCancellationRequested();
				var semanticModel = document.SemanticModel;
				this.ContainingType = semanticModel.GetEnclosingNamedType(methodDeclaration.SpanStart, cancellationToken);
				if (this.ContainingType == null)
				{
					return false;
				}

				if (!this.ContainingType.Interfaces.Contains(this.TypeToGenerateIn))
				{
					return false;
				}

				this.SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);
				return true;
			}

			private bool TryInitializeSimpleName(
				TService service,
				SemanticDocument document,
				TSimpleNameSyntax simpleName,
				CancellationToken cancellationToken)
			{
				MethodKind = MethodKind.Ordinary;
				this.SimpleNameOpt = simpleName;

				SyntaxToken identifierToken;
				TExpressionSyntax simpleNameOrMemberAccessExpression;
				TInvocationExpressionSyntax invocationExpressionOpt;
				bool isInConditionalExpression;
				if (!service.TryInitializeSimpleNameState(
					document, simpleName, cancellationToken,
					out identifierToken, out simpleNameOrMemberAccessExpression, out invocationExpressionOpt, out isInConditionalExpression))
				{
					return false;
				}

				this.IdentifierToken = identifierToken;
				this.SimpleNameOrMemberAccessExpression = simpleNameOrMemberAccessExpression;
				this.InvocationExpressionOpt = invocationExpressionOpt;
				this.IsInConditionalAccessExpression = isInConditionalExpression;

				if (string.IsNullOrWhiteSpace(this.IdentifierToken.ValueText))
				{
					return false;
				}

				// If we're not in a type, don't even bother.  NOTE(cyrusn): We'll have to rethink this
				// for C# Script.
				cancellationToken.ThrowIfCancellationRequested();
				var semanticModel = document.SemanticModel;
				this.ContainingType = semanticModel.GetEnclosingNamedType(this.SimpleNameOpt.SpanStart, cancellationToken);
				if (this.ContainingType == null)
				{
					return false;
				}

				if (this.InvocationExpressionOpt != null)
				{
					this.SignatureInfo = service.CreateInvocationMethodInfo(document, this);
				}
				else
				{
					var delegateType = TypeGuessing.typeInferenceService.InferDelegateType(semanticModel, this.SimpleNameOrMemberAccessExpression, cancellationToken);
					if (delegateType != null && delegateType.DelegateInvokeMethod != null)
					{
						this.SignatureInfo = new MethodSignatureInfo(document, this, delegateType.DelegateInvokeMethod);
					}
					else
					{
						// We don't have and invocation expression or a delegate, but we may have a special expression without parenthesis.  Lets see
						// if the type inference service can directly infer the type for our expression.
						var expressionType = service.CanGenerateMethodForSimpleNameOrMemberAccessExpression(semanticModel, this.SimpleNameOrMemberAccessExpression, cancellationToken);
						if (expressionType == null)
						{
							return false;
						}

						this.SignatureInfo = new MethodSignatureInfo(document, this, CreateMethodSymbolWithReturnType(expressionType));
					}
				}

				// Now, try to bind the invocation and see if it succeeds or not.  if it succeeds and
				// binds uniquely, then we don't need to offer this quick fix.
				cancellationToken.ThrowIfCancellationRequested();

				// If the name bound with errors, then this is a candidate for generate method.
				var semanticInfo = semanticModel.GetSymbolInfo(this.SimpleNameOrMemberAccessExpression, cancellationToken);
				if (semanticInfo.GetAllSymbols().Any(s => s.Kind == SymbolKind.Local || s.Kind == SymbolKind.Parameter) &&
					!service.AreSpecialOptionsActive(semanticModel))
				{
					// if the name bound to something in scope then we don't want to generate the
					// method because it will be shadowed by what's in scope. Unless we are in a 
					// special state such as Option Strict On where we want to generate fixes even
					// if we shadow types.
					return false;
				}

				// Check if the symbol is on the list of valid symbols for this language. 
				cancellationToken.ThrowIfCancellationRequested();
				if (semanticInfo.Symbol != null && !service.IsValidSymbol(semanticInfo.Symbol, semanticModel))
				{
					return false;
				}

				// Either we found no matches, or this was ambiguous. Either way, we might be able
				// to generate a method here.  Determine where the user wants to generate the method
				// into, and if it's valid then proceed.
				cancellationToken.ThrowIfCancellationRequested();
				INamedTypeSymbol typeToGenerateIn;
				bool isStatic;
				if (!service.TryDetermineTypeToGenerateIn(
					document, this.ContainingType, this.SimpleNameOrMemberAccessExpression, cancellationToken,
					out typeToGenerateIn, out isStatic))
				{
					return false;
				}

				var expressionSyntax = (this.InvocationExpressionOpt ?? this.SimpleNameOrMemberAccessExpression) as ExpressionSyntax;
				this.IsWrittenTo = expressionSyntax.IsWrittenTo();
				this.TypeToGenerateIn = typeToGenerateIn;
				this.IsStatic = isStatic;
				this.MethodGenerationKind = MethodGenerationKind.Member;
				return true;
			}

			private static IMethodSymbol CreateMethodSymbolWithReturnType(ITypeSymbol expressionType)
			{
				return CodeGenerationSymbolFactory.CreateMethodSymbol(
					attributes: SpecializedCollections.EmptyList<AttributeData>(),
					accessibility: default(Accessibility),
					modifiers: default(DeclarationModifiers),
					returnType: expressionType,
					explicitInterfaceSymbol: null,
					name: null,
					typeParameters: SpecializedCollections.EmptyList<ITypeParameterSymbol>(),
					parameters: SpecializedCollections.EmptyList<IParameterSymbol>());
			}
		}
	}
}
