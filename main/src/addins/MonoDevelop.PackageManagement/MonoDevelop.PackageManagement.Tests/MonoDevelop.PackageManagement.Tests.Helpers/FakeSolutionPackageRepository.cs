//
// FakeSolutionPackageRepository.cs
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
using ICSharpCode.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeSolutionPackageRepository : ISolutionPackageRepository
	{
		public FakeSharedPackageRepository FakeSharedRepository = new FakeSharedPackageRepository ();

		public List<FakePackage> FakePackages;

		public FakeSolutionPackageRepository ()
		{
			FakePackages = FakeSharedRepository.FakePackages;
		}

		public string InstallPathToReturn;
		public IPackage PackagePassedToGetInstallPath;

		public string GetInstallPath (IPackage package)
		{
			PackagePassedToGetInstallPath = package;
			return InstallPathToReturn;
		}

		public IEnumerable<IPackage> GetPackagesByDependencyOrder ()
		{
			return FakePackages;
		}

		public List<FakePackage> FakePackagesByReverseDependencyOrder = new List<FakePackage> ();

		public IEnumerable<IPackage> GetPackagesByReverseDependencyOrder ()
		{
			return FakePackagesByReverseDependencyOrder;
		}

		public bool IsInstalled (IPackage package)
		{
			return FakeSharedRepository.FakePackages.Exists (p => p == package);
		}

		public virtual IQueryable<IPackage> GetPackages ()
		{
			return FakeSharedRepository.FakePackages.AsQueryable ();
		}

		public ISharedPackageRepository Repository {
			get { return FakeSharedRepository; }
		}

		public IFileSystem FileSystem { get; set; }

		public IPackagePathResolver PackagePathResolver { get; set; }

		public bool IsRestored (PackageReference packageReference)
		{
			return FakeSharedRepository.FakePackages.Any (package => {
				return (package.Id == packageReference.Id) &&
					(package.Version == packageReference.Version);
			});
		}

		public List<PackageReference> PackageReferences = new List<PackageReference> ();

		public void AddPackageReference (string packageId, string packageVersion)
		{
			var packageReference = new PackageReference (
				packageId,
				new SemanticVersion (packageVersion),
				null,
				null,
				false,
				false);
			PackageReferences.Add (packageReference);
		}

		public IEnumerable<PackageReference> GetPackageReferences ()
		{
			return PackageReferences;
		}
	}
}

