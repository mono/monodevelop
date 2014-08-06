﻿//
// PackageCompatibilityRunnerTests.cs
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
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageCompatibilityRunnerTests
	{
		TestablePackageCompatibilityRunner runner;
		FakeDotNetProject project;
		FakePackageManagementSolution solution;
		FakeRegisteredPackageRepositories registeredRepositories;
		FakeProgressMonitorFactory progressMonitorFactory;
		PackageManagementEvents packageManagementEvents;
		PackageManagementProgressProvider progressProvider;
		FakePackageRepositoryFactoryEvents repositoryFactoryEvents;
		FakeProgressMonitor progressMonitor;

		void CreateRunner ()
		{
			project = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj");
			project.Name = "MyProject";
			project.TargetFrameworkMoniker = new TargetFrameworkMoniker ("4.5");
			solution = new FakePackageManagementSolution ();
			registeredRepositories = new FakeRegisteredPackageRepositories ();
			progressMonitorFactory = new FakeProgressMonitorFactory ();
			progressMonitor = progressMonitorFactory.ProgressMonitor;
			packageManagementEvents = new PackageManagementEvents ();

			repositoryFactoryEvents = new FakePackageRepositoryFactoryEvents ();
			progressProvider = new PackageManagementProgressProvider (repositoryFactoryEvents, handler => {
				handler.Invoke ();
			});

			runner = new TestablePackageCompatibilityRunner (
				project,
				solution,
				registeredRepositories,
				progressMonitorFactory,
				packageManagementEvents,
				progressProvider);
		}

		void Run ()
		{
			RunWithoutBackgroundDispatch ();
			runner.ExecuteBackgroundDispatch ();
		}

		void RunWithoutBackgroundDispatch ()
		{
			runner.Run ();
		}

		void ProjectHasNoPackageReferences ()
		{
			runner.FileSystem.FileExistsReturnValue = true;
			runner.FileSystem.FileToReturnFromOpenFile = "<packages />";
		}

		void ProjectHasOnePackageReferenceNeedingReinstall (string packageId)
		{
			var package = FakePackage.CreatePackageWithVersion (packageId, "1.2.3.4");
			package.AddFile (@"lib\net45\MyPackage.dll");
			package.AddFile (@"lib\net40\MyPackage.dll");

			FakePackageManagementProject packageManagementProject =
				solution.AddFakeProjectToReturnFromGetProject (project.Name);
			packageManagementProject.FakePackages.Add (package);

			string xml = String.Format (
				@"<packages>
					<package id='{0}' version='1.2.3.4' targetFramework='net40'/>
				</packages>",
				packageId);

			SetProjectPackagesConfigFileContents (xml);
		}

		void SetProjectPackagesConfigFileContents (string xml)
		{
			runner.FileSystem.FileExistsReturnValue = true;
			runner.FileSystem.FileToReturnFromOpenFile = xml;
		}

		void ProjectPackagesAreNotRestored ()
		{
			solution.FakeProjectsToReturnFromGetProject ["MyProject"].FakePackages.Clear ();
		}

		void ProjectHasOnePackageReferenceCompatibleWithCurrentProjectTargetFramework (string packageId)
		{
			ProjectHasOnePackageReferenceNeedingReinstall (packageId);

			solution.FakeProjectsToReturnFromGetProject ["MyProject"].FakePackages [0].FilesList.Clear ();
		}

		void ProjectHasOnePackageReferenceIncompatibleWithCurrentProjectTargetFramework (string packageId)
		{
			ProjectHasOnePackageReferenceNeedingReinstall (packageId);

			FakePackage package = solution.FakeProjectsToReturnFromGetProject ["MyProject"].FakePackages [0];
			package.FilesList.Clear ();
			package.AddFile (@"lib\wp8\MyPackage.dll");
		}

		void FindPackageInProjectThrowsException (string errorMessage)
		{
			FakePackageManagementProject project = solution.FakeProjectsToReturnFromGetProject ["MyProject"];
			project.FindPackageAction = packageId => {
				throw new Exception (errorMessage);
			};
		}

		void AssertPackageMarkedForReinstallationInPackagesConfigFile (string packageId)
		{
			AssertPackageMarkedForReinstallationInPackagesConfigFile (packageId, true);
		}

		void AssertPackageNotMarkedForReinstallationInPackagesConfigFile (string packageId)
		{
			AssertPackageMarkedForReinstallationInPackagesConfigFile (packageId, false);
		}

		void AssertPackageMarkedForReinstallationInPackagesConfigFile (string packageId, bool expectedReinstallationSetting)
		{
			var fileSystem = new FakeFileSystem ();
			fileSystem.FileExistsReturnValue = true;
			MemoryStream stream = runner.FileSystem.FilesAdded.First ().Value;
			string file = UTF8Encoding.UTF8.GetString (stream.ToArray ());
			fileSystem.FileToReturnFromOpenFile = file;
			var packageReferenceFile = new PackageReferenceFile (fileSystem, "packages.config");
			PackageReference matchedReference = packageReferenceFile
				.GetPackageReferences ()
				.FirstOrDefault (packageReference => packageReference.Id == packageId);

			Assert.AreEqual (expectedReinstallationSetting, matchedReference.RequireReinstallation);
		}

		FilePath ConfigurePackagesConfigFilePath (string packagesconfigFileName)
		{
			string fileName = Path.Combine (project.BaseDirectory, packagesconfigFileName);
			runner.FileSystem.PathToReturnFromGetFullPath = fileName.ToNativePath ();
			return new FilePath (fileName.ToNativePath ());
		}

		[Test]
		public void Run_NoPackageReferences_ProgressMonitorCreatedWithInitialProgressStatus ()
		{
			CreateRunner ();
			ProjectHasNoPackageReferences ();

			Run ();

			Assert.AreEqual ("Status", progressMonitorFactory.StatusText);
		}

		[Test]
		public void Run_NoPackageReferences_ProgressMonitorDisposed ()
		{
			CreateRunner ();
			ProjectHasNoPackageReferences ();

			Run ();

			Assert.IsTrue (progressMonitor.IsDisposed);
		}

		[Test]
		public void Run_NoPackageReferences_StatusSuccessMessageDisplayed ()
		{
			CreateRunner ();
			ProjectHasNoPackageReferences ();

			Run ();

			Assert.AreEqual ("Success", progressMonitor.ReportedSuccessMessage);
		}

		[Test]
		public void Run_OnePackageNeedsReinstalling_StatusWarningMessageDisplayed ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceNeedingReinstall ("MyPackageId");

			Run ();

			Assert.AreEqual ("Warning", progressMonitor.ReportedWarningMessage);
		}

		[Test]
		public void Run_OnePackageNeedsReinstalling_PackageIdLoggedInPackageConsole ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceNeedingReinstall ("MyPackageId");

			Run ();

			progressMonitor.AssertMessageIsLogged ("MyPackageId");
		}

		[Test]
		public void Run_OnePackageNeedsReinstalling_PackageReinstallationWarningIsLoggedInPackageConsole ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceNeedingReinstall ("MyPackageId");

			Run ();

			progressMonitor.AssertMessageIsLogged ("should be retargeted");
		}

		[Test]
		public void Run_OnePackageNeedsReinstalling_PackageConsoleIsDisplayed ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceNeedingReinstall ("MyPackageId");

			Run ();

			Assert.IsTrue (runner.PackageConsoleIsShown);
		}

		/// <summary>
		/// TODO: Should display a warning here.
		/// </summary>
		[Test]
		public void Run_OnePackageNeedsReinstallingButPackageIsNotRestored_StatusSuccessMessageDisplayed ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceNeedingReinstall ("MyPackageId");
			ProjectPackagesAreNotRestored ();

			Run ();

			Assert.AreEqual ("Success", progressMonitor.ReportedSuccessMessage);
		}

		[Test]
		public void Run_OnePackageInProjectButNoReinstallNeeded_StatusSuccessMessageDisplayed ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceCompatibleWithCurrentProjectTargetFramework ("MyPackageId");
			ProjectPackagesAreNotRestored ();

			Run ();

			Assert.AreEqual ("Success", progressMonitor.ReportedSuccessMessage);
		}

		[Test]
		public void Run_OnePackageInProjectNeedingReinstallButFindingPackageThrowsException_StatusErrorMessageDisplayed ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceCompatibleWithCurrentProjectTargetFramework ("MyPackageId");
			FindPackageInProjectThrowsException ("error message");

			Run ();

			Assert.AreEqual ("Error", progressMonitor.ReportedErrorMessage);
			progressMonitor.AssertMessageIsLogged ("error message");
		}

		[Test]
		public void Run_OnePackageNeedsReinstalling_NotifyPackagesConfigFileIsChanged ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceNeedingReinstall ("MyPackageId");
			FilePath expectedFilePath = ConfigurePackagesConfigFilePath ("packages.config");

			Run ();

			Assert.AreEqual (1, runner.EventsMonitor.FilesChanged.Count);
			Assert.AreEqual (expectedFilePath, runner.EventsMonitor.FilesChanged [0]);
		}

		[Test]
		public void Run_OnePackageNeedsReinstalling_PackageIsMarkedForReinstallationInPackagesConfigFile ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceNeedingReinstall ("MyPackageId");

			Run ();

			AssertPackageMarkedForReinstallationInPackagesConfigFile ("MyPackageId");
		}

		[Test]
		public void Run_PackagesConfigFileHasReinstallationAttributeSetButPackageDoesNotRequireReinstall_PackagesConfigUpdatedToRemoveReinstallationAttributes ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceCompatibleWithCurrentProjectTargetFramework ("MyPackageId");
			string xml =
				@"<packages>
					<package id='MyPackageId' version='1.2.3.4' targetFramework='net40' requireReinstallation='True' />
				</packages>";
			SetProjectPackagesConfigFileContents (xml);

			Run ();

			AssertPackageNotMarkedForReinstallationInPackagesConfigFile ("MyPackageId");
		}

		[Test]
		public void Run_PackagesConfigFileHasReinstallationAttributeSetButPackageDoesNotRequireReinstall_PackageConfigFileChangedNotificationIsGenerated ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceCompatibleWithCurrentProjectTargetFramework ("MyPackageId");
			string xml =
				@"<packages>
					<package id='MyPackageId' version='1.2.3.4' targetFramework='net40' requireReinstallation='True' />
				</packages>";
			SetProjectPackagesConfigFileContents (xml);
			FilePath expectedFilePath = ConfigurePackagesConfigFilePath ("packages.config");

			Run ();

			Assert.AreEqual (1, runner.EventsMonitor.FilesChanged.Count);
			Assert.AreEqual (expectedFilePath, runner.EventsMonitor.FilesChanged [0]);
		}

		[Test]
		public void Run_PackageDoesNotRequireReinstall_PackagesConfigIsNotUpdated ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceCompatibleWithCurrentProjectTargetFramework ("MyPackageId");

			Run ();

			Assert.AreEqual (0, runner.FileSystem.FilesAdded.Count);
		}

		[Test]
		public void Run_OnePackageInProjectIncompatibleWithNewProjectTargetFramework_IncompatiblePackageErrorMessageDisplayed ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceIncompatibleWithCurrentProjectTargetFramework ("MyPackageId");

			Run ();

			Assert.AreEqual ("Incompatible packages found. Please see Package Console for details.", progressMonitor.ReportedErrorMessage);
			Assert.IsTrue (runner.PackageConsoleIsShown);
		}
	}
}

