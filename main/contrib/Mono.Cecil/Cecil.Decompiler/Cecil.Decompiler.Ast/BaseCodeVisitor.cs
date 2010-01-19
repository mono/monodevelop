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

namespace Cecil.Decompiler.Ast {

	public class BaseCodeVisitor : ICodeVisitor {

		public virtual void Visit (ICodeNode node)
		{
			if (null == node)
				return;

			switch (node.CodeNodeType) {
			case CodeNodeType.BlockStatement:
				VisitBlockStatement ((BlockStatement) node);
				break;
			case CodeNodeType.ReturnStatement:
				VisitReturnStatement ((ReturnStatement) node);
				break;
			case CodeNodeType.GotoStatement:
				VisitGotoStatement ((GotoStatement) node);
				break;
			case CodeNodeType.LabeledStatement:
				VisitLabeledStatement ((LabeledStatement) node);
				break;
			case CodeNodeType.IfStatement:
				VisitIfStatement ((IfStatement) node);
				break;
			case CodeNodeType.ExpressionStatement:
				VisitExpressionStatement ((ExpressionStatement) node);
				break;
			case CodeNodeType.ThrowStatement:
				VisitThrowStatement ((ThrowStatement) node);
				break;
			case CodeNodeType.WhileStatement:
				VisitWhileStatement ((WhileStatement) node);
				break;
			case CodeNodeType.DoWhileStatement:
				VisitDoWhileStatement ((DoWhileStatement) node);
				break;
			case CodeNodeType.BreakStatement:
				VisitBreakStatement ((BreakStatement) node);
				break;
			case CodeNodeType.ContinueStatement:
				VisitContinueStatement ((ContinueStatement) node);
				break;
			case CodeNodeType.ForStatement:
				VisitForStatement ((ForStatement) node);
				break;
			case CodeNodeType.ForEachStatement:
				VisitForEachStatement ((ForEachStatement) node);
				break;
			case CodeNodeType.ConditionCase:
				VisitConditionCase ((ConditionCase) node);
				break;
			case CodeNodeType.DefaultCase:
				VisitDefaultCase ((DefaultCase) node);
				break;
			case CodeNodeType.SwitchStatement:
				VisitSwitchStatement ((SwitchStatement) node);
				break;
			case CodeNodeType.CatchClause:
				VisitCatchClause ((CatchClause) node);
				break;
			case CodeNodeType.TryStatement:
				VisitTryStatement ((TryStatement) node);
				break;
			case CodeNodeType.BlockExpression:
				VisitBlockExpression ((BlockExpression) node);
				break;
			case CodeNodeType.MethodInvocationExpression:
				VisitMethodInvocationExpression ((MethodInvocationExpression) node);
				break;
			case CodeNodeType.MethodReferenceExpression:
				VisitMethodReferenceExpression ((MethodReferenceExpression) node);
				break;
			case CodeNodeType.DelegateCreationExpression:
				VisitDelegateCreationExpression ((DelegateCreationExpression) node);
				break;
			case CodeNodeType.DelegateInvocationExpression:
				VisitDelegateInvocationExpression ((DelegateInvocationExpression) node);
				break;
			case CodeNodeType.LiteralExpression:
				VisitLiteralExpression ((LiteralExpression) node);
				break;
			case CodeNodeType.UnaryExpression:
				VisitUnaryExpression ((UnaryExpression) node);
				break;
			case CodeNodeType.BinaryExpression:
				VisitBinaryExpression ((BinaryExpression) node);
				break;
			case CodeNodeType.AssignExpression:
				VisitAssignExpression ((AssignExpression) node);
				break;
			case CodeNodeType.ArgumentReferenceExpression:
				VisitArgumentReferenceExpression ((ArgumentReferenceExpression) node);
				break;
			case CodeNodeType.VariableReferenceExpression:
				VisitVariableReferenceExpression ((VariableReferenceExpression) node);
				break;
			case CodeNodeType.VariableDeclarationExpression:
				VisitVariableDeclarationExpression ((VariableDeclarationExpression) node);
				break;
			case CodeNodeType.ThisReferenceExpression:
				VisitThisReferenceExpression ((ThisReferenceExpression) node);
				break;
			case CodeNodeType.BaseReferenceExpression:
				VisitBaseReferenceExpression ((BaseReferenceExpression) node);
				break;
			case CodeNodeType.FieldReferenceExpression:
				VisitFieldReferenceExpression ((FieldReferenceExpression) node);
				break;
			case CodeNodeType.CastExpression:
				VisitCastExpression ((CastExpression) node);
				break;
			case CodeNodeType.SafeCastExpression:
				VisitSafeCastExpression ((SafeCastExpression) node);
				break;
			case CodeNodeType.CanCastExpression:
				VisitCanCastExpression ((CanCastExpression) node);
				break;
			case CodeNodeType.TypeOfExpression:
				VisitTypeOfExpression ((TypeOfExpression) node);
				break;
			case CodeNodeType.ConditionExpression:
				VisitConditionExpression ((ConditionExpression) node);
				break;
			case CodeNodeType.NullCoalesceExpression:
				VisitNullCoalesceExpression ((NullCoalesceExpression) node);
				break;
			case CodeNodeType.AddressDereferenceExpression:
				VisitAddressDereferenceExpression ((AddressDereferenceExpression) node);
				break;
			case CodeNodeType.AddressReferenceExpression:
				VisitAddressReferenceExpression ((AddressReferenceExpression) node);
				break;
			case CodeNodeType.AddressOfExpression:
				VisitAddressOfExpression ((AddressOfExpression) node);
				break;
			case CodeNodeType.ArrayCreationExpression:
				VisitArrayCreationExpression ((ArrayCreationExpression) node);
				break;
			case CodeNodeType.ArrayIndexerExpression:
				VisitArrayIndexerExpression ((ArrayIndexerExpression) node);
				break;
			case CodeNodeType.ObjectCreationExpression:
				VisitObjectCreationExpression ((ObjectCreationExpression) node);
				break;
			case CodeNodeType.PropertyReferenceExpression:
				VisitPropertyReferenceExpression ((PropertyReferenceExpression) node);
				break;
			case CodeNodeType.TypeReferenceExpression:
				VisitTypeReferenceExpression ((TypeReferenceExpression) node);
				break;
			default:
				throw new ArgumentException ();
			}
		}

		public virtual void Visit (IEnumerable collection)
		{
			foreach (ICodeNode node in collection)
				Visit (node);
		}

		public virtual void VisitBlockStatement (BlockStatement node)
		{
			Visit (node.Statements);
		}

		public virtual void VisitReturnStatement (ReturnStatement node)
		{
			Visit (node.Expression);
		}

		public virtual void VisitGotoStatement (GotoStatement node)
		{
		}

		public virtual void VisitLabeledStatement (LabeledStatement node)
		{
		}

		public virtual void VisitIfStatement (IfStatement node)
		{
			Visit (node.Condition);
			Visit (node.Then);
			Visit (node.Else);
		}

		public virtual void VisitExpressionStatement (ExpressionStatement node)
		{
			Visit (node.Expression);
		}

		public virtual void VisitThrowStatement (ThrowStatement node)
		{
			Visit (node.Expression);
		}

		public virtual void VisitWhileStatement (WhileStatement node)
		{
			Visit (node.Condition);
			Visit (node.Body);
		}

		public virtual void VisitDoWhileStatement (DoWhileStatement node)
		{
			Visit (node.Condition);
			Visit (node.Body);
		}

		public virtual void VisitBreakStatement (BreakStatement node)
		{
		}

		public virtual void VisitContinueStatement (ContinueStatement node)
		{
		}

		public virtual void VisitForStatement (ForStatement node)
		{
			Visit (node.Initializer);
			Visit (node.Condition);
			Visit (node.Increment);
			Visit (node.Body);
		}

		public virtual void VisitForEachStatement (ForEachStatement node)
		{
			Visit (node.Variable);
			Visit (node.Expression);
			Visit (node.Body);
		}

		public virtual void VisitConditionCase (ConditionCase node)
		{
			Visit (node.Condition);
		}

		public virtual void VisitDefaultCase (DefaultCase node)
		{
		}

		public virtual void VisitSwitchStatement (SwitchStatement node)
		{
			Visit (node.Expression);
			Visit (node.Cases);
		}

		public virtual void VisitCatchClause (CatchClause node)
		{
			Visit (node.Body);
			Visit (node.Variable);
		}

		public virtual void VisitTryStatement (TryStatement node)
		{
			Visit (node.Try);
			Visit (node.CatchClauses);
			Visit (node.Fault);
			Visit (node.Finally);
		}

		public virtual void VisitBlockExpression (BlockExpression node)
		{
			Visit (node.Expressions);
		}

		public virtual void VisitMethodInvocationExpression (MethodInvocationExpression node)
		{
			Visit (node.Method);
			Visit (node.Arguments);
		}

		public virtual void VisitMethodReferenceExpression (MethodReferenceExpression node)
		{
			Visit (node.Target);
		}

		public virtual void VisitDelegateCreationExpression (DelegateCreationExpression node)
		{
			Visit (node.Target);
		}

		public virtual void VisitDelegateInvocationExpression (DelegateInvocationExpression node)
		{
			Visit (node.Target);
			Visit (node.Arguments);
		}

		public virtual void VisitLiteralExpression (LiteralExpression node)
		{
		}

		public virtual void VisitUnaryExpression (UnaryExpression node)
		{
			Visit (node.Operand);
		}

		public virtual void VisitBinaryExpression (BinaryExpression node)
		{
			Visit (node.Left);
			Visit (node.Right);
		}

		public virtual void VisitAssignExpression (AssignExpression node)
		{
			Visit (node.Target);
			Visit (node.Expression);
		}

		public virtual void VisitArgumentReferenceExpression (ArgumentReferenceExpression node)
		{
		}

		public virtual void VisitVariableReferenceExpression (VariableReferenceExpression node)
		{
		}

		public virtual void VisitVariableDeclarationExpression (VariableDeclarationExpression node)
		{
		}

		public virtual void VisitThisReferenceExpression (ThisReferenceExpression node)
		{
		}

		public virtual void VisitBaseReferenceExpression (BaseReferenceExpression node)
		{
		}

		public virtual void VisitFieldReferenceExpression (FieldReferenceExpression node)
		{
			Visit (node.Target);
		}

		public virtual void VisitCastExpression (CastExpression node)
		{
			Visit (node.Expression);
		}

		public virtual void VisitSafeCastExpression (SafeCastExpression node)
		{
			Visit (node.Expression);
		}

		public virtual void VisitCanCastExpression (CanCastExpression node)
		{
			Visit (node.Expression);
		}

		public virtual void VisitTypeOfExpression (TypeOfExpression node)
		{
		}

		public virtual void VisitConditionExpression (ConditionExpression node)
		{
			Visit (node.Condition);
			Visit (node.Then);
			Visit (node.Else);
		}

		public virtual void VisitNullCoalesceExpression (NullCoalesceExpression node)
		{
			Visit (node.Condition);
			Visit (node.Expression);
		}

		public virtual void VisitAddressDereferenceExpression (AddressDereferenceExpression node)
		{
			Visit (node.Expression);
		}

		public virtual void VisitAddressReferenceExpression (AddressReferenceExpression node)
		{
			Visit (node.Expression);
		}

		public virtual void VisitAddressOfExpression (AddressOfExpression node)
		{
			Visit (node.Expression);
		}

		public virtual void VisitArrayCreationExpression (ArrayCreationExpression node)
		{
			Visit (node.Dimensions);
			Visit (node.Initializer);
		}

		public virtual void VisitArrayIndexerExpression (ArrayIndexerExpression node)
		{
			Visit (node.Target);
			Visit (node.Indices);
		}

		public virtual void VisitObjectCreationExpression (ObjectCreationExpression node)
		{
			Visit (node.Arguments);
			Visit (node.Initializer);
		}

		public virtual void VisitPropertyReferenceExpression (PropertyReferenceExpression node)
		{
			Visit (node.Target);
		}

		public virtual void VisitTypeReferenceExpression (TypeReferenceExpression node)
		{
		}
	}
}
