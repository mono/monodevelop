//
// FakeServiceBasedRepository.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeServiceBasedRepository : FakePackageRepository, IServiceBasedRepository
	{
		Dictionary<string, List<IPackage>> repositoryPackages = new Dictionary<string, List<IPackage>> ();

		public IQueryable<IPackage> Search (string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions)
		{
			string key = GetKey (searchTerm, allowPrereleaseVersions);
			if (repositoryPackages.ContainsKey (key)) {
				return repositoryPackages [key].AsQueryable ();
			}
			return new List<IPackage> ().AsQueryable (); 
		}

		string GetKey (string searchTerm, bool allowPrereleaseVersions)
		{
			return searchTerm + allowPrereleaseVersions.ToString ();
		}

		public void PackagesToReturnForSearch (string search, bool allowPrereleaseVersions, IEnumerable<IPackage> packages)
		{
			string key = GetKey (search, allowPrereleaseVersions);
			repositoryPackages.Add (key, packages.ToList ());
		}

		public Func<IEnumerable<IPackageName>, bool, bool, IEnumerable<FrameworkName>, IEnumerable<IVersionSpec>, IEnumerable<IPackage>> GetUpdatesAction;

		public IEnumerable<IPackage> GetUpdates (IEnumerable<IPackageName> packages, bool includePrerelease, bool includeAllVersions, IEnumerable<FrameworkName> targetFrameworks, IEnumerable<IVersionSpec> versionConstraints)
		{
			return GetUpdatesAction (packages, includePrerelease, includeAllVersions, targetFrameworks, versionConstraints);
		}
	}
}

