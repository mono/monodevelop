using System;
using System.Collections;
using ICSharpCode.SharpRefactory.Parser.AST.VB;

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	public abstract class AbstractASTVisitor : IASTVisitor
	{
		protected Stack blockStack = new Stack();
		
		public BlockStatement CurrentBlock {
			get {
				if (blockStack.Count == 0) {
					return null;
				}
				return (BlockStatement)blockStack.Peek();
			}
		}
		
		public virtual object Visit(INode node, object data)
		{
			Console.WriteLine("Warning, INode visited!");
			Console.WriteLine("Type is " + node.GetType());
			Console.WriteLine("Visitor is " + this.GetType());
			return node.AcceptChildren(this, data);
		}
		
		public virtual object Visit(CompilationUnit compilationUnit, object data)
		{
			if (compilationUnit == null) {
				return data;
			}
			return compilationUnit.AcceptChildren(this, data);
		}
		
		public virtual object Visit(NamespaceDeclaration namespaceDeclaration, object data)
		{
			return namespaceDeclaration.AcceptChildren(this, data);
		}
		
		public virtual object Visit(OptionExplicitDeclaration optionExplicitDeclaration, object data)
		{
			return data;
		}
		
		public virtual object Visit(OptionStrictDeclaration optionStrictDeclaration, object data)
		{
			return data;
		}
		
		public virtual object Visit(OptionCompareDeclaration optionCompareDeclaration, object data)
		{
			return data;
		}
		
		public virtual object Visit(ImportsStatement importsStatement, object data)
		{
			object ret = data;
			foreach (INode n in importsStatement.ImportClauses) {
				ret = n.AcceptVisitor(this, data);
			}
			return ret;
		}
		
		public virtual object Visit(ImportsAliasDeclaration importsAliasDeclaration, object data)
		{
			return data;
		}
		
		public virtual object Visit(ImportsDeclaration importsDeclaration, object data)
		{
			return data;
		}
		
		public virtual object Visit(AttributeSection attributeSection, object data)
		{
			object ret = data;
			foreach (ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute a in attributeSection.Attributes) {
				ret = a.AcceptVisitor(this, data);
			}
			return ret;
		}
		
		public virtual object Visit(ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute attribute, object data)
		{
			object ret = data;
			foreach (Expression e in attribute.PositionalArguments) {
				ret = e.AcceptVisitor(this, data);
			}
			foreach (NamedArgumentExpression n in attribute.NamedArguments) {
				ret = n.AcceptVisitor(this, data);
			}
			return ret;
		}
		
		public virtual object Visit(NamedArgumentExpression namedArgumentExpression, object data)
		{
			return namedArgumentExpression.Expression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(TypeReference typeReference, object data)
		{
			return data;
		}
		
		public virtual object Visit(TypeDeclaration typeDeclaration, object data)
		{
			foreach (AttributeSection a in typeDeclaration.Attributes) {
				a.AcceptVisitor(this, data);
			}
			return typeDeclaration.AcceptChildren(this, data);
		}
		
		public virtual object Visit(DelegateDeclaration delegateDeclaration, object data)
		{
			foreach (AttributeSection a in delegateDeclaration.Attributes) {
				a.AcceptVisitor(this, data);
			}
			if (delegateDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression p in delegateDeclaration.Parameters) {
					p.AcceptVisitor(this, data);
				}
			}
			if (delegateDeclaration.ReturnType == null) {
				return data;
			}
			return delegateDeclaration.ReturnType.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(FieldDeclaration fieldDeclaration, object data)
		{
			foreach (AttributeSection a in fieldDeclaration.Attributes) {
				a.AcceptVisitor(this, data);
			}
			if (fieldDeclaration.Fields != null) {
				foreach (VariableDeclaration v in fieldDeclaration.Fields) {
					v.AcceptVisitor(this, data);
				}
			}
			if (fieldDeclaration.TypeReference == null) {
				return data;
			}
			return fieldDeclaration.TypeReference.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(VariableDeclaration variableDeclaration, object data)
		{
			if (variableDeclaration.Initializer == null) {
				return data;
			}
			return variableDeclaration.Initializer.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			object ret = data;
			foreach (AttributeSection a in parameterDeclarationExpression.Attributes) {
				a.AcceptVisitor(this, data);
			}
			ret = parameterDeclarationExpression.TypeReference.AcceptVisitor(this, data);
			if (parameterDeclarationExpression.DefaultValue != null) {
				ret = parameterDeclarationExpression.DefaultValue.AcceptVisitor(this, data);
			}
			return ret;
		}
		
		public virtual object Visit(ConstructorDeclaration constructorDeclaration, object data)
		{
			blockStack.Push(constructorDeclaration.Body);
			foreach (AttributeSection a in constructorDeclaration.Attributes) {
				a.AcceptVisitor(this, data);
			}
			if (constructorDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression p in constructorDeclaration.Parameters) {
					p.AcceptVisitor(this, data);
				}
			}
			object ret = null;
			if (constructorDeclaration.Body != null) {
				ret = constructorDeclaration.Body.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(MethodDeclaration methodDeclaration, object data)
		{
			blockStack.Push(methodDeclaration.Body);
			foreach (AttributeSection a in methodDeclaration.Attributes) {
				a.AcceptVisitor(this, data);
			}
			if (methodDeclaration.TypeReference != null) {
				methodDeclaration.TypeReference.AcceptVisitor(this, data);
			}
			
			if (methodDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression p in methodDeclaration.Parameters) {
					p.AcceptVisitor(this, data);
				}
			}
			
			if (methodDeclaration.HandlesClause != null) {
				methodDeclaration.HandlesClause.AcceptVisitor(this, data);
			}
			if (methodDeclaration.ImplementsClause != null) {
				methodDeclaration.ImplementsClause.AcceptVisitor(this, data);
			}
			object ret = null;
			if (methodDeclaration.Body != null) {
				methodDeclaration.Body.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(DeclareDeclaration declareDeclaration, object data)
		{
			if (declareDeclaration != null) {
				if (declareDeclaration.Attributes != null) {
					foreach (AttributeSection a in declareDeclaration.Attributes) {
						a.AcceptVisitor(this, data);
					}
				}
				
				if (declareDeclaration.ReturnType != null) {
					declareDeclaration.ReturnType.AcceptVisitor(this, data);
				}
				
				if (declareDeclaration.Parameters != null) {
					foreach (ParameterDeclarationExpression p in declareDeclaration.Parameters) {
						p.AcceptVisitor(this, data);
					}
				}
			}
			return data;
		}
		
		public virtual object Visit(PropertyDeclaration propertyDeclaration, object data)
		{
			foreach (AttributeSection a in propertyDeclaration.Attributes) {
				a.AcceptVisitor(this, data);
			}
			propertyDeclaration.TypeReference.AcceptVisitor(this, data);
			if (propertyDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression p in propertyDeclaration.Parameters) {
					p.AcceptVisitor(this, data);
				}
			}
			if (propertyDeclaration.ImplementsClause != null) {
				propertyDeclaration.ImplementsClause.AcceptVisitor(this, data);
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
			blockStack.Push(propertyGetRegion.Block);
			foreach (AttributeSection a in propertyGetRegion.Attributes) {
				a.AcceptVisitor(this, data);
			}
			object ret = null;
			if (propertyGetRegion.Block != null) {
				ret = propertyGetRegion.Block.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(PropertySetRegion propertySetRegion, object data)
		{
			blockStack.Push(propertySetRegion.Block);
			foreach (AttributeSection a in propertySetRegion.Attributes) {
				a.AcceptVisitor(this, data);
			}
			object ret = null;
			if (propertySetRegion.Block != null) {
				ret = propertySetRegion.Block.AcceptChildren(this, data);
			}
			blockStack.Pop();
			return ret;
		}
		
		public virtual object Visit(EventDeclaration eventDeclaration, object data)
		{
			foreach (AttributeSection a in eventDeclaration.Attributes) {
				a.AcceptVisitor(this, data);
			}
			eventDeclaration.TypeReference.AcceptVisitor(this, data);
			if (eventDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression p in eventDeclaration.Parameters) {
					p.AcceptVisitor(this, data);
				}
			}
			if (eventDeclaration.ImplementsClause != null) {
				eventDeclaration.ImplementsClause.AcceptVisitor(this, data);
			}
			return data;
		}
		
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
		
		public virtual object Visit(Statement statement, object data)
		{
			Console.WriteLine("Warning, visited Statement!");
			return data;
		}
		
		public virtual object Visit(StatementExpression statementExpression, object data)
		{
			if (statementExpression.Expression == null) {
				return data;
			}
			return statementExpression.Expression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			object ret = data;
			if (localVariableDeclaration.Variables != null) {
				foreach (VariableDeclaration v in localVariableDeclaration.Variables) {
					ret = v.AcceptVisitor(this, data);
				}
			}
			return ret;
		}
		
		public virtual object Visit(SimpleIfStatement ifStatement, object data)
		{
			object ret = ifStatement.Condition.AcceptVisitor(this, data);
			if(ifStatement.Statements != null) {
				foreach (Statement s in ifStatement.Statements) {
					ret = s.AcceptVisitor(this, data);
				}
			}
			if(ifStatement.ElseStatements != null) {
				foreach (Statement s in ifStatement.ElseStatements) {
					ret = s.AcceptVisitor(this, data);
				}
			}
			return ret;
		}
		
		public virtual object Visit(IfStatement ifStatement, object data)
		{
			object ret = ifStatement.Condition.AcceptVisitor(this, data);
			if(ifStatement.ElseIfStatements != null) {
				foreach (Statement s in ifStatement.ElseIfStatements) {
					ret = s.AcceptVisitor(this, data);
				}
			}
			if (ifStatement.EmbeddedElseStatement != null) {
				ret = ifStatement.EmbeddedElseStatement.AcceptVisitor(this, data);
			}
			return ret;
		}
		
		public virtual object Visit(LabelStatement labelStatement, object data)
		{
			if (labelStatement.EmbeddedStatement == null) {
				return null;
			}
			return labelStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(GoToStatement goToStatement, object data)
		{
			return data;
		}
		
		public virtual object Visit(SelectStatement selectStatement, object data)
		{
			selectStatement.SelectExpression.AcceptVisitor(this, data);
			if (selectStatement.SelectSections != null) {
				foreach (SelectSection s in selectStatement.SelectSections) {
					s.AcceptVisitor(this, data);
				}
			}
			return data;
		}
		
		public virtual object Visit(SelectSection selectSection, object data)
		{
			if (selectSection.CaseClauses != null) {
				foreach (CaseClause c in selectSection.CaseClauses) {
					c.AcceptVisitor(this, data);
				}
			}
			return data;
		}
		
		public virtual object Visit(CaseClause caseClause, object data)
		{
			caseClause.ComparisonExpression.AcceptVisitor(this, data);
			return caseClause.BoundaryExpression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(ExitStatement exitStatement, object data)
		{
			return data;
		}
		
		public virtual object Visit(EndStatement endStatement, object data)
		{
			return data;
		}
		
		public virtual object Visit(StopStatement stopStatement, object data)
		{
			return data;
		}
		
		public virtual object Visit(ResumeStatement resumeStatement, object data)
		{
			return data;
		}
		
		public virtual object Visit(ErrorStatement errorStatement, object data)
		{
			return errorStatement.Expression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(OnErrorStatement onErrorStatement, object data)
		{
			return onErrorStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(EraseStatement eraseStatement, object data)
		{
			return data;
		}
		
		public virtual object Visit(ReDimStatement reDimStatement, object data)
		{
			return data;
		}
		
		public virtual object Visit(AddHandlerStatement addHandlerStatement, object data)
		{
			addHandlerStatement.EventExpression.AcceptVisitor(this, data);
			addHandlerStatement.HandlerExpression.AcceptVisitor(this, data);
			return data;
		}
		
		public virtual object Visit(RemoveHandlerStatement removeHandlerStatement, object data)
		{
			removeHandlerStatement.EventExpression.AcceptVisitor(this, data);
			removeHandlerStatement.HandlerExpression.AcceptVisitor(this, data);
			return data;
		}
		
		public virtual object Visit(ReturnStatement returnStatement, object data)
		{
			return returnStatement.ReturnExpression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(RaiseEventStatement raiseEventStatement, object data)
		{
			if (raiseEventStatement.Parameters != null) {
				foreach (INode node in raiseEventStatement.Parameters) {
					node.AcceptVisitor(this, data);
				}
			}
			return data;
		}
		
		public virtual object Visit(WhileStatement whileStatement, object data)
		{
			if (whileStatement.EmbeddedStatement == null) {
				return null;
			}
			return whileStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(WithStatement withStatement, object data)
		{
			if (withStatement.WithExpression != null) {
				withStatement.WithExpression.AcceptVisitor(this, data);
			}
			
			if (withStatement.Body == null) {
				return null;
			}
			return withStatement.Body.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(DoLoopStatement doLoopStatement, object data)
		{
			if (doLoopStatement.EmbeddedStatement == null) {
				return null;
			}
			return doLoopStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		public virtual object Visit(ForStatement forStatement, object data)
		{
			if (forStatement.EmbeddedStatement == null) {
				return null;
			}
			return forStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(ForeachStatement foreachStatement, object data)
		{
			if (foreachStatement.EmbeddedStatement == null) {
				return null;
			}
			return foreachStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		public virtual object Visit(LockStatement lockStatement, object data)
		{
			if (lockStatement.EmbeddedStatement == null) {
				return null;
			}
			return lockStatement.EmbeddedStatement.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(TryCatchStatement tryCatchStatement, object data)
		{
			if (tryCatchStatement.StatementBlock == null) {
				return null;
			}
			return tryCatchStatement.StatementBlock.AcceptVisitor(this, data);
		}
		public virtual object Visit(ThrowStatement throwStatement, object data)
		{
			if (throwStatement.ThrowExpression == null) {
				return null;
			}
			return throwStatement.ThrowExpression.AcceptVisitor(this, data);
		}
		
#region Expressions
		public virtual object Visit(FieldReferenceOrInvocationExpression fieldReferenceOrInvocationExpression, object data)
		{
			if (fieldReferenceOrInvocationExpression.TargetObject == null) {
				return null;
			}
			return fieldReferenceOrInvocationExpression.TargetObject.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(PrimitiveExpression primitiveExpression, object data)
		{
			// nothing to visit
			return data;
		}
		
		public virtual object Visit(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			// visit but can't give back 2 values
			binaryOperatorExpression.Left.AcceptVisitor(this, data);
			binaryOperatorExpression.Right.AcceptVisitor(this, data);
			
			return data;
		}
		public virtual object Visit(ParenthesizedExpression parenthesizedExpression, object data)
		{
			return parenthesizedExpression.Expression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(InvocationExpression invocationExpression, object data)
		{
			if (invocationExpression.TargetObject == null) {
				return data;
			}
			return invocationExpression.TargetObject.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(IdentifierExpression identifierExpression, object data)
		{
			// nothing to visit
			return data;
		}
		
		public virtual object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			// nothing to visit
			return data;
		}
		
		public virtual object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			return unaryOperatorExpression.Expression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(AssignmentExpression assignmentExpression, object data)
		{
			// visit but can't give back 2 values
			assignmentExpression.Left.AcceptVisitor(this, data);
			assignmentExpression.Right.AcceptVisitor(this, data);
			
			return data;
		}
		
		public virtual object Visit(CastExpression castExpression, object data)
		{
			return castExpression.Expression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			// nothing to visit
			return data;
		}
		
		public virtual object Visit(BaseReferenceExpression baseReferenceExpression, object data)
		{
			// nothing to visit
			return data;
		}
		
		public virtual object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			// nothing to visit
			return data;
		}
		
		public virtual object Visit(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			if (arrayInitializerExpression.CreateExpressions != null) {
				foreach (INode node in arrayInitializerExpression.CreateExpressions) {
					node.AcceptVisitor(this, data);
				}
			}
			
			return data;
		}
		
		public virtual object Visit(GetTypeExpression getTypeExpression, object data)
		{
			// nothing to visit
			return data;
		}
		
		public virtual object Visit(ClassReferenceExpression classReferenceExpression, object data)
		{
			// nothing to visit
			return data;
		}
		
		public virtual object Visit(LoopControlVariableExpression loopControlVariableExpression, object data)
		{
			return loopControlVariableExpression.Expression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(AddressOfExpression addressOfExpression, object data)
		{
			return addressOfExpression.Procedure.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(TypeOfExpression typeOfExpression, object data)
		{
			return typeOfExpression.Expression.AcceptVisitor(this, data);
		}
		
		public virtual object Visit(ArrayCreateExpression arrayCreateExpression, object data)
		{
			if (arrayCreateExpression.ArrayInitializer == null) {
				return data;
			}
			return arrayCreateExpression.ArrayInitializer.AcceptVisitor(this, data);
		}
#endregion
	}
}
