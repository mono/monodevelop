// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Editing;
using ICSharpCode.NRefactory6.CSharp.CodeGeneration;
using System;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.GenerateMember.GenerateConstructor
{
	public abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
		where TService : AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
		where TArgumentSyntax : SyntaxNode
		where TAttributeArgumentSyntax : SyntaxNode
	{

		protected AbstractGenerateConstructorService()
		{
		}

		protected abstract bool IsSimpleNameGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);
		protected abstract bool IsConstructorInitializerGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken);

		protected abstract bool TryInitializeConstructorInitializerGeneration(SemanticDocument document, SyntaxNode constructorInitializer, CancellationToken cancellationToken, out SyntaxToken token, out IList<TArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn);
		protected abstract bool TryInitializeSimpleNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out IList<TArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn);
		protected abstract bool TryInitializeSimpleAttributeNameGenerationState(SemanticDocument document, SyntaxNode simpleName, CancellationToken cancellationToken, out SyntaxToken token, out IList<TArgumentSyntax> arguments, out IList<TAttributeArgumentSyntax> attributeArguments, out INamedTypeSymbol typeToGenerateIn);

		protected abstract IList<string> GenerateParameterNames(SemanticModel semanticModel, IEnumerable<TArgumentSyntax> arguments, IList<string> reservedNames = null);
		protected virtual IList<string> GenerateParameterNames(SemanticModel semanticModel, IEnumerable<TAttributeArgumentSyntax> arguments, IList<string> reservedNames = null) { return null; }
		protected abstract string GenerateNameForArgument(SemanticModel semanticModel, TArgumentSyntax argument);
		protected virtual string GenerateNameForArgument(SemanticModel semanticModel, TAttributeArgumentSyntax argument) { return null; }
		protected abstract RefKind GetRefKind(TArgumentSyntax argument);
		protected abstract bool IsNamedArgument(TArgumentSyntax argument);
		protected abstract ITypeSymbol GetArgumentType(SemanticModel semanticModel, TArgumentSyntax argument, CancellationToken cancellationToken);
		protected virtual ITypeSymbol GetAttributeArgumentType(SemanticModel semanticModel, TAttributeArgumentSyntax argument, CancellationToken cancellationToken) { return null; }

		public async Task<IEnumerable<CodeAction>> GenerateConstructorAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

			var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
			if (state == null)
			{
				return SpecializedCollections.EmptyEnumerable<CodeAction>();
			}

			return GetActions(document, state);
		}

		private IEnumerable<CodeAction> GetActions(Document document, State state)
		{
			yield return new GenerateConstructorCodeAction((TService)this, document, state);
		}

		private class GenerateConstructorCodeAction : CodeAction
		{
			private readonly State _state;
			private readonly Document _document;
			private readonly TService _service;

			public GenerateConstructorCodeAction(
				TService service,
				Document document,
				State state)
			{
				_service = service;
				_document = document;
				_state = state;
			}

			protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
			{
				var semanticDocument = await SemanticDocument.CreateAsync(_document, cancellationToken).ConfigureAwait(false);
				var editor = new Editor(_service, semanticDocument, _state, cancellationToken);
				return await editor.GetEditAsync().ConfigureAwait(false);
			}

			public override string Title
			{
				get
				{
					return string.Format(Resources.GenerateNewConstructorIn,
						_state.TypeToGenerateIn.Name);
				}
			}
		}

		protected abstract bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);

		internal abstract IMethodSymbol GetDelegatingConstructor(State state, SemanticDocument document, int argumentCount, INamedTypeSymbol namedType, ISet<IMethodSymbol> candidates, CancellationToken cancellationToken);

		private partial class Editor
		{
			private readonly TService _service;
			private readonly SemanticDocument _document;
			private readonly State _state;
			private readonly CancellationToken _cancellationToken;

			public Editor(
				TService service,
				SemanticDocument document,
				State state,
				CancellationToken cancellationToken)
			{
				_service = service;
				_document = document;
				_state = state;
				_cancellationToken = cancellationToken;
			}

			internal async Task<Document> GetEditAsync()
			{
				// First, see if there's an accessible base constructor that would accept these
				// types, then just call into that instead of generating fields.
				//
				// then, see if there are any constructors that would take the first 'n' arguments
				// we've provided.  If so, delegate to those, and then create a field for any
				// remaining arguments.  Try to match from largest to smallest.
				//
				// Otherwise, just generate a normal constructor that assigns any provided
				// parameters into fields.

				var edit = await GenerateThisOrBaseDelegatingConstructorAsync().ConfigureAwait(false);
				if (edit != null)
				{
					return edit;
				}

				return await GenerateFieldDelegatingConstructorAsync().ConfigureAwait(false);
			}

			private async Task<Document> GenerateThisOrBaseDelegatingConstructorAsync()
			{
				// We don't have to deal with the zero length case, since there's nothing to
				// delegate.  It will fall out of the GenerateFieldDelegatingConstructor above.
				for (int i = _state.Arguments.Count; i >= 1; i--)
				{
					var edit = await GenerateThisOrBaseDelegatingConstructorAsync(i).ConfigureAwait(false);
					if (edit != null)
					{
						return edit;
					}
				}

				return null;
			}

			private async Task<Document> GenerateThisOrBaseDelegatingConstructorAsync(int argumentCount)
			{
				Document edit;
				if ((edit = await GenerateDelegatingConstructorAsync(argumentCount, _state.TypeToGenerateIn).ConfigureAwait(false)) != null ||
					(edit = await GenerateDelegatingConstructorAsync(argumentCount, _state.TypeToGenerateIn.BaseType).ConfigureAwait(false)) != null)
				{
					return edit;
				}

				return null;
			}

			private async Task<Document> GenerateDelegatingConstructorAsync(
				int argumentCount,
				INamedTypeSymbol namedType)
			{
				if (namedType == null)
				{
					return null;
				}

				// We can't resolve overloads across language.
				if (_document.Project.Language != namedType.Language)
				{
					return null;
				}

				var arguments = _state.Arguments.Take(argumentCount).ToList();
				var remainingArguments = _state.Arguments.Skip(argumentCount).ToList();
				var remainingAttributeArguments = _state.AttributeArguments != null ? _state.AttributeArguments.Skip(argumentCount).ToList() : null;
				var remainingParameterTypes = _state.ParameterTypes.Skip(argumentCount).ToList();

				var instanceConstructors = namedType.InstanceConstructors.Where(IsSymbolAccessible).ToSet();
				if (instanceConstructors.IsEmpty())
				{
					return null;
				}

				var delegatedConstructor = _service.GetDelegatingConstructor(_state, _document, argumentCount, namedType, instanceConstructors, _cancellationToken);
				if (delegatedConstructor == null)
				{
					return null;
				}

				// There was a best match.  Call it directly.  
				var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
				var syntaxFactory = provider.GetService<SyntaxGenerator>();
				var codeGenerationService = new CSharpCodeGenerationService (_document.Project.Solution.Workspace);

				// Map the first N parameters to the other constructor in this type.  Then
				// try to map any further parameters to existing fields.  Finally, generate
				// new fields if no such parameters exist.

				// Find the names of the parameters that will follow the parameters we're
				// delegating.
				var remainingParameterNames = _service.GenerateParameterNames(
					_document.SemanticModel, remainingArguments, delegatedConstructor.Parameters.Select(p => p.Name).ToList());

				// Can't generate the constructor if the parameter names we're copying over forcibly
				// conflict with any names we generated.
				if (delegatedConstructor.Parameters.Select(p => p.Name).Intersect(remainingParameterNames).Any())
				{
					return null;
				}

				// Try to map those parameters to fields.
				Dictionary<string, ISymbol> parameterToExistingFieldMap;
				Dictionary<string, string> parameterToNewFieldMap;
				List<IParameterSymbol> remainingParameters;
				this.GetParameters(remainingArguments, remainingAttributeArguments, remainingParameterTypes, remainingParameterNames, out parameterToExistingFieldMap, out parameterToNewFieldMap, out remainingParameters);

				var fields = syntaxFactory.CreateFieldsForParameters(remainingParameters, parameterToNewFieldMap);
				var assignStatements = syntaxFactory.CreateAssignmentStatements(remainingParameters, parameterToExistingFieldMap, parameterToNewFieldMap);

				var allParameters = delegatedConstructor.Parameters.Concat(remainingParameters).ToList();

				var isThis = namedType.Equals(_state.TypeToGenerateIn);
				var delegatingArguments = syntaxFactory.CreateArguments(delegatedConstructor.Parameters);
				var baseConstructorArguments = isThis ? null : delegatingArguments;
				var thisConstructorArguments = isThis ? delegatingArguments : null;

				var constructor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
					attributes: null,
					accessibility: Accessibility.Public,
					modifiers: default(DeclarationModifiers),
					typeName: _state.TypeToGenerateIn.Name,
					parameters: allParameters,
					statements: assignStatements.ToList(),
					baseConstructorArguments: baseConstructorArguments,
					thisConstructorArguments: thisConstructorArguments);

				var members = new List<ISymbol>(fields) { constructor };
				var result = await codeGenerationService.AddMembersAsync(
					_document.Project.Solution,
					_state.TypeToGenerateIn,
					members,
					new CodeGenerationOptions(_state.Token.GetLocation(), generateDefaultAccessibility: false),
					_cancellationToken)
					.ConfigureAwait(false);

				return result;
			}

			private async Task<Document> GenerateFieldDelegatingConstructorAsync()
			{
				var arguments = _state.Arguments.ToList();
				var parameterTypes = _state.ParameterTypes;

				var typeParametersNames = _state.TypeToGenerateIn.GetAllTypeParameters().Select(t => t.Name).ToList();
				var parameterNames = _state.AttributeArguments != null
					? _service.GenerateParameterNames(_document.SemanticModel, _state.AttributeArguments, typeParametersNames)
					: _service.GenerateParameterNames(_document.SemanticModel, arguments, typeParametersNames);

				Dictionary<string, ISymbol> parameterToExistingFieldMap;
				Dictionary<string, string> parameterToNewFieldMap;
				List<IParameterSymbol> parameters;
				GetParameters(arguments, _state.AttributeArguments, parameterTypes, parameterNames, out parameterToExistingFieldMap, out parameterToNewFieldMap, out parameters);

				var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
				var syntaxFactory = provider.GetService<SyntaxGenerator>();
				var codeGenerationService = new CSharpCodeGenerationService (_document.Project.Solution.Workspace);

				var syntaxTree = _document.SyntaxTree;
				var members = syntaxFactory.CreateFieldDelegatingConstructor(
					_state.TypeToGenerateIn.Name, _state.TypeToGenerateIn, parameters,
					parameterToExistingFieldMap, parameterToNewFieldMap, _cancellationToken);

				var result = await codeGenerationService.AddMembersAsync(
					_document.Project.Solution,
					_state.TypeToGenerateIn,
					members,
					new CodeGenerationOptions(_state.Token.GetLocation(), generateDefaultAccessibility: false),
					_cancellationToken)
					.ConfigureAwait(false);

				return result;
			}

			private void GetParameters(
				IList<TArgumentSyntax> arguments,
				IList<TAttributeArgumentSyntax> attributeArguments,
				IList<ITypeSymbol> parameterTypes,
				IList<string> parameterNames,
				out Dictionary<string, ISymbol> parameterToExistingFieldMap,
				out Dictionary<string, string> parameterToNewFieldMap,
				out List<IParameterSymbol> parameters)
			{
				parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
				parameterToNewFieldMap = new Dictionary<string, string>();
				parameters = new List<IParameterSymbol>();

				for (var i = 0; i < parameterNames.Count; i++)
				{
					// See if there's a matching field we can use.  First test in a case sensitive
					// manner, then case insensitively.
					if (!TryFindMatchingField(arguments, attributeArguments, parameterNames, parameterTypes, i, parameterToExistingFieldMap, parameterToNewFieldMap, caseSentitive: true))
					{
						if (!TryFindMatchingField(arguments, attributeArguments, parameterNames, parameterTypes, i, parameterToExistingFieldMap, parameterToNewFieldMap, caseSentitive: false))
						{
							parameterToNewFieldMap[parameterNames[i]] = parameterNames[i];
						}
					}

					parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
						attributes: null,
						refKind: _service.GetRefKind(arguments[i]),
						isParams: false,
						type: parameterTypes[i],
						name: parameterNames[i]));
				}
			}

			private bool TryFindMatchingField(
				IList<TArgumentSyntax> arguments,
				IList<TAttributeArgumentSyntax> attributeArguments,
				IList<string> parameterNames,
				IList<ITypeSymbol> parameterTypes,
				int index,
				Dictionary<string, ISymbol> parameterToExistingFieldMap,
				Dictionary<string, string> parameterToNewFieldMap,
				bool caseSentitive)
			{
				var parameterName = parameterNames[index];
				var parameterType = parameterTypes[index];
				var isFixed = _service.IsNamedArgument(arguments[index]);

				// For non-out parameters, see if there's already a field there with the same name.
				// If so, and it has a compatible type, then we can just assign to that field.
				// Otherwise, we'll need to choose a different name for this member so that it
				// doesn't conflict with something already in the type. First check the current type
				// for a matching field.  If so, defer to it.
				var comparison = caseSentitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

				foreach (var type in _state.TypeToGenerateIn.GetBaseTypesAndThis())
				{
					var ignoreAccessibility = type.Equals(_state.TypeToGenerateIn);
					var symbol = type.GetMembers()
						.FirstOrDefault(s => s.Name.Equals(parameterName, comparison));

					if (symbol != null)
					{
						if (ignoreAccessibility || IsSymbolAccessible(symbol))
						{
							if (IsViableFieldOrProperty(parameterType, symbol))
							{
								// Ok!  We can just the existing field.  
								parameterToExistingFieldMap[parameterName] = symbol;
							}
							else
							{
								// Uh-oh.  Now we have a problem.  We can't assign this parameter to
								// this field.  So we need to create a new field.  Find a name not in
								// use so we can assign to that.  
								var newFieldName = NameGenerator.EnsureUniqueness(
									attributeArguments != null ?
									_service.GenerateNameForArgument(_document.SemanticModel, attributeArguments[index]) :
									_service.GenerateNameForArgument(_document.SemanticModel, arguments[index]),
									GetUnavailableMemberNames().Concat(parameterToNewFieldMap.Values));

								if (isFixed)
								{
									// Can't change the parameter name, so map the existing parameter
									// name to the new field name.
									parameterToNewFieldMap[parameterName] = newFieldName;
								}
								else
								{
									// Can change the parameter name, so do so.
									parameterNames[index] = newFieldName;
									parameterToNewFieldMap[newFieldName] = newFieldName;
								}
							}

							return true;
						}
					}
				}

				return false;
			}

			private IEnumerable<string> GetUnavailableMemberNames()
			{
				return _state.TypeToGenerateIn.MemberNames.Concat(
					from type in _state.TypeToGenerateIn.GetBaseTypes()
					from member in type.GetMembers()
					select member.Name);
			}

			private bool IsViableFieldOrProperty(
				ITypeSymbol parameterType,
				ISymbol symbol)
			{
				if (parameterType.Language != symbol.Language)
				{
					return false;
				}

				if (symbol != null && !symbol.IsStatic)
				{
					if (symbol is IFieldSymbol)
					{
						var field = (IFieldSymbol)symbol;
						return
							!field.IsConst &&
							_service.IsConversionImplicit(_document.SemanticModel.Compilation, parameterType, field.Type);
					}
					else if (symbol is IPropertySymbol)
					{
						var property = (IPropertySymbol)symbol;
						return
							property.Parameters.Length == 0 &&
							property.SetMethod != null &&
							_service.IsConversionImplicit(_document.SemanticModel.Compilation, parameterType, property.Type);
					}
				}

				return false;
			}

			private bool IsSymbolAccessible(
				ISymbol symbol)
			{
				if (symbol == null)
				{
					return false;
				}

				if (symbol.Kind == SymbolKind.Property)
				{
					if (!IsSymbolAccessible(((IPropertySymbol)symbol).SetMethod))
					{
						return false;
					}
				}

				// Public and protected constructors are accessible.  Internal constructors are
				// accessible if we have friend access.  We can't call the normal accessibility
				// checkers since they will think that a protected constructor isn't accessible
				// (since we don't have the destination type that would have access to them yet).
				switch (symbol.DeclaredAccessibility)
				{
				case Accessibility.ProtectedOrInternal:
				case Accessibility.Protected:
				case Accessibility.Public:
					return true;
				case Accessibility.ProtectedAndInternal:
				case Accessibility.Internal:
					return _document.SemanticModel.Compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(
						symbol.ContainingAssembly);

				default:
					return false;
				}
			}
		}

		protected internal class State
		{
			public IList<TArgumentSyntax> Arguments { get; private set; }

			public IList<TAttributeArgumentSyntax> AttributeArguments { get; private set; }

			// The type we're creating a constructor for.  Will be a class or struct type.
			public INamedTypeSymbol TypeToGenerateIn { get; private set; }

			public IList<ITypeSymbol> ParameterTypes { get; private set; }
			public IList<RefKind> ParameterRefKinds { get; private set; }

			public SyntaxToken Token { get; private set; }
			public bool IsConstructorInitializerGeneration { get; private set; }

			private State()
			{
				this.IsConstructorInitializerGeneration = false;
			}

			public static async Task<State> GenerateAsync(
				TService service,
				SemanticDocument document,
				SyntaxNode node,
				CancellationToken cancellationToken)
			{
				var state = new State();
				if (!await state.TryInitializeAsync(service, document, node, cancellationToken).ConfigureAwait(false))
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
				if (service.IsConstructorInitializerGeneration(document, node, cancellationToken))
				{
					if (!await TryInitializeConstructorInitializerGenerationAsync(service, document, node, cancellationToken).ConfigureAwait(false))
					{
						return false;
					}
				}
				else if (service.IsSimpleNameGeneration(document, node, cancellationToken))
				{
					if (!await TryInitializeSimpleNameGenerationAsync(service, document, node, cancellationToken).ConfigureAwait(false))
					{
						return false;
					}
				}
				else
				{
					return false;
				}

				if (!new CSharpCodeGenerationService (document.Project.Solution.Workspace).CanAddTo(this.TypeToGenerateIn, document.Project.Solution, cancellationToken))
				{
					return false;
				}

				this.ParameterTypes = this.ParameterTypes ?? GetParameterTypes(service, document, cancellationToken);
				this.ParameterRefKinds = this.Arguments.Select(service.GetRefKind).ToList();

				return !ClashesWithExistingConstructor(service, document, cancellationToken);
			}

			private bool ClashesWithExistingConstructor(TService service, SemanticDocument document, CancellationToken cancellationToken)
			{
				var parameters = this.ParameterTypes.Zip(this.ParameterRefKinds, (t, r) => CodeGenerationSymbolFactory.CreateParameterSymbol(
					attributes: null,
					refKind: r,
					isParams: false,
					type: t,
					name: string.Empty)).ToList();

				var destinationProvider = document.Project.Solution.Workspace.Services.GetLanguageServices(this.TypeToGenerateIn.Language);

				return this.TypeToGenerateIn.InstanceConstructors.Any(c => SignatureComparer.HaveSameSignature(parameters, c.Parameters, compareParameterName: true, isCaseSensitive: true));
			}

			internal List<ITypeSymbol> GetParameterTypes(
				TService service,
				SemanticDocument document,
				CancellationToken cancellationToken)
			{
				var allTypeParameters = this.TypeToGenerateIn.GetAllTypeParameters();
				var semanticModel = document.SemanticModel;
				var allTypes = this.AttributeArguments != null
					? this.AttributeArguments.Select(a => service.GetAttributeArgumentType(semanticModel, a, cancellationToken))
					: this.Arguments.Select(a => service.GetArgumentType(semanticModel, a, cancellationToken));

				return allTypes.Select(t => FixType(t, semanticModel, allTypeParameters)).ToList();
			}

			private ITypeSymbol FixType(ITypeSymbol typeSymbol, SemanticModel semanticModel, IEnumerable<ITypeParameterSymbol> allTypeParameters)
			{
				var compilation = semanticModel.Compilation;
				return typeSymbol.RemoveAnonymousTypes(compilation)
					.RemoveUnavailableTypeParameters(compilation, allTypeParameters)
					.RemoveUnnamedErrorTypes(compilation);
			}

			private async Task<bool> TryInitializeConstructorInitializerGenerationAsync(
				TService service,
				SemanticDocument document,
				SyntaxNode constructorInitializer,
				CancellationToken cancellationToken)
			{
				SyntaxToken token;
				IList<TArgumentSyntax> arguments;
				INamedTypeSymbol typeToGenerateIn;
				if (!service.TryInitializeConstructorInitializerGeneration(document, constructorInitializer, cancellationToken,
					out token, out arguments, out typeToGenerateIn))
				{
					return false;
				}

				this.Token = token;
				this.Arguments = arguments;
				this.IsConstructorInitializerGeneration = true;

				var semanticModel = document.SemanticModel;
				var semanticInfo = semanticModel.GetSymbolInfo(constructorInitializer, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested();
				if (semanticInfo.Symbol != null)
				{
					return false;
				}

				return await TryDetermineTypeToGenerateInAsync(document, typeToGenerateIn, cancellationToken).ConfigureAwait(false);
			}

			private async Task<bool> TryInitializeSimpleNameGenerationAsync(
				TService service,
				SemanticDocument document,
				SyntaxNode simpleName,
				CancellationToken cancellationToken)
			{
				SyntaxToken token;
				IList<TArgumentSyntax> arguments;
				IList<TAttributeArgumentSyntax> attributeArguments;
				INamedTypeSymbol typeToGenerateIn;
				if (service.TryInitializeSimpleNameGenerationState(document, simpleName, cancellationToken,
					out token, out arguments, out typeToGenerateIn))
				{
					this.Token = token;
					this.Arguments = arguments;
				}
				else if (service.TryInitializeSimpleAttributeNameGenerationState(document, simpleName, cancellationToken,
					out token, out arguments, out attributeArguments, out typeToGenerateIn))
				{
					this.Token = token;
					this.AttributeArguments = attributeArguments;
					this.Arguments = arguments;

					//// Attribute parameters are restricted to be constant values (simple types or string, etc).
					if (this.AttributeArguments != null && GetParameterTypes(service, document, cancellationToken).Any(t => !IsValidAttributeParameterType(t)))
					{
						return false;
					}
					else if (GetParameterTypes(service, document, cancellationToken).Any(t => !IsValidAttributeParameterType(t)))
					{
						return false;
					}
				}
				else
				{
					return false;
				}

				cancellationToken.ThrowIfCancellationRequested();

				return await TryDetermineTypeToGenerateInAsync(document, typeToGenerateIn, cancellationToken).ConfigureAwait(false);
			}

			private bool IsValidAttributeParameterType(ITypeSymbol type)
			{
				if (type.Kind == SymbolKind.ArrayType)
				{
					var arrayType = (IArrayTypeSymbol)type;
					if (arrayType.Rank != 1)
					{
						return false;
					}

					type = arrayType.ElementType;
				}

				if (type.IsEnumType())
				{
					return true;
				}

				switch (type.SpecialType)
				{
				case SpecialType.System_Boolean:
				case SpecialType.System_Byte:
				case SpecialType.System_Char:
				case SpecialType.System_Int16:
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
				case SpecialType.System_Double:
				case SpecialType.System_Single:
				case SpecialType.System_String:
					return true;

				default:
					return false;
				}
			}

			private async Task<bool> TryDetermineTypeToGenerateInAsync(
				SemanticDocument document,
				INamedTypeSymbol original,
				CancellationToken cancellationToken)
			{
				var definition = await SymbolFinder.FindSourceDefinitionAsync(original, document.Project.Solution, cancellationToken).ConfigureAwait(false);
				this.TypeToGenerateIn = definition as INamedTypeSymbol;

				return this.TypeToGenerateIn != null &&
					(this.TypeToGenerateIn.TypeKind == TypeKind.Class ||
						this.TypeToGenerateIn.TypeKind == TypeKind.Struct);
			}
		}
	}
}
