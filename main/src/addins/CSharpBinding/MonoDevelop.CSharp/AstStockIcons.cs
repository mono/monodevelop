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
using System.Linq;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.NavigateTo;
using MonoDevelop.Components.MainToolbar;

namespace MonoDevelop.CSharp
{
	// Copied from StockIcons.cs and extended for the c# ast (c# AST shouldn't be in Ide)
	static class AstStockIcons
	{
		static readonly IconId Class = "md-class";
		static readonly IconId Enum = "md-enum";
		static readonly IconId Event = "md-event";
		static readonly IconId Field = "md-field";
		static readonly IconId Interface = "md-interface";
		static readonly IconId Method = "md-method";
		static readonly IconId Property = "md-property";
		static readonly IconId Struct = "md-struct";
		static readonly IconId Delegate = "md-delegate";
		// static readonly IconId Constant = "md-literal";
		public static readonly IconId Namespace = "md-name-space";

			
		static void AdjustAccessibility (SyntaxTokenList modifiers, ref Accessibility acc, ref bool isStatic, ref bool result)
		{
			isStatic = modifiers.Any (mod => mod.Kind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword);
			if (modifiers.Any (mod => mod.Kind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.ProtectedKeyword) &&
				modifiers.Any (mod => mod.Kind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.InternalKeyword)) {
				acc = Accessibility.ProtectedOrInternal;
				result = true;
				return;
			}

			foreach (var mod in modifiers) {
				if (mod.Kind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword) {
					acc = Accessibility.Public;
					result = true;
					return;
				}
				if (mod.Kind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.PrivateKeyword) {
					acc = Accessibility.Private;
					result = true;
					return;
				}
				if (mod.Kind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.ProtectedKeyword) {
					acc = Accessibility.Protected;
					result = true;
					return;
				}
				if (mod.Kind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.InternalKeyword) {
					acc = Accessibility.Internal;
					result = true;
					return;
				}
			}
		}

		static bool GetAccessibility (SyntaxNode element, out Accessibility acc, out bool isStatic)
		{
			isStatic = false;
			if (element.Parent is TypeDeclarationSyntax && element.Parent is InterfaceDeclarationSyntax) {
				acc = Accessibility.Public;
				return true;
			}
			bool result = false;
			acc = Accessibility.Private;
			if (element is TypeDeclarationSyntax && !(element.Parent is TypeDeclarationSyntax))
				acc = Accessibility.Internal;

			if (element is VariableDeclaratorSyntax)
				element = element.Parent.Parent;
			
			if (element is TypeDeclarationSyntax)
				AdjustAccessibility (((TypeDeclarationSyntax)element).Modifiers, ref acc, ref isStatic, ref result);
			if (element is BaseFieldDeclarationSyntax)
				AdjustAccessibility (((BaseFieldDeclarationSyntax)element).Modifiers, ref acc, ref isStatic, ref result);
			if (element is BasePropertyDeclarationSyntax)
				AdjustAccessibility (((BasePropertyDeclarationSyntax)element).Modifiers, ref acc, ref isStatic, ref result);
			if (element is BaseMethodDeclarationSyntax)
				AdjustAccessibility (((BaseMethodDeclarationSyntax)element).Modifiers, ref acc, ref isStatic, ref result);
			if (element is EnumDeclarationSyntax)
				AdjustAccessibility (((EnumDeclarationSyntax)element).Modifiers, ref acc, ref isStatic, ref result);
			if (element is DelegateDeclarationSyntax)
				AdjustAccessibility (((DelegateDeclarationSyntax)element).Modifiers, ref acc, ref isStatic, ref result);
			
			return result;
		}

		static bool IsConst (SyntaxTokenList modifiers)
		{
			return modifiers.Any (mod => mod.Kind () == Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstKeyword);
		}

		static bool IsConst (SyntaxNode element)
		{
			if (element is BaseFieldDeclarationSyntax)
				return IsConst (((BaseFieldDeclarationSyntax)element).Modifiers);
			if (element is LocalDeclarationStatementSyntax)
				return IsConst (((LocalDeclarationStatementSyntax)element).Modifiers);
			return false;
		}
		public static string GetStockIcon (this SyntaxNode element)
		{
			Accessibility acc = Accessibility.Public;
			bool isStatic = false;
			if (element is NamespaceDeclarationSyntax)
				return Namespace;
			
			if (element is AccessorDeclarationSyntax) {
				if (!GetAccessibility ((MemberDeclarationSyntax)element, out acc, out isStatic))
					GetAccessibility (element.Parent as MemberDeclarationSyntax, out acc, out isStatic);

				return "md-" + GetAccess (acc) + "method";
			}
			
			GetAccessibility (element, out acc, out isStatic);

			if (element is EnumDeclarationSyntax) {
				return "md-" + GetAccess (acc) + "enum";
			}

			if (element is TypeDeclarationSyntax) {
				var type = element as TypeDeclarationSyntax;
				switch (type.Keyword.Kind ()) {
				case SyntaxKind.ClassKeyword:
					return "md-" + GetAccess (acc) + "class";
				case SyntaxKind.StructKeyword:
					return "md-" + GetAccess (acc) + "struct";
				case SyntaxKind.InterfaceKeyword:
					return "md-" + GetAccess (acc) + "interface";
				case SyntaxKind.EnumKeyword:
					return "md-" + GetAccess (acc) + "enum";
				default:
					throw new ArgumentOutOfRangeException ();
				}
			}
			if (element is DelegateDeclarationSyntax)
				return "md-" + GetAccess (acc) + "delegate";

			// member accessibility
			GetAccessibility (element, out acc, out isStatic);

			if (element is BaseMethodDeclarationSyntax) {
				// TODO!
				// var method = element as MethodDeclarationSyntax;
				//				if (method.ParameterList.Parameters.First ())
				//	return extensionMethodIconTable [(int) (acc)];


				return "md-" + GetAccess (acc) + GetGlobal (isStatic) + "method";
			}

			if (element is PropertyDeclarationSyntax || element is IndexerDeclarationSyntax)
				return "md-" + GetAccess (acc) + GetGlobal (isStatic) + "property";
			if (element is EventDeclarationSyntax || element is EventFieldDeclarationSyntax)
				return "md-" + GetAccess (acc) + GetGlobal (isStatic) + "event";
			if (element is EnumMemberDeclarationSyntax)
				return "md-literal";
			if (element?.Parent?.Parent is FieldDeclarationSyntax || element?.Parent?.Parent is LocalDeclarationStatementSyntax) {
				if (IsConst (element.Parent.Parent))
					return "md-" + GetAccess (acc) + "literal";
			}

			if (element is FieldDeclarationSyntax || element is LocalDeclarationStatementSyntax) {
				if (IsConst (element))
					return "md-" + GetAccess (acc) + "literal";
			}

			return "md-" + GetAccess (acc) + GetGlobal (isStatic) + "field";
		}

		static string GetGlobal (bool isStatic)
		{
			return isStatic ? "static-" : "";
		}

		static string GetAccess (Accessibility acc)
		{
			return MonoDevelop.Ide.TypeSystem.Stock.GetAccess (acc);
		}

		internal static IconId GetStockIconForNavigableItem (this INavigateToSearchResult item)
		{
			switch (item.Kind) {
			case NavigateToItemKind.Class:
				return Class;

			case NavigateToItemKind.Delegate:
				return Delegate;

			case NavigateToItemKind.Event:
				return Event;

			case NavigateToItemKind.Enum:
				return Enum;

			case NavigateToItemKind.Constant:
			case NavigateToItemKind.Field:
			case NavigateToItemKind.EnumItem:
				return Field;

			case NavigateToItemKind.Interface:
				return Interface;

			case NavigateToItemKind.Method:
			case NavigateToItemKind.Module:
				return Method;

			case NavigateToItemKind.Property:
				return Property;

			case NavigateToItemKind.Structure:
				return Struct;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		internal static string GetDisplayStringForNavigableItem (this INavigateToSearchResult item, string loc)
		{
			switch (item.Kind) {
			case NavigateToItemKind.Class:
				return GettextCatalog.GetString ("class ({0})", loc);

			case NavigateToItemKind.Delegate:
				return GettextCatalog.GetString ("delegate ({0})", loc);

			case NavigateToItemKind.Enum:
				return GettextCatalog.GetString ("enumeration ({0})", loc);

			case NavigateToItemKind.Event:
				return GettextCatalog.GetString ("event ({0})", loc);


			case NavigateToItemKind.Constant:
			case NavigateToItemKind.Field:
				return GettextCatalog.GetString ("field ({0})", loc);

			case NavigateToItemKind.EnumItem:
				return GettextCatalog.GetString ("enum member ({0})", loc);

			case NavigateToItemKind.Interface:
				return GettextCatalog.GetString ("interface ({0})", loc);

			case NavigateToItemKind.Method:
			case NavigateToItemKind.Module:
				return GettextCatalog.GetString ("method ({0})", loc);

			case NavigateToItemKind.Property:
				return GettextCatalog.GetString ("property ({0})", loc);

			case NavigateToItemKind.Structure:
				return GettextCatalog.GetString ("struct ({0})", loc);
			default:
				return GettextCatalog.GetString ("symbol ({0})", loc);
			}
		}
	}
}
