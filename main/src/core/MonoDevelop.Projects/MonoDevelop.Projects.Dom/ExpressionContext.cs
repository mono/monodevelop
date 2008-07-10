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
		object defaultItem;
		
		public object DefaultItem {
			get {
				return defaultItem;
			}
		}
		
		public virtual bool IsObjectCreation {
			get {
				return false;
			}
		}
		
		public virtual bool FilterEntry (object entry)
		{
			if (entry is IMember) {
				System.Console.WriteLine ("Filter entry !!!!");
				return ((IMember)entry).IsSpecialName;
			}
			return false;
		}
		
		
		public static ExpressionContext Default   = new ExpressionContext ();
		public static ExpressionContext Using     = new ExpressionContext ();
		
		
		// SharpDevelop compatibilty contextes
		public static ExpressionContext Type     = new ExpressionContext ();
		public static ExpressionContext Namespace = new ExpressionContext ();
		public static ExpressionContext Attribute = new ExpressionContext ();
		public static ExpressionContext IdentifierExpected = new ExpressionContext ();
		public static ExpressionContext ConstraintsStart = new ExpressionContext ();
		public static ExpressionContext FullyQualifiedType      = new ExpressionContext ();
		public static ExpressionContext BaseConstructorCall      = new ExpressionContext ();
		public static ExpressionContext DelegateType      = new ExpressionContext ();
		public static ExpressionContext FirstParameterType      = new ExpressionContext ();
		public static ExpressionContext ObjectInitializer      = new ExpressionContext ();
		public static ExpressionContext ParameterType      = new ExpressionContext ();
		public static ExpressionContext MethodBody      = new ExpressionContext ();
		public static ExpressionContext PropertyDeclaration      = new ExpressionContext ();
		public static ExpressionContext InterfacePropertyDeclaration      = new ExpressionContext ();
		public static ExpressionContext EventDeclaration      = new ExpressionContext ();
		public static ExpressionContext TypeDeclaration      = new ExpressionContext ();
		public static ExpressionContext InterfaceDeclaration      = new ExpressionContext ();
		public static ExpressionContext Global      = new ExpressionContext ();
		public static ExpressionContext Constraints      = new ExpressionContext ();
		public static ExpressionContext Interface      = new ExpressionContext ();
		public static ExpressionContext EnumBaseType      = new ExpressionContext ();
		public static ExpressionContext InheritableType      = new ExpressionContext ();
		
		public static ExpressionContext TypeDerivingFrom (IReturnType baseType, bool isObjectCreation)
		{
			return new ExpressionContext ();
		}
		
	}
}
