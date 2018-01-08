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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Refactoring
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

		public static async Task GotoBase (MonoDevelop.Ide.Gui.Document doc, ISymbol symbol)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");
			if (symbol == null)
				throw new ArgumentNullException ("symbol");

			var metadata = Navigation.Counters.CreateNavigateToMetadata ("Base");
			using (var timer = Navigation.Counters.NavigateTo.BeginTiming (metadata)) {
				await GotoBaseInternal (doc, symbol);
				Navigation.Counters.UpdateNavigateResult (metadata, true);
			}
		}

		static Task GotoBaseInternal (MonoDevelop.Ide.Gui.Document doc, ISymbol symbol)
		{
			switch (symbol.Kind) {
			case SymbolKind.NamedType:
				return RefactoringService.RoslynJumpToDeclaration (((ITypeSymbol)symbol).BaseType, doc.Project);
			case SymbolKind.Property:
				var property = (IPropertySymbol)symbol;
				if (property.OverriddenProperty != null)
					return RefactoringService.RoslynJumpToDeclaration (property.OverriddenProperty, doc.Project);
				else
					return RefactoringService.RoslynJumpToDeclaration (property.ExplicitInterfaceImplementations.First (), doc.Project);
			case SymbolKind.Event:
				var evt = (IEventSymbol)symbol;
				if (evt.OverriddenEvent != null)
					return RefactoringService.RoslynJumpToDeclaration (evt.OverriddenEvent, doc.Project);
				else
					return RefactoringService.RoslynJumpToDeclaration (evt.ExplicitInterfaceImplementations.First (), doc.Project);
			case SymbolKind.Method:
				var method = (IMethodSymbol)symbol;
				if (method.OverriddenMethod != null)
					return RefactoringService.RoslynJumpToDeclaration (method.OverriddenMethod, doc.Project);
				else
					return RefactoringService.RoslynJumpToDeclaration (method.ExplicitInterfaceImplementations.First (), doc.Project);
			default:
				// CanGotoBase should prevent this from happening.
				throw new ArgumentException (string.Format ("Invalid symbol.Kind {0}", symbol.Kind));
			}
		}
	}
}

