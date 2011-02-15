// 
// LookupTableVisitor.cs
//  
// Author:
//       mkrueger <mkrueger@novell.com>
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
using System.Collections.Generic;
using MonoDevelop.CSharp.Ast.Utils;

namespace MonoDevelop.CSharp.Ast
{
	public sealed class LocalLookupVariable
	{
		public string Name { get; private set; }
		public AstType TypeRef { get; private set; }
		public AstLocation StartPos { get; private set; }
		public AstLocation EndPos { get; private set; }
		public bool IsConst { get; private set; }
		public bool IsLoopVariable { get; private set; }
		public Expression Initializer { get; private set; }
		public Expression ParentLambdaExpression { get; private set; }
		public bool IsQueryContinuation { get; private set; }

		public LocalLookupVariable (string name, AstType typeRef, AstLocation startPos, AstLocation endPos, bool isConst, bool isLoopVariable, Expression initializer, Expression parentLambdaExpression, bool isQueryContinuation)
		{
			this.Name = name;
			this.TypeRef = typeRef;
			this.StartPos = startPos;
			this.EndPos = endPos;
			this.IsConst = isConst;
			this.IsLoopVariable = isLoopVariable;
			this.Initializer = initializer;
			this.ParentLambdaExpression = parentLambdaExpression;
			this.IsQueryContinuation = isQueryContinuation;
		}
	}

	public class LookupTableVisitor : DepthFirstAstVisitor<object, object>
	{
		Dictionary<string, List<LocalLookupVariable>> variables = new Dictionary<string, List<LocalLookupVariable>> (StringComparer.InvariantCulture);
		
		public Dictionary<string, List<LocalLookupVariable>> Variables {
			get {
				return variables;
			}
		}
		
		public void AddVariable (AstType typeRef, string name, 
							AstLocation startPos, AstLocation endPos, bool isConst, 
							bool isLoopVariable, Expression initializer, 
							Expression parentLambdaExpression, 
							bool isQueryContinuation)
		{
			if (name == null || name.Length == 0) {
				return;
			}
			List<LocalLookupVariable> list;
			if (!variables.ContainsKey (name)) {
				variables [name] = list = new List<LocalLookupVariable> ();
			} else {
				list = (List<LocalLookupVariable>)variables [name];
			}
			list.Add (new LocalLookupVariable (name, typeRef, startPos, endPos, isConst, isLoopVariable, initializer, parentLambdaExpression, isQueryContinuation));
		}
		
		Stack<AstLocation> endLocationStack = new Stack<AstLocation> ();

		AstLocation CurrentEndLocation {
			get {
				return (endLocationStack.Count == 0) ? AstLocation.Empty : endLocationStack.Peek ();
			}
		}
		
		public override object VisitCompilationUnit (CompilationUnit unit, object data)
		{
			variables.Clear ();
			return base.VisitCompilationUnit (unit, data);
		}
		
		public override object VisitBlockStatement (BlockStatement blockStatement, object data)
		{
			endLocationStack.Push (blockStatement.EndLocation);
			base.VisitBlockStatement (blockStatement, data);
			endLocationStack.Pop ();
			return null;
		}
		
		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			foreach (var varDecl in variableDeclarationStatement.Variables) {
				AddVariable (variableDeclarationStatement.Type, 
							varDecl.Name, 
							variableDeclarationStatement.StartLocation, 
							CurrentEndLocation, 
							variableDeclarationStatement.Modifiers.IsConst (), 
							false, 
							varDecl.Initializer, null, false);
			}
			
			
			return base.VisitVariableDeclarationStatement (variableDeclarationStatement, data);
			
		}
		
		public override object VisitForeachStatement (ForeachStatement foreachStatement, object data)
		{
			AddVariable (foreachStatement.VariableType, 
						foreachStatement.VariableName, 
						foreachStatement.StartLocation, 
						foreachStatement.EndLocation, 
						false, true, 
						foreachStatement.InExpression, 
						null, 
						false);
			
			return base.VisitForeachStatement (foreachStatement, data);
		}
		
		public override object VisitAnonymousMethodExpression (AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			foreach (var p in anonymousMethodExpression.Parameters) {
				AddVariable (p.Type, p.Name, 
							anonymousMethodExpression.StartLocation, 
							anonymousMethodExpression.EndLocation, 
							false, false, null, null, false);
			}
			return base.VisitAnonymousMethodExpression (anonymousMethodExpression, data);
		}
		
		// todo: queries (queries get overworked in the new AST)
		
		/*
		public override object VisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
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
						if (catchClause.TypeReference != null && catchClause.VariableName != null) {
							AddVariable(catchClause.TypeReference,
										catchClause.VariableName,
										catchClause.StartLocation,
										catchClause.StatementBlock.EndLocation,
										false, false, null, null, false);
						}
						catchClause.StatementBlock.AcceptVisitor(this, data);
					}
				}
			}
			if (tryCatchStatement.FinallyBlock != null) {
				return tryCatchStatement.FinallyBlock.AcceptVisitor(this, data);
			}
			return data;
		}
		
		public override object VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
		{
			foreach (ParameterDeclarationExpression p in lambdaExpression.Parameters) {
				AddVariable(p.TypeReference, p.ParameterName,
							lambdaExpression.StartLocation, lambdaExpression.ExtendedEndLocation,
							false, false, null, lambdaExpression, false);
			}
			return base.VisitLambdaExpression(lambdaExpression, data);
		}

		public override object VisitQueryExpression(QueryExpression queryExpression, object data)
		{
			endLocationStack.Push(GetQueryVariableEndScope(queryExpression));
			base.VisitQueryExpression(queryExpression, data);
			endLocationStack.Pop();
			return null;
		}

		Location GetQueryVariableEndScope(QueryExpression queryExpression)
		{
			return queryExpression.EndLocation;
		}

		public override object VisitQueryExpressionFromClause(QueryExpressionFromClause fromClause, object data)
		{
			QueryExpression parent = fromClause.Parent as QueryExpression;
			AddVariable(fromClause.Type, fromClause.Identifier,
						fromClause.StartLocation, new Location (CurrentEndLocation.Column + 1, CurrentEndLocation.Line),
						false, true, fromClause.InExpression, null, parent != null && parent.IsQueryContinuation);
			return base.VisitQueryExpressionFromClause(fromClause, data);
		}

		public override object VisitQueryExpressionJoinClause(QueryExpressionJoinClause joinClause, object data)
		{
			if (string.IsNullOrEmpty(joinClause.IntoIdentifier)) {
				AddVariable(joinClause.Type, joinClause.Identifier,
							joinClause.StartLocation, CurrentEndLocation,
							false, true, joinClause.InExpression, null, false);
			} else {
				AddVariable(joinClause.Type, joinClause.Identifier,
							joinClause.StartLocation, joinClause.EndLocation,
							false, true, joinClause.InExpression, null, false);

				AddVariable(joinClause.Type, joinClause.IntoIdentifier,
							joinClause.StartLocation, CurrentEndLocation,
							false, false, joinClause.InExpression, null, false);
			}
			return base.VisitQueryExpressionJoinClause(joinClause, data);
		}

		public override object VisitQueryExpressionLetClause(QueryExpressionLetClause letClause, object data)
		{
			AddVariable(null, letClause.Identifier,
						letClause.StartLocation, CurrentEndLocation,
						false, false, letClause.Expression, null, false);
			return base.VisitQueryExpressionLetClause(letClause, data);
		}

		 */
		
	}
}

