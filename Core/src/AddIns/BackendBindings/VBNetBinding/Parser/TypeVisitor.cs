// created on 22.08.2003 at 19:02

using System;
using System.Collections;

//using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.VB;
using ICSharpCode.SharpRefactory.Parser.AST.VB;
using VBBinding.Parser.SharpDevelopTree;

using MonoDevelop.Internal.Parser;

namespace VBBinding.Parser
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
			if (invocationExpression.TargetObject is FieldReferenceOrInvocationExpression) {
				FieldReferenceOrInvocationExpression field = (FieldReferenceOrInvocationExpression)invocationExpression.TargetObject;
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
			IClass c = resolver.SearchType(t.FullyQualifiedName, resolver.CallingClass, resolver.CompilationUnit);
			if (c.ClassType == ClassType.Delegate) {
				ArrayList methods = resolver.SearchMethod(t, "invoke");
				if (methods.Count <= 0) {
					return null;
				}
				return ((IMethod)methods[0]).ReturnType;
			}
			return null;
		}
		
		
		//TODO - Verify logic; did a lot of "just make it work" hacking in this method
		public override object Visit(FieldReferenceOrInvocationExpression fieldReferenceExpression, object data)
		{
			if (fieldReferenceExpression == null) {
				return null;
			}
			
			IReturnType returnType = fieldReferenceExpression.TargetObject.AcceptVisitor(this, data) as IReturnType;
			if (returnType != null) {
				//Console.WriteLine("Got type: " + returnType.FullyQualifiedName);
				string name = resolver.SearchNamespace(returnType.FullyQualifiedName, resolver.CompilationUnit);
				if (name != null) {
					//Console.WriteLine("Got subtype: " + name + "." + fieldReferenceExpression.FieldName);
					string n = resolver.SearchNamespace(string.Concat(name, ".", fieldReferenceExpression.FieldName), null);
					if (n != null) {
						return new ReturnType(n);
					}
					//Console.WriteLine("Trying classes");
					IClass c = resolver.SearchType(string.Concat(name, ".", fieldReferenceExpression.FieldName), resolver.CallingClass, resolver.CompilationUnit);
					//IClass c = resolver.SearchType(string.Concat(name, ".", fieldReferenceExpression.FieldName),  resolver.CompilationUnit);
					if (c != null) {
						resolver.ShowStatic = true;
						return new ReturnType(c.FullyQualifiedName);
					}
					
					//FIXME?
					try{
						return new ReturnType(name + "." + fieldReferenceExpression.FieldName);
					}catch(Exception ex){
						return null;	
					}
				}
				//Console.WriteLine("Trying Members");
				return resolver.SearchMember(returnType, fieldReferenceExpression.FieldName);
			}
//			Console.WriteLine("returnType of child is null!");
			return null;
		}
		
		public override object Visit(IdentifierExpression identifierExpression, object data)
		{
			//Console.WriteLine("visiting IdentifierExpression");
			if (identifierExpression == null) {
				//Console.WriteLine("identifierExpression == null");
				return null;
			}
			string name = resolver.SearchNamespace(identifierExpression.Identifier, resolver.CompilationUnit);
			if (name != null) {
				return new ReturnType(name);
			}
			//Console.WriteLine("no namespace found");
			IClass c = resolver.SearchType(identifierExpression.Identifier, resolver.CallingClass, resolver.CompilationUnit);
			if (c != null) {
				resolver.ShowStatic = true;
				return new ReturnType(c.FullyQualifiedName);
			}
			//Console.WriteLine("no type found");
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
		
		public override object Visit(GetTypeExpression getTypeExpression, object data)
		{
			return new ReturnType("System.Type");
		}
		
		public override object Visit(TypeOfExpression typeOfExpression, object data)
		{
			return new ReturnType("System.Type");
		}
		
		public override object Visit(AddressOfExpression addressOfExpression, object data)
		{
			// no calls allowed !!!
			return null;
		}
		
		public override object Visit(CastExpression castExpression, object data)
		{
			return new ReturnType(castExpression.CastTo.Type);
		}
		
		public override object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			if (resolver.CallingClass == null) {
				return null;
			}
			return new ReturnType(resolver.CallingClass.FullyQualifiedName);
		}
		
		public override object Visit(ClassReferenceExpression classReferenceExpression, object data)
		{
			if (resolver.CallingClass == null) {
				return null;
			}
			return new ReturnType(resolver.CallingClass.FullyQualifiedName);
		}
		
		public override object Visit(BaseReferenceExpression baseReferenceExpression, object data)
		{
			if (resolver.CallingClass == null) {
				return null;
			}
			IClass baseClass = resolver.BaseClass(resolver.CallingClass);
			if (baseClass == null) {
				return null;
			}
			return new ReturnType(baseClass.FullyQualifiedName);
		}
		
		public override object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			string name = resolver.SearchType(objectCreateExpression.CreateType.Type, resolver.CallingClass, resolver.CompilationUnit).FullyQualifiedName;
			return new ReturnType(name, (int[])objectCreateExpression.CreateType.RankSpecifier.ToArray(typeof(int)), 0);
		}
		
		public override object Visit(ArrayCreateExpression arrayCreateExpression, object data)
		{
			ReturnType type = new ReturnType(arrayCreateExpression.CreateType);
			if (arrayCreateExpression.Parameters != null && arrayCreateExpression.Parameters.Count > 0) {
				int[] newRank = new int[arrayCreateExpression.CreateType.RankSpecifier.Count + 1];
				newRank[0] = arrayCreateExpression.Parameters.Count - 1;
				for (int i = 0; i < type.ArrayDimensions.Length; ++i) {
					newRank[i + 1] = type.ArrayDimensions[i];
				}
				Array.Copy(type.ArrayDimensions, 0, newRank, 1, type.ArrayDimensions.Length);
				type.ArrayDimensions = newRank;
			}
			return type;
		}
		
		public override object Visit(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			// no calls allowed !!!
			return null;
		}
	}
}
