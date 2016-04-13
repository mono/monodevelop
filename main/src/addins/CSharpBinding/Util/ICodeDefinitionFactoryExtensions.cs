using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.ExceptionServices;


namespace ICSharpCode.NRefactory6.CSharp
{
	static class ICodeDefinitionFactoryExtensions
	{
		readonly static Type typeInfo;

		static ICodeDefinitionFactoryExtensions ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.Shared.Extensions.ICodeDefinitionFactoryExtensions" + ReflectionNamespaces.WorkspacesAsmName, true);
			createFieldDelegatingConstructorMethod = typeInfo.GetMethod ("CreateFieldDelegatingConstructor", BindingFlags.Static | BindingFlags.Public);
			createFieldsForParametersMethod = typeInfo.GetMethod ("CreateFieldsForParameters", BindingFlags.Static | BindingFlags.Public);
			createAssignmentStatementMethod = typeInfo.GetMethod ("CreateAssignmentStatements", BindingFlags.Static | BindingFlags.Public);
			createThrowNotImplementStatementMethod = typeInfo.GetMethod ("CreateThrowNotImplementStatement", new [] { typeof (SyntaxGenerator), typeof(Compilation) });

		}

		public static IList<SyntaxNode> CreateThrowNotImplementedStatementBlock(
			this SyntaxGenerator codeDefinitionFactory,
			Compilation compilation)
		{
			return new[] { CreateThrowNotImplementStatement(codeDefinitionFactory, compilation) };
		}


		static MethodInfo createThrowNotImplementStatementMethod;
		public static SyntaxNode CreateThrowNotImplementStatement(
			this SyntaxGenerator codeDefinitionFactory,
			Compilation compilation)
		{
			try {
				return (SyntaxNode)createThrowNotImplementStatementMethod.Invoke (null, new object[] { codeDefinitionFactory, compilation });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}


		readonly static MethodInfo createFieldDelegatingConstructorMethod;

		public static IEnumerable<ISymbol> CreateFieldDelegatingConstructor(
			this SyntaxGenerator factory,
			string typeName,
			INamedTypeSymbol containingTypeOpt,
			IList<IParameterSymbol> parameters,
			IDictionary<string, ISymbol> parameterToExistingFieldMap,
			IDictionary<string, string> parameterToNewFieldMap,
			CancellationToken cancellationToken)
		{
			try {
				return (IEnumerable<ISymbol>)createFieldDelegatingConstructorMethod.Invoke (null, new object[] {
					factory,
					typeName,
					containingTypeOpt,
					parameters,
					parameterToExistingFieldMap,
					parameterToNewFieldMap,
					cancellationToken 
				});
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		readonly static MethodInfo createFieldsForParametersMethod;

		public static IEnumerable<IFieldSymbol> CreateFieldsForParameters(
			this SyntaxGenerator factory,
			IList<IParameterSymbol> parameters,
			IDictionary<string, string> parameterToNewFieldMap)
		{
			try {
				return (IEnumerable<IFieldSymbol>)createFieldsForParametersMethod.Invoke (null, new object[] {
					factory,
					parameters,
					parameterToNewFieldMap 
				});
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		readonly static MethodInfo createAssignmentStatementMethod;
	
		public static IEnumerable<SyntaxNode> CreateAssignmentStatements(
			this SyntaxGenerator factory,
			IList<IParameterSymbol> parameters,
			IDictionary<string, ISymbol> parameterToExistingFieldMap,
			IDictionary<string, string> parameterToNewFieldMap)
		{
			try {
				return (IEnumerable<SyntaxNode>)createAssignmentStatementMethod.Invoke (null, new object[] {
					factory,
					parameters,
					parameterToExistingFieldMap,
					parameterToNewFieldMap 
				});
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		public static IList<SyntaxNode> CreateArguments(
			this SyntaxGenerator factory,
			ImmutableArray<IParameterSymbol> parameters)
		{
			return parameters.Select(p => CreateArgument(factory, p)).ToList();
		}

		private static SyntaxNode CreateArgument(
			this SyntaxGenerator factory,
			IParameterSymbol parameter)
		{
			return factory.Argument(parameter.RefKind, factory.IdentifierName(parameter.Name));
		}

		public static IMethodSymbol CreateBaseDelegatingConstructor(
			this SyntaxGenerator factory,
			IMethodSymbol constructor,
			string typeName)
		{
			return CodeGenerationSymbolFactory.CreateConstructorSymbol(
				attributes: null,
				accessibility: Accessibility.Public,
				modifiers: new DeclarationModifiers(),
				typeName: typeName,
				parameters: constructor.Parameters,
				statements: null,
				baseConstructorArguments: constructor.Parameters.Length == 0 ? null : factory.CreateArguments(constructor.Parameters));
		}

	}
}
