//
// PackageManagementSelectedProjectsTests.cs
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
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementSelectedProjectsTests
	{
		PackageManagementSelectedProjects selectedProjects;
		FakePackageManagementSolution fakeSolution;

		void CreateSelectedProjects ()
		{
			selectedProjects = new PackageManagementSelectedProjects (fakeSolution);
		}

		void CreateFakeSolution ()
		{
			fakeSolution = new FakePackageManagementSolution ();
		}

		List<FakeDotNetProject> AddSolutionWithOneProjectToProjectService ()
		{
			FakeDotNetProject project = ProjectHelper.CreateTestProject ("Test1");
			fakeSolution.FakeDotNetProjects.Add (project);

			return fakeSolution.FakeDotNetProjects;
		}

		List<FakeDotNetProject> AddSolutionWithTwoProjectsToProjectService ()
		{
			ISolution solution = ProjectHelper.CreateSolution ();
			FakeDotNetProject project1 = ProjectHelper.CreateTestProject (solution, "Test1");
			FakeDotNetProject project2 = ProjectHelper.CreateTestProject ("Test2");

			fakeSolution.FakeDotNetProjects.Add (project1);
			fakeSolution.FakeDotNetProjects.Add (project2);

			return fakeSolution.FakeDotNetProjects;
		}

		void NoProjectsSelected ()
		{
			fakeSolution.NoProjectsSelected ();
		}

		[Test]
		public void GetProjects_SolutionHasTwoProjectsAndOneProjectSelectedInProjectsBrowser_ReturnsProjectSelectedInProjects ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			FakeDotNetProject project = projectsAddedToSolution [1];
			project.Name = "MyProject";
			fakeSolution.FakeActiveDotNetProject = project;

			fakeSolution.AddFakeProjectToReturnFromGetProject ("MyProject");
			CreateSelectedProjects ();

			var fakePackage = new FakePackage ();
			var projects = new List<IPackageManagementSelectedProject> ();
			projects.AddRange (selectedProjects.GetProjects (fakePackage));

			var expectedProject = new PackageManagementSelectedProject ("MyProject", selected: false, enabled: true);
			var expectedProjects = new List<IPackageManagementSelectedProject> ();
			expectedProjects.Add (expectedProject);

			SelectedProjectCollectionAssert.AreEqual (expectedProjects, projects);
		}

		[Test]
		public void GetProjects_SolutionHasTwoProjectsAndOneProjectSelectedInitiallyAndGetProjectsCalledAgainAfterNoProjectsAreSelected_ReturnsProjectSelectedInProjects ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			FakeDotNetProject project = projectsAddedToSolution [1];
			project.Name = "MyProject";
			fakeSolution.FakeActiveDotNetProject = project;

			fakeSolution.AddFakeProjectToReturnFromGetProject ("MyProject");
			CreateSelectedProjects ();

			var fakePackage = new FakePackage ();
			var projects = new List<IPackageManagementSelectedProject> ();
			projects.AddRange (selectedProjects.GetProjects (fakePackage));

			projects.Clear ();

			NoProjectsSelected ();
			projects.AddRange (selectedProjects.GetProjects (fakePackage));

			var expectedProject = new PackageManagementSelectedProject ("MyProject", selected: false, enabled: true);
			var expectedProjects = new List<IPackageManagementSelectedProject> ();
			expectedProjects.Add (expectedProject);

			SelectedProjectCollectionAssert.AreEqual (expectedProjects, projects);
		}

		[Test]
		public void HasMultipleProjects_SolutionHasTwoProjectsAndOneProjectSelectedInProjectsBrowser_ReturnsFalse ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			FakeDotNetProject expectedProject = projectsAddedToSolution [1];
			fakeSolution.FakeActiveDotNetProject = expectedProject;
			CreateSelectedProjects ();

			bool hasMultipleProjects = selectedProjects.HasMultipleProjects ();

			Assert.IsFalse (hasMultipleProjects);
		}

		[Test]
		public void GetProjects_SolutionHasTwoProjectsAndNoProjectSelectedInProjectsBrowser_ReturnsAllProjectsInSolutionForPackage ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			projectsAddedToSolution [0].Name = "Project A";
			projectsAddedToSolution [1].Name = "Project B";
			fakeSolution.FakeActiveProject = null;

			fakeSolution.AddFakeProjectToReturnFromGetProject ("Project A");
			fakeSolution.AddFakeProjectToReturnFromGetProject ("Project B");
			CreateSelectedProjects ();

			var fakePackage = new FakePackage ();
			var projects = new List<IPackageManagementSelectedProject> ();
			projects.AddRange (selectedProjects.GetProjects (fakePackage));

			var expectedProjects = new List<IPackageManagementSelectedProject> ();
			expectedProjects.Add (new PackageManagementSelectedProject ("Project A", selected: false, enabled: true));
			expectedProjects.Add (new PackageManagementSelectedProject ("Project B", selected: false, enabled: true));

			SelectedProjectCollectionAssert.AreEqual (expectedProjects, projects);
		}

		[Test]
		public void HasMultipleProjects_SolutionHasTwoProjectsAndNoProjectSelectedInProjectsBrowser_ReturnsTrue ()
		{
			CreateFakeSolution ();
			AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveProject = null;
			CreateSelectedProjects ();

			bool hasMultipleProjects = selectedProjects.HasMultipleProjects ();

			Assert.IsTrue (hasMultipleProjects);
		}

		[Test]
		public void HasMultipleProjects_SolutionHasOneProjectAndNoProjectSelectedInProjectsBrowser_ReturnsFalse ()
		{
			CreateFakeSolution ();
			AddSolutionWithOneProjectToProjectService ();
			fakeSolution.FakeActiveProject = null;
			CreateSelectedProjects ();

			bool hasMultipleProjects = selectedProjects.HasMultipleProjects ();

			Assert.IsFalse (hasMultipleProjects);
		}

		[Test]
		public void SelectionName_SolutionHasOneProject_ReturnsProjectNameWithoutFileExtension ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithOneProjectToProjectService ();
			projectsAddedToSolution [0].Name = "MyProject";
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];
			CreateSelectedProjects ();

			string name = selectedProjects.SelectionName;

			Assert.AreEqual ("MyProject", name);
		}

		[Test]
		public void SelectionName_SolutionHasTwoProjectsAndNoProjectSelected_ReturnsSolutionFileNameWithoutFullPath ()
		{
			CreateFakeSolution ();
			AddSolutionWithTwoProjectsToProjectService ();
			NoProjectsSelected ();
			fakeSolution.FileName = @"d:\projects\MyProject\MySolution.sln".ToNativePath ();
			CreateSelectedProjects ();

			string name = selectedProjects.SelectionName;

			Assert.AreEqual ("MySolution.sln", name);
		}

		[Test]
		public void IsPackageInstalled_PackageInstalledInSolutionWithTwoProjectsAndNoProjectSelected_ReturnsTrue ()
		{
			CreateFakeSolution ();
			AddSolutionWithTwoProjectsToProjectService ();
			NoProjectsSelected ();

			var package = new FakePackage ("Test");
			fakeSolution.FakeInstalledPackages.Add (package);
			CreateSelectedProjects ();

			bool installed = selectedProjects.IsPackageInstalled (package);

			Assert.IsTrue (installed);
		}

		[Test]
		public void IsPackageInstalled_PackageIsInstalledInSolutionWithTwoProjectsAndNoProjectSelected_ReturnsFalse ()
		{
			CreateFakeSolution ();
			AddSolutionWithTwoProjectsToProjectService ();
			NoProjectsSelected ();
			CreateSelectedProjects ();

			var package = new FakePackage ("Test");
			bool installed = selectedProjects.IsPackageInstalled (package);

			Assert.IsFalse (installed);
		}

		[Test]
		public void IsPackageInstalled_PackageIsInstalledInProjectAndProjectSelected_ReturnsTrue ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];

			var package = new FakePackage ("Test");
			fakeSolution.FakeProjectToReturnFromGetProject.FakePackages.Add (package);
			CreateSelectedProjects ();

			bool installed = selectedProjects.IsPackageInstalled (package);

			Assert.IsTrue (installed);
		}

		[Test]
		public void IsPackageInstalled_PackageIsNotInstalledInProjectAndProjectSelected_ReturnsFalse ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];
			CreateSelectedProjects ();

			var package = new FakePackage ("Test");
			bool installed = selectedProjects.IsPackageInstalled (package);

			Assert.IsFalse (installed);
		}

		[Test]
		public void IsPackageInstalled_PackagePackageIsNotInstalledInProjectAndProjectSelected_ProjectCreatedUsingPackageRepository ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];
			CreateSelectedProjects ();

			var package = new FakePackage ("Test");
			selectedProjects.IsPackageInstalled (package);

			IPackageRepository repository = fakeSolution.RepositoryPassedToGetProject;
			IPackageRepository expectedRepository = package.FakePackageRepository;

			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void IsPackageInstalledInSolution_PackageInstalledInSolutionWithTwoProjectsAndOneProjectSelected_ReturnsTrue ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];

			var package = new FakePackage ("Test");
			fakeSolution.FakeInstalledPackages.Add (package);
			CreateSelectedProjects ();

			bool installed = selectedProjects.Solution.IsPackageInstalled (package);

			Assert.IsTrue (installed);
		}

		[Test]
		public void IsPackageInstalledInSolution_PackageNotInstalledInSolutionWithTwoProjectsAndOneProjectSelected_ReturnsFalse ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];
			CreateSelectedProjects ();

			var package = new FakePackage ("Test");

			bool installed = selectedProjects.Solution.IsPackageInstalled (package);

			Assert.IsFalse (installed);
		}

		[Test]
		public void GetPackagesInstalledInSolution_PackageInstalledInSolutionAndProjectNotSelected_ReturnsPackageInstalledInSolution ()
		{
			CreateFakeSolution ();
			AddSolutionWithTwoProjectsToProjectService ();
			NoProjectsSelected ();

			var package = new FakePackage ("Test");
			fakeSolution.FakeInstalledPackages.Add (package);
			CreateSelectedProjects ();

			IQueryable<IPackage> packages = selectedProjects.Solution.GetPackages ();

			var expectedPackages = new FakePackage[] {
				package
			};

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetSingleProjectSelected_ProjectSelected_ReturnsProject ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];
			CreateSelectedProjects ();

			var repository = new FakePackageRepository ();
			IPackageManagementProject project = selectedProjects.GetSingleProjectSelected (repository);

			FakePackageManagementProject expectedProject = fakeSolution.FakeProjectToReturnFromGetProject;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetSingleProjectSelected_ProjectSelectedAndRepositoryPassed_ReturnsProjectCreatedWithRepository ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];
			CreateSelectedProjects ();

			var repository = new FakePackageRepository ();
			selectedProjects.GetSingleProjectSelected (repository);

			Assert.AreEqual (repository, fakeSolution.RepositoryPassedToGetProject);
		}

		[Test]
		public void GetSingleProjectSelected_NoProjectSelectedAndRepositoryPassed_ReturnsProjectCreatedWithRepository ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];
			CreateSelectedProjects ();

			var repository = new FakePackageRepository ();
			selectedProjects.GetSingleProjectSelected (repository);

			Assert.AreEqual (repository, fakeSolution.RepositoryPassedToGetProject);
		}

		[Test]
		public void HasSingleProjectSelected_SolutionHasTwoProjectsAndOneProjectSelectedInProjectsBrowser_ReturnsTrue ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			FakeDotNetProject expectedProject = projectsAddedToSolution [1];
			fakeSolution.FakeActiveDotNetProject = expectedProject;
			CreateSelectedProjects ();

			bool singleProjectSelected = selectedProjects.HasSingleProjectSelected ();

			Assert.IsTrue (singleProjectSelected);
		}

		[Test]
		public void HasSingleProjectSelected_SolutionHasTwoProjectsAndNoProjectsSelectedInProjectsBrowser_ReturnsFalse ()
		{
			CreateFakeSolution ();
			AddSolutionWithTwoProjectsToProjectService ();
			NoProjectsSelected ();
			CreateSelectedProjects ();

			bool singleProjectSelected = selectedProjects.HasSingleProjectSelected ();

			Assert.IsFalse (singleProjectSelected);
		}

		[Test]
		public void HasSingleProjectSelected_NoProjectsInitiallySelectedAndProjectSelectedAfterInitialCall_IsUnchangedAndReturnsFalse ()
		{
			CreateFakeSolution ();
			AddSolutionWithTwoProjectsToProjectService ();
			NoProjectsSelected ();
			CreateSelectedProjects ();

			bool singleProjectSelected = selectedProjects.HasSingleProjectSelected ();
			fakeSolution.FakeActiveDotNetProject = fakeSolution.FakeDotNetProjects [0];
			singleProjectSelected = selectedProjects.HasSingleProjectSelected ();

			Assert.IsFalse (singleProjectSelected);
		}

		[Test]
		public void GetInstalledPackages_PackageInstalledInSolutionAndProjectNotSelected_ReturnsPackageInstalledInSolution ()
		{
			CreateFakeSolution ();
			AddSolutionWithTwoProjectsToProjectService ();
			NoProjectsSelected ();

			var package = new FakePackage ("Test");
			fakeSolution.FakeInstalledPackages.Add (package);
			CreateSelectedProjects ();

			var repository = new FakePackageRepository ();
			IQueryable<IPackage> packages = selectedProjects.GetInstalledPackages (repository);

			var expectedPackages = new FakePackage[] {
				package
			};

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetInstalledPackages_PackageInstalledInProjectAndProjectIsSelected_ReturnsPackageInstalledInProject ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];
			fakeSolution.FakeActiveDotNetProject.Name = "MyProject";

			var package = new FakePackage ("Test");
			var project = new FakePackageManagementProject ("MyProject");
			project.FakePackages.Add (package);
			fakeSolution.FakeProjectsToReturnFromGetProject.Add ("MyProject", project);
			CreateSelectedProjects ();

			var repository = new FakePackageRepository ();
			IQueryable<IPackage> packages = selectedProjects.GetInstalledPackages (repository);

			var expectedPackages = new FakePackage[] {
				package
			};

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetInstalledPackages_PackageInstalledInProjectAndProjectIsSelected_CreatesProjectUsingRepository ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			fakeSolution.FakeActiveDotNetProject = projectsAddedToSolution [0];
			CreateSelectedProjects ();

			var expectedRepository = new FakePackageRepository ();
			selectedProjects.GetInstalledPackages (expectedRepository);

			IPackageRepository repository = fakeSolution.RepositoryPassedToGetProject;

			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void GetSingleProjectSelected_NoProjectSelected_ReturnsNull ()
		{
			CreateFakeSolution ();
			AddSolutionWithTwoProjectsToProjectService ();
			NoProjectsSelected ();
			CreateSelectedProjects ();

			var repository = new FakePackageRepository ();
			IPackageManagementProject project = selectedProjects.GetSingleProjectSelected (repository);

			Assert.IsNull (project);
		}

		[Test]
		public void GetProjects_SolutionHasTwoProjectsAndOneProjectSelectedInitiallyAndActiveProjectChangedInSolutionAfterInstanceCreated_ReturnsProjectSelectedInProjects ()
		{
			CreateFakeSolution ();
			List<FakeDotNetProject> projectsAddedToSolution = AddSolutionWithTwoProjectsToProjectService ();
			FakeDotNetProject project = projectsAddedToSolution [1];
			project.Name = "MyProject";
			fakeSolution.FakeActiveDotNetProject = project;
			fakeSolution.AddFakeProjectToReturnFromGetProject ("MyProject");
			CreateSelectedProjects ();

			NoProjectsSelected ();

			var fakePackage = new FakePackage ();
			List<IPackageManagementSelectedProject> projects = selectedProjects.GetProjects (fakePackage).ToList ();

			var expectedProject = new PackageManagementSelectedProject ("MyProject", selected: false, enabled: true);
			var expectedProjects = new List<IPackageManagementSelectedProject> ();
			expectedProjects.Add (expectedProject);

			SelectedProjectCollectionAssert.AreEqual (expectedProjects, projects);
		}
	}
}


