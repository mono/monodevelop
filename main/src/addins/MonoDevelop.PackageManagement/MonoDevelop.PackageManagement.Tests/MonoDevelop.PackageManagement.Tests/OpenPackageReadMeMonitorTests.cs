//
// OpenPackageReadMeMonitorTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class OpenPackageReadMeMonitorTests
	{
		FakeFileService fileService;
		FakePackageManagementProject project;

		OpenPackageReadMeMonitor CreateMonitor (string packageId)
		{
			fileService = new FakeFileService (null);
			project = new FakePackageManagementProject ();
			return new OpenPackageReadMeMonitor (packageId, project, fileService);
		}

		FakePackage CreatePackageWithFile (string packageId, string fileName)
		{
			var package = new FakePackage (packageId);
			package.AddFile (fileName);
			return package;
		}

		PackageOperationEventArgs CreatePackageInstallEventWithFile (string installPath, IPackage package)
		{
			return new PackageOperationEventArgs (package, null, installPath);
		}

		[Test]
		public void OpenReadMeFile_PackageInstalledWithReadmeTxt_ReadmeTxtIsOpened ()
		{
			const string installPath = @"d:\projects\myproject\packages\Test.1.2.0";
			string expectedFileOpened = Path.Combine (installPath, "readme.txt");

			using (OpenPackageReadMeMonitor monitor = CreateMonitor ("Test")) {
				fileService.ExistingFileNames.Add (expectedFileOpened);
				FakePackage package = CreatePackageWithFile ("Test", "readme.txt");
				PackageOperationEventArgs e = CreatePackageInstallEventWithFile (installPath, package);
				project.FirePackageInstalledEvent (e);
				monitor.OpenReadMeFile ();
			}

			Assert.AreEqual (expectedFileOpened, fileService.FileNamePassedToOpenFile);
		}

		[Test]
		public void Dispose_PackageInstalledWithReadmeTxtButOpenReadMeFileMethodIsNotCalled_ReadmeTxtIsNotOpened ()
		{
			const string installPath = @"d:\projects\myproject\packages\Test.1.2.0";
			string expectedFileOpened = Path.Combine (installPath, "readme.txt");

			using (OpenPackageReadMeMonitor monitor = CreateMonitor ("Test")) {
				fileService.ExistingFileNames.Add (expectedFileOpened);
				FakePackage package = CreatePackageWithFile ("Test", "readme.txt");
				PackageOperationEventArgs e = CreatePackageInstallEventWithFile (installPath, package);
				project.FirePackageInstalledEvent (e);
			}

			Assert.IsFalse (fileService.IsOpenFileCalled);
		}

		[Test]
		public void OpenReadMeFile_PackageDependencyIsInstalledWithReadmeTxt_ReadmeTxtIsNotOpened ()
		{
			const string installPath = @"d:\projects\myproject\packages\Test.Dependency.1.2.0";
			string expectedFileOpened = Path.Combine (installPath, "readme.txt");

			using (OpenPackageReadMeMonitor monitor = CreateMonitor ("Test")) {
				fileService.ExistingFileNames.Add (expectedFileOpened);
				FakePackage package = CreatePackageWithFile ("Test.Dependency", "readme.txt");
				PackageOperationEventArgs e = CreatePackageInstallEventWithFile (installPath, package);
				project.FirePackageInstalledEvent (e);
				monitor.OpenReadMeFile ();
			}

			Assert.IsFalse (fileService.IsOpenFileCalled);
		}

		[Test]
		public void OpenReadMeFile_PackageInstalledWithoutReadmeTxt_ReadmeTxtIsNotOpened ()
		{
			const string installPath = @"d:\projects\myproject\packages\Test.1.2.0";
			string expectedFileOpened = Path.Combine (installPath, "readme.txt");

			using (OpenPackageReadMeMonitor monitor = CreateMonitor ("Test")) {
				fileService.ExistingFileNames.Add (expectedFileOpened);
				var package = new FakePackage ("Test");
				PackageOperationEventArgs e = CreatePackageInstallEventWithFile (installPath, package);
				project.FirePackageInstalledEvent (e);
				monitor.OpenReadMeFile ();
			}

			Assert.IsFalse (fileService.IsOpenFileCalled);
		}

		[Test]
		public void OpenReadMeFile_PackageDependencyIsInstalledWithReadmeTxtWithDifferentCase_ReadmeTxtIsOpened ()
		{
			const string installPath = @"d:\projects\myproject\packages\Test.1.2.0";
			string expectedFileOpened = Path.Combine (installPath, "ReadMe.TXT");

			using (OpenPackageReadMeMonitor monitor = CreateMonitor ("Test")) {
				fileService.ExistingFileNames.Add (expectedFileOpened);
				FakePackage package = CreatePackageWithFile ("Test", "ReadMe.TXT");
				PackageOperationEventArgs e = CreatePackageInstallEventWithFile (installPath, package);
				project.FirePackageInstalledEvent (e);
				monitor.OpenReadMeFile ();
			}

			Assert.AreEqual (expectedFileOpened, fileService.FileNamePassedToOpenFile);
		}

		[Test]
		public void OpenReadMeFile_PackageInstalledWithReadmeTxtButFileDoesNotExistOnFileSystem_ReadmeTxtIsNotOpened ()
		{
			const string installPath = @"d:\projects\myproject\packages\Test.1.2.0";
			string readmeFileName = Path.Combine (installPath, "readme.txt");

			using (OpenPackageReadMeMonitor monitor = CreateMonitor ("Test")) {
				FakePackage package = CreatePackageWithFile ("Test", "readme.txt");
				PackageOperationEventArgs e = CreatePackageInstallEventWithFile (installPath, package);
				project.FirePackageInstalledEvent (e);
				monitor.OpenReadMeFile ();
			}

			Assert.IsFalse (fileService.IsOpenFileCalled);
		}

		[Test]
		public void Constructor_PackageDependencyIsInstalledWithReadmeTxt_ReadmeTxtIsNotOpenedUntilOpenReadMeFileMethodIsCalled ()
		{
			const string installPath = @"d:\projects\myproject\packages\Test.1.2.0";
			string expectedFileOpened = Path.Combine (installPath, "ReadMe.TXT");

			OpenPackageReadMeMonitor monitor = CreateMonitor ("Test");
			fileService.ExistingFileNames.Add (expectedFileOpened);
			FakePackage package = CreatePackageWithFile ("Test", "ReadMe.TXT");
			PackageOperationEventArgs e = CreatePackageInstallEventWithFile (installPath, package);
			project.FirePackageInstalledEvent (e);

			Assert.IsFalse (fileService.IsOpenFileCalled);
		}
	}
}