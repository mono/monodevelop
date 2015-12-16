// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using ICSharpCode.NRefactory6.CSharp.CodeGeneration;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.GenerateFromMembers.GenerateConstructor
{
	public abstract partial class AbstractGenerateConstructorService<TService, TMemberDeclarationSyntax> :
	AbstractGenerateFromMembersService<TMemberDeclarationSyntax>
		where TService : AbstractGenerateConstructorService<TService, TMemberDeclarationSyntax>
		where TMemberDeclarationSyntax : SyntaxNode
	{
		protected AbstractGenerateConstructorService()
		{
		}

		public async Task<GenerateConstructorResult> GenerateConstructorAsync(
			Document document, TextSpan textSpan, CancellationToken cancellationToken)
		{
//			using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_GenerateConstructor, cancellationToken))
//			{
			var info = await GetSelectedMemberInfoAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
			if (info != null)
			{
				var state = State.Generate((TService)this, document, textSpan, info.ContainingType, info.SelectedMembers, cancellationToken);
				if (state != null)
				{
					return new GenerateConstructorResult(
						CreateCodeRefactoring(info.SelectedDeclarations, GetCodeActions(document, state)));
				}
			}

			return GenerateConstructorResult.Failure;
//			}
		}

		private IEnumerable<CodeAction> GetCodeActions(Document document, State state)
		{
			yield return new FieldDelegatingCodeAction((TService)this, document, state);
			if (state.DelegatedConstructor != null)
			{
				yield return new ConstructorDelegatingCodeAction((TService)this, document, state);
			}
		}

		private class ConstructorDelegatingCodeAction : CodeAction
		{
			private readonly TService _service;
			private readonly Document _document;
			private readonly State _state;

			public ConstructorDelegatingCodeAction(
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
				// First, see if there are any constructors that would take the first 'n' arguments
				// we've provided.  If so, delegate to those, and then create a field for any
				// remaining arguments.  Try to match from largest to smallest.
				//
				// Otherwise, just generate a normal constructor that assigns any provided
				// parameters into fields.
				var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.ContainingType.Language);
				var factory = provider.GetService<SyntaxGenerator>();

				var thisConstructorArguments = _state.DelegatedConstructor.Parameters.Select (par => factory.Argument (par.RefKind, SyntaxFactory.IdentifierName (par.Name))).ToList ();
				var statements = new List<SyntaxNode>();

				for (var i = _state.DelegatedConstructor.Parameters.Length; i < _state.Parameters.Count; i++)
				{
					var symbolName = _state.SelectedMembers[i].Name;
					var parameterName = _state.Parameters[i].Name;
					var assignExpression = factory.AssignmentStatement(
						factory.MemberAccessExpression(
							factory.ThisExpression(),
							factory.IdentifierName(symbolName)),
						factory.IdentifierName(parameterName));

					var expressionStatement = factory.ExpressionStatement(assignExpression);
					statements.Add(expressionStatement);
				}

				var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
				var codeGenerationService = new CSharpCodeGenerationService (_document.Project.Solution.Workspace.Services.GetLanguageServices (LanguageNames.CSharp));
				var result = await codeGenerationService.AddMethodAsync(
					_document.Project.Solution,
					_state.ContainingType,
					CodeGenerationSymbolFactory.CreateConstructorSymbol(
						attributes: null,
						accessibility: Accessibility.Public,
						modifiers: new DeclarationModifiers(),
						typeName: _state.ContainingType.Name,
						parameters: _state.Parameters,
						statements: statements,
						thisConstructorArguments: thisConstructorArguments),
					new CodeGenerationOptions(contextLocation: syntaxTree.GetLocation(_state.TextSpan), generateDefaultAccessibility: false),
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);

				return result;
			}

			public override string Title
			{
				get
				{
				//	var symbolDisplayService = _document.GetLanguageService<ISymbolDisplayService>();
					var parameters = _state.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
					var parameterString = string.Join(", ", parameters);

					return string.Format(Resources.GenerateDelegatingConstructor,
						_state.ContainingType.Name, parameterString);
				}
			}
		}

		private class FieldDelegatingCodeAction : CodeAction
		{
			private readonly TService _service;
			private readonly Document _document;
			private readonly State _state;

			public FieldDelegatingCodeAction(
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
				// First, see if there are any constructors that would take the first 'n' arguments
				// we've provided.  If so, delegate to those, and then create a field for any
				// remaining arguments.  Try to match from largest to smallest.
				//
				// Otherwise, just generate a normal constructor that assigns any provided
				// parameters into fields.
				var parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
				for (int i = 0; i < _state.Parameters.Count; i++)
				{
					parameterToExistingFieldMap[_state.Parameters[i].Name] = _state.SelectedMembers[i];
				}

				var factory = _document.GetLanguageService<SyntaxGenerator>();

				var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
				var members = factory.CreateFieldDelegatingConstructor(
					_state.ContainingType.Name,
					_state.ContainingType,
					_state.Parameters,
					parameterToExistingFieldMap,
					parameterToNewFieldMap: null,
					cancellationToken: cancellationToken);
				var codeGenerationService = new CSharpCodeGenerationService (_document.Project.Solution.Workspace.Services.GetLanguageServices (LanguageNames.CSharp));

				var result = await codeGenerationService.AddMembersAsync(
					_document.Project.Solution,
					_state.ContainingType,
					members,
					new CodeGenerationOptions(contextLocation: syntaxTree.GetLocation(_state.TextSpan), generateDefaultAccessibility: false),
					cancellationToken)
					.ConfigureAwait(false);

				return result;
			}


			public override string Title
			{
				get
				{
					var parameters = _state.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
					var parameterString = string.Join(", ", parameters);

					if (_state.DelegatedConstructor == null)
					{
						return string.Format(Resources.GenerateConstructor,
							_state.ContainingType.Name, parameterString);
					}
					else
					{
						return string.Format(Resources.GenerateFieldAssigningConstructor,
							_state.ContainingType.Name, parameterString);
					}
				}
			}
		}


		private class State
		{
			public TextSpan TextSpan { get; private set; }
			public IMethodSymbol DelegatedConstructor { get; private set; }
			public INamedTypeSymbol ContainingType { get; private set; }
			public IList<ISymbol> SelectedMembers { get; private set; }
			public List<IParameterSymbol> Parameters { get; private set; }

			public static State Generate(
				TService service,
				Document document,
				TextSpan textSpan,
				INamedTypeSymbol containingType,
				IList<ISymbol> selectedMembers,
				CancellationToken cancellationToken)
			{
				var state = new State();
				if (!state.TryInitialize(service, document, textSpan, containingType, selectedMembers, cancellationToken))
				{
					return null;
				}

				return state;
			}

			private bool TryInitialize(
				TService service,
				Document document,
				TextSpan textSpan,
				INamedTypeSymbol containingType,
				IList<ISymbol> selectedMembers,
				CancellationToken cancellationToken)
			{
				if (!selectedMembers.All(IsWritableInstanceFieldOrProperty))
				{
					return false;
				}

				this.SelectedMembers = selectedMembers;
				this.ContainingType = containingType;
				this.TextSpan = textSpan;
				if (this.ContainingType == null || this.ContainingType.TypeKind == TypeKind.Interface)
				{
					return false;
				}

				this.Parameters = service.DetermineParameters(selectedMembers);

				if (service.HasMatchingConstructor(this.ContainingType, this.Parameters))
				{
					return false;
				}

				this.DelegatedConstructor = service.GetDelegatedConstructor(this.ContainingType, this.Parameters);
				return true;
			}
		}
	}
}
