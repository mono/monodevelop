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

	public interface ICodeTransformer {
		ICodeNode VisitBlockStatement (BlockStatement node);
		ICodeNode VisitReturnStatement (ReturnStatement node);
		ICodeNode VisitGotoStatement (GotoStatement node);
		ICodeNode VisitLabeledStatement (LabeledStatement node);
		ICodeNode VisitIfStatement (IfStatement node);
		ICodeNode VisitExpressionStatement (ExpressionStatement node);
		ICodeNode VisitThrowStatement (ThrowStatement node);
		ICodeNode VisitWhileStatement (WhileStatement node);
		ICodeNode VisitDoWhileStatement (DoWhileStatement node);
		ICodeNode VisitBreakStatement (BreakStatement node);
		ICodeNode VisitContinueStatement (ContinueStatement node);
		ICodeNode VisitForStatement (ForStatement node);
		ICodeNode VisitForEachStatement (ForEachStatement node);
		ICodeNode VisitConditionCase (ConditionCase node);
		ICodeNode VisitDefaultCase (DefaultCase node);
		ICodeNode VisitSwitchStatement (SwitchStatement node);
		ICodeNode VisitCatchClause (CatchClause node);
		ICodeNode VisitTryStatement (TryStatement node);
		ICodeNode VisitBlockExpression (BlockExpression node);
		ICodeNode VisitMethodInvocationExpression (MethodInvocationExpression node);
		ICodeNode VisitMethodReferenceExpression (MethodReferenceExpression node);
		ICodeNode VisitDelegateCreationExpression (DelegateCreationExpression node);
		ICodeNode VisitDelegateInvocationExpression (DelegateInvocationExpression node);
		ICodeNode VisitLiteralExpression (LiteralExpression node);
		ICodeNode VisitUnaryExpression (UnaryExpression node);
		ICodeNode VisitBinaryExpression (BinaryExpression node);
		ICodeNode VisitAssignExpression (AssignExpression node);
		ICodeNode VisitArgumentReferenceExpression (ArgumentReferenceExpression node);
		ICodeNode VisitVariableReferenceExpression (VariableReferenceExpression node);
		ICodeNode VisitVariableDeclarationExpression (VariableDeclarationExpression node);
		ICodeNode VisitThisReferenceExpression (ThisReferenceExpression node);
		ICodeNode VisitBaseReferenceExpression (BaseReferenceExpression node);
		ICodeNode VisitFieldReferenceExpression (FieldReferenceExpression node);
		ICodeNode VisitCastExpression (CastExpression node);
		ICodeNode VisitSafeCastExpression (SafeCastExpression node);
		ICodeNode VisitCanCastExpression (CanCastExpression node);
		ICodeNode VisitTypeOfExpression (TypeOfExpression node);
		ICodeNode VisitConditionExpression (ConditionExpression node);
		ICodeNode VisitNullCoalesceExpression (NullCoalesceExpression node);
		ICodeNode VisitAddressDereferenceExpression (AddressDereferenceExpression node);
		ICodeNode VisitAddressReferenceExpression (AddressReferenceExpression node);
		ICodeNode VisitAddressOfExpression (AddressOfExpression node);
		ICodeNode VisitArrayCreationExpression (ArrayCreationExpression node);
		ICodeNode VisitArrayIndexerExpression (ArrayIndexerExpression node);
		ICodeNode VisitObjectCreationExpression (ObjectCreationExpression node);
		ICodeNode VisitPropertyReferenceExpression (PropertyReferenceExpression node);
		ICodeNode VisitTypeReferenceExpression (TypeReferenceExpression node);
	}
}
