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

namespace MonoDevelop.Components.MainToolbar
{
	class SearchInSolutionSearchCategory : SearchCategory
	{
		public SearchInSolutionSearchCategory () : base (GettextCatalog.GetString("Search"))
		{
		}

		public override Task<ISearchDataSource> GetResults (SearchPopupSearchPattern searchPattern, int resultsCount, CancellationToken token)
		{
			return Task.Factory.StartNew (delegate {
				return (ISearchDataSource)new SearchInSolutionDataSource (searchPattern);
			});
		}

		public override bool IsValidTag (string tag)
		{
			return tag == "search";
		}

		class SearchInSolutionDataSource : ISearchDataSource
		{
			readonly SearchPopupSearchPattern searchPattern;

			public SearchInSolutionDataSource (SearchPopupSearchPattern searchPattern)
			{
				this.searchPattern = searchPattern;
			}

			#region ISearchDataSource implementation

			Xwt.Drawing.Image ISearchDataSource.GetIcon (int item)
			{
				return null;
			}

			string ISearchDataSource.GetMarkup (int item, bool isSelected)
			{
				return GettextCatalog.GetString ("Search in Solution");
			}

			string ISearchDataSource.GetDescriptionMarkup (int item, bool isSelected)
			{
				return null;
			}

			Task<MonoDevelop.Ide.CodeCompletion.TooltipInformation> ISearchDataSource.GetTooltip (CancellationToken token, int item)
			{
				return Task.FromResult<MonoDevelop.Ide.CodeCompletion.TooltipInformation> (null);
			}

			double ISearchDataSource.GetWeight (int item)
			{
				return 0;
			}

			ISegment ISearchDataSource.GetRegion (int item)
			{
				return TextSegment.Invalid;
			}

			string ISearchDataSource.GetFileName (int item)
			{
				return null;
			}

			bool ISearchDataSource.CanActivate (int item)
			{
				return true;
			}

			void ISearchDataSource.Activate (int item)
			{
				var options = new FilterOptions ();
				if (PropertyService.Get ("AutoSetPatternCasing", true))
					options.CaseSensitive = searchPattern.Pattern.Any (c => char.IsUpper (c));
				FindInFilesDialog.SearchReplace (searchPattern.Pattern, null, new WholeSolutionScope (), options, null);
			}

			int ISearchDataSource.ItemCount {
				get {
					return 1;
				}
			}
			#endregion
		}
	}
}

