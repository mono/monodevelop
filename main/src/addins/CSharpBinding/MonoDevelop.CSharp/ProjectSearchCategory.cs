// 
// ProjectSearchCategory.cs
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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp
{
	class ProjectSearchCategory : SearchCategory
	{
		public ProjectSearchCategory () : base (GettextCatalog.GetString ("Solution"))
		{
			sortOrder = FirstCategory;
		}

		static bool IsType (INavigateToSearchResult result)
		{
			switch (result.Kind) {
			case NavigateToItemKind.Class:
			case NavigateToItemKind.Delegate:
			case NavigateToItemKind.Enum:
			case NavigateToItemKind.Structure:
			case NavigateToItemKind.Interface:
				return true;
			}
			return false;
		}

		static bool IsMember (INavigateToSearchResult result)
		{
			switch (result.Kind) {
			case NavigateToItemKind.Constant:
			case NavigateToItemKind.Event:
			case NavigateToItemKind.Field:
			case NavigateToItemKind.Method:
			case NavigateToItemKind.Property:
				return true;
			}
			return false;
		}

		static bool MatchesTag (string tag, INavigateToSearchResult result)
		{
			if (string.IsNullOrWhiteSpace (tag))
				return true;

			switch (tag) {
			case "type":
			case "t":
				return IsType (result);
			case "class":
				return result.Kind == NavigateToItemKind.Class;
			case "struct":
				return result.Kind == NavigateToItemKind.Structure;
			case "interface":
				return result.Kind == NavigateToItemKind.Interface;
			case "delegate":
				return result.Kind == NavigateToItemKind.Delegate;
			case "member":
			case "m":
				return IsMember (result);
			case "method":
				return result.Kind == NavigateToItemKind.Method;
			case "property":
				return result.Kind == NavigateToItemKind.Property;
			case "field":
				return result.Kind == NavigateToItemKind.Field || result.Kind == NavigateToItemKind.Constant;
			case "event":
				return result.Kind == NavigateToItemKind.Event;
			}

			return false;
		}
		static readonly string [] tags = new [] {
				// Types
				"type", "t", "class", "struct", "interface", "enum", "delegate",
				// Members
				"member", "m", "method", "property", "field", "event"
		};

		public override string[] Tags {
			get {
				return tags;
			}
		}

		public override bool IsValidTag (string tag)
		{
			return tags.Contains (tag);
		}

		public override Task GetResults (ISearchResultCallback searchResultCallback, SearchPopupSearchPattern searchPattern, CancellationToken token)
		{
			return Task.Run (async delegate {
				if (searchPattern.Tag != null && !tags.Contains (searchPattern.Tag) || searchPattern.HasLineNumber)
					return;
				try {
					// Maybe use language services instead of AbstractNavigateToSearchService
					var aggregatedResults = await Task.WhenAll (TypeSystemService.AllWorkspaces
										.Select (ws => ws.CurrentSolution)
										.SelectMany (s => s.Projects)
										.Select (proj => AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync (
												proj,
												searchPattern.Pattern,
												token))
					).ConfigureAwait (false);

					foreach (var results in aggregatedResults) {
						foreach (var result in results) {
							if (!MatchesTag (searchPattern.Tag, result))
								continue;

							int laneLength = result.NameMatchSpans.Length;
							int index = laneLength > 0 ? result.NameMatchSpans [0].Start : -1;

							int rank = 0;
							if (result.MatchKind == NavigateToMatchKind.Exact) {
								rank = int.MaxValue;
							} else if (result.MatchKind == NavigateToMatchKind.Prefix) {
								rank = int.MaxValue - (result.Name.Length - 1) * 10 - index;
							} else {
								int patternLength = searchPattern.Pattern.Length;
								rank = searchPattern.Pattern.Length - result.Name.Length;
								rank -= index;

								rank += laneLength * 100;

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
