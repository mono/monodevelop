// 
// RoslynSearchCategory.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//       Marius Ungureanu <maungu@microsoft.com>
// 
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Components.MainToolbar
{
	sealed partial class RoslynSearchCategory : SearchCategory
	{
		public RoslynSearchCategory () : base (GettextCatalog.GetString ("Solution"))
		{
			sortOrder = FirstCategory;
		}

		static readonly string [] tags = new [] {
				// Types
				"type", "t", "class", "struct", "interface", "enum", "delegate",
				// Members
				"member", "m", "method", "property", "field", "event"
		};

		public override string [] Tags {
			get {
				return tags;
			}
		}

		public override bool IsValidTag (string tag)
		{
			return tags.Contains (tag);
		}

		static IImmutableSet<string> GetTagKinds (string tag)
		{
			var set = ImmutableHashSet.CreateBuilder<string> ();
			switch (tag) {
			case "type":
			case "t":
				set.Add (NavigateToItemKind.Class);
				set.Add (NavigateToItemKind.Delegate);
				set.Add (NavigateToItemKind.Enum);
				set.Add (NavigateToItemKind.Interface);
				set.Add (NavigateToItemKind.Structure);
				break;
			case "class":
				set.Add (NavigateToItemKind.Class);
				break;
			case "struct":
				set.Add (NavigateToItemKind.Structure);
				break;
			case "interface":
				set.Add (NavigateToItemKind.Interface);
				break;
			case "delegate":
				set.Add (NavigateToItemKind.Delegate);
				break;
			case "member":
			case "m":
				set.Add (NavigateToItemKind.Constant);
				set.Add (NavigateToItemKind.Event);
				set.Add (NavigateToItemKind.Field);
				set.Add (NavigateToItemKind.Method);
				set.Add (NavigateToItemKind.Property);
				break;
			case "method":
				set.Add (NavigateToItemKind.Method);
				break;
			case "property":
				set.Add (NavigateToItemKind.Property);
				break;
			case "field":
				set.Add (NavigateToItemKind.Field);
				set.Add (NavigateToItemKind.Constant);
				break;
			case "event":
				set.Add (NavigateToItemKind.Event);
				break;
			}

			return set.ToImmutable ();
		}

		public override Task GetResults (ISearchResultCallback searchResultCallback, SearchPopupSearchPattern searchPattern, CancellationToken token)
		{
			if (string.IsNullOrEmpty (searchPattern.Pattern))
				return Task.CompletedTask;

			if (searchPattern.Tag != null && !tags.Contains (searchPattern.Tag) || searchPattern.HasLineNumber)
				return Task.CompletedTask;
			
			return Task.Run (async delegate {
				try {
					var kinds = GetTagKinds (searchPattern.Tag);
					// Maybe use language services instead of AbstractNavigateToSearchService
					var aggregatedResults = await Task.WhenAll (TypeSystemService.AllWorkspaces
										.Select (ws => ws.CurrentSolution)
										.SelectMany (sol => sol.Projects)
										.Select (async proj => {
											using (proj.Solution.Services.CacheService?.EnableCaching (proj.Id)) {
												var searchService = proj.LanguageServices.GetService<INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate> ();
												if (searchService == null)
													return ImmutableArray<INavigateToSearchResult>.Empty;
												return await searchService.SearchProjectAsync (proj, searchPattern.Pattern, kinds, token).ConfigureAwait (false);
											}
										})
					).ConfigureAwait (false);

					foreach (var results in aggregatedResults) {
						foreach (var result in results) {
							int laneLength = result.NameMatchSpans.Length;
							int index = laneLength > 0 ? result.NameMatchSpans [0].Start : -1;

							int rank = 0;
							if (result.MatchKind == NavigateToMatchKind.Exact) {
								rank = int.MaxValue;
							} else {
								int patternLength = searchPattern.Pattern.Length;
								rank = searchPattern.Pattern.Length - result.Name.Length;
								rank -= index;

								rank -= laneLength * 100;

								// Favor matches with less splits. That is, 'abc def' is better than 'ab c def'.
								int baseRank = (patternLength - laneLength - 1) * 5000;

								// First matching letter close to the begining is better
								// The more matched letters the better
								rank = baseRank - (index + (laneLength - patternLength));

								// rank up matches which start with a filter substring
								if (index == 0)
									rank += result.NameMatchSpans [0].Length * 50;
							}

							if (!result.IsCaseSensitive)
								rank /= 2;

							searchResultCallback.ReportResult (new DeclaredSymbolInfoResult (
								searchPattern.Pattern,
								result.Name,
								rank,
								result
							));
						}
					}
				} catch {
					token.ThrowIfCancellationRequested ();
					throw;
				}
			}, token);
		}
	}
}
