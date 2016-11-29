//
// PackageCompatibilityResultTests.cs
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
using System.Runtime.Versioning;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageCompatibilityTests
	{
		FakePackage package;
		PackageCompatibility packageCompatibility;
		FakeDotNetProject project;

		NetPortableProfileTable profileTable;

		[TestFixtureSetUp]
		public void SetUp ()
		{
			string appDataFolder = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify);
			string dummyPath = Path.Combine (appDataFolder, "MonoDevelopPackageManagementTests");
			Environment.SetEnvironmentVariable ("NuGetPortableReferenceAssemblyPath", dummyPath);
			profileTable = NetPortableProfileTable.Instance;
		}

		void CreateProject (string frameworkVersion)
		{
			project = new FakeDotNetProject ();
			project.TargetFrameworkMoniker = new TargetFrameworkMoniker (frameworkVersion);
		}

		void CreatePackageCompatibility (string packageFrameworkName)
		{
			package = FakePackage.CreatePackageWithVersion ("MyPackage", "1.2.3.4");
			FrameworkName packageFramework = VersionUtility.ParseFrameworkName (packageFrameworkName);
			packageCompatibility = new PackageCompatibility (project, package, packageFramework);
		}

		void AssertReportsAreEqual (string expected, string actual)
		{
			expected = expected.Replace ("\r\n", "\n");
			actual = actual.Replace ("\r\n", "n");
		}

		[Test]
		public void CheckCompatibility_PackageHasNoFiles_PackageShouldNotNeedReinstall ()
		{
			CreateProject ("v4.5");
			CreatePackageCompatibility ("net40");

			packageCompatibility.CheckCompatibility ();

			Assert.IsFalse (packageCompatibility.ShouldReinstallPackage);
			Assert.IsTrue (packageCompatibility.IsCompatibleWithNewProjectTargetFramework);
		}

		[Test]
		public void CheckCompatibility_PackageHasFilesForNewerFrameworkVersionComparedToProject_PackageShouldNeedReinstall ()
		{
			CreateProject ("v4.0");
			CreatePackageCompatibility ("net45");
			package.AddFile (@"lib\net45\MyPackage.dll");

			packageCompatibility.CheckCompatibility ();

			Assert.IsTrue (packageCompatibility.ShouldReinstallPackage);
			Assert.IsFalse (packageCompatibility.IsCompatibleWithNewProjectTargetFramework);
			Assert.IsTrue (packageCompatibility.IsCompatibleWithOriginalProjectTargetFramework);
		}

		[Test]
		public void CheckCompatibility_PackageAlreadyInstalledForProjectTargetFramework_PackageShouldNotNeedReinstall ()
		{
			CreateProject ("v4.5");
			CreatePackageCompatibility ("net45");
			package.AddFile (@"lib\net45\MyPackage.dll");

			packageCompatibility.CheckCompatibility ();

			Assert.IsFalse (packageCompatibility.ShouldReinstallPackage);
			Assert.IsTrue (packageCompatibility.IsCompatibleWithNewProjectTargetFramework);
		}

		[Test]
		public void CheckCompatibility_PackageHasNoFrameworkSpecificFiles_PackageShouldNotNeedReinstall ()
		{
			CreateProject ("v4.0");
			CreatePackageCompatibility ("net45");
			package.AddFile (@"lib\MyPackage.dll");

			packageCompatibility.CheckCompatibility ();

			Assert.IsFalse (packageCompatibility.ShouldReinstallPackage);
			Assert.IsTrue (packageCompatibility.IsCompatibleWithNewProjectTargetFramework);
			Assert.IsTrue (packageCompatibility.IsCompatibleWithOriginalProjectTargetFramework);
		}
	}
}

