using System;
using ICSharpCode.SharpRefactory.Parser.AST.VB;

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	public interface IASTVisitor
	{
		// Abstract
		object Visit(INode node, object data);
		object Visit(CompilationUnit compilationUnit, object data);
		
		// Statements
		object Visit(Statement statement, object data);
		object Visit(StatementExpression statementExpression, object data);
		object Visit(LocalVariableDeclaration localVariableDeclaration, object data);
		object Visit(SimpleIfStatement ifStatement, object data);
		object Visit(IfStatement ifStatement, object data);
		object Visit(LabelStatement labelStatement, object data);
		object Visit(GoToStatement goToStatement, object data);
		object Visit(SelectStatement selectStatement, object data);
		object Visit(StopStatement stopStatement, object data);
		object Visit(ResumeStatement resumeStatement, object data);
		object Visit(EraseStatement eraseStatement, object data);
		object Visit(ErrorStatement errorStatement, object data);
		object Visit(OnErrorStatement onErrorStatement, object data);
		object Visit(ReDimStatement reDimStatement, object data);
		object Visit(AddHandlerStatement addHandlerStatement, object data);
		object Visit(BlockStatement blockStatement, object data);
		object Visit(DoLoopStatement doLoopStatement, object data);
		object Visit(EndStatement endStatement, object data);
		object Visit(ExitStatement exitStatement, object data);
		object Visit(ForeachStatement foreachStatement, object data);
		object Visit(ForStatement forStatement, object data);
		object Visit(LockStatement lockStatement, object data);
		object Visit(RaiseEventStatement raiseEventStatement, object data);
		object Visit(RemoveHandlerStatement removeHandlerStatement, object data);
		object Visit(ReturnStatement returnStatement, object data);
		object Visit(ThrowStatement throwStatement, object data);
		object Visit(TryCatchStatement tryCatchStatement, object data);
		object Visit(WhileStatement whileStatement, object data);
		object Visit(WithStatement withStatement, object data);
		
		// Declarations
		object Visit(VariableDeclaration variableDeclaration, object data);
		object Visit(FieldDeclaration    fieldDeclaration, object data);
		
		object Visit(MethodDeclaration methodDeclaration, object data);
		object Visit(ConstructorDeclaration constructorDeclaration, object data);
		object Visit(DeclareDeclaration declareDeclaration, object data);
		object Visit(PropertyDeclaration propertyDeclaration, object data);
		object Visit(PropertyGetRegion propertyGetRegion, object data);
		object Visit(PropertySetRegion PropertySetRegion, object data);
		object Visit(EventDeclaration eventDeclaration, object data);
		
		// Global scope
		object Visit(ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute attribute, object data);
		object Visit(AttributeSection attributeDeclaration, object data);
		object Visit(DelegateDeclaration delegateDeclaration, object data);
		object Visit(ImportsDeclaration importsAliasDeclaration, object data);
		object Visit(ImportsAliasDeclaration importsAliasDeclaration, object data);
		object Visit(ImportsStatement importsStatement, object data);
		object Visit(NamespaceDeclaration namespaceDeclaration, object data);
		object Visit(OptionCompareDeclaration optionCompareDeclaration, object data);
		object Visit(OptionExplicitDeclaration optionExplicitDeclaration, object data);
		object Visit(OptionStrictDeclaration optionStrictDeclaration, object data);
		object Visit(TypeDeclaration typeDeclaration, object data);
		object Visit(TypeReference typeReference, object data);
		
		// Expressions
		object Visit(PrimitiveExpression      primitiveExpression, object data);
		object Visit(BinaryOperatorExpression binaryOperatorExpression, object data);
		object Visit(ParenthesizedExpression parenthesizedExpression, object data);
		object Visit(InvocationExpression invocationExpression, object data);
		object Visit(IdentifierExpression identifierExpression, object data);
		object Visit(TypeReferenceExpression typeReferenceExpression, object data);
		object Visit(UnaryOperatorExpression unaryOperatorExpression, object data);
		object Visit(AssignmentExpression assignmentExpression, object data);
		object Visit(CastExpression castExpression, object data);
		object Visit(ThisReferenceExpression thisReferenceExpression, object data);
		object Visit(BaseReferenceExpression baseReferenceExpression, object data);
		object Visit(ObjectCreateExpression objectCreateExpression, object data);
		object Visit(ParameterDeclarationExpression parameterDeclarationExpression, object data);
		object Visit(FieldReferenceOrInvocationExpression fieldReferenceOrInvocationExpression, object data);
		object Visit(ArrayInitializerExpression arrayInitializerExpression, object data);
		object Visit(GetTypeExpression getTypeExpression, object data);
		object Visit(ClassReferenceExpression classReferenceExpression, object data);
		object Visit(LoopControlVariableExpression loopControlVariableExpression, object data);
		object Visit(NamedArgumentExpression namedArgumentExpression, object data);
		object Visit(AddressOfExpression addressOfExpression, object data);
		object Visit(TypeOfExpression typeOfExpression, object data);
		object Visit(ArrayCreateExpression arrayCreateExpression, object data);
		
	}
}
