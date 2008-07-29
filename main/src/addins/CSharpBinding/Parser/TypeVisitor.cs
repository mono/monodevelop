// created on 22.08.2003 at 19:02

using System;
using System.Collections;

using CSharpBinding.Parser.SharpDevelopTree;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

using MonoDevelop.Projects.Dom;

namespace CSharpBinding.Parser
{/*
	internal class TypeVisitor : AbstractAstVisitor
	{
		Resolver resolver;
		
		internal TypeVisitor(Resolver resolver)
		{
			this.resolver = resolver;
		}
		
		public override object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			if (primitiveExpression.Value != null) {
				return new ReturnType(primitiveExpression.Value.GetType().FullName);
			}
			return null;
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			string name = null;
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Add:
					name = "op_Addition";
					break;
				case BinaryOperatorType.Subtract:
					name = "op_Subtraction";
					break;
				case BinaryOperatorType.Multiply:
					name = "op_Multiply";
					break;
				case BinaryOperatorType.Divide:
					name = "op_Division";
					break;
				case BinaryOperatorType.Modulus:
					name = "op_Modulus";
					break;
				
				case BinaryOperatorType.BitwiseAnd:
					name = "op_BitwiseAnd";
					break;
				case BinaryOperatorType.BitwiseOr:
					name = "op_BitwiseOr";
					break;
				case BinaryOperatorType.ExclusiveOr:
					name = "op_ExclusiveOr";
					break;
				
				case BinaryOperatorType.ShiftLeft:
					name = "op_LeftShift";
					break;
				case BinaryOperatorType.ShiftRight:
					name = "op_RightShift";
					break;
				
				case BinaryOperatorType.GreaterThan:
					name = "op_GreaterThan";
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					name = "op_GreaterThanOrEqual";
					break;
				case BinaryOperatorType.Equality:
					name = "op_Equality";
					break;
				case BinaryOperatorType.InEquality:
					name = "op_Inequality";
					break;
				case BinaryOperatorType.LessThan:
					name = "op_LessThan";
					break;
				case BinaryOperatorType.LessThanOrEqual:./Parser/TypeVisitor.cs
					name = "op_LessThanOrEqual";
					break;
			}
			IReturnType t1 = binaryOperatorExpression.Left.AcceptVisitor (this, data) as IReturnType;
			IReturnType t2 = binaryOperatorExpression.Right.AcceptVisitor (this, data) as IReturnType;
			
			if (t1 == null || t2 == null)
				return null;
			
			IType c1 = resolver.SearchType (t1, resolver.CompilationUnit);
			IType c2 = resolver.SearchType (t2, resolver.CompilationUnit);
			
			if (c1 == null && c2 == null)
				return t1;
			
			// Look for operator overloads in both classes
			
			IMethod met1, met2;
			int level1, level2;
			
			FindOperator (name, c1, t2, 0, 1, out met1, out level1);
			FindOperator (name, c2, t1, 1, 0, out met2, out level2);
			
			// No operator overloads found
			if (met1 == null && met2 == null)
				return t1;
				
			if (met1 != null && met2 == null)
				return met1.ReturnType;
			
			if (met1 == null && met2 != null)
				return met2.ReturnType;
				
			// There are two possible candidates. Get the one closer in the inheritance hierarchy
			if (level1 < level2)
				return met1.ReturnType;
			else
				return met2.ReturnType;
		}
		
		// This methods look for an operator method. c1 is the class on which the operator is
		// being searched. c2 is the type of the second operand. ownerParamPos is the position
		// of the parameter for the class being searched. otherParamPos is the position of the
		// c2 parameter. met is the method found (or null if not found). sublevel is the number
		// of superclasses that had to be searched.
		void FindOperator (string name, IType c1, IReturnType c2, int ownerParamPos, int otherParamPos, out IMethod met, out int sublevel)
		{
			
			sublevel = 0;
			do {
				if (c1.Methods != null) {
					foreach (IMethod m in c1.Methods) {
						if (m.IsSpecialName && m.Name == name) {
							// Check parameter types
							IParameter par1 = m.Parameters [ownerParamPos];
							if (par1.ReturnType.ArrayCount != 0 || par1.ReturnType.PointerNestingLevel != 0 || par1.ReturnType.ByRef)
								continue;
							IType pc = resolver.ParserContext.GetClass (par1.ReturnType.FullyQualifiedName, par1.ReturnType.GenericArguments, true, true);
							if (pc == null || (pc.FullyQualifiedName != c1.FullyQualifiedName))
								continue;

							// Ok, the class that implements the operator is in the right parameter position
							// Now let's check if the other parameter is compatible with the other operand
							
							IParameter par2 = m.Parameters [otherParamPos];
							if (DefaultReturnType.IsTypeAssignable (resolver.ParserContext, par2.ReturnType, c2) == -1)
								continue;
							met = m;
							return;
						}
					}
				}
				// Operator not found in this class, look in the base class
				// Avoid implemented interfaces
				IType baseClass = null;
				if (c1.BaseTypes != null) {
					foreach (IReturnType bt in c1.BaseTypes) {
						IType bc = resolver.ParserContext.GetClass (bt.FullyQualifiedName, bt.GenericArguments, true, true);
						if (bc.ClassType != MonoDevelop.Projects.Parser.ClassType.Interface) {
							baseClass = bc;
							break;
						}
					}
				}
				c1 = baseClass;
				sublevel++;
			} while (c1 != null);
			
			// Not found
			met = null;
		}
		

		public override object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			if (parenthesizedExpression == null) {
				return null;
			}
			return parenthesizedExpression.Expression.AcceptVisitor(this, data);
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			if (invocationExpression.TargetObject is FieldReferenceExpression) {
				FieldReferenceExpression field = (FieldReferenceExpression)invocationExpression.TargetObject;
				IReturnType type = field.TargetObject.AcceptVisitor(this, data) as IReturnType;
				ArrayList methods = resolver.SearchMethod(type, field.FieldName);
				resolver.ShowStatic = false;
				if (methods.Count <= 0) {
					return null;
				}
				// TODO: Find the right method
				return ((IMethod)methods[0]).ReturnType;
			} else if (invocationExpression.TargetObject is IdentifierExpression) {
				string id = ((IdentifierExpression)invocationExpression.TargetObject).Identifier;
				if (resolver.CallingClass == null) {
					return null;
				}
				IReturnType type = new ReturnType(resolver.CallingClass.FullyQualifiedName);
				ArrayList methods = resolver.SearchMethod(type, id);
				resolver.ShowStatic = false;
				if (methods.Count <= 0) {
					return null;
				}
				// TODO: Find the right method
				return ((IMethod)methods[0]).ReturnType;
			}
			// invocationExpression is delegate call
			IReturnType t = invocationExpression.AcceptChildren(this, data) as IReturnType;
			if (t == null) {
				return null;
			}
			IType c = resolver.SearchType(t, resolver.CompilationUnit);
			if (c.ClassType == MonoDevelop.Projects.Parser.ClassType.Delegate) {
				ArrayList methods = resolver.SearchMethod(t, "invoke");
				if (methods.Count <= 0) {
					return null;
				}
				return ((IMethod)methods[0]).ReturnType;
			}
			return null;
		}
		
		public override object VisitFieldReferenceExpression(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			if (fieldReferenceExpression == null) {
				return null;
			}
			// "int." generates a FieldreferenceExpression with TargetObject TypeReferenceExpression and no FieldName
			if (fieldReferenceExpression.FieldName == null || fieldReferenceExpression.FieldName == "") {
				if (fieldReferenceExpression.TargetObject is TypeReferenceExpression) {
					resolver.ShowStatic = true;
					return new ReturnType(((TypeReferenceExpression)fieldReferenceExpression.TargetObject).TypeReference);
				}
			}
			IReturnType returnType = fieldReferenceExpression.TargetObject.AcceptVisitor(this, data) as IReturnType;
			if (returnType != null) {
				string name = resolver.SearchNamespace(returnType.FullyQualifiedName, resolver.CompilationUnit);
				if (name != null) {
					string n = resolver.SearchNamespace(string.Concat(name, ".", fieldReferenceExpression.FieldName), null);
					if (n != null) {
						return new ReturnType(n);
					}
					IType c = resolver.SearchType(string.Concat(name, ".", fieldReferenceExpression.FieldName), null, resolver.CompilationUnit);
					if (c != null) {
						resolver.ShowStatic = true;
						return new ReturnType(c.FullyQualifiedName);
					}
					return null;
				}
				object res = resolver.SearchMember (returnType, fieldReferenceExpression.FieldName);
				if (res != null)
					return res;
				resolver.ShowStatic = true;
				return resolver.SearchMember (returnType, fieldReferenceExpression.FieldName);
			}
//			Console.WriteLine("returnType of child is null!");
			return null;
		}
		
		public override object VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			ReturnType type = pointerReferenceExpression.TargetObject.AcceptVisitor(this, data) as ReturnType;
			if (type == null) {
				return null;
			}
			type = type.Clone();
			--type.PointerNestingLevel;
			if (type.PointerNestingLevel != 0) {
				return null;
			}
			return resolver.SearchMember(type, pointerReferenceExpression.Identifier);
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			//Console.WriteLine("visiting IdentifierExpression");
			if (identifierExpression == null) {
				return null;
			}
			
			object ob = resolver.DynamicLookup(identifierExpression.Identifier);
			if (ob != null)
				return ob;
			
			string name = resolver.SearchNamespace (identifierExpression.Identifier, resolver.CompilationUnit);
			if (name != null)
				return new ReturnType (name);

			if (resolver.CallingClass != null) {
				// It may be a reference to a child namespace
				name = resolver.SearchNamespace (resolver.CallingClass.Namespace + "." + identifierExpression.Identifier, resolver.CompilationUnit);
				if (name != null)
					return new ReturnType (name);
				
				// check parent namespaces
				string ns = resolver.CallingClass.Namespace;
				int dot = ns.Length;
				
				while (dot > 1 && (dot = ns.LastIndexOf ('.', dot - 1)) != -1) {
					name = ns.Substring (0, dot + 1) + identifierExpression.Identifier;
					name = resolver.SearchNamespace (name, resolver.CompilationUnit);
					if (name != null)
						return new ReturnType (name);
				}
			}
			
			IType c = resolver.SearchType(identifierExpression.Identifier, null, resolver.CompilationUnit);
			if (c != null) {
				resolver.ShowStatic = true;
				return new ReturnType(c.FullyQualifiedName);
			}
			return null;
		}
		
		public override object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return new ReturnType(typeReferenceExpression.TypeReference);
		}
		
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			if (unaryOperatorExpression == null) {
				return null;
			}
			ReturnType expressionType = unaryOperatorExpression.Expression.AcceptVisitor(this, data) as ReturnType;
			// TODO: Little bug: unary operator MAY change the return type,
			//                   but that is only a minor issue
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.Not:
					break;
				case UnaryOperatorType.BitNot:
					break;
				case UnaryOperatorType.Minus:
					break;
				case UnaryOperatorType.Plus:
					break;
				case UnaryOperatorType.Increment:
				case UnaryOperatorType.PostIncrement:
					break;
				case UnaryOperatorType.Decrement:
				case UnaryOperatorType.PostDecrement:
					break;
				case UnaryOperatorType.Star:       // dereference
					--expressionType.PointerNestingLevel;
					break;
				case UnaryOperatorType.BitWiseAnd: // get reference
					++expressionType.PointerNestingLevel; 
					break;
				case UnaryOperatorType.None:
					break;
			}
			return expressionType;
		}
		
		public override object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			return assignmentExpression.Left.AcceptVisitor(this, data);
		}
		
		public override object VisitSizeOfExpression(SizeOfExpression sizeOfExpression, object data)
		{
			return new ReturnType("System.Int32");
		}
		
		public override object VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			return new ReturnType("System.Type");
		}
		
		public override object VisitCheckedExpression(CheckedExpression checkedExpression, object data)
		{
			return checkedExpression.Expression.AcceptVisitor(this, data);
		}
		
		public override object VisitUncheckedExpression(UncheckedExpression uncheckedExpression, object data)
		{
			return uncheckedExpression.Expression.AcceptVisitor(this, data);
		}
		
		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			return new ReturnType(castExpression.CastTo.Type);
		}
		
		public override object VisitStackAllocExpression(StackAllocExpression stackAllocExpression, object data)
		{
			ReturnType returnType = new ReturnType(stackAllocExpression.TypeReference);
			++returnType.PointerNestingLevel;
			return returnType;
		}
		
		public override object VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			//Console.WriteLine("TypeVisiting IndexerExpression: " + indexerExpression);
			IReturnType type = (IReturnType)indexerExpression.TargetObject.AcceptVisitor(this, data);
			if (type == null) {
				return null;
			}
				
			if (type.ArrayDimensions == null || type.ArrayDimensions.Length == 0) {
				//Console.WriteLine("No Array, checking indexer");
				// check if ther is an indexer
				if (indexerExpression.TargetObject is ThisReferenceExpression) {
					if (resolver.CallingClass == null) {
						return null;
					}
					type = new ReturnType(resolver.CallingClass.FullyQualifiedName);
				}
				ArrayList indexer = resolver.SearchIndexer(type);
				if (indexer.Count == 0) {
					return null;
				}
				// TODO: get the right indexer
				return ((IIndexer)indexer[0]).ReturnType;
			}
			
			// TODO: what is a[0] if a is pointer to array or array of pointer ? 
			if (type.ArrayDimensions[0] != indexerExpression.Indexes.Count) {
				//Console.WriteLine("Number of indices do not match the Array dimension");
				return null;
			}
			int[] newArray = new int[type.ArrayDimensions.Length - 1];
			Array.Copy(type.ArrayDimensions, 1, newArray, 0, type.ArrayDimensions.Length - 1);
			return new ReturnType(type.FullyQualifiedName, newArray, type.PointerNestingLevel, type.GenericArguments, false);
		}
		
		public override object VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			// The 'this' reference is invalid outside of a member
			if (resolver.GetMember () == null)
				return null;

			return new ReturnType(resolver.CallingClass.FullyQualifiedName);
		}
		
		public override object VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
//			Console.WriteLine("Visiting base");
			if (resolver.CallingClass == null) {
				return null;
			}
			IType baseClass = resolver.BaseClass(resolver.CallingClass);
			if (baseClass == null) {
//				Console.WriteLine("Base Class not found");
				return null;
			}
//			Console.WriteLine("Base Class: " + baseClass.FullyQualifiedName);
			return new ReturnType(baseClass.FullyQualifiedName);
		}
		
		public override object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			IType type = resolver.SearchType (ReturnType.GetFullTypeName (objectCreateExpression.CreateType), null, resolver.CompilationUnit);
			if (type == null) return null;
			return new ReturnType (objectCreateExpression.CreateType, type);
		}
		
		public override object VisitArrayCreateExpression (ArrayCreateExpression arrayCreateExpression, object data)
		{
			ReturnType type = new ReturnType(arrayCreateExpression.CreateType);
			if (arrayCreateExpression.Arguments != null && arrayCreateExpression.Arguments.Count > 0) {
				int[] newRank = new int[arrayCreateExpression.CreateType.RankSpecifier.Length + 1];
				newRank[0] = arrayCreateExpression.Arguments.Count - 1;
				Array.Copy(type.ArrayDimensions, 0, newRank, 1, type.ArrayDimensions.Length);
				type.ArrayDimensions = newRank;
			}
			return type;
		}
		
		public override object VisitDirectionExpression (DirectionExpression directionExpression, object data)
		{
			// no calls allowed !!!
			return null;
		}
	}*/
}
