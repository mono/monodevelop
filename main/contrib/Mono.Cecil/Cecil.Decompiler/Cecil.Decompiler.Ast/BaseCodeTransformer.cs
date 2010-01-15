#region license
//
//	(C) 2005 - 2007 db4objects Inc. http://www.db4o.com
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

// Warning: generated do not edit

using System;
using System.Collections;
using System.Collections.Generic;

namespace Cecil.Decompiler.Ast {

	public class BaseCodeTransformer : ICodeTransformer {

		public virtual ICodeNode Visit (ICodeNode node)
		{
			if (node == null)
				return null;

			switch (node.CodeNodeType) {
			case CodeNodeType.BlockStatement:
				return VisitBlockStatement ((BlockStatement) node);
			case CodeNodeType.ReturnStatement:
				return VisitReturnStatement ((ReturnStatement) node);
			case CodeNodeType.GotoStatement:
				return VisitGotoStatement ((GotoStatement) node);
			case CodeNodeType.LabeledStatement:
				return VisitLabeledStatement ((LabeledStatement) node);
			case CodeNodeType.IfStatement:
				return VisitIfStatement ((IfStatement) node);
			case CodeNodeType.ExpressionStatement:
				return VisitExpressionStatement ((ExpressionStatement) node);
			case CodeNodeType.ThrowStatement:
				return VisitThrowStatement ((ThrowStatement) node);
			case CodeNodeType.WhileStatement:
				return VisitWhileStatement ((WhileStatement) node);
			case CodeNodeType.DoWhileStatement:
				return VisitDoWhileStatement ((DoWhileStatement) node);
			case CodeNodeType.BreakStatement:
				return VisitBreakStatement ((BreakStatement) node);
			case CodeNodeType.ContinueStatement:
				return VisitContinueStatement ((ContinueStatement) node);
			case CodeNodeType.ForStatement:
				return VisitForStatement ((ForStatement) node);
			case CodeNodeType.ForEachStatement:
				return VisitForEachStatement ((ForEachStatement) node);
			case CodeNodeType.ConditionCase:
				return VisitConditionCase ((ConditionCase) node);
			case CodeNodeType.DefaultCase:
				return VisitDefaultCase ((DefaultCase) node);
			case CodeNodeType.SwitchStatement:
				return VisitSwitchStatement ((SwitchStatement) node);
			case CodeNodeType.CatchClause:
				return VisitCatchClause ((CatchClause) node);
			case CodeNodeType.TryStatement:
				return VisitTryStatement ((TryStatement) node);
			case CodeNodeType.BlockExpression:
				return VisitBlockExpression ((BlockExpression) node);
			case CodeNodeType.MethodInvocationExpression:
				return VisitMethodInvocationExpression ((MethodInvocationExpression) node);
			case CodeNodeType.MethodReferenceExpression:
				return VisitMethodReferenceExpression ((MethodReferenceExpression) node);
			case CodeNodeType.DelegateCreationExpression:
				return VisitDelegateCreationExpression ((DelegateCreationExpression) node);
			case CodeNodeType.DelegateInvocationExpression:
				return VisitDelegateInvocationExpression ((DelegateInvocationExpression) node);
			case CodeNodeType.LiteralExpression:
				return VisitLiteralExpression ((LiteralExpression) node);
			case CodeNodeType.UnaryExpression:
				return VisitUnaryExpression ((UnaryExpression) node);
			case CodeNodeType.BinaryExpression:
				return VisitBinaryExpression ((BinaryExpression) node);
			case CodeNodeType.AssignExpression:
				return VisitAssignExpression ((AssignExpression) node);
			case CodeNodeType.ArgumentReferenceExpression:
				return VisitArgumentReferenceExpression ((ArgumentReferenceExpression) node);
			case CodeNodeType.VariableReferenceExpression:
				return VisitVariableReferenceExpression ((VariableReferenceExpression) node);
			case CodeNodeType.VariableDeclarationExpression:
				return VisitVariableDeclarationExpression ((VariableDeclarationExpression) node);
			case CodeNodeType.ThisReferenceExpression:
				return VisitThisReferenceExpression ((ThisReferenceExpression) node);
			case CodeNodeType.BaseReferenceExpression:
				return VisitBaseReferenceExpression ((BaseReferenceExpression) node);
			case CodeNodeType.FieldReferenceExpression:
				return VisitFieldReferenceExpression ((FieldReferenceExpression) node);
			case CodeNodeType.CastExpression:
				return VisitCastExpression ((CastExpression) node);
			case CodeNodeType.SafeCastExpression:
				return VisitSafeCastExpression ((SafeCastExpression) node);
			case CodeNodeType.CanCastExpression:
				return VisitCanCastExpression ((CanCastExpression) node);
			case CodeNodeType.TypeOfExpression:
				return VisitTypeOfExpression ((TypeOfExpression) node);
			case CodeNodeType.ConditionExpression:
				return VisitConditionExpression ((ConditionExpression) node);
			case CodeNodeType.NullCoalesceExpression:
				return VisitNullCoalesceExpression ((NullCoalesceExpression) node);
			case CodeNodeType.AddressDereferenceExpression:
				return VisitAddressDereferenceExpression ((AddressDereferenceExpression) node);
			case CodeNodeType.AddressReferenceExpression:
				return VisitAddressReferenceExpression ((AddressReferenceExpression) node);
			case CodeNodeType.AddressOfExpression:
				return VisitAddressOfExpression ((AddressOfExpression) node);
			case CodeNodeType.ArrayCreationExpression:
				return VisitArrayCreationExpression ((ArrayCreationExpression) node);
			case CodeNodeType.ArrayIndexerExpression:
				return VisitArrayIndexerExpression ((ArrayIndexerExpression) node);
			case CodeNodeType.ObjectCreationExpression:
				return VisitObjectCreationExpression ((ObjectCreationExpression) node);
			case CodeNodeType.PropertyReferenceExpression:
				return VisitPropertyReferenceExpression ((PropertyReferenceExpression) node);
			case CodeNodeType.TypeReferenceExpression:
				return VisitTypeReferenceExpression ((TypeReferenceExpression) node);
			default:
				throw new ArgumentException ();
			}
		}

		protected virtual TCollection Visit<TCollection, TElement> (TCollection original)
			where TCollection : class, IList<TElement>, new ()
			where TElement : class, ICodeNode
		{
			TCollection collection = null;

			for (int i = 0; i < original.Count; i++) {
				var element = (TElement) Visit (original [i]);

				if (collection != null) {
					if (element != null)
						collection.Add (element);

					continue;
				}

				if (!EqualityComparer<TElement>.Default.Equals (element, original [i])) {
					collection = new TCollection ();
					for (int j = 0; j < i; j++)
						collection.Add (original [j]);

					if (element != null)
						collection.Add (element);
				}
			}

			return collection ?? original;
		}

		public virtual ICollection<Statement> Visit (StatementCollection node)
		{
			return Visit<StatementCollection, Statement> (node); 
		}

		public virtual ICollection<Expression> Visit (ExpressionCollection node)
		{
			return Visit<ExpressionCollection, Expression> (node); 
		}

		public virtual ICollection<SwitchCase> Visit (SwitchCaseCollection node)
		{
			return Visit<SwitchCaseCollection, SwitchCase> (node); 
		}

		public virtual ICollection<CatchClause> Visit (CatchClauseCollection node)
		{
			return Visit<CatchClauseCollection, CatchClause> (node); 
		}

		public virtual ICodeNode VisitBlockStatement (BlockStatement node)
		{
			node.Statements = (StatementCollection) Visit (node.Statements);
			return node;
		}

		public virtual ICodeNode VisitReturnStatement (ReturnStatement node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitGotoStatement (GotoStatement node)
		{
			return node;
		}

		public virtual ICodeNode VisitLabeledStatement (LabeledStatement node)
		{
			return node;
		}

		public virtual ICodeNode VisitIfStatement (IfStatement node)
		{
			node.Condition = (Expression) Visit (node.Condition);
			node.Then = (BlockStatement) Visit (node.Then);
			node.Else = (BlockStatement) Visit (node.Else);
			return node;
		}

		public virtual ICodeNode VisitExpressionStatement (ExpressionStatement node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitThrowStatement (ThrowStatement node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitWhileStatement (WhileStatement node)
		{
			node.Condition = (Expression) Visit (node.Condition);
			node.Body = (BlockStatement) Visit (node.Body);
			return node;
		}

		public virtual ICodeNode VisitDoWhileStatement (DoWhileStatement node)
		{
			node.Condition = (Expression) Visit (node.Condition);
			node.Body = (BlockStatement) Visit (node.Body);
			return node;
		}

		public virtual ICodeNode VisitBreakStatement (BreakStatement node)
		{
			return node;
		}

		public virtual ICodeNode VisitContinueStatement (ContinueStatement node)
		{
			return node;
		}

		public virtual ICodeNode VisitForStatement (ForStatement node)
		{
			node.Initializer = (Statement) Visit (node.Initializer);
			node.Condition = (Expression) Visit (node.Condition);
			node.Increment = (Statement) Visit (node.Increment);
			node.Body = (BlockStatement) Visit (node.Body);
			return node;
		}

		public virtual ICodeNode VisitForEachStatement (ForEachStatement node)
		{
			node.Variable = (VariableDeclarationExpression) Visit (node.Variable);
			node.Expression = (Expression) Visit (node.Expression);
			node.Body = (BlockStatement) Visit (node.Body);
			return node;
		}

		public virtual ICodeNode VisitConditionCase (ConditionCase node)
		{
			node.Condition = (Expression) Visit (node.Condition);
			return node;
		}

		public virtual ICodeNode VisitDefaultCase (DefaultCase node)
		{
			return node;
		}

		public virtual ICodeNode VisitSwitchStatement (SwitchStatement node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			node.Cases = (SwitchCaseCollection) Visit (node.Cases);
			return node;
		}

		public virtual ICodeNode VisitCatchClause (CatchClause node)
		{
			node.Body = (BlockStatement) Visit (node.Body);
			node.Variable = (VariableDeclarationExpression) Visit (node.Variable);
			return node;
		}

		public virtual ICodeNode VisitTryStatement (TryStatement node)
		{
			node.Try = (BlockStatement) Visit (node.Try);
			node.CatchClauses = (CatchClauseCollection) Visit (node.CatchClauses);
			node.Fault = (BlockStatement) Visit (node.Fault);
			node.Finally = (BlockStatement) Visit (node.Finally);
			return node;
		}

		public virtual ICodeNode VisitBlockExpression (BlockExpression node)
		{
			node.Expressions = (ExpressionCollection) Visit (node.Expressions);
			return node;
		}

		public virtual ICodeNode VisitMethodInvocationExpression (MethodInvocationExpression node)
		{
			node.Method = (Expression) Visit (node.Method);
			node.Arguments = (ExpressionCollection) Visit (node.Arguments);
			return node;
		}

		public virtual ICodeNode VisitMethodReferenceExpression (MethodReferenceExpression node)
		{
			node.Target = (Expression) Visit (node.Target);
			return node;
		}

		public virtual ICodeNode VisitDelegateCreationExpression (DelegateCreationExpression node)
		{
			node.Target = (Expression) Visit (node.Target);
			return node;
		}

		public virtual ICodeNode VisitDelegateInvocationExpression (DelegateInvocationExpression node)
		{
			node.Target = (Expression) Visit (node.Target);
			node.Arguments = (ExpressionCollection) Visit (node.Arguments);
			return node;
		}

		public virtual ICodeNode VisitLiteralExpression (LiteralExpression node)
		{
			return node;
		}

		public virtual ICodeNode VisitUnaryExpression (UnaryExpression node)
		{
			node.Operand = (Expression) Visit (node.Operand);
			return node;
		}

		public virtual ICodeNode VisitBinaryExpression (BinaryExpression node)
		{
			node.Left = (Expression) Visit (node.Left);
			node.Right = (Expression) Visit (node.Right);
			return node;
		}

		public virtual ICodeNode VisitAssignExpression (AssignExpression node)
		{
			node.Target = (Expression) Visit (node.Target);
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitArgumentReferenceExpression (ArgumentReferenceExpression node)
		{
			return node;
		}

		public virtual ICodeNode VisitVariableReferenceExpression (VariableReferenceExpression node)
		{
			return node;
		}

		public virtual ICodeNode VisitVariableDeclarationExpression (VariableDeclarationExpression node)
		{
			return node;
		}

		public virtual ICodeNode VisitThisReferenceExpression (ThisReferenceExpression node)
		{
			return node;
		}

		public virtual ICodeNode VisitBaseReferenceExpression (BaseReferenceExpression node)
		{
			return node;
		}

		public virtual ICodeNode VisitFieldReferenceExpression (FieldReferenceExpression node)
		{
			node.Target = (Expression) Visit (node.Target);
			return node;
		}

		public virtual ICodeNode VisitCastExpression (CastExpression node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitSafeCastExpression (SafeCastExpression node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitCanCastExpression (CanCastExpression node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitTypeOfExpression (TypeOfExpression node)
		{
			return node;
		}

		public virtual ICodeNode VisitConditionExpression (ConditionExpression node)
		{
			node.Condition = (Expression) Visit (node.Condition);
			node.Then = (Expression) Visit (node.Then);
			node.Else = (Expression) Visit (node.Else);
			return node;
		}

		public virtual ICodeNode VisitNullCoalesceExpression (NullCoalesceExpression node)
		{
			node.Condition = (Expression) Visit (node.Condition);
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitAddressDereferenceExpression (AddressDereferenceExpression node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitAddressReferenceExpression (AddressReferenceExpression node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitAddressOfExpression (AddressOfExpression node)
		{
			node.Expression = (Expression) Visit (node.Expression);
			return node;
		}

		public virtual ICodeNode VisitArrayCreationExpression (ArrayCreationExpression node)
		{
			node.Dimensions = (ExpressionCollection) Visit (node.Dimensions);
			node.Initializer = (BlockExpression) Visit (node.Initializer);
			return node;
		}

		public virtual ICodeNode VisitArrayIndexerExpression (ArrayIndexerExpression node)
		{
			node.Target = (Expression) Visit (node.Target);
			node.Indices = (ExpressionCollection) Visit (node.Indices);
			return node;
		}

		public virtual ICodeNode VisitObjectCreationExpression (ObjectCreationExpression node)
		{
			node.Arguments = (ExpressionCollection) Visit (node.Arguments);
			node.Initializer = (BlockExpression) Visit (node.Initializer);
			return node;
		}

		public virtual ICodeNode VisitPropertyReferenceExpression (PropertyReferenceExpression node)
		{
			node.Target = (Expression) Visit (node.Target);
			return node;
		}

		public virtual ICodeNode VisitTypeReferenceExpression (TypeReferenceExpression node)
		{
			return node;
		}
	}
}
