//
// UninstallPackageActionTests.cs
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

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class UninstallPackageActionTests
	{
		UninstallPackageAction action;
		PackageManagementEvents packageManagementEvents;
		UninstallPackageHelper uninstallPackageHelper;
		FakePackageManagementProject fakeProject;

		void CreateAction ()
		{
			packageManagementEvents = new PackageManagementEvents ();
			fakeProject = new FakePackageManagementProject ();
			action = new UninstallPackageAction (fakeProject, packageManagementEvents);
			uninstallPackageHelper = new UninstallPackageHelper (action);
		}

		FakePackage AddOnePackageToProjectSourceRepository (string packageId)
		{
			return fakeProject.FakeSourceRepository.AddFakePackage (packageId);
		}

		FakePackage AddOnePackageToProjectSourceRepository (string packageId, string version)
		{
			return fakeProject.FakeSourceRepository.AddFakePackageWithVersion (packageId, version);
		}

		void AddFileToPackageBeingUninstalled (string fileName)
		{
			var package = new FakePackage ();
			package.AddFile (fileName);

			action.Package = package;
		}

		[Test]
		public void Execute_PackageObjectPassed_UninstallsPackageFromProject ()
		{
			CreateAction ();

			uninstallPackageHelper.UninstallTestPackage ();

			var actualPackage = fakeProject.PackagePassedToUninstallPackage;
			var expectedPackage = uninstallPackageHelper.TestPackage;

			Assert.AreEqual (expectedPackage, actualPackage);
		}

		[Test]
		public void Execute_PackageAndPackageRepositoryPassed_OnPackageUninstalledCalledWithPackage ()
		{
			CreateAction ();
			IPackage actualPackage = null;
			packageManagementEvents.ParentPackageUninstalled += (sender, e) => {
				actualPackage = e.Package;
			};

			uninstallPackageHelper.UninstallTestPackage ();

			var expectedPackage = uninstallPackageHelper.TestPackage;
			Assert.AreEqual (expectedPackage, actualPackage);
		}

		[Test]
		public void Execute_PackageObjectPassedAndForceRemoveIsFalse_PackageIsNotForcefullyRemoved ()
		{
			CreateAction ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			uninstallPackageHelper.ForceRemove = false;
			uninstallPackageHelper.UninstallPackageById ("PackageId");

			bool forceRemove = fakeProject.ForceRemovePassedToUninstallPackage;

			Assert.IsFalse (forceRemove);
		}

		[Test]
		public void Execute_PackageObjectPassedAndForceRemoveIsTrue_PackageIsForcefullyRemoved ()
		{
			CreateAction ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			uninstallPackageHelper.ForceRemove = true;
			uninstallPackageHelper.UninstallPackageById ("PackageId");

			bool forceRemove = fakeProject.ForceRemovePassedToUninstallPackage;

			Assert.IsTrue (forceRemove);
		}

		[Test]
		public void Execute_PackageObjectPassedAndRemoveDependenciesIsFalse_PackageDependenciesAreNotRemoved ()
		{
			CreateAction ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			uninstallPackageHelper.RemoveDependencies = false;
			uninstallPackageHelper.UninstallPackageById ("PackageId");

			bool removeDependencies = fakeProject.RemoveDependenciesPassedToUninstallPackage;

			Assert.IsFalse (removeDependencies);
		}

		[Test]
		public void Execute_PackageObjectPassedAndRemoveDependenciesIsTrue_PackageDependenciesAreRemoved ()
		{
			CreateAction ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			uninstallPackageHelper.RemoveDependencies = true;
			uninstallPackageHelper.UninstallPackageById ("PackageId");

			bool removeDependencies = fakeProject.RemoveDependenciesPassedToUninstallPackage;

			Assert.IsTrue (removeDependencies);
		}

		[Test]
		public void Execute_PackageIdSpecifiedAndForceRemoveIsTrue_PackageIsForcefullyRemoved ()
		{
			CreateAction ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			uninstallPackageHelper.ForceRemove = true;
			uninstallPackageHelper.UninstallPackageById ("PackageId");

			bool forceRemove = fakeProject.ForceRemovePassedToUninstallPackage;

			Assert.IsTrue (forceRemove);
		}

		[Test]
		public void Execute_PackageIdSpecifiedAndRemoveDependenciesIsTrue_PackageDependenciesAreRemoved ()
		{
			CreateAction ();
			fakeProject.AddFakePackageToSourceRepository ("PackageId");
			uninstallPackageHelper.RemoveDependencies = true;
			uninstallPackageHelper.UninstallPackageById ("PackageId");

			bool removeDependencies = fakeProject.RemoveDependenciesPassedToUninstallPackage;

			Assert.IsTrue (removeDependencies);
		}

		[Test]
		public void Execute_VersionSpecified_VersionUsedWhenSearchingForPackage ()
		{
			CreateAction ();

			AddOnePackageToProjectSourceRepository ("PackageId", "1.2.0.0");
			AddOnePackageToProjectSourceRepository ("PackageId", "1.0.0.0");
			FakePackage package = AddOnePackageToProjectSourceRepository ("PackageId", "1.1.0");

			uninstallPackageHelper.Version = package.Version;
			uninstallPackageHelper.UninstallPackageById ("PackageId");

			var actualPackage = fakeProject.PackagePassedToUninstallPackage;

			Assert.AreEqual (package, actualPackage);
		}

		[Test]
		public void ForceRemove_DefaultValue_ReturnsFalse ()
		{
			CreateAction ();
			Assert.IsFalse (action.ForceRemove);
		}

		[Test]
		public void RemoveDependencies_DefaultValue_ReturnsFalse ()
		{
			CreateAction ();
			Assert.IsFalse (action.RemoveDependencies);
		}

		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasUninstallPowerShellScript_ReturnsTrue ()
		{
			CreateAction ();
			AddFileToPackageBeingUninstalled (@"tools\uninstall.ps1");

			bool hasPackageScripts = action.HasPackageScriptsToRun ();

			Assert.IsTrue (hasPackageScripts);
		}

		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasNoFiles_ReturnsFalse ()
		{
			CreateAction ();
			action.Package = new FakePackage ();

			bool hasPackageScripts = action.HasPackageScriptsToRun ();

			Assert.IsFalse (hasPackageScripts);
		}

		[Test]
		public void HasPackageScriptsToRun_OnePackageInOperationsHasUninstallPowerShellScriptInUpperCase_ReturnsTrue ()
		{
			CreateAction ();
			AddFileToPackageBeingUninstalled (@"tools\UNINSTALL.PS1");

			bool hasPackageScripts = action.HasPackageScriptsToRun ();

			Assert.IsTrue (hasPackageScripts);
		}

		[Test]
		public void AllowPreleasePackages_DefaultValue_IsTrue ()
		{
			CreateAction ();

			Assert.IsTrue (action.AllowPrereleaseVersions);
		}

		[Test]
		public void Execute_PackageHasPowerShellUninstallScript_PowerShellInfoLogged ()
		{
			CreateAction ();
			FakePackage package = FakePackage.CreatePackageWithVersion ("Test", "1.0");
			action.Package = package;
			package.AddFile (@"tools\uninstall.ps1");
			var messagesLogged = new List<string> ();
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				if (e.Message.Level == MessageLevel.Info) {
					messagesLogged.Add (e.Message.ToString ());
				}
			};

			action.Execute ();

			Assert.That (messagesLogged, Contains.Item ("WARNING: Test Package contains PowerShell scripts which will not be run."));
		}

		[Test]
		public void Execute_PackageHasPowerShellInstallScript_NoPowerShellWarningLogged ()
		{
			CreateAction ();
			FakePackage package = FakePackage.CreatePackageWithVersion ("Test", "1.0");
			action.Package = package;
			package.AddFile (@"tools\install.ps1");
			var messagesLogged = new List<string> ();
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				messagesLogged.Add (e.Message.ToString ());
			};

			action.Execute ();

			Assert.IsFalse (messagesLogged.Any (message => message.Contains ("PowerShell")));
		}
	}
}

