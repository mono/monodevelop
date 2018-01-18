//
// NRefactoryStock.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.Decompiler.TypeSystem;

namespace MonoDevelop.AssemblyBrowser
{
	static class NRefactoryStock
	{
		static readonly IconId Field = "md-field";
		public static readonly IconId Namespace = "md-name-space";

		public static IconId GetStockIcon (this INamedElement element)
		{
			if (element is IType)
				return ((IType)element).GetStockIcon ();
			if (element is ITypeParameter)
				return ((ITypeParameter)element).GetStockIcon ();
			if (element is IUnresolvedEntity)
				return ((IUnresolvedEntity)element).GetStockIcon ();
			return ((IEntity)element).GetStockIcon ();
		}

		public static IconId GetStockIcon (this ITypeDefinition entity)
		{
			return GetStockIcon ((IType)entity);
		}

		public static IconId GetStockIcon (this IField field)
		{
			return GetStockIcon ((IEntity)field);
		}

		public static IconId GetStockIcon (this IVariable variable)
		{
			return Field;
		}

		public static IconId GetStockIcon (this IParameter parameter)
		{
			return Field;
		}

		public static IconId GetStockIcon (this IUnresolvedTypeParameter parameter)
		{
			return Field;
		}

		public static IconId GetStockIcon (this IEntity entity, bool showAccessibility = true)
		{
			return "md-" + GetAccess (entity) + GetGlobal (entity) + GetSource (entity);
		}

		static string GetAccess (IEntity entity)
		{
			switch (entity.Accessibility) {
				case Accessibility.None:
				return "";
				case Accessibility.Private:
				return "private-";
				case Accessibility.Public:
				return "";
				case Accessibility.Protected:
				return "protected-";
				case Accessibility.Internal:
				return "internal-";
				case Accessibility.ProtectedOrInternal:
				case Accessibility.ProtectedAndInternal:
				return "ProtectedOrInternal-";
				default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		static string GetGlobal (IEntity entity)
		{
			switch (entity.SymbolKind) {
			case SymbolKind.Field:
				if (((IField)entity).IsConst)
					return "";
				return entity.IsStatic ? "static-" : "";
			case SymbolKind.Method:
			case SymbolKind.Constructor:
			case SymbolKind.Destructor:
			case SymbolKind.Operator:
			case SymbolKind.Property:
			case SymbolKind.Indexer:
				return entity.IsStatic ? "static-" : "";
			}
			return "";

		}

		static string GetSource (IEntity entity)
		{
			switch (entity.SymbolKind) {
			case SymbolKind.TypeDefinition:
				var type = (IType)entity;
				switch (type.Kind) {
				case TypeKind.Class:
					return "class";
				case TypeKind.Interface:
					return "interface";
				case TypeKind.Struct:
					return "struct";
				case TypeKind.Delegate:
					return "delegate";
				case TypeKind.Enum:
					return "enum";
				}
				return "class";
			case SymbolKind.Field:
				if (((IField)entity).IsConst)
					return "literal";
				return "field";
			case SymbolKind.Event:
				return "event";
			case SymbolKind.Method:
			case SymbolKind.Constructor:
			case SymbolKind.Destructor:
			case SymbolKind.Operator:
				return "method";
			case SymbolKind.Property:
			case SymbolKind.Indexer:
				return "property";
			}
			return "";
		}

		public static IconId GetStockIcon (this IUnresolvedEntity entity, bool showAccessibility = true)
		{
			return "md-" + GetAccess (entity) + GetGlobal (entity) + GetSource (entity);
		}

		static string GetAccess (IUnresolvedEntity entity)
		{
			switch (entity.Accessibility) {
				case Accessibility.None:
				return "";
				case Accessibility.Private:
				return "private-";
				case Accessibility.Public:
				return "";
				case Accessibility.Protected:
				return "protected-";
				case Accessibility.Internal:
				return "internal-";
				case Accessibility.ProtectedOrInternal:
				case Accessibility.ProtectedAndInternal:
				return "ProtectedOrInternal-";
				default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		static string GetGlobal (IUnresolvedEntity entity)
		{
			switch (entity.SymbolKind) {
			case SymbolKind.Field:
				var field = (IUnresolvedField)entity;
				return field.IsStatic && !field.IsConst ? "static-" : "";
			case SymbolKind.Method:
			case SymbolKind.Constructor:
			case SymbolKind.Destructor:
			case SymbolKind.Operator:
			case SymbolKind.Property:
			case SymbolKind.Indexer:
				return entity.IsStatic ? "static-" : "";
			}
			return "";
		}

		static string GetSource (IUnresolvedEntity entity)
		{
			switch (entity.SymbolKind) {
			case SymbolKind.TypeDefinition:
				var type = (IUnresolvedTypeDefinition)entity;
				switch (type.Kind) {
				case TypeKind.Class:
					return "class";
				case TypeKind.Interface:
					return "interface";
				case TypeKind.Struct:
					return "struct";
				case TypeKind.Delegate:
					return "delegate";
				case TypeKind.Enum:
					return "enum";
				}
				return "class";
			case SymbolKind.Field:
				var field = (IUnresolvedField)entity;
				return field.IsConst ? "literal" : "field";
			case SymbolKind.Event:
				return "event";
			case SymbolKind.Method:
			case SymbolKind.Constructor:
			case SymbolKind.Destructor:
			case SymbolKind.Operator:
				return "method";
			case SymbolKind.Property:
			case SymbolKind.Indexer:
				return "property";
			}
			return "";
		}
	}
}