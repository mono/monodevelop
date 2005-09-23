using System;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.Parser
{
	public class DebugASTVisitor : AbstractASTVisitor
	{
#region ICSharpCode.SharpRefactory.Parser.IASTVisitor interface implementation
		public override object Visit(CompilationUnit compilationUnit, object data)
		{
			Console.WriteLine(compilationUnit.ToString());
			return compilationUnit.AcceptChildren(this, data);
		}

		public override object Visit(ExternAliasDeclaration externAliasDeclaration, object data)
		{
			Console.WriteLine (externAliasDeclaration.ToString ());
			return externAliasDeclaration.AcceptChildren (this, data);
		}
		
		public override object Visit(FieldDeclaration fieldDeclaration, object data)
		{
			Console.WriteLine(fieldDeclaration.ToString());
			return fieldDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(NamespaceDeclaration namespaceDeclaration, object data)
		{
			Console.WriteLine(namespaceDeclaration.ToString());
			return namespaceDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(UsingDeclaration usingDeclaration, object data)
		{
			Console.WriteLine(usingDeclaration.ToString());
			return usingDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(UsingAliasDeclaration usingAliasDeclaration, object data)
		{
			Console.WriteLine(usingAliasDeclaration.ToString());
			return usingAliasDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(TypeDeclaration typeDeclaration, object data)
		{
			Console.WriteLine(typeDeclaration.ToString());
			return typeDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(DelegateDeclaration delegateDeclaration, object data)
		{
			Console.WriteLine(delegateDeclaration.ToString());
			return delegateDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(VariableDeclaration variableDeclaration, object data)
		{
			Console.WriteLine(variableDeclaration.ToString());
			return variableDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(MethodDeclaration methodDeclaration, object data)
		{
			Console.WriteLine(methodDeclaration.ToString());
			if (methodDeclaration.Body != null) {
				return methodDeclaration.Body.AcceptChildren(this, data);
			}
			return methodDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(AttributeSection attributeSection, object data)
		{
			Console.WriteLine(attributeSection.ToString());
			return attributeSection.AcceptChildren(this, data);
		}
		
		public override object Visit(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			Console.WriteLine(fieldReferenceExpression.ToString());
			return fieldReferenceExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(ConstructorDeclaration constructorDeclaration, object data)
		{
			Console.WriteLine(constructorDeclaration.ToString());
			return constructorDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(DestructorDeclaration destructorDeclaration, object data)
		{
			Console.WriteLine(destructorDeclaration.ToString());
			return destructorDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(OperatorDeclaration operatorDeclaration, object data)
		{
			Console.WriteLine(operatorDeclaration.ToString());
			return operatorDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(IndexerDeclaration indexerDeclaration, object data)
		{
			Console.WriteLine(indexerDeclaration.ToString());
			return indexerDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			Console.WriteLine(parameterDeclarationExpression.ToString());
			return parameterDeclarationExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(FixedStatement fixedStatement, object data)
		{
			Console.WriteLine(fixedStatement.ToString());
			return fixedStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(PropertyDeclaration propertyDeclaration, object data)
		{
			Console.WriteLine(propertyDeclaration.ToString());
			return propertyDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(PropertyGetRegion propertyGetRegion, object data)
		{
			Console.WriteLine(propertyGetRegion.ToString());
			return propertyGetRegion.AcceptChildren(this, data);
		}
		
		public override object Visit(PropertySetRegion PropertySetRegion, object data)
		{
			Console.WriteLine(PropertySetRegion.ToString());
			return PropertySetRegion.AcceptChildren(this, data);
		}
		
		public override object Visit(EventDeclaration eventDeclaration, object data)
		{
			Console.WriteLine(eventDeclaration.ToString());
			return eventDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(EventAddRegion eventAddRegion, object data)
		{
			Console.WriteLine(eventAddRegion.ToString());
			return eventAddRegion.AcceptChildren(this, data);
		}
		
		public override object Visit(EventRemoveRegion eventRemoveRegion, object data)
		{
			Console.WriteLine(eventRemoveRegion.ToString());
			return eventRemoveRegion.AcceptChildren(this, data);
		}
		
		public override object Visit(BlockStatement blockStatement, object data)
		{
			Console.WriteLine(blockStatement.ToString());
			return blockStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(StatementExpression statementExpression, object data)
		{
			Console.WriteLine(statementExpression.ToString());
			return statementExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			Console.WriteLine(localVariableDeclaration.ToString());
			return localVariableDeclaration.AcceptChildren(this, data);
		}
		
		public override object Visit(EmptyStatement emptyStatement, object data)
		{
			Console.WriteLine(emptyStatement.ToString());
			return emptyStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(YieldStatement yieldStatement, object data)
		{
			Console.WriteLine (yieldStatement.ToString ());
			return yieldStatement.AcceptChildren (this, data);
		}

		public override object Visit(ReturnStatement returnStatement, object data)
		{
			Console.WriteLine(returnStatement.ToString());
			return returnStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(IfStatement ifStatement, object data)
		{
			Console.WriteLine(ifStatement.ToString());
			return ifStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(IfElseStatement ifElseStatement, object data)
		{
			Console.WriteLine(ifElseStatement.ToString());
			return ifElseStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(WhileStatement whileStatement, object data)
		{
			Console.WriteLine(whileStatement.ToString());
			return whileStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(DoWhileStatement doWhileStatement, object data)
		{
			Console.WriteLine(doWhileStatement.ToString());
			return doWhileStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(ForStatement forStatement, object data)
		{
			Console.WriteLine(forStatement.ToString());
			return forStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(LabelStatement labelStatement, object data)
		{
			Console.WriteLine(labelStatement.ToString());
			return labelStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(GotoStatement gotoStatement, object data)
		{
			Console.WriteLine(gotoStatement.ToString());
			return gotoStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(SwitchStatement switchStatement, object data)
		{
			Console.WriteLine(switchStatement.ToString());
			return switchStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(BreakStatement breakStatement, object data)
		{
			Console.WriteLine(breakStatement.ToString());
			return breakStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(ContinueStatement continueStatement, object data)
		{
			Console.WriteLine(continueStatement.ToString());
			return continueStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(GotoCaseStatement gotoCaseStatement, object data)
		{
			Console.WriteLine(gotoCaseStatement.ToString());
			return gotoCaseStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(ForeachStatement foreachStatement, object data)
		{
			Console.WriteLine(foreachStatement.ToString());
			return foreachStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(LockStatement lockStatement, object data)
		{
			Console.WriteLine(lockStatement.ToString());
			return lockStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(UsingStatement usingStatement, object data)
		{
			Console.WriteLine(usingStatement.ToString());
			return usingStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(TryCatchStatement tryCatchStatement, object data)
		{
			Console.WriteLine(tryCatchStatement.ToString());
			return tryCatchStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(ThrowStatement throwStatement, object data)
		{
			Console.WriteLine(throwStatement.ToString());
			return throwStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(CheckedStatement checkedStatement, object data)
		{
			Console.WriteLine(checkedStatement.ToString());
			return checkedStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(UncheckedStatement uncheckedStatement, object data)
		{
			Console.WriteLine(uncheckedStatement.ToString());
			return uncheckedStatement.AcceptChildren(this, data);
		}
		
		public override object Visit(PrimitiveExpression expression, object data)
		{
			Console.WriteLine(expression.ToString());
			return expression.AcceptChildren(this, data);
		}
		
		public override object Visit(BinaryOperatorExpression expression, object data)
		{
			Console.WriteLine(expression.ToString());
			return expression.AcceptChildren(this, data);
		}
		
		public override object Visit(ParenthesizedExpression expression, object data)
		{
			Console.WriteLine(expression.ToString());
			return expression.AcceptChildren(this, data);
		}
		
		public override object Visit(InvocationExpression invocationExpression, object data)
		{
			Console.WriteLine(invocationExpression.ToString());
			return invocationExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(IdentifierExpression expression, object data)
		{
			Console.WriteLine(expression.ToString());
			return expression.AcceptChildren(this, data);
		}
		
		public override object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			Console.WriteLine(typeReferenceExpression.ToString());
			return typeReferenceExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			Console.WriteLine(unaryOperatorExpression.ToString());
			return unaryOperatorExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(AssignmentExpression assignmentExpression, object data)
		{
			Console.WriteLine(assignmentExpression.ToString());
			return assignmentExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(SizeOfExpression sizeOfExpression, object data)
		{
			Console.WriteLine(sizeOfExpression.ToString());
			return sizeOfExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(TypeOfExpression typeOfExpression, object data)
		{
			Console.WriteLine(typeOfExpression.ToString());
			return typeOfExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(CheckedExpression checkedExpression, object data)
		{
			Console.WriteLine(checkedExpression.ToString());
			return checkedExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(UncheckedExpression uncheckedExpression, object data)
		{
			Console.WriteLine(uncheckedExpression.ToString());
			return uncheckedExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			Console.WriteLine(pointerReferenceExpression.ToString());
			return pointerReferenceExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(CastExpression castExpression, object data)
		{
			Console.WriteLine(castExpression.ToString());
			return castExpression.AcceptChildren(this, data);
		}
		
		
		public override object Visit(StackAllocExpression stackAllocExpression, object data)
		{
			Console.WriteLine(stackAllocExpression.ToString());
			return stackAllocExpression.AcceptChildren(this, data);
		}
		
		
		public override object Visit(IndexerExpression indexerExpression, object data)
		{
			Console.WriteLine(indexerExpression.ToString());
			return indexerExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			Console.WriteLine(thisReferenceExpression.ToString());
			return thisReferenceExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(BaseReferenceExpression baseReferenceExpression, object data)
		{
			Console.WriteLine(baseReferenceExpression.ToString());
			return baseReferenceExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			Console.WriteLine(objectCreateExpression.ToString());
			return objectCreateExpression.AcceptChildren(this, data);
		}
		
		public override object Visit(ArrayCreateExpression arrayCreateExpression, object data)
		{
			Console.WriteLine(arrayCreateExpression.ToString());
			return arrayCreateExpression.AcceptChildren(this, data);
		}
		public override object Visit(DirectionExpression directionExpression, object data)
		{
			Console.WriteLine(directionExpression.ToString());
			return directionExpression.AcceptChildren(this, data);
		}
		public override object Visit(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			Console.WriteLine(arrayInitializerExpression.ToString());
			return arrayInitializerExpression.AcceptChildren(this, data);
		}
		public override object Visit(ConditionalExpression conditionalExpression, object data)
		{
			Console.WriteLine(conditionalExpression.ToString());
			return conditionalExpression.AcceptChildren(this, data);
		}

#endregion
	}
}
