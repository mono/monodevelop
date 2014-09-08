//
// ReinstallProjectPackagesActionTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ReinstallProjectPackagesActionTests
	{
		ReinstallProjectPackagesAction action;
		FakePackageManagementProject project;
		PackageManagementEvents packageManagementEvents;

		void CreateAction ()
		{
			project = new FakePackageManagementProject ();
			packageManagementEvents = new PackageManagementEvents ();
			action = new ReinstallProjectPackagesAction (project, packageManagementEvents);
		}

		FakePackage AddPackageToProject (string packageId, string version = "1.0")
		{
			FakePackage package = FakePackage.CreatePackageWithVersion (packageId, version);
			project.FakePackages.Add (package);
			return package;
		}

		FakePackage AddPackageToProjectAndSourceRepository (string packageId, string version = "1.0")
		{
			FakePackage package = AddPackageToProject (packageId, version);
			project.FakeSourceRepository.FakePackages.Add (package);
			AddReinstallOperationsToProject (project.FakeSourceRepository.FakePackages);
			return package;
		}

		FakePackage AddPackageToSourceRepository (string packageId, string version = "1.0")
		{
			FakePackage package = FakePackage.CreatePackageWithVersion (packageId, version);
			project.FakeSourceRepository.FakePackages.Add (package);
			return package;
		}

		ReinstallPackageOperations AddReinstallOperationsToProject (IEnumerable<IPackage> packages)
		{
			List<PackageOperation> operations = packages
				.Select (p => new PackageOperation (p, PackageAction.Install))
				.ToList ();

			var reinstallOperations = new ReinstallPackageOperations (operations, packages);
			project.ReinstallOperations = reinstallOperations;
			return reinstallOperations;
		}

		[Test]
		public void Packages_OneProjectPackage_ReturnsOneProjectPackage ()
		{
			CreateAction ();
			FakePackage package = AddPackageToProject ("MyPackage");
			var expectedPackages = new FakePackage[] { package };

			List<IPackage> packages = action.Packages.ToList ();

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void Execute_ProjectPackageNotAvailableFromSourceRepository_ExceptionThrownForMissingPackage ()
		{
			CreateAction ();
			AddPackageToProject ("MyPackage");

			Exception ex = Assert.Throws <ApplicationException> (() => {
				action.Execute ();
			});
			Assert.That (ex.Message, Contains.Substring ("MyPackage"));
		}

		[Test]
		public void Execute_OneProjectPackage_PackageIsUninstalled ()
		{
			CreateAction ();
			FakePackage package = AddPackageToProjectAndSourceRepository ("MyPackage");

			action.Execute ();

			Assert.AreEqual (package, project.PackagePassedToUninstallPackage);
			Assert.IsTrue (project.ForceRemovePassedToUninstallPackage);
			Assert.IsFalse (project.RemoveDependenciesPassedToUninstallPackage);
		}

		[Test]
		public void Execute_OneProjectPackage_ReinstallPackageOperationsAreCreatedBasedOnPackagesFromSourceRepository ()
		{
			CreateAction ();
			AddPackageToProject ("MyPackage", "1.0");
			FakePackage sourceRepositoryPackage = AddPackageToSourceRepository ("MyPackage", "1.0");
			var expectedPackages = new FakePackage[] { sourceRepositoryPackage };
			AddReinstallOperationsToProject (expectedPackages);

			action.Execute ();

			PackageCollectionAssert.AreEqual (expectedPackages, project.PackagesPassedToGetReinstallPackageOperations);
		}

		[Test]
		public void Execute_OneProjectPackage_ReinstallPackageOperationsAreRun ()
		{
			CreateAction ();
			AddPackageToProject ("MyPackage", "1.0");
			FakePackage sourceRepositoryPackage = AddPackageToSourceRepository ("MyPackage", "1.0");
			var expectedPackages = new FakePackage[] { sourceRepositoryPackage };
			ReinstallPackageOperations operations = AddReinstallOperationsToProject (expectedPackages);

			action.Execute ();

			Assert.AreEqual (operations.Operations, project.PackageOperationsRun);
		}

		[Test]
		public void Execute_OneProjectPackage_PackageReferencesAddedForPackagesReturnedWithReinstallOperations ()
		{
			CreateAction ();
			AddPackageToProject ("MyPackage", "1.0");
			AddPackageToSourceRepository ("MyPackage", "1.0");
			var expectedPackages = new FakePackage[] { FakePackage.CreatePackageWithVersion ("MyPackage", "1.0") };
			AddReinstallOperationsToProject (expectedPackages);

			action.Execute ();

			PackageCollectionAssert.AreEqual (expectedPackages, project.PackageReferencesAdded);
		}

		[Test]
		public void Execute_OneProjectPackage_PackageRetargetingMessageIsLogged ()
		{
			CreateAction ();
			AddPackageToProjectAndSourceRepository ("MyPackage");
			string expectedMessage = GettextCatalog.GetString ("Retargeting packages...{0}", Environment.NewLine);
			var messages = new List<string> ();
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				messages.Add (e.Message.ToString ());
			};

			action.Execute ();

			Assert.AreEqual (expectedMessage, messages.FirstOrDefault ());
		}
	}
}

