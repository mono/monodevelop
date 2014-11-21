//
// TestableLocalPackageRepository.cs
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
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class TestableLocalPackageRepository : LocalPackageRepository
	{
		public TestableLocalPackageRepository ()
			: base (@"d:\projects\MySolution\packages".ToNativePath ())
		{
		}

		public override IEnumerable<string> GetPackageLookupPaths (string packageId, SemanticVersion version)
		{
			var packageName = new PackageName (packageId, version);
			List<string> filePaths = null;
			if (packageLookupPaths.TryGetValue (packageName, out filePaths)) {
				return filePaths;
			}
			return Enumerable.Empty<string> ();
		}

		Dictionary<PackageName, List<string>> packageLookupPaths = new Dictionary<PackageName, List<string>> ();

		public void AddPackageLookupPath (PackageName packageName, params string[] filePaths)
		{
			packageLookupPaths.Add (packageName, filePaths.ToList ());
		}
	}
}

