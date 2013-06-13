// 
// StockIcons.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Core;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;

namespace MonoDevelop.Ide.TypeSystem
{
	public static class Stock
	{
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

		static readonly IconId InternalAndProtectedClass = "md-InternalAndProtected-class";
		static readonly IconId InternalAndProtectedDelegate = "md-InternalAndProtected-delegate";
		static readonly IconId InternalAndProtectedEnum = "md-InternalAndProtected-enum";
		static readonly IconId InternalAndProtectedEvent = "md-InternalAndProtected-event";
		static readonly IconId InternalAndProtectedField = "md-InternalAndProtected-field";
		static readonly IconId InternalAndProtectedInterface = "md-InternalAndProtected-interface";
		static readonly IconId InternalAndProtectedMethod = "md-InternalAndProtected-method";
		static readonly IconId InternalAndProtectedExtensionMethod = "md-InternalAndProtected-extensionmethod";
		static readonly IconId InternalAndProtectedProperty = "md-InternalAndProtected-property";
		static readonly IconId InternalAndProtectedStruct = "md-InternalAndProtected-struct";

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
		
		static readonly IconId ProtectedOrInternalClass = "md-ProtectedOrInternal-class";
		static readonly IconId ProtectedOrInternalDelegate = "md-ProtectedOrInternal-delegate";
		static readonly IconId ProtectedOrInternalEnum = "md-ProtectedOrInternal-enum";
		static readonly IconId ProtectedOrInternalEvent = "md-ProtectedOrInternal-event";
		static readonly IconId ProtectedOrInternalField = "md-ProtectedOrInternal-field";
		static readonly IconId ProtectedOrInternalInterface = "md-ProtectedOrInternal-interface";
		static readonly IconId ProtectedOrInternalMethod = "md-ProtectedOrInternal-method";
		static readonly IconId ProtectedOrInternalExtensionMethod = "md-ProtectedOrInternal-extensionmethod";
		static readonly IconId ProtectedOrInternalProperty = "md-ProtectedOrInternal-property";
		static readonly IconId ProtectedOrInternalStruct = "md-ProtectedOrInternal-struct";
		
		static IconId[,] typeIconTable = new IconId[,] {
			{Class,     PrivateClass,		Class,		ProtectedClass,     InternalClass,		ProtectedOrInternalClass, 		InternalAndProtectedClass},     // class
			{Enum,      PrivateEnum,		Enum,		ProtectedEnum,      InternalEnum,		ProtectedOrInternalEnum, 		InternalAndProtectedEnum},      // enum
			{Interface, PrivateInterface,	Interface,	ProtectedInterface, InternalInterface,	ProtectedOrInternalInterface,	InternalAndProtectedInterface}, // interface
			{Struct,    PrivateStruct,		Struct,   	ProtectedStruct,    InternalStruct,		ProtectedOrInternalStruct,		InternalAndProtectedStruct},    // struct
			{Delegate,  PrivateDelegate,	Delegate, 	ProtectedDelegate,  InternalDelegate,	ProtectedOrInternalDelegate,	InternalAndProtectedDelegate}   // delegate
		};
		static readonly IconId[] fieldIconTable = {
			Stock.Field, Stock.PrivateField, Stock.Field, Stock.ProtectedField, Stock.InternalField, Stock.ProtectedOrInternalField, Stock.InternalAndProtectedField
		};
		static readonly IconId[] methodIconTable = {
			Stock.Method, Stock.PrivateMethod, Stock.Method, Stock.ProtectedMethod, Stock.InternalMethod, Stock.ProtectedOrInternalMethod, Stock.InternalAndProtectedMethod
		};
		static readonly IconId[] extensionMethodIconTable = {
			Stock.ExtensionMethod, Stock.PrivateExtensionMethod, Stock.ExtensionMethod, Stock.ProtectedExtensionMethod, Stock.InternalExtensionMethod, Stock.ProtectedOrInternalExtensionMethod, Stock.InternalAndProtectedExtensionMethod
		};
		static readonly IconId[] propertyIconTable = {
			Stock.Property, Stock.PrivateProperty, Stock.Property, Stock.ProtectedProperty, Stock.InternalProperty, Stock.ProtectedOrInternalProperty, Stock.InternalAndProtectedProperty
		};
		static readonly IconId[] eventIconTable = {
			Stock.Event, Stock.PrivateEvent, Stock.Event, Stock.ProtectedEvent, Stock.InternalEvent, Stock.ProtectedOrInternalEvent, Stock.InternalAndProtectedEvent
		};

		public static string GetStockIcon (this INamedElement element)
		{
			if (element is IType)
				return ((IType)element).GetStockIcon ();
			if (element is ITypeParameter)
				return ((ITypeParameter)element).GetStockIcon ();
			if (element is IUnresolvedEntity)
				return ((IUnresolvedEntity)element).GetStockIcon ();
			return ((IEntity)element).GetStockIcon ();
		}
		
		public static string GetStockIcon (this ITypeDefinition entity)
		{
			return GetStockIcon ((IType)entity);
		}
		
		public static string GetStockIcon (this IType entity)
		{
			var def = entity.GetDefinition ();
			if (def == null)
				return Class;
			switch (def.Kind) {
			case TypeKind.Class:
				return typeIconTable [0, (int)def.Accessibility];
			case TypeKind.Enum:
				return typeIconTable [1, (int)def.Accessibility];
			case TypeKind.Interface:
				return typeIconTable [2, (int)def.Accessibility];
			case TypeKind.Struct:
				return typeIconTable [3, (int)def.Accessibility];
			case TypeKind.Delegate:
				return typeIconTable [4, (int)def.Accessibility];
			default:
				return typeIconTable [0, (int)def.Accessibility];
			}
		}
		
		public static string GetStockIcon (this IUnresolvedTypeDefinition def)
		{
			switch (def.Kind) {
			case TypeKind.Class:
				return typeIconTable [0, (int)def.Accessibility];
			case TypeKind.Enum:
				return typeIconTable [1, (int)def.Accessibility];
			case TypeKind.Interface:
				return typeIconTable [2, (int)def.Accessibility];
			case TypeKind.Struct:
				return typeIconTable [3, (int)def.Accessibility];
			case TypeKind.Delegate:
				return typeIconTable [4, (int)def.Accessibility];
			default:
				return typeIconTable [0, (int)def.Accessibility];
			}
		}
		
		public static string GetStockIcon (this IField field)
		{
			return GetStockIcon ((IEntity)field);
		}
		
		public static string GetStockIcon (this IVariable variable)
		{
			return Field;
		}
		
		public static string GetStockIcon (this IParameter parameter)
		{
			return Field;
		}
		
		public static string GetStockIcon (this IUnresolvedTypeParameter parameter)
		{
			return Field;
		}
		
		public static string GetStockIcon (this IEntity entity, bool showAccessibility = true)
		{
			switch (entity.EntityType) {
			case EntityType.TypeDefinition:
				return GetStockIcon ((IType)entity);
			case EntityType.Field:
				if (showAccessibility)
					return fieldIconTable [(int)entity.Accessibility];
				else
					return fieldIconTable [0];
			case EntityType.Method:
			case EntityType.Constructor:
			case EntityType.Destructor:
			case EntityType.Operator:
				if (showAccessibility) {
					if (((IMethod)entity).IsExtensionMethod)
						return extensionMethodIconTable [(int)entity.Accessibility];
					return methodIconTable [(int)entity.Accessibility];
				} else {
					if (((IMethod)entity).IsExtensionMethod)
						return extensionMethodIconTable [0];
					return methodIconTable [0];
				}
			case EntityType.Property:
			case EntityType.Indexer:
				if (showAccessibility)
					return propertyIconTable [(int)entity.Accessibility];
				else
					return propertyIconTable [0];
			case EntityType.Event:
				if (showAccessibility)
					return eventIconTable [(int)entity.Accessibility];
				else
					return eventIconTable [0];
			}
			return "";
		}
		public static string GetStockIcon (this IUnresolvedEntity entity, bool showAccessibility = true)
		{
			switch (entity.EntityType) {
			case EntityType.TypeDefinition:
				return GetStockIcon ((IUnresolvedTypeDefinition)entity);
			case EntityType.Field:
				if (showAccessibility)
					return fieldIconTable [(int)entity.Accessibility];
				else
					return fieldIconTable [0];
			case EntityType.Method:
			case EntityType.Constructor:
			case EntityType.Destructor:
			case EntityType.Operator:
				if (showAccessibility)
					return methodIconTable [(int)entity.Accessibility];
				else
					return methodIconTable [0];
			case EntityType.Property:
			case EntityType.Indexer:
				if (showAccessibility)
					return propertyIconTable [(int)entity.Accessibility];
				else
					return propertyIconTable [0];
			case EntityType.Event:
				if (showAccessibility)
					return eventIconTable [(int)entity.Accessibility];
				else
					return eventIconTable [0];
			}
			return "";
		}
	}
}
