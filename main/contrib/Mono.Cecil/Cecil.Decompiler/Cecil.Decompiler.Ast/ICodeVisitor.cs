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

namespace Cecil.Decompiler.Ast {

	public interface ICodeVisitor {
		void VisitBlockStatement (BlockStatement node);
		void VisitReturnStatement (ReturnStatement node);
		void VisitGotoStatement (GotoStatement node);
		void VisitLabeledStatement (LabeledStatement node);
		void VisitIfStatement (IfStatement node);
		void VisitExpressionStatement (ExpressionStatement node);
		void VisitThrowStatement (ThrowStatement node);
		void VisitWhileStatement (WhileStatement node);
		void VisitDoWhileStatement (DoWhileStatement node);
		void VisitBreakStatement (BreakStatement node);
		void VisitContinueStatement (ContinueStatement node);
		void VisitForStatement (ForStatement node);
		void VisitForEachStatement (ForEachStatement node);
		void VisitConditionCase (ConditionCase node);
		void VisitDefaultCase (DefaultCase node);
		void VisitSwitchStatement (SwitchStatement node);
		void VisitCatchClause (CatchClause node);
		void VisitTryStatement (TryStatement node);
		void VisitBlockExpression (BlockExpression node);
		void VisitMethodInvocationExpression (MethodInvocationExpression node);
		void VisitMethodReferenceExpression (MethodReferenceExpression node);
		void VisitDelegateCreationExpression (DelegateCreationExpression node);
		void VisitDelegateInvocationExpression (DelegateInvocationExpression node);
		void VisitLiteralExpression (LiteralExpression node);
		void VisitUnaryExpression (UnaryExpression node);
		void VisitBinaryExpression (BinaryExpression node);
		void VisitAssignExpression (AssignExpression node);
		void VisitArgumentReferenceExpression (ArgumentReferenceExpression node);
		void VisitVariableReferenceExpression (VariableReferenceExpression node);
		void VisitVariableDeclarationExpression (VariableDeclarationExpression node);
		void VisitThisReferenceExpression (ThisReferenceExpression node);
		void VisitBaseReferenceExpression (BaseReferenceExpression node);
		void VisitFieldReferenceExpression (FieldReferenceExpression node);
		void VisitCastExpression (CastExpression node);
		void VisitSafeCastExpression (SafeCastExpression node);
		void VisitCanCastExpression (CanCastExpression node);
		void VisitTypeOfExpression (TypeOfExpression node);
		void VisitConditionExpression (ConditionExpression node);
		void VisitNullCoalesceExpression (NullCoalesceExpression node);
		void VisitAddressDereferenceExpression (AddressDereferenceExpression node);
		void VisitAddressReferenceExpression (AddressReferenceExpression node);
		void VisitAddressOfExpression (AddressOfExpression node);
		void VisitArrayCreationExpression (ArrayCreationExpression node);
		void VisitArrayIndexerExpression (ArrayIndexerExpression node);
		void VisitObjectCreationExpression (ObjectCreationExpression node);
		void VisitPropertyReferenceExpression (PropertyReferenceExpression node);
		void VisitTypeReferenceExpression (TypeReferenceExpression node);
	}
}
