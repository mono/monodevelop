//
// FakePackageSearchMetadata.cs
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakePackageSearchMetadata : IPackageSearchMetadata
	{
		public string Authors { get; set; }

		public List<PackageDependencyGroup> DependencySetsList = new List<PackageDependencyGroup> ();

		public IEnumerable<PackageDependencyGroup> DependencySets {
			get { return DependencySetsList; }
		}

		public string Description { get; set; }

		public long? DownloadCount { get; set; }

		public Uri IconUrl { get; set; }

		public PackageIdentity Identity { get; set; }

		public Uri LicenseUrl { get; set; }

		public string Owners { get; set; }

		public Uri ProjectUrl { get; set; }

		public DateTimeOffset? Published { get; set; }

		public Uri ReportAbuseUrl { get; set; }

		public bool RequireLicenseAcceptance { get; set; }

		public string Summary { get; set; }

		public string Tags { get; set; }

		public string Title { get; set; }

		public bool IsListed { get; set; }

		public Task<IEnumerable<VersionInfo>> GetVersionsAsync ()
		{
			throw new NotImplementedException ();
		}
	}
}

