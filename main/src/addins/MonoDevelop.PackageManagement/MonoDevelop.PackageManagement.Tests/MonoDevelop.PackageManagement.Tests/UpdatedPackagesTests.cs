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
		List<FakePackage> sourceRepositoryPackages;
		List<IPackageName> packageNamesUsedWhenCheckingForUpdates;
		bool includePreleaseUsedWhenCheckingForUpdates;
		FakePackageManagementProject project;

		[SetUp]
		public void Init ()
		{
			sourceRepository = new FakeServiceBasedRepository ();
			sourceRepositoryPackages = new List<FakePackage> ();
			packageNamesUsedWhenCheckingForUpdates = new List<IPackageName> ();
			project = new FakePackageManagementProject ();
		}

		void CreateUpdatedPackages ()
		{
			sourceRepository.GetUpdatesAction = (packagesNames, includePrerelease, includeAllVersions, targetFrameworks, versionConstraints) => {
				includePreleaseUsedWhenCheckingForUpdates = includePrerelease;
				packageNamesUsedWhenCheckingForUpdates.AddRange (packagesNames.Select (p => (IPackageName)p));
				return sourceRepositoryPackages.AsQueryable ();
			};
			CreateUpdatedPackages (sourceRepository);
		}

		void CreateUpdatedPackages (IPackageRepository repository)
		{
			updatedPackages = new UpdatedPackages (project, repository);
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

		PackageReference AddPackageReference (string packageId, string packageVersion)
		{
			return project.AddPackageReference (packageId, packageVersion);
		}

		[Test]
		public void GetUpdatedPackages_OnePackageReferenceAndUpdateAvailable_UpdatedPackageReturned ()
		{
			AddPackageReference ("Test", "1.0");
			IPackage expectedPackage = AddPackageToSourceRepository ("Test", "1.1");
			var expectedPackages = new IPackage[] { expectedPackage };
			CreateUpdatedPackages ();

			IEnumerable<IPackage> packages = updatedPackages.GetUpdatedPackages ();

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetUpdatedPackages_OnePackageReferencedAndUpdateAvailable_InstalledPackageNameUsedToCheckIfSourceRepositoryHasAnyUpdates ()
		{
			AddPackageReference ("Test", "1.0");
			AddPackageToSourceRepository ("Test", "1.1");
			CreateUpdatedPackages ();

			updatedPackages.GetUpdatedPackages ();

			IPackageName packageChecked = packageNamesUsedWhenCheckingForUpdates.FirstOrDefault ();
			Assert.AreSame ("Test", packageChecked.Id);
			Assert.AreSame ("1.0", packageChecked.Version.ToString ());
			Assert.AreEqual (1, packageNamesUsedWhenCheckingForUpdates.Count);
		}

		[Test]
		public void GetUpdatedPackages_JQueryPackageInstalledTwiceWithDifferentVersions_OnlyOlderJQueryPackageUsedToDetermineUpdatedPackages ()
		{
			AddPackageReference ("jquery", "1.6");
			AddPackageReference ("jquery", "1.7");
			AddPackageToSourceRepository ("jquery", "2.1");
			CreateUpdatedPackages ();

			updatedPackages.GetUpdatedPackages ();

			IPackageName packageChecked = packageNamesUsedWhenCheckingForUpdates.FirstOrDefault ();
			Assert.AreSame ("jquery", packageChecked.Id);
			Assert.AreSame ("1.6", packageChecked.Version.ToString ());
			Assert.AreEqual (1, packageNamesUsedWhenCheckingForUpdates.Count);
		}

		[Test]
		public void GetUpdatedPackages_JQueryPackageInstalledTwiceWithDifferentVersionsAndNewerVersionsFirst_OnlyOlderJQueryPackageUsedToDetermineUpdatedPackages ()
		{
			AddPackageReference ("jquery", "1.7");
			AddPackageReference ("jquery", "1.6");
			AddPackageToSourceRepository ("jquery", "2.1");
			CreateUpdatedPackages ();

			updatedPackages.GetUpdatedPackages ();

			IPackageName packageChecked = packageNamesUsedWhenCheckingForUpdates.FirstOrDefault ();
			Assert.AreSame ("jquery", packageChecked.Id);
			Assert.AreSame ("1.6", packageChecked.Version.ToString ());
			Assert.AreEqual (1, packageNamesUsedWhenCheckingForUpdates.Count);
		}

		[Test]
		public void GetUpdatedPackages_AllowPrereleaseIsTrue_PrereleasePackagesAllowedForUpdates ()
		{
			AddPackageReference ("Test", "1.0");
			CreateUpdatedPackages ();

			updatedPackages.GetUpdatedPackages (includePrerelease: true);

			Assert.IsTrue (includePreleaseUsedWhenCheckingForUpdates);
		}

		[Test]
		public void GetUpdatedPackages_AllowPrereleaseIsFalse_PrereleasePackagesNotAllowedForUpdates ()
		{
			AddPackageReference ("Test", "1.0");
			CreateUpdatedPackages ();

			updatedPackages.GetUpdatedPackages (includePrerelease: false);

			Assert.IsFalse (includePreleaseUsedWhenCheckingForUpdates);
		}

		[Test]
		public void GetUpdatedPackages_OnePackageReferencedWithConstraintAndUpdatesAvailable_LatestVersionReturnedBasedOnConstraint ()
		{
			AddPackageReference ("Test", "1.0");
			FakePackage package = AddPackageToSourceRepository ("Test", "2.0");
			FakePackage [] expectedPackages = new [] {
				package
			};
			AddPackageToSourceRepository ("Test", "3.0");
			var versionSpec = new VersionSpec ();
			versionSpec.MinVersion = new SemanticVersion ("1.0");
			versionSpec.IsMinInclusive = true;
			versionSpec.MaxVersion = new SemanticVersion ("2.0");
			versionSpec.IsMaxInclusive = true;
			var constraintProvider = new DefaultConstraintProvider ();
			constraintProvider.AddConstraint ("Test", versionSpec);
			project.ConstraintProvider = constraintProvider;
			var repository = new FakePackageRepository ();
			repository.FakePackages = sourceRepositoryPackages;
			CreateUpdatedPackages (repository);

			IEnumerable<IPackage> packages = updatedPackages.GetUpdatedPackages ();

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}
	}
}
