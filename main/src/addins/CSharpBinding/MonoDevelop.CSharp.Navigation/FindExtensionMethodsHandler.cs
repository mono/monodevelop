//
// FindExtensionMethodHandler.cs
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.FindInFiles;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Components.Commands;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp.Navigation
{
	class FindExtensionMethodsHandler : CommandHandler
	{
		protected override async void Update (CommandInfo info)
		{
			var sym = await GetNamedTypeAtCaret (IdeApp.Workbench.ActiveDocument);
			info.Enabled = sym != null && sym.IsKind (SymbolKind.NamedType);
			info.Bypass = !info.Enabled;
		}

		protected async override void Run ()
		{
			var metadata = Counters.CreateNavigateToMetadata ("ExtensionMethods");
			using (var timer = Counters.NavigateTo.BeginTiming (metadata)) {
				var doc = IdeApp.Workbench.ActiveDocument;
				var sym = await GetNamedTypeAtCaret (doc);
				if (sym == null) {
					Counters.UpdateUserFault (metadata);
					return;
				}

				using (var source = new CancellationTokenSource ()) {
					try {
						FindExtensionMethods (await doc.GetCompilationAsync (), sym, source);
						Counters.UpdateNavigateResult (metadata, true);
					} finally {
						Counters.UpdateUserCancellation (metadata, source.Token);
					}
				}
			}
		}

		internal static async System.Threading.Tasks.Task<INamedTypeSymbol> GetNamedTypeAtCaret (Ide.Gui.Document doc)
		{
			if (doc == null)
				return null;
			var info = await RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor);
			var sym = info.Symbol ?? info.DeclaredSymbol;
			return sym as INamedTypeSymbol;
		}

		void FindExtensionMethods (Compilation compilation, ISymbol sym, CancellationTokenSource cancellationTokenSource)
		{
			var symType = sym as ITypeSymbol;
			if (symType == null)
				return;

			var searchMonitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			using (var monitor = searchMonitor.WithCancellationSource (cancellationTokenSource)) {
				foreach (var type in compilation.Assembly.GlobalNamespace.GetAllTypes (monitor.CancellationToken)) {
					if (!type.MightContainExtensionMethods)
						continue;

					foreach (var extMethod in type.GetMembers ().OfType<IMethodSymbol> ().Where (method => method.IsExtensionMethod)) {
						if (monitor.CancellationToken.IsCancellationRequested)
							break;

						var reducedMethod = extMethod.ReduceExtensionMethod (symType);
						if (reducedMethod != null) {
							var loc = extMethod.Locations.First ();
							searchMonitor.ReportResult (new MemberReference (extMethod, loc.SourceTree.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
						}
					}
				}
			}
		}
	}
}