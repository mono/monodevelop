using System;
using System.Collections;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.Parser
{
	public abstract class AbstractASTVisitor : IASTVisitor
	{
		Errors errors = new Errors ();
		protected Stack blockStack = new Stack();
		
		public BlockStatement CurrentBlock {
			get {
				if (blockStack.Count == 0) {
					return null;
				}
				return (BlockStatement)blockStack.Peek();
			}
		}

		public Errors Errors {
			get { return errors; }
			set { errors = value; }
		}
		
#region ICSharpCode.SharpRefactory.Parser.IASTVisitor interface implementation
		public virtual object Visit(INode node, object data)
		{
			//Console.WriteLine("Warning: INode visited");
			//Console.WriteLine("Visitor was: " + this.GetType());
			//Console.WriteLine("Node was : " + node.GetType());
			return node.AcceptChildren(this, data);
		}
		
		public virtual object Visit(CompilationUnit compilationUnit, object data)
		{
			if (compilationUnit == null) {
				return data;
			}
			return compilationUnit.AcceptChildren(this, data);
		}
		
#region Global Scope
		public virtual object Visit(ExternAliasDeclaration externAliasDeclaration, object data)
		{
			return data;
		}

		public virtual object Visit(UsingDeclaration usingDeclaration, object data)
		{
			return data;
		}
		
		public virtual object Visit(UsingAliasDeclaration usingAliasDeclaration, object data)
		{
			return data;
		}
		
		public virtual object Visit(NamespaceDeclaration namespaceDeclaration, object data)
		{
			return namespaceDeclaration.AcceptChildren(this, data);
		}
		
		public virtual object Visit(AttributeSection attributeSection, object data)
		{
			return data;
		}
		
		public virtual object Visit(TypeDeclaration typeDeclaration, object data)
		{
			foreach (AttributeSection section in typeDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			return typeDeclaration.AcceptChildren(this, data);
		}
		
		public virtual object Visit(DelegateDeclaration delegateDeclaration, object data)
		{
			foreach (AttributeSection section in delegateDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			foreach (ParameterDeclarationExpression p in delegateDeclaration.Parameters) {
				p.AcceptVisitor(this, data);
			}
			return data;
		}
#endregion

#region Type level
		public virtual object Visit(VariableDeclaration variableDeclaration, object data)
		{
			if (variableDeclaration.Initializer != null) {
				return variableDeclaration.Initializer.AcceptVisitor(this, data);
			}
			return data;
		}
		
		public virtual object Visit(FieldDeclaration fieldDeclaration, object data)
		{
			foreach (AttributeSection section in fieldDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			foreach (VariableDeclaration var in fieldDeclaration.Fields) {
				var.AcceptVisitor(this, fieldDeclaration);
			}
			return data;
		}
		
		public virtual object Visit(PropertyDeclaration propertyDeclaration, object data)
		{
			foreach (AttributeSection section in propertyDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			if (propertyDeclaration.HasGetRegion) {
				propertyDeclaration.GetRegion.AcceptVisitor(this, data);
			}
			if (propertyDeclaration.HasSetRegion) {
				propertyDeclaration.SetRegion.AcceptVisitor(this, data);
			}
			return data;
		}
		
		public virtual object Visit(PropertyGetRegion propertyGetRegion, object data)
		{
			foreach (AttributeSection section in propertyGetRegion.Attributes) {
				section.AcceptVisitor(this, data);
			}
			blockStack.Push(propertyGetRegion.Block);
			object ret = data;
			if (propertyGetRegion.Block != null) {
				ret = propertyGetRegion.Block.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(PropertySetRegion propertySetRegion, object data)
		{
			foreach (AttributeSection section in propertySetRegion.Attributes) {
				section.AcceptVisitor(this, data);
			}
			blockStack.Push(propertySetRegion.Block);
			object ret = data;
			if (propertySetRegion.Block != null) {
				ret = propertySetRegion.Block.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(EventDeclaration eventDeclaration, object data)
		{
			foreach (AttributeSection section in eventDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			if (eventDeclaration.HasAddRegion) {
				eventDeclaration.AddRegion.AcceptVisitor(this, data);
			}
			if (eventDeclaration.HasRemoveRegion) {
				eventDeclaration.RemoveRegion.AcceptVisitor(this, data);
			}
			return data;
		}
		
		public virtual object Visit(EventAddRegion eventAddRegion, object data)
		{
			foreach (AttributeSection section in eventAddRegion.Attributes) {
				section.AcceptVisitor(this, data);
			}
			blockStack.Push(eventAddRegion.Block);
			object ret = data;
			if (eventAddRegion.Block != null) {
				ret = eventAddRegion.Block.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(EventRemoveRegion eventRemoveRegion, object data)
		{
			foreach (AttributeSection section in eventRemoveRegion.Attributes) {
				section.AcceptVisitor(this, data);
			}
			blockStack.Push(eventRemoveRegion.Block);
			object ret = data;
			if (eventRemoveRegion.Block != null) {
				eventRemoveRegion.Block.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			foreach (AttributeSection section in parameterDeclarationExpression.Attributes) {
				section.AcceptVisitor(this, data);
			}
			return parameterDeclarationExpression.AcceptChildren(this, data);
		}
		
		public virtual object Visit(IndexerDeclaration indexerDeclaration, object data)
		{
			foreach (AttributeSection section in indexerDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			foreach (ParameterDeclarationExpression p in indexerDeclaration.Parameters) {
				p.AcceptVisitor(this, data);
			}
			if (indexerDeclaration.HasGetRegion) {
				indexerDeclaration.GetRegion.AcceptVisitor(this, data);
			}
			if (indexerDeclaration.HasSetRegion) {
				indexerDeclaration.SetRegion.AcceptVisitor(this, data);
			}
			return data;
		}
		
		public virtual object Visit(MethodDeclaration methodDeclaration, object data)
		{
			foreach (AttributeSection section in methodDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			blockStack.Push(methodDeclaration.Body);
			foreach (ParameterDeclarationExpression p in methodDeclaration.Parameters) {
				p.AcceptVisitor(this, data);
			}
			object ret = data;
			if (methodDeclaration.Body != null) {
				methodDeclaration.Body.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(ConstructorDeclaration constructorDeclaration, object data)
		{
			foreach (AttributeSection section in constructorDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			blockStack.Push(constructorDeclaration.Body);
			foreach (ParameterDeclarationExpression p in constructorDeclaration.Parameters) {
				p.AcceptVisitor(this, data);
			}
			object ret = data;
			if (constructorDeclaration.Body != null) {
				ret = constructorDeclaration.Body.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		public virtual object Visit(DestructorDeclaration destructorDeclaration, object data)
		{
			foreach (AttributeSection section in destructorDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			blockStack.Push(destructorDeclaration.Body);
			object ret = data;
			if (destructorDeclaration.Body != null) {
				destructorDeclaration.Body.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(OperatorDeclaration operatorDeclaration, object data)
		{
			foreach (AttributeSection section in operatorDeclaration.Attributes) {
				section.AcceptVisitor(this, data);
			}
			blockStack.Push(operatorDeclaration.Body);
			object ret = data;
			if (operatorDeclaration.Body != null) {
				ret = operatorDeclaration.Body.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
#endregion
		
#region Statements
		public virtual object Visit(BlockStatement blockStatement, object data)
		{
			if (blockStatement == null) {
				return null;
			}
			blockStack.Push(blockStatement);
			object ret = blockStatement.AcceptChildren(this, data);
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			foreach (VariableDeclaration decl in localVariableDeclaration.Variables) {
				decl.AcceptVisitor(this, data);
			}
			return data;
		}
		
		public virtual object Visit(EmptyStatement emptyStatement, object data)
		{
			return data;
		}
		
		public virtual object Visit(IfStatement ifStatement, object data)
		{
			object ret = data;
			if (ifStatement.Condition != null) {
				ret = ifStatement.Condition.AcceptVisitor(this, data);
			}
			if (ifStatement.EmbeddedStatement != null) {
				ret = ifStatement.EmbeddedStatement.AcceptVisitor(this, data);
			}
			return ret;
		}
		public virtual object Visit(IfElseStatement ifElseStatement, object data)
		{
			object ret = data;
			if (ifElseStatement.Condition != null) {
				ret = ifElseStatement.Condition.AcceptVisitor(this, data);
			}
			if (ifElseStatement.EmbeddedStatement != null) {
				ret = ifElseStatement.EmbeddedStatement.AcceptVisitor(this, data);
			}
			if (ifElseStatement.EmbeddedElseStatement != null) {
				ret = ifElseStatement.EmbeddedElseStatement.AcceptVisitor(this, data);
			}
			return ret;
		}
		
		public virtual object Visit(WhileStatement whileStatement, object data)
		{
			object ret = data;
			if (whileStatement.Condition != null) {
				ret = whileStatement.Condition.AcceptVisitor(this, data);
			}
			if (whileStatement.EmbeddedStatement == null) {
				return ret;
			}
			return whileStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		public virtual object Visit(DoWhileStatement doWhileStatement, object data)
		{
			object ret = data;
			if (doWhileStatement.Condition != null) {
				ret = doWhileStatement.Condition.AcceptVisitor(this, data);
			}
			if (doWhileStatement.EmbeddedStatement == null) {
				return ret;
			}
			return doWhileStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		public virtual object Visit(ForStatement forStatement, object data)
		{
			object ret = data;
			if (forStatement.Initializers != null) {
				foreach(INode n in forStatement.Initializers) {
					n.AcceptVisitor(this, data);
				}
			}
			if (forStatement.Condition != null) {
				ret = forStatement.Condition.AcceptVisitor(this, data);
			}
			if (forStatement.Iterator != null) {
				foreach(INode n in forStatement.Iterator) {
					n.AcceptVisitor(this, data);
				}
			}
			if (forStatement.EmbeddedStatement == null) {
				return ret;
			}
			return forStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		public virtual object Visit(ForeachStatement foreachStatement, object data)
		{
			if (foreachStatement.Expression != null) {
				foreachStatement.Expression.AcceptVisitor(this, data);
			}
			if (foreachStatement.EmbeddedStatement == null) {
				return data;
			}
			return foreachStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(LabelStatement labelStatement, object data)
		{
			return data;
		}
		public virtual object Visit(GotoStatement gotoStatement, object data)
		{
			return data;
		}
		public virtual object Visit(BreakStatement breakStatement, object data)
		{
			return data;
		}
		public virtual object Visit(ContinueStatement continueStatement, object data)
		{
			return data;
		}
		public virtual object Visit(ReturnStatement returnStatement, object data)
		{
			if (returnStatement.ReturnExpression != null) {
				return returnStatement.ReturnExpression.AcceptVisitor(this, data);
			}
			return data;
		}

		public virtual object Visit(YieldStatement yieldStatement, object data)
		{
			return data;
		}
		
		public virtual object Visit(SwitchStatement switchStatement, object data)
		{
			if (switchStatement == null) {
				return null;
			}
			if (switchStatement.SwitchExpression != null) {
				switchStatement.SwitchExpression.AcceptVisitor(this, data);
			}
			foreach (SwitchSection section in switchStatement.SwitchSections) {
				section.AcceptVisitor(this, data);
			}
			return data;
		}
		
		public virtual object Visit(SwitchSection switchSection, object data)
		{
			foreach (CaseLabel label in switchSection.SwitchLabels) {
				if (label.Label != null) {
					label.Label.AcceptVisitor(this, data);
				}
			}
			return switchSection.AcceptChildren(this, data);
		}
		
		public virtual object Visit(GotoCaseStatement gotoCaseStatement, object data)
		{
			if (gotoCaseStatement.CaseExpression != null) {
				gotoCaseStatement.CaseExpression.AcceptVisitor(this, data);
			}
			return data;
		}
		
		public virtual object Visit(LockStatement lockStatement, object data)
		{
			if (lockStatement.EmbeddedStatement == null) {
				return data;
			}
			return lockStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(UsingStatement usingStatement, object data)
		{
			object ret = data;
			if (usingStatement.UsingStmnt != null) {
				ret = usingStatement.UsingStmnt.AcceptVisitor(this, data);
			}
			if (usingStatement.EmbeddedStatement != null) {
				ret = usingStatement.EmbeddedStatement.AcceptVisitor(this, data);
			}
			return ret;
		}
		public virtual object Visit(TryCatchStatement tryCatchStatement, object data)
		{
			if (tryCatchStatement == null) {
				return data;
			}
			if (tryCatchStatement.StatementBlock != null) {
				tryCatchStatement.StatementBlock.AcceptVisitor(this, data);
			}
			if (tryCatchStatement.CatchClauses != null) {
				foreach (CatchClause catchClause in tryCatchStatement.CatchClauses) {
					if (catchClause != null) {
						catchClause.StatementBlock.AcceptVisitor(this, data);
					}
				}
			}
			if (tryCatchStatement.FinallyBlock != null) {
				return tryCatchStatement.FinallyBlock.AcceptVisitor(this, data);
			}
			return data;
		}
		public virtual object Visit(ThrowStatement throwStatement, object data)
		{
			if (throwStatement.ThrowExpression == null) {
				return data;
			}
			return throwStatement.ThrowExpression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(FixedStatement fixedStatement, object data)
		{
			return fixedStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		public virtual object Visit(CheckedStatement checkedStatement, object data)
		{
			if (checkedStatement.Block == null) {
				return data;
			}
			return checkedStatement.Block.AcceptVisitor(this, data);
		}
		public virtual object Visit(UncheckedStatement uncheckedStatement, object data)
		{
			if (uncheckedStatement.Block == null) {
				return null;
			}
			return uncheckedStatement.Block.AcceptVisitor(this, data);
		}
		public virtual object Visit(StatementExpression statementExpression, object data)
		{
			if (statementExpression.Expression == null) {
				return null;
			}
			return statementExpression.Expression.AcceptVisitor(this, data);
		}

		public virtual object Visit(UnsafeStatement unsafeStatement, object data)
		{
			if (unsafeStatement.Block == null) {
				return null;
			}
			return unsafeStatement.Block.AcceptVisitor(this, data);
		}
#endregion
		
#region Expressions
		public virtual object Visit(PrimitiveExpression primitiveExpression, object data)
		{
			return data;
		}
		public virtual object Visit(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			binaryOperatorExpression.Left.AcceptVisitor(this, data);
			return binaryOperatorExpression.Right.AcceptVisitor(this, data);
		}
		public virtual object Visit(ParenthesizedExpression parenthesizedExpression, object data)
		{
			return parenthesizedExpression.Expression.AcceptVisitor(this, data);
		}
		public virtual object Visit(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			if (fieldReferenceExpression.TargetObject != null) {
				return fieldReferenceExpression.TargetObject.AcceptVisitor(this, data);
			}
			return data;
		}
		public virtual object Visit(InvocationExpression invocationExpression, object data)
		{
			object result = data;
			if (invocationExpression.TargetObject != null) {
				result = invocationExpression.TargetObject.AcceptVisitor(this, data);
			}
			if (invocationExpression.Parameters != null) {
				foreach (INode n in invocationExpression.Parameters) {
					n.AcceptVisitor(this, data);
				}
			}
			return result;
		}
		public virtual object Visit(IdentifierExpression identifierExpression, object data)
		{
			return data;
		}
		public virtual object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return data;
		}
		public virtual object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			return unaryOperatorExpression.Expression.AcceptVisitor(this, data);
		}
		public virtual object Visit(AssignmentExpression assignmentExpression, object data)
		{
			if (assignmentExpression.Left == null || assignmentExpression.Right == null) return null;
			assignmentExpression.Left.AcceptVisitor(this, data);
			return assignmentExpression.Right.AcceptVisitor(this, data);
		}
		public virtual object Visit(SizeOfExpression sizeOfExpression, object data)
		{
			return data;
		}
		public virtual object Visit(TypeOfExpression typeOfExpression, object data)
		{
			return data;
		}
		public virtual object Visit(CheckedExpression checkedExpression, object data)
		{
			return checkedExpression.Expression.AcceptVisitor(this, data);
		}
		public virtual object Visit(UncheckedExpression uncheckedExpression, object data)
		{
			return uncheckedExpression.Expression.AcceptVisitor(this, data);
		}
		public virtual object Visit(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			return pointerReferenceExpression.Expression.AcceptVisitor(this, data);
		}
		public virtual object Visit(CastExpression castExpression, object data)
		{
			return castExpression.Expression.AcceptVisitor(this, data);
		}
		public virtual object Visit(StackAllocExpression stackAllocExpression, object data)
		{
			return stackAllocExpression.Expression.AcceptVisitor(this, data);
		}
		public virtual object Visit(IndexerExpression indexerExpression, object data)
		{
			object res = indexerExpression.TargetObject.AcceptVisitor(this, data);
			foreach (INode n in indexerExpression.Indices) {
				n.AcceptVisitor(this, data);
			}
			return res;
		}
		public virtual object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			return data;
		}
		public virtual object Visit(BaseReferenceExpression baseReferenceExpression, object data)
		{
			return data;
		}
		public virtual object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			foreach (Expression e in objectCreateExpression.Parameters) {
				e.AcceptVisitor(this, data);
			}
			return data;
		}
		
		public virtual object Visit(ArrayCreationParameter arrayCreationParameter, object data)
		{
			if (arrayCreationParameter.Expressions != null) {
				foreach (Expression e in arrayCreationParameter.Expressions) {
					e.AcceptVisitor(this, data);
				}
			}
			return data;
		}
		
		public virtual object Visit(ArrayCreateExpression arrayCreateExpression, object data)
		{
			if (arrayCreateExpression.Parameters != null) {
				foreach (INode node in arrayCreateExpression.Parameters) {
					node.AcceptVisitor(this, data);
				}
			}
			if (arrayCreateExpression.ArrayInitializer != null) {
				return arrayCreateExpression.ArrayInitializer.AcceptVisitor(this, data);
			}
			return data;
		}
		public virtual object Visit(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			foreach (Expression e in arrayInitializerExpression.CreateExpressions) {
				e.AcceptVisitor(this, data);
			}
			return data;
		}
		public virtual object Visit(DirectionExpression directionExpression, object data)
		{
			return directionExpression.Expression.AcceptVisitor(this, data);
		}
		public virtual object Visit(ConditionalExpression conditionalExpression, object data)
		{
			conditionalExpression.TestCondition.AcceptVisitor(this, data);
			conditionalExpression.TrueExpression.AcceptVisitor(this, data);
			return conditionalExpression.FalseExpression.AcceptVisitor(this, data);
		}
#endregion
#endregion
	}
}
