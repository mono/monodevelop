//
// FakePackageMetadataResource.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakePackageMetadataResource : PackageMetadataResource
	{
		public override Task<IEnumerable<IPackageSearchMetadata>> GetMetadataAsync (string packageId, bool includePrerelease, bool includeUnlisted, ILogger log, CancellationToken token)
		{
			var packages = packageMetadataList.Where (p => IsMatch (p, packageId, includePrerelease));
			return Task.FromResult (packages);
		}

		public override Task<IPackageSearchMetadata> GetMetadataAsync (PackageIdentity package, ILogger log, CancellationToken token)
		{
			var metadata = packageMetadataList.Where (p => IsMatch (p, package.Id, true)).FirstOrDefault ();
			return Task.FromResult (metadata);
		}

		static bool IsMatch (IPackageSearchMetadata package, string packageId, bool includePrerelease)
		{
			if (package.Identity.Id == packageId) {
				if (package.Identity.Version.IsPrerelease) {
					return includePrerelease;
				}
				return true;
			}
			return false;
		}

		List<IPackageSearchMetadata> packageMetadataList = new List<IPackageSearchMetadata> ();

		public FakePackageSearchMetadata AddPackageMetadata (string id, string version)
		{
			var metadata = new FakePackageSearchMetadata {
				Identity = new PackageIdentity (id, new NuGetVersion (version))
			};

			packageMetadataList.Add (metadata);

			return metadata;
		}
	}
}

