//
// PackageFilesTests.cs
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
using NUnit.Framework;
using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageFilesTests
	{
		PackageFiles packageFiles;

		void CreatePackageFiles (FakePackage package)
		{
			packageFiles = new PackageFiles (package);
		}

		void CreatePackageFilesWithOneFile (string fileName)
		{
			var package = new FakePackage ();
			package.AddFile (fileName);
			CreatePackageFiles (package);
		}

		void CreatePackageFilesWithTwoFiles (string fileName1, string fileName2)
		{
			var package = new FakePackage ();
			package.AddFile (fileName1);
			package.AddFile (fileName2);
			CreatePackageFiles (package);
		}

		[Test]
		public void HasAnyPackageScripts_HasOnePowerShellInitScript_ReturnsTrue ()
		{
			CreatePackageFilesWithOneFile (@"tools\init.ps1");

			bool hasScripts = packageFiles.HasAnyPackageScripts ();

			Assert.IsTrue (hasScripts);
		}

		[Test]
		public void HasAnyPackageScripts_HasOneCSharpFile_ReturnsFalse ()
		{
			CreatePackageFilesWithOneFile (@"src\test.cs");

			bool hasScripts = packageFiles.HasAnyPackageScripts ();

			Assert.IsFalse (hasScripts);
		}

		[Test]
		public void HasAnyPackageScripts_HasOnePowerShellInitScriptWithDifferentParentFolder_ReturnsTrue ()
		{
			CreatePackageFilesWithOneFile (@"parentfolder\init.ps1");

			bool hasScripts = packageFiles.HasAnyPackageScripts ();

			Assert.IsTrue (hasScripts);
		}

		[Test]
		public void HasAnyPackageScripts_HasOnePowerShellInitScriptInUpperCase_ReturnsTrue ()
		{
			CreatePackageFilesWithOneFile (@"tools\INIT.PS1");

			bool hasScripts = packageFiles.HasAnyPackageScripts ();

			Assert.IsTrue (hasScripts);
		}

		[Test]
		public void HasAnyPackageScripts_HasOnePowerShellInstallScript_ReturnsTrue ()
		{
			CreatePackageFilesWithOneFile (@"tools\install.ps1");

			bool hasScripts = packageFiles.HasAnyPackageScripts ();

			Assert.IsTrue (hasScripts);
		}

		[Test]
		public void HasAnyPackageScripts_HasOnePowerShellInstallScriptInUpperCase_ReturnsTrue ()
		{
			CreatePackageFilesWithOneFile (@"tools\INSTALL.PS1");

			bool hasScripts = packageFiles.HasAnyPackageScripts ();

			Assert.IsTrue (hasScripts);
		}

		[Test]
		public void HasAnyPackageScripts_HasOnePowerShellUninstallScript_ReturnsTrue ()
		{
			CreatePackageFilesWithOneFile (@"tools\uninstall.ps1");

			bool hasScripts = packageFiles.HasAnyPackageScripts ();

			Assert.IsTrue (hasScripts);
		}

		[Test]
		public void HasAnyPackageScripts_HasOnePowerShellUninstallScriptInUpperCase_ReturnsTrue ()
		{
			CreatePackageFilesWithOneFile (@"tools\UNINSTALL.PS1");

			bool hasScripts = packageFiles.HasAnyPackageScripts ();

			Assert.IsTrue (hasScripts);
		}

		[Test]
		public void HasAnyPackageScripts_SecondFileIsPowerShellInitScript_ReturnsTrue ()
		{
			CreatePackageFilesWithTwoFiles (@"src\test.cs", @"tools\init.ps1");

			bool hasScripts = packageFiles.HasAnyPackageScripts ();

			Assert.IsTrue (hasScripts);
		}

		[Test]
		public void HasUninstallPackageScript_HasOnePowerShellUninstallScript_ReturnsTrue ()
		{
			CreatePackageFilesWithOneFile (@"tools\uninstall.ps1");

			bool hasScript = packageFiles.HasUninstallPackageScript ();

			Assert.IsTrue (hasScript);
		}

		[Test]
		public void HasUninstallPackageScript_HasOneCSharpFile_ReturnsFalse ()
		{
			CreatePackageFilesWithOneFile (@"tools\test.cs");

			bool hasScript = packageFiles.HasUninstallPackageScript ();

			Assert.IsFalse (hasScript);
		}
	}
}

