//
// RestorePackagesAction.cs
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
using NUnit.Framework;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class RestorePackagesActionTests
	{
		RestorePackagesAction action;
		FakePackageManagementSolution solution;
		FakePackageManagerFactory packageManagerFactory;
		FakePackageRepositoryFactory packageRepositoryCache;
		PackageManagementEvents packageEvents;
		List<PackageOperationMessage> messagesLogged;

		void CreateSolution ()
		{
			packageManagerFactory = new FakePackageManagerFactory ();
			packageRepositoryCache = new FakePackageRepositoryFactory ();
			packageEvents = new PackageManagementEvents ();
			solution = new FakePackageManagementSolution ();
		}

		FakePackageManagementProject CreateSolutionWithOneProject ()
		{
			CreateSolution ();
			return solution.AddFakeProject ("MyProject");
		}

		void CreateAction ()
		{
			action = new RestorePackagesAction (
				solution,
				packageEvents,
				packageRepositoryCache,
				packageManagerFactory);
		}

		FakePackage AddPackageToPriorityRepository (string packageId, string packageVersion)
		{
			return packageRepositoryCache
				.FakePriorityPackageRepository
				.AddFakePackageWithVersion (packageId, packageVersion);
		}

		void CaptureMessagesLogged ()
		{
			messagesLogged = new List<PackageOperationMessage> ();
			packageEvents.PackageOperationMessageLogged += (sender, e) => {
				messagesLogged.Add (e.Message);
			};
		}

		FakeOperationAwarePackageRepository MakePriorityRepositoryOperationAware ()
		{
			var repository = new FakeOperationAwarePackageRepository ();
			packageRepositoryCache.FakePriorityPackageRepository = repository;
			return repository;
		}

		void PackageIsRestored (FakePackage package)
		{
			solution.SolutionPackageRepository.FakePackages.Add (package);
		}

		void AssertMessageLogged (string expectedMessage)
		{
			List<string> allMessages = messagesLogged.Select (message => message.ToString ()).ToList ();
			Assert.That (allMessages, Contains.Item (expectedMessage));
		}

		void AssertMessageIsNotLogged (string expectedMessage)
		{
			List<string> allMessages = messagesLogged.Select (message => message.ToString ()).ToList ();
			Assert.That (allMessages, Has.No.Contains (expectedMessage));
		}

		void AssertNoMessageLoggedThatContains (string expectedMessage)
		{
			List<string> allMessages = messagesLogged.Select (message => message.ToString ()).ToList ();
			Assert.That (allMessages, Has.No.ContainsSubstring (expectedMessage));
		}

		[Test]
		public void Execute_ProjectHasOneUnrestoredPackage_PackageFromPriorityRepositoryIsInstalled ()
		{
			FakePackageManagementProject project = CreateSolutionWithOneProject ();
			project.AddPackageReference ("MyPackage", "1.0");
			FakePackage package = AddPackageToPriorityRepository ("MyPackage", "1.0");
			CreateAction ();

			action.Execute ();

			Assert.AreEqual (package, packageManagerFactory.FakePackageManager.PackagePassedToInstallPackage);
			Assert.IsTrue (packageManagerFactory.FakePackageManager.IgnoreWalkInfoPassedToInstallPackage);
			Assert.IsTrue (packageManagerFactory.FakePackageManager.IgnoreDependenciesPassedToInstallPackage);
			Assert.IsTrue (packageManagerFactory.FakePackageManager.AllowPrereleaseVersionsPassedToInstallPackage);
			Assert.AreEqual (packageRepositoryCache.FakePriorityPackageRepository, packageManagerFactory.PackageRepositoryPassedToCreatePackageManager);
			Assert.AreEqual (solution.SolutionPackageRepository, packageManagerFactory.SolutionPackageRepositoryPassedToCreatePackageManager);
		}

		[Test]
		public void Execute_ProjectHasOneUnrestoredPackage_PackageManagerHasLoggerConfigured ()
		{
			FakePackageManagementProject project = CreateSolutionWithOneProject ();
			project.AddPackageReference ("MyPackage", "1.0");
			FakePackage package = AddPackageToPriorityRepository ("MyPackage", "1.0");
			CreateAction ();

			action.Execute ();

			Assert.IsInstanceOf<PackageManagementLogger> (packageManagerFactory.FakePackageManager.Logger);
		}

		[Test]
		public void Execute_ProjectHasOneUnrestoredPackage_RestoringPackagesMessageLogged ()
		{
			FakePackageManagementProject project = CreateSolutionWithOneProject ();
			project.AddPackageReference ("MyPackage", "1.0");
			FakePackage package = AddPackageToPriorityRepository ("MyPackage", "1.0");
			CreateAction ();
			CaptureMessagesLogged ();

			action.Execute ();

			Assert.AreEqual ("Restoring packages...", messagesLogged [0].ToString ());
		}

		[Test]
		public void Execute_ProjectHasOneUnrestoredPackage_RestoreOperationAddedToHttpHeader ()
		{
			FakePackageManagementProject project = CreateSolutionWithOneProject ();
			project.AddPackageReference ("MyPackage", "1.0");
			FakeOperationAwarePackageRepository repository = MakePriorityRepositoryOperationAware ();
			FakePackage package = AddPackageToPriorityRepository ("MyPackage", "1.0");
			CreateAction ();

			action.Execute ();

			repository.AssertOperationWasStartedAndDisposed (
				RepositoryOperationNames.Restore,
				"MyPackage",
				"1.0");
		}

		[Test]
		public void Execute_ProjectHasOnePackageWhichIsRestored_PackageIsNotInstalledAgain ()
		{
			FakePackageManagementProject project = CreateSolutionWithOneProject ();
			project.AddPackageReference ("MyPackage", "1.0");
			FakePackage package = AddPackageToPriorityRepository ("MyPackage", "1.0");
			PackageIsRestored (package);
			CreateAction ();

			action.Execute ();

			Assert.IsFalse (packageManagerFactory.FakePackageManager.IsPackageInstalled);
		}

		[Test]
		public void Execute_ProjectHasOnePackageWhichIsRestored_AllPackagesAlreadyRestoredMessageIsLogged ()
		{
			FakePackageManagementProject project = CreateSolutionWithOneProject ();
			project.AddPackageReference ("MyPackage", "1.2.3.4");
			FakePackage package = AddPackageToPriorityRepository ("MyPackage", "1.2.3.4");
			PackageIsRestored (package);
			CreateAction ();
			CaptureMessagesLogged ();

			action.Execute ();

			AssertMessageLogged ("All packages are already restored.");
			AssertMessageLogged ("Skipping 'MyPackage 1.2.3.4' because it is already restored.");
		}

		[Test]
		public void Execute_ProjectHasOnePackageWhichIsNotRestored_AllPackagesAreAlreadyRestoredMessageIsNotLogged ()
		{
			FakePackageManagementProject project = CreateSolutionWithOneProject ();
			project.AddPackageReference ("MyPackage", "1.0");
			AddPackageToPriorityRepository ("MyPackage", "1.0");
			CreateAction ();
			CaptureMessagesLogged ();

			action.Execute ();

			AssertMessageIsNotLogged ("All packages are already restored.");
			AssertMessageLogged ("1 package restored successfully.");
		}

		[Test]
		public void Execute_ProjectHasTwoMissingPackages_PackagesRestoredSuccessfullyMessageIsLogged ()
		{
			FakePackageManagementProject project = CreateSolutionWithOneProject ();
			project.AddPackageReference ("MyPackage", "1.0");
			project.AddPackageReference ("MyOtherPackage", "1.2");
			AddPackageToPriorityRepository ("MyPackage", "1.0");
			AddPackageToPriorityRepository ("MyOtherPackage", "1.2");
			CreateAction ();
			CaptureMessagesLogged ();

			action.Execute ();

			AssertMessageIsNotLogged ("All packages are already restored.");
			AssertMessageLogged ("2 packages restored successfully.");
		}

		[Test]
		public void Execute_TwoProjectsEachWithSameMissingPackage_PackageIsRestoredOnce ()
		{
			CreateSolution ();
			FakePackageManagementProject project1 = solution.AddFakeProject ("MyProject1");
			FakePackageManagementProject project2 = solution.AddFakeProject ("MyProject2");
			project1.AddPackageReference ("MyPackage", "1.2.3.4");
			project2.AddPackageReference ("MyPackage", "1.2.3.4");
			AddPackageToPriorityRepository ("MyPackage", "1.2.3.4");
			CreateAction ();
			CaptureMessagesLogged ();

			action.Execute ();

			Assert.AreEqual (1, packageManagerFactory.FakePackageManager.PackagesInstalled.Count);
			Assert.AreEqual ("MyPackage", packageManagerFactory.FakePackageManager.PackagePassedToInstallPackage.Id);
			Assert.AreEqual ("1.2.3.4", packageManagerFactory.FakePackageManager.PackagePassedToInstallPackage.Version.ToString ());
			AssertMessageLogged ("1 package restored successfully.");
		}
	}
}

