// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis;
using System.Linq;
using System;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.GenerateMember.GenerateParameterizedMember
{
	public abstract partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
	AbstractGenerateMemberService<TSimpleNameSyntax, TExpressionSyntax>
		where TService : AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
		where TSimpleNameSyntax : TExpressionSyntax
		where TExpressionSyntax : SyntaxNode
		where TInvocationExpressionSyntax : TExpressionSyntax
	{
		protected AbstractGenerateParameterizedMemberService()
		{
		}

		protected abstract AbstractInvocationInfo CreateInvocationMethodInfo(SemanticDocument document, State abstractState);

		protected abstract bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel);
		protected abstract bool AreSpecialOptionsActive(SemanticModel semanticModel);

		protected virtual bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
		{
			return false;
		}

		protected virtual string GetImplicitConversionDisplayText(State state)
		{
			return string.Empty;
		}

		protected virtual string GetExplicitConversionDisplayText(State state)
		{
			return string.Empty;
		}

		protected IEnumerable<CodeAction> GetActions(Document document, State state, CancellationToken cancellationToken)
		{
			yield return new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: false, generateProperty: false);

			// If we're trying to generate an instance method into an abstract class (but not a
			// static class or an interface), then offer to generate it abstractly.
			var canGenerateAbstractly = state.TypeToGenerateIn.IsAbstract &&
				!state.TypeToGenerateIn.IsStatic &&
				state.TypeToGenerateIn.TypeKind != TypeKind.Interface &&
				!state.IsStatic;

			if (canGenerateAbstractly)
			{
				yield return new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: true, generateProperty: false);
			}

			if (true/*semanticFacts.SupportsParameterizedProperties*/ &&
				state.InvocationExpressionOpt != null)
			{
				var typeParameters = state.SignatureInfo.DetermineTypeParameters(cancellationToken);
				var returnType = state.SignatureInfo.DetermineReturnType(cancellationToken);

				if (typeParameters.Count == 0 && returnType.SpecialType != SpecialType.System_Void)
				{
					yield return new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: false, generateProperty: true);

					if (canGenerateAbstractly)
					{
						yield return new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: true, generateProperty: true);
					}
				}
			}
		}
		internal protected abstract class AbstractInvocationInfo : SignatureInfo
		{
			protected abstract bool IsIdentifierName();

			protected abstract IList<ITypeParameterSymbol> GetCapturedTypeParameters(CancellationToken cancellationToken);
			protected abstract IList<ITypeParameterSymbol> GenerateTypeParameters(CancellationToken cancellationToken);

			protected AbstractInvocationInfo(SemanticDocument document, State state)
				: base(document, state)
			{
			}

			public override IList<ITypeParameterSymbol> DetermineTypeParameters(CancellationToken cancellationToken)
			{
				var typeParameters = DetermineTypeParametersWorker(cancellationToken);
				return typeParameters.Select(tp => MassageTypeParameter(tp, cancellationToken)).ToList();
			}

			private IList<ITypeParameterSymbol> DetermineTypeParametersWorker(
				CancellationToken cancellationToken)
			{
				if (IsIdentifierName())
				{
					// If the user wrote something like Foo(x) then we still might want to generate
					// a generic method if the expression 'x' captured any method type variables.
					var capturedTypeParameters = GetCapturedTypeParameters(cancellationToken);
					var availableTypeParameters = this.State.TypeToGenerateIn.GetAllTypeParameters();
					var result = capturedTypeParameters.Except(availableTypeParameters).ToList();
					return result;
				}
				else
				{
					return GenerateTypeParameters(cancellationToken);
				}
			}

			private ITypeParameterSymbol MassageTypeParameter(
				ITypeParameterSymbol typeParameter,
				CancellationToken cancellationToken)
			{
				var constraints = typeParameter.ConstraintTypes.Where(ts => !ts.IsUnexpressableTypeParameterConstraint()).ToList();
				var classTypes = constraints.Where(ts => ts.TypeKind == TypeKind.Class).ToList();
				var nonClassTypes = constraints.Where(ts => ts.TypeKind != TypeKind.Class).ToList();

				classTypes = MergeClassTypes(classTypes, cancellationToken);
				constraints = classTypes.Concat(nonClassTypes).ToList();
				if (constraints.SequenceEqual(typeParameter.ConstraintTypes))
				{
					return typeParameter;
				}

				return CodeGenerationSymbolFactory.CreateTypeParameter(
					attributes: null,
					varianceKind: typeParameter.Variance,
					name: typeParameter.Name,
					constraintTypes: ImmutableArray.CreateRange<ITypeSymbol>(constraints),
					hasConstructorConstraint: typeParameter.HasConstructorConstraint,
					hasReferenceConstraint: typeParameter.HasReferenceTypeConstraint,
					hasValueConstraint: typeParameter.HasValueTypeConstraint);
			}

			private List<ITypeSymbol> MergeClassTypes(List<ITypeSymbol> classTypes, CancellationToken cancellationToken)
			{
				var compilation = this.Document.SemanticModel.Compilation;
				for (int i = classTypes.Count - 1; i >= 0; i--)
				{
					// For example, 'Attribute'.
					var type1 = classTypes[i];

					for (int j = 0; j < classTypes.Count; j++)
					{
						if (j != i)
						{
							// For example 'FooAttribute'.
							var type2 = classTypes[j];

							if (IsImplicitReferenceConversion(compilation, type2, type1))
							{
								// If there's an implicit reference conversion (i.e. from
								// FooAttribute to Attribute), then we don't need Attribute as it's
								// implied by the second attribute;
								classTypes.RemoveAt(i);
								break;
							}
						}
					}
				}

				return classTypes;
			}

			protected abstract bool IsImplicitReferenceConversion(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);
		}
		private partial class GenerateParameterizedMemberCodeAction : CodeAction
		{
			private readonly TService _service;
			private readonly Document _document;
			private readonly State _state;
			private readonly bool _isAbstract;
			private readonly bool _generateProperty;

			public GenerateParameterizedMemberCodeAction(
				TService service,
				Document document,
				State state,
				bool isAbstract,
				bool generateProperty)
			{
				_service = service;
				_document = document;
				_state = state;
				_isAbstract = isAbstract;
				_generateProperty = generateProperty;
			}

			private string GetDisplayText(
				State state,
				bool isAbstract,
				bool generateProperty)
			{
				switch (state.MethodGenerationKind)
				{
				case MethodGenerationKind.Member:
					var text = generateProperty ?
						isAbstract ? Resources.GenerateAbstractProperty : Resources.GeneratePropertyIn :
						isAbstract ? Resources.GenerateAbstractMethod : Resources.GenerateMethodIn;

					var name = state.IdentifierToken.ValueText;
					var destination = state.TypeToGenerateIn.Name;
					return string.Format(text, name, destination);
				case MethodGenerationKind.ImplicitConversion:
					return _service.GetImplicitConversionDisplayText(_state);
				case MethodGenerationKind.ExplicitConversion:
					return _service.GetExplicitConversionDisplayText(_state);
				default:
					throw new InvalidOperationException();
				}
			}

			protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
			{
				var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
				var syntaxFactory = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language).GetService<SyntaxGenerator>();

				if (_generateProperty)
				{
					var property = _state.SignatureInfo.GenerateProperty(syntaxFactory, _isAbstract, _state.IsWrittenTo, cancellationToken);

					var result = await CodeGenerator.AddPropertyDeclarationAsync(
						_document.Project.Solution,
						_state.TypeToGenerateIn,
						property,
						new CodeGenerationOptions(afterThisLocation: _state.IdentifierToken.GetLocation(), generateDefaultAccessibility: false),
						cancellationToken)
						.ConfigureAwait(false);

					return result;
				}
				else
				{
					var method = _state.SignatureInfo.GenerateMethod(syntaxFactory, _isAbstract, cancellationToken);

					var result = await CodeGenerator.AddMethodDeclarationAsync(
						_document.Project.Solution,
						_state.TypeToGenerateIn,
						method,
						new CodeGenerationOptions(afterThisLocation: _state.Location, generateDefaultAccessibility: false),
						cancellationToken)
						.ConfigureAwait(false);

					return result;
				}
			}

			public override string Title
			{
				get
				{
					return GetDisplayText(_state, _isAbstract, _generateProperty);
				}
			}
		}

		protected class MethodSignatureInfo : SignatureInfo
		{
			private readonly IMethodSymbol _methodSymbol;

			public MethodSignatureInfo(
				SemanticDocument document,
				State state,
				IMethodSymbol methodSymbol)
				: base(document, state)
			{
				_methodSymbol = methodSymbol;
			}

			protected override ITypeSymbol DetermineReturnTypeWorker(CancellationToken cancellationToken)
			{
				if (State.IsInConditionalAccessExpression)
				{
					return _methodSymbol.ReturnType.RemoveNullableIfPresent();
				}

				return _methodSymbol.ReturnType;
			}

			public override IList<ITypeParameterSymbol> DetermineTypeParameters(CancellationToken cancellationToken)
			{
				return _methodSymbol.TypeParameters;
			}

			protected override IList<RefKind> DetermineParameterModifiers(CancellationToken cancellationToken)
			{
				return _methodSymbol.Parameters.Select(p => p.RefKind).ToList();
			}

			protected override IList<bool> DetermineParameterOptionality(CancellationToken cancellationToken)
			{
				return _methodSymbol.Parameters.Select(p => p.IsOptional).ToList();
			}

			protected override IList<ITypeSymbol> DetermineParameterTypes(CancellationToken cancellationToken)
			{
				return _methodSymbol.Parameters.Select(p => p.Type).ToList();
			}

			protected override IList<string> DetermineParameterNames(CancellationToken cancellationToken)
			{
				return _methodSymbol.Parameters.Select(p => p.Name).ToList();
			}
		}

		internal protected abstract class SignatureInfo
		{
			protected readonly SemanticDocument Document;
			protected readonly State State;

			public SignatureInfo(
				SemanticDocument document,
				State state)
			{
				this.Document = document;
				this.State = state;
			}

			public abstract IList<ITypeParameterSymbol> DetermineTypeParameters(CancellationToken cancellationToken);
			public ITypeSymbol DetermineReturnType(CancellationToken cancellationToken)
			{
				return FixType(DetermineReturnTypeWorker(cancellationToken), cancellationToken);
			}

			protected abstract ITypeSymbol DetermineReturnTypeWorker(CancellationToken cancellationToken);
			protected abstract IList<RefKind> DetermineParameterModifiers(CancellationToken cancellationToken);
			protected abstract IList<ITypeSymbol> DetermineParameterTypes(CancellationToken cancellationToken);
			protected abstract IList<bool> DetermineParameterOptionality(CancellationToken cancellationToken);
			protected abstract IList<string> DetermineParameterNames(CancellationToken cancellationToken);

			internal IPropertySymbol GenerateProperty(
				SyntaxGenerator factory,
				bool isAbstract, bool includeSetter,
				CancellationToken cancellationToken)
			{
				var accessibility = DetermineAccessibility(isAbstract);
				var getMethod = CodeGenerationSymbolFactory.CreateAccessorSymbol(
					attributes: null,
					accessibility: accessibility,
					statements: GenerateStatements(factory, isAbstract, cancellationToken));

				var setMethod = includeSetter ? getMethod : null;

				return CodeGenerationSymbolFactory.CreatePropertySymbol(
					attributes: null,
					accessibility: accessibility,
					modifiers: DeclarationModifiers.None.WithIsStatic(State.IsStatic).WithIsAbstract (isAbstract),
					type: DetermineReturnType(cancellationToken),
					explicitInterfaceSymbol: null,
					name: this.State.IdentifierToken.ValueText,
					parameters: DetermineParameters(cancellationToken),
					getMethod: getMethod,
					setMethod: setMethod);
			}

			public IMethodSymbol GenerateMethod(
				SyntaxGenerator factory,
				bool isAbstract,
				CancellationToken cancellationToken)
			{
				var parameters = DetermineParameters(cancellationToken);
				var returnType = DetermineReturnType(cancellationToken);
				var isUnsafe = (parameters
					.Any(p => p.Type.IsUnsafe()) || returnType.IsUnsafe()) &&
					!State.IsContainedInUnsafeType;
				var method = CodeGenerationSymbolFactory.CreateMethodSymbol(
					attributes: null,
					accessibility: DetermineAccessibility(isAbstract),
					modifiers: DeclarationModifiers.None.WithIsStatic(State.IsStatic).WithIsAbstract (isAbstract).WithIsUnsafe (isUnsafe),
					returnType: returnType,
					explicitInterfaceSymbol: null,
					name: this.State.IdentifierToken.ValueText,
					typeParameters: DetermineTypeParameters(cancellationToken),
					parameters: parameters,
					statements: GenerateStatements(factory, isAbstract, cancellationToken),
					handlesExpressions: null,
					returnTypeAttributes: null,
					methodKind: State.MethodKind);

				// Ensure no conflicts between type parameter names and parameter names.
				var languageServiceProvider = this.Document.Project.Solution.Workspace.Services.GetLanguageServices(this.State.TypeToGenerateIn.Language);

				var equalityComparer = true ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
				var reservedParameterNames = this.DetermineParameterNames(cancellationToken).ToSet(equalityComparer);
				var newTypeParameterNames = NameGenerator.EnsureUniqueness(
					method.TypeParameters.Select(t => t.Name).ToList(), n => !reservedParameterNames.Contains(n));

				return method.RenameTypeParameters(newTypeParameterNames);
			}

			private ITypeSymbol FixType(
				ITypeSymbol typeSymbol,
				CancellationToken cancellationToken)
			{
				// A type can't refer to a type parameter that isn't available in the type we're
				// eventually generating into.
				var availableMethodTypeParameters = this.DetermineTypeParameters(cancellationToken);
				var availableTypeParameters = this.State.TypeToGenerateIn.GetAllTypeParameters();

				var compilation = this.Document.SemanticModel.Compilation;
				var allTypeParameters = availableMethodTypeParameters.Concat(availableTypeParameters);

				return typeSymbol.RemoveAnonymousTypes(compilation)
					.ReplaceTypeParametersBasedOnTypeConstraints(compilation, allTypeParameters, this.Document.Document.Project.Solution, cancellationToken)
					.RemoveUnavailableTypeParameters(compilation, allTypeParameters)
					.RemoveUnnamedErrorTypes(compilation);
			}

			private IList<SyntaxNode> GenerateStatements(
				SyntaxGenerator factory,
				bool isAbstract,
				CancellationToken cancellationToken)
			{
				var throwStatement = CodeGenerationHelpers.GenerateThrowStatement(factory, this.Document, "System.NotImplementedException", cancellationToken);

				return isAbstract || State.TypeToGenerateIn.TypeKind == TypeKind.Interface || throwStatement == null
					? null
						: new[] { throwStatement };
			}

			private IList<IParameterSymbol> DetermineParameters(CancellationToken cancellationToken)
			{
				var modifiers = DetermineParameterModifiers(cancellationToken);
				var types = DetermineParameterTypes(cancellationToken).Select(t => FixType(t, cancellationToken)).ToList();
				var optionality = DetermineParameterOptionality(cancellationToken);
				var names = DetermineParameterNames(cancellationToken);

				var result = new List<IParameterSymbol>();
				for (var i = 0; i < modifiers.Count; i++)
				{
					result.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
						attributes: null,
						refKind: modifiers[i],
						isParams: false,
						isOptional: optionality[i],
						type: types[i],
						name: names[i]));
				}

				return result;
			}

			private Accessibility DetermineAccessibility(bool isAbstract)
			{
				var containingType = this.State.ContainingType;

				// If we're generating into an interface, then we don't use any modifiers.
				if (State.TypeToGenerateIn.TypeKind != TypeKind.Interface)
				{
					// Otherwise, figure out what accessibility modifier to use and optionally
					// mark it as static.
					if (containingType.IsContainedWithin(State.TypeToGenerateIn) && !isAbstract)
					{
						return Accessibility.Private;
					}
					else if (DerivesFrom(containingType) && State.IsStatic)
					{
						// NOTE(cyrusn): We only generate protected in the case of statics.  Consider
						// the case where we're generating into one of our base types.  i.e.:
						//
						// class B : A { void Foo() { A a; a.Foo(); }
						//
						// In this case we can *not* mark the method as protected.  'B' can only
						// access protected members of 'A' through an instance of 'B' (or a subclass
						// of B).  It can not access protected members through an instance of the
						// superclass.  In this case we need to make the method public or internal.
						//
						// However, this does not apply if the method will be static.  i.e.
						// 
						// class B : A { void Foo() { A.Foo(); }
						//
						// B can access the protected statics of A, and so we generate 'Foo' as
						// protected.

						// TODO: Code coverage
						return Accessibility.Protected;
					}
					else if (containingType.ContainingAssembly.IsSameAssemblyOrHasFriendAccessTo(State.TypeToGenerateIn.ContainingAssembly))
					{
						return Accessibility.Internal;
					}
					else
					{
						// TODO: Code coverage
						return Accessibility.Public;
					}
				}

				return Accessibility.NotApplicable;
			}

			private bool DerivesFrom(INamedTypeSymbol containingType)
			{
				return containingType.GetBaseTypes().Select(t => t.OriginalDefinition)
					.OfType<INamedTypeSymbol>()
					.Contains(State.TypeToGenerateIn);
			}
		}

		internal protected abstract class State
		{
			public INamedTypeSymbol ContainingType { get; protected set; }
			public INamedTypeSymbol TypeToGenerateIn { get; protected set; }
			public bool IsStatic { get; protected set; }
			public bool IsContainedInUnsafeType { get; protected set; }

			// Just the name of the method.  i.e. "Foo" in "X.Foo" or "X.Foo()"
			public SyntaxToken IdentifierToken { get; protected set; }
			public TSimpleNameSyntax SimpleNameOpt { get; protected set; }

			// The entire expression containing the name, not including the invocation.  i.e. "X.Foo"
			// in "X.Foo()".
			public TExpressionSyntax SimpleNameOrMemberAccessExpression { get; protected set; }
			public TInvocationExpressionSyntax InvocationExpressionOpt { get; protected set; }
			public bool IsInConditionalAccessExpression { get; protected set; }

			public bool IsWrittenTo { get; protected set; }

			public SignatureInfo SignatureInfo { get; protected set; }
			public MethodKind MethodKind { get; internal set; }
			public MethodGenerationKind MethodGenerationKind { get; protected set; }
			protected Location location = null;
			public Location Location
			{
				get
				{
					if (IdentifierToken.SyntaxTree != null)
					{
						return IdentifierToken.GetLocation();
					}

					return location;
				}
			}

			protected async Task<bool> TryFinishInitializingState(TService service, SemanticDocument document, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				this.TypeToGenerateIn = await SymbolFinder.FindSourceDefinitionAsync(this.TypeToGenerateIn, document.Project.Solution, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
				if (this.TypeToGenerateIn.IsErrorType())
				{
					return false;
				}

				if (!service.ValidateTypeToGenerateIn(document.Project.Solution, this.TypeToGenerateIn,
					this.IsStatic, ClassInterfaceModuleStructTypes, cancellationToken))
				{
					return false;
				}

				if (!new CSharpCodeGenerationService(document.Project.Solution.Workspace).CanAddTo(this.TypeToGenerateIn, document.Project.Solution, cancellationToken))
				{
					return false;
				}

				// Ok.  It either didn't bind to any symbols, or it bound to a symbol but with
				// errors.  In the former case we definitely want to offer to generate a method.  In
				// the latter case, we want to generate a method *unless* there's an existing method
				// with the same signature.
				var existingMethods = this.TypeToGenerateIn.GetMembers(this.IdentifierToken.ValueText)
					.OfType<IMethodSymbol>();

				var destinationProvider = document.Project.Solution.Workspace.Services.GetLanguageServices(this.TypeToGenerateIn.Language);
				var syntaxFactory = destinationProvider.GetService<SyntaxGenerator>();
				this.IsContainedInUnsafeType = service.ContainingTypesOrSelfHasUnsafeKeyword(this.TypeToGenerateIn);
				var generatedMethod = this.SignatureInfo.GenerateMethod(syntaxFactory, false, cancellationToken);
				return !existingMethods.Any(m => SignatureComparer.HaveSameSignature(m, generatedMethod, caseSensitive: true, compareParameterName: true, isParameterCaseSensitive: true));
			}
		}
	}
}
