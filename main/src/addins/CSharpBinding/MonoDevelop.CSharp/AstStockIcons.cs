// 
// AstStockIcons.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharp
{
	// Copied from StockIcons.cs and extended for the c# ast (c# AST shouldn't be in Ide)
	public static class AstStockIcons
	{
//		static readonly IconId Error = "gtk-dialog-error";
		static readonly IconId Class = "md-class";
		static readonly IconId Enum = "md-enum";
		static readonly IconId Event = "md-event";
		static readonly IconId Field = "md-field";
		static readonly IconId Interface = "md-interface";
		static readonly IconId Method = "md-method";
		static readonly IconId ExtensionMethod = "md-extensionmethod";
		static readonly IconId Property = "md-property";
		static readonly IconId Struct = "md-struct";
		static readonly IconId Delegate = "md-delegate";
		public static readonly IconId Namespace = "md-name-space";
		static readonly IconId InternalClass = "md-internal-class";
		static readonly IconId InternalDelegate = "md-internal-delegate";
		static readonly IconId InternalEnum = "md-internal-enum";
		static readonly IconId InternalEvent = "md-internal-event";
		static readonly IconId InternalField = "md-internal-field";
		static readonly IconId InternalInterface = "md-internal-interface";
		static readonly IconId InternalMethod = "md-internal-method";
		static readonly IconId InternalExtensionMethod = "md-internal-extensionmethod";
		static readonly IconId InternalProperty = "md-internal-property";
		static readonly IconId InternalStruct = "md-internal-struct";
		static readonly IconId PrivateClass = "md-private-class";
		static readonly IconId PrivateDelegate = "md-private-delegate";
		static readonly IconId PrivateEnum = "md-private-enum";
		static readonly IconId PrivateEvent = "md-private-event";
		static readonly IconId PrivateField = "md-private-field";
		static readonly IconId PrivateInterface = "md-private-interface";
		static readonly IconId PrivateMethod = "md-private-method";
		static readonly IconId PrivateExtensionMethod = "md-private-extensionmethod";
		static readonly IconId PrivateProperty = "md-private-property";
		static readonly IconId PrivateStruct = "md-private-struct";
		static readonly IconId ProtectedClass = "md-protected-class";
		static readonly IconId ProtectedDelegate = "md-protected-delegate";
		static readonly IconId ProtectedEnum = "md-protected-enum";
		static readonly IconId ProtectedEvent = "md-protected-event";
		static readonly IconId ProtectedField = "md-protected-field";
		static readonly IconId ProtectedInterface = "md-protected-interface";
		static readonly IconId ProtectedMethod = "md-protected-method";
		static readonly IconId ProtectedExtensionMethod = "md-protected-extensionmethod";
		static readonly IconId ProtectedProperty = "md-protected-property";
		static readonly IconId ProtectedStruct = "md-protected-struct";
		
		static IconId[,] typeIconTable = new IconId[,] {
			{Class,     PrivateClass,     ProtectedClass,     InternalClass},     // class
			{Enum,      PrivateEnum,      ProtectedEnum,      InternalEnum},      // enum
			{Interface, PrivateInterface, ProtectedInterface, InternalInterface}, // interface
			{Struct,    PrivateStruct,    ProtectedStruct,    InternalStruct},    // struct
			{Delegate,  PrivateDelegate,  ProtectedDelegate,  InternalDelegate}   // delegate
		};
		static readonly IconId[] fieldIconTable = {AstStockIcons.Field, AstStockIcons.PrivateField, AstStockIcons.ProtectedField, AstStockIcons.InternalField};
		static readonly IconId[] methodIconTable = {AstStockIcons.Method, AstStockIcons.PrivateMethod, AstStockIcons.ProtectedMethod, AstStockIcons.InternalMethod};
		static readonly IconId[] extensionMethodIconTable = {AstStockIcons.ExtensionMethod, AstStockIcons.PrivateExtensionMethod, AstStockIcons.ProtectedExtensionMethod, AstStockIcons.InternalExtensionMethod};
		static readonly IconId[] propertyIconTable = {AstStockIcons.Property, AstStockIcons.PrivateProperty, AstStockIcons.ProtectedProperty, AstStockIcons.InternalProperty};
		static readonly IconId[] eventIconTable = {AstStockIcons.Event, AstStockIcons.PrivateEvent, AstStockIcons.ProtectedEvent, AstStockIcons.InternalEvent};
		
		static int ModifierToOffset (Accessibility acc)
		{
			if ((acc & Accessibility.Private) == Accessibility.Private)
				return 1;
			if ((acc & Accessibility.Protected) == Accessibility.Protected)
				return 2;
			if ((acc & Accessibility.Internal) == Accessibility.Internal)
				return 3;
			return 0;
		}
		
		public static string GetStockIcon (this EntityDeclaration element)
		{
			Accessibility acc = Accessibility.None;
			// type accessibility
			acc = Accessibility.Internal;
			if (element.HasModifier (Modifiers.Public)) {
				acc = Accessibility.Public;
			} else if (element.HasModifier (Modifiers.Protected)) {
				acc = Accessibility.Protected;
			} else if (element.HasModifier (Modifiers.Private)) {
				acc = Accessibility.Private;
			}
			
			if (element is TypeDeclaration) {
				var type = element as TypeDeclaration;
				switch (type.ClassType) {
				case ClassType.Class:
					return typeIconTable [0, ModifierToOffset (acc)];
				case ClassType.Struct:
					return typeIconTable [3, ModifierToOffset (acc)];
				case ClassType.Interface:
					return typeIconTable [2, ModifierToOffset (acc)];
				case ClassType.Enum:
					return typeIconTable [1, ModifierToOffset (acc)];
				default:
					throw new ArgumentOutOfRangeException ();
				}
			}
			if (element is DelegateDeclaration)
				return typeIconTable [4, ModifierToOffset (acc)];

			// member accessibility
			acc = Accessibility.Private;
			if (element.HasModifier (Modifiers.Public)) {
				acc = Accessibility.Public;
			} else if (element.HasModifier (Modifiers.Protected)) {
				acc = Accessibility.Protected;
			} else if (element.HasModifier (Modifiers.Internal)) {
				acc = Accessibility.Internal;
			}

			if (element is MethodDeclaration) {
				var method = element as MethodDeclaration;
				if (method.IsExtensionMethod)
					return extensionMethodIconTable [ModifierToOffset (acc)];
				return methodIconTable [ModifierToOffset (acc)];
			}
			if (element is OperatorDeclaration || element is ConstructorDeclaration || element is DestructorDeclaration || element is Accessor)
				return methodIconTable [ModifierToOffset (acc)];

			if (element is PropertyDeclaration)
				return propertyIconTable [ModifierToOffset (acc)];
			if (element is EventDeclaration || element is CustomEventDeclaration)
				return eventIconTable [ModifierToOffset (acc)];

			return fieldIconTable [ModifierToOffset (acc)];
		}
	}

}
