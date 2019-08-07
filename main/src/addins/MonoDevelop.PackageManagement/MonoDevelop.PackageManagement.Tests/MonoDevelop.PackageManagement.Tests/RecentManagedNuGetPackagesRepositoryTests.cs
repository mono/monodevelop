//
// RecentManagedNuGetPackagesRepositoryTests.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet.PackageManagement.UI;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class RecentManagedNuGetPackagesRepositoryTests
	{
		RecentManagedNuGetPackagesRepository repository;

		void CreateRepository ()
		{
			repository = new RecentManagedNuGetPackagesRepository ();
		}

		ManagePackagesSearchResultViewModel AddOnePackageToRepository (string id, string packageSource)
		{
			var viewModel = CreatePackage (id);
			repository.AddPackage (viewModel, packageSource);
			return viewModel;
		}

		ManagePackagesSearchResultViewModel CreatePackage (string id)
		{
			var packageViewModel = new PackageItemListViewModel {
				Id = id
			};
			return new ManagePackagesSearchResultViewModel (null, packageViewModel);
		}

		IEnumerable<ManagePackagesSearchResultViewModel> AddTwoDifferentPackagesToRepository (string packageSource)
		{
			yield return AddOnePackageToRepository ("Test.Package.1", packageSource);
			yield return AddOnePackageToRepository ("Test.Package.2", packageSource);
		}

		IEnumerable<ManagePackagesSearchResultViewModel> AddFourDifferentPackagesToRepository (string packageSource)
		{
			yield return AddOnePackageToRepository ("Test.Package.1", packageSource);
			yield return AddOnePackageToRepository ("Test.Package.2", packageSource);
			yield return AddOnePackageToRepository ("Test.Package.3", packageSource);
			yield return AddOnePackageToRepository ("Test.Package.4", packageSource);
		}

		[Test]
		public void GetPackages_RepositoryIsEmptyAndOnePackageAdded_ReturnsPackageAdded ()
		{
			CreateRepository ();
			string packageSourceUrl = "http://test.com/nuget/v2";
			var package = AddOnePackageToRepository ("Test.Package", packageSourceUrl);

			var packages = repository.GetPackages (packageSourceUrl);

			Assert.AreEqual (package, packages.Single ());
		}

		[Test]
		public void AddPackage_NoRecentPackages_PackageMarkedAsRecentPackage ()
		{
			CreateRepository ();
			string packageSourceUrl = "http://test.com/nuget/v2";
			var package = CreatePackage ("Test");
			package.IsRecentPackage = false;
			repository.AddPackage (package, packageSourceUrl);

			Assert.IsTrue (package.IsRecentPackage);
		}

		[Test]
		public void GetPackages_RepositoryHasOnePackageAddedDifferentPackageSourceRequested_ReturnsNoPackages ()
		{
			CreateRepository ();
			string packageSourceUrl = "http://test.com/nuget/v2";
			AddOnePackageToRepository ("Test.Package", packageSourceUrl);

			var packages = repository.GetPackages ("http://another/nuget/v2");

			Assert.AreEqual (0, packages.Count ());
		}

		[Test]
		public void GetPackages_RepositoryIsEmptyAndTwoDifferentPackagesAdded_ReturnsPackagesInReverseOrderWithLastAddedFirst ()
		{
			CreateRepository ();
			string packageSourceUrl = "http://test.com/nuget/v2";
			var packagesAdded = AddTwoDifferentPackagesToRepository (packageSourceUrl);

			var packages = repository.GetPackages (packageSourceUrl);

			var expectedPackages = packagesAdded.Reverse ();

			CollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetPackages_RepositoryCanHoldThreePackagesAndFourPackagesAdded_ReturnsLastThreePackagesAddedInReverseOrder ()
		{
			CreateRepository ();
			repository.MaximumPackagesCount = 3;
			string packageSourceUrl = "http://test.com/nuget/v2";
			var packagesAdded = AddFourDifferentPackagesToRepository (packageSourceUrl);

			var packages = repository.GetPackages (packageSourceUrl);

			var expectedPackages = packagesAdded.Reverse ().Take (3);

			CollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetPackages_RepositoryIsEmptyAndSamePackageIsAddedTwice_OnePackageReturned ()
		{
			CreateRepository ();
			string packageSourceUrl = "http://test.com/nuget/v2";
			AddOnePackageToRepository ("Test", packageSourceUrl);
			var package = AddOnePackageToRepository ("Test", packageSourceUrl);

			var packages = repository.GetPackages (packageSourceUrl);

			var expectedPackages = new [] {
				package
			};

			CollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void AddPackage_RepositoryIsEmptyAndTwoPackagesAdded_BothRecentPackagesAdded ()
		{
			CreateRepository ();
			string packageSourceUrl = "http://test.com/nuget/v2";
			var package1 = AddOnePackageToRepository ("Test1", packageSourceUrl);
			var package2 = AddOnePackageToRepository ("Test2", packageSourceUrl);

			var expectedPackages = new [] {
				package2,
				package1
			};

			CollectionAssert.AreEqual (expectedPackages, repository.GetPackages (packageSourceUrl));
		}
	}
}
