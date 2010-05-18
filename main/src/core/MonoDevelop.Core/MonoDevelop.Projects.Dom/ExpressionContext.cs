//
// ExpressionContext.cs
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

namespace MonoDevelop.Projects.Dom
{
	public class ExpressionContext
	{
		public object DefaultItem {
			get;
			set;
		}
		
		public virtual bool IsObjectCreation {
			get {
				return false;
			}
		}

		public string ContextName {
			get;
			protected set;
		}
		
		public virtual bool FilterEntry (object entry)
		{
			IMember member = entry as IMember;
			if (member != null && member.Name != null && member.Name.IndexOf ('<') >= 0)
				return true;
			IMethod method = entry as IMethod;
			if (method != null && method.Name == "Finalize" && method.DeclaringType.FullName == "System.Object")
				return true;
			IProperty property = entry as IProperty;
			if (property != null && property.IsIndexer)
				return true;
			if (member != null) {
				if (member.DeclaringType != null && member.DeclaringType.ClassType == ClassType.Enum) {
					return !member.IsStatic && !member.IsConst;
				}
				return member.IsSpecialName;
			}
		
			return false;
		}
		
		protected ExpressionContext ()
		{
		}
		
		public ExpressionContext (string contextName)
		{
			this.ContextName = contextName;
		}
		
		public override string ToString ()
		{
			return String.Format ("[ExpressionContext:ContextName={0}]", ContextName);
		}
		
		public static ExpressionContext Default   = new ExpressionContext ("Default");
		public static ExpressionContext Using     = new ExpressionContext ("Using");
		
		
		// SharpDevelop compatibilty contextes
		public static ExpressionContext Namespace                    = new ExpressionContext ("Namespace");
		public static ExpressionContext Attribute                    = new ExpressionContext ("Attribute");
		public static ExpressionContext AttributeArguments           = new ExpressionContext ("AttributeArguments");
		public static ExpressionContext IdentifierExpected           = new ExpressionContext ("IdentifierExpected");
		public static ExpressionContext ConstraintsStart             = new ExpressionContext ("ConstraintsStart");
		public static ExpressionContext FullyQualifiedType           = new ExpressionContext ("FullyQualifiedType");
		public static ExpressionContext BaseConstructorCall          = new ExpressionContext ("BaseConstructorCall");
		public static ExpressionContext DelegateType                 = new ExpressionContext ("DelegateType");
		public static ExpressionContext FirstParameterType           = new ExpressionContext ("FirstParameterType");
		public static ExpressionContext ObjectInitializer            = new ExpressionContext ("ObjectInitializer");
		public static ExpressionContext ParameterType                = new ExpressionContext ("ParameterType");
		public static ExpressionContext MethodBody                   = new ExpressionContext ("MethodBody");
		public static ExpressionContext PropertyDeclaration          = new ExpressionContext ("PropertyDeclaration");
		public static ExpressionContext InterfacePropertyDeclaration = new ExpressionContext ("InterfacePropertyDeclaration");
		public static ExpressionContext EventDeclaration             = new ExpressionContext ("EventDeclaration");
		public static ExpressionContext TypeDeclaration              = new ExpressionContext ("TypeDeclaration");
		public static ExpressionContext InterfaceDeclaration         = new ExpressionContext ("InterfaceDeclaration");
		public static ExpressionContext Global                       = new ExpressionContext ("Global");
		public static ExpressionContext Constraints                  = new ExpressionContext ("Constraints");
		public static ExpressionContext Interface                    = new ExpressionContext ("Interface");
		public static ExpressionContext EnumBaseType                 = new ExpressionContext ("EnumBaseType");
		public static ExpressionContext InheritableType              = new ExpressionContext ("InheritableType");
		public static ExpressionContext NamespaceNameExcepted        = new ExpressionContext ("NamespaceNameExcepted");
		public static ExpressionContext TypeName                     = new ExpressionContext ("TypeName");
		public static ExpressionContext LinqContext                  = new ExpressionContext ("LinqContext");
	
		public static ExpressionContext ObjectCreation               = new ObjectCreationContext ();
		
		public class ObjectCreationContext : ExpressionContext
		{
			public override bool IsObjectCreation {
				get {
					return true;
				}
			}
			public ObjectCreationContext ()
			{
				ContextName = "ObjectCreationContext";
			}
		}
		
		public static TypeExpressionContext TypeDerivingFrom (IReturnType baseType, IReturnType unresolvedBaseType, bool isObjectCreation)
		{
			return new TypeExpressionContext (baseType, unresolvedBaseType, isObjectCreation);
		}
		
		public class TypeExpressionContext : ExpressionContext
		{
			public IReturnType Type {
				get;
				private set;
			}
			
			public IReturnType UnresolvedType {
				get;
				private set;
			}
			
			bool isObjectCreation;
			public override bool IsObjectCreation {
				get {
					return isObjectCreation;
				}
			}
			
			public override bool FilterEntry (object entry)
			{
				IType type = entry as IType;
				if (type != null && (type.IsSpecialName || type.Name.IndexOf ('<') >= 0))
					return true;
				if (IsObjectCreation && entry is IType) {
					return type.ClassType != ClassType.Class && type.ClassType != ClassType.Struct && (type.IsStatic || type.IsAbstract);
				}
				if (entry is Namespace)
					return false;
				return true;
			}
			
			public TypeExpressionContext (IReturnType type, IReturnType unresolvedType, bool isObjectCreation)
			{
				this.Type             = type;
				this.UnresolvedType   = unresolvedType;
				this.isObjectCreation = isObjectCreation;
				this.ContextName = this.ToString ();
			}
			
			public override string ToString ()
			{
				return String.Format ("[TypeExpressionContext:Type={0}, UnresolvedType={1}, IsObjectCreation={2}]", Type, UnresolvedType, IsObjectCreation);
			}
		}
		
	}
}
