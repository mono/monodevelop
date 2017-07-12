//
// InstallNuGetPackageActionTests.cs
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
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class InstallNuGetPackageActionTests
	{
		TestableInstallNuGetPackageAction action;
		FakeSolutionManager solutionManager;
		FakeDotNetProject project;
		FakeNuGetProject nugetProject;
		List<SourceRepository> primaryRepositories;
		FakeNuGetPackageManager packageManager;
		FakePackageMetadataResource packageMetadataResource;
		IPackageManagementEvents packageManagementEvents;
		FakeFileRemover fileRemover;

		void CreateAction (
			string packageId = "Test",
			string version = "2.1")
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

			action = new TestableInstallNuGetPackageAction (
				primaryRepositories,
				solutionManager,
				project);

			packageManager = action.PackageManager;
			packageManagementEvents = action.PackageManagementEvents;
			fileRemover = action.FileRemover;

			action.PackageId = packageId;
			action.Version = new NuGetVersion (version);
		}

		void AddInstallPackageIntoProjectAction (string packageId, string version)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, version, NuGetProjectActionType.Install);
			packageManager.InstallActions.Add (projectAction);
		}

		void ThrowPackageAlreadyInstalledExceptionOnPreviewInstallPackageAsync (string message)
		{
			packageManager.BeforePreviewInstallPackageAsyncAction = () => {
				throw new InvalidOperationException(
					message,
					new PackageAlreadyInstalledException(message));
			};
		}

		[Test]
		public void Execute_PackageIdAndVersionIsSet_ActionsResolvedFromNuGetPackageManager ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = false;
			action.PreserveLocalCopyReferences = false;

			action.Execute ();

			Assert.AreEqual (primaryRepositories, packageManager.PreviewInstallPrimarySources);
			Assert.IsNull (packageManager.PreviewInstallSecondarySources);
			Assert.AreEqual (nugetProject, packageManager.PreviewInstallProject);
			Assert.AreEqual ("Test", packageManager.PreviewInstallPackageIdentity.Id);
			Assert.AreEqual ("1.2", packageManager.PreviewInstallPackageIdentity.Version.ToString ());
			Assert.IsFalse (packageManager.PreviewInstallResolutionContext.IncludePrerelease);
			Assert.AreEqual (VersionConstraints.None, packageManager.PreviewInstallResolutionContext.VersionConstraints);
			Assert.IsTrue (packageManager.PreviewInstallResolutionContext.IncludeUnlisted);
			Assert.AreEqual (DependencyBehavior.Lowest, packageManager.PreviewInstallResolutionContext.DependencyBehavior);
		}

		[Test]
		public void Execute_PackageIdAndVersionIsSet_ActionsAvailableForInstrumentation ()
		{
			CreateAction ();
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (action.GetNuGetProjectActions(), packageManager.InstallActions);
		}

		[Test]
		public void Execute_PackageIdAndVersionIsSet_DirectInstallSetAndCleared ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = false;
			action.PreserveLocalCopyReferences = false;

			action.Execute ();

			Assert.AreEqual ("Test", packageManager.SetDirectInstallPackageIdentity.Id);
			Assert.AreEqual ("1.2", packageManager.SetDirectInstallPackageIdentity.Version.ToString ());
			Assert.AreEqual (action.ProjectContext, packageManager.SetDirectInstallProjectContext);
			Assert.AreSame (action.ProjectContext, packageManager.ClearDirectInstallProjectContext);
		}

		[Test]
		public void Execute_PackageIdAndVersionIsSet_InstallsPackageUsingResolvedActions ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = false;
			action.PreserveLocalCopyReferences = false;
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			AddInstallPackageIntoProjectAction ("A", "2.1");

			action.Execute ();

			Assert.AreEqual (packageManager.InstallActions, packageManager.ExecutedActions);
			Assert.AreEqual (nugetProject, packageManager.ExecutedNuGetProject);
			Assert.AreEqual (action.ProjectContext, packageManager.ExecutedProjectContext);
		}

		[Test]
		public void Execute_NoVersionIsSet_LatestVersionUsedWhenResolvingInstallActions ()
		{
			CreateAction ();
			action.PackageId = "Test";
			action.Version = null;
			action.LicensesMustBeAccepted = false;
			action.PreserveLocalCopyReferences = false;
			packageManager.LatestVersion = new NuGetVersion ("1.2.3.4");

			action.Execute ();

			Assert.AreEqual ("Test", packageManager.PreviewInstallPackageIdentity.Id);
			Assert.AreEqual ("1.2.3.4", packageManager.PreviewInstallPackageIdentity.Version.ToString ());
			Assert.AreEqual (nugetProject, packageManager.GetLatestVersionProject);
			Assert.AreEqual ("Test", packageManager.GetLatestVersionPackageId);
			Assert.AreEqual (primaryRepositories, packageManager.GetLatestVersionSources);
			Assert.AreEqual (DependencyBehavior.Lowest, packageManager.GetLatestVersionResolutionContext.DependencyBehavior);
			Assert.IsFalse (packageManager.GetLatestVersionResolutionContext.IncludePrerelease);
			Assert.IsFalse (packageManager.GetLatestVersionResolutionContext.IncludeUnlisted);
			Assert.AreEqual (VersionConstraints.None, packageManager.GetLatestVersionResolutionContext.VersionConstraints);
		}

		[Test]
		public void Execute_PackageIsSetAndAllowPrereleaseVersionsIsTrue_InstallsPackageAllowingPrereleaseVersions ()
		{
			CreateAction ();
			action.LicensesMustBeAccepted = false;
			action.PreserveLocalCopyReferences = false;
			action.IncludePrerelease = true;

			action.Execute ();

			Assert.IsTrue (packageManager.PreviewInstallResolutionContext.IncludePrerelease);
		}

		[Test]
		public void Execute_NuGetProjectIsBuildIntegratedProject_OnAfterExecuteActionsIsCalled ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = false;
			action.PreserveLocalCopyReferences = false;
			AddInstallPackageIntoProjectAction ("Test", "1.2");

			action.Execute ();

			Assert.AreEqual (packageManager.InstallActions, nugetProject.ActionsPassedToOnAfterExecuteActions);
		}

		[Test]
		public void Execute_NuGetProjectIsBuildIntegratedProject_PostProcessingIsRun ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = false;
			action.PreserveLocalCopyReferences = false;

			action.Execute ();

			Assert.AreEqual (action.ProjectContext, nugetProject.PostProcessProjectContext);
		}

		[Test]
		public void LicensesMustBeAccepted_NewAction_IsTrueByDefault ()
		{
			CreateAction ();

			Assert.IsTrue (action.LicensesMustBeAccepted);
		}

		[Test]
		public void PreserveLocalCopyReferencesd_NewAction_IsTrueByDefault ()
		{
			CreateAction ();

			Assert.IsTrue (action.PreserveLocalCopyReferences);
		}

		[Test]
		public void Execute_PackageHasALicenseToBeAcceptedWhichIsAccepted_UserPromptedToAcceptLicenses ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = true;
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
		public void Execute_LicensesMustBeAcceptedIsFalseAndPackageHasALicenseToBeAccepted_UserNotPromptedToAcceptLicenses ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = false;
			action.LicenseAcceptanceService.AcceptLicensesReturnValue = false;
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var metadata = packageMetadataResource.AddPackageMetadata ("Test", "1.2");
			metadata.RequireLicenseAcceptance = true;
			metadata.LicenseUrl = new Uri ("http://test.com/license");
			action.LicenseAcceptanceService.PackageLicensesAccepted = null;

			action.Execute ();

			Assert.IsNull (action.LicenseAcceptanceService.PackageLicensesAccepted);
		}

		[Test]
		public void Execute_PackageHasALicenseToBeAcceptedWhichIsNotAccepted_ExceptionThrown ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = true;
			action.LicenseAcceptanceService.AcceptLicensesReturnValue = false;
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var metadata = packageMetadataResource.AddPackageMetadata ("Test", "1.2");
			metadata.RequireLicenseAcceptance = true;
			metadata.LicenseUrl = new Uri ("http://test.com/license");

			Exception ex = Assert.Throws (typeof(AggregateException), () => action.Execute ());

			Assert.AreEqual ("Licenses not accepted.", ex.GetBaseException ().Message);
		}

		[Test]
		public void Execute_LicensesMustBeAcceptedButPackageAlreadyInstalled_UserNotPromptedToAcceptLicenses ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = false;
			action.LicenseAcceptanceService.AcceptLicensesReturnValue = false;
			AddInstallPackageIntoProjectAction ("Test", "1.2");
			var metadata = packageMetadataResource.AddPackageMetadata ("Test", "1.2");
			metadata.RequireLicenseAcceptance = true;
			metadata.LicenseUrl = new Uri ("http://test.com/license");
			action.LicenseAcceptanceService.PackageLicensesAccepted = null;
			packageManager.AddPackageToPackagesFolder ("Test", "1.2");

			action.Execute ();

			Assert.IsNull (action.LicenseAcceptanceService.PackageLicensesAccepted);
		}

		[Test]
		public void Execute_PackageAlreadyExistsWhenInstallingItAgainAndReferenceBeingInstalledOriginallyHadLocalCopyFalse_ReferenceAddedHasLocalCopyFalse ()
		{
			CreateAction ();
			action.PreserveLocalCopyReferences = true;
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
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
			Assert.IsTrue (action.PreserveLocalCopyReferences);
		}

		[Test]
		public void Execute_PreserveLocalCopyReferencesSetToFalse_ReferenceThatOriginallyHadLocalCopyFalseIsAddedHasLocalCopySetToTrue ()
		{
			CreateAction ();
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			packageManager.BeforeExecuteAction = () => {
				var referenceBeingRemoved = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
				referenceBeingRemoved.LocalCopy = false;
				packageManagementEvents.OnReferenceRemoving (referenceBeingRemoved);
				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
			};
			action.PreserveLocalCopyReferences = false;

			action.Execute ();

			Assert.IsTrue (firstReferenceBeingAdded.LocalCopy);
			Assert.IsTrue (secondReferenceBeingAdded.LocalCopy);
		}

		[Test]
		public void Execute_PackagesConfigFileDeletedDuringInstall_FileServicePackagesConfigFileDeletionIsCancelled ()
		{
			CreateAction ();
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
		public void Execute_ScriptFileDeletedDuringInstallFileDeletionIsNotCancelled ()
		{
			CreateAction ();
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
		public void Execute_OpenReadmeFileIsFalse_DirectInstallIsNotSetWhichPreventsReadmeFileBeingOpened ()
		{
			CreateAction ("Test", "1.2");
			action.OpenReadmeFile = false;

			action.Execute ();

			Assert.IsNull (packageManager.SetDirectInstallPackageIdentity);
		}

		[Test]
		public void OpenReadmeFile_NewAction_IsTrueByDefault ()
		{
			CreateAction ("Test", "1.2");

			Assert.IsTrue (action.OpenReadmeFile);
		}

		[Test]
		public void Execute_OpenReadmeFileIsTrueAndPackageIsAlreadyInstalledInSolution_DirectInstallIsNotSetWhichPreventsReadmeFileBeingOpened ()
		{
			CreateAction ("Test", "1.2");
			packageManager.AddPackageToPackagesFolder ("Test", "1.2");
			action.OpenReadmeFile = true;

			action.Execute ();

			Assert.IsNull (packageManager.SetDirectInstallPackageIdentity);
		}

		[Test]
		public void Execute_OpenReadmeFileIsTrueAndPackageIsNotAlreadyInstalledInSolution_DirectInstallIsSetWhichAllowsReadmeFileToBeOpened ()
		{
			CreateAction ("Test", "1.2");
			action.OpenReadmeFile = true;

			action.Execute ();

			Assert.AreEqual ("Test", packageManager.SetDirectInstallPackageIdentity.Id);
			Assert.AreEqual ("1.2", packageManager.SetDirectInstallPackageIdentity.Version.ToString ());
		}

		[Test]
		public void Execute_PackageAlreadyInstalled_ExceptionNotThrownAndAndInstallCompletesSuccessfullyWithMessageLogged ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = false;
			ThrowPackageAlreadyInstalledExceptionOnPreviewInstallPackageAsync ("Package already installed");

			action.Execute ();

			Assert.AreEqual ("Package already installed", action.ProjectContext.LastMessageLogged);
			Assert.AreEqual (MessageLevel.Info, action.ProjectContext.LastLogLevel);
		}

		[Test]
		public void Execute_InvalidOperationExceptionThrownDuringPreview_ExceptionIsThrown ()
		{
			CreateAction ("Test", "1.2");
			action.LicensesMustBeAccepted = false;
			packageManager.BeforePreviewInstallPackageAsyncAction = () => {
				throw new InvalidOperationException ("Error");
			};

			var ex = Assert.Throws<AggregateException> (() => action.Execute ());
			var invalidOperationException = ex.GetBaseException () as InvalidOperationException;

			Assert.AreEqual ("Error", invalidOperationException.Message);
		}

		[Test]
		public void GetNuGetProjectActions_NotExecuted_ReturnsEmptyList ()
		{
			CreateAction ("Test");

			Assert.AreEqual (0, action.GetNuGetProjectActions ().Count ());
		}

		[Test]
		public void Execute_PackageVersionIsPrereleaseButIncludePrereleaseIsFalse_ResolutionContextIncludesPrerelease ()
		{
			CreateAction ("Test", "1.2-beta1");
			action.LicensesMustBeAccepted = false;
			action.PreserveLocalCopyReferences = false;
			action.IncludePrerelease = false;

			action.Execute ();

			Assert.IsTrue (packageManager.PreviewInstallResolutionContext.IncludePrerelease);
		}

		[Test]
		public void Execute_IgnoreDependenciesTrue_ResolutionContextIgnoresDependencies ()
		{
			CreateAction ("Test", "1.2-beta1");
			action.LicensesMustBeAccepted = false;
			action.IgnoreDependencies = true;

			action.Execute ();

			Assert.AreEqual (DependencyBehavior.Ignore, packageManager.PreviewInstallResolutionContext.DependencyBehavior);
		}
	}
}

