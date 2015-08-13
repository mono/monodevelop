//
// DotNetProjectExtensionsTests.cs
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

using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class DotNetProjectExtensionsTests
	{
		List<string> existingFiles;
		FakeDotNetProject project;

		[SetUp]
		public void Init ()
		{
			existingFiles = new List<string> ();
			DotNetProjectExtensions.FileExists = existingFiles.Contains;
		}

		void CreateProject (string fileName, string projectName)
		{
			project = new FakeDotNetProject (fileName.ToNativePath ()) {
				Name = projectName
			};
		}

		void AddExistingFile (string fileName)
		{
			existingFiles.Add (fileName.ToNativePath ());
		}

		[Test]
		public void GetPackagesConfigFilePath_ProjectPackagesConfigFileDoesNotExist_ReturnsDefaultPackagesConfigFile ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");

			string fileName = project.GetPackagesConfigFilePath ();

			Assert.AreEqual (@"d:\projects\packages.config".ToNativePath (), fileName);
		}

		[Test]
		public void GetPackagesConfigFilePath_ProjectPackagesConfigFileExists_ReturnsPackagesConfigFileNamedAfterProject ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");
			AddExistingFile (@"d:\projects\packages.MyProject.config");

			string fileName = project.GetPackagesConfigFilePath ();

			Assert.AreEqual (@"d:\projects\packages.MyProject.config".ToNativePath (), fileName);
		}

		[Test]
		public void GetPackagesConfigFilePath_ProjectNameHasSpaceProjectPackagesConfigFileExists_ReturnsPackagesConfigFileNamedAfterProject ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "My Project");
			AddExistingFile (@"d:\projects\packages.My_Project.config");

			string fileName = project.GetPackagesConfigFilePath ();

			Assert.AreEqual (@"d:\projects\packages.My_Project.config".ToNativePath (), fileName);
		}

		[Test]
		public void HasPackages_PackagesConfigFileDoesNotExist_ReturnsFalse ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");

			bool result = project.HasPackages ();

			Assert.IsFalse (result);
		}

		[Test]
		public void HasPackages_PackagesConfigFileExistsInProjectDirectory_ReturnsTrue ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");
			AddExistingFile (@"d:\projects\packages.config");

			bool result = project.HasPackages ();

			Assert.IsTrue (result);
		}

		[Test]
		public void HasPackages_ProjectPackagesConfigFileExistsInProjectDirectory_ReturnsTrue ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "MyProject");
			AddExistingFile (@"d:\projects\packages.MyProject.config");

			bool result = project.HasPackages ();

			Assert.IsTrue (result);
		}

		[Test]
		public void HasPackages_ProjectNameHasSpaceAndProjectPackagesConfigFileExistsInProjectDirectory_ReturnsTrue ()
		{
			CreateProject (@"d:\projects\MyProject.csproj", "My Project");
			AddExistingFile (@"d:\projects\packages.My_Project.config");

			bool result = project.HasPackages ();

			Assert.IsTrue (result);
		}
	}
}

