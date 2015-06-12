// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp.GenerateMember.GenerateEnumMember
{
	public abstract class AbstractGenerateEnumMemberService<TService, TSimpleNameSyntax, TExpressionSyntax> :
	AbstractGenerateMemberService<TSimpleNameSyntax, TExpressionSyntax>
		where TService : AbstractGenerateEnumMemberService<TService, TSimpleNameSyntax, TExpressionSyntax>
		where TSimpleNameSyntax : TExpressionSyntax
		where TExpressionSyntax : SyntaxNode
	{
		protected AbstractGenerateEnumMemberService()
		{
		}

		protected abstract bool IsIdentifierNameGeneration(SyntaxNode node);
		protected abstract bool TryInitializeIdentifierNameState(SemanticDocument document, TSimpleNameSyntax identifierName, CancellationToken cancellationToken, out SyntaxToken identifierToken, out TExpressionSyntax simpleNameOrMemberAccessExpression);

		public async Task<IEnumerable<CodeAction>> GenerateEnumMemberAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
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
			yield return new GenerateEnumMemberCodeAction((TService)this, document, state);
		}

		private partial class GenerateEnumMemberCodeAction : CodeAction
		{
			private readonly TService _service;
			private readonly Document _document;
			private readonly State _state;

			public GenerateEnumMemberCodeAction(
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
				var languageServices = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);

				var value = _state.TypeToGenerateIn.LastEnumValueHasInitializer()
					? EnumValueUtilities.GetNextEnumValue(_state.TypeToGenerateIn, cancellationToken)
					: null;

				var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
				var result = await new CSharpCodeGenerationService (_document.Project.Solution.Workspace).AddFieldAsync(
					_document.Project.Solution,
					_state.TypeToGenerateIn,
					CodeGenerationSymbolFactory.CreateFieldSymbol(
						attributes: null,
						accessibility: Accessibility.Public,
						modifiers: default(DeclarationModifiers),
						type: _state.TypeToGenerateIn,
						name: _state.IdentifierToken.ValueText,
						hasConstantValue: value != null,
						constantValue: value),
					new CodeGenerationOptions(contextLocation: _state.IdentifierToken.GetLocation(), generateDefaultAccessibility: false),
					cancellationToken)
					.ConfigureAwait(false);

				return result;
			}

			public override string Title
			{
				get
				{
					var text = Resources.GenerateEnumMemberIn;

					return string.Format(
						text,
						_state.IdentifierToken.ValueText,
						_state.TypeToGenerateIn.Name);
				}
			}
		}


		private partial class State
		{
			// public TypeDeclarationSyntax ContainingTypeDeclaration { get; private set; }
			public INamedTypeSymbol TypeToGenerateIn { get; private set; }

			// Just the name of the method.  i.e. "Foo" in "Foo" or "X.Foo"
			public SyntaxToken IdentifierToken { get; private set; }
			public TSimpleNameSyntax SimpleName { get; private set; }
			public TExpressionSyntax SimpleNameOrMemberAccessExpression { get; private set; }

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
				if (service.IsIdentifierNameGeneration(node))
				{
					if (!TryInitializeIdentifierName(service, document, (TSimpleNameSyntax)node, cancellationToken))
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
				// with the same name.  Note: it's ok if there's an existing field with the same
				// name.
				var existingMembers = this.TypeToGenerateIn.GetMembers(this.IdentifierToken.ValueText);
				if (existingMembers.Any())
				{
					// TODO: Code coverage There was an existing member that the new member would
					// clash with.  
					return false;
				}

				cancellationToken.ThrowIfCancellationRequested();
				this.TypeToGenerateIn = await SymbolFinder.FindSourceDefinitionAsync(this.TypeToGenerateIn, document.Project.Solution, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
				if (!service.ValidateTypeToGenerateIn(
					document.Project.Solution, this.TypeToGenerateIn, true, EnumType, cancellationToken))
				{
					return false;
				}

				return CodeGenerator.CanAdd(document.Project.Solution, this.TypeToGenerateIn, cancellationToken);
			}

			private bool TryInitializeIdentifierName(
				TService service,
				SemanticDocument document,
				TSimpleNameSyntax identifierName,
				CancellationToken cancellationToken)
			{
				this.SimpleName = identifierName;

				SyntaxToken identifierToken;
				TExpressionSyntax simpleNameOrMemberAccessExpression;
				if (!service.TryInitializeIdentifierNameState(document, identifierName, cancellationToken,
					out identifierToken, out simpleNameOrMemberAccessExpression))
				{
					return false;
				}

				this.IdentifierToken = identifierToken;
				this.SimpleNameOrMemberAccessExpression = simpleNameOrMemberAccessExpression;

				var semanticModel = document.SemanticModel;
				if ((simpleNameOrMemberAccessExpression as ExpressionSyntax).IsWrittenTo() ||
					simpleNameOrMemberAccessExpression.IsInNamespaceOrTypeContext())
				{
					return false;
				}

				// Now, try to bind the invocation and see if it succeeds or not.  if it succeeds and
				// binds uniquely, then we don't need to offer this quick fix.
				cancellationToken.ThrowIfCancellationRequested();
				var containingType = semanticModel.GetEnclosingNamedType(identifierToken.SpanStart, cancellationToken);
				if (containingType == null)
				{
					return false;
				}

				var semanticInfo = semanticModel.GetSymbolInfo(this.SimpleNameOrMemberAccessExpression, cancellationToken);
				if (cancellationToken.IsCancellationRequested)
				{
					return false;
				}

				if (semanticInfo.Symbol != null)
				{
					return false;
				}

				// Either we found no matches, or this was ambiguous. Either way, we might be able
				// to generate a method here.  Determine where the user wants to generate the method
				// into, and if it's valid then proceed.
				INamedTypeSymbol typeToGenerateIn;
				bool isStatic;
				if (!service.TryDetermineTypeToGenerateIn(
					document, containingType, simpleNameOrMemberAccessExpression, cancellationToken,
					out typeToGenerateIn, out isStatic))
				{
					return false;
				}

				if (!isStatic)
				{
					return false;
				}

				this.TypeToGenerateIn = typeToGenerateIn;
				return true;
			}
		}
	}
}
