//
// UpdateAllNuGetPackagesInProjectActionTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	public class UpdateAllNuGetPackagesInProjectActionTests
	{
		TestableUpdateAllNuGetPackagesInProjectAction action;
		FakeSolutionManager solutionManager;
		FakeDotNetProject project;
		FakeNuGetProject nugetProject;
		List<SourceRepository> primaryRepositories;
		FakeNuGetPackageManager packageManager;
		FakePackageMetadataResource packageMetadataResource;
		PackageManagementEvents packageManagementEvents;
		FakeFileRemover fileRemover;
		FakePackageRestoreManager restoreManager;

		void CreateAction ()
		{
			project = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj");
			solutionManager = new FakeSolutionManager ();
			nugetProject = new FakeNuGetProject (project);
			solutionManager.NuGetProjects[project] = nugetProject;

			var metadataResourceProvider = new FakePackageMetadataResourceProvider ();
			packageMetadataResource = metadataResourceProvider.PackageMetadataResource;
			var source = new PackageSource ("http://test.com");
			var providers = new INuGetResourceProvider[] {
				metadataResourceProvider
			};
			var sourceRepository = new SourceRepository (source, providers);
			primaryRepositories = new [] {
				sourceRepository
			}.ToList ();
			solutionManager.SourceRepositoryProvider.Repositories.AddRange (primaryRepositories);

			action = new TestableUpdateAllNuGetPackagesInProjectAction (
				solutionManager,
				project);

			packageManager = action.PackageManager;
			packageManagementEvents = action.PackageManagementEvents;
			fileRemover = action.FileRemover;
			restoreManager = action.RestoreManager;
		}

		void AddInstallPackageIntoProjectAction (string packageId, string version)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, version, NuGetProjectActionType.Install);
			packageManager.UpdateActions.Add (projectAction);
		}

		void AddUninstallPackageFromProjectAction (string packageId, string version)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, version, NuGetProjectActionType.Uninstall);
			packageManager.UpdateActions.Add (projectAction);
		}

		void AddPackageToProject (string packageId, string version)
		{
			nugetProject.AddPackageReference (packageId, version);
		}

		void AddUnrestoredPackageForProject (string projectName)
		{
			restoreManager.AddUnrestoredPackageForProject (projectName, solutionManager.SolutionDirectory);
		}

		[Test]
		public void Execute_PackagesAreRestoredAndNoPrereleasePackages_ActionsResolvedFromNuGetPackageManager ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (primaryRepositories, packageManager.PreviewUpdatePrimarySources);
			Assert.AreEqual (new SourceRepository[0], packageManager.PreviewUpdateSecondarySources);
			Assert.AreEqual (nugetProject, packageManager.PreviewUpdateProject);
			Assert.IsNull (packageManager.PreviewUpdatePackageId);
			Assert.IsFalse (packageManager.PreviewUpdateResolutionContext.IncludePrerelease);
			Assert.AreEqual (VersionConstraints.None, packageManager.PreviewUpdateResolutionContext.VersionConstraints);
			Assert.IsFalse (packageManager.PreviewUpdateResolutionContext.IncludeUnlisted);
			Assert.AreEqual (DependencyBehavior.Lowest, packageManager.PreviewUpdateResolutionContext.DependencyBehavior);
		}

		[Test]
		public void Execute_PackagesAreRestoredAndNoPrereleasePackages_ActionsAvailableForInstrumentation ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (action.GetNuGetProjectActions (), packageManager.UpdateActions);
		}

		[Test]
		public void Execute_PackagesAreRestoredAndNoPrereleasePackages_UpdatesPackageUsingResolvedActions ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddInstallPackageIntoProjectAction ("A", "2.1");

			action.Execute ();

			Assert.AreEqual (packageManager.UpdateActions, packageManager.ExecutedActions);
			Assert.AreEqual (nugetProject, packageManager.ExecutedNuGetProject);
			Assert.AreEqual (action.ProjectContext, packageManager.ExecutedProjectContext);
		}

		[Test]
		public void Execute_ProjectHasPrereleasePackage_ActionsResolvedFromNuGetPackageManagerWithIncludePrereleaseSetToFalse ()
		{
			CreateAction ();
			AddPackageToProject ("Test", "1.0.1-alpha");
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.IsFalse (packageManager.PreviewUpdateResolutionContext.IncludePrerelease);
		}

		[Test]
		public void Execute_NoActions_NoUpdateFoundEventFires ()
		{
			CreateAction ();
			IDotNetProject noUpdateFoundForProject = null;
			packageManagementEvents.NoUpdateFound += (sender, e) => {
				noUpdateFoundForProject = e.Project;
			};

			action.Execute ();

			Assert.AreEqual (project, noUpdateFoundForProject);
		}

		[Test]
		public void Execute_OneAction_NoUpdateFoundEventDoesNotFire ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			IDotNetProject noUpdateFoundForProject = null;
			packageManagementEvents.NoUpdateFound += (sender, e) => {
				noUpdateFoundForProject = e.Project;
			};

			action.Execute ();

			Assert.IsNull (noUpdateFoundForProject);
		}

		[Test]
		public void Execute_NoActions_NoActionsExecuted ()
		{
			CreateAction ();
			IDotNetProject noUpdateFoundForProject = null;
			packageManagementEvents.NoUpdateFound += (sender, e) => {
				noUpdateFoundForProject = e.Project;
			};

			action.Execute ();

			Assert.IsNull (packageManager.ExecutedActions);
			Assert.AreEqual (project, noUpdateFoundForProject);
		}

		[Test]
		public void Execute_OnePackageNotRestored_PackageIsRestored ()
		{
			CreateAction ();
			solutionManager.SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
			project.Name = "MyProject";
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddUnrestoredPackageForProject ("MyProject");

			action.Execute ();

			Assert.AreEqual (solutionManager.SolutionDirectory, restoreManager.RestoreMissingPackagesSolutionDirectory);
			Assert.AreEqual (action.ProjectContext, restoreManager.RestoreMissingPackagesProjectContext);
			Assert.AreEqual (nugetProject, restoreManager.RestoreMissingPackagesProject);
		}

		[Test]
		public void Execute_OnePackageNotRestored_ProjectReferencesRefreshed ()
		{
			CreateAction ();
			solutionManager.SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
			project.Name = "MyProject";
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddUnrestoredPackageForProject ("MyProject");

			action.Execute ();

			Assert.IsTrue (project.IsReferenceStatusRefreshed);
		}

		[Test]
		public void Execute_OnePackageNotRestored_PackagesRestoredEventIsFired ()
		{
			CreateAction ();
			solutionManager.SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
			project.Name = "MyProject";
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddUnrestoredPackageForProject ("MyProject");
			bool packagesRestored = false;
			packageManagementEvents.PackagesRestored += (sender, e) => packagesRestored = true;

			action.Execute ();

			Assert.IsTrue (packagesRestored);
		}

		[Test]
		public void Execute_OnePackageNotRestoredAndPackageRestoreFails_ExceptionThrownAndRestoreFailureMessageLogged ()
		{
			CreateAction ();
			solutionManager.SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
			project.Name = "MyProject";
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddUnrestoredPackageForProject ("MyProject");
			string messageLogged = null;
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				messageLogged = e.Message.ToString ();
			};
			restoreManager.BeforeRestoreMissingPackagesAsync = () => {
				var exception = new Exception ("RestoreErrorMessage");
				restoreManager.RaisePackageRestoreFailedEvent (exception, "MyProject");
			};

			var ex = Assert.Throws<AggregateException> (() => {
				action.Execute ();
			});

			Assert.AreEqual ("Package restore failed for project MyProject: RestoreErrorMessage", messageLogged);
			Assert.AreEqual ("Package restore failed.", ex.GetBaseException ().Message);
		}

		[Test]
		public void Execute_PackagesConfigFileDeletedDuringUpdate_FileServicePackagesConfigFileDeletionIsCancelled ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			string expectedFileName = @"d:\projects\MyProject\packages.config".ToNativePath ();
			bool? fileRemovedResult = null;
			packageManager.BeforeExecuteAction = () => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (expectedFileName);
			};

			action.Execute ();

			Assert.AreEqual (expectedFileName, fileRemover.FileRemoved);
			Assert.IsFalse (fileRemovedResult.Value);
		}

		[Test]
		public void Execute_ScriptFileDeletedDuringUpdate_FileDeletionIsNotCancelled ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			string fileName = @"d:\projects\MyProject\scripts\myscript.js".ToNativePath ();
			bool? fileRemovedResult = null;
			packageManager.BeforeExecuteAction = () => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (fileName);
			};

			action.Execute ();

			Assert.IsTrue (fileRemovedResult.Value);
			Assert.IsNull (fileRemover.FileRemoved);
		}

		[Test]
		public void Execute_ReferenceBeingUpdatedHasLocalCopyTrue_ReferenceAddedHasLocalCopyTrue ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			packageManager.BeforeExecuteAction = () => {
				var referenceBeingRemoved = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
				referenceBeingRemoved.LocalCopy = true;
				packageManagementEvents.OnReferenceRemoving (referenceBeingRemoved);
				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
			};

			action.Execute ();

			Assert.IsTrue (firstReferenceBeingAdded.LocalCopy);
			Assert.IsTrue (secondReferenceBeingAdded.LocalCopy);
		}

		[Test]
		public void Execute_ReferenceBeingUpdatedHasLocalCopyTrueButCaseIsDifferent_ReferenceAddedHasLocalCopyTrue ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			packageManager.BeforeExecuteAction = () => {
				var referenceBeingRemoved = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "nunit.framework");
				referenceBeingRemoved.LocalCopy = true;
				packageManagementEvents.OnReferenceRemoving (referenceBeingRemoved);
				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
			};

			action.Execute ();

			Assert.IsTrue (firstReferenceBeingAdded.LocalCopy);
			Assert.IsTrue (secondReferenceBeingAdded.LocalCopy);
		}

		[Test]
		public void Execute_ReferenceBeingUpdatedHasLocalCopyFalse_ReferenceAddedHasLocalCopyFalse ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			firstReferenceBeingAdded.LocalCopy = true;
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			packageManager.BeforeExecuteAction = () => {
				var referenceBeingRemoved = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
				referenceBeingRemoved.LocalCopy = false;
				packageManagementEvents.OnReferenceRemoving (referenceBeingRemoved);
				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
			};
			action.Execute ();

			Assert.IsTrue (firstReferenceBeingAdded.LocalCopy);
			Assert.IsFalse (secondReferenceBeingAdded.LocalCopy);
		}

		[Test]
		public void Execute_ReferenceBeingUpdatedHasLocalCopyFalseButCaseIsDifferent_ReferenceAddedHasLocalCopyFalse ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			firstReferenceBeingAdded.LocalCopy = true;
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			packageManager.BeforeExecuteAction = () => {
				var referenceBeingRemoved = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "nunit.framework");
				referenceBeingRemoved.LocalCopy = false;
				packageManagementEvents.OnReferenceRemoving (referenceBeingRemoved);
				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
			};

			action.Execute ();

			Assert.IsTrue (firstReferenceBeingAdded.LocalCopy);
			Assert.IsFalse (secondReferenceBeingAdded.LocalCopy);
		}

		[Test]
		public void Execute_PackagesConfigFileNamedAfterProjectDeletedDuringUpdate_FileServicePackagesConfigFileDeletionIsCancelled ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			string expectedFileName = @"d:\projects\MyProject\packages.MyProject.config".ToNativePath ();
			bool? fileRemovedResult = null;
			packageManager.BeforeExecuteAction = () => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (expectedFileName);
			};
			action.Execute ();

			Assert.AreEqual (expectedFileName, fileRemover.FileRemoved);
			Assert.IsFalse (fileRemovedResult.Value);
		}

		[Test]
		public void Execute_NuGetProjectIsBuildIntegratedProject_OnAfterExecuteActionsIsCalled ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (packageManager.UpdateActions, nugetProject.ActionsPassedToOnAfterExecuteActions);
		}

		[Test]
		public void Execute_NuGetProjectIsBuildIntegratedProject_PostProcessingIsRun ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (action.ProjectContext, nugetProject.PostProcessProjectContext);
		}

		[Test]
		public void Execute_PackageHasALicenseToBeAcceptedWhichIsAccepted_UserPromptedToAcceptLicenses ()
		{
			CreateAction ();
			action.LicenseAcceptanceService.AcceptLicensesReturnValue = true;
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var metadata = packageMetadataResource.AddPackageMetadata ("Test", "1.2");
			metadata.RequireLicenseAcceptance = true;
			metadata.LicenseUrl = new Uri ("http://test.com/license");

			action.Execute ();

			var license = action.LicenseAcceptanceService.PackageLicensesAccepted.Single ();
			Assert.AreEqual ("Test", license.PackageId);
			Assert.AreEqual (metadata.LicenseUrl, license.LicenseUrl);
			Assert.AreEqual (metadata.Authors, license.PackageAuthor);
			Assert.AreEqual (metadata.Title, license.PackageTitle);
			Assert.AreEqual ("Test", license.PackageIdentity.Id);
			Assert.AreEqual ("1.2", license.PackageIdentity.Version.ToString ());
		}

		[Test]
		public void Execute_PackageHasALicenseToBeAcceptedWhichIsNotAccepted_ExceptionThrown ()
		{
			CreateAction ();
			action.LicenseAcceptanceService.AcceptLicensesReturnValue = false;
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var metadata = packageMetadataResource.AddPackageMetadata ("Test", "1.2");
			metadata.RequireLicenseAcceptance = true;
			metadata.LicenseUrl = new Uri ("http://test.com/license");

			Exception ex = Assert.Throws (typeof(AggregateException), () => action.Execute ());

			Assert.AreEqual ("Licenses not accepted.", ex.GetBaseException ().Message);
		}

		[Test]
		public void Execute_OnePackageIsUpdated_OpenReadmeFileForPackageIfItExists ()
		{
			CreateAction ();
			AddPackageToProject ("Test", "1.0");
			AddUninstallPackageFromProjectAction ("Test", "1.0");
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			var packageIdentity = packageManager.OpenReadmeFilesForPackages.Single ();
			Assert.AreEqual (nugetProject, packageManager.OpenReadmeFilesForProject);
			Assert.AreEqual (action.ProjectContext, packageManager.OpenReadmeFilesWithProjectContext);
			Assert.AreEqual ("Test", packageIdentity.Id);
			Assert.AreEqual ("1.2", packageIdentity.Version.ToString ());
		}
	}
}

