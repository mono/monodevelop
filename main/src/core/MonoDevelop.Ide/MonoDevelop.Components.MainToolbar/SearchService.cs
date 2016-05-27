//
// SearchService.cs
//
// Author:
//       iain <iain@xamarin.com>
//
// Copyright (c) 2016 
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
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Components.MainToolbar
{
	public class SearchService : IDisposable
	{
		List<SearchCategory> categories = new List<SearchCategory> ();
		List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> results = new List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> ();
		public List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> Results {
			get {
				return results;
			}
		}

		CancellationTokenSource src;
		static readonly SearchCategory.DataItemComparer cmp = new SearchCategory.DataItemComparer ();

		public SearchPopupSearchPattern Pattern { get; private set; }

		public SearchService ()
		{
			categories.Add (new FileSearchCategory ());
			categories.Add (new CommandSearchCategory ());
			categories.Add (new SearchInSolutionSearchCategory ());
			foreach (var cat in AddinManager.GetExtensionObjects<SearchCategory> ("/MonoDevelop/Ide/SearchCategories")) {
				categories.Add (cat);
				cat.Initialize ();
			}

			categories.Sort ();
		}

		public void Dispose ()
		{
			if (src != null) {
				src.Cancel ();
			}
		}

		class SearchResultCollector : ISearchResultCallback
		{
			List<SearchResult> searchResults = new List<SearchResult> (maxItems);
			const int maxItems = 8;

			public IReadOnlyList<SearchResult> Results {
				get {
					return searchResults;
				}
			}

			public SearchCategory Category { get; private set; }

			public SearchResultCollector (SearchCategory cat)
			{
				this.Category = cat;
			}

			public Task Task { get; set; }

			#region ISearchResultCallback implementation
			void ISearchResultCallback.ReportResult (SearchResult result)
			{
				if (maxItems == searchResults.Count) {
					int i = searchResults.Count;
					while (i > 0) {
						if (cmp.Compare (result, searchResults [i - 1]) > 0)
							break;
						i--;
					}
					if (i == maxItems) {
						return;//this means it's worse then current worst
					} else {
						if (!result.IsValid)
							return;
						searchResults.RemoveAt (maxItems - 1);
						searchResults.Insert (i, result);
					}
				} else {
					if (!result.IsValid)
						return;
					int i = searchResults.Count;
					while (i > 0) {
						if (cmp.Compare (result, searchResults [i - 1]) > 0)
							break;
						i--;
					}
					searchResults.Insert (i, result);
				}
			}

			#endregion
		}
		public void Update (SearchPopupSearchPattern pattern)
		{
			// in case of 'string:' it's not clear if the user ment 'tag:pattern'  or 'pattern:line' therefore guess
			// 'tag:', if no valid tag is found guess 'pattern:'
			if (!string.IsNullOrEmpty (pattern.Tag) && string.IsNullOrEmpty (pattern.Pattern) && !categories.Any (c => c.IsValidTag (pattern.Tag))) {
				pattern = new SearchPopupSearchPattern (null, pattern.Tag, pattern.LineNumber, pattern.Column, pattern.UnparsedPattern);
			}

			Pattern = pattern;
			if (src != null)
				src.Cancel ();
			
			src = new CancellationTokenSource ();

			var collectors = new List<SearchResultCollector> ();
			var token = src.Token;
			foreach (var _cat in categories) {
				var cat = _cat;
				if (!string.IsNullOrEmpty (pattern.Tag) && !cat.IsValidTag (pattern.Tag))
					continue;
				var col = new SearchResultCollector (_cat);
				collectors.Add (col);
				col.Task = cat.GetResults (col, pattern, token);
			}

			Task.WhenAll (collectors.Select (c => c.Task)).ContinueWith (t => {
				if (token.IsCancellationRequested)
					return;
				
				var newResults = new List<Tuple<SearchCategory, IReadOnlyList<SearchResult>>> (collectors.Count);
				foreach (var col in collectors) {
					if (col.Task.IsCanceled) {
						continue;
					} else if (col.Task.IsFaulted) {
						LoggingService.LogError ($"Error getting search results for {col.Category}", col.Task.Exception);
					} else {
						newResults.Add (Tuple.Create (col.Category, col.Results));
					}
				}

				results = newResults;
				ResultsUpdated?.Invoke (this, EventArgs.Empty);
			}, token);
		}

		public event EventHandler<EventArgs> ResultsUpdated;
	}
}

