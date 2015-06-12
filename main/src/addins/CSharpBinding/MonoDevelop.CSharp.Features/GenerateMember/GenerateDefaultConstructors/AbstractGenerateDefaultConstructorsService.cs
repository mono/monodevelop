// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.Editing;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.GenerateMember.GenerateDefaultConstructors
{
	public abstract partial class AbstractGenerateDefaultConstructorsService<TService>
		where TService : AbstractGenerateDefaultConstructorsService<TService>
	{
		protected AbstractGenerateDefaultConstructorsService()
		{
		}

		protected abstract bool TryInitializeState(SemanticDocument document, TextSpan textSpan, CancellationToken cancellationToken, out SyntaxNode baseTypeNode, out INamedTypeSymbol classType);

		public async Task<GenerateDefaultConstructorsResult> GenerateDefaultConstructorsAsync(
			Document document,
			TextSpan textSpan,
			CancellationToken cancellationToken)
		{
			var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

			if (textSpan.IsEmpty)
			{
				var state = State.Generate((TService)this, semanticDocument, textSpan, cancellationToken);
				if (state != null)
				{
					return new GenerateDefaultConstructorsResult(new CodeRefactoring(null, GetActions(document, state)));
				}
			}

			return GenerateDefaultConstructorsResult.Failure;
		}

		private IEnumerable<CodeAction> GetActions(Document document, State state)
		{
			foreach (var constructor in state.UnimplementedConstructors)
			{
				yield return new GenerateDefaultConstructorCodeAction((TService)this, document, state, constructor);
			}

			if (state.UnimplementedConstructors.Count > 1)
			{
				yield return new CodeActionAll((TService)this, document, state, state.UnimplementedConstructors);
			}
		}

		private abstract class AbstractCodeAction : CodeAction
		{
			private readonly IList<IMethodSymbol> _constructors;
			private readonly Document _document;
			private readonly State _state;
			private readonly TService _service;
			private readonly string _title;

			protected AbstractCodeAction(
				TService service,
				Document document,
				State state,
				IList<IMethodSymbol> constructors,
				string title)
			{
				_service = service;
				_document = document;
				_state = state;
				_constructors = constructors;
				_title = title;
			}

			public override string Title
			{
				get { return _title; }
			}

			protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
			{
				var result = await CodeGenerator.AddMemberDeclarationsAsync(
					_document.Project.Solution,
					_state.ClassType,
					_constructors.Select(CreateConstructorDefinition),
					cancellationToken: cancellationToken).ConfigureAwait(false);

				return result;
			}

			private IMethodSymbol CreateConstructorDefinition(
				IMethodSymbol constructor)
			{
				var syntaxFactory = _document.GetLanguageService<SyntaxGenerator>();
				var baseConstructorArguments = constructor.Parameters.Length != 0
					? syntaxFactory.CreateArguments(constructor.Parameters)
					: null;

				return CodeGenerationSymbolFactory.CreateConstructorSymbol(
					attributes: null,
					accessibility: constructor.DeclaredAccessibility,
					modifiers: new DeclarationModifiers(),
					typeName: _state.ClassType.Name,
					parameters: constructor.Parameters,
					statements: null,
					baseConstructorArguments: baseConstructorArguments);
			}
		}


		private class GenerateDefaultConstructorCodeAction : AbstractCodeAction
		{
			public GenerateDefaultConstructorCodeAction(
				TService service,
				Document document,
				State state,
				IMethodSymbol constructor)
				: base(service, document, state, new[] { constructor }, GetDisplayText(state, constructor))
			{
			}

			private static string GetDisplayText(State state, IMethodSymbol constructor)
			{
				var parameters = constructor.Parameters.Select(p => p.Name);
				var parameterString = string.Join(", ", parameters);

				return string.Format(Resources.GenerateConstructor + ".",
					state.ClassType.Name, parameterString);
			}
		}

		private class CodeActionAll : AbstractCodeAction
		{
			public CodeActionAll(
				TService service,
				Document document,
				State state,
				IList<IMethodSymbol> constructors)
				: base(service, document, state, GetConstructors(state, constructors), Resources.GenerateAll)
			{
			}

			private static IList<IMethodSymbol> GetConstructors(State state, IList<IMethodSymbol> constructors)
			{
				return state.UnimplementedDefaultConstructor != null
					? new[] { state.UnimplementedDefaultConstructor }.Concat(constructors).ToList()
					: constructors;
			}
		}
		private class State
		{
			public INamedTypeSymbol ClassType { get; private set; }

			public IList<IMethodSymbol> UnimplementedConstructors { get; private set; }
			public IMethodSymbol UnimplementedDefaultConstructor { get; private set; }

			public SyntaxNode BaseTypeNode { get; private set; }

			private State()
			{
			}

			public static State Generate(
				TService service,
				SemanticDocument document,
				TextSpan textSpan,
				CancellationToken cancellationToken)
			{
				var state = new State();
				if (!state.TryInitialize(service, document, textSpan, cancellationToken))
				{
					return null;
				}

				return state;
			}

			private bool TryInitialize(
				TService service,
				SemanticDocument document,
				TextSpan textSpan,
				CancellationToken cancellationToken)
			{
				SyntaxNode baseTypeNode;
				INamedTypeSymbol classType;
				if (!service.TryInitializeState(document, textSpan, cancellationToken, out baseTypeNode, out classType))
				{
					return false;
				}

				if (!baseTypeNode.Span.IntersectsWith(textSpan.Start))
				{
					return false;
				}

				this.BaseTypeNode = baseTypeNode;
				this.ClassType = classType;

				var baseType = this.ClassType.BaseType;

				if (this.ClassType.TypeKind != TypeKind.Class ||
					this.ClassType.IsStatic ||
					baseType == null ||
					baseType.SpecialType == SpecialType.System_Object ||
					baseType.TypeKind == TypeKind.Error)
				{
					return false;
				}

				var classConstructors = this.ClassType.InstanceConstructors;
				var baseTypeConstructors =
					baseType.InstanceConstructors
						.Where(c => c.IsAccessibleWithin(this.ClassType));

				var destinationProvider = document.Project.Solution.Workspace.Services.GetLanguageServices(this.ClassType.Language);

				var missingConstructors =
					baseTypeConstructors.Where(c1 => !classConstructors.Any(
						c2 => SignatureComparer.HaveSameSignature(c1.Parameters, c2.Parameters, compareParameterName: true, isCaseSensitive: true))).ToList();

				this.UnimplementedConstructors = missingConstructors;

				this.UnimplementedDefaultConstructor = baseTypeConstructors.FirstOrDefault(c => c.Parameters.Length == 0);
				if (this.UnimplementedDefaultConstructor != null)
				{
					if (classConstructors.Any(c => c.Parameters.Length == 0 && !c.IsImplicitlyDeclared))
					{
						this.UnimplementedDefaultConstructor = null;
					}
				}

				return this.UnimplementedConstructors.Count > 0;
			}
		}

	}
}
