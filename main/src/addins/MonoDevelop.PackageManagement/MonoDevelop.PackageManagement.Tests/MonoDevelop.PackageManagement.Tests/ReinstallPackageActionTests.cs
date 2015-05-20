//
// ReinstallPackageActionTests.cs
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
using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ReinstallPackageActionTests
	{
		ReinstallPackageAction action;
		PackageManagementEvents packageManagementEvents;
		FakePackageManagementProject project;

		void CreateAction (string packageId = "MyPackage", string packageVersion = "1.2.3.4")
		{
			project = new FakePackageManagementProject ();
			project.AddFakeInstallOperation ();

			packageManagementEvents = new PackageManagementEvents ();

			action = new ReinstallPackageAction (project, packageManagementEvents);
			action.PackageId = packageId;
			action.PackageVersion = new SemanticVersion (packageVersion);
		}

		FakePackage AddPackageToSourceRepository (string packageId, string packageVersion)
		{
			return project.FakeSourceRepository.AddFakePackageWithVersion (packageId, packageVersion);
		}

		[Test]
		public void Execute_PackageExistsInSourceRepository_PackageIsUninstalled ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			FakePackage package = AddPackageToSourceRepository ("MyPackage", "1.2.3.4");

			action.Execute ();

			Assert.IsTrue (project.FakeUninstallPackageAction.IsExecuted);
			Assert.AreEqual (package, project.FakeUninstallPackageAction.Package);
		}

		[Test]
		public void Execute_PackageExistsInSourceRepository_PackageIsInstalled ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			FakePackage package = AddPackageToSourceRepository ("MyPackage", "1.2.3.4");

			action.Execute ();

			Assert.IsTrue (project.LastInstallPackageCreated.IsExecuteCalled);
			Assert.AreEqual (package, project.LastInstallPackageCreated.Package);
		}

		[Test]
		public void Execute_PackageExistsInSourceRepository_PackageIsForcefullyRemovedSoItDoesNotFailIfOtherPackagesDependOnIt ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			AddPackageToSourceRepository ("MyPackage", "1.2.3.4");

			action.Execute ();

			Assert.IsTrue (project.FakeUninstallPackageAction.ForceRemove);
		}

		[Test]
		public void Execute_PackageExistsInSourceRepository_PackageIsInstalledWithoutOpeningReadmeTxt ()
		{
			CreateAction ("MyPackage", "1.2.3.4");
			FakePackage package = AddPackageToSourceRepository ("MyPackage", "1.2.3.4");

			action.Execute ();

			Assert.IsTrue (project.LastInstallPackageCreated.IsExecuteCalled);
			Assert.IsFalse (project.LastInstallPackageCreated.OpenReadMeText);
		}
	}
}

