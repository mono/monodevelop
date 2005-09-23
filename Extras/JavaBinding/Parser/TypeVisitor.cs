// created on 22.08.2003 at 19:02

using System;
using System.Collections;

using JRefactory.Parser;
using JRefactory.Parser.AST;
using JavaBinding.Parser.SharpDevelopTree;
using MonoDevelop.Internal.Parser;

namespace JavaBinding.Parser
{
	public class TypeVisitor : AbstractASTVisitor
	{
		Resolver resolver;
		
		public TypeVisitor(Resolver resolver)
		{
			this.resolver = resolver;
		}
		
		public override object Visit(PrimitiveExpression primitiveExpression, object data)
		{
			if (primitiveExpression.Value != null) {
//				Console.WriteLine("Visiting " + primitiveExpression.Value);
				return new ReturnType(primitiveExpression.Value.GetType().FullName);
			}
			return null;
		}
		
		public override object Visit(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			// TODO : Operators 
			return binaryOperatorExpression.Left.AcceptVisitor(this, data);
		}
		
		public override object Visit(ParenthesizedExpression parenthesizedExpression, object data)
		{
			if (parenthesizedExpression == null) {
				return null;
			}
			return parenthesizedExpression.Expression.AcceptVisitor(this, data);
		}
		
		public override object Visit(InvocationExpression invocationExpression, object data)
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
			IClass c = resolver.SearchType(t.FullyQualifiedName, resolver.CompilationUnit);
			if (c.ClassType == ClassType.Delegate) {
				ArrayList methods = resolver.SearchMethod(t, "invoke");
				if (methods.Count <= 0) {
					return null;
				}
				return ((IMethod)methods[0]).ReturnType;
			}
			return null;
		}
		
		public override object Visit(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			if (fieldReferenceExpression == null) {
				return null;
			}
			
			IReturnType returnType = fieldReferenceExpression.TargetObject.AcceptVisitor(this, data) as IReturnType;
			if (returnType != null) {
				string name = resolver.SearchNamespace(returnType.FullyQualifiedName, resolver.CompilationUnit);
				if (name != null) {
					string n = resolver.SearchNamespace(string.Concat(name, ".", fieldReferenceExpression.FieldName), null);
					if (n != null) {
						return new ReturnType(n);
					}
					IClass c = resolver.SearchType(string.Concat(name, ".", fieldReferenceExpression.FieldName), resolver.CompilationUnit);
					if (c != null) {
						resolver.ShowStatic = true;
						return new ReturnType(c.FullyQualifiedName);
					}
					return null;
				}
				return resolver.SearchMember(returnType, fieldReferenceExpression.FieldName);
			}
//			Console.WriteLine("returnType of child is null!");
			return null;
		}
		
		public override object Visit(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			ReturnType type = pointerReferenceExpression.Expression.AcceptVisitor(this, data) as ReturnType;
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
		
		public override object Visit(IdentifierExpression identifierExpression, object data)
		{
			Console.WriteLine("visiting IdentifierExpression");
			if (identifierExpression == null) {
				return null;
			}
			string name = resolver.SearchNamespace(identifierExpression.Identifier, resolver.CompilationUnit);
			if (name != null) {
				return new ReturnType(name);
			}
			IClass c = resolver.SearchType(identifierExpression.Identifier, resolver.CompilationUnit);
			if (c != null) {
				resolver.ShowStatic = true;
				return new ReturnType(c.FullyQualifiedName);
			}
			return resolver.DynamicLookup(identifierExpression.Identifier);
		}
		
		public override object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return new ReturnType(typeReferenceExpression.TypeReference);
		}
		
		public override object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
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
		
		public override object Visit(AssignmentExpression assignmentExpression, object data)
		{
			return assignmentExpression.Left.AcceptVisitor(this, data);
		}
		
		public override object Visit(SizeOfExpression sizeOfExpression, object data)
		{
			return new ReturnType("System.Int32");
		}
		
		public override object Visit(TypeOfExpression typeOfExpression, object data)
		{
			return new ReturnType("System.Type");
		}
		
		public override object Visit(CheckedExpression checkedExpression, object data)
		{
			return checkedExpression.Expression.AcceptVisitor(this, data);
		}
		
		public override object Visit(UncheckedExpression uncheckedExpression, object data)
		{
			return uncheckedExpression.Expression.AcceptVisitor(this, data);
		}
		
		public override object Visit(CastExpression castExpression, object data)
		{
			return new ReturnType(castExpression.CastTo.Type);
		}
		
		public override object Visit(StackAllocExpression stackAllocExpression, object data)
		{
			ReturnType returnType = new ReturnType(stackAllocExpression.Type);
			++returnType.PointerNestingLevel;
			return returnType;
		}
		
		public override object Visit(IndexerExpression indexerExpression, object data)
		{
			IReturnType type = (IReturnType)indexerExpression.TargetObject.AcceptVisitor(this, data);
			if (type == null) {
				return null;
			}
			if (type.ArrayDimensions == null || type.ArrayDimensions.Length == 0) {
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
			if (type.ArrayDimensions[type.ArrayDimensions.Length - 1] != indexerExpression.Indices.Count) {
				return null;
			}
			int[] newArray = new int[type.ArrayDimensions.Length - 1];
			Array.Copy(type.ArrayDimensions, 0, newArray, 0, type.ArrayDimensions.Length - 1);
			return new ReturnType(type.Name, newArray, type.PointerNestingLevel);
		}
		
		public override object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			if (resolver.CallingClass == null) {
				return null;
			}
			return new ReturnType(resolver.CallingClass.FullyQualifiedName);
		}
		
		public override object Visit(BaseReferenceExpression baseReferenceExpression, object data)
		{
//			Console.WriteLine("Visiting base");
			if (resolver.CallingClass == null) {
				return null;
			}
			IClass baseClass = resolver.BaseClass(resolver.CallingClass);
			if (baseClass == null) {
//				Console.WriteLine("Base Class not found");
				return null;
			}
//			Console.WriteLine("Base Class: " + baseClass.FullyQualifiedName);
			return new ReturnType(baseClass.FullyQualifiedName);
		}
		
		public override object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			string name = resolver.SearchType(objectCreateExpression.CreateType.Type, resolver.CompilationUnit).FullyQualifiedName;
			return new ReturnType(name, objectCreateExpression.CreateType.RankSpecifier, objectCreateExpression.CreateType.PointerNestingLevel);
		}
		
		public override object Visit(ArrayCreateExpression arrayCreateExpression, object data)
		{
			ReturnType type = new ReturnType(arrayCreateExpression.CreateType);
			if (arrayCreateExpression.Parameters != null && arrayCreateExpression.Parameters.Count > 0) {
				int[] newRank = new int[arrayCreateExpression.Rank.Length + 1];
				newRank[0] = arrayCreateExpression.Parameters.Count - 1;
				Array.Copy(type.ArrayDimensions, 0, newRank, 1, type.ArrayDimensions.Length);
				type.ArrayDimensions = newRank;
			}
			return type;
		}
		
		public override object Visit(DirectionExpression directionExpression, object data)
		{
			// no calls allowed !!!
			return null;
		}
		
		public override object Visit(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			// no calls allowed !!!
			return null;
		}
	}
}
