// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 975 $</version>
// </file>

using System;
using System.Collections.Generic;

namespace NRefactoryASTGenerator.AST
{
	[CustomImplementation]
	abstract class Expression : AbstractNode {}
	
	[CustomImplementation]
	class PrimitiveExpression : Expression {}
	
	enum ParamModifier { In }
	
	class ParameterDeclarationExpression : Expression {
		List<AttributeSection> attributes;
		[QuestionMarkDefault]
		string         parameterName;
		TypeReference  typeReference;
		ParamModifier  paramModifier;
		Expression     defaultValue;
		
		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName) {}
		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName, ParamModifier paramModifier) {}
		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName, ParamModifier paramModifier, Expression defaultValue) {}
	}
	
	class NamedArgumentExpression : Expression {
		string     name;
		Expression expression;
		
		public NamedArgumentExpression(string name, Expression expression) {}
	}
	
	class ArrayCreateExpression : Expression {
		TypeReference              createType;
		List<Expression>           arguments;
		ArrayInitializerExpression arrayInitializer;
		
		public ArrayCreateExpression(TypeReference createType) {}
		public ArrayCreateExpression(TypeReference createType, List<Expression> arguments) {}
		public ArrayCreateExpression(TypeReference createType, ArrayInitializerExpression arrayInitializer) {}
	}
	
	[ImplementNullable(NullableImplementation.Shadow)]
	class ArrayInitializerExpression : Expression {
		List<Expression> createExpressions;
		
		public ArrayInitializerExpression() {}
		public ArrayInitializerExpression(List<Expression> createExpressions) {}
	}
	
	enum AssignmentOperatorType {}
	
	class AssignmentExpression : Expression {
		Expression             left;
		AssignmentOperatorType op;
		Expression             right;
		
		public AssignmentExpression(Expression left, AssignmentOperatorType op, Expression right) {}
	}
	
	class BaseReferenceExpression : Expression {}
	
	enum BinaryOperatorType {}
	
	class BinaryOperatorExpression : Expression
	{
		Expression         left;
		BinaryOperatorType op;
		Expression         right;
		
		public BinaryOperatorExpression(Expression left, BinaryOperatorType op, Expression right) {}
	}
	
	enum CastType {}
	
	class CastExpression : Expression
	{
		TypeReference castTo;
		Expression    expression;
		CastType      castType;
		
		public CastExpression(TypeReference castTo) {}
		public CastExpression(TypeReference castTo, Expression expression, CastType castType) {}
	}
	
	class FieldReferenceExpression : Expression
	{
		Expression targetObject;
		string     fieldName;
		
		public FieldReferenceExpression(Expression targetObject, string fieldName) {}
	}
	
	class IdentifierExpression : Expression {
		string identifier;
		
		public IdentifierExpression(string identifier) {}
	}
	
	class InvocationExpression : Expression {
		Expression          targetObject;
		List<Expression>    arguments;
		List<TypeReference> typeArguments;
		
		public InvocationExpression(Expression targetObject) {}
		public InvocationExpression(Expression targetObject, List<Expression> arguments) {}
		public InvocationExpression(Expression targetObject, List<Expression> arguments, List<TypeReference> typeArguments) {}
	}
	
	class ObjectCreateExpression : Expression {
		TypeReference    createType;
		List<Expression> parameters;
		
		public ObjectCreateExpression(TypeReference createType, List<Expression> parameters) {}
	}
	
	class ParenthesizedExpression : Expression {
		Expression expression;
		
		public ParenthesizedExpression(Expression expression) {}
	}
	
	class ThisReferenceExpression : Expression {}
	
	class TypeOfExpression : Expression {
		TypeReference typeReference;
		
		public TypeOfExpression(TypeReference typeReference) {}
	}
	
	[IncludeMember("public TypeReferenceExpression(string typeName) : this(new TypeReference(typeName)) {}")]
	class TypeReferenceExpression : Expression {
		TypeReference typeReference;
		
		public TypeReferenceExpression(TypeReference typeReference) {}
	}
	
	enum UnaryOperatorType {}
	
	class UnaryOperatorExpression : Expression {
		UnaryOperatorType op;
		Expression        expression;
		
		public UnaryOperatorExpression(UnaryOperatorType op) {}
		public UnaryOperatorExpression(Expression expression, UnaryOperatorType op) {}
	}
	
	class AnonymousMethodExpression : Expression {
		List<ParameterDeclarationExpression> parameters;
		BlockStatement body;
	}
	
	class CheckedExpression : Expression {
		Expression expression;
		
		public CheckedExpression(Expression expression) {}
	}
	
	class ConditionalExpression : Expression {
		Expression condition;
		Expression trueExpression;
		Expression falseExpression;
		
		public ConditionalExpression(Expression condition, Expression trueExpression, Expression falseExpression) {}
	}
	
	class DefaultValueExpression : Expression {
		TypeReference typeReference;
		
		public DefaultValueExpression(TypeReference typeReference) {}
	}
	
	enum FieldDirection {}
	
	class DirectionExpression : Expression {
		FieldDirection fieldDirection;
		Expression     expression;
		
		public DirectionExpression(FieldDirection fieldDirection, Expression expression) {}
	}
	
	class IndexerExpression : Expression {
		Expression       targetObject;
		List<Expression> indices;
		
		public IndexerExpression(Expression targetObject, List<Expression> indices) {}
	}
	
	class PointerReferenceExpression : Expression {
		Expression targetObject;
		string     identifier;
		
		public PointerReferenceExpression(Expression targetObject, string identifier) {}
	}
	
	class SizeOfExpression : Expression {
		TypeReference typeReference;
		
		public SizeOfExpression(TypeReference typeReference) {}
	}
	
	class StackAllocExpression : Expression {
		TypeReference typeReference;
		Expression    expression;
		
		public StackAllocExpression(TypeReference typeReference, Expression expression) {}
	}
	
	class UncheckedExpression : Expression {
		Expression expression;
		
		public UncheckedExpression(Expression expression) {}
	}
	
	class AddressOfExpression : Expression {
		Expression expression;
		
		public AddressOfExpression(Expression expression) {}
	}
	
	class ClassReferenceExpression : Expression {}
	
	class TypeOfIsExpression : Expression {
		Expression    expression;
		TypeReference typeReference;
		
		public TypeOfIsExpression(Expression expression, TypeReference typeReference) {}
	}
}
