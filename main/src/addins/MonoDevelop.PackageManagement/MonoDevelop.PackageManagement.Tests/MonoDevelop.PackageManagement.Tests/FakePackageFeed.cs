//
// FakePackageFeed.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.PackageManagement.UI;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.Tests
{
	class FakePackageFeed : IPackageFeed
	{
		public bool IsMultiSource {
			get { return true; }
		}

		public List<PackageIdentity> Packages = new List<PackageIdentity> ();

		public void AddPackage (string id, string version)
		{
			var package = new PackageIdentity (id, new NuGetVersion (version));
			Packages.Add (package);
		}

		public string SearchText;
		public SearchFilter SearchFilter;

		public Task<SearchResult<IPackageSearchMetadata>> SearchAsync (string searchText, SearchFilter filter, CancellationToken cancellationToken)
		{
			var results = new SearchResult<IPackageSearchMetadata> {
				RefreshToken = new RefreshToken { },
				SourceSearchStatus = new Dictionary<string, LoadingStatus> {
					{ "Test", LoadingStatus.Loading }
				}
			};

			SearchText = searchText;
			SearchFilter = filter;

			return Task.FromResult (results);
		}

		public Task<SearchResult<IPackageSearchMetadata>> RefreshSearchAsync (RefreshToken refreshToken, CancellationToken cancellationToken)
		{
			var items = Packages
				.Select (package => PackageSearchMetadataBuilder.FromIdentity (package).Build ())
				.ToArray ();

			var results = SearchResult.FromItems (items);
			results.NextToken = new ContinuationToken { };
			results.SourceSearchStatus = new Dictionary<string, LoadingStatus> {
				{ "Test", LoadingStatus.Ready }
			};
			return Task.FromResult (results);
		}

		public Task<SearchResult<IPackageSearchMetadata>> ContinueSearchAsync (ContinuationToken continuationToken, CancellationToken cancellationToken)
		{
			var results =  new SearchResult<IPackageSearchMetadata> ();
			results.SourceSearchStatus = new Dictionary<string, LoadingStatus> {
				{ "Test", LoadingStatus.NoMoreItems }
			};
			return Task.FromResult (results);
		}
	}
}

