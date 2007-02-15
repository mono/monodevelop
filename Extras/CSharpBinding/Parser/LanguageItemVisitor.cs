// created on 22.08.2003 at 19:02

using System;
using System.Collections;

using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;
using CSharpBinding.Parser.SharpDevelopTree;

using MonoDevelop.Projects.Parser;

namespace CSharpBinding.Parser
{
	internal class LanguageItemVisitor : AbstractAstVisitor
	{
		Resolver resolver;
		
		internal LanguageItemVisitor (Resolver resolver)
		{
			this.resolver = resolver;
		}
		
		public override object Visit(PrimitiveExpression primitiveExpression, object data)
		{
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
				TypeVisitor tv = new TypeVisitor (resolver);
				IReturnType type = field.TargetObject.AcceptVisitor(tv, data) as IReturnType;
				ArrayList methods = resolver.SearchMethod(type, field.FieldName);
				resolver.ShowStatic = false;
				if (methods.Count <= 0) {
					return null;
				}
				// TODO: Find the right method
				return ResolveOverload (methods, invocationExpression, data);
			} else if (invocationExpression.TargetObject is IdentifierExpression) {
				string id = ((IdentifierExpression)invocationExpression.TargetObject).Identifier;
				if (resolver.CallingClass == null) {
					return null;
				}
				IReturnType type = new ReturnType(resolver.CallingClass.FullyQualifiedName);
				resolver.ShowStatic = false;
				ArrayList methods = resolver.SearchMethod(type, id);
				if (methods.Count == 0) {
					resolver.ShowStatic = true;
					methods = resolver.SearchMethod(type, id);
				}
				resolver.ShowStatic = false;
				if (methods.Count <= 0) {
					// It may be a call to a constructor
					if (invocationExpression.TypeArguments != null && invocationExpression.TypeArguments.Count > 0)
						return resolver.SearchType (id + "`" + invocationExpression.TypeArguments.Count, null, resolver.CompilationUnit);
					else
						return resolver.SearchType (id, null, resolver.CompilationUnit);
				}
				// TODO: Find the right method
				return ResolveOverload (methods, invocationExpression, data);
			}
			// invocationExpression is delegate call
			IReturnType t = invocationExpression.AcceptChildren(this, data) as IReturnType;
			if (t == null) {
				return null;
			}
			IClass c = resolver.SearchType (t, resolver.CompilationUnit);
			if (c.ClassType == MonoDevelop.Projects.Parser.ClassType.Delegate) {
				ArrayList methods = resolver.SearchMethod (t, "invoke");
				if (methods.Count <= 0) {
					return null;
				}
				return ResolveOverload (methods, invocationExpression, data);
			}
			return null;
		}
		
		IMethod ResolveOverload (ArrayList methods, InvocationExpression invocationExpression, object data)
		{
			TypeVisitor tv = new TypeVisitor (resolver);
			IReturnType[] argTypes = new IReturnType [invocationExpression.Arguments.Count];
			for (int n=0; n<invocationExpression.Arguments.Count; n++) {
				Expression arg = invocationExpression.Arguments [n];
				argTypes [n] = arg.AcceptVisitor (tv, data) as IReturnType;
				
				// This may happen when trying to resolve a method declaration
				if (argTypes [n] == null)
					return (IMethod) methods [0];
			}
			
			// Look for the method with the most closest parameter types.
			IMethod bestMethod = null;
			int bestLevel = int.MaxValue;
			
			foreach (IMethod met in methods) {
				if (met.Parameters.Count != argTypes.Length)
					continue;
				int metLevel = 0;
				for (int n=0; n<argTypes.Length; n++) {
					int tlevel = DefaultReturnType.IsTypeAssignable (resolver.ParserContext, met.Parameters[n].ReturnType, argTypes [n]);
					if (tlevel == -1) {
						// Type not assignable
						metLevel = -1;
						break;
					}
					metLevel += tlevel;
				}
				if (metLevel != -1 && metLevel < bestLevel) {
					bestMethod = met;
					bestLevel = metLevel;
				}
			}
			if (bestMethod != null)
				return bestMethod;
			
			// If no exact match can be found, just return one of them
			return (IMethod) methods [0];
		}
		
		public override object Visit(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			if (fieldReferenceExpression == null) {
				return null;
			}
			// int. generates a FieldreferenceExpression with TargetObject TypeReferenceExpression and no FieldName
			if (fieldReferenceExpression.FieldName == null || fieldReferenceExpression.FieldName == "") {
				if (fieldReferenceExpression.TargetObject is TypeReferenceExpression) {
					resolver.ShowStatic = true;
					ReturnType rt = new ReturnType (((TypeReferenceExpression)fieldReferenceExpression.TargetObject).TypeReference);
					return resolver.SearchType (rt, resolver.CompilationUnit);
				}
			}
			TypeVisitor tv = new TypeVisitor (resolver);
			IReturnType returnType = fieldReferenceExpression.TargetObject.AcceptVisitor(tv, data) as IReturnType;
			if (returnType != null) {
				string name = resolver.SearchNamespace(returnType.FullyQualifiedName, resolver.CompilationUnit);
				if (name != null) {
					string n = resolver.SearchNamespace(string.Concat(name, ".", fieldReferenceExpression.FieldName), null);
					if (n != null) {
						return new Namespace (n, "");
					}
					IClass c = resolver.SearchType(string.Concat(name, ".", fieldReferenceExpression.FieldName), null, resolver.CompilationUnit);
					if (c != null) {
						resolver.ShowStatic = true;
						return c;
					}
					return null;
				}
				
				object res = resolver.SearchClassMember (returnType, fieldReferenceExpression.FieldName, true);
				if (res != null)
					return res;
				resolver.ShowStatic = true;
				return resolver.SearchClassMember (returnType, fieldReferenceExpression.FieldName, true);
			}
			return null;
		}
		
		public override object Visit(PointerReferenceExpression pointerReferenceExpression, object data)
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
			return resolver.SearchClassMember (type, pointerReferenceExpression.Identifier, true);
		}
		
		public override object Visit(IdentifierExpression identifierExpression, object data)
		{
			if (identifierExpression == null) {
				return null;
			}
			string name = resolver.SearchNamespace(identifierExpression.Identifier, resolver.CompilationUnit);
			if (name != null) {
				return new Namespace (name, "");
			}
			IClass c = resolver.SearchType(identifierExpression.Identifier, null, resolver.CompilationUnit);
			if (c != null) {
				resolver.ShowStatic = true;
				return c;
			}
			return resolver.IdentifierLookup (identifierExpression.Identifier);
		}
		
		public override object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return resolver.SearchType (new ReturnType (typeReferenceExpression.TypeReference), resolver.CompilationUnit);
		}
		
		public override object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			return null;
		}
		
		public override object Visit(AssignmentExpression assignmentExpression, object data)
		{
			return assignmentExpression.Left.AcceptVisitor(this, data);
		}
		
		public override object Visit(SizeOfExpression sizeOfExpression, object data)
		{
			return null;
		}
		
		public override object Visit(TypeOfExpression typeOfExpression, object data)
		{
			return null;
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
			return null;
		}
		
		public override object Visit(StackAllocExpression stackAllocExpression, object data)
		{
			return null;
		}
		
		public override object Visit(IndexerExpression indexerExpression, object data)
		{
			object ob = indexerExpression.TargetObject.AcceptVisitor(this, data);
			IReturnType type = ob as IReturnType;
			if (type != null) {
				if (type.ArrayDimensions == null || type.ArrayDimensions.Length == 0) {
					if (indexerExpression.TargetObject is ThisReferenceExpression) {
						return null;
					}
					ArrayList indexer = resolver.SearchIndexer(type);
					if (indexer.Count == 0) {
						return null;
					}
					// TODO: get the right indexer
					return indexer[0];
				}
				return null;
			}
			
			return ob;
		}
		
		public override object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			return null;
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
			return baseClass;
		}
		
		public override object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			return resolver.SearchType (new ReturnType (objectCreateExpression.CreateType), resolver.CompilationUnit);
		}
		
		public override object Visit(ArrayCreateExpression arrayCreateExpression, object data)
		{
			return resolver.SearchType (new ReturnType (arrayCreateExpression.CreateType), resolver.CompilationUnit);
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
