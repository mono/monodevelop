//
// FindBaseSymbolsHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Components.Commands;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp.Navigation
{
	class FindBaseSymbolsHandler : CommandHandler
	{
		Task FindSymbols (ISymbol sym, CancellationTokenSource cancellationTokenSource)
		{
			if (sym == null)
				return Task.FromResult (0);
			return Task.Run (delegate {
				var searchMonitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
				using (var monitor = searchMonitor.WithCancellationSource (cancellationTokenSource)) {
					var foundSymbol = sym.OverriddenMember ();
					while (foundSymbol != null) {
						foreach (var loc in foundSymbol.Locations) {
							if (monitor.CancellationToken.IsCancellationRequested)
								return;

							if (loc.SourceTree == null)
								continue;
							
							searchMonitor.ReportResult (new MemberReference (foundSymbol, loc.SourceTree.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
						}
						foundSymbol = foundSymbol.OverriddenMember ();
					}
				}
			});
		}

		internal static async Task<ISymbol> GetSymbolAtCaret (Ide.Gui.Document doc)
		{
			if (doc == null)
				return null;
			var info = await RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor);
			return info.Symbol ?? info.DeclaredSymbol;
		}

		protected override async void Update (CommandInfo info)
		{
			var sym = await GetSymbolAtCaret (IdeApp.Workbench.ActiveDocument);
			info.Enabled = sym != null;
			info.Bypass = !info.Enabled;
		}

		protected override async void Run ()
		{
			var metadata = Counters.CreateNavigateToMetadata ("BaseSymbols");
			using (var timer = Counters.NavigateTo.BeginTiming (metadata)) {
				var sym = await GetSymbolAtCaret (IdeApp.Workbench.ActiveDocument);
				if (sym == null) {
					Counters.UpdateUserFault (metadata);
					return;
				}

				using (var source = new CancellationTokenSource ()) {
					try {
						await FindSymbols (sym, source);
						Counters.UpdateNavigateResult (metadata, true);
					} finally {
						Counters.UpdateUserCancellation (metadata, source.Token);
					}
				}
			}
		}
	}
}

