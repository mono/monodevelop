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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.CSharp.Navigation;
using MonoDevelop.Ide;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Refactoring
{
	class FindDerivedSymbolsHandler : CommandHandler
	{
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

		static Task FindDerivedSymbols (ISymbol symbol, CancellationTokenSource cancellationTokenSource)
		{
			if (symbol == null)
				return Task.FromResult (0);
			var solution = IdeApp.Workbench.ActiveDocument?.AnalysisDocument?.Project?.Solution ?? TypeSystemService.Workspace.CurrentSolution;
			return Task.Run (async delegate {
				var searchMonitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
				using (var monitor = searchMonitor.WithCancellationSource (cancellationTokenSource)) {
					IEnumerable<ISymbol> result;
					try {
						if (symbol.ContainingType != null && symbol.ContainingType.TypeKind == TypeKind.Interface) {
							result = await SymbolFinder.FindImplementationsAsync (symbol, solution, cancellationToken: monitor.CancellationToken).ConfigureAwait (false);
						} else if (symbol.Kind == SymbolKind.NamedType) {
							var type = (INamedTypeSymbol)symbol;
							if (type.TypeKind == TypeKind.Interface) {

								result = (await SymbolFinder.FindImplementationsAsync (type, solution, cancellationToken: monitor.CancellationToken).ConfigureAwait (false)).Cast<ISymbol> ().Concat (
									await FindInterfaceImplementaitonsAsync (type, solution, monitor.CancellationToken).ConfigureAwait (false)
								);
							} else {
								result = (await SymbolFinder.FindDerivedClassesAsync (type, solution, cancellationToken: monitor.CancellationToken).ConfigureAwait (false)).Cast<ISymbol> ();
							}
						} else {
							result = await SymbolFinder.FindOverridesAsync (symbol, solution, cancellationToken: monitor.CancellationToken).ConfigureAwait (false);
						}
						foreach (var foundSymbol in result) {
							foreach (var loc in foundSymbol.Locations) {
								monitor.CancellationToken.ThrowIfCancellationRequested ();
								searchMonitor.ReportResult (new MemberReference (foundSymbol, loc.SourceTree.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
							}
						}
					} catch (OperationCanceledException) {
					}
				}
			});
		}

		static async Task<IEnumerable<ISymbol>> FindInterfaceImplementaitonsAsync (INamedTypeSymbol type, Microsoft.CodeAnalysis.Solution currentSolution, CancellationToken token = default(CancellationToken))
		{
			var result = new List<ISymbol> ();

			foreach (var project in currentSolution.Projects) {
				var comp = await project.GetCompilationAsync (token).ConfigureAwait (false);
				foreach (var i in comp.GetAllTypesInMainAssembly (token).Where (t => t.TypeKind == TypeKind.Interface)) {
					if (i.AllInterfaces.Any (t => t.InheritsFromOrEqualsIgnoringConstruction (type)))
						result.Add (i);
				}
			}

			return result;
		}

		protected override async void Update (CommandInfo info)
		{
			var sym = await FindBaseSymbolsHandler.GetSymbolAtCaret (IdeApp.Workbench.ActiveDocument);
			info.Enabled = sym != null;
			info.Bypass = !info.Enabled;
		}

		protected override async void Run (object dataItem)
		{
			var metadata = Navigation.Counters.CreateNavigateToMetadata ("DerivedSymbols");
			using (var timer = Navigation.Counters.NavigateTo.BeginTiming (metadata)) {
				var sym = await FindBaseSymbolsHandler.GetSymbolAtCaret (IdeApp.Workbench.ActiveDocument);
				if (sym == null) {
					Navigation.Counters.UpdateUserFault (metadata);
					return;
				}

				using (var source = new CancellationTokenSource ()) {
					try {
						await FindDerivedSymbols (sym, source);
						Navigation.Counters.UpdateNavigateResult (metadata, true);
					} finally {
						Navigation.Counters.UpdateUserCancellation (metadata, source.Token);
					}
				}
			}
		}
	}
}

