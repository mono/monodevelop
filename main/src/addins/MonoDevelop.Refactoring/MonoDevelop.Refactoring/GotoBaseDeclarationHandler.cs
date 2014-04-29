//
// GotoBaseDeclarationHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring
{
	static class GotoBaseDeclarationHandler
	{
		public static string GetDescription (ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			switch (symbol.Kind) {
			case SymbolKind.NamedType:
				return GettextCatalog.GetString ("Go to _Base Type");
			case SymbolKind.Property:
				var property = (IPropertySymbol)symbol;
				return property.OverriddenProperty != null ? GettextCatalog.GetString ("Go to _Base Property") : GettextCatalog.GetString ("Go to _Interface Property");
			case SymbolKind.Event:
				var evt = (IEventSymbol)symbol;
				return evt.OverriddenEvent != null ? GettextCatalog.GetString ("Go to _Base Event") : GettextCatalog.GetString ("Go to _Interface Event");
			case SymbolKind.Method:
				var method = (IMethodSymbol)symbol;
				return method.OverriddenMethod != null ? GettextCatalog.GetString ("Go to _Base Method") : GettextCatalog.GetString ("Go to _Interface Method");
			}
			return GettextCatalog.GetString ("Go to _Base Symbol");
		}

		public static bool CanGotoBase (ISymbol symbol)
		{
			if (symbol == null)
				return false;
			switch (symbol.Kind) {
			case SymbolKind.NamedType:
				return true;
			case SymbolKind.Property:
				var property = (IPropertySymbol)symbol;
				return property.OverriddenProperty != null || property.ExplicitInterfaceImplementations.Length > 0;
			case SymbolKind.Event:
				var evt = (IEventSymbol)symbol;
				return evt.OverriddenEvent != null || evt.ExplicitInterfaceImplementations.Length > 0;
			case SymbolKind.Method:
				var method = (IMethodSymbol)symbol;
				return method.OverriddenMethod != null || method.ExplicitInterfaceImplementations.Length > 0;
			}
			return false;
		}

		public static void GotoBase (MonoDevelop.Ide.Gui.Document doc, ISymbol symbol)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			switch (symbol.Kind) {
			case SymbolKind.NamedType:
				IdeApp.ProjectOperations.JumpToDeclaration (((ITypeSymbol)symbol).BaseType, doc.Project);
				break;
			case SymbolKind.Property:
				var property = (IPropertySymbol)symbol;
				if (property.OverriddenProperty != null)
					IdeApp.ProjectOperations.JumpToDeclaration (property.OverriddenProperty, doc.Project);
				else
					IdeApp.ProjectOperations.JumpToDeclaration (property.ExplicitInterfaceImplementations.First (), doc.Project);
				break;
			case SymbolKind.Event:
				var evt = (IEventSymbol)symbol;
				if (evt.OverriddenEvent != null)
					IdeApp.ProjectOperations.JumpToDeclaration (evt.OverriddenEvent, doc.Project);
				else
					IdeApp.ProjectOperations.JumpToDeclaration (evt.ExplicitInterfaceImplementations.First (), doc.Project);
				break;
			case SymbolKind.Method:
				var method = (IMethodSymbol)symbol;
				if (method.OverriddenMethod != null)
					IdeApp.ProjectOperations.JumpToDeclaration (method.OverriddenMethod, doc.Project);
				else
					IdeApp.ProjectOperations.JumpToDeclaration (method.ExplicitInterfaceImplementations.First (), doc.Project);
				break;
			}
		}
	}
}

