// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using ICSharpCode.NRefactory6.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.FindSymbols;
using System.IO;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;

namespace ICSharpCode.NRefactory6.CSharp.GenerateType
{
	abstract partial class AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax> 
		where TService : AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax>
		where TSimpleNameSyntax : TExpressionSyntax
		where TObjectCreationExpressionSyntax : TExpressionSyntax
		where TExpressionSyntax : SyntaxNode
		where TTypeDeclarationSyntax : SyntaxNode
		where TArgumentSyntax : SyntaxNode
	{
		protected AbstractGenerateTypeService ()
		{
		}

		protected abstract bool TryInitializeState (SemanticDocument document, TSimpleNameSyntax simpleName, CancellationToken cancellationToken, out GenerateTypeServiceStateOptions generateTypeServiceStateOptions);

		protected abstract TExpressionSyntax GetLeftSideOfDot (TSimpleNameSyntax simpleName);

		protected abstract bool TryGetArgumentList (TObjectCreationExpressionSyntax objectCreationExpression, out IList<TArgumentSyntax> argumentList);

		protected abstract string DefaultFileExtension { get; }

		protected abstract IList<ITypeParameterSymbol> GetTypeParameters (State state, SemanticModel semanticModel, CancellationToken cancellationToken);

		protected abstract Accessibility GetAccessibility (State state, SemanticModel semanticModel, bool intoNamespace, CancellationToken cancellationToken);

		protected abstract IList<string> GenerateParameterNames (SemanticModel semanticModel, IList<TArgumentSyntax> arguments);

		protected abstract INamedTypeSymbol DetermineTypeToGenerateIn (SemanticModel semanticModel, TSimpleNameSyntax simpleName, CancellationToken cancellationToken);

		protected abstract ITypeSymbol DetermineArgumentType (SemanticModel semanticModel, TArgumentSyntax argument, CancellationToken cancellationToken);

		protected abstract bool IsInCatchDeclaration (TExpressionSyntax expression);

		protected abstract bool IsArrayElementType (TExpressionSyntax expression);

		protected abstract bool IsInVariableTypeContext (TExpressionSyntax expression);

		protected abstract bool IsInValueTypeConstraintContext (SemanticModel semanticModel, TExpressionSyntax expression, CancellationToken cancellationToken);

		protected abstract bool IsInInterfaceList (TExpressionSyntax expression);

		internal abstract bool TryGetBaseList (TExpressionSyntax expression, out TypeKindOptions returnValue);

		internal abstract bool IsPublicOnlyAccessibility (TExpressionSyntax expression, Project project);

		internal abstract bool IsGenericName (TSimpleNameSyntax simpleName);

		internal abstract bool IsSimpleName (TExpressionSyntax expression);

		internal abstract Solution TryAddUsingsOrImportToDocument (Solution updatedSolution, SyntaxNode modifiedRoot, Document document, TSimpleNameSyntax simpleName, string includeUsingsOrImports, CancellationToken cancellationToken);

		protected abstract bool TryGetNameParts (TExpressionSyntax expression, out IList<string> nameParts);

		public abstract string GetRootNamespace (CompilationOptions options);

		public abstract Task<Tuple<INamespaceSymbol, INamespaceOrTypeSymbol, Location>> GetOrGenerateEnclosingNamespaceSymbol (INamedTypeSymbol namedTypeSymbol, string[] containers, Document selectedDocument, SyntaxNode selectedDocumentRoot, CancellationToken cancellationToken);

		public async Task<IEnumerable<CodeAction>> GenerateTypeAsync (
			Document document,
			SyntaxNode node,
			CancellationToken cancellationToken)
		{
			//using (Logger.LogBlock (FunctionId.Refactoring_GenerateType, cancellationToken)) {
				var semanticDocument = await SemanticDocument.CreateAsync (document, cancellationToken).ConfigureAwait (false);

				var state = State.Generate ((TService)this, semanticDocument, node, cancellationToken);
				if (state != null) {
					return GetActions (semanticDocument, node, state, cancellationToken);
				}

				return SpecializedCollections.EmptyEnumerable<CodeAction> ();
			//}
		}

		private IEnumerable<CodeAction> GetActions (
			SemanticDocument document,
			SyntaxNode node,
			State state,
			CancellationToken cancellationToken)
		{
			//var generateNewTypeInDialog = false;
			if (state.NamespaceToGenerateInOpt != null) {
				var workspace = document.Project.Solution.Workspace;
				if (workspace == null || workspace.CanApplyChange (ApplyChangesKind.AddDocument)) {
					//generateNewTypeInDialog = true;
					yield return new GenerateTypeCodeAction ((TService)this, document.Document, state, intoNamespace: true, inNewFile: true);
				}

				// If they just are generating "Foo" then we want to offer to generate it into the
				// namespace in the same file.  However, if they are generating "SomeNS.Foo", then we
				// only want to allow them to generate if "SomeNS" is the namespace they are
				// currently in.
				var isSimpleName = state.SimpleName == state.NameOrMemberAccessExpression;
				var generateIntoContaining = IsGeneratingIntoContainingNamespace (document, node, state, cancellationToken);

				if ((isSimpleName || generateIntoContaining) &&
				    CanGenerateIntoContainingNamespace (document, node, state, cancellationToken)) {
					yield return new GenerateTypeCodeAction ((TService)this, document.Document, state, intoNamespace: true, inNewFile: false);
				}
			}

			if (state.TypeToGenerateInOpt != null) {
				yield return new GenerateTypeCodeAction ((TService)this, document.Document, state, intoNamespace: false, inNewFile: false);
			}

			//if (generateNewTypeInDialog) {
			//	yield return new GenerateTypeCodeActionWithOption ((TService)this, document.Document, state);
			//}
		}

		private bool CanGenerateIntoContainingNamespace (SemanticDocument document, SyntaxNode node, State state, CancellationToken cancellationToken)
		{
			var containingNamespace = document.SemanticModel.GetEnclosingNamespace (node.SpanStart, cancellationToken);

			// Only allow if the containing namespace is one that can be generated
			// into.  
			var decl = containingNamespace.GetDeclarations ()
				.Where (r => r.SyntaxTree == node.SyntaxTree)
				.Select (r => r.GetSyntax (cancellationToken))
				.FirstOrDefault (node.GetAncestorsOrThis<SyntaxNode> ().Contains);

			return
				decl != null &&
				new CSharpCodeGenerationService (document.Project.Solution.Workspace).CanAddTo (decl, document.Project.Solution, cancellationToken);
		}

		private bool IsGeneratingIntoContainingNamespace (
			SemanticDocument document,
			SyntaxNode node,
			State state,
			CancellationToken cancellationToken)
		{
			var containingNamespace = document.SemanticModel.GetEnclosingNamespace (node.SpanStart, cancellationToken);
			if (containingNamespace != null) {
				var containingNamespaceName = containingNamespace.ToDisplayString ();
				return containingNamespaceName.Equals (state.NamespaceToGenerateInOpt);
			}

			return false;
		}

		protected static string GetTypeName (State state)
		{
			const string AttributeSuffix = "Attribute";

			return state.IsAttribute && !state.NameIsVerbatim && !state.Name.EndsWith (AttributeSuffix, StringComparison.Ordinal)
				? state.Name + AttributeSuffix
					: state.Name;
		}

		protected IList<ITypeParameterSymbol> GetTypeParameters (
			State state,
			SemanticModel semanticModel,
			IEnumerable<SyntaxNode> typeArguments,
			CancellationToken cancellationToken)
		{
			var arguments = typeArguments.ToList ();
			var arity = arguments.Count;
			var typeParameters = new List<ITypeParameterSymbol> ();

			// For anything that was a type parameter, just use the name (if we haven't already
			// used it).  Otherwise, synthesize new names for the parameters.
			var names = new string[arity];
			var isFixed = new bool[arity];
			for (var i = 0; i < arity; i++) {
				var argument = i < arguments.Count ? arguments [i] : null;
				var type = argument == null ? null : semanticModel.GetTypeInfo (argument, cancellationToken).Type;
				if (type is ITypeParameterSymbol) {
					var name = type.Name;

					// If we haven't seen this type parameter already, then we can use this name
					// and 'fix' it so that it doesn't change. Otherwise, use it, but allow it
					// to be changed if it collides with anything else.
					isFixed [i] = !names.Contains (name);
					names [i] = name;
					typeParameters.Add ((ITypeParameterSymbol)type);
				} else {
					names [i] = "T";
					typeParameters.Add (null);
				}
			}

			// We can use a type parameter as long as it hasn't been used in an outer type.
			var canUse = state.TypeToGenerateInOpt == null
				? default(Func<string, bool>)
				: s => state.TypeToGenerateInOpt.GetAllTypeParameters ().All (t => t.Name != s);

			var uniqueNames = NameGenerator.EnsureUniqueness (names, isFixed, canUse: canUse);
			for (int i = 0; i < uniqueNames.Count; i++) {
				if (typeParameters [i] == null || typeParameters [i].Name != uniqueNames [i]) {
					typeParameters [i] = CodeGenerationSymbolFactory.CreateTypeParameterSymbol (uniqueNames [i]);
				}
			}

			return typeParameters;
		}

		protected Accessibility DetermineDefaultAccessibility (
			State state,
			SemanticModel semanticModel,
			bool intoNamespace,
			CancellationToken cancellationToken)
		{
			if (state.IsPublicAccessibilityForTypeGeneration) {
				return Accessibility.Public;
			}

			// If we're a nested type of the type being generated into, then the new type can be
			// private.  otherwise, it needs to be internal.
			if (!intoNamespace && state.TypeToGenerateInOpt != null) {
				var outerTypeSymbol = semanticModel.GetEnclosingNamedType (state.SimpleName.SpanStart, cancellationToken);

				if (outerTypeSymbol != null && outerTypeSymbol.IsContainedWithin (state.TypeToGenerateInOpt)) {
					return Accessibility.Private;
				}
			}

			return Accessibility.Internal;
		}

		protected IList<ITypeParameterSymbol> GetAvailableTypeParameters (
			State state,
			SemanticModel semanticModel,
			bool intoNamespace,
			CancellationToken cancellationToken)
		{
			var availableInnerTypeParameters = GetTypeParameters (state, semanticModel, cancellationToken);
			var availableOuterTypeParameters = !intoNamespace && state.TypeToGenerateInOpt != null
				? state.TypeToGenerateInOpt.GetAllTypeParameters ()
				: SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol> ();

			return availableOuterTypeParameters.Concat (availableInnerTypeParameters).ToList ();
		}

		protected bool IsWithinTheImportingNamespace (Document document, int triggeringPosition, string includeUsingsOrImports, CancellationToken cancellationToken)
		{
			var semanticModel = document.GetSemanticModelAsync (cancellationToken).WaitAndGetResult (cancellationToken);
			if (semanticModel != null) {
				var namespaceSymbol = semanticModel.GetEnclosingNamespace (triggeringPosition, cancellationToken);
				if (namespaceSymbol != null && namespaceSymbol.ToDisplayString ().StartsWith (includeUsingsOrImports, StringComparison.Ordinal)) {
					return true;
				}
			}

			return false;
		}

		protected bool GeneratedTypesMustBePublic (Project project)
		{
//			var projectInfoService = project.Solution.Workspace.Services.GetService<IProjectInfoService> ();
//			if (projectInfoService != null) {
//				return projectInfoService.GeneratedTypesMustBePublic (project);
//			}

			return false;
		}

		private class GenerateTypeCodeAction : CodeAction
		{
			private readonly bool _intoNamespace;
			private readonly bool _inNewFile;
			private readonly TService _service;
			private readonly Document _document;
			private readonly State _state;
			private readonly string _equivalenceKey;

			public GenerateTypeCodeAction (
				TService service,
				Document document,
				State state,
				bool intoNamespace,
				bool inNewFile)
			{
				_service = service;
				_document = document;
				_state = state;
				_intoNamespace = intoNamespace;
				_inNewFile = inNewFile;
				_equivalenceKey = Title;
			}

			private static string FormatDisplayText (
				State state,
				bool inNewFile,
				string destination)
			{
				//var finalName = GetTypeName (state);

				if (inNewFile) {
					return string.Format (Resources.GenerateForInNewFile,
						state.IsStruct ? "struct" : state.IsInterface ? "interface" : "class",
						state.Name, destination);
				} else {
					return string.Format (Resources.GenerateForIn,
						state.IsStruct ? "struct" : state.IsInterface ? "interface" : "class",
						state.Name, destination);
				}
			}

			protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync (CancellationToken cancellationToken)
			{
				var semanticDocument = await SemanticDocument.CreateAsync (_document, cancellationToken).ConfigureAwait (false);

				var editor = new Editor ( _service, semanticDocument, _state, _intoNamespace, _inNewFile, cancellationToken: cancellationToken);

				return await editor.GetOperationsAsync ().ConfigureAwait (false);
			}

			public override string Title {
				get {
					if (_intoNamespace) {
						var namespaceToGenerateIn = string.IsNullOrEmpty (_state.NamespaceToGenerateInOpt) ? Resources.GlobalNamespace : _state.NamespaceToGenerateInOpt;
						return FormatDisplayText (_state, _inNewFile, namespaceToGenerateIn);
					} else {
						return FormatDisplayText (_state, inNewFile: false, destination: _state.TypeToGenerateInOpt.Name);
					}
				}
			}

			public override string EquivalenceKey {
				get {
					return _equivalenceKey;
				}
			}
		}

		private class GenerateTypeCodeActionWithOption : CodeActionWithOptions
		{
			private readonly TService _service;
			private readonly Document _document;
			private readonly State _state;

			internal GenerateTypeCodeActionWithOption (TService service, Document document, State state)
			{
				_service = service;
				_document = document;
				_state = state;
			}

			public override string Title {
				get {
					return Resources.GenerateNewType;
				}
			}

			public override string EquivalenceKey {
				get {
					return _state.Name;
				}
			}

			public override object GetOptions (CancellationToken cancellationToken)
			{
				//var typeKindValue = GetTypeKindOption (_state);
				var isPublicOnlyAccessibility = IsPublicOnlyAccessibility (_state, _document.Project);

				// TODO : Callback
				return new GenerateTypeOptionsResult (
					isPublicOnlyAccessibility ? Accessibility.Public : Accessibility.Internal,
					TypeKind.Class,
					_state.Name,
					_document.Project,
					true,
					_state.Name + ".cs",
					null,
					null,
					_document,
					false
				);
				/*
				//				return generateTypeOptionsService.GetGenerateTypeOptions (
//					_state.Name,
					//					new GenerateTypeDialogOptions (isPublicOnlyAccessibility, typeKindValue, _state.IsAttribute),
//					_document,
//					notificationService,
//					projectManagementService,
//					syntaxFactsService);
				private class VisualStudioGenerateTypeOptionsService : IGenerateTypeOptionsService
				{
					private bool _isNewFile = false;
					private string _accessSelectString = "";
					private string _typeKindSelectString = "";

					private IGeneratedCodeRecognitionService _generatedCodeService;

					public VisualStudioGenerateTypeOptionsService(IGeneratedCodeRecognitionService generatedCodeService)
					{
						_generatedCodeService = generatedCodeService;
					}

					public GenerateTypeOptionsResult GetGenerateTypeOptions(
						string typeName,
						GenerateTypeDialogOptions generateTypeDialogOptions,
						Document document,
						INotificationService notificationService,
						IProjectManagementService projectManagementService,
						ISyntaxFactsService syntaxFactsService)
					{
						var viewModel = new GenerateTypeDialogViewModel(
							document,
							notificationService,
							projectManagementService,
							syntaxFactsService,
							_generatedCodeService,
							generateTypeDialogOptions,
							typeName,
							document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb",
							_isNewFile,
							_accessSelectString,
							_typeKindSelectString);

						var dialog = new GenerateTypeDialog(viewModel);
						var result = dialog.ShowModal();

						if (result.HasValue && result.Value)
						{
							// Retain choice
							_isNewFile = viewModel.IsNewFile;
							_accessSelectString = viewModel.SelectedAccessibilityString;
							_typeKindSelectString = viewModel.SelectedTypeKindString;

							return new GenerateTypeOptionsResult(
								accessibility: viewModel.SelectedAccessibility,
								typeKind: viewModel.SelectedTypeKind,
								typeName: viewModel.TypeName,
								project: viewModel.SelectedProject,
								isNewFile: viewModel.IsNewFile,
								newFileName: viewModel.FileName.Trim(),
								folders: viewModel.Folders,
								fullFilePath: viewModel.FullFilePath,
								existingDocument: viewModel.SelectedDocument,
								areFoldersValidIdentifiers: viewModel.AreFoldersValidIdentifiers);
						}
						else
						{
							return GenerateTypeOptionsResult.Cancelled;
						}
					}
				}

				*/
			}

			private bool IsPublicOnlyAccessibility (State state, Project project)
			{
				return _service.IsPublicOnlyAccessibility (state.NameOrMemberAccessExpression, project) || _service.IsPublicOnlyAccessibility (state.SimpleName, project);
			}

			private TypeKindOptions GetTypeKindOption (State state)
			{
				TypeKindOptions typeKindValue;

				var gotPreassignedTypeOptions = GetPredefinedTypeKindOption (state, out typeKindValue);
				if (!gotPreassignedTypeOptions) {
					typeKindValue = state.IsSimpleNameGeneric ? TypeKindOptionsHelper.RemoveOptions (typeKindValue, TypeKindOptions.GenericInCompatibleTypes) : typeKindValue;
					typeKindValue = state.IsMembersWithModule ? TypeKindOptionsHelper.AddOption (typeKindValue, TypeKindOptions.Module) : typeKindValue;
					typeKindValue = state.IsInterfaceOrEnumNotAllowedInTypeContext ? TypeKindOptionsHelper.RemoveOptions (typeKindValue, TypeKindOptions.Interface, TypeKindOptions.Enum) : typeKindValue;
					typeKindValue = state.IsDelegateAllowed ? typeKindValue : TypeKindOptionsHelper.RemoveOptions (typeKindValue, TypeKindOptions.Delegate);
					typeKindValue = state.IsEnumNotAllowed ? TypeKindOptionsHelper.RemoveOptions (typeKindValue, TypeKindOptions.Enum) : typeKindValue;
				}

				return typeKindValue;
			}

			private bool GetPredefinedTypeKindOption (State state, out TypeKindOptions typeKindValueFinal)
			{
				if (state.IsAttribute) {
					typeKindValueFinal = TypeKindOptions.Attribute;
					return true;
				}

				TypeKindOptions typeKindValue = TypeKindOptions.None;
				if (_service.TryGetBaseList (state.NameOrMemberAccessExpression, out typeKindValue) || _service.TryGetBaseList (state.SimpleName, out typeKindValue)) {
					typeKindValueFinal = typeKindValue;
					return true;
				}

				if (state.IsClassInterfaceTypes) {
					typeKindValueFinal = TypeKindOptions.BaseList;
					return true;
				}

				if (state.IsDelegateOnly) {
					typeKindValueFinal = TypeKindOptions.Delegate;
					return true;
				}

				if (state.IsTypeGeneratedIntoNamespaceFromMemberAccess) {
					typeKindValueFinal = state.IsSimpleNameGeneric ? TypeKindOptionsHelper.RemoveOptions (TypeKindOptions.MemberAccessWithNamespace, TypeKindOptions.GenericInCompatibleTypes) : TypeKindOptions.MemberAccessWithNamespace;
					typeKindValueFinal = state.IsEnumNotAllowed ? TypeKindOptionsHelper.RemoveOptions (typeKindValueFinal, TypeKindOptions.Enum) : typeKindValueFinal;
					return true;
				}

				typeKindValueFinal = TypeKindOptions.AllOptions;
				return false;
			}

			protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync (object options, CancellationToken cancellationToken)
			{
				IEnumerable<CodeActionOperation> operations = null;

				var generateTypeOptions = options as GenerateTypeOptionsResult;
				if (generateTypeOptions != null && !generateTypeOptions.IsCancelled) {
					var semanticDocument = SemanticDocument.CreateAsync (_document, cancellationToken).WaitAndGetResult (cancellationToken);
					var editor = new Editor (_service, semanticDocument, _state, true, generateTypeOptions, cancellationToken);
					operations = await editor.GetOperationsAsync ().ConfigureAwait (false);
				}

				return operations;
			}
		}

		protected abstract bool IsConversionImplicit (Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);

		private partial class Editor
		{
			private TService _service;
			private TargetProjectChangeInLanguage _targetProjectChangeInLanguage = TargetProjectChangeInLanguage.NoChange;
			AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax>  _targetLanguageService;

			private readonly SemanticDocument _document;
			private readonly State _state;
			private readonly bool _intoNamespace;
			private readonly bool _inNewFile;
			private readonly bool _fromDialog;
			private readonly GenerateTypeOptionsResult _generateTypeOptionsResult;
			private readonly CancellationToken _cancellationToken;


			public Editor (
				TService service,
				SemanticDocument document,
				State state,
				bool intoNamespace,
				bool inNewFile,
				CancellationToken cancellationToken)
			{
				_service = service;
				_document = document;
				_state = state;
				_intoNamespace = intoNamespace;
				_inNewFile = inNewFile;
				_cancellationToken = cancellationToken;
			}

			public Editor (
				TService service,
				SemanticDocument document,
				State state,
				bool fromDialog,
				GenerateTypeOptionsResult generateTypeOptionsResult,
				CancellationToken cancellationToken)
			{
				_service = service;
				_document = document;
				_state = state;
				_fromDialog = fromDialog;
				_generateTypeOptionsResult = generateTypeOptionsResult;
				_cancellationToken = cancellationToken;
			}

			private enum TargetProjectChangeInLanguage
			{
				NoChange,
				CSharpToVisualBasic,
				VisualBasicToCSharp
			}

			internal async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync ()
			{
				// Check to see if it is from GFU Dialog
				if (!_fromDialog) {
					// Generate the actual type declaration.
					var namedType = GenerateNamedType ();

					if (_intoNamespace) {
						if (_inNewFile) {
							// Generating into a new file is somewhat complicated.
							var documentName = GetTypeName (_state) + _service.DefaultFileExtension;

							return await GetGenerateInNewFileOperationsAsync (
								namedType,
								documentName,
								null,
								true,
								null,
								_document.Project,
								_document.Project,
								isDialog: false).ConfigureAwait (false);
						} else {
							return await GetGenerateIntoContainingNamespaceOperationsAsync (namedType).ConfigureAwait (false);
						}
					} else {
						return await GetGenerateIntoTypeOperationsAsync (namedType).ConfigureAwait (false);
					}
				} else {
					var namedType = GenerateNamedType (_generateTypeOptionsResult);

//					// Honor the options from the dialog
//					// Check to see if the type is requested to be generated in cross language Project
//					// e.g.: C# -> VB or VB -> C#
//					if (_document.Project.Language != _generateTypeOptionsResult.Project.Language) {
//						_targetProjectChangeInLanguage =
//							_generateTypeOptionsResult.Project.Language == LanguageNames.CSharp
//							? TargetProjectChangeInLanguage.VisualBasicToCSharp
//							: TargetProjectChangeInLanguage.CSharpToVisualBasic;
//
//						// Get the cross language service
//						_targetLanguageService = _generateTypeOptionsResult.Project.LanguageServices.GetService<IGenerateTypeService> ();
//					}

					if (_generateTypeOptionsResult.IsNewFile) {
						return await GetGenerateInNewFileOperationsAsync (
							namedType,
							_generateTypeOptionsResult.NewFileName,
							_generateTypeOptionsResult.Folders,
							_generateTypeOptionsResult.AreFoldersValidIdentifiers,
							_generateTypeOptionsResult.FullFilePath,
							_generateTypeOptionsResult.Project,
							_document.Project,
							isDialog: true).ConfigureAwait (false);
					} else {
						return await GetGenerateIntoExistingDocumentAsync (
							namedType,
							_document.Project,
							_generateTypeOptionsResult,
							isDialog: true).ConfigureAwait (false);
					}
				}
			}

			private string GetNamespaceToGenerateInto ()
			{
				var namespaceToGenerateInto = _state.NamespaceToGenerateInOpt.Trim ();
				var rootNamespace = _service.GetRootNamespace (_document.SemanticModel.Compilation.Options).Trim ();
				if (!string.IsNullOrWhiteSpace (rootNamespace)) {
					if (namespaceToGenerateInto == rootNamespace ||
					    namespaceToGenerateInto.StartsWith (rootNamespace + ".", StringComparison.Ordinal)) {
						namespaceToGenerateInto = namespaceToGenerateInto.Substring (rootNamespace.Length);
					}
				}

				return namespaceToGenerateInto;
			}

			private string GetNamespaceToGenerateIntoForUsageWithNamespace (Project targetProject, Project triggeringProject)
			{
				var namespaceToGenerateInto = _state.NamespaceToGenerateInOpt.Trim ();

				if (targetProject.Language == LanguageNames.CSharp ||
				    targetProject == triggeringProject) {
					// If the target project is C# project then we don't have to make any modification to the namespace
					// or
					// This is a VB project generation into itself which requires no change as well
					return namespaceToGenerateInto;
				}

				// If the target Project is VB then we have to check if the RootNamespace of the VB project is the parent most namespace of the type being generated
				// True, Remove the RootNamespace
				// False, Add Global to the Namespace
				//Contract.Assert (targetProject.Language == LanguageNames.VisualBasic);
				var targetLanguageService = _targetLanguageService;
//				if (_document.Project.Language == LanguageNames.VisualBasic) {
//					targetLanguageService = _service;
//				} else {
//					targetLanguageService = _targetLanguageService;
//				}

				var rootNamespace = targetLanguageService.GetRootNamespace (targetProject.CompilationOptions).Trim ();
				if (!string.IsNullOrWhiteSpace (rootNamespace)) {
					var rootNamespaceLength = CheckIfRootNamespacePresentInNamespace (namespaceToGenerateInto, rootNamespace);
					if (rootNamespaceLength > -1) {
						// True, Remove the RootNamespace
						namespaceToGenerateInto = namespaceToGenerateInto.Substring (rootNamespaceLength);
					} else {
						// False, Add Global to the Namespace
						namespaceToGenerateInto = AddGlobalDotToTheNamespace (namespaceToGenerateInto);
					}
				} else {
					// False, Add Global to the Namespace
					namespaceToGenerateInto = AddGlobalDotToTheNamespace (namespaceToGenerateInto);
				}

				return namespaceToGenerateInto;
			}

			private string AddGlobalDotToTheNamespace (string namespaceToBeGenerated)
			{
				return "Global." + namespaceToBeGenerated;
			}

			// Returns the length of the meaningful rootNamespace substring part of namespaceToGenerateInto
			private int CheckIfRootNamespacePresentInNamespace (string namespaceToGenerateInto, string rootNamespace)
			{
				if (namespaceToGenerateInto == rootNamespace) {
					return rootNamespace.Length;
				}

				if (namespaceToGenerateInto.StartsWith (rootNamespace + ".", StringComparison.Ordinal)) {
					return rootNamespace.Length + 1;
				}

				return -1;
			}

			private void AddFoldersToNamespaceContainers (List<string> container, IList<string> folders)
			{
				// Add the folder as part of the namespace if there are not empty
				if (folders != null && folders.Count != 0) {
					// Remove the empty entries and replace the spaces in the folder name to '_'
					var refinedFolders = folders.Where (n => n != null && !n.IsEmpty ()).Select (n => n.Replace (' ', '_')).ToArray ();
					container.AddRange (refinedFolders);
				}
			}

			private async Task<IEnumerable<CodeActionOperation>> GetGenerateInNewFileOperationsAsync (
				INamedTypeSymbol namedType,
				string documentName,
				IList<string> folders,
				bool areFoldersValidIdentifiers,
				string fullFilePath,
				Project projectToBeUpdated,
				Project triggeringProject,
				bool isDialog)
			{
				// First, we fork the solution with a new, empty, file in it.  
				var newDocumentId = DocumentId.CreateNewId (projectToBeUpdated.Id, debugName: documentName);
				var newSolution = projectToBeUpdated.Solution.AddDocument (newDocumentId, documentName, string.Empty, folders, fullFilePath);

				// Now we get the semantic model for that file we just added.  We do that to get the
				// root namespace in that new document, along with location for that new namespace.
				// That way, when we use the code gen service we can say "add this symbol to the
				// root namespace" and it will pick the one in the new file.
				var newDocument = newSolution.GetDocument (newDocumentId);
				var newSemanticModel = await newDocument.GetSemanticModelAsync (_cancellationToken).ConfigureAwait (false);
				var enclosingNamespace = newSemanticModel.GetEnclosingNamespace (0, _cancellationToken);

				var namespaceContainersAndUsings = GetNamespaceContainersAndAddUsingsOrImport (isDialog, folders, areFoldersValidIdentifiers, projectToBeUpdated, triggeringProject);

				var containers = namespaceContainersAndUsings.Item1;
				var includeUsingsOrImports = namespaceContainersAndUsings.Item2;

				var rootNamespaceOrType = namedType.GenerateRootNamespaceOrType (containers);

				// Now, actually ask the code gen service to add this namespace or type to the root
				// namespace in the new file.  This will properly generate the code, and add any
				// additional niceties like imports/usings.
				var codeGenResult = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync (
					                    newSolution,
					                    enclosingNamespace,
					                    rootNamespaceOrType,
					                    new CodeGenerationOptions (newSemanticModel.SyntaxTree.GetLocation (new TextSpan ()), generateDefaultAccessibility: false),
					                    _cancellationToken).ConfigureAwait (false);

				// containers is determined to be
				// 1: folders -> if triggered from Dialog
				// 2: containers -> if triggered not from a Dialog but from QualifiedName
				// 3: triggering document folder structure -> if triggered not from a Dialog and a SimpleName
				var adjustedContainer = isDialog ? folders :
					_state.SimpleName != _state.NameOrMemberAccessExpression ? containers.ToList () : _document.Document.Folders.ToList ();

				// Now, take the code that would be generated and actually create an edit that would
				// produce a document with that code in it.

				return CreateAddDocumentAndUpdateUsingsOrImportsOperations (
					projectToBeUpdated,
					triggeringProject,
					documentName,
					await codeGenResult.GetSyntaxRootAsync (_cancellationToken).ConfigureAwait (false),
					_document.Document,
					includeUsingsOrImports,
					adjustedContainer,
					SourceCodeKind.Regular,
					_cancellationToken);
			}

			private IEnumerable<CodeActionOperation> CreateAddDocumentAndUpdateUsingsOrImportsOperations (
				Project projectToBeUpdated,
				Project triggeringProject,
				string documentName,
				SyntaxNode root,
				Document generatingDocument,
				string includeUsingsOrImports,
				IList<string> containers,
				SourceCodeKind sourceCodeKind,
				CancellationToken cancellationToken)
			{
				// TODO(cyrusn): make sure documentId is unique.
				var documentId = DocumentId.CreateNewId (projectToBeUpdated.Id, documentName);

				var updatedSolution = projectToBeUpdated.Solution.AddDocument (DocumentInfo.Create (
					documentId,
					documentName,
					containers,
					sourceCodeKind,
					filePath: Path.Combine (Path.GetDirectoryName (generatingDocument.FilePath), documentName)
				));

				updatedSolution = updatedSolution.WithDocumentSyntaxRoot (documentId, root, PreservationMode.PreserveIdentity);

				// Update the Generating Document with a using if required
				if (includeUsingsOrImports != null) {
					updatedSolution = _service.TryAddUsingsOrImportToDocument (updatedSolution, null, _document.Document, _state.SimpleName, includeUsingsOrImports, cancellationToken);
				}

				// Add reference of the updated project to the triggering Project if they are 2 different projects
				updatedSolution = AddProjectReference (projectToBeUpdated, triggeringProject, updatedSolution);

				return new CodeActionOperation[] {
					new ApplyChangesOperation (updatedSolution),
					new OpenDocumentOperation (documentId)
				};
			}

			private static Solution AddProjectReference (Project projectToBeUpdated, Project triggeringProject, Solution updatedSolution)
			{
				if (projectToBeUpdated != triggeringProject) {
					if (!triggeringProject.ProjectReferences.Any (pr => pr.ProjectId == projectToBeUpdated.Id)) {
						updatedSolution = updatedSolution.AddProjectReference (triggeringProject.Id, new ProjectReference (projectToBeUpdated.Id));
					}
				}

				return updatedSolution;
			}

			private async Task<IEnumerable<CodeActionOperation>> GetGenerateIntoContainingNamespaceOperationsAsync (INamedTypeSymbol namedType)
			{
				var enclosingNamespace = _document.SemanticModel.GetEnclosingNamespace (
					                         _state.SimpleName.SpanStart, _cancellationToken);

				var solution = _document.Project.Solution;
				var codeGenResult = await CodeGenerator.AddNamedTypeDeclarationAsync (
					                    solution,
					                    enclosingNamespace,
					                    namedType,
					                    new CodeGenerationOptions (afterThisLocation: _document.SyntaxTree.GetLocation (_state.SimpleName.Span), generateDefaultAccessibility: false),
					                    _cancellationToken)
					.ConfigureAwait (false);

				return new CodeActionOperation[] { new ApplyChangesOperation (codeGenResult.Project.Solution) };
			}

			private async Task<IEnumerable<CodeActionOperation>> GetGenerateIntoExistingDocumentAsync (
				INamedTypeSymbol namedType,
				Project triggeringProject,
				GenerateTypeOptionsResult generateTypeOptionsResult,
				bool isDialog)
			{
				var root = await generateTypeOptionsResult.ExistingDocument.GetSyntaxRootAsync (_cancellationToken).ConfigureAwait (false);
				var folders = generateTypeOptionsResult.ExistingDocument.Folders;

				var namespaceContainersAndUsings = GetNamespaceContainersAndAddUsingsOrImport (isDialog, new List<string> (folders), generateTypeOptionsResult.AreFoldersValidIdentifiers, generateTypeOptionsResult.Project, triggeringProject);

				var containers = namespaceContainersAndUsings.Item1;
				var includeUsingsOrImports = namespaceContainersAndUsings.Item2;

				Tuple<INamespaceSymbol, INamespaceOrTypeSymbol, Location> enclosingNamespaceGeneratedTypeToAddAndLocation = null;
				if (_targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange) {
					enclosingNamespaceGeneratedTypeToAddAndLocation = _service.GetOrGenerateEnclosingNamespaceSymbol (
						namedType,
						containers,
						generateTypeOptionsResult.ExistingDocument,
						root,
						_cancellationToken).WaitAndGetResult (_cancellationToken);
				} else {
					enclosingNamespaceGeneratedTypeToAddAndLocation = _targetLanguageService.GetOrGenerateEnclosingNamespaceSymbol (
						namedType,
						containers,
						generateTypeOptionsResult.ExistingDocument,
						root,
						_cancellationToken).WaitAndGetResult (_cancellationToken);
				}

				var solution = _document.Project.Solution;
				var codeGenResult = await CodeGenerator.AddNamespaceOrTypeDeclarationAsync (
					                    solution,
					                    enclosingNamespaceGeneratedTypeToAddAndLocation.Item1,
					                    enclosingNamespaceGeneratedTypeToAddAndLocation.Item2,
					                    new CodeGenerationOptions (afterThisLocation: enclosingNamespaceGeneratedTypeToAddAndLocation.Item3, generateDefaultAccessibility: false),
					                    _cancellationToken)
					.ConfigureAwait (false);
				var newRoot = await codeGenResult.GetSyntaxRootAsync (_cancellationToken).ConfigureAwait (false);
				var updatedSolution = solution.WithDocumentSyntaxRoot (generateTypeOptionsResult.ExistingDocument.Id, newRoot, PreservationMode.PreserveIdentity);

				// Update the Generating Document with a using if required
				if (includeUsingsOrImports != null) {
					updatedSolution = _service.TryAddUsingsOrImportToDocument (
						updatedSolution,
						generateTypeOptionsResult.ExistingDocument.Id == _document.Document.Id ? newRoot : null,
						_document.Document,
						_state.SimpleName,
						includeUsingsOrImports,
						_cancellationToken);
				}

				updatedSolution = AddProjectReference (generateTypeOptionsResult.Project, triggeringProject, updatedSolution);

				return new CodeActionOperation[] { new ApplyChangesOperation (updatedSolution) };
			}

			private Tuple<string[], string> GetNamespaceContainersAndAddUsingsOrImport (
				bool isDialog,
				IList<string> folders,
				bool areFoldersValidIdentifiers,
				Project targetProject,
				Project triggeringProject)
			{
				string includeUsingsOrImports = null;
				if (!areFoldersValidIdentifiers) {
					folders = SpecializedCollections.EmptyList<string> ();
				}

				// Now actually create the symbol that we want to add to the root namespace.  The
				// symbol may either be a named type (if we're not generating into a namespace) or
				// it may be a namespace symbol.
				string[] containers = null;
				if (!isDialog) {
					// Not generated from the Dialog 
					containers = GetNamespaceToGenerateInto ().Split (new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				} else if (!_service.IsSimpleName (_state.NameOrMemberAccessExpression)) {
					// If the usage was with a namespace
					containers = GetNamespaceToGenerateIntoForUsageWithNamespace (targetProject, triggeringProject).Split (new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				} else {
					// Generated from the Dialog
					List<string> containerList = new List<string> ();

					string rootNamespaceOfTheProjectGeneratedInto;

					if (_targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange) {
						rootNamespaceOfTheProjectGeneratedInto = _service.GetRootNamespace (_generateTypeOptionsResult.Project.CompilationOptions).Trim ();
					} else {
						rootNamespaceOfTheProjectGeneratedInto = _targetLanguageService.GetRootNamespace (_generateTypeOptionsResult.Project.CompilationOptions).Trim ();
					}

					// TODO : Default namespace support
					//var projectManagementService = _document.Project.Solution.Workspace.Services.GetService<IProjectManagementService> ();
					var defaultNamespace = "";// projectManagementService.GetDefaultNamespace (targetProject, targetProject.Solution.Workspace);

					// Case 1 : If the type is generated into the same C# project or
					// Case 2 : If the type is generated from a C# project to a C# Project
					// Case 3 : If the Type is generated from a VB Project to a C# Project
					// Using and Namespace will be the DefaultNamespace + Folder Structure
					if ((_document.Project == _generateTypeOptionsResult.Project && _document.Project.Language == LanguageNames.CSharp) ||
					    (_targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange && _generateTypeOptionsResult.Project.Language == LanguageNames.CSharp) ||
					    _targetProjectChangeInLanguage == TargetProjectChangeInLanguage.VisualBasicToCSharp) {
						if (!string.IsNullOrWhiteSpace (defaultNamespace)) {
							containerList.Add (defaultNamespace);
						}

						// Populate the ContainerList
						AddFoldersToNamespaceContainers (containerList, folders);

						containers = containerList.ToArray ();
						includeUsingsOrImports = string.Join (".", containerList.ToArray ());
					}

					// Case 4 : If the type is generated into the same VB project or
					// Case 5 : If Type is generated from a VB Project to VB Project
					// Case 6 : If Type is generated from a C# Project to VB Project 
					// Namespace will be Folder Structure and Import will have the RootNamespace of the project generated into as part of the Imports
					if ((_document.Project == _generateTypeOptionsResult.Project && _document.Project.Language == LanguageNames.VisualBasic) ||
					    (_document.Project != _generateTypeOptionsResult.Project && _targetProjectChangeInLanguage == TargetProjectChangeInLanguage.NoChange && _generateTypeOptionsResult.Project.Language == LanguageNames.VisualBasic) ||
					    _targetProjectChangeInLanguage == TargetProjectChangeInLanguage.CSharpToVisualBasic) {
						// Populate the ContainerList
						AddFoldersToNamespaceContainers (containerList, folders);
						containers = containerList.ToArray ();
						includeUsingsOrImports = string.Join (".", containerList.ToArray ());
						if (!string.IsNullOrWhiteSpace (rootNamespaceOfTheProjectGeneratedInto)) {
							includeUsingsOrImports = string.IsNullOrEmpty (includeUsingsOrImports) ?
								rootNamespaceOfTheProjectGeneratedInto :
								rootNamespaceOfTheProjectGeneratedInto + "." + includeUsingsOrImports;
						}
					}
				}

				return Tuple.Create (containers, includeUsingsOrImports);
			}

			private async Task<IEnumerable<CodeActionOperation>> GetGenerateIntoTypeOperationsAsync (INamedTypeSymbol namedType)
			{
				//var codeGenService = GetCodeGenerationService ();
				var solution = _document.Project.Solution;
				var codeGenResult = await CodeGenerator.AddNamedTypeDeclarationAsync (
					                    solution,
					                    _state.TypeToGenerateInOpt,
					                    namedType,
					                    new CodeGenerationOptions (contextLocation: _state.SimpleName.GetLocation (), generateDefaultAccessibility: false),
					                    _cancellationToken)
					.ConfigureAwait (false);

				return new CodeActionOperation[] { new ApplyChangesOperation (codeGenResult.Project.Solution) };
			}

			private IList<ITypeSymbol> GetArgumentTypes (IList<TArgumentSyntax> argumentList)
			{
				var types = argumentList.Select (a => _service.DetermineArgumentType (_document.SemanticModel, a, _cancellationToken));
				return types.Select (FixType).ToList ();
			}

			private ITypeSymbol FixType (
				ITypeSymbol typeSymbol)
			{
				var compilation = _document.SemanticModel.Compilation;
				return typeSymbol.RemoveUnnamedErrorTypes (compilation);
			}

			private CSharpCodeGenerationService GetCodeGenerationService ()
			{
				var language = _state.TypeToGenerateInOpt == null
					? _state.SimpleName.Language
					: _state.TypeToGenerateInOpt.Language;
				return new CSharpCodeGenerationService(_document.Project.Solution.Workspace, language);
			}

			private bool TryFindMatchingField (
				string parameterName,
				ITypeSymbol parameterType,
				Dictionary<string, ISymbol> parameterToFieldMap,
				bool caseSensitive)
			{
				// If the base types have an accessible field or property with the same name and
				// an acceptable type, then we should just defer to that.
				if (_state.BaseTypeOrInterfaceOpt != null) {
					var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
					var query =
						_state.BaseTypeOrInterfaceOpt
							.GetBaseTypesAndThis ()
							.SelectMany (t => t.GetMembers ())
							.Where (s => s.Name.Equals (parameterName, comparison));
					var symbol = query.FirstOrDefault (IsSymbolAccessible);

					if (IsViableFieldOrProperty (parameterType, symbol)) {
						parameterToFieldMap [parameterName] = symbol;
						return true;
					}
				}

				return false;
			}

			private bool IsViableFieldOrProperty (
				ITypeSymbol parameterType,
				ISymbol symbol)
			{
				if (symbol != null && !symbol.IsStatic && parameterType.Language == symbol.Language) {
					if (symbol is IFieldSymbol) {
						var field = (IFieldSymbol)symbol;
						return
							!field.IsReadOnly &&
						_service.IsConversionImplicit (_document.SemanticModel.Compilation, parameterType, field.Type);
					} else if (symbol is IPropertySymbol) {
						var property = (IPropertySymbol)symbol;
						return
							property.Parameters.Length == 0 &&
						property.SetMethod != null &&
						IsSymbolAccessible (property.SetMethod) &&
						_service.IsConversionImplicit (_document.SemanticModel.Compilation, parameterType, property.Type);
					}
				}

				return false;
			}

			private bool IsSymbolAccessible (ISymbol symbol)
			{
				// Public and protected constructors are accessible.  Internal constructors are
				// accessible if we have friend access.  We can't call the normal accessibility
				// checkers since they will think that a protected constructor isn't accessible
				// (since we don't have the destination type that would have access to them yet).
				switch (symbol.DeclaredAccessibility) {
				case Accessibility.ProtectedOrInternal:
				case Accessibility.Protected:
				case Accessibility.Public:
					return true;
				case Accessibility.ProtectedAndInternal:
				case Accessibility.Internal:
					// TODO: Code coverage
					return _document.SemanticModel.Compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo (
						symbol.ContainingAssembly);

				default:
					return false;
				}
			}
		}

		internal abstract IMethodSymbol GetDelegatingConstructor (TObjectCreationExpressionSyntax objectCreation, INamedTypeSymbol namedType, SemanticModel model, ISet<IMethodSymbol> candidates, CancellationToken cancellationToken);

		private partial class Editor
		{
			private INamedTypeSymbol GenerateNamedType ()
			{
				return CodeGenerationSymbolFactory.CreateNamedTypeSymbol (
					DetermineAttributes (),
					DetermineAccessibility (),
					DetermineModifiers (),
					DetermineTypeKind (),
					DetermineName (),
					DetermineTypeParameters (),
					DetermineBaseType (),
					DetermineInterfaces (),
					members: DetermineMembers ());
			}

			private INamedTypeSymbol GenerateNamedType (GenerateTypeOptionsResult options)
			{
				if (options.TypeKind == TypeKind.Delegate) {
					return CodeGenerationSymbolFactory.CreateDelegateTypeSymbol (
						DetermineAttributes (),
						options.Accessibility,
						DetermineModifiers (),
						DetermineReturnType (options),
						options.TypeName,
						DetermineTypeParameters (options),
						DetermineParameters (options));
				}

				return CodeGenerationSymbolFactory.CreateNamedTypeSymbol (
					DetermineAttributes (),
					options.Accessibility,
					DetermineModifiers (),
					options.TypeKind,
					options.TypeName,
					DetermineTypeParameters (),
					DetermineBaseType (),
					DetermineInterfaces (),
					members: DetermineMembers (options));
			}

			private ITypeSymbol DetermineReturnType (GenerateTypeOptionsResult options)
			{
				if (_state.DelegateMethodSymbol == null ||
				    _state.DelegateMethodSymbol.ReturnType == null ||
				    _state.DelegateMethodSymbol.ReturnType is IErrorTypeSymbol) {
					// Since we cannot determine the return type, we are returning void
					return _state.Compilation.GetSpecialType (SpecialType.System_Void);
				} else {
					return _state.DelegateMethodSymbol.ReturnType;
				}
			}

			private IList<ITypeParameterSymbol> DetermineTypeParameters (GenerateTypeOptionsResult options)
			{
				if (_state.DelegateMethodSymbol != null) {
					return _state.DelegateMethodSymbol.TypeParameters;
				}

				// If the delegate symbol cannot be determined then 
				return DetermineTypeParameters ();
			}

			private IList<IParameterSymbol> DetermineParameters (GenerateTypeOptionsResult options)
			{
				if (_state.DelegateMethodSymbol != null) {
					return _state.DelegateMethodSymbol.Parameters;
				}

				return null;
			}

			private IList<ISymbol> DetermineMembers (GenerateTypeOptionsResult options = null)
			{
				var members = new List<ISymbol> ();
				AddMembers (members, options);

				if (_state.IsException) {
					AddExceptionConstructors (members);
				}

				return members;
			}

			private void AddMembers (IList<ISymbol> members, GenerateTypeOptionsResult options = null)
			{
				AddProperties (members);

				IList<TArgumentSyntax> argumentList;
				if (!_service.TryGetArgumentList (_state.ObjectCreationExpressionOpt, out argumentList)) {
					return;
				}

				var parameterTypes = GetArgumentTypes (argumentList);

				// Don't generate this constructor if it would conflict with a default exception
				// constructor.  Default exception constructors will be added automatically by our
				// caller.
				if (_state.IsException &&
				    _state.BaseTypeOrInterfaceOpt.InstanceConstructors.Any (
					    c => c.Parameters.Select (p => p.Type).SequenceEqual (parameterTypes))) {
					return;
				}

				// If there's an accessible base constructor that would accept these types, then
				// just call into that instead of generating fields.
				if (_state.BaseTypeOrInterfaceOpt != null) {
					if (_state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface && argumentList.Count == 0) {
						// No need to add the default constructor if our base type is going to be
						// 'object'.  We get that constructor for free.
						return;
					}

					var accessibleInstanceConstructors = _state.BaseTypeOrInterfaceOpt.InstanceConstructors.Where (
						                                     IsSymbolAccessible).ToSet ();

					if (accessibleInstanceConstructors.Any ()) {
						var delegatedConstructor = _service.GetDelegatingConstructor (_state.ObjectCreationExpressionOpt, _state.BaseTypeOrInterfaceOpt, _document.SemanticModel, accessibleInstanceConstructors, _cancellationToken);
						if (delegatedConstructor != null) {
							// There was a best match.  Call it directly.  
							AddBaseDelegatingConstructor (delegatedConstructor, members);
							return;
						}
					}
				}

				// Otherwise, just generate a normal constructor that assigns any provided
				// parameters into fields.
				AddFieldDelegatingConstructor (argumentList, members, options);
			}

			private void AddProperties (IList<ISymbol> members)
			{
				foreach (var property in _state.PropertiesToGenerate) {
					IPropertySymbol generatedProperty;
					if (_service.TryGenerateProperty (property, _document.SemanticModel, _cancellationToken, out generatedProperty)) {
						members.Add (generatedProperty);
					}
				}
			}

			private void AddBaseDelegatingConstructor (
				IMethodSymbol methodSymbol,
				IList<ISymbol> members)
			{
				// If we're generating a constructor to delegate into the no-param base constructor
				// then we can just elide the constructor entirely.
				if (methodSymbol.Parameters.Length == 0) {
					return;
				}

				var factory = _document.Project.LanguageServices.GetService<SyntaxGenerator> ();
				members.Add (factory.CreateBaseDelegatingConstructor (
					methodSymbol, DetermineName ()));
			}

			private void AddFieldDelegatingConstructor (
				IList<TArgumentSyntax> argumentList, IList<ISymbol> members, GenerateTypeOptionsResult options = null)
			{
				var factory = _document.Project.LanguageServices.GetService<SyntaxGenerator> ();

				var availableTypeParameters = _service.GetAvailableTypeParameters (_state, _document.SemanticModel, _intoNamespace, _cancellationToken);
				var parameterTypes = GetArgumentTypes (argumentList);
				var parameterNames = _service.GenerateParameterNames (_document.SemanticModel, argumentList);
				var parameters = new List<IParameterSymbol> ();

				var parameterToExistingFieldMap = new Dictionary<string, ISymbol> ();
				var parameterToNewFieldMap = new Dictionary<string, string> ();

				for (var i = 0; i < parameterNames.Count; i++) {
					var refKind = argumentList [i].GetRefKindOfArgument ();

					var parameterName = parameterNames [i];
					var parameterType = (ITypeSymbol)parameterTypes [i];
					parameterType = parameterType.RemoveUnavailableTypeParameters (
						_document.SemanticModel.Compilation, availableTypeParameters);

					if (!TryFindMatchingField (parameterName, parameterType, parameterToExistingFieldMap, caseSensitive: true)) {
						if (!TryFindMatchingField (parameterName, parameterType, parameterToExistingFieldMap, caseSensitive: false)) {
							parameterToNewFieldMap [parameterName] = parameterName;
						}
					}

					parameters.Add (CodeGenerationSymbolFactory.CreateParameterSymbol (
						attributes: null,
						refKind: refKind,
						isParams: false,
						type: parameterType,
						name: parameterName));
				}

				// Empty Constructor for Struct is not allowed
				if (!(parameters.Count == 0 && options != null && (options.TypeKind == TypeKind.Struct || options.TypeKind == TypeKind.Structure))) {
					var symbols = factory.CreateFieldDelegatingConstructor (DetermineName (), null, parameters, parameterToExistingFieldMap, parameterToNewFieldMap, _cancellationToken);
					foreach (var c in symbols)
						members.Add (c);
				}
			}

			private void AddExceptionConstructors (IList<ISymbol> members)
			{
				var factory = _document.Project.LanguageServices.GetService<SyntaxGenerator> ();
				var exceptionType = _document.SemanticModel.Compilation.ExceptionType ();
				var constructors =
					exceptionType.InstanceConstructors
						.Where (c => c.DeclaredAccessibility == Accessibility.Public || c.DeclaredAccessibility == Accessibility.Protected)
						.Select (c => CodeGenerationSymbolFactory.CreateConstructorSymbol (
						attributes: null,
						accessibility: c.DeclaredAccessibility,
						modifiers: default(DeclarationModifiers),
						typeName: DetermineName (),
						parameters: c.Parameters,
						statements: null,
						baseConstructorArguments: c.Parameters.Length == 0 ? null : factory.CreateArguments (c.Parameters)));
				foreach (var c in constructors)
					members.Add (c);
			}

			private IList<AttributeData> DetermineAttributes ()
			{
				if (_state.IsException) {
					var serializableType = _document.SemanticModel.Compilation.SerializableAttributeType ();
					if (serializableType != null) {
						var attribute = CodeGenerationSymbolFactory.CreateAttributeData (serializableType);
						return new[] { attribute };
					}
				}

				return null;
			}

			private Accessibility DetermineAccessibility ()
			{
				return _service.GetAccessibility (_state, _document.SemanticModel, _intoNamespace, _cancellationToken);
			}

			private DeclarationModifiers DetermineModifiers ()
			{
				return default(DeclarationModifiers);
			}

			private INamedTypeSymbol DetermineBaseType ()
			{
				if (_state.BaseTypeOrInterfaceOpt == null || _state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface) {
					return null;
				}

				return RemoveUnavailableTypeParameters (_state.BaseTypeOrInterfaceOpt);
			}

			private IList<INamedTypeSymbol> DetermineInterfaces ()
			{
				if (_state.BaseTypeOrInterfaceOpt != null && _state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface) {
					var type = RemoveUnavailableTypeParameters (_state.BaseTypeOrInterfaceOpt);
					if (type != null) {
						return new[] { type };
					}
				}

				return SpecializedCollections.EmptyList<INamedTypeSymbol> ();
			}

			private INamedTypeSymbol RemoveUnavailableTypeParameters (INamedTypeSymbol type)
			{
				return type.RemoveUnavailableTypeParameters (
					_document.SemanticModel.Compilation, GetAvailableTypeParameters ()) as INamedTypeSymbol;
			}

			private string DetermineName ()
			{
				return GetTypeName (_state);
			}

			private IList<ITypeParameterSymbol> DetermineTypeParameters ()
			{
				return _service.GetTypeParameters (_state, _document.SemanticModel, _cancellationToken);
			}

			private TypeKind DetermineTypeKind ()
			{
				return _state.IsStruct
					? TypeKind.Struct
						: _state.IsInterface
					? TypeKind.Interface
						: TypeKind.Class;
			}

			protected IList<ITypeParameterSymbol> GetAvailableTypeParameters ()
			{
				var availableInnerTypeParameters = _service.GetTypeParameters (_state, _document.SemanticModel, _cancellationToken);
				var availableOuterTypeParameters = !_intoNamespace && _state.TypeToGenerateInOpt != null
					? _state.TypeToGenerateInOpt.GetAllTypeParameters ()
					: SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol> ();

				return availableOuterTypeParameters.Concat (availableInnerTypeParameters).ToList ();
			}
		}

		internal abstract bool TryGenerateProperty (TSimpleNameSyntax propertyName, SemanticModel semanticModel, CancellationToken cancellationToken, out IPropertySymbol property);

		protected class State
		{
			public string Name { get; private set; }

			public bool NameIsVerbatim { get; private set; }

			// The name node that we're on.  Will be used to the name the type if it's
			// generated.
			public TSimpleNameSyntax SimpleName { get; private set; }

			// The entire expression containing the name, not including the creation.  i.e. "X.Foo"
			// in "new X.Foo()".
			public TExpressionSyntax NameOrMemberAccessExpression { get; private set; }

			// The object creation node if we have one.  i.e. if we're on the 'Foo' in "new X.Foo()".
			public TObjectCreationExpressionSyntax ObjectCreationExpressionOpt { get; private set; }

			// One of these will be non null.  It's also possible for both to be non null. For
			// example, if you have "class C { Foo f; }", then "Foo" can be generated inside C or
			// inside the global namespace.  The namespace can be null or the type can be null if the
			// user has something like "ExistingType.NewType" or "ExistingNamespace.NewType".  In
			// that case they're being explicit about what they want to generate into.
			public INamedTypeSymbol TypeToGenerateInOpt { get; private set; }

			public string NamespaceToGenerateInOpt { get; private set; }

			// If we can infer a base type or interface for this type.
			//
			// i.e.: "IList<int> foo = new MyList();"
			public INamedTypeSymbol BaseTypeOrInterfaceOpt { get; private set; }

			public bool IsInterface { get; private set; }

			public bool IsStruct { get; private set; }

			public bool IsAttribute { get; private set; }

			public bool IsException { get; private set; }

			public bool IsMembersWithModule { get; private set; }

			public bool IsTypeGeneratedIntoNamespaceFromMemberAccess { get; private set; }

			public bool IsSimpleNameGeneric { get; private set; }

			public bool IsPublicAccessibilityForTypeGeneration { get; private set; }

			public bool IsInterfaceOrEnumNotAllowedInTypeContext { get; private set; }

			public IMethodSymbol DelegateMethodSymbol { get; private set; }

			public bool IsDelegateAllowed { get; private set; }

			public bool IsEnumNotAllowed { get; private set; }

			public Compilation Compilation { get; }

			public bool IsDelegateOnly { get; private set; }

			public bool IsClassInterfaceTypes { get; private set; }

			public List<TSimpleNameSyntax> PropertiesToGenerate { get; private set; }

			private State (Compilation compilation)
			{
				this.Compilation = compilation;
			}

			public static State Generate (
				TService service,
				SemanticDocument document,
				SyntaxNode node,
				CancellationToken cancellationToken)
			{
				var state = new State (document.SemanticModel.Compilation);
				if (!state.TryInitialize (service, document, node, cancellationToken)) {
					return null;
				}

				return state;
			}

			private bool TryInitialize (
				TService service,
				SemanticDocument document,
				SyntaxNode node,
				CancellationToken cancellationToken)
			{
				if (!(node is TSimpleNameSyntax)) {
					return false;
				}

				this.SimpleName = (TSimpleNameSyntax)node;
				string name;
				int arity;
				this.SimpleName.GetNameAndArityOfSimpleName (out name, out arity);

				this.Name = name;
				this.NameIsVerbatim = this.SimpleName.GetFirstToken ().IsVerbatimIdentifier ();
				if (string.IsNullOrWhiteSpace (this.Name)) {
					return false;
				}

				// We only support simple names or dotted names.  i.e. "(some + expr).Foo" is not a
				// valid place to generate a type for Foo.
				GenerateTypeServiceStateOptions generateTypeServiceStateOptions;
				if (!service.TryInitializeState (document, this.SimpleName, cancellationToken, out generateTypeServiceStateOptions)) {
					return false;
				}

				this.NameOrMemberAccessExpression = generateTypeServiceStateOptions.NameOrMemberAccessExpression;
				this.ObjectCreationExpressionOpt = generateTypeServiceStateOptions.ObjectCreationExpressionOpt;

				var semanticModel = document.SemanticModel;
				var info = semanticModel.GetSymbolInfo (this.SimpleName, cancellationToken);
				if (info.Symbol != null) {
					// This bound, so no need to generate anything.
					return false;
				}

				if (!semanticModel.IsTypeContext (this.NameOrMemberAccessExpression.SpanStart, cancellationToken) &&
					!semanticModel.IsExpressionContext (this.NameOrMemberAccessExpression.SpanStart, cancellationToken) &&
					!semanticModel.IsStatementContext (this.NameOrMemberAccessExpression.SpanStart, cancellationToken) &&
					!semanticModel.IsNameOfContext (this.NameOrMemberAccessExpression.SpanStart, cancellationToken) &&
					!semanticModel.IsNamespaceContext (this.NameOrMemberAccessExpression.SpanStart, cancellationToken)) {
					return false;
				}

				// If this isn't something that can be created, then don't bother offering to create
				// it.
				if (info.CandidateReason == CandidateReason.NotCreatable) {
					return false;
				}

				if (info.CandidateReason == CandidateReason.Inaccessible ||
				   info.CandidateReason == CandidateReason.NotReferencable ||
				   info.CandidateReason == CandidateReason.OverloadResolutionFailure) {
					// We bound to something inaccessible, or overload resolution on a 
					// constructor call failed.  Don't want to offer GenerateType here.
					return false;
				}

				if (this.ObjectCreationExpressionOpt != null) {
					// If we're new'ing up something illegal, then don't offer generate type.
					var typeInfo = semanticModel.GetTypeInfo (this.ObjectCreationExpressionOpt, cancellationToken);
					if (typeInfo.Type.IsModuleType ()) {
						return false;
					}
				}

				DetermineNamespaceOrTypeToGenerateIn (service, document, cancellationToken);

				// Now, try to infer a possible base type for this new class/interface.
				this.InferBaseType (service, document, cancellationToken);
				this.IsInterface = GenerateInterface (service, cancellationToken);
				this.IsStruct = GenerateStruct (service, semanticModel, cancellationToken);
				this.IsAttribute = this.BaseTypeOrInterfaceOpt != null && this.BaseTypeOrInterfaceOpt.Equals (semanticModel.Compilation.AttributeType ());
				this.IsException = this.BaseTypeOrInterfaceOpt != null && this.BaseTypeOrInterfaceOpt.Equals (semanticModel.Compilation.ExceptionType ());
				this.IsMembersWithModule = generateTypeServiceStateOptions.IsMembersWithModule;
				this.IsTypeGeneratedIntoNamespaceFromMemberAccess = generateTypeServiceStateOptions.IsTypeGeneratedIntoNamespaceFromMemberAccess;
				this.IsInterfaceOrEnumNotAllowedInTypeContext = generateTypeServiceStateOptions.IsInterfaceOrEnumNotAllowedInTypeContext;
				this.IsDelegateAllowed = generateTypeServiceStateOptions.IsDelegateAllowed;
				this.IsDelegateOnly = generateTypeServiceStateOptions.IsDelegateOnly;
				this.IsEnumNotAllowed = generateTypeServiceStateOptions.IsEnumNotAllowed;
				this.DelegateMethodSymbol = generateTypeServiceStateOptions.DelegateCreationMethodSymbol;
				this.IsClassInterfaceTypes = generateTypeServiceStateOptions.IsClassInterfaceTypes;
				this.IsSimpleNameGeneric = service.IsGenericName (this.SimpleName);
				this.PropertiesToGenerate = generateTypeServiceStateOptions.PropertiesToGenerate;

				if (this.IsAttribute && this.TypeToGenerateInOpt.GetAllTypeParameters ().Any ()) {
					this.TypeToGenerateInOpt = null;
				}

				return this.TypeToGenerateInOpt != null || this.NamespaceToGenerateInOpt != null;
			}

			private void InferBaseType (
				TService service,
				SemanticDocument document,
				CancellationToken cancellationToken)
			{
				// See if we can find a possible base type for the type being generated.
				// NOTE(cyrusn): I currently limit this to when we have an object creation node.
				// That's because that's when we would have an expression that could be conerted to
				// somethign else.  i.e. if the user writes "IList<int> list = new Foo()" then we can
				// infer a base interface for 'Foo'.  However, if they write "IList<int> list = Foo"
				// then we don't really want to infer a base type for 'Foo'.

				// However, there are a few other cases were we can infer a base type.
				if (service.IsInCatchDeclaration (this.NameOrMemberAccessExpression)) {
					this.BaseTypeOrInterfaceOpt = document.SemanticModel.Compilation.ExceptionType ();
				} else if (NameOrMemberAccessExpression.IsAttributeName ()) {
					this.BaseTypeOrInterfaceOpt = document.SemanticModel.Compilation.AttributeType ();
				} else if (
					service.IsArrayElementType (this.NameOrMemberAccessExpression) ||
					service.IsInVariableTypeContext (this.NameOrMemberAccessExpression) ||
					this.ObjectCreationExpressionOpt != null) {
					var expr = this.ObjectCreationExpressionOpt ?? this.NameOrMemberAccessExpression;
					var baseType = TypeGuessing.typeInferenceService.InferType (document.SemanticModel, expr, objectAsDefault: true, cancellationToken: cancellationToken) as INamedTypeSymbol;
					SetBaseType (baseType);
				}
			}

			private void SetBaseType (INamedTypeSymbol baseType)
			{
				if (baseType == null) {
					return;
				}

				// A base type need to be non class or interface type.  Also, being 'object' is
				// redundant as the base type.  
				if (baseType.IsSealed || baseType.IsStatic || baseType.SpecialType == SpecialType.System_Object) {
					return;
				}

				if (baseType.TypeKind != TypeKind.Class && baseType.TypeKind != TypeKind.Interface) {
					return;
				}

				this.BaseTypeOrInterfaceOpt = baseType;
			}

			private bool GenerateStruct (TService service, SemanticModel semanticModel, CancellationToken cancellationToken)
			{
				return service.IsInValueTypeConstraintContext (semanticModel, this.NameOrMemberAccessExpression, cancellationToken);
			}

			private bool GenerateInterface (
				TService service,
				CancellationToken cancellationToken)
			{
				if (!this.IsAttribute &&
				   !this.IsException &&
				   this.Name.LooksLikeInterfaceName () &&
				   this.ObjectCreationExpressionOpt == null &&
				   (this.BaseTypeOrInterfaceOpt == null || this.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface)) {
					return true;
				}

				return service.IsInInterfaceList (this.NameOrMemberAccessExpression);
			}

			private void DetermineNamespaceOrTypeToGenerateIn (
				TService service,
				SemanticDocument document,
				CancellationToken cancellationToken)
			{
				DetermineNamespaceOrTypeToGenerateInWorker (service, document.SemanticModel, cancellationToken);

				// Can only generate into a type if it's a class and it's from source.
				if (this.TypeToGenerateInOpt != null) {
					if (this.TypeToGenerateInOpt.TypeKind != TypeKind.Class &&
					   this.TypeToGenerateInOpt.TypeKind != TypeKind.Module) {
						this.TypeToGenerateInOpt = null;
					} else {
						var symbol = SymbolFinder.FindSourceDefinitionAsync (this.TypeToGenerateInOpt, document.Project.Solution, cancellationToken).WaitAndGetResult (cancellationToken);
						if (symbol == null ||
						   !symbol.IsKind (SymbolKind.NamedType) ||
						   !symbol.Locations.Any (loc => loc.IsInSource)) {
							this.TypeToGenerateInOpt = null;
							return;
						}

						var sourceTreeToBeGeneratedIn = symbol.Locations.First (loc => loc.IsInSource).SourceTree;
						var documentToBeGeneratedIn = document.Project.Solution.GetDocument (sourceTreeToBeGeneratedIn);

						if (documentToBeGeneratedIn == null) {
							this.TypeToGenerateInOpt = null;
							return;
						}

						// If the 2 documents are in different project then we must have Public Accessibility.
						// If we are generating in a website project, we also want to type to be public so the 
						// designer files can access the type.
						if (documentToBeGeneratedIn.Project != document.Project ||
						   service.GeneratedTypesMustBePublic (documentToBeGeneratedIn.Project)) {
							this.IsPublicAccessibilityForTypeGeneration = true;
						}

						this.TypeToGenerateInOpt = (INamedTypeSymbol)symbol;
					}
				}

				if (this.TypeToGenerateInOpt != null) {
					if (!CodeGenerator.CanAdd (document.Project.Solution, this.TypeToGenerateInOpt, cancellationToken)) {
						this.TypeToGenerateInOpt = null;
					}
				}
			}

			private bool DetermineNamespaceOrTypeToGenerateInWorker (
				TService service,
				SemanticModel semanticModel,
				CancellationToken cancellationToken)
			{
				// If we're on the right of a dot, see if we can figure out what's on the left.  If
				// it doesn't bind to a type or a namespace, then we can't proceed.
				if (this.SimpleName != this.NameOrMemberAccessExpression) {
					return DetermineNamespaceOrTypeToGenerateIn (
						service, semanticModel,
						service.GetLeftSideOfDot (this.SimpleName), cancellationToken);
				} else {
					// The name is standing alone.  We can either generate the type into our
					// containing type, or into our containing namespace.
					//
					// TODO(cyrusn): We need to make this logic work if the type is in the
					// base/interface list of a type.
					var format = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle (SymbolDisplayGlobalNamespaceStyle.Omitted);
					this.TypeToGenerateInOpt = service.DetermineTypeToGenerateIn (semanticModel, this.SimpleName, cancellationToken);
					if (this.TypeToGenerateInOpt != null) {
						this.NamespaceToGenerateInOpt = this.TypeToGenerateInOpt.ContainingNamespace.ToDisplayString (format);
					} else {
						var namespaceSymbol = semanticModel.GetEnclosingNamespace (this.SimpleName.SpanStart, cancellationToken);
						if (namespaceSymbol != null) {
							this.NamespaceToGenerateInOpt = namespaceSymbol.ToDisplayString (format);
						}
					}
				}

				return true;
			}

			private bool DetermineNamespaceOrTypeToGenerateIn (
				TService service,
				SemanticModel semanticModel,
				TExpressionSyntax leftSide,
				CancellationToken cancellationToken)
			{
				var leftSideInfo = semanticModel.GetSymbolInfo (leftSide, cancellationToken);

				if (leftSideInfo.Symbol != null) {
					var symbol = leftSideInfo.Symbol;

					if (symbol is INamespaceSymbol) {
						this.NamespaceToGenerateInOpt = symbol.ToDisplayString (Ambience.NameFormat);
						return true;
					} else if (symbol is INamedTypeSymbol) {
						// TODO: Code coverage
						this.TypeToGenerateInOpt = (INamedTypeSymbol)symbol.OriginalDefinition;
						return true;
					}

					// We bound to something other than a namespace or named type.  Can't generate a
					// type inside this.
					return false;
				} else {
					// If it's a dotted name, then perhaps it's a namespace.  i.e. the user wrote
					// "new Foo.Bar.Baz()".  In this case we want to generate a namespace for
					// "Foo.Bar".
					IList<string> nameParts;
					if (service.TryGetNameParts (leftSide, out nameParts)) {
						this.NamespaceToGenerateInOpt = string.Join (".", nameParts);
						return true;
					}
				}

				return false;
			}
		}

		protected class GenerateTypeServiceStateOptions
		{
			public TExpressionSyntax NameOrMemberAccessExpression { get; set; }

			public TObjectCreationExpressionSyntax ObjectCreationExpressionOpt { get; set; }

			public IMethodSymbol DelegateCreationMethodSymbol { get; set; }

			public List<TSimpleNameSyntax> PropertiesToGenerate { get; }

			public bool IsMembersWithModule { get; set; }

			public bool IsTypeGeneratedIntoNamespaceFromMemberAccess { get; set; }

			public bool IsInterfaceOrEnumNotAllowedInTypeContext { get; set; }

			public bool IsDelegateAllowed { get; set; }

			public bool IsEnumNotAllowed { get; set; }

			public bool IsDelegateOnly { get; internal set; }

			public bool IsClassInterfaceTypes { get; internal set; }

			public GenerateTypeServiceStateOptions ()
			{
				NameOrMemberAccessExpression = null;
				ObjectCreationExpressionOpt = null;
				DelegateCreationMethodSymbol = null;
				IsMembersWithModule = false;
				PropertiesToGenerate = new List<TSimpleNameSyntax> ();
				IsTypeGeneratedIntoNamespaceFromMemberAccess = false;
				IsInterfaceOrEnumNotAllowedInTypeContext = false;
				IsDelegateAllowed = true;
				IsEnumNotAllowed = false;
				IsDelegateOnly = false;
			}
		}

	}
}
