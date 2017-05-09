//
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
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageCompatibilityRunnerTests
	{
		TestablePackageCompatibilityRunner runner;
		FakeDotNetProject project;
		FakeProgressMonitorFactory progressMonitorFactory;
		PackageManagementEvents packageManagementEvents;
		FakeProgressMonitor progressMonitor;

		void CreateRunner ()
		{
			project = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj");
			project.Name = "MyProject";
			project.TargetFrameworkMoniker = new TargetFrameworkMoniker ("4.5");
			progressMonitorFactory = new FakeProgressMonitorFactory ();
			progressMonitor = progressMonitorFactory.ProgressMonitor;
			packageManagementEvents = new PackageManagementEvents ();

			runner = new TestablePackageCompatibilityRunner (
				project,
				progressMonitorFactory,
				packageManagementEvents);
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
			runner.PackageCompatibilityChecker.NuGetProject.InstalledPackages.Clear ();
		}

		void ProjectHasOnePackageReferenceNeedingReinstall (string packageId)
		{
			var packageReference = new PackageReference (
				new PackageIdentity (packageId, NuGetVersion.Parse ("1.2.3.4")),
				NuGetFramework.Parse ("net40"));

			runner.PackageCompatibilityChecker.NuGetProject.InstalledPackages.Add (packageReference);

			runner.PackageCompatibilityChecker.OnCreatePackageCompatibility = item => {
				item.PackageFilesReader.LibItems.Add (new FrameworkSpecificGroup (
					NuGetFramework.Parse ("net45"),
					new [] { @"lib\net45\MyPackage.dll" }));
				
				item.PackageFilesReader.LibItems.Add (new FrameworkSpecificGroup (
					NuGetFramework.Parse ("net40"),
					new [] { @"lib\net40\MyPackage.dll" }));
			};
		}

		void ProjectPackagesAreNotRestored ()
		{
			runner.PackageCompatibilityChecker.FileExistsReturnValue = false;
		}

		void ProjectHasOnePackageReferenceCompatibleWithCurrentProjectTargetFramework (string packageId)
		{
			ProjectHasOnePackageReferenceNeedingReinstall (packageId);
		}

		void ProjectHasOnePackageReferenceIncompatibleWithCurrentProjectTargetFramework (string packageId)
		{
			var packageReference = new PackageReference (
				new PackageIdentity (packageId, NuGetVersion.Parse ("1.2.3.4")),
				NuGetFramework.Parse ("net45"));

			runner.PackageCompatibilityChecker.NuGetProject.InstalledPackages.Add (packageReference);
			runner.PackageCompatibilityChecker.NuGetProject.TargetFramework = NuGetFramework.Parse ("net40");

			runner.PackageCompatibilityChecker.OnCreatePackageCompatibility = item => {
				item.PackageFilesReader.LibItems.Add (new FrameworkSpecificGroup (
					NuGetFramework.Parse ("net45"),
					new [] { @"lib\net45\MyPackage.dll" }));
			};
		}

		void FindPackageInProjectThrowsException (string errorMessage)
		{
			runner.PackageCompatibilityChecker.NuGetProject.ExceptionToThrowWhenInstalledPackagesCalled = 
				new Exception (errorMessage);
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
			var entry = runner.PackageCompatibilityChecker.PackageReferencesToUpdate.FirstOrDefault (
				item => item.Key.PackageIdentity.Id == packageId);

			Assert.AreEqual (expectedReinstallationSetting, entry.Value.RequireReinstallation);
		}

		FilePath ConfigurePackagesConfigFilePath (string packagesconfigFileName)
		{
			string fileName = Path.Combine (project.BaseDirectory, packagesconfigFileName);
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

			var packageReference = new PackageReference (
				new PackageIdentity ("MyPackageId", NuGetVersion.Parse ("1.2.3.4")),
				NuGetFramework.Parse ("net45"),
				false,
				false,
				requireReinstallation: true);

			runner.PackageCompatibilityChecker.NuGetProject.InstalledPackages.Clear ();
			runner.PackageCompatibilityChecker.NuGetProject.InstalledPackages.Add (packageReference);

			Run ();

			AssertPackageNotMarkedForReinstallationInPackagesConfigFile ("MyPackageId");
		}

		[Test]
		public void Run_PackagesConfigFileHasReinstallationAttributeSetButPackageDoesNotRequireReinstall_PackageConfigFileChangedNotificationIsGenerated ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceCompatibleWithCurrentProjectTargetFramework ("MyPackageId");
			var packageReference = new PackageReference (
				new PackageIdentity ("MyPackageId", NuGetVersion.Parse ("1.2.3.4")),
				NuGetFramework.Parse ("net40"),
				false,
				false,
				requireReinstallation: true);

			runner.PackageCompatibilityChecker.NuGetProject.InstalledPackages.Clear ();
			runner.PackageCompatibilityChecker.NuGetProject.InstalledPackages.Add (packageReference);

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
			runner.PackageCompatibilityChecker.NuGetProject.TargetFramework = NuGetFramework.Parse ("net40");

			Run ();

			Assert.AreEqual (0, runner.EventsMonitor.FilesChanged.Count);
		}

		[Test]
		public void Run_OnePackageInProjectIncompatibleWithNewProjectTargetFramework_IncompatiblePackageErrorMessageDisplayed ()
		{
			CreateRunner ();
			ProjectHasOnePackageReferenceIncompatibleWithCurrentProjectTargetFramework ("MyPackageId");

			Run ();

			Assert.AreEqual ("Incompatible packages found.", progressMonitor.ReportedErrorMessage);
			Assert.IsTrue (runner.PackageConsoleIsShown);
		}
	}
}

