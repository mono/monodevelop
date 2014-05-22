//
// MonoDevelopProjectManagerTests.cs
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
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class MonoDevelopProjectManagerTests
	{
		TestableProjectManager projectManager;

		void CreateProjectManager ()
		{
			projectManager = new TestableProjectManager ();
		}

		void AddFakePackageToProjectLocalRepository (string packageId, string version)
		{
			projectManager.AddFakePackageToProjectLocalRepository (packageId, version);
		}

		FakePackage CreatePackage (string packageId, string version)
		{
			return FakePackage.CreatePackageWithVersion (packageId, version);
		}

		[Test]
		public void IsInstalled_PackageIdPassedThatDoesNotExistInProjectLocalRepository_ReturnsFalse ()
		{
			CreateProjectManager ();

			bool installed = projectManager.IsInstalled ("Test");

			Assert.IsFalse (installed);
		}

		[Test]
		public void IsInstalled_PackageIdPassedExistsInProjectLocalRepository_ReturnsTrue ()
		{
			CreateProjectManager ();
			projectManager.AddFakePackageToProjectLocalRepository ("Test", "1.0.2");

			bool installed = projectManager.IsInstalled ("Test");

			Assert.IsTrue (installed);
		}

		[Test]
		public void HasOlderPackageInstalled_ProjectLocalRepositoryDoesNotHavePackage_ReturnsFalse ()
		{
			CreateProjectManager ();
			FakePackage package = CreatePackage ("Test", "1.0");

			bool installed = projectManager.HasOlderPackageInstalled (package);

			Assert.IsFalse (installed);
		}

		[Test]
		public void HasOlderPackageInstalled_ProjectLocalRepositoryHasOlderPackage_ReturnsTrue ()
		{
			CreateProjectManager ();
			projectManager.AddFakePackageToProjectLocalRepository ("Test", "1.0");
			FakePackage package = CreatePackage ("Test", "1.1");

			bool installed = projectManager.HasOlderPackageInstalled (package);

			Assert.IsTrue (installed);
		}

		[Test]
		public void HasOlderPackageInstalled_ProjectLocalRepositoryHasSamePackageVersion_ReturnsFalse ()
		{
			CreateProjectManager ();
			projectManager.AddFakePackageToProjectLocalRepository ("Test", "1.1");
			FakePackage package = CreatePackage ("Test", "1.1");

			bool installed = projectManager.HasOlderPackageInstalled (package);

			Assert.IsFalse (installed);
		}
	}
}
