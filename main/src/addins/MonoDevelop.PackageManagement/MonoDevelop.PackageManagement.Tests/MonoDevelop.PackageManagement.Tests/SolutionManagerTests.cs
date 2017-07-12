//
// SolutionManagerTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	class SolutionManagerTests
	{
		FakeSolutionManager solutionManager;

		void CreateSolutionManager ()
		{
			solutionManager = new FakeSolutionManager ();
		}

		FakeNuGetProject AddNuGetProject ()
		{
			var project = new FakeNuGetProject (new FakeDotNetProject ());
			solutionManager.NuGetProjects [project.Project] = project;
			return project;
		}

		[Test]
		public async Task GetInstalledVersions_OneProjectWithPackage_ReturnsOneVersion ()
		{
			CreateSolutionManager ();
			var project = AddNuGetProject ();
			project.AddPackageReference ("Test", "1.0");

			var versions = await solutionManager.GetInstalledVersions ("Test");

			Assert.AreEqual ("1.0", versions.First ().ToString ());
			Assert.AreEqual (1, versions.Count ());
		}

		[Test]
		public async Task GetInstalledVersions_ThreeProjectsWithDifferentPackageVersions_ReturnsThreeVersionsHighestFirst ()
		{
			CreateSolutionManager ();
			var project1 = AddNuGetProject ();
			project1.AddPackageReference ("Test", "1.0");
			var project2 = AddNuGetProject ();
			project2.AddPackageReference ("Test", "1.2");
			var project3 = AddNuGetProject ();
			project3.AddPackageReference ("Test", "1.1");

			var versions = await solutionManager.GetInstalledVersions ("Test");
			var versionList = versions.ToList ();

			Assert.AreEqual ("1.2", versionList[0].ToString ());
			Assert.AreEqual ("1.1", versionList[1].ToString ());
			Assert.AreEqual ("1.0", versionList[2].ToString ());
			Assert.AreEqual (3, versionList.Count);
		}

		[Test]
		public async Task GetInstalledVersions_OneProjectWithTwoPackages_ReturnsOneVersion ()
		{
			CreateSolutionManager ();
			var project = AddNuGetProject ();
			project.AddPackageReference ("Test", "1.0");
			project.AddPackageReference ("Another", "1.2");

			var versions = await solutionManager.GetInstalledVersions ("Test");

			Assert.AreEqual ("1.0", versions.First ().ToString ());
			Assert.AreEqual (1, versions.Count ());
		}

		[Test]
		public async Task GetInstalledVersions_TwoProjectsWithSamePackageVersions_ReturnsOneVersion ()
		{
			CreateSolutionManager ();
			var project1 = AddNuGetProject ();
			project1.AddPackageReference ("Test", "1.0");
			var project2 = AddNuGetProject ();
			project2.AddPackageReference ("Test", "1.0");

			var versions = await solutionManager.GetInstalledVersions ("Test");
			var versionList = versions.ToList ();

			Assert.AreEqual ("1.0", versionList[0].ToString ());
			Assert.AreEqual (1, versionList.Count);
		}

		[Test]
		public async Task GetInstalledVersions_TwoProjectsWithDifferentPackageVersionsAndPackageIdCaseDifferent_ReturnsTwoVersions ()
		{
			CreateSolutionManager ();
			var project1 = AddNuGetProject ();
			project1.AddPackageReference ("TEST", "1.0");
			var project2 = AddNuGetProject ();
			project2.AddPackageReference ("test", "1.1");

			var versions = await solutionManager.GetInstalledVersions ("Test");
			var versionList = versions.ToList ();

			Assert.AreEqual ("1.1", versionList[0].ToString ());
			Assert.AreEqual ("1.0", versionList[1].ToString ());
			Assert.AreEqual (2, versionList.Count);
		}

		[Test]
		public async Task GetProjectsWithInstalledPackage_OneProjectWithPackage_ReturnsOneProject ()
		{
			CreateSolutionManager ();
			var project = AddNuGetProject ();
			project.AddPackageReference ("Test", "1.0");

			var projects = await solutionManager.GetProjectsWithInstalledPackage ("Test", "1.0");

			Assert.AreEqual (project.Project, projects.First ());
			Assert.AreEqual (1, projects.Count ());
		}

		[Test]
		public async Task GetProjectsWithInstalledPackage_TwoProjectsWithPackage_ReturnsTwoProject ()
		{
			CreateSolutionManager ();
			var project1 = AddNuGetProject ();
			project1.AddPackageReference ("Test", "1.0");
			var project2 = AddNuGetProject ();
			project2.AddPackageReference ("Test", "1.0");

			var projects = await solutionManager.GetProjectsWithInstalledPackage ("Test", "1.0");

			Assert.That (projects, Contains.Item (project1.Project));
			Assert.That (projects, Contains.Item (project2.Project));
			Assert.AreEqual (2, projects.Count ());
		}

		[Test]
		public async Task GetProjectsWithInstalledPackage_TwoProjectsDifferentVersionsOneVersionMatch_ReturnsOneProject ()
		{
			CreateSolutionManager ();
			var project1 = AddNuGetProject ();
			project1.AddPackageReference ("Test", "1.0");
			var project2 = AddNuGetProject ();
			project2.AddPackageReference ("Test", "1.1");

			var projects = await solutionManager.GetProjectsWithInstalledPackage ("Test", "1.0");

			Assert.AreEqual (project1.Project, projects.First ());
			Assert.AreEqual (1, projects.Count ());
		}

		[Test]
		public async Task GetProjectsWithInstalledPackage_TwoProjectsWithPackageDifferentCase_ReturnsTwoProject ()
		{
			CreateSolutionManager ();
			var project1 = AddNuGetProject ();
			project1.AddPackageReference ("TEST", "1.0");
			var project2 = AddNuGetProject ();
			project2.AddPackageReference ("test", "1.0");

			var projects = await solutionManager.GetProjectsWithInstalledPackage ("Test", "1.0");

			Assert.That (projects, Contains.Item (project1.Project));
			Assert.That (projects, Contains.Item (project2.Project));
			Assert.AreEqual (2, projects.Count ());
		}
	}
}
