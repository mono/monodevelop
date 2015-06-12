// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using ICSharpCode.NRefactory6.CSharp.GenerateMember;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.Editing;
using ICSharpCode.NRefactory6.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateVariable
{
	public abstract partial class AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax> :
	AbstractGenerateMemberService<TSimpleNameSyntax, TExpressionSyntax>
		where TService : AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax>
		where TSimpleNameSyntax : TExpressionSyntax
		where TExpressionSyntax : SyntaxNode
	{
		protected AbstractGenerateVariableService()
		{
		}

		protected abstract bool IsExplicitInterfaceGeneration(SyntaxNode node);
		protected abstract bool IsIdentifierNameGeneration(SyntaxNode node);

		protected abstract bool TryInitializeExplicitInterfaceState(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken, out SyntaxToken identifierToken, out IPropertySymbol propertySymbol, out INamedTypeSymbol typeToGenerateIn);
		protected abstract bool TryInitializeIdentifierNameState(SemanticDocument document, TSimpleNameSyntax identifierName, CancellationToken cancellationToken, out SyntaxToken identifierToken, out TExpressionSyntax simpleNameOrMemberAccessExpression, out bool isInExecutableBlock, out bool isinConditionalAccessExpression);

		protected abstract bool TryConvertToLocalDeclaration(ITypeSymbol type, SyntaxToken identifierToken, OptionSet options, out SyntaxNode newRoot);

		public async Task<IEnumerable<CodeAction>> GenerateVariableAsync(
			Document document,
			SyntaxNode node,
			CancellationToken cancellationToken)
		{
			var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

			var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
			if (state == null)
			{
				return SpecializedCollections.EmptyEnumerable<CodeAction>();
			}

			var result = new List<CodeAction>();

			var canGenerateMember = CodeGenerator.CanAdd(document.Project.Solution, state.TypeToGenerateIn, cancellationToken);

			// prefer fields over properties (and vice versa) depending on the casing of the member.
			// lowercase -> fields.  title case -> properties.
			var name = state.IdentifierToken.ValueText;
			if (char.IsUpper(name.FirstOrDefault()))
			{
				if (canGenerateMember)
				{
					AddPropertyCodeActions(result, document, state);
					AddFieldCodeActions(result, document, state);
				}

				AddLocalCodeActions(result, document, state);
			}
			else
			{
				if (canGenerateMember)
				{
					AddFieldCodeActions(result, document, state);
					AddPropertyCodeActions(result, document, state);
				}

				AddLocalCodeActions(result, document, state);
			}

			return result;
		}

		protected virtual bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
		{
			return false;
		}

		private void AddPropertyCodeActions(List<CodeAction> result, Document document, State state)
		{
			if (state.IsInRefContext || state.IsInOutContext)
			{
				return;
			}

			if (state.IsConstant)
			{
				return;
			}

			if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface && state.IsStatic)
			{
				return;
			}

			result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: true, isReadonly: false, isConstant: false));

			if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface && !state.IsWrittenTo)
			{
				result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: true, isReadonly: true, isConstant: false));
			}
		}

		private void AddFieldCodeActions(List<CodeAction> result, Document document, State state)
		{
			if (state.TypeToGenerateIn.TypeKind != TypeKind.Interface)
			{
				if (state.IsConstant)
				{
					result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: false, isReadonly: false, isConstant: true));
				}
				else
				{
					result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: false, isReadonly: false, isConstant: false));

					// If we haven't written to the field, or we're in the constructor for the type
					// we're writing into, then we can generate this field read-only.
					if (!state.IsWrittenTo || state.IsInConstructor)
					{
						result.Add(new GenerateVariableCodeAction((TService)this, document, state, generateProperty: false, isReadonly: true, isConstant: false));
					}
				}
			}
		}

		private void AddLocalCodeActions(List<CodeAction> result, Document document, State state)
		{
			if (state.CanGenerateLocal())
			{
				result.Add(new GenerateLocalCodeAction((TService)this, document, state));
			}
		}

		private partial class GenerateVariableCodeAction : CodeAction
		{
			private readonly TService _service;
			private readonly State _state;
			private readonly bool _generateProperty;
			private readonly bool _isReadonly;
			private readonly bool _isConstant;
			private readonly Document _document;
			private readonly string _equivalenceKey;

			public GenerateVariableCodeAction(
				TService service,
				Document document,
				State state,
				bool generateProperty,
				bool isReadonly,
				bool isConstant)
			{
				_service = service;
				_document = document;
				_state = state;
				_generateProperty = generateProperty;
				_isReadonly = isReadonly;
				_isConstant = isConstant;
				_equivalenceKey = Title;
			}

			protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
			{
				var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
				var generateUnsafe = _state.TypeMemberType.IsUnsafe() &&
					!_state.IsContainedInUnsafeType;

				if (_generateProperty)
				{
					var getAccessor = CodeGenerationSymbolFactory.CreateAccessorSymbol(
						attributes: null,
						accessibility: DetermineMaximalAccessibility(_state),
						statements: null);
					var setAccessor = _isReadonly ? null : CodeGenerationSymbolFactory.CreateAccessorSymbol(
						attributes: null,
						accessibility: DetermineMinimalAccessibility(_state),
						statements: null);

					var result = await CodeGenerator.AddPropertyDeclarationAsync(
						_document.Project.Solution,
						_state.TypeToGenerateIn,
						CodeGenerationSymbolFactory.CreatePropertySymbol(
							attributes: null,
							accessibility: DetermineMaximalAccessibility(_state),
							modifiers: DeclarationModifiers.None.WithIsStatic(_state.IsStatic).WithIsUnsafe (generateUnsafe),
							type: _state.TypeMemberType,
							explicitInterfaceSymbol: null,
							name: _state.IdentifierToken.ValueText,
							isIndexer: _state.IsIndexer,
							parameters: _state.Parameters,
							getMethod: getAccessor,
							setMethod: setAccessor),
						new CodeGenerationOptions(contextLocation: _state.IdentifierToken.GetLocation(), generateDefaultAccessibility: false),
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);

					return result;
				}
				else
				{
					var result = await CodeGenerator.AddFieldDeclarationAsync(
						_document.Project.Solution,
						_state.TypeToGenerateIn,
						CodeGenerationSymbolFactory.CreateFieldSymbol(
							attributes: null,
							accessibility: DetermineMinimalAccessibility(_state),
							modifiers: _isConstant ?
							DeclarationModifiers.None.WithIsConst(true).WithIsUnsafe(generateUnsafe) :
							DeclarationModifiers.None.WithIsStatic(_state.IsStatic).WithIsReadOnly (_isReadonly).WithIsUnsafe(generateUnsafe),
							type: _state.TypeMemberType,
							name: _state.IdentifierToken.ValueText),
						new CodeGenerationOptions(contextLocation: _state.IdentifierToken.GetLocation(), generateDefaultAccessibility: false),
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);

					return result;
				}
			}

			private Accessibility DetermineMaximalAccessibility(State state)
			{
				if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface)
				{
					return Accessibility.NotApplicable;
				}

				var accessibility = Accessibility.Public;

				// Ensure that we're not overly exposing a type.
				var containingTypeAccessibility = state.TypeToGenerateIn.DetermineMinimalAccessibility();
				var effectiveAccessibility = CommonAccessibilityUtilities.Minimum(
					containingTypeAccessibility, accessibility);

				var returnTypeAccessibility = state.TypeMemberType.DetermineMinimalAccessibility();

				if (CommonAccessibilityUtilities.Minimum(effectiveAccessibility, returnTypeAccessibility) !=
					effectiveAccessibility)
				{
					return returnTypeAccessibility;
				}

				return accessibility;
			}

			private Accessibility DetermineMinimalAccessibility(State state)
			{
				if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface)
				{
					return Accessibility.NotApplicable;
				}

				// Otherwise, figure out what accessibility modifier to use and optionally mark
				// it as static.
				if (state.SimpleNameOrMemberAccessExpressionOpt.IsAttributeNamedArgumentIdentifier())
				{
					return Accessibility.Public;
				}
				else if (state.ContainingType.IsContainedWithin(state.TypeToGenerateIn))
				{
					return Accessibility.Private;
				}
				else if (DerivesFrom(state, state.ContainingType) && state.IsStatic)
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
					return Accessibility.Protected;
				}
				else if (state.ContainingType.ContainingAssembly.IsSameAssemblyOrHasFriendAccessTo(state.TypeToGenerateIn.ContainingAssembly))
				{
					return Accessibility.Internal;
				}
				else
				{
					// TODO: Code coverage - we need a unit-test that generates across projects
					return Accessibility.Public;
				}
			}

			private bool DerivesFrom(State state, INamedTypeSymbol containingType)
			{
				return containingType.GetBaseTypes().Select(t => t.OriginalDefinition)
					.Contains(state.TypeToGenerateIn);
			}

			public override string Title
			{
				get
				{
					var text = _isConstant
						? Resources.GenerateConstantIn
						: _generateProperty
						? _isReadonly ? Resources.GenerateReadonlyProperty : Resources.GeneratePropertyIn
						: _isReadonly ? Resources.GenerateReadonlyField : Resources.GenerateFieldIn;

					return string.Format(
						text,
						_state.IdentifierToken.ValueText,
						_state.TypeToGenerateIn.Name);
				}
			}

			public override string EquivalenceKey
			{
				get
				{
					return _equivalenceKey;
				}
			}
		}

		private class GenerateLocalCodeAction : CodeAction
		{
			private readonly TService _service;
			private readonly Document _document;
			private readonly State _state;

			public GenerateLocalCodeAction(TService service, Document document, State state)
			{
				_service = service;
				_document = document;
				_state = state;
			}

			public override string Title
			{
				get
				{
					var text = Resources.GenerateLocal;

					return string.Format(
						text,
						_state.IdentifierToken.ValueText);
				}
			}

			protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
			{
				var newRoot = GetNewRoot(cancellationToken);
				var newDocument = _document.WithSyntaxRoot(newRoot);

				return Task.FromResult(newDocument);
			}

			private SyntaxNode GetNewRoot(CancellationToken cancellationToken)
			{
				SyntaxNode newRoot;
				if (_service.TryConvertToLocalDeclaration(_state.LocalType, _state.IdentifierToken, _document.Project.Solution.Workspace.Options, out newRoot))
				{
					return newRoot;
				}

				var syntaxFactory = _document.GetLanguageService<SyntaxGenerator>();
				var initializer = _state.IsOnlyWrittenTo
					? null
					: syntaxFactory.DefaultExpression(_state.LocalType);

				var type = _state.LocalType;
				var localStatement = syntaxFactory.LocalDeclarationStatement(type, _state.IdentifierToken.ValueText, initializer);
				localStatement = localStatement.WithAdditionalAnnotations(Microsoft.CodeAnalysis.Formatting.Formatter.Annotation);

				var codeGenService = new CSharpCodeGenerationService (_document.Project.Solution.Workspace);
				var root = _state.IdentifierToken.GetAncestors<SyntaxNode>().Last();

				return codeGenService.AddStatements(
					root,
					SpecializedCollections.SingletonEnumerable(localStatement),
					options: new CodeGenerationOptions(beforeThisLocation: _state.IdentifierToken.GetLocation()),
					cancellationToken: cancellationToken);
			}
		}

		private partial class State
		{
			public INamedTypeSymbol ContainingType { get; private set; }
			public INamedTypeSymbol TypeToGenerateIn { get; private set; }
			public bool IsStatic { get; private set; }
			public bool IsConstant { get; private set; }
			public bool IsIndexer { get; private set; }
			public bool IsContainedInUnsafeType { get; private set; }
			public IList<IParameterSymbol> Parameters { get; private set; }

			// Just the name of the method.  i.e. "Foo" in "Foo" or "X.Foo"
			public SyntaxToken IdentifierToken { get; private set; }
			public TSimpleNameSyntax SimpleNameOpt { get; private set; }

			// The entire expression containing the name.  i.e. "X.Foo"
			public TExpressionSyntax SimpleNameOrMemberAccessExpressionOpt { get; private set; }

			public ITypeSymbol TypeMemberType { get; private set; }
			public ITypeSymbol LocalType { get; private set; }

			public bool IsWrittenTo { get; private set; }
			public bool IsOnlyWrittenTo { get; private set; }

			public bool IsInConstructor { get; private set; }
			public bool IsInRefContext { get; private set; }
			public bool IsInOutContext { get; private set; }
			public bool IsInMemberContext { get; private set; }

			public bool IsInExecutableBlock { get; private set; }
			public bool IsInConditionalAccessExpression { get; private set; }

			public static async Task<State> GenerateAsync(
				TService service,
				SemanticDocument document,
				SyntaxNode interfaceNode,
				CancellationToken cancellationToken)
			{
				var state = new State();
				if (!await state.TryInitializeAsync(service, document, interfaceNode, cancellationToken).ConfigureAwait(false))
				{
					return null;
				}

				return state;
			}

			private async Task<bool> TryInitializeAsync(
				TService service,
				SemanticDocument document,
				SyntaxNode node,
				CancellationToken cancellationToken)
			{
				if (service.IsIdentifierNameGeneration(node))
				{
					// Cases that we deal with currently:
					//
					// 1) expr.Foo
					// 2) expr->Foo
					// 3) Foo
					if (!TryInitializeSimpleName(service, document, (TSimpleNameSyntax)node, cancellationToken))
					{
						return false;
					}
				}
				else if (service.IsExplicitInterfaceGeneration(node))
				{
					// 4)  bool IFoo.NewProp
					if (!TryInitializeExplicitInterface(service, document, node, cancellationToken))
					{
						return false;
					}
				}
				else
				{
					return false;
				}

				// Ok.  It either didn't bind to any symbols, or it bound to a symbol but with
				// errors.  In the former case we definitely want to offer to generate a field.  In
				// the latter case, we want to generate a field *unless* there's an existing member
				// with the same name.  Note: it's ok if there's a  method with the same name.
				var existingMembers = this.TypeToGenerateIn.GetMembers(this.IdentifierToken.ValueText)
					.Where(m => m.Kind != SymbolKind.Method);
				if (existingMembers.Any())
				{
					// TODO: Code coverage
					// There was an existing method that the new method would clash with.  
					return false;
				}

				if (cancellationToken.IsCancellationRequested)
				{
					return false;
				}

				this.TypeToGenerateIn = await SymbolFinder.FindSourceDefinitionAsync(this.TypeToGenerateIn, document.Project.Solution, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;

				if (!service.ValidateTypeToGenerateIn(
					document.Project.Solution, this.TypeToGenerateIn, this.IsStatic, ClassInterfaceModuleStructTypes, cancellationToken))
				{
					return false;
				}

				this.IsContainedInUnsafeType = service.ContainingTypesOrSelfHasUnsafeKeyword(this.TypeToGenerateIn);

				return CanGenerateLocal() || CodeGenerator.CanAdd(document.Project.Solution, this.TypeToGenerateIn, cancellationToken);
			}

			internal bool CanGenerateLocal()
			{
				return !this.IsInMemberContext && this.IsInExecutableBlock;
			}

			private bool TryInitializeExplicitInterface(
				TService service,
				SemanticDocument document,
				SyntaxNode propertyDeclaration,
				CancellationToken cancellationToken)
			{
				SyntaxToken identifierToken;
				IPropertySymbol propertySymbol;
				INamedTypeSymbol typeToGenerateIn;
				if (!service.TryInitializeExplicitInterfaceState(
					document, propertyDeclaration, cancellationToken,
					out identifierToken, out propertySymbol, out typeToGenerateIn))
				{
					return false;
				}

				this.IdentifierToken = identifierToken;
				this.TypeToGenerateIn = typeToGenerateIn;

				if (propertySymbol.ExplicitInterfaceImplementations.Any())
				{
					return false;
				}

				cancellationToken.ThrowIfCancellationRequested();

				var semanticModel = document.SemanticModel;
				this.ContainingType = semanticModel.GetEnclosingNamedType(this.IdentifierToken.SpanStart, cancellationToken);
				if (this.ContainingType == null)
				{
					return false;
				}

				if (!this.ContainingType.Interfaces.OfType<INamedTypeSymbol>().Contains(this.TypeToGenerateIn))
				{
					return false;
				}

				this.IsIndexer = propertySymbol.IsIndexer;
				this.Parameters = propertySymbol.Parameters;
				this.TypeMemberType = propertySymbol.Type;

				// By default, make it readonly, unless there's already an setter defined.
				this.IsWrittenTo = propertySymbol.SetMethod != null;

				return true;
			}

			private bool TryInitializeSimpleName(
				TService service,
				SemanticDocument document,
				TSimpleNameSyntax simpleName,
				CancellationToken cancellationToken)
			{
				SyntaxToken identifierToken;
				TExpressionSyntax simpleNameOrMemberAccessExpression;
				bool isInExecutableBlock;
				bool isInConditionalAccessExpression;
				if (!service.TryInitializeIdentifierNameState(
					document, simpleName, cancellationToken,
					out identifierToken, out simpleNameOrMemberAccessExpression, out isInExecutableBlock, out isInConditionalAccessExpression))
				{
					return false;
				}

				if (string.IsNullOrWhiteSpace(identifierToken.ValueText))
				{
					return false;
				}

				this.SimpleNameOpt = simpleName;
				this.IdentifierToken = identifierToken;
				this.SimpleNameOrMemberAccessExpressionOpt = simpleNameOrMemberAccessExpression;
				this.IsInExecutableBlock = isInExecutableBlock;
				this.IsInConditionalAccessExpression = isInConditionalAccessExpression;

				// If we're in a type context then we shouldn't offer to generate a field or
				// property.
				if (SimpleNameOrMemberAccessExpressionOpt.IsInNamespaceOrTypeContext())
				{
					return false;
				}

				var expr = SimpleNameOrMemberAccessExpressionOpt as ExpressionSyntax;
				this.IsConstant = expr.IsInConstantContext();

				// If we're not in a type, don't even bother.  NOTE(cyrusn): We'll have to rethink this
				// for C# Script.
				cancellationToken.ThrowIfCancellationRequested();
				var semanticModel = document.SemanticModel;
				this.ContainingType = semanticModel.GetEnclosingNamedType(this.IdentifierToken.SpanStart, cancellationToken);
				if (this.ContainingType == null)
				{
					return false;
				}

				// Now, try to bind the invocation and see if it succeeds or not.  if it succeeds and
				// binds uniquely, then we don't need to offer this quick fix.
				cancellationToken.ThrowIfCancellationRequested();
				var semanticInfo = semanticModel.GetSymbolInfo(this.SimpleNameOrMemberAccessExpressionOpt, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested();
				if (semanticInfo.Symbol != null)
				{
					return false;
				}

				// Either we found no matches, or this was ambiguous. Either way, we might be able
				// to generate a method here.  Determine where the user wants to generate the method
				// into, and if it's valid then proceed.
				cancellationToken.ThrowIfCancellationRequested();
				INamedTypeSymbol typeToGenerateIn;
				bool isStatic;
				if (!service.TryDetermineTypeToGenerateIn(document, this.ContainingType, this.SimpleNameOrMemberAccessExpressionOpt, cancellationToken,
					out typeToGenerateIn, out isStatic))
				{
					return false;
				}

				this.TypeToGenerateIn = typeToGenerateIn;
				this.IsStatic = isStatic;

				DetermineFieldType(document, cancellationToken);

				this.IsInRefContext = expr.IsInRefContext();
				this.IsInOutContext = expr.IsInOutContext();
				this.IsWrittenTo = expr.IsWrittenTo();
				this.IsOnlyWrittenTo = expr.IsOnlyWrittenTo();
				this.IsInConstructor = DetermineIsInConstructor(document);
				this.IsInMemberContext = this.SimpleNameOpt != this.SimpleNameOrMemberAccessExpressionOpt ||
					expr.IsObjectInitializerNamedAssignmentIdentifier();
				return true;
			}

			private void DetermineFieldType(
				SemanticDocument document,
				CancellationToken cancellationToken)
			{
				var inferredType = TypeGuessing.typeInferenceService.InferType(
					document.SemanticModel, this.SimpleNameOrMemberAccessExpressionOpt,
					objectAsDefault: true,
					cancellationToken: cancellationToken);
				inferredType = inferredType.SpecialType == SpecialType.System_Void
					? document.SemanticModel.Compilation.ObjectType
					: inferredType;

				if (this.IsInConditionalAccessExpression)
				{
					inferredType = inferredType.RemoveNullableIfPresent();
				}

				// Substitute 'object' for all captured method type parameters.  Note: we may need to
				// do this for things like anonymous types, as well as captured type parameters that
				// aren't in scope in the destination type.
				var capturedMethodTypeParameters = inferredType.GetReferencedMethodTypeParameters();
				var mapping = capturedMethodTypeParameters.ToDictionary(tp => tp,
					tp => document.SemanticModel.Compilation.ObjectType);

				this.TypeMemberType = inferredType.SubstituteTypes(mapping, document.SemanticModel.Compilation);
				var availableTypeParameters = this.TypeToGenerateIn.GetAllTypeParameters();
				this.TypeMemberType = TypeMemberType.RemoveUnavailableTypeParameters(
					document.SemanticModel.Compilation, availableTypeParameters);

				var enclosingMethodSymbol = document.SemanticModel.GetEnclosingSymbol<IMethodSymbol>(this.SimpleNameOrMemberAccessExpressionOpt.SpanStart, cancellationToken);
				if (enclosingMethodSymbol != null && enclosingMethodSymbol.TypeParameters != null && enclosingMethodSymbol.TypeParameters.Count() != 0)
				{
					var combinedTypeParameters = new List<ITypeParameterSymbol>();
					combinedTypeParameters.AddRange(availableTypeParameters);
					combinedTypeParameters.AddRange(enclosingMethodSymbol.TypeParameters);
					this.LocalType = inferredType.RemoveUnavailableTypeParameters(
						document.SemanticModel.Compilation, combinedTypeParameters);
				}
				else
				{
					this.LocalType = this.TypeMemberType;
				}
			}

			private bool DetermineIsInConstructor(SemanticDocument document)
			{
				if (!this.ContainingType.OriginalDefinition.Equals(this.TypeToGenerateIn.OriginalDefinition))
				{
					return false;
				}

				return SimpleNameOpt.IsInConstructor();
			}
		}
	}
}
