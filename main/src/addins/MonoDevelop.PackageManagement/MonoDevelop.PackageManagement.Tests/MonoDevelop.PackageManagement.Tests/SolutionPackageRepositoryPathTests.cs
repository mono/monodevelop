//
// SolutionPackageRepositoryPathTests.cs
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
using MonoDevelop.PackageManagement;
using NUnit.Framework;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class SolutionPackageRepositoryPathTests
	{
		SolutionPackageRepositoryPath repositoryPath;
		FakeProject project;
		FakeSolution solution;
		FakeSettingsProvider settingsProvider;
		FakeSettings settings;

		void CreateSolutionPackageRepositoryPath ()
		{
			repositoryPath = new SolutionPackageRepositoryPath (project, settingsProvider);
		}

		void CreateSolutionPackageRepositoryPath (ISolution solution)
		{
			repositoryPath = new SolutionPackageRepositoryPath (solution, settingsProvider);
		}

		void CreateTestProject ()
		{
			project = new FakeProject ();
		}

		void CreateSolution (string fileName)
		{
			solution = new FakeSolution (fileName);
		}

		void CreateSettings ()
		{
			settingsProvider = new FakeSettingsProvider ();
			settings = settingsProvider.FakeSettings;
		}

		void SolutionNuGetConfigFileHasCustomPackagesPath (string fullPath)
		{
			settings.SetRepositoryPathSetting (fullPath.ToNativePath ());
		}

		[Test]
		public void PackageRepositoryPath_ProjectAndSolutionHaveDifferentFolders_IsConfiguredPackagesFolderInsideSolutionFolder ()
		{
			CreateSettings ();
			CreateTestProject ();
			CreateSolution (@"d:\projects\MyProject\MySolution.sln");
			solution.BaseDirectory = @"d:\projects\MyProject\".ToNativePath ();
			project.ParentSolution = solution;
			CreateSolutionPackageRepositoryPath ();

			string path = repositoryPath.PackageRepositoryPath;
			string expectedPath = @"d:\projects\MyProject\packages".ToNativePath ();

			Assert.AreEqual (expectedPath, path);
		}

		[Test]
		public void PackageRepositoryPath_PassSolutionToConstructor_IsConfiguredPackagesFolderInsideSolutionFolder ()
		{
			CreateSettings ();
			CreateSolution (@"d:\projects\MySolution\MySolution.sln");
			CreateSolutionPackageRepositoryPath (solution);

			string path = repositoryPath.PackageRepositoryPath;
			string expectedPath = @"d:\projects\MySolution\packages".ToNativePath ();

			Assert.AreEqual (expectedPath, path);
		}

		[Test]
		public void PackageRepositoryPath_SolutionHasNuGetFileThatOverridesDefaultPackagesRepositoryPath_OverriddenPathReturned ()
		{
			CreateSettings ();
			CreateSolution (@"d:\projects\MySolution\MySolution.sln");
			SolutionNuGetConfigFileHasCustomPackagesPath (@"d:\Team\MyPackages");
			CreateSolutionPackageRepositoryPath (solution);
			string expectedPath = @"d:\Team\MyPackages".ToNativePath ();

			string path = repositoryPath.PackageRepositoryPath;

			Assert.AreEqual (expectedPath, path);
		}

		[Test]
		public void PackageRepositoryPath_SolutionHasNuGetFileThatOverridesDefaultPackagesRepositoryPathAndPathContainsDotDots_OverriddenPathReturnedWithoutDotDots ()
		{
			CreateSettings ();
			CreateSolution (@"d:\projects\MySolution\MySolution.sln");
			SolutionNuGetConfigFileHasCustomPackagesPath (@"d:\projects\MySolution\..\..\Team\MyPackages");
			CreateSolutionPackageRepositoryPath (solution);
			string expectedPath = @"d:\Team\MyPackages".ToNativePath ();

			string path = repositoryPath.PackageRepositoryPath;

			Assert.AreEqual (expectedPath, path);
		}
	}
}
