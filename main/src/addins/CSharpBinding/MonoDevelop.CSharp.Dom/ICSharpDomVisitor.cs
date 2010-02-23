// 
// ICSharpDomVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.CSharp.Dom
{
	public interface ICSharpDomVisitor<T, S>
	{
		S VisitCompilationUnit (CompilationUnit unit, T data);
		
		#region General scope
		S VisitAttribute (Attribute attribute, T data);
		S VisitAttributeSection (AttributeSection attributeSection, T data);
		S VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, T data);
		S VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, T data);
		S VisitTypeDeclaration (TypeDeclaration typeDeclaration, T data);
		S VisitEnumDeclaration (EnumDeclaration enumDeclaration, T data);
		S VisitEnumMemberDeclaration (EnumMemberDeclaration enumMemberDeclaration, T data);
		S VisitUsingDeclaration (UsingDeclaration usingDeclaration, T data);
		S VisitUsingAliasDeclaration (UsingAliasDeclaration usingDeclaration, T data);
		#endregion
		
		#region Type members
		S VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, T data);
		S VisitConstructorInitializer (ConstructorInitializer constructorInitializer, T data);
		S VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, T data);
		S VisitEventDeclaration (EventDeclaration eventDeclaration, T data);
		S VisitFieldDeclaration (FieldDeclaration fieldDeclaration, T data);
		S VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, T data);
		S VisitMethodDeclaration (MethodDeclaration methodDeclaration, T data);
		S VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, T data);
		S VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, T data);
		S VisitVariableInitializer (VariableInitializer variableInitializer, T data);
		S VisitArgumentDeclaration (ArgumentDeclaration argumentDeclaration, T data);
		#endregion
		
		#region Statements
		S VisitBlockStatement (BlockStatement blockStatement, T data);
		S VisitExpressionStatement (ExpressionStatement expressionStatement, T data);
		S VisitBreakStatement (BreakStatement breakStatement, T data);
		S VisitCheckedStatement (CheckedStatement checkedStatement, T data);
		S VisitContinueStatement (ContinueStatement continueStatement, T data);
		S VisitEmptyStatement (EmptyStatement emptyStatement, T data);
		S VisitFixedStatement (FixedStatement fixedStatement, T data);
		S VisitForeachStatement (ForeachStatement foreachStatement, T data);
		S VisitForStatement (ForStatement forStatement, T data);
		S VisitGotoStatement (GotoStatement gotoStatement, T data);
		S VisitIfElseStatement (IfElseStatement ifElseStatement, T data);
		S VisitLabelStatement (LabelStatement labelStatement, T data);
		S VisitLockStatement (LockStatement lockStatement, T data);
		S VisitReturnStatement (ReturnStatement returnStatement, T data);
		S VisitSwitchStatement (SwitchStatement switchStatement, T data);
		S VisitSwitchSection (SwitchSection switchSection, T data);
		S VisitCaseLabel (CaseLabel caseLabel, T data);
		
		S VisitThrowStatement (ThrowStatement throwStatement, T data);
		S VisitTryCatchStatement (TryCatchStatement tryCatchStatement, T data);
		S VisitCatchClause (CatchClause catchClause, T data);
		
		S VisitUncheckedStatement (UncheckedStatement uncheckedStatement, T data);
		S VisitUnsafeStatement (UnsafeStatement unsafeStatement, T data);
		S VisitUsingStatement (UsingStatement usingStatement, T data);
		S VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, T data);
		S VisitWhileStatement (WhileStatement whileStatement, T data);
		S VisitYieldStatement (YieldStatement yieldStatement, T data);
		#endregion
		
		#region Expressions
		S VisitAnonymousMethodExpression (AnonymousMethodExpression anonymousMethodExpression, T data);
		S VisitAssignmentExpression (AssignmentExpression assignmentExpression, T data);
		S VisitBaseReferenceExpression (BaseReferenceExpression baseReferenceExpression, T data);
		S VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, T data);
		S VisitCastExpression (CastExpression castExpression, T data);
		S VisitCheckedExpression (CheckedExpression checkedExpression, T data);
		S VisitConditionalExpression (ConditionalExpression conditionalExpression, T data);
		S VisitIdentifierExpression (IdentifierExpression identifierExpression, T data);
		S VisitIndexerExpression (IndexerExpression indexerExpression, T data);
		S VisitInvocationExpression (InvocationExpression invocationExpression, T data);
		S VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression, T data);
		S VisitNullReferenceExpression (NullReferenceExpression nullReferenceExpression, T data);
		S VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression, T data);
		S VisitCollectionInitializerExpression (CollectionInitializerExpression collectionInitializerExpression, T data);
		S VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression, T data);
		S VisitPointerReferenceExpression (PointerReferenceExpression pointerReferenceExpression, T data);
		S VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, T data);
		S VisitSizeOfExpression (SizeOfExpression sizeOfExpression, T data);
		S VisitStackAllocExpression (StackAllocExpression stackAllocExpression, T data);
		S VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, T data);
		S VisitTypeOfExpression (TypeOfExpression typeOfExpression, T data);
		S VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression, T data);
		S VisitUncheckedExpression (UncheckedExpression uncheckedExpression, T data);
		S VisitAsExpression (AsExpression asExpression, T data);
		S VisitIsExpression (IsExpression isExpression, T data);
		S VisitDefaultValueExpression (DefaultValueExpression defaultValueExpression, T data);
		#endregion
		
		#region Query Expressions
		S VisitQueryExpressionFromClause (QueryExpressionFromClause queryExpressionFromClause, T data);
		S VisitQueryExpressionWhereClause (QueryExpressionWhereClause queryExpressionWhereClause, T data);
		S VisitQueryExpressionJoinClause (QueryExpressionJoinClause queryExpressionJoinClause, T data);
		S VisitQueryExpressionGroupClause (QueryExpressionGroupClause queryExpressionGroupClause, T data);
		S VisitQueryExpressionLetClause (QueryExpressionLetClause queryExpressionLetClause, T data);
		S VisitQueryExpressionOrderClause (QueryExpressionOrderClause queryExpressionOrderClause, T data);
		S VisitQueryExpressionOrdering (QueryExpressionOrdering queryExpressionOrdering, T data);
		S VisitQueryExpressionSelectClause (QueryExpressionSelectClause queryExpressionSelectClause, T data);
		#endregion
	}
}
