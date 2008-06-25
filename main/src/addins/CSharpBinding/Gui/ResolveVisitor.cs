// ResolveVisitor.cs created with MonoDevelop
// User: mkrueger at 15:56 24.06.2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharpBinding
{
	internal class ResolveVisitor : AbstractAstVisitor
	{
		NRefactoryResolver resolver;
		
		public ResolveVisitor (NRefactoryResolver resolver)
		{
			this.resolver = resolver;
		}
		
		public ResolveResult Resolve (Expression expression)
		{
			ResolveResult result = (ResolveResult)expression.AcceptVisitor (this, null);
			if (result == null)
				result = CreateResult ("");
			return result;
		}
		
		IReturnType GetTypeSafe (Expression expression)
		{
			ResolveResult result = Resolve (expression);
			return result.ResolvedType ?? DomReturnType.Void;
		}
		
		ResolveResult CreateResult (TypeReference typeReference)
		{
			return CreateResult (typeReference.Type);
		}
		
		ResolveResult CreateResult (string fullTypeName)
		{
			ResolveResult result = new ResolveResult ();
			result.CallingType   = resolver.CallingType;
			result.CallingMember = resolver.CallingMember;
			if (!String.IsNullOrEmpty (fullTypeName))
				result.ResolvedType  = new DomReturnType (fullTypeName);
			return result;
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			return resolver.ResolveIdentifier (identifierExpression.Identifier);
		}
		
		public override object VisitSizeOfExpression (SizeOfExpression sizeOfExpression, object data)
		{
			return CreateResult (typeof(System.Int32).FullName);
		}
		
		public override object VisitTypeOfExpression (TypeOfExpression typeOfExpression, object data)
		{
			return CreateResult (typeof(System.Type).FullName);
		}
		
		public override object VisitTypeOfIsExpression (TypeOfIsExpression typeOfIsExpression, object data)
		{
			return CreateResult (typeof(System.Boolean).FullName);
		}
		
		public override object VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression, object data)
		{
			if (parenthesizedExpression == null)
				return null;
			return parenthesizedExpression.Expression.AcceptVisitor (this, data);
		}
		
		public override object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			return CreateResult (arrayCreateExpression.CreateType);
		}
		
		public override object VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data) 
		{
			return Resolve (assignmentExpression.Left);
		}
		
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			return Resolve (unaryOperatorExpression.Expression);
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
				case BinaryOperatorType.ReferenceEquality:
				case BinaryOperatorType.ReferenceInequality:
				case BinaryOperatorType.LogicalAnd:
				case BinaryOperatorType.LogicalOr:
				case BinaryOperatorType.LessThan:
				case BinaryOperatorType.LessThanOrEqual:
				case BinaryOperatorType.GreaterThan:
				case BinaryOperatorType.GreaterThanOrEqual:
					return CreateResult (typeof(bool).FullName);
				case BinaryOperatorType.NullCoalescing:
					return Resolve (binaryOperatorExpression.Left);
				
				// vb operators
				case BinaryOperatorType.DivideInteger:
					return CreateResult (typeof(int).FullName);
				case BinaryOperatorType.Concat:
					return CreateResult (typeof(string).FullName);
					
				default:
					return CreateResult (GetCommonType (GetTypeSafe (binaryOperatorExpression.Left), 
					                                    GetTypeSafe (binaryOperatorExpression.Right)).FullName);
			}
		}
		
		public override object VisitCastExpression (CastExpression castExpression, object data) 
		{
			return CreateResult (castExpression.CastTo);
		}
		
		public override object VisitConditionalExpression (ConditionalExpression conditionalExpression, object data) 
		{
			return CreateResult (GetCommonType (GetTypeSafe (conditionalExpression.TrueExpression), 
			                                    GetTypeSafe (conditionalExpression.FalseExpression)).FullName);
		}
		
		public override object VisitCheckedExpression (CheckedExpression checkedExpression, object data)
		{
			return Resolve (checkedExpression.Expression);
		}
		
		public override object VisitUncheckedExpression (UncheckedExpression uncheckedExpression, object data)
		{
			return Resolve (uncheckedExpression.Expression);
		}
		
		public override object VisitThisReferenceExpression (ThisReferenceExpression thisReferenceExpression, object data)
		{
			return CreateResult (resolver.CallingType.FullName);
		}
		
		
		IReturnType GetCommonType (IReturnType left, IReturnType right)
		{
			return left ?? right;
		}
		
	}
}
