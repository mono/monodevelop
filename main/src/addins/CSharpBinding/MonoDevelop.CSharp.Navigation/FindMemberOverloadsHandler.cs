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

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Navigation
{
	class FindMemberOverloadsHandler : CommandHandler
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

		public static void FindOverloads (ISymbol symbol, CancellationTokenSource cancellationTokenSource)
		{
			var searchMonitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			using (var monitor = searchMonitor.WithCancellationSource (cancellationTokenSource)) {
				switch (symbol.Kind) {
				case SymbolKind.Method:
					foreach (var method in symbol.ContainingType.GetMembers (symbol.Name).OfType<IMethodSymbol> ()) {
						foreach (var loc in method.Locations) {
							if (monitor.CancellationToken.IsCancellationRequested)
								return;
							
							searchMonitor.ReportResult (new MemberReference (method, loc.SourceTree.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
						}
					}
					break;
				case SymbolKind.Property:
					foreach (var property in symbol.ContainingType.GetMembers ().OfType<IPropertySymbol> () .Where (p => p.IsIndexer)) {
						foreach (var loc in property.Locations) {
							if (monitor.CancellationToken.IsCancellationRequested)
								return;
							
							searchMonitor.ReportResult (new MemberReference (property, loc.SourceTree.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
						}
					}
					break;

				}
			}
		}

		protected override async void Update (CommandInfo info)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null) {
				info.Enabled = false;
				return;
			}
			var symInfo = await RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor);
			var sym = symInfo.Symbol ?? symInfo.DeclaredSymbol;
			info.Enabled = sym != null && (sym.IsKind (SymbolKind.Method) || sym.IsKind (SymbolKind.Property) && ((IPropertySymbol)sym).IsIndexer);
			info.Bypass = !info.Enabled;
		}

		protected async override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;

			var metadata = Counters.CreateNavigateToMetadata ("MemberOverloads");
			using (var timer = Counters.NavigateTo.BeginTiming (metadata)) {

				var info = await RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor);
				var sym = info.Symbol ?? info.DeclaredSymbol;
				if (sym == null) {
					Counters.UpdateUserFault (metadata);
					return;
				}

				using (var source = new CancellationTokenSource ()) {
					try {
						FindOverloads (sym, source);
						Counters.UpdateNavigateResult (metadata, true);
					} finally {
						Counters.UpdateUserCancellation (metadata, source.Token);
					}
				}
			}
		}
	}
}