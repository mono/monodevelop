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
	public class ResolveVisitor : AbstractAstVisitor
	{
		NRefactoryResolver resolver;
		
		public ResolveVisitor (NRefactoryResolver resolver)
		{
			this.resolver = resolver;
		}
		
		public ResolveResult Resolve (Expression expression)
		{
			ResolveResult result = expression.AcceptVisitor (this, null) as ResolveResult;
			if (result == null)
				result = CreateResult ("");
			return result;
		}
		
		IReturnType GetTypeSafe (Expression expression)
		{
			ResolveResult result = Resolve (expression);
			return result.ResolvedType ?? DomReturnType.Void;
		}
		
		internal ResolveResult CreateResult (TypeReference typeReference)
		{
			return CreateResult (String.IsNullOrEmpty (typeReference.SystemType) ? typeReference.Type : typeReference.SystemType);
		}
		
		internal ResolveResult CreateResult (string fullTypeName)
		{
			return CreateResult (new DomReturnType (fullTypeName));
		}
		
		internal ResolveResult CreateResult (IReturnType type)
		{
			return CreateResult (resolver.Unit, type);
		}
		
		ResolveResult CreateResult (ICompilationUnit unit, IReturnType type)
		{
			MemberResolveResult result = new MemberResolveResult (null);
			result.CallingType   = resolver.CallingType;
			result.CallingMember = resolver.CallingMember;
			result.ResolvedType = type;
			System.Console.WriteLine("create result in:" + unit);
			if (unit != null) {
				SearchTypeResult searchedTypeResult = resolver.Dom.SearchType (new SearchTypeRequest (unit, -1, -1, type.FullName));
				System.Console.WriteLine("Search result:" + searchedTypeResult);
				if (searchedTypeResult != null) {
					result.ResolvedType.Name      = searchedTypeResult.Result.Name;
					result.ResolvedType.Namespace = searchedTypeResult.Result.Namespace;
				}
			}
			return result;
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			return resolver.ResolveIdentifier (this, identifierExpression.Identifier.TrimEnd ('.'));
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
		
		public override object VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			return Resolve (unaryOperatorExpression.Expression);
		}
		
		public override object VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			return Resolve (indexerExpression.TargetObject);
		}
		
		
		static string GetAnonymousTypeFieldName (Expression expr)
		{
			System.Console.WriteLine(expr);
			if (expr is MemberReferenceExpression) 
				return ((MemberReferenceExpression)expr).MemberName;
			if (expr is NamedArgumentExpression) 
				return ((NamedArgumentExpression)expr).Name;
			if (expr is IdentifierExpression) 
				return ((IdentifierExpression)expr).Identifier;
			return "?";
		}
		
		IType CreateAnonymousClass (CollectionInitializerExpression initializer)
		{
			DomType result = new DomType ("AnonymousType");
			foreach (Expression expr in initializer.CreateExpressions) {
				DomProperty newProperty = new DomProperty (GetAnonymousTypeFieldName (expr), MonoDevelop.Projects.Dom.Modifiers.Public, DomLocation.Empty, DomRegion.Empty, ResolveType(expr));
				newProperty.DeclaringType = result;
				result.Add (newProperty);
			}
			return result;
		}
		public override object VisitNamedArgumentExpression (NamedArgumentExpression expr, object data)
		{
			return expr.Expression.AcceptVisitor (this, data);
		}
		
		public override object VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression, object data)
		{
			if (objectCreateExpression.IsAnonymousType) {
				ResolveResult result =  new AnonymousTypeResolveResult (CreateAnonymousClass (objectCreateExpression.ObjectInitializer));
				result.CallingType   = resolver.CallingType;
				result.CallingMember = resolver.CallingMember;
				return result;
			}
			return null;
		/*else {
				IReturnType rt = TypeVisitor.CreateReturnType(objectCreateExpression.CreateType, resolver);
				if (rt == null)
					return new UnknownConstructorCallResolveResult(resolver.CallingClass, resolver.CallingMember, objectCreateExpression.CreateType.ToString());
				
				return ResolveConstructorOverload(rt, objectCreateExpression.Parameters)
					?? CreateResolveResult(rt);
			}*/
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
			result.ResolvedType  = DomReturnType.GetSharedReturnType (new DomReturnType (resolver.CallingType));
			return result;
		}
		
		public override object VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			if (resolver.CallingType == null || resolver.CallingType.FullName == "System.Object")
				return CreateResult (DomReturnType.Void);
			
			BaseResolveResult result = new BaseResolveResult ();
			result.CallingType   = resolver.CallingType;
			result.CallingMember = resolver.CallingMember;
			if (resolver.CallingType != null)
				result.ResolvedType  = resolver.CallingType.BaseType;
			return result;
		}
		
		public override object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return CreateResult (typeReferenceExpression.TypeReference);
		}
		
		public override object VisitMemberReferenceExpression(MemberReferenceExpression fieldReferenceExpression, object data)
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
//				if (fieldReferenceExpression.TargetObject is ThisReferenceExpression) {
//					result = CreateResult (((TypeReferenceExpression)fieldReferenceExpression.TargetObject).TypeReference);
//					result.StaticResolve = true;
//					return result;
//				}

//				return fieldReferenceExpression.TargetObject.AcceptVisitor(this, data);
			}
			result = fieldReferenceExpression.TargetObject.AcceptVisitor(this, data) as ResolveResult;
			NamespaceResolveResult namespaceResult = result as NamespaceResolveResult;
			if (namespaceResult != null) {
				if (String.IsNullOrEmpty (fieldReferenceExpression.FieldName))
					return namespaceResult;
				string fullName = namespaceResult.Namespace + "." + fieldReferenceExpression.FieldName;
				if (resolver.Dom.NamespaceExists (fullName, true))
					return new NamespaceResolveResult (fullName);
				
				IType type = resolver.Dom.GetType (fullName, -1, true, true);
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
					foreach (IType curType in resolver.Dom.GetInheritanceTree (type)) {
						List <IMember> member = curType.SearchMember (fieldReferenceExpression.FieldName, true);
						if (member != null && member.Count > 0) {
							if (member[0] is IMethod) {
								result = new MethodResolveResult (member);
								result.CallingType   = resolver.CallingType;
								result.CallingMember = resolver.CallingMember;
								return result;
							}
							if (member[0] is IType) {
								result = CreateResult (member[0].FullName);
								result.StaticResolve = true;
							} else {
								result = CreateResult (curType.CompilationUnit, member[0].ReturnType);
								((MemberResolveResult)result).ResolvedMember = member[0];
							}
							return result;
						}
					}
				}
			}
			
			return result;
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			if (invocationExpression == null) 
				return null;
			
			ResolveResult targetResult = Resolve (invocationExpression.TargetObject);
			
			return targetResult;
		}
		
		public override object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			if (primitiveExpression.Value == null) 
				return CreateResult ("");
			Type type = primitiveExpression.Value.GetType();
			return CreateResult (type.FullName);
		}
		
		public override object VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
		{
			return resolver.ResolveLambda (this, lambdaExpression);
		}
		
		public IReturnType ResolveType (Expression expr)
		{
			ResolveResult res = Resolve (expr);
			if (res is AnonymousTypeResolveResult)
				return new DomReturnType (((AnonymousTypeResolveResult)res).AnonymousType);
			if (res != null)
				return res.ResolvedType;
			return null;
		}
		
		public override object VisitQueryExpression(QueryExpression queryExpression, object data)
		{
			IReturnType type = null;
			System.Console.WriteLine("----------");
			System.Console.WriteLine(Environment.StackTrace);
			QueryExpressionSelectClause selectClause = queryExpression.SelectOrGroupClause as QueryExpressionSelectClause;
			if (selectClause != null) {
				type = ResolveType (selectClause.Projection);
			}
			if (type == null) {
				QueryExpressionGroupClause groupClause = queryExpression.SelectOrGroupClause as QueryExpressionGroupClause;
				if (groupClause != null)
					type = new DomReturnType ("System.Linq.IGrouping", false,  new List<IReturnType> (new IReturnType[] { ResolveType(groupClause.GroupBy), ResolveType(groupClause.Projection) }));
			}
			if (type != null) 
				return CreateResult (new DomReturnType("System.Collections.Generic.IEnumerable", false, new List<IReturnType> (new IReturnType[] { type })));
			
			return null;
		}
		
		
		IReturnType GetCommonType (IReturnType left, IReturnType right)
		{
			return left ?? right;
		}
		
	}
}
