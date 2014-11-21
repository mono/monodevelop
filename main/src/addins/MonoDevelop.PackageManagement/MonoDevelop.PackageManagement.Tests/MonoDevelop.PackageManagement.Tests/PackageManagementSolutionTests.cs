//
// PackageManagementSolutionTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using ICSharpCode.PackageManagement;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementSolutionTests
	{
		PackageManagementSolution solution;
		FakeRegisteredPackageRepositories fakeRegisteredPackageRepositories;
		OneRegisteredPackageSourceHelper packageSourcesHelper;
		FakePackageManagementProjectService fakeProjectService;
		FakePackageManagementProjectFactory fakeProjectFactory;
		FakeDotNetProject testProject;
		FakeSolutionPackageRepositoryFactory fakeSolutionPackageRepositoryFactory;
		FakeSolutionPackageRepository fakeSolutionPackageRepository;

		void CreatePackageSources ()
		{
			packageSourcesHelper = new OneRegisteredPackageSourceHelper ();
		}

		void CreateSolution ()
		{
			CreatePackageSources ();
			CreateSolution (packageSourcesHelper.Options);
		}

		void CreateSolution (PackageManagementOptions options)
		{
			testProject = ProjectHelper.CreateTestProject ();
			fakeRegisteredPackageRepositories = new FakeRegisteredPackageRepositories ();
			fakeProjectFactory = new FakePackageManagementProjectFactory ();
			fakeProjectService = new FakePackageManagementProjectService ();

			fakeProjectService.CurrentProject = testProject;
			fakeProjectService.OpenSolution = testProject.ParentSolution;

			fakeSolutionPackageRepositoryFactory = new FakeSolutionPackageRepositoryFactory ();
			fakeSolutionPackageRepository = fakeSolutionPackageRepositoryFactory.FakeSolutionPackageRepository;

			solution =
				new PackageManagementSolution (
				fakeRegisteredPackageRepositories,
				fakeProjectService,
				fakeProjectFactory,
				fakeSolutionPackageRepositoryFactory);
		}

		FakeDotNetProject AddProjectToOpenProjects (string projectName)
		{
			FakeDotNetProject project = ProjectHelper.CreateTestProject (projectName);
			fakeProjectService.OpenProjects.Add (project);
			return project;
		}

		FakePackage AddPackageInReverseDependencyOrderToSolution (string packageId)
		{
			var package = new FakePackage (packageId);
			fakeSolutionPackageRepository.FakePackagesByReverseDependencyOrder.Add (package);
			return package;
		}

		AggregateRepository CreateAggregateRepositoryWithOneRepository ()
		{
			var repositories = new FakePackageRepository [] {
				new FakePackageRepository ()
			};
			return new AggregateRepository (repositories);
		}

		[Test]
		public void GetActiveProject_ProjectIsSelected_CreatesProjectUsingCurrentProjectSelectedInSharpDevelop ()
		{
			CreateSolution ();

			solution.GetActiveProject ();

			IProject actualProject = fakeProjectFactory.FirstProjectPassedToCreateProject;
			Assert.AreEqual (testProject, actualProject);
		}

		[Test]
		public void GetActiveProject_ProjectIsSelected_CreatesProjectUsingAggregateRepository ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = new FakeDotNetProject ();
			AggregateRepository expectedRepository = CreateAggregateRepositoryWithOneRepository ();
			fakeRegisteredPackageRepositories.CreateAggregateRepositoryAction = () => expectedRepository;

			solution.GetActiveProject ();

			IPackageRepository repository = fakeProjectFactory.FirstRepositoryPassedToCreateProject;
			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void GetActiveProject_ProjectIsSelected_ReturnsProjectCreatedByFactory ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = new FakeDotNetProject ();

			IPackageManagementProject activeProject = solution.GetActiveProject ();
			IPackageManagementProject expectedProject = fakeProjectFactory.FirstFakeProjectCreated;

			Assert.AreEqual (expectedProject, activeProject);
		}

		[Test]
		public void GetActiveProject_RepositoryPassed_CreatesProjectUsingFallbackRepositoryWithAggregateAsFallback ()
		{
			CreateSolution ();
			var expectedRepository = new FakePackageRepository ();

			solution.GetActiveProject (expectedRepository);

			IPackageRepository repository = fakeProjectFactory.FirstRepositoryPassedToCreateProject;
			var fallbackRepository = repository as FallbackRepository;
			Assert.IsNotNull (fallbackRepository);
			Assert.AreEqual (expectedRepository, fallbackRepository.SourceRepository);
			Assert.AreEqual (fakeRegisteredPackageRepositories.FakeAggregateRepository, fallbackRepository.DependencyResolver);
		}

		[Test]
		public void GetActiveProject_AggregateRepositoryPassed_CreatesProjectUsingAggregateRepository ()
		{
			CreateSolution ();
			AggregateRepository expectedRepository = CreateAggregateRepositoryWithOneRepository ();

			solution.GetActiveProject (expectedRepository);

			IPackageRepository repository = fakeProjectFactory.FirstRepositoryPassedToCreateProject;
			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void GetActiveProject_RepositoryPassed_CreatesProjectUsingCurrentActiveProject ()
		{
			CreateSolution ();
			var expectedRepository = new FakePackageRepository ();
			FakeDotNetProject expectedProject = ProjectHelper.CreateTestProject ();
			fakeProjectService.CurrentProject = expectedProject;

			solution.GetActiveProject (expectedRepository);

			IDotNetProject project = fakeProjectFactory.FirstProjectPassedToCreateProject;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetActiveProject_RepositoryPassed_ReturnsProjectFromProjectFactory ()
		{
			CreateSolution ();
			var expectedRepository = new FakePackageRepository ();
			IPackageManagementProject project = solution.GetActiveProject (expectedRepository);

			FakePackageManagementProject expectedProject = fakeProjectFactory.FirstFakeProjectCreated;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProject_PackagesSourceAndProjectNamePassed_CreatesProjectUsingFoundProjectMatchingName ()
		{
			CreateSolution ();
			FakeDotNetProject expectedProject = AddProjectToOpenProjects ("Test");
			var source = new PackageSource ("http://sharpdevelop.net");

			solution.GetProject (source, "Test");

			IDotNetProject project = fakeProjectFactory.FirstProjectPassedToCreateProject;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProject_PackagesSourceAndProjectNameWithDifferentCasePassed_CreatesProjectUsingFoundProjectMatchingName ()
		{
			CreateSolution ();
			FakeDotNetProject expectedProject = AddProjectToOpenProjects ("Test");
			var source = new PackageSource ("http://sharpdevelop.net");

			solution.GetProject (source, "TEST");

			IDotNetProject project = fakeProjectFactory.FirstProjectPassedToCreateProject;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProject_PackagesSourceAndProjectPassed_ReturnsProjectFromProjectFactory ()
		{
			CreateSolution ();
			AddProjectToOpenProjects ("Test");
			var source = new PackageSource ("http://sharpdevelop.net");
			IPackageManagementProject project = solution.GetProject (source, "Test");

			FakePackageManagementProject expectedProject = fakeProjectFactory.FirstFakeProjectCreated;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProject_PackagesSourceAndProjectPassed_PackageSourceUsedToCreateRepository ()
		{
			CreateSolution ();
			AddProjectToOpenProjects ("Test");
			var expectedSource = new PackageSource ("http://sharpdevelop.net");
			solution.GetProject (expectedSource, "Test");

			PackageSource actualSource = fakeRegisteredPackageRepositories.PackageSourcePassedToCreateRepository;

			Assert.AreEqual (expectedSource, actualSource);
		}

		[Test]
		public void GetProject_PackagesRepositoryAndProjectNamePassed_CreatesProjectUsingFoundProjectMatchingName ()
		{
			CreateSolution ();
			FakeDotNetProject expectedProject = AddProjectToOpenProjects ("Test");
			var repository = new FakePackageRepository ();

			solution.GetProject (repository, "Test");

			IDotNetProject project = fakeProjectFactory.FirstProjectPassedToCreateProject;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProject_PackagesRepositoryAndProjectPassed_CreatesProjectUsingProjectPassed ()
		{
			CreateSolution ();
			FakeDotNetProject expectedProject = AddProjectToOpenProjects ("Test");
			var repository = new FakePackageRepository ();

			solution.GetProject (repository, expectedProject);

			IDotNetProject project = fakeProjectFactory.FirstProjectPassedToCreateProject;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProject_PackagesRepositoryAndProjectPassed_ReturnsProjectCreatedFromProjectFactory ()
		{
			CreateSolution ();
			FakeDotNetProject msbuildProject = AddProjectToOpenProjects ("Test");
			var repository = new FakePackageRepository ();

			IPackageManagementProject project = solution.GetProject (repository, msbuildProject);

			FakePackageManagementProject expectedProject = fakeProjectFactory.FirstFakeProjectCreated;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProject_PackagesRepositoryAndProjectPassed_CreatesProjectUsingRepository ()
		{
			CreateSolution ();
			FakeDotNetProject expectedProject = AddProjectToOpenProjects ("Test");
			AggregateRepository expectedRepository = CreateAggregateRepositoryWithOneRepository ();

			solution.GetProject (expectedRepository, expectedProject);

			IPackageRepository repository = fakeProjectFactory.FirstRepositoryPassedToCreateProject;

			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void GetProject_RepositoryAndProjectNameWithDifferentCasePassed_CreatesProjectUsingFoundProjectMatchingName ()
		{
			CreateSolution ();
			FakeDotNetProject expectedProject = AddProjectToOpenProjects ("Test");
			var repository = new FakePackageRepository ();

			solution.GetProject (repository, "TEST");

			IDotNetProject project = fakeProjectFactory.FirstProjectPassedToCreateProject;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProject_RepositoryAndProjectNamePassed_ReturnsProject ()
		{
			CreateSolution ();
			AddProjectToOpenProjects ("Test");
			var repository = new FakePackageRepository ();

			IPackageManagementProject project = solution.GetProject (repository, "Test");

			FakePackageManagementProject expectedProject = fakeProjectFactory.FirstFakeProjectCreated;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProject_RepositoryAndProjectNamePassed_RepositoryUsedToCreateProject ()
		{
			CreateSolution ();
			AddProjectToOpenProjects ("Test");
			AggregateRepository expectedRepository = CreateAggregateRepositoryWithOneRepository ();

			solution.GetProject (expectedRepository, "Test");

			IPackageRepository actualRepository = fakeProjectFactory.FirstRepositoryPassedToCreateProject;

			Assert.AreEqual (expectedRepository, actualRepository);
		}

		[Test]
		public void GetMSBuildProjects_TwoProjectsInOpenSolution_ReturnsTwoProjects ()
		{
			CreateSolution ();
			AddProjectToOpenProjects ("A");
			AddProjectToOpenProjects ("B");

			IEnumerable<IDotNetProject> projects = solution.GetDotNetProjects ();
			IEnumerable<IDotNetProject> expectedProjects = fakeProjectService.OpenProjects;

			CollectionAssert.AreEqual (expectedProjects, projects);
		}

		[Test]
		public void IsOpen_NoSolutionOpen_ReturnsFalse ()
		{
			CreateSolution ();
			fakeProjectService.OpenSolution = null;

			bool open = solution.IsOpen;

			Assert.IsFalse (open);
		}

		[Test]
		public void IsOpen_SolutionIsOpen_ReturnsTrue ()
		{
			CreateSolution ();
			fakeProjectService.OpenSolution = new FakeSolution ();

			bool open = solution.IsOpen;

			Assert.IsTrue (open);
		}

		[Test]
		public void GetActiveMSBuildProject_CurrentProjectIsSetInProjectService_ReturnsProjectCurrentlySelected ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = testProject;

			IProject activeProject = solution.GetActiveDotNetProject ();

			Assert.AreEqual (testProject, activeProject);
		}

		[Test]
		public void HasMultipleProjects_OneProjectInSolution_ReturnsFalse ()
		{
			CreateSolution ();
			FakeDotNetProject project = ProjectHelper.CreateTestProject ();
			fakeProjectService.OpenProjects.Add (project);

			bool hasMultipleProjects = solution.HasMultipleProjects ();

			Assert.IsFalse (hasMultipleProjects);
		}

		[Test]
		public void HasMultipleProjects_TwoProjectsInSolution_ReturnsTrue ()
		{
			CreateSolution ();
			FakeDotNetProject project1 = ProjectHelper.CreateTestProject ();
			fakeProjectService.OpenProjects.Add (project1);
			FakeDotNetProject project2 = ProjectHelper.CreateTestProject ();
			fakeProjectService.OpenProjects.Add (project2);

			bool hasMultipleProjects = solution.HasMultipleProjects ();

			Assert.IsTrue (hasMultipleProjects);
		}

		[Test]
		public void FileName_SolutionHasFileName_ReturnsSolutionFileName ()
		{
			CreateSolution ();
			string expectedFileName = @"d:\projects\myproject\Project.sln".ToNativePath ();
			var solution = new FakeSolution (expectedFileName);
			fakeProjectService.OpenSolution = solution;

			string fileName = this.solution.FileName;

			Assert.AreEqual (expectedFileName, fileName);
		}

		[Test]
		public void IsInstalled_PackageIsInstalledInSolutionLocalRepository_ReturnsTrue ()
		{
			CreateSolution ();
			FakePackage package = FakePackage.CreatePackageWithVersion ("Test", "1.3.4.5");
			fakeSolutionPackageRepository.FakeSharedRepository.FakePackages.Add (package);

			bool installed = solution.IsPackageInstalled (package);

			Assert.IsTrue (installed);
		}

		[Test]
		public void IsInstalled_PackageIsNotInstalledInSolutionLocalRepository_ReturnsFalse ()
		{
			CreateSolution ();
			FakePackage package = FakePackage.CreatePackageWithVersion ("Test", "1.3.4.5");

			bool installed = solution.IsPackageInstalled (package);

			Assert.IsFalse (installed);
		}

		[Test]
		public void IsInstalled_PackageIsNotInstalledInSolutionLocalRepository_ActivSolutionUsedToCreateSolutionPackageRepository ()
		{
			CreateSolution ();
			FakePackage package = FakePackage.CreatePackageWithVersion ("Test", "1.3.4.5");

			solution.IsPackageInstalled (package);

			ISolution expectedSolution = fakeProjectService.OpenSolution;
			ISolution solutionUsedToCreateSolutionPackageRepository = 
				fakeSolutionPackageRepositoryFactory.SolutionPassedToCreateSolutionPackageRepository;

			Assert.AreEqual (expectedSolution, solutionUsedToCreateSolutionPackageRepository);
		}

		[Test]
		public void GetActiveProject_SolutionOpenButNoProjectSelected_ReturnsNull ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = null;

			IPackageManagementProject activeProject = solution.GetActiveProject ();

			Assert.IsNull (activeProject);
		}

		[Test]
		public void GetActiveProject_RepositoryPassedWhenSolutionOpenButNoProjectSelected_ReturnsNull ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = null;

			var repository = new FakePackageRepository ();
			IPackageManagementProject activeProject = solution.GetActiveProject (repository);

			Assert.IsNull (activeProject);
		}

		[Test]
		public void GetPackages_OnePackageInSolutionRepository_ReturnsOnePackage ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = null;
			FakePackage package = FakePackage.CreatePackageWithVersion ("Test", "1.3.4.5");
			fakeSolutionPackageRepository.FakeSharedRepository.FakePackages.Add (package);
			AddProjectToOpenProjects ("Test");
			var project = new FakePackageManagementProject ();
			fakeProjectFactory.CreatePackageManagementProject = (repository, msbuildProject) => {
				return project;
			};
			project.FakePackages.Add (package);

			IQueryable<IPackage> packages = solution.GetPackages ();

			var expectedPackages = new FakePackage[] {
				package
			};
			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetPackagesInReverseDependencyOrder_TwoPackages_ReturnsPackagesFromSolutionLocalRepositoryInCorrectOrder ()
		{
			CreateSolution ();
			FakePackage packageA = AddPackageInReverseDependencyOrderToSolution ("A");
			FakePackage packageB = AddPackageInReverseDependencyOrderToSolution ("A");

			packageB.DependenciesList.Add (new PackageDependency ("A"));

			var expectedPackages = new FakePackage[] {
				packageB,
				packageA
			};

			IEnumerable<IPackage> packages = solution.GetPackagesInReverseDependencyOrder ();

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetProjects_SolutionHasOneProject_ReturnsOneProject ()
		{
			CreateSolution ();
			AddProjectToOpenProjects ("MyProject");
			var repository = new FakePackageRepository ();
			List<IPackageManagementProject> projects = solution.GetProjects (repository).ToList ();

			Assert.AreEqual (1, projects.Count);
		}

		[Test]
		public void GetProjects_SolutionHasOneProject_RepositoryUsedToCreateProject ()
		{
			CreateSolution ();
			AddProjectToOpenProjects ("MyProject");
			AggregateRepository expectedRepository = CreateAggregateRepositoryWithOneRepository ();
			solution.GetProjects (expectedRepository).ToList ();

			IPackageRepository repository = fakeProjectFactory.FirstRepositoryPassedToCreateProject;

			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void GetProjects_SolutionHasOneProject_FallbackRepositoryUsedToCreateProject ()
		{
			CreateSolution ();
			AddProjectToOpenProjects ("MyProject");
			var expectedRepository = new FakePackageRepository ();

			solution.GetProjects (expectedRepository).ToList ();

			IPackageRepository repository = fakeProjectFactory.FirstRepositoryPassedToCreateProject;
			var fallbackRepository = repository as FallbackRepository;
			Assert.AreEqual (expectedRepository, fallbackRepository.SourceRepository);
			Assert.AreEqual (fakeRegisteredPackageRepositories.FakeAggregateRepository, fallbackRepository.DependencyResolver);
		}

		[Test]
		public void GetProjects_SolutionHasOneProject_MSBuildProjectUsedToCreateProject ()
		{
			CreateSolution ();
			FakeDotNetProject expectedProject = AddProjectToOpenProjects ("MyProject");
			var repository = new FakePackageRepository ();
			solution.GetProjects (repository).ToList ();

			IDotNetProject project = fakeProjectFactory.FirstProjectPassedToCreateProject;

			Assert.AreEqual (expectedProject, project);
		}

		[Test]
		public void GetProjects_SolutionHasNoProjects_ReturnsNoProjects ()
		{
			CreateSolution ();
			var repository = new FakePackageRepository ();
			List<IPackageManagementProject> projects = solution.GetProjects (repository).ToList ();

			Assert.AreEqual (0, projects.Count);
		}

		[Test]
		public void GetProjects_SolutionHasTwoProjects_ReturnsTwoProjects ()
		{
			CreateSolution ();
			AddProjectToOpenProjects ("One");
			AddProjectToOpenProjects ("Two");
			var repository = new FakePackageRepository ();
			List<IPackageManagementProject> projects = solution.GetProjects (repository).ToList ();

			Assert.AreEqual (2, projects.Count);
		}

		[Test]
		public void GetInstallPath_OnePackageInSolutionRepository_ReturnsPackageInstallPath ()
		{
			CreateSolution ();
			FakePackage package = FakePackage.CreatePackageWithVersion ("Test", "1.3.4.5");
			string expectedInstallPath = @"d:\projects\MyProject\packages\TestPackage".ToNativePath ();
			fakeSolutionPackageRepository.InstallPathToReturn = expectedInstallPath;

			string installPath = solution.GetInstallPath (package);

			Assert.AreEqual (expectedInstallPath, installPath);
			Assert.AreEqual (package, fakeSolutionPackageRepository.PackagePassedToGetInstallPath);
		}

		[Test]
		public void GetPackages_OnePackageInstalledIntoOneProjectButTwoPackagesInSolutionRepository_ReturnsOnePackageInstalledInProject ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = null;
			AddProjectToOpenProjects ("Test");
			var project = new FakePackageManagementProject ();
			fakeProjectFactory.CreatePackageManagementProject = (repository, msbuildProject) => {
				return project;
			};
			FakePackage notInstalledPackage = FakePackage.CreatePackageWithVersion ("NotInstalled", "1.0.0.0");
			fakeSolutionPackageRepository.FakeSharedRepository.FakePackages.Add (notInstalledPackage);
			FakePackage installedPackage = FakePackage.CreatePackageWithVersion ("Installed", "1.0.0.0");
			fakeSolutionPackageRepository.FakeSharedRepository.FakePackages.Add (installedPackage);
			project.FakePackages.Add (installedPackage);

			IQueryable<IPackage> packages = solution.GetPackages ();

			var expectedPackages = new FakePackage[] {
				installedPackage
			};

			Assert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetPackages_OnePackageInstalledIntoOneProjectButTwoPackagesInSolutionRepository_ReturnsOnlyOneProjectPackage ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = null;
			AddProjectToOpenProjects ("Test");
			var project = new FakePackageManagementProject ();
			fakeProjectFactory.CreatePackageManagementProject = (repository, msbuildProject) => {
				return project;
			};
			FakePackage installedSolutionPackage = FakePackage.CreatePackageWithVersion ("SolutionPackage", "1.0.0.0");
			fakeSolutionPackageRepository.FakeSharedRepository.FakePackages.Add (installedSolutionPackage);
			FakePackage installedProjectPackage = FakePackage.CreatePackageWithVersion ("ProjectPackage", "1.0.0.0");
			fakeSolutionPackageRepository.FakeSharedRepository.FakePackages.Add (installedProjectPackage);
			project.FakePackages.Add (installedProjectPackage);

			IQueryable<IPackage> packages = solution.GetPackages ();

			var expectedPackages = new FakePackage[] {
				installedProjectPackage
			};

			Assert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetPackages_TwoProjectsButNoPackagesInstalledInProjects_PackageProjectsCreatedUsingActiveRepository ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = null;
			FakeDotNetProject testProject1 = AddProjectToOpenProjects ("Test1");
			FakeDotNetProject testProject2 = AddProjectToOpenProjects ("Test2");
			AggregateRepository repository = CreateAggregateRepositoryWithOneRepository ();
			fakeRegisteredPackageRepositories.GetActiveRepositoryAction = () => repository;

			solution.GetPackages ();

			Assert.AreEqual (testProject1, fakeProjectFactory.ProjectsPassedToCreateProject [0]);
			Assert.AreEqual (testProject2, fakeProjectFactory.ProjectsPassedToCreateProject [1]);
			Assert.AreEqual (fakeRegisteredPackageRepositories.ActiveRepository, fakeProjectFactory.RepositoriesPassedToCreateProject [0]);
			Assert.AreEqual (fakeRegisteredPackageRepositories.ActiveRepository, fakeProjectFactory.RepositoriesPassedToCreateProject [1]);
		}

		[Test]
		public void GetPackages_OnePackageInstalledIntoPackagesFolderOnly_ReturnsNoPackages ()
		{
			CreateSolution ();
			fakeProjectService.CurrentProject = null;
			FakePackage fakePackage = FakePackage.CreatePackageWithVersion ("One", "1.0");
			fakeSolutionPackageRepository.FakeSharedRepository.FakePackages.Add (fakePackage);

			IQueryable<IPackage> packages = solution.GetPackages ();

			var expectedPackages = new FakePackage [0];
			Assert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void GetRepository_OpenSolution_ReturnsRepositoryForSolution ()
		{
			CreateSolution ();
			var fakeSolution = new FakeSolution (@"d:\projects\MyProject\MySolution.sln");
			fakeProjectService.OpenSolution = fakeSolution;

			ISolutionPackageRepository repository = solution.GetRepository ();

			Assert.IsNotNull (repository);
			Assert.AreEqual (fakeSolutionPackageRepositoryFactory.SolutionPassedToCreateSolutionPackageRepository, fakeSolution);
			Assert.AreEqual (fakeSolutionPackageRepositoryFactory.FakeSolutionPackageRepository, repository);
		}
	}
}


