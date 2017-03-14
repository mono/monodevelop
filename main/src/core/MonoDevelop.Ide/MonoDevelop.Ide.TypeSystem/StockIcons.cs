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
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.TypeSystem
{
	public static class Stock
	{
		public static readonly IconId Namespace = "md-name-space";

		public static IconId GetStockIcon (this Microsoft.CodeAnalysis.ISymbol symbol)
		{
			return "md-" + GetAccess (symbol.DeclaredAccessibility) + GetGlobal (symbol) + GetSource (symbol);
		}

		internal static string GetAccess (Accessibility accessibility)
		{
			switch (accessibility) {
			case Microsoft.CodeAnalysis.Accessibility.NotApplicable:
				return "";
			case Microsoft.CodeAnalysis.Accessibility.Private:
				return "private-";
			case Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal:
				return "ProtectedOrInternal-";
			case Microsoft.CodeAnalysis.Accessibility.Protected:
				return "protected-";
			case Microsoft.CodeAnalysis.Accessibility.Internal:
				return "internal-";
			case Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal:
				return "ProtectedOrInternal-";
			case Microsoft.CodeAnalysis.Accessibility.Public:
				return "";
			default:
				throw new ArgumentOutOfRangeException ();
			}		
		}

		static string GetGlobal (ISymbol symbol)
		{
			switch (symbol.Kind) {
			case Microsoft.CodeAnalysis.SymbolKind.NamedType:
				return "";
			case Microsoft.CodeAnalysis.SymbolKind.Field:
				var field = (IFieldSymbol)symbol;
				if (field.IsConst)
					return "";
				return symbol.IsStatic ? "static-" : "";
			case Microsoft.CodeAnalysis.SymbolKind.Event:
			case Microsoft.CodeAnalysis.SymbolKind.Method:
			case Microsoft.CodeAnalysis.SymbolKind.Property:
				return symbol.IsStatic ? "static-" : "";
			default:
				return "";
			}
		}

		static string GetSource(Microsoft.CodeAnalysis.ISymbol symbol)
		{
			switch (symbol.Kind) {
			case Microsoft.CodeAnalysis.SymbolKind.Alias:
			case Microsoft.CodeAnalysis.SymbolKind.ArrayType:
			case Microsoft.CodeAnalysis.SymbolKind.Assembly:
			case Microsoft.CodeAnalysis.SymbolKind.DynamicType:
			case Microsoft.CodeAnalysis.SymbolKind.ErrorType:
			case Microsoft.CodeAnalysis.SymbolKind.Label:

			case Microsoft.CodeAnalysis.SymbolKind.NetModule:
			case Microsoft.CodeAnalysis.SymbolKind.PointerType:
			case Microsoft.CodeAnalysis.SymbolKind.RangeVariable:
			case Microsoft.CodeAnalysis.SymbolKind.TypeParameter:
			case Microsoft.CodeAnalysis.SymbolKind.Preprocessing:
				return "field";
			case Microsoft.CodeAnalysis.SymbolKind.Parameter:
				return "variable";
			case Microsoft.CodeAnalysis.SymbolKind.Field:
				var field = (IFieldSymbol)symbol;
				if (field.IsConst)
					return "literal";
				return "field";
			case Microsoft.CodeAnalysis.SymbolKind.Local:
				var local = (ILocalSymbol)symbol;
				if (local.IsConst)
					return "literal";
				return "variable";
			case Microsoft.CodeAnalysis.SymbolKind.NamedType:
				var namedTypeSymbol = (Microsoft.CodeAnalysis.INamedTypeSymbol)symbol;
				switch (namedTypeSymbol.TypeKind) {
				case Microsoft.CodeAnalysis.TypeKind.Class:
					return "class";
				case Microsoft.CodeAnalysis.TypeKind.Delegate:
					return "delegate";
				case Microsoft.CodeAnalysis.TypeKind.Enum:
					return "enum";
				case Microsoft.CodeAnalysis.TypeKind.Interface:
					return "interface";
				case Microsoft.CodeAnalysis.TypeKind.Struct:
					return "struct";
				default:
					return "class";
				}
							          
			case Microsoft.CodeAnalysis.SymbolKind.Method:
				return "method";
			case Microsoft.CodeAnalysis.SymbolKind.Namespace:
				return "name-space";
			case Microsoft.CodeAnalysis.SymbolKind.Property:
				return "property";
			case Microsoft.CodeAnalysis.SymbolKind.Event:
				return "event";
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}
	}
}
