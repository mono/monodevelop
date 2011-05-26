// 
// ObservableAstVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;

namespace ICSharpCode.NRefactory.CSharp
{
	public delegate void ObserveNodeHandler<T> (T node);
	
	public class ObservableAstVisitor : IAstVisitor<object, object>
	{
		
		public event ObserveNodeHandler<CompilationUnit> Visited;

		object VisitChildren (AstNode node, object data)
		{
			AstNode next;
			for (var child = node.FirstChild; child != null; child = next) {
				// Store next to allow the loop to continue
				// if the visitor removes/replaces child.
				next = child.NextSibling;
				child.AcceptVisitor (this, data);
			}
			return null;
		}
		
		public event ObserveNodeHandler<CompilationUnit> CompilationUnitVisited;

		object IAstVisitor<object, object>.VisitCompilationUnit (CompilationUnit unit, object data)
		{
			var handler = CompilationUnitVisited;
			if (handler != null)
				handler (unit);
			return VisitChildren (unit, data);
		}
		
		public event ObserveNodeHandler<Comment> CommentVisited;

		object IAstVisitor<object, object>.VisitComment (Comment comment, object data)
		{
			var handler = CommentVisited;
			if (handler != null)
				handler (comment);
			return VisitChildren (comment, data);
		}
		
		public event ObserveNodeHandler<Identifier> IdentifierVisited;

		object IAstVisitor<object, object>.VisitIdentifier (Identifier identifier, object data)
		{
			var handler = IdentifierVisited;
			if (handler != null)
				handler (identifier);
			return VisitChildren (identifier, data);
		}
		
		public event ObserveNodeHandler<CSharpTokenNode> CSharpTokenNodeVisited;

		object IAstVisitor<object, object>.VisitCSharpTokenNode (CSharpTokenNode token, object data)
		{
			var handler = CSharpTokenNodeVisited;
			if (handler != null)
				handler (token);
			return VisitChildren (token, data);
		}
		
		public event ObserveNodeHandler<PrimitiveType> PrimitiveTypeVisited;

		object IAstVisitor<object, object>.VisitPrimitiveType (PrimitiveType primitiveType, object data)
		{
			var handler = PrimitiveTypeVisited;
			if (handler != null)
				handler (primitiveType);
			return VisitChildren (primitiveType, data);
		}
		
		public event ObserveNodeHandler<ComposedType> ComposedTypeVisited;

		object IAstVisitor<object, object>.VisitComposedType (ComposedType composedType, object data)
		{
			var handler = ComposedTypeVisited;
			if (handler != null)
				handler (composedType);
			return VisitChildren (composedType, data);
		}
		
		public event ObserveNodeHandler<SimpleType> SimpleTypeVisited;

		object IAstVisitor<object, object>.VisitSimpleType (SimpleType simpleType, object data)
		{
			var handler = SimpleTypeVisited;
			if (handler != null)
				handler (simpleType);
			return VisitChildren (simpleType, data);
		}
		
		public event ObserveNodeHandler<MemberType> MemberTypeVisited;

		object IAstVisitor<object, object>.VisitMemberType (MemberType memberType, object data)
		{
			var handler = MemberTypeVisited;
			if (handler != null)
				handler (memberType);
			return VisitChildren (memberType, data);
		}
		
		public event ObserveNodeHandler<Attribute> AttributeVisited;

		object IAstVisitor<object, object>.VisitAttribute (Attribute attribute, object data)
		{
			var handler = AttributeVisited;
			if (handler != null)
				handler (attribute);
			return VisitChildren (attribute, data);
		}
		
		public event ObserveNodeHandler<AttributeSection> AttributeSectionVisited;

		object IAstVisitor<object, object>.VisitAttributeSection (AttributeSection attributeSection, object data)
		{
			var handler = AttributeSectionVisited;
			if (handler != null)
				handler (attributeSection);
			return VisitChildren (attributeSection, data);
		}
		
		public event ObserveNodeHandler<DelegateDeclaration> DelegateDeclarationVisited;

		object IAstVisitor<object, object>.VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, object data)
		{
			var handler = DelegateDeclarationVisited;
			if (handler != null)
				handler (delegateDeclaration);
			return VisitChildren (delegateDeclaration, data);
		}
		
		public event ObserveNodeHandler<NamespaceDeclaration> NamespaceDeclarationVisited;

		object IAstVisitor<object, object>.VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, object data)
		{
			var handler = NamespaceDeclarationVisited;
			if (handler != null)
				handler (namespaceDeclaration);
			return VisitChildren (namespaceDeclaration, data);
		}
		
		public event ObserveNodeHandler<TypeDeclaration> TypeDeclarationVisited;

		object IAstVisitor<object, object>.VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			var handler = TypeDeclarationVisited;
			if (handler != null)
				handler (typeDeclaration);
			return VisitChildren (typeDeclaration, data);
		}
		
		public event ObserveNodeHandler<TypeParameterDeclaration> TypeParameterDeclarationVisited;

		object IAstVisitor<object, object>.VisitTypeParameterDeclaration (TypeParameterDeclaration typeParameterDeclaration, object data)
		{
			var handler = TypeParameterDeclarationVisited;
			if (handler != null)
				handler (typeParameterDeclaration);
			return VisitChildren (typeParameterDeclaration, data);
		}
		
		public event ObserveNodeHandler<EnumMemberDeclaration> EnumMemberDeclarationVisited;

		object IAstVisitor<object, object>.VisitEnumMemberDeclaration (EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			var handler = EnumMemberDeclarationVisited;
			if (handler != null)
				handler (enumMemberDeclaration);
			return VisitChildren (enumMemberDeclaration, data);
		}
		
		public event ObserveNodeHandler<UsingDeclaration> UsingDeclarationVisited;

		object IAstVisitor<object, object>.VisitUsingDeclaration (UsingDeclaration usingDeclaration, object data)
		{
			var handler = UsingDeclarationVisited;
			if (handler != null)
				handler (usingDeclaration);
			return VisitChildren (usingDeclaration, data);
		}
		
		public event ObserveNodeHandler<UsingAliasDeclaration> UsingAliasDeclarationVisited;

		object IAstVisitor<object, object>.VisitUsingAliasDeclaration (UsingAliasDeclaration usingDeclaration, object data)
		{
			var handler = UsingAliasDeclarationVisited;
			if (handler != null)
				handler (usingDeclaration);
			return VisitChildren (usingDeclaration, data);
		}
		
		public event ObserveNodeHandler<ExternAliasDeclaration> ExternAliasDeclarationVisited;

		object IAstVisitor<object, object>.VisitExternAliasDeclaration (ExternAliasDeclaration externAliasDeclaration, object data)
		{
			var handler = ExternAliasDeclarationVisited;
			if (handler != null)
				handler (externAliasDeclaration);
			return VisitChildren (externAliasDeclaration, data);
		}
		
		public event ObserveNodeHandler<ConstructorDeclaration> ConstructorDeclarationVisited;

		object IAstVisitor<object, object>.VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
		{
			var handler = ConstructorDeclarationVisited;
			if (handler != null)
				handler (constructorDeclaration);
			return VisitChildren (constructorDeclaration, data);
		}
		
		public event ObserveNodeHandler<ConstructorInitializer> ConstructorInitializerVisited;

		object IAstVisitor<object, object>.VisitConstructorInitializer (ConstructorInitializer constructorInitializer, object data)
		{
			var handler = ConstructorInitializerVisited;
			if (handler != null)
				handler (constructorInitializer);
			return VisitChildren (constructorInitializer, data);
		}
		
		public event ObserveNodeHandler<DestructorDeclaration> DestructorDeclarationVisited;

		object IAstVisitor<object, object>.VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
		{
			var handler = DestructorDeclarationVisited;
			if (handler != null)
				handler (destructorDeclaration);
			return VisitChildren (destructorDeclaration, data);
		}
		
		public event ObserveNodeHandler<EventDeclaration> EventDeclarationVisited;

		object IAstVisitor<object, object>.VisitEventDeclaration (EventDeclaration eventDeclaration, object data)
		{
			var handler = EventDeclarationVisited;
			if (handler != null)
				handler (eventDeclaration);
			return VisitChildren (eventDeclaration, data);
		}
		
		public event ObserveNodeHandler<CustomEventDeclaration> CustomEventDeclarationVisited;

		object IAstVisitor<object, object>.VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration, object data)
		{
			var handler = CustomEventDeclarationVisited;
			if (handler != null)
				handler (eventDeclaration);
			return VisitChildren (eventDeclaration, data);
		}
		
		public event ObserveNodeHandler<FieldDeclaration> FieldDeclarationVisited;

		object IAstVisitor<object, object>.VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
		{
			var handler = FieldDeclarationVisited;
			if (handler != null)
				handler (fieldDeclaration);
			return VisitChildren (fieldDeclaration, data);
		}
		
		public event ObserveNodeHandler<FixedFieldDeclaration> FixedFieldDeclarationVisited;

		object IAstVisitor<object, object>.VisitFixedFieldDeclaration (FixedFieldDeclaration fixedFieldDeclaration, object data)
		{
			var handler = FixedFieldDeclarationVisited;
			if (handler != null)
				handler (fixedFieldDeclaration);
			return VisitChildren (fixedFieldDeclaration, data);
		}
		
		public event ObserveNodeHandler<FixedVariableInitializer> FixedVariableInitializerVisited;

		object IAstVisitor<object, object>.VisitFixedVariableInitializer (FixedVariableInitializer fixedVariableInitializer, object data)
		{
			var handler = FixedVariableInitializerVisited;
			if (handler != null)
				handler (fixedVariableInitializer);
			return VisitChildren (fixedVariableInitializer, data);
		}
		
		public event ObserveNodeHandler<IndexerDeclaration> IndexerDeclarationVisited;

		object IAstVisitor<object, object>.VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, object data)
		{
			var handler = IndexerDeclarationVisited;
			if (handler != null)
				handler (indexerDeclaration);
			return VisitChildren (indexerDeclaration, data);
		}
		
		public event ObserveNodeHandler<MethodDeclaration> MethodDeclarationVisited;

		object IAstVisitor<object, object>.VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			var handler = MethodDeclarationVisited;
			if (handler != null)
				handler (methodDeclaration);
			return VisitChildren (methodDeclaration, data);
		}
		
		public event ObserveNodeHandler<OperatorDeclaration> OperatorDeclarationVisited;

		object IAstVisitor<object, object>.VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, object data)
		{
			var handler = OperatorDeclarationVisited;
			if (handler != null)
				handler (operatorDeclaration);
			return VisitChildren (operatorDeclaration, data);
		}
		
		public event ObserveNodeHandler<PropertyDeclaration> PropertyDeclarationVisited;

		object IAstVisitor<object, object>.VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			var handler = PropertyDeclarationVisited;
			if (handler != null)
				handler (propertyDeclaration);
			return VisitChildren (propertyDeclaration, data);
		}
		
		public event ObserveNodeHandler<Accessor> AccessorVisited;

		object IAstVisitor<object, object>.VisitAccessor (Accessor accessor, object data)
		{
			var handler = AccessorVisited;
			if (handler != null)
				handler (accessor);
			return VisitChildren (accessor, data);
		}
		
		public event ObserveNodeHandler<VariableInitializer> VariableInitializerVisited;

		object IAstVisitor<object, object>.VisitVariableInitializer (VariableInitializer variableInitializer, object data)
		{
			var handler = VariableInitializerVisited;
			if (handler != null)
				handler (variableInitializer);
			return VisitChildren (variableInitializer, data);
		}
		
		public event ObserveNodeHandler<ParameterDeclaration> ParameterDeclarationVisited;

		object IAstVisitor<object, object>.VisitParameterDeclaration (ParameterDeclaration parameterDeclaration, object data)
		{
			var handler = ParameterDeclarationVisited;
			if (handler != null)
				handler (parameterDeclaration);
			return VisitChildren (parameterDeclaration, data);
		}
		
		public event ObserveNodeHandler<Constraint> ConstraintVisited;

		object IAstVisitor<object, object>.VisitConstraint (Constraint constraint, object data)
		{
			var handler = ConstraintVisited;
			if (handler != null)
				handler (constraint);
			return VisitChildren (constraint, data);
		}
		
		public event ObserveNodeHandler<BlockStatement> BlockStatementVisited;

		object IAstVisitor<object, object>.VisitBlockStatement (BlockStatement blockStatement, object data)
		{
			var handler = BlockStatementVisited;
			if (handler != null)
				handler (blockStatement);
			return VisitChildren (blockStatement, data);
		}
		
		public event ObserveNodeHandler<ExpressionStatement> ExpressionStatementVisited;

		object IAstVisitor<object, object>.VisitExpressionStatement (ExpressionStatement expressionStatement, object data)
		{
			var handler = ExpressionStatementVisited;
			if (handler != null)
				handler (expressionStatement);
			return VisitChildren (expressionStatement, data);
		}
		
		public event ObserveNodeHandler<BreakStatement> BreakStatementVisited;

		object IAstVisitor<object, object>.VisitBreakStatement (BreakStatement breakStatement, object data)
		{
			var handler = BreakStatementVisited;
			if (handler != null)
				handler (breakStatement);
			return VisitChildren (breakStatement, data);
		}
		
		public event ObserveNodeHandler<CheckedStatement> CheckedStatementVisited;

		object IAstVisitor<object, object>.VisitCheckedStatement (CheckedStatement checkedStatement, object data)
		{
			var handler = CheckedStatementVisited;
			if (handler != null)
				handler (checkedStatement);
			return VisitChildren (checkedStatement, data);
		}
		
		public event ObserveNodeHandler<ContinueStatement> ContinueStatementVisited;

		object IAstVisitor<object, object>.VisitContinueStatement (ContinueStatement continueStatement, object data)
		{
			var handler = ContinueStatementVisited;
			if (handler != null)
				handler (continueStatement);
			return VisitChildren (continueStatement, data);
		}
		
		public event ObserveNodeHandler<DoWhileStatement> DoWhileStatementVisited;

		object IAstVisitor<object, object>.VisitDoWhileStatement (DoWhileStatement doWhileStatement, object data)
		{
			var handler = DoWhileStatementVisited;
			if (handler != null)
				handler (doWhileStatement);
			return VisitChildren (doWhileStatement, data);
		}
		
		public event ObserveNodeHandler<EmptyStatement> EmptyStatementVisited;

		object IAstVisitor<object, object>.VisitEmptyStatement (EmptyStatement emptyStatement, object data)
		{
			var handler = EmptyStatementVisited;
			if (handler != null)
				handler (emptyStatement);
			return VisitChildren (emptyStatement, data);
		}
		
		public event ObserveNodeHandler<FixedStatement> FixedStatementVisited;

		object IAstVisitor<object, object>.VisitFixedStatement (FixedStatement fixedStatement, object data)
		{
			var handler = FixedStatementVisited;
			if (handler != null)
				handler (fixedStatement);
			return VisitChildren (fixedStatement, data);
		}
		
		public event ObserveNodeHandler<ForeachStatement> ForeachStatementVisited;

		object IAstVisitor<object, object>.VisitForeachStatement (ForeachStatement foreachStatement, object data)
		{
			var handler = ForeachStatementVisited;
			if (handler != null)
				handler (foreachStatement);
			return VisitChildren (foreachStatement, data);
		}
		
		public event ObserveNodeHandler<ForStatement> ForStatementVisited;

		object IAstVisitor<object, object>.VisitForStatement (ForStatement forStatement, object data)
		{
			var handler = ForStatementVisited;
			if (handler != null)
				handler (forStatement);
			return VisitChildren (forStatement, data);
		}
		
		public event ObserveNodeHandler<GotoCaseStatement> GotoCaseStatementVisited;

		object IAstVisitor<object, object>.VisitGotoCaseStatement (GotoCaseStatement gotoCaseStatement, object data)
		{
			var handler = GotoCaseStatementVisited;
			if (handler != null)
				handler (gotoCaseStatement);
			return VisitChildren (gotoCaseStatement, data);
		}
		
		public event ObserveNodeHandler<GotoDefaultStatement> GotoDefaultStatementVisited;

		object IAstVisitor<object, object>.VisitGotoDefaultStatement (GotoDefaultStatement gotoDefaultStatement, object data)
		{
			var handler = GotoDefaultStatementVisited;
			if (handler != null)
				handler (gotoDefaultStatement);
			return VisitChildren (gotoDefaultStatement, data);
		}
		
		public event ObserveNodeHandler<GotoStatement> GotoStatementVisited;

		object IAstVisitor<object, object>.VisitGotoStatement (GotoStatement gotoStatement, object data)
		{
			var handler = GotoStatementVisited;
			if (handler != null)
				handler (gotoStatement);
			return VisitChildren (gotoStatement, data);
		}
		
		public event ObserveNodeHandler<IfElseStatement> IfElseStatementVisited;

		object IAstVisitor<object, object>.VisitIfElseStatement (IfElseStatement ifElseStatement, object data)
		{
			var handler = IfElseStatementVisited;
			if (handler != null)
				handler (ifElseStatement);
			return VisitChildren (ifElseStatement, data);
		}
		
		public event ObserveNodeHandler<LabelStatement> LabelStatementVisited;

		object IAstVisitor<object, object>.VisitLabelStatement (LabelStatement labelStatement, object data)
		{
			var handler = LabelStatementVisited;
			if (handler != null)
				handler (labelStatement);
			return VisitChildren (labelStatement, data);
		}
		
		public event ObserveNodeHandler<LockStatement> LockStatementVisited;

		object IAstVisitor<object, object>.VisitLockStatement (LockStatement lockStatement, object data)
		{
			var handler = LockStatementVisited;
			if (handler != null)
				handler (lockStatement);
			return VisitChildren (lockStatement, data);
		}
		
		public event ObserveNodeHandler<ReturnStatement> ReturnStatementVisited;

		object IAstVisitor<object, object>.VisitReturnStatement (ReturnStatement returnStatement, object data)
		{
			var handler = ReturnStatementVisited;
			if (handler != null)
				handler (returnStatement);
			return VisitChildren (returnStatement, data);
		}
		
		public event ObserveNodeHandler<SwitchStatement> SwitchStatementVisited;

		object IAstVisitor<object, object>.VisitSwitchStatement (SwitchStatement switchStatement, object data)
		{
			var handler = SwitchStatementVisited;
			if (handler != null)
				handler (switchStatement);
			return VisitChildren (switchStatement, data);
		}
		
		public event ObserveNodeHandler<SwitchSection> SwitchSectionVisited;

		object IAstVisitor<object, object>.VisitSwitchSection (SwitchSection switchSection, object data)
		{
			var handler = SwitchSectionVisited;
			if (handler != null)
				handler (switchSection);
			return VisitChildren (switchSection, data);
		}
		
		public event ObserveNodeHandler<CaseLabel> CaseLabelVisited;

		object IAstVisitor<object, object>.VisitCaseLabel (CaseLabel caseLabel, object data)
		{
			var handler = CaseLabelVisited;
			if (handler != null)
				handler (caseLabel);
			return VisitChildren (caseLabel, data);
		}
		
		public event ObserveNodeHandler<ThrowStatement> ThrowStatementVisited;

		object IAstVisitor<object, object>.VisitThrowStatement (ThrowStatement throwStatement, object data)
		{
			var handler = ThrowStatementVisited;
			if (handler != null)
				handler (throwStatement);
			return VisitChildren (throwStatement, data);
		}
		
		public event ObserveNodeHandler<TryCatchStatement> TryCatchStatementVisited;

		object IAstVisitor<object, object>.VisitTryCatchStatement (TryCatchStatement tryCatchStatement, object data)
		{
			var handler = TryCatchStatementVisited;
			if (handler != null)
				handler (tryCatchStatement);
			return VisitChildren (tryCatchStatement, data);
		}
		
		public event ObserveNodeHandler<CatchClause> CatchClauseVisited;

		object IAstVisitor<object, object>.VisitCatchClause (CatchClause catchClause, object data)
		{
			var handler = CatchClauseVisited;
			if (handler != null)
				handler (catchClause);
			return VisitChildren (catchClause, data);
		}
		
		public event ObserveNodeHandler<UncheckedStatement> UncheckedStatementVisited;

		object IAstVisitor<object, object>.VisitUncheckedStatement (UncheckedStatement uncheckedStatement, object data)
		{
			var handler = UncheckedStatementVisited;
			if (handler != null)
				handler (uncheckedStatement);
			return VisitChildren (uncheckedStatement, data);
		}
		
		public event ObserveNodeHandler<UnsafeStatement> UnsafeStatementVisited;

		object IAstVisitor<object, object>.VisitUnsafeStatement (UnsafeStatement unsafeStatement, object data)
		{
			var handler = UnsafeStatementVisited;
			if (handler != null)
				handler (unsafeStatement);
			return VisitChildren (unsafeStatement, data);
		}
		
		public event ObserveNodeHandler<UsingStatement> UsingStatementVisited;

		object IAstVisitor<object, object>.VisitUsingStatement (UsingStatement usingStatement, object data)
		{
			var handler = UsingStatementVisited;
			if (handler != null)
				handler (usingStatement);
			return VisitChildren (usingStatement, data);
		}
		
		public event ObserveNodeHandler<VariableDeclarationStatement> VariableDeclarationStatementVisited;

		object IAstVisitor<object, object>.VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			var handler = VariableDeclarationStatementVisited;
			if (handler != null)
				handler (variableDeclarationStatement);
			return VisitChildren (variableDeclarationStatement, data);
		}
		
		public event ObserveNodeHandler<WhileStatement> WhileStatementVisited;

		object IAstVisitor<object, object>.VisitWhileStatement (WhileStatement whileStatement, object data)
		{
			var handler = WhileStatementVisited;
			if (handler != null)
				handler (whileStatement);
			return VisitChildren (whileStatement, data);
		}
		
		public event ObserveNodeHandler<YieldBreakStatement> YieldBreakStatementVisited;

		object IAstVisitor<object, object>.VisitYieldBreakStatement (YieldBreakStatement yieldBreakStatement, object data)
		{
			var handler = YieldBreakStatementVisited;
			if (handler != null)
				handler (yieldBreakStatement);
			return VisitChildren (yieldBreakStatement, data);
		}
		
		public event ObserveNodeHandler<YieldStatement> YieldStatementVisited;

		object IAstVisitor<object, object>.VisitYieldStatement (YieldStatement yieldStatement, object data)
		{
			var handler = YieldStatementVisited;
			if (handler != null)
				handler (yieldStatement);
			return VisitChildren (yieldStatement, data);
		}
		
		public event ObserveNodeHandler<AnonymousMethodExpression> AnonymousMethodExpressionVisited;

		object IAstVisitor<object, object>.VisitAnonymousMethodExpression (AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			var handler = AnonymousMethodExpressionVisited;
			if (handler != null)
				handler (anonymousMethodExpression);
			return VisitChildren (anonymousMethodExpression, data);
		}
		
		public event ObserveNodeHandler<LambdaExpression> LambdaExpressionVisited;

		object IAstVisitor<object, object>.VisitLambdaExpression (LambdaExpression lambdaExpression, object data)
		{
			var handler = LambdaExpressionVisited;
			if (handler != null)
				handler (lambdaExpression);
			return VisitChildren (lambdaExpression, data);
		}
		
		public event ObserveNodeHandler<AssignmentExpression> AssignmentExpressionVisited;

		object IAstVisitor<object, object>.VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data)
		{
			var handler = AssignmentExpressionVisited;
			if (handler != null)
				handler (assignmentExpression);
			return VisitChildren (assignmentExpression, data);
		}
		
		public event ObserveNodeHandler<BaseReferenceExpression> BaseReferenceExpressionVisited;

		object IAstVisitor<object, object>.VisitBaseReferenceExpression (BaseReferenceExpression baseReferenceExpression, object data)
		{
			var handler = BaseReferenceExpressionVisited;
			if (handler != null)
				handler (baseReferenceExpression);
			return VisitChildren (baseReferenceExpression, data);
		}
		
		public event ObserveNodeHandler<BinaryOperatorExpression> BinaryOperatorExpressionVisited;

		object IAstVisitor<object, object>.VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			var handler = BinaryOperatorExpressionVisited;
			if (handler != null)
				handler (binaryOperatorExpression);
			return VisitChildren (binaryOperatorExpression, data);
		}
		
		public event ObserveNodeHandler<CastExpression> CastExpressionVisited;

		object IAstVisitor<object, object>.VisitCastExpression (CastExpression castExpression, object data)
		{
			var handler = CastExpressionVisited;
			if (handler != null)
				handler (castExpression);
			return VisitChildren (castExpression, data);
		}
		
		public event ObserveNodeHandler<CheckedExpression> CheckedExpressionVisited;

		object IAstVisitor<object, object>.VisitCheckedExpression (CheckedExpression checkedExpression, object data)
		{
			var handler = CheckedExpressionVisited;
			if (handler != null)
				handler (checkedExpression);
			return VisitChildren (checkedExpression, data);
		}
		
		public event ObserveNodeHandler<ConditionalExpression> ConditionalExpressionVisited;

		object IAstVisitor<object, object>.VisitConditionalExpression (ConditionalExpression conditionalExpression, object data)
		{
			var handler = ConditionalExpressionVisited;
			if (handler != null)
				handler (conditionalExpression);
			return VisitChildren (conditionalExpression, data);
		}
		
		public event ObserveNodeHandler<IdentifierExpression> IdentifierExpressionVisited;

		object IAstVisitor<object, object>.VisitIdentifierExpression (IdentifierExpression identifierExpression, object data)
		{
			var handler = IdentifierExpressionVisited;
			if (handler != null)
				handler (identifierExpression);
			return VisitChildren (identifierExpression, data);
		}
		
		public event ObserveNodeHandler<IndexerExpression> IndexerExpressionVisited;

		object IAstVisitor<object, object>.VisitIndexerExpression (IndexerExpression indexerExpression, object data)
		{
			var handler = IndexerExpressionVisited;
			if (handler != null)
				handler (indexerExpression);
			return VisitChildren (indexerExpression, data);
		}
		
		public event ObserveNodeHandler<InvocationExpression> InvocationExpressionVisited;

		object IAstVisitor<object, object>.VisitInvocationExpression (InvocationExpression invocationExpression, object data)
		{
			var handler = InvocationExpressionVisited;
			if (handler != null)
				handler (invocationExpression);
			return VisitChildren (invocationExpression, data);
		}
		
		public event ObserveNodeHandler<DirectionExpression> DirectionExpressionVisited;

		object IAstVisitor<object, object>.VisitDirectionExpression (DirectionExpression directionExpression, object data)
		{
			var handler = DirectionExpressionVisited;
			if (handler != null)
				handler (directionExpression);
			return VisitChildren (directionExpression, data);
		}
		
		public event ObserveNodeHandler<MemberReferenceExpression> MemberReferenceExpressionVisited;

		object IAstVisitor<object, object>.VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression, object data)
		{
			var handler = MemberReferenceExpressionVisited;
			if (handler != null)
				handler (memberReferenceExpression);
			return VisitChildren (memberReferenceExpression, data);
		}
		
		public event ObserveNodeHandler<NullReferenceExpression> NullReferenceExpressionVisited;

		object IAstVisitor<object, object>.VisitNullReferenceExpression (NullReferenceExpression nullReferenceExpression, object data)
		{
			var handler = NullReferenceExpressionVisited;
			if (handler != null)
				handler (nullReferenceExpression);
			return VisitChildren (nullReferenceExpression, data);
		}
		
		public event ObserveNodeHandler<ObjectCreateExpression> ObjectCreateExpressionVisited;

		object IAstVisitor<object, object>.VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression, object data)
		{
			var handler = ObjectCreateExpressionVisited;
			if (handler != null)
				handler (objectCreateExpression);
			return VisitChildren (objectCreateExpression, data);
		}
		
		public event ObserveNodeHandler<AnonymousTypeCreateExpression> AnonymousTypeCreateExpressionVisited;

		object IAstVisitor<object, object>.VisitAnonymousTypeCreateExpression (AnonymousTypeCreateExpression anonymousTypeCreateExpression, object data)
		{
			var handler = AnonymousTypeCreateExpressionVisited;
			if (handler != null)
				handler (anonymousTypeCreateExpression);
			return VisitChildren (anonymousTypeCreateExpression, data);
		}
		
		public event ObserveNodeHandler<ArrayCreateExpression> ArrayCreateExpressionVisited;

		object IAstVisitor<object, object>.VisitArrayCreateExpression (ArrayCreateExpression arrayObjectCreateExpression, object data)
		{
			var handler = ArrayCreateExpressionVisited;
			if (handler != null)
				handler (arrayObjectCreateExpression);
			return VisitChildren (arrayObjectCreateExpression, data);
		}
		
		public event ObserveNodeHandler<ParenthesizedExpression> ParenthesizedExpressionVisited;

		object IAstVisitor<object, object>.VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression, object data)
		{
			var handler = ParenthesizedExpressionVisited;
			if (handler != null)
				handler (parenthesizedExpression);
			return VisitChildren (parenthesizedExpression, data);
		}
		
		public event ObserveNodeHandler<PointerReferenceExpression> PointerReferenceExpressionVisited;

		object IAstVisitor<object, object>.VisitPointerReferenceExpression (PointerReferenceExpression pointerReferenceExpression, object data)
		{
			var handler = PointerReferenceExpressionVisited;
			if (handler != null)
				handler (pointerReferenceExpression);
			return VisitChildren (pointerReferenceExpression, data);
		}
		
		public event ObserveNodeHandler<PrimitiveExpression> PrimitiveExpressionVisited;

		object IAstVisitor<object, object>.VisitPrimitiveExpression (PrimitiveExpression primitiveExpression, object data)
		{
			var handler = PrimitiveExpressionVisited;
			if (handler != null)
				handler (primitiveExpression);
			return VisitChildren (primitiveExpression, data);
		}
		
		public event ObserveNodeHandler<SizeOfExpression> SizeOfExpressionVisited;

		object IAstVisitor<object, object>.VisitSizeOfExpression (SizeOfExpression sizeOfExpression, object data)
		{
			var handler = SizeOfExpressionVisited;
			if (handler != null)
				handler (sizeOfExpression);
			return VisitChildren (sizeOfExpression, data);
		}
		
		public event ObserveNodeHandler<StackAllocExpression> StackAllocExpressionVisited;

		object IAstVisitor<object, object>.VisitStackAllocExpression (StackAllocExpression stackAllocExpression, object data)
		{
			var handler = StackAllocExpressionVisited;
			if (handler != null)
				handler (stackAllocExpression);
			return VisitChildren (stackAllocExpression, data);
		}
		
		public event ObserveNodeHandler<ThisReferenceExpression> ThisReferenceExpressionVisited;

		object IAstVisitor<object, object>.VisitThisReferenceExpression (ThisReferenceExpression thisReferenceExpression, object data)
		{
			var handler = ThisReferenceExpressionVisited;
			if (handler != null)
				handler (thisReferenceExpression);
			return VisitChildren (thisReferenceExpression, data);
		}
		
		public event ObserveNodeHandler<TypeOfExpression> TypeOfExpressionVisited;

		object IAstVisitor<object, object>.VisitTypeOfExpression (TypeOfExpression typeOfExpression, object data)
		{
			var handler = TypeOfExpressionVisited;
			if (handler != null)
				handler (typeOfExpression);
			return VisitChildren (typeOfExpression, data);
		}
		
		public event ObserveNodeHandler<TypeReferenceExpression> TypeReferenceExpressionVisited;

		object IAstVisitor<object, object>.VisitTypeReferenceExpression (TypeReferenceExpression typeReferenceExpression, object data)
		{
			var handler = TypeReferenceExpressionVisited;
			if (handler != null)
				handler (typeReferenceExpression);
			return VisitChildren (typeReferenceExpression, data);
		}
		
		public event ObserveNodeHandler<UnaryOperatorExpression> UnaryOperatorExpressionVisited;

		object IAstVisitor<object, object>.VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			var handler = UnaryOperatorExpressionVisited;
			if (handler != null)
				handler (unaryOperatorExpression);
			return VisitChildren (unaryOperatorExpression, data);
		}
		
		public event ObserveNodeHandler<UncheckedExpression> UncheckedExpressionVisited;

		object IAstVisitor<object, object>.VisitUncheckedExpression (UncheckedExpression uncheckedExpression, object data)
		{
			var handler = UncheckedExpressionVisited;
			if (handler != null)
				handler (uncheckedExpression);
			return VisitChildren (uncheckedExpression, data);
		}
		
		public event ObserveNodeHandler<QueryExpression> QueryExpressionVisited;

		object IAstVisitor<object, object>.VisitQueryExpression (QueryExpression queryExpression, object data)
		{
			var handler = QueryExpressionVisited;
			if (handler != null)
				handler (queryExpression);
			return VisitChildren (queryExpression, data);
		}
		
		public event ObserveNodeHandler<QueryContinuationClause> QueryContinuationClauseVisited;

		object IAstVisitor<object, object>.VisitQueryContinuationClause (QueryContinuationClause queryContinuationClause, object data)
		{
			var handler = QueryContinuationClauseVisited;
			if (handler != null)
				handler (queryContinuationClause);
			return VisitChildren (queryContinuationClause, data);
		}
		
		public event ObserveNodeHandler<QueryFromClause> QueryFromClauseVisited;

		object IAstVisitor<object, object>.VisitQueryFromClause (QueryFromClause queryFromClause, object data)
		{
			var handler = QueryFromClauseVisited;
			if (handler != null)
				handler (queryFromClause);
			return VisitChildren (queryFromClause, data);
		}
		
		public event ObserveNodeHandler<QueryLetClause> QueryLetClauseVisited;

		object IAstVisitor<object, object>.VisitQueryLetClause (QueryLetClause queryLetClause, object data)
		{
			var handler = QueryLetClauseVisited;
			if (handler != null)
				handler (queryLetClause);
			return VisitChildren (queryLetClause, data);
		}
		
		public event ObserveNodeHandler<QueryWhereClause> QueryWhereClauseVisited;

		object IAstVisitor<object, object>.VisitQueryWhereClause (QueryWhereClause queryWhereClause, object data)
		{
			var handler = QueryWhereClauseVisited;
			if (handler != null)
				handler (queryWhereClause);
			return VisitChildren (queryWhereClause, data);
		}
		
		public event ObserveNodeHandler<QueryJoinClause> QueryJoinClauseVisited;

		object IAstVisitor<object, object>.VisitQueryJoinClause (QueryJoinClause queryJoinClause, object data)
		{
			var handler = QueryJoinClauseVisited;
			if (handler != null)
				handler (queryJoinClause);
			return VisitChildren (queryJoinClause, data);
		}
		
		public event ObserveNodeHandler<QueryOrderClause> QueryOrderClauseVisited;

		object IAstVisitor<object, object>.VisitQueryOrderClause (QueryOrderClause queryOrderClause, object data)
		{
			var handler = QueryOrderClauseVisited;
			if (handler != null)
				handler (queryOrderClause);
			return VisitChildren (queryOrderClause, data);
		}
		
		public event ObserveNodeHandler<QueryOrdering> QueryOrderingVisited;

		object IAstVisitor<object, object>.VisitQueryOrdering (QueryOrdering queryOrdering, object data)
		{
			var handler = QueryOrderingVisited;
			if (handler != null)
				handler (queryOrdering);
			return VisitChildren (queryOrdering, data);
		}
		
		public event ObserveNodeHandler<QuerySelectClause> QuerySelectClauseVisited;

		object IAstVisitor<object, object>.VisitQuerySelectClause (QuerySelectClause querySelectClause, object data)
		{
			var handler = QuerySelectClauseVisited;
			if (handler != null)
				handler (querySelectClause);
			return VisitChildren (querySelectClause, data);
		}
		
		public event ObserveNodeHandler<QueryGroupClause> QueryGroupClauseVisited;

		object IAstVisitor<object, object>.VisitQueryGroupClause (QueryGroupClause queryGroupClause, object data)
		{
			var handler = QueryGroupClauseVisited;
			if (handler != null)
				handler (queryGroupClause);
			return VisitChildren (queryGroupClause, data);
		}
		
		public event ObserveNodeHandler<AsExpression> AsExpressionVisited;

		object IAstVisitor<object, object>.VisitAsExpression (AsExpression asExpression, object data)
		{
			var handler = AsExpressionVisited;
			if (handler != null)
				handler (asExpression);
			return VisitChildren (asExpression, data);
		}
		
		public event ObserveNodeHandler<IsExpression> IsExpressionVisited;

		object IAstVisitor<object, object>.VisitIsExpression (IsExpression isExpression, object data)
		{
			var handler = IsExpressionVisited;
			if (handler != null)
				handler (isExpression);
			return VisitChildren (isExpression, data);
		}
		
		public event ObserveNodeHandler<DefaultValueExpression> DefaultValueExpressionVisited;

		object IAstVisitor<object, object>.VisitDefaultValueExpression (DefaultValueExpression defaultValueExpression, object data)
		{
			var handler = DefaultValueExpressionVisited;
			if (handler != null)
				handler (defaultValueExpression);
			return VisitChildren (defaultValueExpression, data);
		}
		
		public event ObserveNodeHandler<UndocumentedExpression> UndocumentedExpressionVisited;

		object IAstVisitor<object, object>.VisitUndocumentedExpression (UndocumentedExpression undocumentedExpression, object data)
		{
			var handler = UndocumentedExpressionVisited;
			if (handler != null)
				handler (undocumentedExpression);
			return VisitChildren (undocumentedExpression, data);
		}
		
		public event ObserveNodeHandler<ArrayInitializerExpression> ArrayInitializerExpressionVisited;

		object IAstVisitor<object, object>.VisitArrayInitializerExpression (ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			var handler = ArrayInitializerExpressionVisited;
			if (handler != null)
				handler (arrayInitializerExpression);
			return VisitChildren (arrayInitializerExpression, data);
		}
		
		public event ObserveNodeHandler<ArraySpecifier> ArraySpecifierVisited;

		object IAstVisitor<object, object>.VisitArraySpecifier (ArraySpecifier arraySpecifier, object data)
		{
			var handler = ArraySpecifierVisited;
			if (handler != null)
				handler (arraySpecifier);
			return VisitChildren (arraySpecifier, data);
		}
		
		public event ObserveNodeHandler<NamedArgumentExpression> NamedArgumentExpressionVisited;

		object IAstVisitor<object, object>.VisitNamedArgumentExpression (NamedArgumentExpression namedArgumentExpression, object data)
		{
			var handler = NamedArgumentExpressionVisited;
			if (handler != null)
				handler (namedArgumentExpression);
			return VisitChildren (namedArgumentExpression, data);
		}
		
		public event ObserveNodeHandler<EmptyExpression> EmptyExpressionVisited;

		object IAstVisitor<object, object>.VisitEmptyExpression (EmptyExpression emptyExpression, object data)
		{
			var handler = EmptyExpressionVisited;
			if (handler != null)
				handler (emptyExpression);
			return VisitChildren (emptyExpression, data);
		}
		
		object IAstVisitor<object, object>.VisitPatternPlaceholder (AstNode placeholder, PatternMatching.Pattern pattern, object data)
		{
			return VisitChildren (placeholder, data);
		}
	}
}


