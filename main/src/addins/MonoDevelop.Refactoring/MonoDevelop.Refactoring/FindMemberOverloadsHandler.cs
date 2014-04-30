//
// FindMemberOverloadsHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.FindInFiles;
using Mono.TextEditor;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;

namespace MonoDevelop.Refactoring
{
	static class FindMemberOverloadsHandler
	{
		public static bool CanFindMemberOverloads (ISymbol symbol, out string description)
		{
			switch (symbol.Kind) {
			case SymbolKind.Method:
				description = GettextCatalog.GetString ("Find Method Overloads");
				return symbol.ContainingType.GetMembers (symbol.Name).OfType<IMethodSymbol> ().Count () > 1;
			case SymbolKind.Property:
				description = GettextCatalog.GetString ("Find Indexer Overloads");
				return symbol.ContainingType.GetMembers ().OfType<IPropertySymbol> () .Where (p => p.IsIndexer).Count () > 1;
			default:
				description = null;
				return false;
			}
		}

		public static void FindOverloads (ISymbol symbol)
		{
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
				switch (symbol.Kind) {
				case SymbolKind.Method:
					foreach (var method in symbol.ContainingType.GetMembers (symbol.Name).OfType<IMethodSymbol> ()) {
						foreach (var loc in method.Locations)
							monitor.ReportResult (new MemberReference (method, loc.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
					}
					break;
				case SymbolKind.Property:
					foreach (var property in symbol.ContainingType.GetMembers ().OfType<IPropertySymbol> () .Where (p => p.IsIndexer)) {
						foreach (var loc in property.Locations)
							monitor.ReportResult (new MemberReference (property, loc.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
					}
					break;
				}
			}
		}
	}
}

