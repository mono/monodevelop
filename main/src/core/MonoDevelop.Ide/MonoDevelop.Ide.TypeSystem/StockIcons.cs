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
		static readonly IconId Error = "gtk-dialog-error";
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
		static readonly IconId[] fieldIconTable = {Stock.Field, Stock.PrivateField, Stock.ProtectedField, Stock.InternalField};
		static readonly IconId[] methodIconTable = {Stock.Method, Stock.PrivateMethod, Stock.ProtectedMethod, Stock.InternalMethod};
		static readonly IconId[] extensionMethodIconTable = {Stock.ExtensionMethod, Stock.PrivateExtensionMethod, Stock.ProtectedExtensionMethod, Stock.InternalExtensionMethod};
		static readonly IconId[] propertyIconTable = {Stock.Property, Stock.PrivateProperty, Stock.ProtectedProperty, Stock.InternalProperty};
		static readonly IconId[] eventIconTable = {Stock.Event, Stock.PrivateEvent, Stock.ProtectedEvent, Stock.InternalEvent};
		
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
				return typeIconTable [0, ModifierToOffset (def.Accessibility)];
			case TypeKind.Enum:
				return typeIconTable [1, ModifierToOffset (def.Accessibility)];
			case TypeKind.Interface:
				return typeIconTable [2, ModifierToOffset (def.Accessibility)];
			case TypeKind.Struct:
				return typeIconTable [3, ModifierToOffset (def.Accessibility)];
			case TypeKind.Delegate:
				return typeIconTable [4, ModifierToOffset (def.Accessibility)];
			default:
				return typeIconTable [0, ModifierToOffset (def.Accessibility)];
			}
		}
		
		public static string GetStockIcon (this IUnresolvedTypeDefinition def)
		{
			switch (def.Kind) {
			case TypeKind.Class:
				return typeIconTable [0, ModifierToOffset (def.Accessibility)];
			case TypeKind.Enum:
				return typeIconTable [1, ModifierToOffset (def.Accessibility)];
			case TypeKind.Interface:
				return typeIconTable [2, ModifierToOffset (def.Accessibility)];
			case TypeKind.Struct:
				return typeIconTable [3, ModifierToOffset (def.Accessibility)];
			case TypeKind.Delegate:
				return typeIconTable [4, ModifierToOffset (def.Accessibility)];
			default:
				return typeIconTable [0, ModifierToOffset (def.Accessibility)];
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
		
		public static string GetStockIcon (this IEntity entity)
		{
			switch (entity.EntityType) {
			case EntityType.TypeDefinition:
				return GetStockIcon ((IType)entity);
			case EntityType.Field:
				return fieldIconTable [ModifierToOffset (entity.Accessibility)];
			case EntityType.Method:
			case EntityType.Constructor:
			case EntityType.Destructor:
			case EntityType.Operator:
				if (((IMethod)entity).IsExtensionMethod)
					return extensionMethodIconTable [ModifierToOffset (entity.Accessibility)];
				return methodIconTable [ModifierToOffset (entity.Accessibility)];
			case EntityType.Property:
			case EntityType.Indexer:
				return propertyIconTable [ModifierToOffset (entity.Accessibility)];
			case EntityType.Event:
				return eventIconTable [ModifierToOffset (entity.Accessibility)];
			}
			return "";
		}
		public static string GetStockIcon (this IUnresolvedEntity entity)
		{
			switch (entity.EntityType) {
			case EntityType.TypeDefinition:
				return GetStockIcon ((IUnresolvedTypeDefinition)entity);
			case EntityType.Field:
				return fieldIconTable [ModifierToOffset (entity.Accessibility)];
			case EntityType.Method:
			case EntityType.Constructor:
			case EntityType.Destructor:
			case EntityType.Operator:
				return methodIconTable [ModifierToOffset (entity.Accessibility)];
			case EntityType.Property:
			case EntityType.Indexer:
				return propertyIconTable [ModifierToOffset (entity.Accessibility)];
			case EntityType.Event:
				return eventIconTable [ModifierToOffset (entity.Accessibility)];
			}
			return "";
		}
	}
}
