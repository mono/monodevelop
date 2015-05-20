//
// UpdatePackageActionTests.cs
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
using System.IO;
using System.Linq;
using ICSharpCode.PackageManagement;
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UpdatePackageActionTests
	{
		TestableUpdatePackageAction action;
		PackageManagementEvents packageManagementEvents;
		FakePackageManagementProject fakeProject;
		UpdatePackageHelper updatePackageHelper;
		FakeFileRemover fileRemover;
		List<PackageOperationMessage> messagesLogged;
		FakeFileService fileService;

		void CreateSolution ()
		{
			packageManagementEvents = new PackageManagementEvents ();
			fakeProject = new FakePackageManagementProject ();
			fileRemover = new FakeFileRemover ();
			action = new TestableUpdatePackageAction (fakeProject, packageManagementEvents, fileRemover);
			updatePackageHelper = new UpdatePackageHelper (action);
		}

		void AddInstallOperationWithFile (string fileName)
		{
			var package = new FakePackage ();
			package.AddFile (fileName);

			var operation = new PackageOperation (package, PackageAction.Install);
			var operations = new List<PackageOperation> ();
			operations.Add (operation);

			action.Operations = operations;
		}

		void RecordMessagesLogged ()
		{
			messagesLogged = new List<PackageOperationMessage> ();
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => messagesLogged.Add (e.Message);
		}

		void AssertMessageIsLogged (string expectedMessage)
		{
			List<string> messages = messagesLogged.Select (m => m.ToString ()).ToList ();

			Assert.That (messages, Contains.Item (expectedMessage));
		}

		IOpenPackageReadMeMonitor CreateReadMeMonitor (string packageId)
		{
			fileService = new FakeFileService (fakeProject.FakeDotNetProject);
			return new OpenPackageReadMeMonitor (packageId, fakeProject, fileService);
		}

		[Test]
		public void Execute_PackageAndRepositoryPassed_PackageIsUpdated ()
		{
			CreateSolution ();
			updatePackageHelper.UpdateTestPackage ();

			FakePackage expectedPackage = updatePackageHelper.TestPackage;
			IPackage actualPackage = fakeProject.PackagePassedToUpdatePackage;

			Assert.AreEqual (expectedPackage, actualPackage);
		}

		[Test]
		public void UpdateDependencies_DefaultValue_ReturnsTrue ()
		{
			CreateSolution ();
			Assert.IsTrue (action.UpdateDependencies);
		}

		[Test]
		public void AllowPrereleaseVersions_DefaultValue_ReturnsFalse ()
		{
			CreateSolution ();
			Assert.IsFalse (action.AllowPrereleaseVersions);
		}

		[Test]
		public void Execute_PackageAndRepositoryPassed_PackageOperationsUsedToUpdatePackage ()
		{
			CreateSolution ();
			updatePackageHelper.UpdateTestPackage ();

			IEnumerable<PackageOperation> expectedOperations = updatePackageHelper.PackageOperations;
			IEnumerable<PackageOperation> actualOperations = fakeProject.PackageOperationsPassedToUpdatePackage;

			Assert.AreEqual (expectedOperations, actualOperations);
		}

		[Test]
		public void Execute_PackageAndRepositoryPassed_DependenciesUpdated ()
		{
			CreateSolution ();
			updatePackageHelper.UpdateDependencies = true;
			updatePackageHelper.UpdateTestPackage ();

			bool result = fakeProject.UpdateDependenciesPassedToUpdatePackage;

			Assert.IsTrue (result);
		}

		[Test]
		public void Execute_PackageAndRepositoryPassed_PrereleaseVersionsNotAllowed ()
		{
			CreateSolution ();
			updatePackageHelper.AllowPrereleaseVersions = false;
			updatePackageHelper.UpdateTestPackage ();

			bool result = fakeProject.AllowPrereleaseVersionsPassedToUpdatePackage;

			Assert.IsFalse (result);
		}

		[Test]
		public void Execute_PackageAndRepositoryPassedAndAllowPrereleaseVersions_PrereleaseVersionsAllowed ()
		{
			CreateSolution ();
			updatePackageHelper.AllowPrereleaseVersions = true;
			updatePackageHelper.UpdateTestPackage ();

			bool result = fakeProject.AllowPrereleaseVersionsPassedToUpdatePackage;

			Assert.IsTrue (result);
		}

		[Test]
		public void Execute_PackageAndRepositoryPassedAndUpdateDependenciesIsFalse_DependenciesNotUpdated ()
		{
			CreateSolution ();
			updatePackageHelper.UpdateDependencies = false;
			updatePackageHelper.UpdateTestPackage ();

			bool result = fakeProject.UpdateDependenciesPassedToUpdatePackage;

			Assert.IsFalse (result);
		}

		[Test]
		public void Execute_PackageAndRepositoryPassed_PackageInstalledEventIsFired ()
		{
			CreateSolution ();
			IPackage actualPackage = null;
			packageManagementEvents.ParentPackageInstalled += (sender, e) => {
				actualPackage = e.Package;
			};
			updatePackageHelper.UpdateTestPackage ();

			FakePackage expectedPackage = updatePackageHelper.TestPackage;
			Assert.AreEqual (expectedPackage, actualPackage);
		}

		[Test]
		public void Execute_PackagePassedButNoPackageOperations_PackageOperationsRetrievedFromProject ()
		{
			CreateSolution ();
			updatePackageHelper.PackageOperations = null;
			updatePackageHelper.UpdateTestPackage ();

			IEnumerable<PackageOperation> actualOperations = action.Operations;
			List<FakePackageOperation> expectedOperations = fakeProject.FakeInstallOperations;

			Assert.AreEqual (expectedOperations, actualOperations);
		}

		[Test]
		public void Execute_PackagePassedButNoPackageOperations_PackageOperationsCreatedForPackage ()
		{
			CreateSolution ();
			updatePackageHelper.PackageOperations = null;
			updatePackageHelper.UpdateTestPackage ();

			var expectedPackage = updatePackageHelper.TestPackage;
			var actualPackage = fakeProject.PackagePassedToGetInstallPackageOperations;

			Assert.AreEqual (expectedPackage, actualPackage);
		}

		[Test]
		public void Execute_PackageIdAndSourceAndProjectPassedAndUpdateDependenciesIsTrue_DependenciesUpdatedWhenUpdatingPackage ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			updatePackageHelper.UpdateDependencies = true;
			updatePackageHelper.UpdatePackageById ("PackageId");

			bool result = fakeProject.UpdateDependenciesPassedToUpdatePackage;

			Assert.IsTrue (result);
		}

		[Test]
		public void Execute_PackageIdAndSourceAndProjectPassedAndUpdateDependenciesIsFalse_DependenciesNotUpdatedWhenGettingPackageOperations ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			updatePackageHelper.UpdateDependencies = false;
			updatePackageHelper.UpdatePackageById ("PackageId");

			bool result = fakeProject.UpdateDependenciesPassedToUpdatePackage;

			Assert.IsFalse (result);
		}

		[Test]
		public void Execute_UpdatedDepdenciesIsFalseAndNoPackageOperations_DependenciesIgnoredWhenGettingPackageOperations ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			updatePackageHelper.UpdateDependencies = false;
			updatePackageHelper.UpdatePackageById ("PackageId");

			bool result = fakeProject.IgnoreDependenciesPassedToGetInstallPackageOperations;

			Assert.IsTrue (result);
		}

		[Test]
		public void Execute_UpdateDependenciesIsTrueAndNoPackageOperations_DependenciesNotIgnoredWhenGettingPackageOperations ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			updatePackageHelper.UpdateDependencies = true;
			updatePackageHelper.UpdatePackageById ("PackageId");

			bool result = fakeProject.IgnoreDependenciesPassedToGetInstallPackageOperations;

			Assert.IsFalse (result);
		}

		[Test]
		public void Execute_AllowPrereleaseVersionsIsFalseAndNoPackageOperations_PrereleaseVersionsNotAllowedWhenGettingPackageOperations ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			updatePackageHelper.AllowPrereleaseVersions = false;
			updatePackageHelper.UpdatePackageById ("PackageId");

			bool result = fakeProject.AllowPrereleaseVersionsPassedToGetInstallPackageOperations;

			Assert.IsFalse (result);
		}

		[Test]
		public void Execute_AllowPrereleaseVersionsIsTrueAndNoPackageOperations_PrereleaseVersionsAllowedWhenGettingPackageOperations ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			updatePackageHelper.AllowPrereleaseVersions = true;
			updatePackageHelper.UpdatePackageById ("PackageId");

			bool result = fakeProject.AllowPrereleaseVersionsPassedToGetInstallPackageOperations;

			Assert.IsTrue (result);
		}

		[Test]
		public void Execute_PackageAndPackageOperationsSet_OperationsNotRetrievedFromPackageManager ()
		{
			CreateSolution ();
			updatePackageHelper.UpdateTestPackage ();

			IPackage actualPackage = fakeProject.PackagePassedToGetInstallPackageOperations;

			Assert.IsNull (actualPackage);
		}

		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasInitPowerShellScript_ReturnsTrue ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("Test");
			action.PackageId = "Test";
			AddInstallOperationWithFile (@"tools\init.ps1");

			bool hasPackageScripts = action.HasPackageScriptsToRun ();

			Assert.IsTrue (hasPackageScripts);
		}

		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasNoFiles_ReturnsFalse ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("Test");
			action.PackageId = "Test";
			action.Operations = new List<PackageOperation> ();

			bool hasPackageScripts = action.HasPackageScriptsToRun ();

			Assert.IsFalse (hasPackageScripts);
		}

		[Test]
		public void UpdateIfPackageDoesNotExistInProject_NewUpdateActionInstanceCreate_ReturnsTrue ()
		{
			CreateSolution ();
			bool update = action.UpdateIfPackageDoesNotExistInProject;

			Assert.IsTrue (update);
		}

		[Test]
		public void Execute_UpdateIfPackageDoesNotExistInProjectSetToFalseAndPackageDoesNotExistInProject_PackageIsNotUpdated ()
		{
			CreateSolution ();
			action.Package = new FakePackage ("Test");
			action.UpdateIfPackageDoesNotExistInProject = false;
			action.Execute ();

			bool updated = fakeProject.IsUpdatePackageCalled;

			Assert.IsFalse (updated);
		}

		[Test]
		public void Execute_UpdateIfPackageDoesNotExistInProjectSetToFalseAndPackageDoesNotExistInProject_PackageInstalledEventIsNotFired ()
		{
			CreateSolution ();
			bool updated = false;
			packageManagementEvents.ParentPackageInstalled += (sender, e) => updated = true;
			action.UpdateIfPackageDoesNotExistInProject = false;

			updatePackageHelper.UpdateTestPackage ();

			Assert.IsFalse (updated);
		}

		[Test]
		public void Execute_UpdateIfPackageDoesNotExistInProjectSetToFalseAndPackageExistsInProject_PackageIsUpdated ()
		{
			CreateSolution ();
			action.UpdateIfPackageDoesNotExistInProject = false;
			action.PackageId = "Test";
			FakePackage expectedPackage = fakeProject.FakeSourceRepository.AddFakePackageWithVersion ("Test", "1.1");
			fakeProject.FakePackages.Add (new FakePackage ("Test", "1.0"));
			action.Execute ();

			IPackage actualPackage = fakeProject.PackagePassedToUpdatePackage;

			Assert.AreEqual (expectedPackage, actualPackage);
		}

		[Test]
		public void Execute_PackagePassedAndUpdateIfPackageDoesNotExistInProjectSetToFalseAndPackageExistsInProject_PackageIsUpdated ()
		{
			CreateSolution ();
			action.UpdateIfPackageDoesNotExistInProject = false;
			var expectedPackage = new FakePackage ("Test", "1.1");
			action.Package = expectedPackage;
			fakeProject.FakePackages.Add (new FakePackage ("Test", "1.0"));
			action.Execute ();

			IPackage actualPackage = fakeProject.PackagePassedToUpdatePackage;

			Assert.AreEqual (expectedPackage, actualPackage);
		}

		[Test]
		public void Execute_PackagesConfigFileDeletedDuringUpdate_FileServicePackagesConfigFileDeletionIsCancelled ()
		{
			CreateSolution ();
			action.Package = new FakePackage ("Test");
			string expectedFileName = @"d:\projects\MyProject\packages.config".ToNativePath ();
			bool? fileRemovedResult = null;
			fakeProject.UpdatePackageAction = (p, a) => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (expectedFileName);
			};
			action.Execute ();

			Assert.AreEqual (expectedFileName, fileRemover.FileRemoved);
			Assert.IsFalse (fileRemovedResult.Value);
		}

		[Test]
		public void Execute_ScriptFileDeletedDuringUpdate_FileDeletionIsNotCancelled ()
		{
			CreateSolution ();
			action.Package = new FakePackage ("Test");
			string fileName = @"d:\projects\MyProject\scripts\myscript.js".ToNativePath ();
			bool? fileRemovedResult = null;
			fakeProject.UpdatePackageAction = (p, a) => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (fileName);
			};
			action.Execute ();

			Assert.IsTrue (fileRemovedResult.Value);
			Assert.IsNull (fileRemover.FileRemoved);
		}

		[Test]
		public void Execute_PackageHasConstraint_LatestPackageIsNotUpdatedButPackageWithHighestVersionThatMatchesConstraint ()
		{
			CreateSolution ();
			var constraintProvider = new DefaultConstraintProvider ();
			var versionSpec = new VersionSpec ();
			versionSpec.MinVersion = new SemanticVersion ("1.0");
			versionSpec.IsMinInclusive = true;
			versionSpec.IsMaxInclusive = true;
			versionSpec.MaxVersion = new SemanticVersion ("2.0");
			constraintProvider.AddConstraint ("MyPackage", versionSpec);
			fakeProject.ConstraintProvider = constraintProvider;
			fakeProject.AddFakePackageToSourceRepository ("MyPackage", "1.0");
			FakePackage packageVersion2 = fakeProject.AddFakePackageToSourceRepository ("MyPackage", "2.0");
			fakeProject.AddFakePackageToSourceRepository ("MyPackage", "3.0");
			fakeProject.FakePackages.Add (new FakePackage ("MyPackage", "1.0"));
			action.PackageId = "MyPackage";

			action.Execute ();

			Assert.AreEqual (packageVersion2, fakeProject.PackagePassedToUpdatePackage);
		}

		[Test]
		public void Execute_NewerPrereleaseInstalledAndTryToUpdateToOlderStableRelease_UpdateIsNotInstalled ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("MyPackage", "1.2");
			fakeProject.FakePackages.Add (new FakePackage ("MyPackage", "1.3.0.6275-pre1"));
			action.PackageId = "MyPackage";
			fakeProject.Name = "MyProject";
			action.UpdateIfPackageDoesNotExistInProject = false;
			RecordMessagesLogged ();

			action.Execute ();

			Assert.IsNull (fakeProject.PackagePassedToUpdatePackage);
			Assert.IsFalse (fakeProject.IsUpdatePackageCalled);
			AssertMessageIsLogged ("No updates available for 'MyPackage' in project 'MyProject'.");
		}

		[Test]
		public void Execute_NewerPrereleaseInstalledAndTryToUpdateToOlderStableReleaseAndUpdateIfPackageDoesNotExistInProject_UpdateIsNotInstalled ()
		{
			CreateSolution ();
			fakeProject.AddFakePackageToSourceRepository ("MyPackage", "1.2");
			fakeProject.FakePackages.Add (new FakePackage ("MyPackage", "1.3.0.6275-pre1"));
			action.PackageId = "MyPackage";
			fakeProject.Name = "MyProject";
			action.UpdateIfPackageDoesNotExistInProject = true;
			RecordMessagesLogged ();

			action.Execute ();

			Assert.IsNull (fakeProject.PackagePassedToUpdatePackage);
			Assert.IsFalse (fakeProject.IsUpdatePackageCalled);
			AssertMessageIsLogged ("No updates available for 'MyPackage' in project 'MyProject'.");
		}

		[Test]
		public void Execute_PackageUpdatedSuccessfully_OpenPackageReadmeMonitorCreated ()
		{
			CreateSolution ();
			updatePackageHelper.TestPackage.Id = "Test";
			updatePackageHelper.UpdateTestPackage ();

			Assert.AreEqual ("Test", action.OpenPackageReadMeMonitor.PackageId);
			Assert.IsTrue (action.OpenPackageReadMeMonitor.IsDisposed);
		}

		[Test]
		public void Execute_PackageInstalledSuccessfullyWithReadmeTxt_ReadmeTxtFileIsOpened ()
		{
			CreateSolution ();
			updatePackageHelper.TestPackage.Id = "Test";
			updatePackageHelper.TestPackage.AddFile ("readme.txt");
			action.CreateOpenPackageReadMeMonitorAction = packageId => {
				return CreateReadMeMonitor (packageId);
			};
			string installPath = @"d:\projects\myproject\packages\Test.1.0".ToNativePath ();
			string readmeFileName = Path.Combine (installPath, "readme.txt");
			fakeProject.UpdatePackageAction = (package, updateAction) => {
				var eventArgs = new PackageOperationEventArgs (package, null, installPath);
				fakeProject.FirePackageInstalledEvent (eventArgs);
				fileService.ExistingFileNames.Add (readmeFileName);
			};
			updatePackageHelper.UpdateTestPackage ();

			Assert.IsTrue (fileService.IsOpenFileCalled);
			Assert.AreEqual (readmeFileName, fileService.FileNamePassedToOpenFile);
		}

		[Test]
		public void Execute_PackageWithReadmeTxtIsInstalledButExceptionThrownWhenAddingPackageToProject_ReadmeFileIsNotOpened ()
		{
			CreateSolution ();
			updatePackageHelper.TestPackage.Id = "Test";
			updatePackageHelper.TestPackage.AddFile ("readme.txt");
			OpenPackageReadMeMonitor monitor = null;
			action.CreateOpenPackageReadMeMonitorAction = packageId => {
				monitor = CreateReadMeMonitor (packageId) as OpenPackageReadMeMonitor;
				return monitor;
			};
			string installPath = @"d:\projects\myproject\packages\Test.1.0".ToNativePath ();
			string readmeFileName = Path.Combine (installPath, "readme.txt");
			fakeProject.UpdatePackageAction = (package, updateAction) => {
				var eventArgs = new PackageOperationEventArgs (package, null, installPath);
				fakeProject.FirePackageInstalledEvent (eventArgs);
				fileService.ExistingFileNames.Add (readmeFileName);
				throw new ApplicationException ();
			};
			Assert.Throws<ApplicationException> (() => {
				updatePackageHelper.UpdateTestPackage ();
			});

			Assert.IsFalse (fileService.IsOpenFileCalled);
			Assert.IsTrue (monitor.IsDisposed);
		}
	}
}

