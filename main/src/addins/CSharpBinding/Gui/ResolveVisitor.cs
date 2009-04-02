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
		
		public IReturnType GetTypeSafe (Expression expression)
		{
			ResolveResult result = Resolve (expression);
			return result.ResolvedType ?? DomReturnType.Void;
		}
		
		internal ResolveResult CreateResult (TypeReference typeReference)
		{
			return CreateResult (NRefactoryResolver.ConvertTypeReference (typeReference));
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
			result.UnresolvedType = type;
			if (unit != null && resolver.Dom != null && type != null && type.Type == null) {
				SearchTypeRequest req = new SearchTypeRequest (unit, type, resolver.CallingType);
				req.CallingType = resolver.CallingType;
				IType searchedType = resolver.Dom.SearchType (req);
				if (searchedType != null) {
					DomReturnType resType = new DomReturnType (searchedType);
					resType.ArrayDimensions = type.ArrayDimensions;
					for (int i = 0; i < type.ArrayDimensions; i++) {
						resType.SetDimension (i, type.GetDimension (i));
					}
					resType.PointerNestingLevel = type.PointerNestingLevel;
					result.ResolvedType = resType;
				}
			}
			return result;
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
//			Console.WriteLine ("visit id: " + identifierExpression.Identifier);
			var res = resolver.ResolveIdentifier (this, identifierExpression.Identifier.TrimEnd ('.'));
//			Console.WriteLine ("result: " + res.ResolvedType);
			return res;
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
		
		public override object VisitCollectionInitializerExpression (CollectionInitializerExpression collectionInitializerExpression, object data)
		{
			if (collectionInitializerExpression.CreateExpressions.Count == 0)
				return null;
			DomReturnType type = (DomReturnType)ResolveType (collectionInitializerExpression.CreateExpressions[0]);
			type.ArrayDimensions++;
			return CreateResult (type);
		}
		
		public override object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			if (arrayCreateExpression.IsImplicitlyTyped) {
//				Console.WriteLine (arrayCreateExpression.ArrayInitializer);
				return Resolve (arrayCreateExpression.ArrayInitializer);
			}
			return CreateResult (arrayCreateExpression.CreateType);
		}
		
		public override object VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data) 
		{
			return Resolve (assignmentExpression.Left);
		}
		static string GetOperatorName (UnaryOperatorType type)
		{
			switch (type) {
			case UnaryOperatorType.Not:
				return "op_LogicalNot";
			case UnaryOperatorType.BitNot:
				return "op_OnesComplement";
			case UnaryOperatorType.Minus:
				return "op_UnaryNegation";
			case UnaryOperatorType.Plus:
				return "op_UnaryPlus";
			}
			return null;
		}
		
		public override object VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			string name = GetOperatorName (unaryOperatorExpression.Op);
			if (!String.IsNullOrEmpty (name)) {
				IReturnType returnType = GetTypeSafe (unaryOperatorExpression.Expression);
				IType type  = returnType != null ? this.resolver.Dom.GetType (returnType) : null;
				if (type != null) {
					int level;
					IMethod op = FindOperator (type, name, out level);
					if (op != null) {
						return CreateResult (op.ReturnType);
					}
				}
			
			}
			return Resolve (unaryOperatorExpression.Expression);
		}
		
		public override object VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			if (indexerExpression.Indexes == null || indexerExpression.Indexes.Count == 0)
				return null;
			ResolveResult result = Resolve (indexerExpression.TargetObject);
			
			if (result.ResolvedType != null && result.ResolvedType.ArrayDimensions > 0)
				return CreateResult (result.ResolvedType.FullName);
			IType resolvedType = resolver.Dom.GetType (result.ResolvedType);
			if (resolvedType != null) {
				foreach (IType curType in resolver.Dom.GetInheritanceTree (resolvedType)) {
					foreach (IProperty property in curType.Properties) {
//						System.Console.WriteLine(property);
						if (property.IsIndexer)
							return CreateResult (property.ReturnType);
					}
				}
			}
			if (result.ResolvedType != null && result.ResolvedType.GenericArguments.Count > 0) {
				//System.Console.WriteLine("genArg:" + result.ResolvedType.GenericArguments[0]);
				return CreateResult (result.ResolvedType.GenericArguments[0]);
			}
			return result;
		}
		
		
		static string GetAnonymousTypeFieldName (Expression expr)
		{
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
			DomType result = new AnonymousType ();
			result.SourceProjectDom = resolver.Dom;
			foreach (Expression expr in initializer.CreateExpressions) {
				DomProperty newProperty = new DomProperty (GetAnonymousTypeFieldName (expr), MonoDevelop.Projects.Dom.Modifiers.Public, DomLocation.Empty, DomRegion.Empty, ResolveType(expr));
				newProperty.Modifiers = MonoDevelop.Projects.Dom.Modifiers.Public;
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
			return CreateResult (objectCreateExpression.CreateType);
		}

		static string GetOperatorName (BinaryOperatorType type)
		{
			switch (type) {
				case BinaryOperatorType.Add:
					return "op_Addition";
				case BinaryOperatorType.Subtract:
					return "op_Subtraction";
				case BinaryOperatorType.Multiply:
					return "op_Multiply";
				case BinaryOperatorType.Divide:
					return "op_Division";
				case BinaryOperatorType.Modulus:
					return "op_Modulus";
				
				case BinaryOperatorType.BitwiseAnd:
					return "op_BitwiseAnd";
				case BinaryOperatorType.BitwiseOr:
					return "op_BitwiseOr";
				case BinaryOperatorType.ExclusiveOr:
					return "op_ExclusiveOr";
				
				case BinaryOperatorType.ShiftLeft:
					return "op_LeftShift";
				case BinaryOperatorType.ShiftRight:
					return "op_RightShift";
				
				case BinaryOperatorType.Equality:
					return "op_Equality";
				case BinaryOperatorType.InEquality:
					return "op_Inequality";
					
				case BinaryOperatorType.LessThan:
					return "op_LessThan";
				case BinaryOperatorType.LessThanOrEqual:
				
					return "op_LessThanOrEqual";
				case BinaryOperatorType.GreaterThan:
					return "op_GreaterThan";
				case BinaryOperatorType.GreaterThanOrEqual:
					return "op_GreaterThanOrEqual";
			}
			return "";
		}
		
		IMethod FindOperator (IType type, string operatorName, out int level)
		{
			level = 0;
			foreach (IType curType in resolver.Dom.GetInheritanceTree (type)) {
				
				foreach (IMember member in curType.SearchMember (operatorName, true)) {
					IMethod method = (IMethod)member;
					
					if (method == null || !method.IsSpecialName)
						continue;
					return method;
				}
				level++;
			}
			return null;
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			IReturnType left  = GetTypeSafe (binaryOperatorExpression.Left);
			IReturnType right = GetTypeSafe (binaryOperatorExpression.Right);
			string opName = GetOperatorName (binaryOperatorExpression.Op);
			
			if (!String.IsNullOrEmpty (opName)) {
				IType leftType  = this.resolver.Dom.GetType (left);
				int leftOperatorLevel = 0;
				IMethod leftOperator = leftType != null ? FindOperator (leftType, opName, out leftOperatorLevel) : null;
				
				IType rightType = this.resolver.Dom.GetType (right);
				int rightOperatorLevel = 0;
				IMethod rightOperator = rightType != null ? FindOperator (rightType, opName, out rightOperatorLevel) : null;
				
				if (leftOperator != null && rightOperator != null) {
					if (leftOperatorLevel < rightOperatorLevel)
						return CreateResult (leftOperator.ReturnType);
					return CreateResult (rightOperator.ReturnType);
				}
				if (leftOperator != null)
					return CreateResult (leftOperator.ReturnType);
				if (rightOperator != null)
					return CreateResult (rightOperator.ReturnType);
			}
			
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
					return CreateResult (GetCommonType (left, 
					                                    right).FullName);
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
			result.UnresolvedType = result.ResolvedType  = DomReturnType.GetSharedReturnType (new DomReturnType (resolver.CallingType));
			return result;
		}
		
		public override object VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			if (resolver.CallingType == null || resolver.CallingType.FullName == "System.Object")
				return CreateResult (DomReturnType.Void);
			
			BaseResolveResult result = new BaseResolveResult ();
			result.CallingType   = resolver.CallingType;
			result.CallingMember = resolver.CallingMember;
			if (resolver.CallingType != null) {
				IType type = null;
				if (resolver.CallingType.BaseType != null) 
					type = this.resolver.Dom.SearchType (new SearchTypeRequest (resolver.Unit, resolver.CallingType.BaseType, resolver.CallingType));
				result.UnresolvedType = result.ResolvedType  = type != null ? new DomReturnType (type) : DomReturnType.Object;
			}
			return result;
		}
		
		public override object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			string[] types = typeReferenceExpression.TypeReference.Type.Split ('.');
			if (types == null || types.Length == 0)
				return null;
			if (types.Length == 1) {
				ResolveResult result = resolver.ResolveIdentifier (this, typeReferenceExpression.TypeReference.Type);
				if (result == null) 
					result = CreateResult (typeReferenceExpression.TypeReference);
				result.StaticResolve = true;
				return result;
			}
			Expression expr = new IdentifierExpression (types[0]);
			for (int i = 1; i < types.Length; i++) {
				if (types[i] != "?")
					expr = new MemberReferenceExpression (expr, types[i]);
			}
			
			return expr.AcceptVisitor (this, data);
		}
		
		public override object VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			if (memberReferenceExpression == null) {
				return null;
			}
			ResolveResult result;
			if (String.IsNullOrEmpty (memberReferenceExpression.MemberName)) {
				if (memberReferenceExpression.TargetObject is TypeReferenceExpression) {
					result = CreateResult (((TypeReferenceExpression)memberReferenceExpression.TargetObject).TypeReference);
					result.StaticResolve = true;
					return result;
				}
//				if (memberReferenceExpression.TargetObject is ThisReferenceExpression) {
//					result = CreateResult (((TypeReferenceExpression)memberReferenceExpression.TargetObject).TypeReference);
//					result.StaticResolve = true;
//					return result;
//				}

//				return memberReferenceExpression.TargetObject.AcceptVisitor(this, data);
			}
			result = memberReferenceExpression.TargetObject.AcceptVisitor(this, data) as ResolveResult;
			
			NamespaceResolveResult namespaceResult = result as NamespaceResolveResult;
			if (namespaceResult != null) {
				if (String.IsNullOrEmpty (memberReferenceExpression.MemberName))
					return namespaceResult;
				string fullName = namespaceResult.Namespace + "." + memberReferenceExpression.MemberName;
				if (resolver.Dom.NamespaceExists (fullName, true))
					return new NamespaceResolveResult (fullName);
				IType type = resolver.Dom.GetType (fullName);
				if (type != null) {
					result = CreateResult (this.resolver.Unit, new DomReturnType (type));
					result.StaticResolve = true;
					return result;
				}
				return null;
			}
			
			if (result != null && result.ResolvedType != null) {
				IType type = resolver.Dom.GetType (result.ResolvedType);
				if (type != null) {
					List<IMember> member = new List <IMember> ();
					List<IType> accessibleExtTypes = DomType.GetAccessibleExtensionTypes (resolver.Dom, resolver.Unit);
					// Inheritance of extension methods is handled in DomType
					foreach (IMethod method in type.GetExtensionMethods (accessibleExtTypes)) {
						if (method.Name == memberReferenceExpression.MemberName) 
							member.Add (method);
					}
					foreach (IType curType in resolver.Dom.GetInheritanceTree (type)) {
						member.AddRange (curType.SearchMember (memberReferenceExpression.MemberName, true));
					}
					
					if (member.Count > 0) {
						if (member[0] is IMethod) {
							bool isStatic = result.StaticResolve;
							bool includeProtected = true;
							for (int i = 0; i < member.Count; i++) {
								IMethod method = member[i] as IMethod;
								if (method != null && !method.IsFinalizer && method.IsExtension && method.IsAccessibleFrom (resolver.Dom, type, resolver.CallingMember, true))
									continue;
								if ((member[i].IsStatic ^ isStatic) || !member[i].IsAccessibleFrom (resolver.Dom, type, resolver.CallingMember, includeProtected) || (method != null && method.IsFinalizer)) {
									member.RemoveAt (i);
									i--;
								}
							}
							if (member.Count == 0)
								return null;
							result = new MethodResolveResult (member);
							((MethodResolveResult)result).Type = type;
							result.CallingType   = resolver.CallingType;
							result.CallingMember = resolver.CallingMember;
							//result.StaticResolve = isStatic;
							//result.UnresolvedType = result.ResolvedType  = member[0].ReturnType;
							foreach (TypeReference typeReference in memberReferenceExpression.TypeArguments) {
								((MethodResolveResult)result).AddGenericArgument (new DomReturnType (String.IsNullOrEmpty (typeReference.SystemType) ? typeReference.Type : typeReference.SystemType));
							}
							//System.Console.WriteLine(result + "/" + result.ResolvedType);
							return result;
						}
						if (member[0] is IType) {
							result = CreateResult (member[0].FullName);
							result.StaticResolve = true;
						} else {
							IType searchType = resolver.Dom.GetType (member[0].ReturnType);
							result = CreateResult (member[0].DeclaringType.CompilationUnit, searchType != null ? new DomReturnType (searchType) : DomReturnType.Void);
							((MemberResolveResult)result).ResolvedMember = member[0];
						}
						return result;
					}
				} else {
					MonoDevelop.Core.LoggingService.LogWarning ("Couldn't resolve type " + result.ResolvedType);
				}
			}
			
			return result;
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			if (invocationExpression == null) 
				return null;
			// add support for undocumented __makeref and __reftype keywords
			if (invocationExpression.TargetObject is IdentifierExpression) {
				IdentifierExpression idExpr = invocationExpression.TargetObject as IdentifierExpression;
				if (idExpr.Identifier == "__makeref") 
					return CreateResult ("System.TypedReference");
				if (idExpr.Identifier == "__reftype") 
					return CreateResult ("System.Type");
			}
			
			ResolveResult targetResult = Resolve (invocationExpression.TargetObject);
			//System.Console.WriteLine("target:" + targetResult);
			MethodResolveResult methodResult = targetResult as MethodResolveResult;
			if (methodResult != null) {
				//Console.WriteLine ("--------------------");
				//Console.WriteLine ("i:" + methodResult.ResolvedType);
				foreach (Expression arg in invocationExpression.Arguments) {
					var type = GetTypeSafe (arg);
//					Console.WriteLine ("  arg:" + arg);
//					Console.WriteLine ("type :" + type);
					methodResult.AddArgument (type);
				}
				//Console.WriteLine ("--------------------");
				methodResult.ResolveExtensionMethods ();
//				Console.WriteLine ("i2:" + methodResult.ResolvedType);
			/*	MemberReferenceExpression mre = invocationExpression.TargetObject as MemberReferenceExpression;
				if (mre != null) {
					foreach (TypeReference typeReference in mre.TypeArguments) {
						methodResult.AddGenericArgument (new DomReturnType (String.IsNullOrEmpty (typeReference.SystemType) ? typeReference.Type : typeReference.SystemType));
					}
				}*/
//				return CreateResult (methodResult.Methods [0].ReturnType);
			}
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
		
		QueryExpression queryExpression = null;
		public override object VisitQueryExpression(QueryExpression queryExpression, object data)
		{
			if (this.queryExpression != null) // prevent endloss loop: var n = from n select n; n.$ (doesn't make sense, but you can type this)
				return null;
			this.queryExpression = queryExpression;
			IReturnType type = null;
			QueryExpressionSelectClause selectClause = queryExpression.SelectOrGroupClause as QueryExpressionSelectClause;
			if (selectClause != null) {
				InvocationExpression selectInvocation = new InvocationExpression (new MemberReferenceExpression (queryExpression.FromClause.InExpression, "Select"));
				LambdaExpression selectLambdaExpr = new LambdaExpression ();
				selectLambdaExpr.Parent = selectInvocation;
				selectLambdaExpr.Parameters.Add (new ParameterDeclarationExpression (null, "par"));
				selectLambdaExpr.ExpressionBody = selectClause.Projection;
				selectInvocation.Arguments.Add (selectLambdaExpr);
				return CreateResult (ResolveType (selectInvocation));
			}
			
			QueryExpressionGroupClause groupClause = queryExpression.SelectOrGroupClause as QueryExpressionGroupClause;
			if (groupClause != null) {
				InvocationExpression groupInvocation = new InvocationExpression (new MemberReferenceExpression (queryExpression.FromClause.InExpression, "GroupBy"));
				
				LambdaExpression keyLambdaExpr = new LambdaExpression ();
				keyLambdaExpr.Parent = groupInvocation;
				keyLambdaExpr.Parameters.Add (new ParameterDeclarationExpression (null, "par"));
				keyLambdaExpr.ExpressionBody = groupClause.GroupBy;
				groupInvocation.Arguments.Add (keyLambdaExpr);
				
				LambdaExpression elementLambdaExpr = new LambdaExpression ();
				elementLambdaExpr.Parent = groupInvocation;
				elementLambdaExpr.Parameters.Add (new ParameterDeclarationExpression (null, "par"));
				elementLambdaExpr.ExpressionBody = groupClause.Projection;
				groupInvocation.Arguments.Add (elementLambdaExpr);
				return CreateResult (ResolveType (groupInvocation));
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
