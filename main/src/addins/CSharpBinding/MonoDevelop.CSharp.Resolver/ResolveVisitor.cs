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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;
using MonoDevelop.Refactoring;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Projects.Dom.Output;

namespace MonoDevelop.CSharp.Resolver
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
				IType searchedType = resolver.SearchType (type);
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
			if (identifierExpression.TypeArguments != null && identifierExpression.TypeArguments.Count > 0) {
				if (resolver.CallingType != null) {
					foreach (var type in resolver.Dom.GetInheritanceTree (resolver.CallingType)) {
						IMethod possibleMethod = type.Methods.Where (m => m.Name == identifierExpression.Identifier && m.TypeParameters.Count == identifierExpression.TypeArguments.Count && m.IsAccessibleFrom (resolver.Dom, resolver.CallingType, resolver.CallingMember, true)).FirstOrDefault ();
						if (possibleMethod != null) {
							MethodResolveResult methodResolveResult = new MethodResolveResult (possibleMethod);
							methodResolveResult.CallingType   = resolver.CallingType;
							methodResolveResult.CallingMember = resolver.CallingMember;
							
							identifierExpression.TypeArguments.ForEach (arg => methodResolveResult.AddGenericArgument (resolver.ResolveType (arg.ConvertToReturnType ())));
							methodResolveResult.ResolveExtensionMethods ();
							return methodResolveResult;
						}
					}
				}
				TypeReference reference = new TypeReference (identifierExpression.Identifier);
				reference.GenericTypes.AddRange (identifierExpression.TypeArguments);
				ResolveResult result = CreateResult (reference);
				result.StaticResolve = true;
				return result;
			}
//			Console.WriteLine ("visit id: " + identifierExpression.Identifier);
			var res = resolver.ResolveIdentifier (this, identifierExpression.Identifier.TrimEnd ('.'));
//			Console.WriteLine ("result: " + res);
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
			DomReturnType type = null;
			IType typeObject = null;
			
			for (int i = 0; i < collectionInitializerExpression.CreateExpressions.Count; i++) {
				DomReturnType curType = (DomReturnType)ResolveType (collectionInitializerExpression.CreateExpressions[i]);
				// if we found object or we have only one create expression we can stop
				if (curType.DecoratedFullName == DomReturnType.Object.DecoratedFullName || collectionInitializerExpression.CreateExpressions.Count == 1) {
					type = curType;
					break;
				}
				IType curTypeObject = resolver.Dom.GetType (curType);
				if (curTypeObject == null)
					continue;
				if (type == null || resolver.Dom.GetInheritanceTree (typeObject).Any (t => t.DecoratedFullName == curTypeObject.DecoratedFullName)) {
					type = curType;
					typeObject = curTypeObject;
				}
			}
			
			if (type != null)
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
		
		public override object VisitDefaultValueExpression (DefaultValueExpression defaultValueExpression, object data)
		{
			return CreateResult (defaultValueExpression.TypeReference);
		}
		
		public override object VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			if (indexerExpression.Indexes == null || indexerExpression.Indexes.Count == 0)
				return null;
			ResolveResult result = Resolve (indexerExpression.TargetObject);
			
			if (result.ResolvedType != null && result.ResolvedType.ArrayDimensions > 0) {
				((DomReturnType)result.ResolvedType).ArrayDimensions--;
				return CreateResult (result.ResolvedType);
			}
			
			IType resolvedType = resolver.Dom.GetType (result.ResolvedType);
			if (resolvedType != null) {
				foreach (IType curType in resolver.Dom.GetInheritanceTree (resolvedType)) {
					foreach (IProperty property in curType.Properties) {
						if (property.IsExplicitDeclaration)
							continue;
						if (property.IsIndexer)
							return CreateResult (property.ReturnType);
					}
				}
			}
			if (result.ResolvedType != null && result.ResolvedType.GenericArguments.Count > 0) {
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
		
		Dictionary<CollectionInitializerExpression, DomType> anonymousTypes = new Dictionary<CollectionInitializerExpression, DomType> ();
		IType CreateAnonymousClass (CollectionInitializerExpression initializer)
		{
			DomType result;
			if (!anonymousTypes.TryGetValue (initializer, out result)) {
				result = new AnonymousType ();
				result.SourceProjectDom = resolver.Dom;
				foreach (Expression expr in initializer.CreateExpressions) {
					var oldPos = resolver.ResolvePosition;
					if (!expr.StartLocation.IsEmpty)
						resolver.resolvePosition = new DomLocation (expr.StartLocation.Line + resolver.CallingMember.Location.Line, expr.StartLocation.Column);
					DomProperty newProperty = new DomProperty (GetAnonymousTypeFieldName (expr), MonoDevelop.Projects.Dom.Modifiers.Public, DomLocation.Empty, DomRegion.Empty, ResolveType (expr));
					newProperty.DeclaringType = result;
					result.Add (newProperty);
					resolver.resolvePosition = oldPos;
				}
				anonymousTypes[initializer] = result;
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
					type = this.resolver.SearchType (resolver.CallingType.BaseType);
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
		
		public override object VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression, object data)
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
			result = memberReferenceExpression.TargetObject.AcceptVisitor (this, data) as ResolveResult;
			
			NamespaceResolveResult namespaceResult = result as NamespaceResolveResult;
			if (namespaceResult != null) {
				if (String.IsNullOrEmpty (memberReferenceExpression.MemberName))
					return namespaceResult;
				string fullName = namespaceResult.Namespace + "." + memberReferenceExpression.MemberName;
				if (resolver.Dom.NamespaceExists (fullName, true))
					return new NamespaceResolveResult (fullName);
				DomReturnType searchType = new DomReturnType (fullName);
				if (memberReferenceExpression.TypeArguments != null) {
					foreach (TypeReference typeRef in memberReferenceExpression.TypeArguments) {
						searchType.AddTypeParameter (NRefactoryResolver.ConvertTypeReference (typeRef));
					}
				}
				IType type = resolver.Dom.GetType (searchType);
				if (type != null) {
					result = CreateResult (this.resolver.Unit, new DomReturnType (type));
					result.StaticResolve = true;
					return result;
				}
				return null;
			}
			if (result != null && result.ResolvedType != null) {
				foreach (ResolveResult innerResult in result.ResolveResults) {
					ResolveResult resolvedResult = ResolveMemberReference (innerResult, memberReferenceExpression);
					if (resolvedResult != null)
						return resolvedResult;
				}
			} else {
				if (result != null)
					MonoDevelop.Core.LoggingService.LogWarning ("Couldn't resolve type " + result);
			}
			
			return null;
		}
		static MonoDevelop.CSharp.Dom.CSharpAmbience ambience = new MonoDevelop.CSharp.Dom.CSharpAmbience ();
		ResolveResult ResolveMemberReference (ResolveResult result, MemberReferenceExpression memberReferenceExpression)
		{
			IType type = resolver.Dom.GetType (result.ResolvedType);
			if (type == null) 
				return null;
			//Console.WriteLine ("Resolve member: " + memberReferenceExpression.MemberName + " on " + type);
			
			List<IMember> member = new List<IMember> ();
			List<IType> accessibleExtTypes = DomType.GetAccessibleExtensionTypes (resolver.Dom, resolver.Unit);
			// Inheritance of extension methods is handled in DomType
			foreach (IMethod method in type.GetExtensionMethods (accessibleExtTypes)) {
				if (method.Name == memberReferenceExpression.MemberName) {
					member.Add (method);
				}
			}
			bool includeProtected = true;
			foreach (IType curType in resolver.Dom.GetInheritanceTree (type)) {
				if (curType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface && type.ClassType != MonoDevelop.Projects.Dom.ClassType.Interface)
					continue;
				if (curType.IsAccessibleFrom (resolver.Dom, resolver.CallingType, resolver.CallingMember, includeProtected)) {
					foreach (IMember foundMember in curType.SearchMember (memberReferenceExpression.MemberName, true)) {
						if (foundMember.IsExplicitDeclaration)
							continue;
						if (result is BaseResolveResult && foundMember.IsAbstract)
							continue;
						member.Add (foundMember);
					}
				} 
			}
			if (member.Count > 0) {
				if (member[0] is IMethod) {
					bool isStatic = result.StaticResolve;
					List<IMember> nonMethodMembers = new List<IMember> ();
					List<string> errors = new List<string> ();
					int typeParameterCount = 0;
					if (memberReferenceExpression.TypeArguments != null)
						typeParameterCount = memberReferenceExpression.TypeArguments.Count;
					
					for (int i = 0; i < member.Count; i++) {
						IMethod method = member[i] as IMethod;
						if (method == null)
							nonMethodMembers.Add (member[i]);
						
						if (!member[i].IsAccessibleFrom (resolver.Dom, type, resolver.CallingMember, includeProtected))
							errors.Add (
								MonoDevelop.Core.GettextCatalog.GetString ("'{0}' is inaccessible due to its protection level.",
								ambience.GetString (method, OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics)));
						
						if (method != null && !method.IsFinalizer && (method.IsExtension || method.WasExtended)/* && method.IsAccessibleFrom (resolver.Dom, type, resolver.CallingMember, true)*/) {
							continue;
						}
						if ((member[i].IsStatic ^ isStatic) || 
/*						    !member[i].IsAccessibleFrom (resolver.Dom, type, resolver.CallingMember, includeProtected) || */
						    (method != null && (method.IsFinalizer || typeParameterCount > 0 && method.TypeParameters.Count != typeParameterCount))) {
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
					result.ResolveErrors.AddRange (errors);
					//result.StaticResolve = isStatic;
					//result.UnresolvedType = result.ResolvedType  = member[0].ReturnType;
					foreach (TypeReference typeReference in memberReferenceExpression.TypeArguments) {
						((MethodResolveResult)result).AddGenericArgument (resolver.ResolveType (typeReference.ConvertToReturnType ()));
					}
					((MethodResolveResult)result).ResolveExtensionMethods ();
					if (nonMethodMembers.Count > 0) {
						MemberResolveResult baseResult = (MemberResolveResult) CreateResult (nonMethodMembers[0].DeclaringType.CompilationUnit, nonMethodMembers[0].ReturnType);
						baseResult.ResolvedMember = nonMethodMembers[0];
						return new CombinedMethodResolveResult (baseResult, (MethodResolveResult)result);
					}
					//System.Console.WriteLine(result + "/" + result.ResolvedType);
					return result;
				}
				
				if (member[0] is IType) {
					result = CreateResult (member[0].FullName);
					result.StaticResolve = true;
				} else {
					result = CreateResult (member[0].DeclaringType.CompilationUnit, member[0].ReturnType);
					((MemberResolveResult)result).ResolvedMember = member[0];
				}
				if (!member[0].IsAccessibleFrom (resolver.Dom, type, resolver.CallingMember, includeProtected))
					result.ResolveErrors.Add (string.Format (MonoDevelop.Core.GettextCatalog.GetString ("'{0}' is inaccessible due to it's protection level."), ambience.GetString (member[0], OutputFlags.IncludeParameters | OutputFlags.IncludeGenerics)));
			
				return result;
			}
			return new UnresolvedMemberResolveResult (result, memberReferenceExpression.MemberName) {
				CallingType   = resolver.CallingType,
				CallingMember = resolver.CallingMember
			};
		}
		
		Dictionary<InvocationExpression, ResolveResult> invocationDictionary = new Dictionary<InvocationExpression, ResolveResult> ();
		public void ResetVisitor ()
		{
			invocationDictionary.Clear ();
			lambdaDictionary.Clear ();
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			if (invocationExpression == null) 
				return null;
			
			if (invocationDictionary.ContainsKey (invocationExpression))
				return invocationDictionary[invocationExpression];
			
			// add support for undocumented __makeref and __reftype keywords
			if (invocationExpression.TargetObject is IdentifierExpression) {
				IdentifierExpression idExpr = invocationExpression.TargetObject as IdentifierExpression;
				if (idExpr.Identifier == "__makeref") 
					return CreateResult ("System.TypedReference");
				if (idExpr.Identifier == "__reftype") 
					return CreateResult ("System.Type");
			}
			
			ResolveResult targetResult = Resolve (invocationExpression.TargetObject);
			
			if (targetResult is CombinedMethodResolveResult)
				targetResult = ((CombinedMethodResolveResult)targetResult).MethodResolveResult;
			
			targetResult.StaticResolve = false; // invocation result is never static
			if (this.resolver.CallingType != null) {
				if (targetResult is ThisResolveResult) {
					targetResult = new MethodResolveResult (this.resolver.CallingType.Methods.Where (method => method.IsConstructor));
					((MethodResolveResult)targetResult).Type = this.resolver.CallingType;
					targetResult.CallingType   = resolver.CallingType;
					targetResult.CallingMember = resolver.CallingMember;
				} else if (targetResult is BaseResolveResult) {
					System.Collections.IEnumerable baseConstructors = null;
					IType firstBaseType = null;
					foreach (IReturnType bT in this.resolver.CallingType.BaseTypes) {
						IType resolvedBaseType = resolver.SearchType (bT);
						if (firstBaseType == null && resolvedBaseType.ClassType != MonoDevelop.Projects.Dom.ClassType.Interface)
							firstBaseType = resolvedBaseType;
						foreach (IType baseType in resolver.Dom.GetInheritanceTree (resolvedBaseType)) {
							if (baseType.ClassType == MonoDevelop.Projects.Dom.ClassType.Interface)
								break;
							baseConstructors = baseType.Methods.Where (method => method.IsConstructor);
							goto bailOut;
						}
					}
				bailOut:
					if (baseConstructors == null) {
						if (firstBaseType != null) {
							// if there is a real base type without a .ctor a default .ctor for this type is generated.
							DomMethod constructedConstructor;
							constructedConstructor = new DomMethod ();
							constructedConstructor.Name = ".ctor";
							constructedConstructor.MethodModifier = MethodModifier.IsConstructor;
							constructedConstructor.DeclaringType = firstBaseType;
							constructedConstructor.Modifiers = MonoDevelop.Projects.Dom.Modifiers.Public;
							baseConstructors = new IMethod[] {
								constructedConstructor
							};
						} else {
							baseConstructors = resolver.SearchType (DomReturnType.Object).SearchMember (".ctor", true);
						}
						
					}
					targetResult = new MethodResolveResult (baseConstructors);
					((MethodResolveResult)targetResult).Type = this.resolver.CallingType;
					targetResult.CallingType   = resolver.CallingType;
					targetResult.CallingMember = resolver.CallingMember;
				}
			}
			
			MethodResolveResult methodResult = targetResult as MethodResolveResult;
			if (methodResult != null) {
				methodResult.GetsInvoked = true;
//				Console.WriteLine ("--------------------");
//				Console.WriteLine ("i:" + methodResult.ResolvedType);
/*				foreach (var arg in methodResult.GenericArguments) {
					methodResult.AddGenericArgument (arg);
				}*/
				foreach (Expression arg in invocationExpression.Arguments) {
					var type = GetTypeSafe (arg);
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
			invocationDictionary[invocationExpression] = targetResult;
			return targetResult;
		}
		
		public override object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			if (primitiveExpression.Value == null) 
				return CreateResult ("");
			Type type = primitiveExpression.Value.GetType();
			return CreateResult (type.FullName);
		}
		
		Dictionary<LambdaExpression, ResolveResult> lambdaDictionary = new Dictionary<LambdaExpression, ResolveResult> ();
		
		public override object VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
		{
			ResolveResult result;
			if (!lambdaDictionary.TryGetValue (lambdaExpression, out result)) {
				lambdaDictionary[lambdaExpression] = result;
				result = resolver.ResolveLambda (this, lambdaExpression);
			}
			return result;
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
				TypeReference typeRef = new TypeReference ("System.Func");
				typeRef.GenericTypes.Add (TypeReference.Null);
				ResolveResult result = resolver.ResolveExpression (selectLambdaExpr, resolver.ResolvePosition, false);
				
				typeRef.GenericTypes.Add (result.ResolvedType.ConvertToTypeReference ());
				
				ObjectCreateExpression createExpression = new ObjectCreateExpression (typeRef, new List<Expression> (new Expression [] {
					null,
					selectLambdaExpr
				}));
				
				selectInvocation.Arguments.Add (createExpression);
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
