//
// UpdatedPackagesTests.cs
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
using ICSharpCode.PackageManagement;
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UpdatedPackagesTests
	{
		UpdatedPackages updatedPackages;
		FakeServiceBasedRepository sourceRepository;
		List<FakePackage> installedPackages;
		List<FakePackage> sourceRepositoryPackages;
		List<IPackage> packagesUsedWhenCheckingForUpdates;
		bool includePreleaseUsedWhenCheckingForUpdates;

		[SetUp]
		public void Init ()
		{
			sourceRepository = new FakeServiceBasedRepository ();
			installedPackages = new List<FakePackage> ();
			sourceRepositoryPackages = new List<FakePackage> ();
			packagesUsedWhenCheckingForUpdates = new List<IPackage> ();
		}

		void CreateUpdatedPackages ()
		{
			sourceRepository.GetUpdatesAction = (packages, includePrerelease, includeAllVersions, targetFrameworks, versionConstraints) => {
				includePreleaseUsedWhenCheckingForUpdates = includePrerelease;
				packagesUsedWhenCheckingForUpdates.AddRange (packages.Select (p => (IPackage)p));
				return sourceRepositoryPackages.AsQueryable ();
			};
			updatedPackages = new UpdatedPackages (installedPackages.AsQueryable (), sourceRepository);
		}

		FakePackage AddPackageToSourceRepository (string id, string version)
		{
			FakePackage package = CreatePackage (id, version);
			sourceRepositoryPackages.Add (package);
			return package;
		}

		FakePackage CreatePackage (string id, string version)
		{
			var helper = new TestPackageHelper (id, version);
			helper.IsLatestVersion ();
			helper.Listed ();
			return helper.Package;
		}

		FakePackage AddInstalledPackage (string id, string version)
		{
			FakePackage package = CreatePackage (id, version);
			installedPackages.Add (package);
			return package;
		}

		[Test]
		public void GetUpdatedPackages_OnePackageInstalledAndUpdateAvailable_UpdatedPackageReturned ()
		{
			AddInstalledPackage ("Test", "1.0");
			IPackage expectedPackage = AddPackageToSourceRepository ("Test", "1.1");
			var expectedPackages = new IPackage[] { expectedPackage };
			CreateUpdatedPackages ();

			IEnumerable<IPackage> packages = updatedPackages.GetUpdatedPackages ();

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetUpdatedPackages_OnePackageInstalledAndUpdateAvailable_InstalledPackageUsedToCheckIfSourceRepositoryHasAnyUpdates ()
		{
			IPackage expectedPackage = AddInstalledPackage ("Test", "1.0");
			var expectedPackages = new IPackage[] { expectedPackage };
			AddPackageToSourceRepository ("Test", "1.1");
			CreateUpdatedPackages ();

			IEnumerable<IPackage> packages = updatedPackages.GetUpdatedPackages ();

			PackageCollectionAssert.AreEqual (expectedPackages, packagesUsedWhenCheckingForUpdates);
		}

		[Test]
		public void GetUpdatedPackages_JQueryPackageInstalledTwiceWithDifferentVersions_OnlyOlderJQueryPackageUsedToDetermineUpdatedPackages ()
		{
			IPackage expectedPackage = AddInstalledPackage ("jquery", "1.6");
			var expectedPackages = new IPackage[] { expectedPackage };
			AddInstalledPackage ("jquery", "1.7");
			AddPackageToSourceRepository ("jquery", "2.1");
			CreateUpdatedPackages ();

			updatedPackages.GetUpdatedPackages ();

			PackageCollectionAssert.AreEqual (expectedPackages, packagesUsedWhenCheckingForUpdates);
		}

		[Test]
		public void GetUpdatedPackages_JQueryPackageInstalledTwiceWithDifferentVersionsAndNewerVersionsFirst_OnlyOlderJQueryPackageUsedToDetermineUpdatedPackages ()
		{
			AddInstalledPackage ("jquery", "1.7");
			IPackage expectedPackage = AddInstalledPackage ("jquery", "1.6");
			var expectedPackages = new IPackage[] { expectedPackage };
			AddPackageToSourceRepository ("jquery", "2.1");
			CreateUpdatedPackages ();

			updatedPackages.GetUpdatedPackages ();

			PackageCollectionAssert.AreEqual (expectedPackages, packagesUsedWhenCheckingForUpdates);
		}

		[Test]
		public void GetUpdatedPackages_AllowPrereleaseIsTrue_PrereleasePackagesAllowedForUpdates ()
		{
			AddInstalledPackage ("Test", "1.0");
			CreateUpdatedPackages ();

			updatedPackages.GetUpdatedPackages (includePrerelease: true);

			Assert.IsTrue (includePreleaseUsedWhenCheckingForUpdates);
		}

		[Test]
		public void GetUpdatedPackages_AllowPrereleaseIsFalse_PrereleasePackagesNotAllowedForUpdates ()
		{
			AddInstalledPackage ("Test", "1.0");
			CreateUpdatedPackages ();

			updatedPackages.GetUpdatedPackages (includePrerelease: false);

			Assert.IsFalse (includePreleaseUsedWhenCheckingForUpdates);
		}
	}
}
