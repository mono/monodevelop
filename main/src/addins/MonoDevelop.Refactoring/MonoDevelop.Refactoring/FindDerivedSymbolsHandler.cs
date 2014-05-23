//
// FindDerivedSymbolsHandler.cs
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.FindInFiles;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using System.Threading;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Refactoring
{
	class FindDerivedSymbolsHandler : CommandHandler
	{
		public bool IsValid {
			get {
				return true;
			}
		}

		public static bool CanFindDerivedSymbols (ISymbol symbol, out string description)
		{
			if (symbol.Kind == SymbolKind.NamedType) {
				var type = (ITypeSymbol)symbol;
				description = type.TypeKind == TypeKind.Interface ? GettextCatalog.GetString ("Find Implementing Types") : GettextCatalog.GetString ("Find Derived Types");
				return !type.IsStatic && !type.IsSealed;
			}
			if (symbol.ContainingType != null && symbol.ContainingType.TypeKind == TypeKind.Interface) {
 				description = GettextCatalog.GetString ("Find Implementing Symbols");
			} else {
 				description = GettextCatalog.GetString ("Find Derived Symbols");
			}
			return symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride;
		}

		public static void FindDerivedSymbols (ISymbol symbol)
		{
			Task.Factory.StartNew (delegate {
				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
					IEnumerable<ISymbol> task;

					if (symbol.ContainingType != null && symbol.ContainingType.TypeKind == TypeKind.Interface) {
						task = SymbolFinder.FindImplementationsAsync (symbol, RoslynTypeSystemService.Workspace.CurrentSolution).Result; 
					} else if (symbol.Kind == SymbolKind.NamedType) {
						var type = (INamedTypeSymbol)symbol;
						if (type.TypeKind == TypeKind.Interface) {
							task = SymbolFinder.FindImplementationsAsync (symbol, RoslynTypeSystemService.Workspace.CurrentSolution).Result; 
						} else {
							task = type.FindDerivedClassesAsync (RoslynTypeSystemService.Workspace.CurrentSolution).Result.Cast<ISymbol> ();
						}
					} else {
						task = SymbolFinder.FindOverridesAsync (symbol, RoslynTypeSystemService.Workspace.CurrentSolution).Result;
					}
					foreach (var foundSymbol in task) {
						foreach (var loc in foundSymbol.Locations)
							monitor.ReportResult (new MemberReference (foundSymbol, loc.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
					}
				}
				
			});
		}
		
		protected async override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var info = await CurrentRefactoryOperationsHandler.GetSymbolInfoAsync (analysisDocument, doc.Editor.Caret.Offset);
			if (info.DeclaredSymbol != null)
				FindDerivedSymbols (info.DeclaredSymbol);

		}
	}
}

