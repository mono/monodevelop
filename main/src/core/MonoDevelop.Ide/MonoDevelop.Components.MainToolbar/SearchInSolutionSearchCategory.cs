//
// SearchInSolutionSearchCategory.cs
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

using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.FindInFiles;
using System.Linq;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.CodeCompletion;
using Roslyn.Utilities;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.MainToolbar
{
	class SearchInSolutionSearchCategory : SearchCategory
	{
		public SearchInSolutionSearchCategory () : base (GettextCatalog.GetString("Search"))
		{
		}

		public override Task GetResults (ISearchResultCallback searchResultCallback, SearchPopupSearchPattern pattern, CancellationToken token)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedSolution != null)
				searchResultCallback.ReportResult (new SearchInSolutionSearchResult (pattern));
			return Task.CompletedTask;
		}

		//public override Task<ISearchDataSource> GetResults (SearchPopupSearchPattern searchPattern, int resultsCount, CancellationToken token)
		//{
		//	return Task.Factory.StartNew (delegate {
		//		return (ISearchDataSource)new SearchInSolutionDataSource (searchPattern);
		//	});
		//} 
		static readonly string[] tags = { "search" };

		public override string[] Tags {
			get {
				return tags;
			}
		}

		public override bool IsValidTag (string tag)
		{
			return tag == "search";
		}

		class SearchInSolutionSearchResult : SearchResult
		{
			SearchPopupSearchPattern pattern;

			public override bool CanActivate {
				get {
					return true;
				}
			}

			public SearchInSolutionSearchResult (SearchPopupSearchPattern pattern) : base ("", "", 0)
			{
				this.pattern = pattern;
			}

			public override void Activate ()
			{
				var options = new FilterOptions ();
				if (PropertyService.Get ("AutoSetPatternCasing", true))
					options.CaseSensitive = pattern.Pattern.Any (char.IsUpper);
				FindInFilesDialog.SearchReplace (pattern.Pattern, null, new WholeSolutionScope (), options, null, null);
			}

			public override string GetMarkupText (bool selected)
			{
				return GettextCatalog.GetString ("Search in Solution...");
			}
		}
	}
}
