//
// ReinstallNuGetPackageActionTests.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ReinstallNuGetPackageActionTests
	{
		TestableReinstallNuGetPackageAction action;
		FakeSolutionManager solutionManager;
		FakeDotNetProject project;
		FakeNuGetProject nugetProject;
		List<SourceRepository> primaryRepositories;
		IPackageManagementEvents packageManagementEvents;
		FakeFileRemover fileRemover;
		FakeNuGetPackageManager uninstallPackageManager;
		FakeNuGetPackageManager installPackageManager;

		void CreateAction (
			string packageId = "Test",
			string version = "2.1")
		{
			project = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj");
			solutionManager = new FakeSolutionManager ();
			nugetProject = new FakeNuGetProject (project);
			solutionManager.NuGetProjects[project] = nugetProject;

			var repositoryProvider = solutionManager.SourceRepositoryProvider;
			var source = new PackageSource ("http://test.com");
			repositoryProvider.AddRepository (source);
			primaryRepositories = repositoryProvider.Repositories;

			action = new TestableReinstallNuGetPackageAction (
				project,
				solutionManager);

			packageManagementEvents = action.PackageManagementEvents;
			fileRemover = action.FileRemover;

			action.PackageId = packageId;
			action.Version = new NuGetVersion (version);

			uninstallPackageManager = action.UninstallAction.PackageManager;
			installPackageManager = action.InstallAction.PackageManager;
		}

		void AddInstallPackageIntoProjectAction (string packageId, string version)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, version, NuGetProjectActionType.Install);
			action.InstallAction.PackageManager.InstallActions.Add (projectAction);
		}

		void AddUninstallPackageIntoProjectAction (string packageId, string version)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, version, NuGetProjectActionType.Uninstall);
			action.UninstallAction.PackageManager.UninstallActions.Add (projectAction);
		}

		[Test]
		public void Execute_PackageExists_PackageIsForcefullyUninstalled ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			AddUninstallPackageIntoProjectAction ("MyPackage", "1.2.3.4");

			action.Execute ();

			var executedAction = uninstallPackageManager.ExecutedActions.Single ();
			Assert.AreEqual ("MyPackage", executedAction.PackageIdentity.Id);
			Assert.AreEqual ("1.2.3.4", executedAction.PackageIdentity.Version.ToString ());
			Assert.AreEqual ("MyPackage", action.UninstallAction.PackageId);
			Assert.IsTrue (action.UninstallAction.ForceRemove);
		}

		[Test]
		public void Execute_PackageExists_PackageIsInstalled ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			AddInstallPackageIntoProjectAction ("MyPackage", "1.2.3.4");

			action.Execute ();

			var executedAction = installPackageManager.ExecutedActions.Single ();
			Assert.AreEqual ("MyPackage", executedAction.PackageIdentity.Id);
			Assert.AreEqual ("1.2.3.4", executedAction.PackageIdentity.Version.ToString ());
			Assert.AreEqual ("MyPackage", action.InstallAction.PackageId);
			Assert.AreEqual ("1.2.3.4", action.InstallAction.Version.ToString ());
			Assert.IsFalse (action.InstallAction.LicensesMustBeAccepted);
			Assert.IsFalse (action.InstallAction.PreserveLocalCopyReferences);
			Assert.AreEqual (primaryRepositories, installPackageManager.PreviewInstallPrimarySources);
		}

		[Test]
		public void Execute_ReferenceHasLocalCopyFalseWhenUninstalled_ReferenceHasLocalCopyFalseAfterBeingReinstalled ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			AddUninstallPackageIntoProjectAction ("MyPackage", "1.2.3.4");
			AddInstallPackageIntoProjectAction ("MyPackage", "1.2.3.4");
			var firstReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NewAssembly");
			var secondReferenceBeingAdded = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
			uninstallPackageManager.BeforeExecuteAction = () => {
				var referenceBeingRemoved = ProjectReference.CreateCustomReference (ReferenceType.Assembly, "NUnit.Framework");
				referenceBeingRemoved.LocalCopy = false;
				packageManagementEvents.OnReferenceRemoving (referenceBeingRemoved);
			};
			bool installActionMaintainsLocalCopyReferences = false;
			installPackageManager.BeforeExecuteAction = () => {
				installActionMaintainsLocalCopyReferences = action.InstallAction.PreserveLocalCopyReferences;
				packageManagementEvents.OnReferenceAdding (firstReferenceBeingAdded);
				packageManagementEvents.OnReferenceAdding (secondReferenceBeingAdded);
			};

			action.Execute ();

			Assert.IsTrue (firstReferenceBeingAdded.LocalCopy);
			Assert.IsFalse (secondReferenceBeingAdded.LocalCopy);
			Assert.IsFalse (installActionMaintainsLocalCopyReferences, "Should be false since the reinstall action will maintain the local copies");
		}

		[Test]
		public void Execute_PackagesConfigFileDeletedDuringUninstall_FileServicePackagesConfigFileDeletionIsCancelled ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			AddUninstallPackageIntoProjectAction ("MyPackage", "1.2.3.4");
			string expectedFileName = @"d:\projects\MyProject\packages.config".ToNativePath ();
			bool? fileRemovedResult = null;
			uninstallPackageManager.BeforeExecuteAction = () => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (expectedFileName);
			};

			action.Execute ();

			Assert.AreEqual (expectedFileName, fileRemover.FileRemoved);
			Assert.IsFalse (fileRemovedResult.Value);
		}

		[Test]
		public void Execute_ScriptFileDeletedDuringUninstall_FileDeletionIsNotCancelled ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			AddUninstallPackageIntoProjectAction ("MyPackage", "1.2.3.4");
			string fileName = @"d:\projects\MyProject\scripts\myscript.js".ToNativePath ();
			bool? fileRemovedResult = null;
			uninstallPackageManager.BeforeExecuteAction = () => {
				fileRemovedResult = packageManagementEvents.OnFileRemoving (fileName);
			};

			action.Execute ();

			Assert.IsTrue (fileRemovedResult.Value);
			Assert.IsNull (fileRemover.FileRemoved);
		}

		[Test]
		public void Execute_PackageExists_PackageIsInstalledWithoutReOpeningReadmeFile ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			AddInstallPackageIntoProjectAction ("MyPackage", "1.2.3.4");

			action.Execute ();

			var executedAction = installPackageManager.ExecutedActions.Single ();
			Assert.AreEqual ("MyPackage", executedAction.PackageIdentity.Id);
			Assert.AreEqual ("MyPackage", action.InstallAction.PackageId);
			Assert.IsFalse (action.InstallAction.OpenReadmeFile);
		}
	}
}

