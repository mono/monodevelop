//
// ResolveVisitor.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;

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
			return CreateResult (String.IsNullOrEmpty (typeReference.SystemType) ? typeReference.Type : typeReference.SystemType);
		}
		
		ResolveResult CreateResult (string fullTypeName)
		{
			MemberResolveResult result = new MemberResolveResult ();
			result.CallingType   = resolver.CallingType;
			result.CallingMember = resolver.CallingMember;
			if (!String.IsNullOrEmpty (fullTypeName))
				result.ResolvedType  = new DomReturnType (fullTypeName);
			return result;
		}
		
		ResolveResult CreateResult (IReturnType type)
		{
			MemberResolveResult result = new MemberResolveResult ();
			result.CallingType   = resolver.CallingType;
			result.CallingMember = resolver.CallingMember;
			result.ResolvedType  = type;
			return result;
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			return resolver.ResolveIdentifier (identifierExpression.Identifier.TrimEnd ('.'));
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
			if (resolver.CallingType == null)
				return CreateResult (DomReturnType.Void);
				
			ThisResolveResult result = new ThisResolveResult ();
			result.CallingType   = resolver.CallingType;
			result.CallingMember = resolver.CallingMember;
				
			return result;
		}
		
		public override object VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			if (resolver.CallingType == null || resolver.CallingType.FullName == "System.Object")
				return CreateResult (DomReturnType.Void);
			
			BaseResolveResult result = new BaseResolveResult ();
			result.CallingType   = resolver.CallingType;
			result.CallingMember = resolver.CallingMember;
			return result;
		}
		
		public override object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return CreateResult (typeReferenceExpression.TypeReference);
		}
		
		public override object VisitFieldReferenceExpression(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			if (fieldReferenceExpression == null) {
				return null;
			}
			ResolveResult result;
			if (String.IsNullOrEmpty (fieldReferenceExpression.FieldName)) {
				if (fieldReferenceExpression.TargetObject is TypeReferenceExpression) {
					result = CreateResult (((TypeReferenceExpression)fieldReferenceExpression.TargetObject).TypeReference);
					result.StaticResolve = true;
					return result;
				}
				return fieldReferenceExpression.TargetObject.AcceptVisitor(this, data);
			}
			result = fieldReferenceExpression.TargetObject.AcceptVisitor(this, data) as ResolveResult;
			NamespaceResolveResult namespaceResult = result as NamespaceResolveResult;
			if (namespaceResult != null) {
				string fullName = namespaceResult.Namespace + "." + fieldReferenceExpression.FieldName;
				if (resolver.Dom.NamespaceExists (fullName))
					return new NamespaceResolveResult (fullName);
				
				IType type = resolver.Dom.GetType (fullName, -1, true);
				if (type != null) {
					result = CreateResult (fullName);
					result.StaticResolve = true;
					return result;
				}
				return null;
			}
			
			if (result != null && result.ResolvedType != null) {
				IType type = resolver.Dom.GetType (result.ResolvedType);
				if (type != null) {
					List <IMember> member = type.SearchMember (fieldReferenceExpression.FieldName, true);
					if (member != null && member.Count > 0) {
						if (member[0] is IMethod) {
							result = new MethodResolveResult (member);
							result.CallingType   = resolver.CallingType;
							result.CallingMember = resolver.CallingMember;
							return result;
						}
						result = CreateResult (member[0].ReturnType);
						return result;
					}
				}
			}
			
			return result;
		}
		
		IReturnType GetCommonType (IReturnType left, IReturnType right)
		{
			return left ?? right;
		}
		
	}
}
