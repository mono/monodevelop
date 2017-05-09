//
// SearchPackagesSearchCategory.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using Xwt.Drawing;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement
{
	internal class SearchPackagesSearchCategory : SearchCategory
	{
		public SearchPackagesSearchCategory ()
			: base (GettextCatalog.GetString("Search"))
		{
		}
		public override Task GetResults (ISearchResultCallback searchResultCallback, SearchPopupSearchPattern pattern, CancellationToken token)
		{
			if (IsProjectSelected ()) {
				searchResultCallback.ReportResult (new SearchPackageSearchResult (pattern));
			}
			return Task.CompletedTask;
		}

		class SearchPackageSearchResult : SearchResult
		{
			SearchPopupSearchPattern pattern;

			public override bool CanActivate {
				get {
					return IsProjectSelected ();
				}
			}

			public SearchPackageSearchResult (SearchPopupSearchPattern pattern) : base ("", "", 0)
			{
				this.pattern = pattern;
			}

			public override void Activate ()
			{
				var runner = new AddPackagesDialogRunner ();
				runner.Run (pattern.UnparsedPattern);
			}

			public override string GetMarkupText (bool selected)
			{
				return GettextCatalog.GetString ("Search Packages...");
			}
		}

		static bool IsProjectSelected ()
		{
			return PackageManagementServices.ProjectService.CurrentProject != null;
		}

		static readonly string [] tags = { "search" };

		public override string [] Tags {
			get {
				return tags;
			}
		}

		public override bool IsValidTag (string tag)
		{
			return tag == "search";
		}
	}
}

