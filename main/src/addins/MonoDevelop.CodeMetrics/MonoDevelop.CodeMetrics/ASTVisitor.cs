//// 
//// CyclomaticComplexity.cs
////  
//// Author:
////       Nikhil Sarda <diff.operator@gmail.com>
////	     Michael J. Hutchinson <diff.operator@gmail.com>
//// 
//// Copyright (c) 2009 Nikhil Sarda, Michael J. Hutchinson
//// 
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
//// 
//// The above copyright notice and this permission notice shall be included in
//// all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//// THE SOFTWARE.
//
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.IO;
//
//using Gtk;
//
//using MonoDevelop.Core;
//using MonoDevelop.Ide.Gui;
//using MonoDevelop.Projects;
//using Mono.TextEditor;
//using ICSharpCode.NRefactory.CSharp;
//
//namespace MonoDevelop.CodeMetrics
//{
//	//TODO Read up on visitor pattern and implement it on top of this routine (recommended by Levi (tak!))
//	public partial class ComplexityMetrics
//	{
//		interface IStatementVisitor
//		{
//			void VisitStatement (BlockStatement statement, MethodProperties meth);
//			void VisitStatement (IfElseStatement statement, MethodProperties meth);
//			void VisitStatement (ElseIfSection statement, MethodProperties meth);
//			void VisitStatement (ForeachStatement statement, MethodProperties meth);
//			void VisitStatement (ForNextStatement  statement, MethodProperties meth);
//			void VisitStatement (ForStatement statement, MethodProperties meth);
//			void VisitStatement (DoLoopStatement statement, MethodProperties meth);
//			void VisitStatement (SwitchStatement statement, MethodProperties meth);
//			void VisitStatement (LocalVariableDeclaration statement, MethodProperties meth);
//			void VisitStatement (ExpressionStatement statement, MethodProperties meth);
//		}
//		
//		interface IExpressionVisitor
//		{
//			void VisitExpression (BinaryOperatorExpression expression, MethodProperties meth);
//			void VisitExpression (MemberReferenceExpression expression, MethodProperties meth);
//			void VisitExpression (ParenthesizedExpression expression, MethodProperties meth);
//			void VisitExpression (ConditionalExpression expression, MethodProperties meth);
//			void VisitExpression (IdentifierExpression expression, MethodProperties meth);
//			void VisitExpression (AssignmentExpression expression, MethodProperties meth);
//			void VisitExpression (PrimitiveExpression expression, MethodProperties meth);
//			void VisitExpression (InvocationExpression expression, MethodProperties meth);
//		}
//		
//		private class ASTVisitor : IStatementVisitor, IExpressionVisitor
//		{
//			private static ClassProperties cls;
//			
//			internal static void EvaluateComplexityMetrics (ICSharpCode.OldNRefactory.Ast.INode method, MethodProperties props)
//			{
//				props.CyclometricComplexity = 1;
//				props.LOCReal=0;
//				props.NumberOfVariables=0;
//				
//				cls = props.ParentClass;
//				
//				ASTVisitor ctxAstVisitor = new ASTVisitor();
//				if(method is MethodDeclaration) {
//					foreach (var statement in ((MethodDeclaration)method).Body.Children) {
//						ctxAstVisitor.VisitStatement(statement, props);
//					}
//				} else if (method is ConstructorDeclaration) {
//					foreach (var statement in ((ConstructorDeclaration)method).Body.Children) {
//						ctxAstVisitor.VisitStatement(statement, props);
//					}
//				}
//				cls.CyclometricComplexity += props.CyclometricComplexity;
//			}
//		
//			private ASTVisitor(){}
//			
//			public void VisitExpression (BinaryOperatorExpression expression, MethodProperties meth)
//			{
//				if(((BinaryOperatorExpression)expression).Op==BinaryOperatorType.LogicalAnd||((BinaryOperatorExpression)expression).Op==BinaryOperatorType.LogicalOr) {
//					meth.CyclometricComplexity++;
//					VisitExpression(((BinaryOperatorExpression)expression).Left , meth);
//					VisitExpression(((BinaryOperatorExpression)expression).Right, meth);
//				}
//			}
//			
//			public void VisitExpression (MemberReferenceExpression expression, MethodProperties meth)
//			{
//				return;
//			}
//			
//			public void VisitExpression (ParenthesizedExpression expression, MethodProperties meth)
//			{
//				VisitExpression(((ParenthesizedExpression)expression).Expression, meth);
//			}
//			
//			public void VisitExpression (ConditionalExpression expression, MethodProperties meth)
//			{
//				VisitExpression(((ConditionalExpression)expression).Condition, meth);
//				VisitExpression(((ConditionalExpression)expression).TrueExpression, meth);
//			}
//			
//			public void VisitExpression (IdentifierExpression expression, MethodProperties meth)
//			{
//				if(cls.Fields.ContainsKey(expression.Identifier)){
//					cls.Fields[expression.Identifier].InternalAccessCount++;
//				} else {
//					foreach(var field in cls.Fields){
//					//TODO External access
//					}
//				
//				}
//			}
//			
//			public void VisitExpression (AssignmentExpression expression, MethodProperties meth)
//			{
//				VisitExpression(expression.Left, meth);
//				VisitExpression(expression.Right, meth);
//			}
//			
//			public void VisitExpression (PrimitiveExpression expression, MethodProperties meth)
//			{
//				return;
//			}
//			
//			public void VisitExpression (InvocationExpression expression, MethodProperties meth)
//			{
//				//Coupling.EvaluateMethodCoupling(expression, meth);
//			}
//			
//			public void VisitStatement (BlockStatement statement, MethodProperties meth)
//			{
//				meth.LOCReal++;
//				foreach(var innerStatement in ((BlockStatement)statement).Children)
//					VisitStatement((Statement)innerStatement, meth);				
//			}
//			
//			public void VisitStatement (IfElseStatement statement, MethodProperties meth)
//			{
//				meth.CyclometricComplexity++;
//				//Process the conditions
//				VisitExpression(((IfElseStatement)statement).Condition, meth);
//				//Handle the true statement
//				foreach(Statement innerStatement in  ((IfElseStatement)statement).TrueStatement)
//					VisitStatement(innerStatement, meth);
//				//Handle the false statement
//				foreach(Statement innerStatement in ((IfElseStatement)statement).FalseStatement) {
//					meth.CyclometricComplexity++;		
//					VisitStatement(innerStatement, meth);
//				}
//				//Handle the ElseIf statements
//				foreach(ElseIfSection elseIfSection in ((IfElseStatement)statement).ElseIfSections)
//					VisitStatement(elseIfSection, meth);
//			}
//			
//			public void VisitStatement (ElseIfSection statement, MethodProperties meth)
//			{
//				meth.CyclometricComplexity++;
//				
//				VisitExpression(statement.Condition, meth);
//				VisitStatement(statement.EmbeddedStatement, meth);
//			}
//			
//			public void VisitStatement (ForeachStatement statement, MethodProperties meth)
//			{
//				meth.CyclometricComplexity++;
//				VisitExpression(((ForeachStatement)(statement)).Expression, meth);
//				
//				VisitExpression(statement.NextExpression, meth);						
//				
//				foreach(var innerStatement in ((ForeachStatement)statement).EmbeddedStatement.Children)
//					VisitStatement((Statement)innerStatement, meth);
//			}
//			
//			public void VisitStatement (ForNextStatement statement, MethodProperties meth)
//			{
//				meth.CyclometricComplexity++;
//				VisitExpression(statement.LoopVariableExpression, meth);
//				
//				foreach(Expression innerExpression in statement.NextExpressions)
//					VisitExpression(innerExpression, meth);
//				
//				foreach(var innerStatement in statement.EmbeddedStatement.Children)
//					VisitStatement(innerStatement, meth);
//			}
//			
//			public void VisitStatement (ForStatement statement, MethodProperties meth)
//			{
//				meth.CyclometricComplexity++;
//				VisitExpression(statement.Condition, meth);
//				
//				foreach(var innerStatement in ((ForStatement)statement).EmbeddedStatement.Children)
//					VisitStatement(innerStatement, meth);
//			}
//			
//			public void VisitStatement (DoLoopStatement statement, MethodProperties meth)
//			{
//				meth.CyclometricComplexity++;
//				VisitExpression(statement.Condition, meth);
//				
//				foreach(var innerStatement in statement.EmbeddedStatement.Children)
//					VisitStatement(innerStatement, meth);
//			}
//			
//			public void VisitStatement (SwitchStatement statement, MethodProperties meth)
//			{
//				meth.CyclometricComplexity++;
//				VisitExpression(((SwitchStatement)statement).SwitchExpression, meth);
//				foreach(SwitchSection innerSection in ((SwitchStatement)statement).SwitchSections){
//					meth.CyclometricComplexity++;
//					foreach(var caseLabel in innerSection.SwitchLabels)
//						VisitExpression(caseLabel.ToExpression, meth);
//				}
//			}
//			
//			public void VisitStatement (LocalVariableDeclaration statement, MethodProperties meth) 
//			{
//				meth.NumberOfVariables+=statement.Variables.Count;
//				foreach(VariableDeclaration variable in statement.Variables)
//					VisitExpression(variable.Initializer, meth);
//			}
//			
//			public void VisitStatement (ExpressionStatement statement, MethodProperties meth)
//			{
//				//TODO Ability to evaluate access count of external fields
//				// Currently we assume that MemberReferenceExpression is not called and that we directly go on to IdentifierExpression
//				VisitExpression(statement.Expression, meth);
//			}
//			
//			private void VisitStatement(ICSharpCode.OldNRefactory.Ast.INode statement, MethodProperties meth)
//			{
//				try{
//					if(statement is BlockStatement){
//						VisitStatement((BlockStatement)statement, meth);
//					} else if (statement is IfElseStatement) {
//					
//						VisitStatement((IfElseStatement)statement, meth);
//					} else if (statement is ElseIfSection) {
//						
//						VisitStatement((ElseIfSection)statement, meth);			
//					} else if (statement is ForeachStatement) {
//						
//						VisitStatement((ForeachStatement)statement, meth);
//					} else if (statement is ForStatement) {
//						
//						VisitStatement((ForStatement)statement,meth);
//					} else if (statement is ForNextStatement) {
//						
//						VisitStatement((ForNextStatement)statement, meth);
//					} else if (statement is DoLoopStatement) {
//						
//						VisitStatement((DoLoopStatement)statement, meth);
//					} else if (statement is SwitchStatement) {
//												
//						VisitStatement((SwitchStatement)statement, meth); 
//					} else if (statement is LocalVariableDeclaration) {
//						
//						VisitStatement((LocalVariableDeclaration)statement, meth);
//					} else if (statement is ExpressionStatement) {
//						
//						VisitStatement((ExpressionStatement)statement, meth);
//					} 
//				}catch(Exception ex){
//				Console.WriteLine(ex.ToString());
//				}
//			//See other potential types to exploit
//			}	
//			
//			private void VisitExpression(Expression expression, MethodProperties meth)
//			{
//				if(expression is IdentifierExpression){
//					
//					VisitExpression((IdentifierExpression)expression, meth);
//				} else if (expression is AssignmentExpression) {
//					
//					VisitExpression((AssignmentExpression)expression, meth);
//				} else if (expression is PrimitiveExpression) {
//					
//					VisitExpression((PrimitiveExpression)expression, meth);
//				} else if (expression is BinaryOperatorExpression) {
//			
//					VisitExpression((BinaryOperatorExpression) expression, meth);
//				} else if (expression is MemberReferenceExpression) {
//					//TODO something
//				} else if (expression is ParenthesizedExpression) {
//					
//					VisitExpression((ParenthesizedExpression)expression, meth);
//				} else if (expression is ConditionalExpression) {
//					
//					VisitExpression((ConditionalExpression)expression, meth);
//				} else if (expression is InvocationExpression) {
//					
//					VisitExpression((InvocationExpression)expression, meth);
//				}
//			}
//				//There are many more types to exploit
//		}
//	}
//}
//
