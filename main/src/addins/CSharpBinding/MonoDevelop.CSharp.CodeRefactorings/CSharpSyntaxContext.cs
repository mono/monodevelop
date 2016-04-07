/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.ExceptionServices;

namespace ICSharpCode.NRefactory6.CSharp
{
	public class ReflectionNamespaces
	{
		public const string WorkspacesAsmName = ", Microsoft.CodeAnalysis.Workspaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
		public const string CSWorkspacesAsmName = ", Microsoft.CodeAnalysis.CSharp.Workspaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
		public const string CAAsmName = ", Microsoft.CodeAnalysis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
		public const string CACSharpAsmName = ", Microsoft.CodeAnalysis.CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
	}

	public class CSharpSyntaxContext
	{
		readonly static Type typeInfoCSharpSyntaxContext;
		readonly static Type typeInfoAbstractSyntaxContext;
		readonly static MethodInfo createContextMethod;
		readonly static PropertyInfo leftTokenProperty;
		readonly static PropertyInfo targetTokenProperty;
		readonly static FieldInfo isIsOrAsTypeContextField;
		readonly static FieldInfo isInstanceContextField;
		readonly static FieldInfo isNonAttributeExpressionContextField;
		readonly static FieldInfo isPreProcessorKeywordContextField;
		readonly static FieldInfo isPreProcessorExpressionContextField;
		readonly static FieldInfo containingTypeDeclarationField;
		readonly static FieldInfo isGlobalStatementContextField;
		readonly static FieldInfo isParameterTypeContextField;
		readonly static PropertyInfo syntaxTreeProperty;


		object instance;

		public SyntaxToken LeftToken {
			get {
				return (SyntaxToken)leftTokenProperty.GetValue (instance);
			}
		}

		public SyntaxToken TargetToken {
			get {
				return (SyntaxToken)targetTokenProperty.GetValue (instance);
			}
		}

		public bool IsIsOrAsTypeContext {
			get {
				return (bool)isIsOrAsTypeContextField.GetValue (instance);
			}
		}

		public bool IsInstanceContext {
			get {
				return (bool)isInstanceContextField.GetValue (instance);
			}
		}

		public bool IsNonAttributeExpressionContext {
			get {
				return (bool)isNonAttributeExpressionContextField.GetValue (instance);
			}
		}

		public bool IsPreProcessorKeywordContext {
			get {
				return (bool)isPreProcessorKeywordContextField.GetValue (instance);
			}
		}

		public bool IsPreProcessorExpressionContext {
			get {
				return (bool)isPreProcessorExpressionContextField.GetValue (instance);
			}
		}

		public TypeDeclarationSyntax ContainingTypeDeclaration {
			get {
				return (TypeDeclarationSyntax)containingTypeDeclarationField.GetValue (instance);
			}
		}

		public bool IsGlobalStatementContext {
			get {
				return (bool)isGlobalStatementContextField.GetValue (instance);
			}
		}

		public bool IsParameterTypeContext {
			get {
				return (bool)isParameterTypeContextField.GetValue (instance);
			}
		}

		public SyntaxTree SyntaxTree {
			get {
				return (SyntaxTree)syntaxTreeProperty.GetValue (instance);
			}
		}


		readonly static MethodInfo isMemberDeclarationContextMethod;

		public bool IsMemberDeclarationContext (
			ISet<SyntaxKind> validModifiers = null,
			ISet<SyntaxKind> validTypeDeclarations = null,
			bool canBePartial = false,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (bool)isMemberDeclarationContextMethod.Invoke (instance, new object[] {
					validModifiers,
					validTypeDeclarations,
					canBePartial,
					cancellationToken
				});
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return false;
			}
		}

		readonly static MethodInfo isTypeDeclarationContextMethod;

		public bool IsTypeDeclarationContext (
			ISet<SyntaxKind> validModifiers = null,
			ISet<SyntaxKind> validTypeDeclarations = null,
			bool canBePartial = false,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (bool)isTypeDeclarationContextMethod.Invoke (instance, new object[] {
					validModifiers,
					validTypeDeclarations,
					canBePartial,
					cancellationToken
				});
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return false;
			}
		}

		readonly static PropertyInfo isPreProcessorDirectiveContextProperty;

		public bool IsPreProcessorDirectiveContext {
			get {
				return (bool)isPreProcessorDirectiveContextProperty.GetValue (instance);
			}
		}

		readonly static FieldInfo isInNonUserCodeField;

		public bool IsInNonUserCode {
			get {
				return (bool)isInNonUserCodeField.GetValue (instance);
			}
		}

		readonly static FieldInfo isIsOrAsContextField;

		public bool IsIsOrAsContext {
			get {
				return (bool)isIsOrAsContextField.GetValue (instance);
			}
		}

		readonly static MethodInfo isTypeAttributeContextMethod;

		public bool IsTypeAttributeContext (CancellationToken cancellationToken)
		{
			try {
				return (bool)isTypeAttributeContextMethod.Invoke (instance, new object[] { cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return false;
			}
		}

		readonly static PropertyInfo isAnyExpressionContextProperty;

		public bool IsAnyExpressionContext {
			get {
				return (bool)isAnyExpressionContextProperty.GetValue (instance);
			}
		}

		readonly static PropertyInfo isStatementContextProperty;

		public bool IsStatementContext {
			get {
				return (bool)isStatementContextProperty.GetValue (instance);
			}
		}

		readonly static FieldInfo isDefiniteCastTypeContextField;

		public bool IsDefiniteCastTypeContext {
			get {
				return (bool)isDefiniteCastTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isObjectCreationTypeContextField;

		public bool IsObjectCreationTypeContext {
			get {
				return (bool)isObjectCreationTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isGenericTypeArgumentContextField;

		public bool IsGenericTypeArgumentContext {
			get {
				return (bool)isGenericTypeArgumentContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isLocalVariableDeclarationContextField;

		public bool IsLocalVariableDeclarationContext {
			get {
				return (bool)isLocalVariableDeclarationContextField.GetValue (instance);
			}
		}


		readonly static FieldInfo isFixedVariableDeclarationContextField;

		public bool IsFixedVariableDeclarationContext {
			get {
				return (bool)isFixedVariableDeclarationContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isPossibleLambdaOrAnonymousMethodParameterTypeContextField;

		public bool IsPossibleLambdaOrAnonymousMethodParameterTypeContext {
			get {
				return (bool)isPossibleLambdaOrAnonymousMethodParameterTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isImplicitOrExplicitOperatorTypeContextField;

		public bool IsImplicitOrExplicitOperatorTypeContext {
			get {
				return (bool)isImplicitOrExplicitOperatorTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isPrimaryFunctionExpressionContextField;

		public bool IsPrimaryFunctionExpressionContext {
			get {
				return (bool)isPrimaryFunctionExpressionContextField.GetValue (instance);
			}
		}


		readonly static FieldInfo isCrefContextField;

		public bool IsCrefContext {
			get {
				return (bool)isCrefContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isDelegateReturnTypeContextField;

		public bool IsDelegateReturnTypeContext {
			get {
				return (bool)isDelegateReturnTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isEnumBaseListContextField;

		public bool IsEnumBaseListContext {
			get {
				return (bool)isEnumBaseListContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isConstantExpressionContextField;

		public bool IsConstantExpressionContext {
			get {
				return (bool)isConstantExpressionContextField.GetValue (instance);
			}
		}

		readonly static MethodInfo isMemberAttributeContextMethod;
		public bool IsMemberAttributeContext(ISet<SyntaxKind> validTypeDeclarations, CancellationToken cancellationToken)
		{
			try {
				return (bool)isMemberAttributeContextMethod.Invoke (instance, new object [] {
					validTypeDeclarations,
					cancellationToken
				});
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return false;
			}

		}

		readonly static FieldInfo precedingModifiersField;

		public ISet<SyntaxKind> PrecedingModifiers {
			get {
				return (ISet<SyntaxKind>)precedingModifiersField.GetValue (instance);
			}
		}

		readonly static FieldInfo isTypeOfExpressionContextField;

		public bool IsTypeOfExpressionContext {
			get {
				return (bool)isTypeOfExpressionContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo containingTypeOrEnumDeclarationField;

		public BaseTypeDeclarationSyntax ContainingTypeOrEnumDeclaration {
			get {
				return (BaseTypeDeclarationSyntax)containingTypeOrEnumDeclarationField.GetValue (instance);
			}
		}
		static readonly PropertyInfo isAttributeNameContextProperty;

		public bool IsAttributeNameContext {
			get {
				return (bool)isAttributeNameContextProperty.GetValue (instance);
			}
		}

		static readonly PropertyInfo isInQueryProperty;
		public bool IsInQuery {
			get {
				return (bool)isInQueryProperty.GetValue (instance);
			}
		}


		static CSharpSyntaxContext ()
		{
			typeInfoAbstractSyntaxContext = Type.GetType("Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery.AbstractSyntaxContext" + ReflectionNamespaces.WorkspacesAsmName, true);
			typeInfoCSharpSyntaxContext = Type.GetType ("Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery.CSharpSyntaxContext" + ReflectionNamespaces.CSWorkspacesAsmName, true);

			createContextMethod = typeInfoCSharpSyntaxContext.GetMethod ("CreateContext", BindingFlags.Static | BindingFlags.Public);
			leftTokenProperty = typeInfoAbstractSyntaxContext.GetProperty ("LeftToken");
			targetTokenProperty = typeInfoAbstractSyntaxContext.GetProperty ("TargetToken");
			isIsOrAsTypeContextField = typeInfoCSharpSyntaxContext.GetField ("IsIsOrAsTypeContext");
			isInstanceContextField = typeInfoCSharpSyntaxContext.GetField ("IsInstanceContext");
			isNonAttributeExpressionContextField = typeInfoCSharpSyntaxContext.GetField ("IsNonAttributeExpressionContext");
			isPreProcessorKeywordContextField = typeInfoCSharpSyntaxContext.GetField ("IsPreProcessorKeywordContext");
			isPreProcessorExpressionContextField = typeInfoCSharpSyntaxContext.GetField ("IsPreProcessorExpressionContext");
			containingTypeDeclarationField = typeInfoCSharpSyntaxContext.GetField ("ContainingTypeDeclaration");
			isGlobalStatementContextField = typeInfoCSharpSyntaxContext.GetField ("IsGlobalStatementContext");
			isParameterTypeContextField = typeInfoCSharpSyntaxContext.GetField ("IsParameterTypeContext");
			isMemberDeclarationContextMethod = typeInfoCSharpSyntaxContext.GetMethod ("IsMemberDeclarationContext", BindingFlags.Instance | BindingFlags.Public);
			isTypeDeclarationContextMethod = typeInfoCSharpSyntaxContext.GetMethod ("IsTypeDeclarationContext", BindingFlags.Instance | BindingFlags.Public);
			syntaxTreeProperty = typeInfoAbstractSyntaxContext.GetProperty ("SyntaxTree");
			isPreProcessorDirectiveContextProperty = typeInfoAbstractSyntaxContext.GetProperty ("IsPreProcessorDirectiveContext");
			isInNonUserCodeField = typeInfoCSharpSyntaxContext.GetField ("IsInNonUserCode");
			isIsOrAsContextField = typeInfoCSharpSyntaxContext.GetField ("IsIsOrAsContext");
			isTypeAttributeContextMethod = typeInfoCSharpSyntaxContext.GetMethod ("IsTypeAttributeContext", BindingFlags.Instance | BindingFlags.Public);
			isAnyExpressionContextProperty = typeInfoAbstractSyntaxContext.GetProperty ("IsAnyExpressionContext");
			isStatementContextProperty = typeInfoAbstractSyntaxContext.GetProperty ("IsStatementContext");
			isDefiniteCastTypeContextField = typeInfoCSharpSyntaxContext.GetField ("IsDefiniteCastTypeContext");
			isObjectCreationTypeContextField = typeInfoCSharpSyntaxContext.GetField ("IsObjectCreationTypeContext");
			isGenericTypeArgumentContextField = typeInfoCSharpSyntaxContext.GetField ("IsGenericTypeArgumentContext");
			isLocalVariableDeclarationContextField = typeInfoCSharpSyntaxContext.GetField ("IsLocalVariableDeclarationContext");
			isFixedVariableDeclarationContextField = typeInfoCSharpSyntaxContext.GetField ("IsFixedVariableDeclarationContext");
			isPossibleLambdaOrAnonymousMethodParameterTypeContextField = typeInfoCSharpSyntaxContext.GetField ("IsPossibleLambdaOrAnonymousMethodParameterTypeContext");
			isImplicitOrExplicitOperatorTypeContextField = typeInfoCSharpSyntaxContext.GetField ("IsImplicitOrExplicitOperatorTypeContext");
			isPrimaryFunctionExpressionContextField = typeInfoCSharpSyntaxContext.GetField ("IsPrimaryFunctionExpressionContext");
			isCrefContextField = typeInfoCSharpSyntaxContext.GetField ("IsCrefContext");
			isDelegateReturnTypeContextField = typeInfoCSharpSyntaxContext.GetField ("IsDelegateReturnTypeContext");
			isEnumBaseListContextField = typeInfoCSharpSyntaxContext.GetField ("IsEnumBaseListContext");
			isConstantExpressionContextField = typeInfoCSharpSyntaxContext.GetField ("IsConstantExpressionContext");
			isMemberAttributeContextMethod = typeInfoCSharpSyntaxContext.GetMethod ("IsMemberAttributeContext", BindingFlags.Instance | BindingFlags.Public);
			precedingModifiersField = typeInfoCSharpSyntaxContext.GetField ("PrecedingModifiers");
			isTypeOfExpressionContextField = typeInfoCSharpSyntaxContext.GetField ("IsTypeOfExpressionContext");
			containingTypeOrEnumDeclarationField = typeInfoCSharpSyntaxContext.GetField ("ContainingTypeOrEnumDeclaration");

			isAttributeNameContextProperty = typeInfoAbstractSyntaxContext.GetProperty ("IsAttributeNameContext");
			isInQueryProperty = typeInfoAbstractSyntaxContext.GetProperty ("IsInQuery");
		}

		public SemanticModel SemanticModel {
			get;
			private set;
		}

		public int Position {
			get;
			private set;
		}

		CSharpSyntaxContext (object instance)
		{
			this.instance = instance;
		}

		public static CSharpSyntaxContext CreateContext (Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			try {
				return new CSharpSyntaxContext (createContextMethod.Invoke (null, new object[] {
					workspace,
					semanticModel,
					position,
					cancellationToken
				})) {
					SemanticModel = semanticModel,
					Position = position
				};
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}
	}

	class CSharpTypeInferenceService
	{
		readonly static Type typeInfo;
		readonly static MethodInfo inferTypesMethod;
		readonly static MethodInfo inferTypes2Method;
		readonly object instance;

		static CSharpTypeInferenceService ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CSharp.CSharpTypeInferenceService" + ReflectionNamespaces.CSWorkspacesAsmName, true);

			inferTypesMethod = typeInfo.GetMethod ("InferTypes", new[] {
				typeof(SemanticModel),
				typeof(int),
				typeof(CancellationToken)
			});
			inferTypes2Method = typeInfo.GetMethod ("InferTypes", new[] {
				typeof(SemanticModel),
				typeof(SyntaxNode),
				typeof(CancellationToken)
			});
		}

		public CSharpTypeInferenceService ()
		{
			instance = Activator.CreateInstance (typeInfo);
		}

		public IEnumerable<ITypeSymbol> InferTypes (SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			try {
				return (IEnumerable<ITypeSymbol>)inferTypesMethod.Invoke (instance, new object[] {
					semanticModel,
					position,
					cancellationToken
				});
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		public IEnumerable<ITypeSymbol> InferTypes (SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
		{
			try {
				return (IEnumerable<ITypeSymbol>)inferTypes2Method.Invoke (instance, new object[] {
					semanticModel,
					expression,
					cancellationToken
				});
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}


		public ITypeSymbol InferType(
			SemanticModel semanticModel,
			SyntaxNode expression,
			bool objectAsDefault,
			CancellationToken cancellationToken)
		{
			var types = InferTypes(semanticModel, expression, cancellationToken)
				.WhereNotNull();

			if (!types.Any())
			{
				return objectAsDefault ? semanticModel.Compilation.ObjectType : null;
			}

			return types.FirstOrDefault();
		}


		public INamedTypeSymbol InferDelegateType(
			SemanticModel semanticModel,
			SyntaxNode expression,
			CancellationToken cancellationToken)
		{
			var type = this.InferType(semanticModel, expression, objectAsDefault: false, cancellationToken: cancellationToken);
			return type.GetDelegateType(semanticModel.Compilation);
		}


		public ITypeSymbol InferType(
			SemanticModel semanticModel,
			int position,
			bool objectAsDefault,
			CancellationToken cancellationToken)
		{
			var types = this.InferTypes(semanticModel, position, cancellationToken)
				.WhereNotNull();

			if (!types.Any())
			{
				return objectAsDefault ? semanticModel.Compilation.ObjectType : null;
			}

			return types.FirstOrDefault();
		}

	}

	public class CaseCorrector
	{
		readonly static Type typeInfo;
		readonly static MethodInfo caseCorrectAsyncMethod;

		static CaseCorrector ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CaseCorrection.CaseCorrector" + ReflectionNamespaces.WorkspacesAsmName, true);

			Annotation = (SyntaxAnnotation)typeInfo.GetField ("Annotation", BindingFlags.Public | BindingFlags.Static).GetValue (null);

			caseCorrectAsyncMethod = typeInfo.GetMethod ("CaseCorrectAsync", new[] {
				typeof(Document),
				typeof(SyntaxAnnotation),
				typeof(CancellationToken)
			});
		}

		public static readonly SyntaxAnnotation Annotation;

		public static Task<Document> CaseCorrectAsync (Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken)
		{
			try {
				return (Task<Document>)caseCorrectAsyncMethod.Invoke (null, new object[] { document, annotation, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}
	}


}
*/