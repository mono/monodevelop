//
// UpdatedNuGetPackagesInWorkspace.cs
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
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UpdatedNuGetPackagesInWorkspaceTests
	{
		TestableUpdatedNuGetPackagesInWorkspace updatedPackagesInWorkspace;
		TestableCheckForNuGetPackageUpdatesTaskRunner taskRunner;
		IPackageManagementEvents packageManagementEvents;
		FakeSolution solution;
		FakeDotNetProject dotNetProject;
		FakePackageMetadataResource packageMetadataResource;
		List<Exception> errorsLogged;

		void CreateUpdatedPackagesInWorkspace ()
		{
			updatedPackagesInWorkspace = new TestableUpdatedNuGetPackagesInWorkspace ();
			taskRunner = updatedPackagesInWorkspace.TaskRunner;
			errorsLogged = taskRunner.ErrorsLogged;
			packageManagementEvents = updatedPackagesInWorkspace.PackageManagementEvents;
			solution = new FakeSolution ();

			var metadataResourceProvider = new FakePackageMetadataResourceProvider ();
			packageMetadataResource = metadataResourceProvider.PackageMetadataResource;
			var source = new PackageSource ("http://test.com");
			var providers = new INuGetResourceProvider[] {
				metadataResourceProvider
			};
			var sourceRepository = new SourceRepository (source, providers);
			taskRunner.SolutionManager.SourceRepositoryProvider.Repositories.Add (sourceRepository);
		}

		SourceRepository CreateExceptionThrowingSourceRepository (Exception ex)
		{
			var metadataResourceProvider = new FakePackageMetadataResourceProvider ();
			var metadataResource = new ExceptionThrowingPackageMetadataResource (ex);
			metadataResourceProvider.PackageMetadataResource = metadataResource;
			var source = new PackageSource ("http://test.com");
			var providers = new INuGetResourceProvider[] {
				metadataResourceProvider
			};
			return new SourceRepository (source, providers);
		}

		FakeNuGetProject AddNuGetProjectToSolution ()
		{
			dotNetProject = new FakeDotNetProject ();
			solution.Projects.Add (dotNetProject);
			return taskRunner.AddNuGetProject (dotNetProject);
		}

		Task CheckForUpdates ()
		{
			updatedPackagesInWorkspace.CheckForUpdates (solution);
			return taskRunner.CheckForUpdatesTask;
		}

		[Test]
		public async Task CheckForUpdates_OnePackageUpdated_OneUpdatedPackageFoundForProject ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.2");
			var updatedPackage = packageMetadataResource.AddPackageMetadata ("MyPackage", "1.2").Identity;
			var expectedPackages = new [] {
				updatedPackage
			};

			await CheckForUpdates ();
			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			Assert.AreEqual (project.Project, updatedPackages.Project);
			Assert.IsNotNull (updatedPackages.Project);
			CollectionAssert.AreEqual (expectedPackages, updatedPackages.GetPackages ());
		}

		[Test]
		public async Task CheckForUpdates_NoPackagesUpdated_DoesNotReturnNullUpdatedPackagesForProject ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");

			await CheckForUpdates ();
			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			Assert.AreEqual (project.Project, updatedPackages.Project);
			Assert.IsNotNull (updatedPackages.Project);
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public async Task Clear_OnePackageUpdatedButEverythingCleared_NoUpdatedPackagesFoundForProject ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			await CheckForUpdates ();

			updatedPackagesInWorkspace.Clear ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			Assert.AreEqual (project.Project, updatedPackages.Project);
			Assert.IsNotNull (updatedPackages.Project);
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public async Task CheckForUpdates_OnePackageUpdated_UpdatedPackagesAvailableEventIsFired ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			bool updatesAvailableEventFired = false;
			packageManagementEvents.UpdatedPackagesAvailable += (sender, e) => {
				updatesAvailableEventFired = true;
			};

			await CheckForUpdates ();

			Assert.IsTrue (updatesAvailableEventFired);
		}

		[Test]
		public async Task CheckForUpdates_NoPackagesUpdated_UpdatedPackagesAvailableEventIsNotFired ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			bool fired = false;
			packageManagementEvents.UpdatedPackagesAvailable += (sender, e) => {
				fired = true;
			};

			await CheckForUpdates ();

			Assert.IsFalse (fired);
		}

		[Test]
		public async Task GetUpdatedPackages_OnePackageUpdatedSameUnderlyingDotNetProjectButDifferentProxy_OneUpdatedPackageFoundForProject ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			var updatedPackage = packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1").Identity;
			var expectedPackages = new [] { updatedPackage };
			var newProject = new FakeDotNetProject ();
			dotNetProject.EqualsAction = p => {
				return p == newProject;
			};
			await CheckForUpdates ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (newProject);

			Assert.IsNotNull (updatedPackages.Project);
			CollectionAssert.AreEqual (expectedPackages, updatedPackages.GetPackages ());
			Assert.AreNotEqual (newProject, updatedPackages.Project);
		}

		[Test]
		public async Task RemoveUpdatedPackages_OnePackageUpdatedAndPackageUpdateIsInstalled_NoUpdatesAvailable ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			await CheckForUpdates ();
			project.InstalledPackages.Clear ();
			project.AddPackageReference  ("MyPackage", "1.1");

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);
			int updatedPackagesCountBeforeUpdating = updatedPackages.GetPackages ().Count ();
			updatedPackages.RemoveUpdatedPackages (project.InstalledPackages);

			Assert.AreEqual (1, updatedPackagesCountBeforeUpdating);
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public async Task GetUpdatedPackages_OnePackageUpdatedAndPackageIsUninstalled_NoUpdatesAvailableForUninstalledPackage ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			await CheckForUpdates ();
			project.InstalledPackages.Clear ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);
			int updatedPackagesCountBeforeUpdating = updatedPackages.GetPackages ().Count ();
			updatedPackages.RemoveUpdatedPackages (project.InstalledPackages);

			Assert.AreEqual (1, updatedPackagesCountBeforeUpdating);
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public async Task GetUpdatedPackages_TwoPackagesInstalledOneUpdatedAndUpdatesAvailableForBoth_OneUpdateAvailable ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("One", "1.0");
			project.AddPackageReference ("Two", "1.0");
			packageMetadataResource.AddPackageMetadata ("One", "1.1");
			packageMetadataResource.AddPackageMetadata ("Two", "1.1");
			await CheckForUpdates ();
			project.InstalledPackages.Clear ();
			project.AddPackageReference ("One", "1.1");
			project.AddPackageReference ("Two", "1.0");

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);
			int updatedPackagesCountBeforeUpdating = updatedPackages.GetPackages ().Count ();
			updatedPackages.RemoveUpdatedPackages (project.InstalledPackages);

			Assert.AreEqual (2, updatedPackagesCountBeforeUpdating);
			Assert.AreEqual (1, updatedPackages.GetPackages ().Count ());
			Assert.AreEqual ("Two", updatedPackages.GetPackages ().FirstOrDefault ().Id);
		}

		[Test]
		public async Task GetUpdatedPackages_TwoPackagesInstalledOneUpdatedWhichUpdatesItsDependency_NoUpdatesAvailable ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("One", "1.0");
			project.AddPackageReference ("Two", "1.0");
			packageMetadataResource.AddPackageMetadata ("One", "1.1");
			packageMetadataResource.AddPackageMetadata ("Two", "1.1");
			await CheckForUpdates ();
			project.InstalledPackages.Clear ();
			project.AddPackageReference ("One", "1.1");
			project.AddPackageReference ("Two", "1.1");

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);
			int updatedPackagesCountBeforeUpdating = updatedPackages.GetPackages ().Count ();
			updatedPackages.RemoveUpdatedPackages (project.InstalledPackages);

			Assert.AreEqual (2, updatedPackagesCountBeforeUpdating);
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public async Task CheckForUpdates_OnePackageUpdatedButSolutionClosedBeforeResultsReturned_UpdatedPackagesAvailableEventIsNotFiredAndNoPackageUpdatesAvailable ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			bool fired = false;
			packageManagementEvents.UpdatedPackagesAvailable += (sender, e) => {
				fired = true;
			};
			taskRunner.AfterCheckForUpdatesAction = () => {
				updatedPackagesInWorkspace.Clear ();
			};
			await CheckForUpdates ();

			Assert.IsFalse (fired);
			Assert.IsFalse (updatedPackagesInWorkspace.AnyUpdates ());
		}

		[Test]
		public async Task CheckForUpdates_NoPackagesUpdated_NoUpdates ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");

			await CheckForUpdates ();
			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			Assert.IsFalse (updatedPackagesInWorkspace.AnyUpdates ());
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public async Task CheckForUpdates_OnePackageUpdatedAndSolutionClosedBeforeResultsReturnedAndThenSolutionOpenedAgain_UpdatedPackagesAvailableEventIsFiredForSecondOpeningOfSolution ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			bool fired = false;
			packageManagementEvents.UpdatedPackagesAvailable += (sender, e) => {
				fired = true;
			};
			taskRunner.AfterCheckForUpdatesAction = () => {
				updatedPackagesInWorkspace.Clear ();
			};
			await CheckForUpdates ();
			Assert.IsFalse (fired);
			taskRunner.AfterCheckForUpdatesAction = () => { };
			await CheckForUpdates ();

			Assert.IsTrue (updatedPackagesInWorkspace.AnyUpdates ());
			Assert.IsTrue (fired);
		}

		[Test]
		public async Task GetUpdatedPackages_OnePackageHasUpdatesAndNewerVersionButNotLatestIsInstalled_UpdatesStillShowAsAvailable ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.9");
			await CheckForUpdates ();
			project.InstalledPackages.Clear ();
			project.AddPackageReference ("MyPackage", "1.1");

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);
			updatedPackages.RemoveUpdatedPackages (project.InstalledPackages);

			Assert.AreEqual (1, updatedPackages.GetPackages ().Count ());
			Assert.AreEqual ("MyPackage", updatedPackages.GetPackages ().First ().Id);
			Assert.AreEqual ("1.9", updatedPackages.GetPackages ().First ().Version.ToString ());
		}

		[Test]
		public async Task CheckForUpdates_ProjectHasPreReleasePackageWhichHasUpdatedPrereleasePackage_OnePrereleaseUpdateFound ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0.1-alpha");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.0.1-beta");
			await CheckForUpdates ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			Assert.AreEqual (1, updatedPackages.GetPackages ().Count ());
			Assert.AreEqual ("MyPackage", updatedPackages.GetPackages ().First ().Id);
			Assert.AreEqual ("1.0.1-beta", updatedPackages.GetPackages ().First ().Version.ToString ());
		}

		[Test]
		public async Task CheckForUpdates_ProjectHasPreReleasePackageWhichHasUpdatedStablePackage_OneStableUpdateFound ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0.1-alpha");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.0.1");
			await CheckForUpdates ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			Assert.AreEqual (1, updatedPackages.GetPackages ().Count ());
			Assert.AreEqual ("MyPackage", updatedPackages.GetPackages ().First ().Id);
			Assert.AreEqual ("1.0.1", updatedPackages.GetPackages ().First ().Version.ToString ());
		}

		[Test]
		public async Task CheckForUpdates_ProjectHasStableAndPreReleasePackagesBothWithUpdatese_TwoUpdatesFound ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0.1-alpha");
			project.AddPackageReference ("AnotherPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.0.1-beta");
			packageMetadataResource.AddPackageMetadata ("AnotherPackage", "1.1");
			await CheckForUpdates ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			var anotherPackageUpdate = updatedPackages.GetPackages ().FirstOrDefault (p => p.Id == "AnotherPackage");
			var myPackageUpdate =  updatedPackages.GetPackages ().FirstOrDefault (p => p.Id == "MyPackage");
			Assert.AreEqual (2, updatedPackages.GetPackages ().Count ());
			Assert.IsNotNull (anotherPackageUpdate);
			Assert.AreEqual ("1.1", anotherPackageUpdate.Version.ToString ());
			Assert.IsNotNull (myPackageUpdate);
			Assert.AreEqual ("1.0.1-beta", myPackageUpdate.Version.ToString ());
		}

		[Test]
		public async Task CheckForUpdates_ProjectHasStablePackageWhichHasUpdatedPrereleasePackage_NoUpdatesFound ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0.1");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1.0-alpha");
			await CheckForUpdates ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (project.Project);

			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public async Task GetUpdatedPackages_UpdatingThreeOldAndroidPackagesInstallsOneAndUpdatesOneAndRemovesOneWithOneInstall_NoUpdatesRemain ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("Xamarin.Android.Support.v13", "20.0.0.4");
			project.AddPackageReference ("Xamarin.Android.Support.v4", "20.0.0.4");
			project.AddPackageReference ("Xamarin.Android.Support.v7.AppCompat", "20.0.0.2");
			packageMetadataResource.AddPackageMetadata ("Xamarin.Android.Support.v13", "23.1.1.0");
			packageMetadataResource.AddPackageMetadata ("Xamarin.Android.Support.v4", "23.1.1.0");
			packageMetadataResource.AddPackageMetadata ("Xamarin.Android.Support.v7.AppCompat", "23.1.1.0");
			await CheckForUpdates ();
			int originalUpdatesAvailable = updatedPackagesInWorkspace
				.GetUpdatedPackages (project.Project)
				.GetPackages ()
				.Count ();
			project.InstalledPackages.Clear ();
			project.AddPackageReference ("Xamarin.Android.Support.v4", "23.1.1.0");
			project.AddPackageReference ("Xamarin.Android.Support.v7.AppCompat", "23.1.1.0");

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (project.Project);
			updatedPackages.RemoveUpdatedPackages (project.InstalledPackages);

			Assert.AreEqual (3, originalUpdatesAvailable);
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
		}

		[Test]
		public async Task GetUpdatedPackages_SecondSolutionOpenedWhilstCheckingForUpdatesForFirstSolution_UpdatesFoundForProjectsInBothSolutions ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			packageMetadataResource.AddPackageMetadata ("AnotherPackage", "1.2");
			var firstDotNetProject = dotNetProject;
			FakeDotNetProject secondDotNetProject = null;
			taskRunner.AfterCheckForUpdatesAction = () => {
				if (secondDotNetProject == null) {
					solution = new FakeSolution ();
					FakeNuGetProject anotherProject = AddNuGetProjectToSolution ();
					anotherProject.AddPackageReference ("AnotherPackage", "0.1");
					secondDotNetProject = dotNetProject;
					updatedPackagesInWorkspace.CheckForUpdates (solution);
				}
			};
			await CheckForUpdates ();
			// Wait for second solution checks.
			await taskRunner.CheckForUpdatesTask;

			var updatedPackagesForProjectInFirstSolution = updatedPackagesInWorkspace.GetUpdatedPackages (firstDotNetProject);
			var updatedPackagesForProjectInSecondSolution = updatedPackagesInWorkspace.GetUpdatedPackages (secondDotNetProject);

			Assert.AreEqual (1, updatedPackagesForProjectInFirstSolution.GetPackages ().Count ());
			Assert.AreEqual ("MyPackage", updatedPackagesForProjectInFirstSolution.GetPackages ().First ().Id);
			Assert.AreEqual ("1.1", updatedPackagesForProjectInFirstSolution.GetPackages ().First ().Version.ToString ());
			Assert.AreEqual (1, updatedPackagesForProjectInSecondSolution.GetPackages ().Count ());
			Assert.AreEqual ("AnotherPackage", updatedPackagesForProjectInSecondSolution.GetPackages ().First ().Id);
			Assert.AreEqual ("1.2", updatedPackagesForProjectInSecondSolution.GetPackages ().First ().Version.ToString ());
		}

		[Test]
		public async Task GetUpdatedPackages_OnePackageReferencedWithConstraintAndUpdatesAvailable_LatestVersionReturnedBasedOnConstraint ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			var versionRange = new VersionRange (
				minVersion: new NuGetVersion ("1.0"),
				includeMinVersion: true,
				maxVersion: new NuGetVersion ("2.0"),
				includeMaxVersion: true);
			project.AddPackageReference ("Test", "1.0", versionRange);
			var package = packageMetadataResource.AddPackageMetadata ("Test", "2.0").Identity;
			packageMetadataResource.AddPackageMetadata ("Test", "3.0");
			var expectedPackages = new [] {
				package
			};
			await CheckForUpdates ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			CollectionAssert.AreEqual (expectedPackages, updatedPackages.GetPackages ());
		}

		[Test]
		public async Task CheckForUpdates_TwoSourceRepositoriesAndFirstOneThrowsException_UpdatedPackageFoundFromNonFailingSourceRepository ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", "1.0");
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			var ex = new ApplicationException ("Error");
			var sourceRepository = CreateExceptionThrowingSourceRepository (ex);
			taskRunner.SolutionManager.SourceRepositoryProvider.Repositories.Insert (0, sourceRepository);

			await CheckForUpdates ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			var package = updatedPackages.GetPackages ().Single ();
			Assert.AreEqual ("MyPackage", package.Id);
			Assert.AreEqual ("1.1", package.Version.ToString ());
		}

		/// <summary>
		/// Do not check for updates if no version is set in the project.
		/// </summary>
		[Test]
		public async Task GetUpdatedPackages_PackageReferenceHasNoVersion_NoUpdatedPackagesFoundForProject ()
		{
			CreateUpdatedPackagesInWorkspace ();
			FakeNuGetProject project = AddNuGetProjectToSolution ();
			project.AddPackageReference ("MyPackage", version: null);
			packageMetadataResource.AddPackageMetadata ("MyPackage", "1.1");
			await CheckForUpdates ();

			var updatedPackages = updatedPackagesInWorkspace.GetUpdatedPackages (dotNetProject);

			Assert.AreEqual (project.Project, updatedPackages.Project);
			Assert.IsNotNull (updatedPackages.Project);
			Assert.AreEqual (0, updatedPackages.GetPackages ().Count ());
			Assert.AreEqual (0, errorsLogged.Count);
		}
	}
}

