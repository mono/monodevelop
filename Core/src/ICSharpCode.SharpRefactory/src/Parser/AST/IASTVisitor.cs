using System;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.Parser
{
	public interface IASTVisitor
	{
		// Errors
		Errors Errors {
			get;
			set;
		}

		object Visit(INode node, object data);
		
		object Visit(CompilationUnit compilationUnit, object data);
		object Visit(ExternAliasDeclaration externAliasDeclaration, object data);
		object Visit(NamespaceDeclaration namespaceDeclaration, object data);
		object Visit(UsingDeclaration usingDeclaration, object data);
		object Visit(UsingAliasDeclaration usingAliasDeclaration, object data);
		object Visit(AttributeSection attributeSection, object data);
		
		// Types
		object Visit(TypeDeclaration typeDeclaration, object data);
		object Visit(DelegateDeclaration delegateDeclaration, object data);
		
		// Members
		object Visit(VariableDeclaration variableDeclaration, object data);
		object Visit(FieldDeclaration    fieldDeclaration, object data);
		
		object Visit(MethodDeclaration methodDeclaration, object data);
		object Visit(PropertyDeclaration propertyDeclaration, object data);
		object Visit(PropertyGetRegion propertyGetRegion, object data);
		object Visit(PropertySetRegion PropertySetRegion, object data);
		object Visit(EventDeclaration eventDeclaration, object data);
		object Visit(EventAddRegion eventAddRegion, object data);
		object Visit(EventRemoveRegion eventRemoveRegion, object data);
		
		object Visit(ConstructorDeclaration constructorDeclaration, object data);
		object Visit(DestructorDeclaration destructorDeclaration, object data);
		object Visit(OperatorDeclaration operatorDeclaration, object data);
		object Visit(IndexerDeclaration indexerDeclaration, object data);
		
		// Statements
		object Visit(BlockStatement blockStatement, object data);
		object Visit(StatementExpression statementExpression, object data);
		object Visit(LocalVariableDeclaration variableDeclaration, object data);
		object Visit(EmptyStatement emptyStatement, object data);
		object Visit(ReturnStatement returnStatement, object data);
		object Visit(IfStatement ifStatement, object data);
		object Visit(IfElseStatement ifElseStatement, object data);
		object Visit(WhileStatement whileStatement, object data);
		object Visit(DoWhileStatement doWhileStatement, object data);
		object Visit(ForStatement forStatement, object data);
		object Visit(LabelStatement labelStatement, object data);
		object Visit(GotoStatement gotoStatement, object data);
		object Visit(SwitchStatement switchStatement, object data);
		object Visit(BreakStatement breakStatement, object data);
		object Visit(ContinueStatement continueStatement, object data);
		object Visit(GotoCaseStatement gotoCaseStatement, object data);
		object Visit(ForeachStatement foreachStatement, object data);
		object Visit(LockStatement lockStatement, object data);
		object Visit(UsingStatement usingStatement, object data);
		object Visit(TryCatchStatement tryCatchStatement, object data);
		object Visit(ThrowStatement throwStatement, object data);
		object Visit(FixedStatement fixedStatement, object data);
		object Visit(CheckedStatement checkedStatement, object data);
		object Visit(UncheckedStatement uncheckedStatement, object data);
		object Visit(UnsafeStatement unsafeStatement, object data);
		object Visit(YieldStatement returnStatement, object data);
		
		// Expressions
		object Visit(PrimitiveExpression      primitiveExpression, object data);
		object Visit(BinaryOperatorExpression binaryOperatorExpression, object data);
		object Visit(ParenthesizedExpression parenthesizedExpression, object data);
		object Visit(InvocationExpression invocationExpression, object data);
		object Visit(IdentifierExpression identifierExpression, object data);
		object Visit(TypeReferenceExpression typeReferenceExpression, object data);
		object Visit(UnaryOperatorExpression unaryOperatorExpression, object data);
		object Visit(AssignmentExpression assignmentExpression, object data);
		object Visit(SizeOfExpression sizeOfExpression, object data);
		object Visit(TypeOfExpression typeOfExpression, object data);
		object Visit(CheckedExpression checkedExpression, object data);
		object Visit(UncheckedExpression uncheckedExpression, object data);
		object Visit(PointerReferenceExpression pointerReferenceExpression, object data);
		object Visit(CastExpression castExpression, object data);
		object Visit(StackAllocExpression stackAllocExpression, object data);
		object Visit(IndexerExpression indexerExpression, object data);
		object Visit(ThisReferenceExpression thisReferenceExpression, object data);
		object Visit(BaseReferenceExpression baseReferenceExpression, object data);
		object Visit(ObjectCreateExpression objectCreateExpression, object data);
		object Visit(ArrayCreationParameter arrayCreationParameter, object data);
		object Visit(ArrayCreateExpression arrayCreateExpression, object data);
		object Visit(ParameterDeclarationExpression parameterDeclarationExpression, object data);
		object Visit(FieldReferenceExpression fieldReferenceExpression, object data);
		object Visit(DirectionExpression directionExpression, object data);
		object Visit(ArrayInitializerExpression arrayInitializerExpression, object data);
		object Visit(ConditionalExpression conditionalExpression, object data);
		
		
	}
}
