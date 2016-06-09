//
// MonoDevelopPackageManagerTests.cs
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.PackageManagement;
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class MonoDevelopPackageManagerTests
	{
		MonoDevelopPackageManager packageManager;
		FakePackageRepository fakeFeedSourceRepository;
		FakeSharedPackageRepository fakeSolutionSharedRepository;
		FakeProject project;
		PackageManagementOptions options;
		SolutionPackageRepositoryPath repositoryPaths;
		PackageReferenceRepositoryHelper packageRefRepositoryHelper;
		TestableProjectManager testableProjectManager;
		FakeFileSystem fakeFileSystem;
		FakePackageOperationResolver fakePackageOperationResolver;

		void CreatePackageManager (IProject project, PackageReferenceRepositoryHelper packageRefRepositoryHelper)
		{
			CreatePackageManager (project, packageRefRepositoryHelper, new FakePackageRepository ());
		}

		void CreatePackageManager (
			IProject project,
			PackageReferenceRepositoryHelper packageRefRepositoryHelper,
			IPackageRepository sourceRepository)
		{
			options = new TestablePackageManagementOptions ();

			repositoryPaths = new SolutionPackageRepositoryPath (project, options);
			var pathResolver = new DefaultPackagePathResolver (repositoryPaths.PackageRepositoryPath);

			fakeFileSystem = new FakeFileSystem ();

			fakeFeedSourceRepository = sourceRepository as FakePackageRepository;
			fakeSolutionSharedRepository = packageRefRepositoryHelper.FakeSharedSourceRepository;

			var fakeSolutionPackageRepository = new FakeSolutionPackageRepository ();
			fakeSolutionPackageRepository.FileSystem = fakeFileSystem;
			fakeSolutionPackageRepository.PackagePathResolver = pathResolver;
			fakeSolutionPackageRepository.FakeSharedRepository = fakeSolutionSharedRepository;

			packageManager = new MonoDevelopPackageManager (sourceRepository,
				packageRefRepositoryHelper.FakeProjectSystem,
				fakeSolutionPackageRepository);
		}

		void CreatePackageManager ()
		{
			CreatePackageManager (new FakePackageRepository ());
		}

		void CreatePackageManager (IPackageRepository sourceRepository)
		{
			CreateTestProject ();
			CreatePackageReferenceRepositoryHelper ();
			CreatePackageManager (project, packageRefRepositoryHelper, sourceRepository);
		}

		void CreatePackageReferenceRepositoryHelper ()
		{
			packageRefRepositoryHelper = new PackageReferenceRepositoryHelper ();
		}

		void CreateTestProject ()
		{
			var solution = new FakeSolution (@"c:\projects\Test\MyProject\MySolution.sln");
			solution.BaseDirectory = @"c:\projects\Test\MyProject";
			project = new FakeProject ();
			project.ParentSolution = solution;
		}

		void CreateTestableProjectManager ()
		{
			testableProjectManager = new TestableProjectManager ();
			packageManager.ProjectManager = testableProjectManager;
		}

		FakePackage CreateFakePackage (string id = "Test", string version = "1.0.0.0")
		{
			return new FakePackage (id, version);
		}

		FakePackage InstallPackage ()
		{
			FakePackage package = CreateFakePackage ();
			packageManager.InstallPackage (package);
			return package;
		}

		FakePackage InstallPackageAndIgnoreDependencies ()
		{
			return InstallPackageWithParameters (true, false);
		}

		FakePackage InstallPackageWithParameters (bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			FakePackage package = CreateFakePackage ();
			packageManager.InstallPackage (package, ignoreDependencies, allowPrereleaseVersions);
			return package;
		}

		FakePackage InstallPackageAndAllowPrereleaseVersions ()
		{
			return InstallPackageWithParameters (false, true);
		}

		FakePackage InstallPackageAndDoNotAllowPrereleaseVersions ()
		{
			return InstallPackageWithParameters (false, false);
		}

		FakePackage InstallPackageAndDoNotIgnoreDependencies ()
		{
			return InstallPackageWithParameters (false, false);
		}

		FakePackage UninstallPackage ()
		{
			FakePackage package = CreateFakePackage ();
			testableProjectManager.FakeLocalRepository.FakePackages.Add (package);

			packageManager.UninstallPackage (package);
			return package;
		}

		FakePackage UninstallPackageAndForceRemove ()
		{
			FakePackage package = CreateFakePackage ();
			testableProjectManager.FakeLocalRepository.FakePackages.Add (package);

			bool removeDependencies = false;
			bool forceRemove = true;
			packageManager.UninstallPackage (package, forceRemove, removeDependencies);

			return package;
		}

		FakePackage UninstallPackageAndRemoveDependencies ()
		{
			FakePackage package = CreateFakePackage ();
			testableProjectManager.FakeLocalRepository.FakePackages.Add (package);

			bool removeDependencies = true;
			bool forceRemove = false;
			packageManager.UninstallPackage (package, forceRemove, removeDependencies);

			return package;
		}

		PackageOperation CreateOneInstallPackageOperation (string id = "PackageToInstall", string version = "1.0")
		{
			FakePackage package = CreateFakePackage (id, version);
			return new PackageOperation (package, PackageAction.Install);
		}

		PackageOperation AddInstallOperationForPackage (IPackage package)
		{
			var operation = new PackageOperation (package, PackageAction.Install);
			AddInstallOperationsForPackage (package, operation);
			return operation;
		}

		void AddInstallOperationsForPackage (IPackage package, params PackageOperation[] operations)
		{
			fakePackageOperationResolver.AddOperations (package, operations);
		}

		void RaisePackageRemovedEventWhenPackageReferenceUpdated (
			FakeProjectManager projectManager,
			FakePackage updatedPackage,
			params PackageOperationEventArgs[] eventArgs)
		{
			projectManager.WhenUpdatePackageReferenceCalled (
				updatedPackage.Id,
				updatedPackage.Version,
				() => eventArgs.ToList ().ForEach (eventArg => projectManager.FirePackageReferenceRemoved (eventArg)));
		}

		void RaisePackageAddedEventWhenPackageReferenceUpdated (
			FakeProjectManager projectManager,
			FakePackage updatedPackage,
			params PackageOperationEventArgs[] eventArgs)
		{
			projectManager.WhenUpdatePackageReferenceCalled (
				updatedPackage.Id,
				updatedPackage.Version,
				() => eventArgs.ToList ().ForEach (eventArg => projectManager.FirePackageReferenceAdded (eventArg)));
		}

		void RaisePackageRemovedEventWhenPackageReferenceAdded (
			FakeProjectManager projectManager,
			FakePackage newPackage,
			params PackageOperationEventArgs[] eventArgs)
		{
			projectManager.WhenAddPackageReferenceCalled (
				newPackage.Id,
				newPackage.Version,
				() => eventArgs.ToList ().ForEach (eventArg => projectManager.FirePackageReferenceRemoved (eventArg)));
		}

		void RaisePackageAddedEventWhenPackageReferenceAdded (
			FakeProjectManager projectManager,
			FakePackage newPackage,
			params PackageOperationEventArgs[] eventArgs)
		{
			projectManager.WhenAddPackageReferenceCalled (
				newPackage.Id,
				newPackage.Version,
				() => eventArgs.ToList ().ForEach (eventArg => projectManager.FirePackageReferenceAdded (eventArg)));
		}

		[Test]
		public void ProjectManager_InstanceCreated_SourceRepositoryIsAggregrateRepositoryContainingSharedRepositoryPassedToPackageManager ()
		{
			CreatePackageManager ();

			var aggregateRepository = packageManager.ProjectManager.SourceRepository as AggregateRepository;
			var secondaryRepository = aggregateRepository.Repositories.Last () as FakePackageRepository;
			Assert.AreEqual (2, aggregateRepository.Repositories.Count ());
			Assert.AreEqual (fakeSolutionSharedRepository, aggregateRepository.Repositories.First ());
			Assert.IsTrue (secondaryRepository.IsCloneOf (fakeFeedSourceRepository));
		}

		[Test]
		public void ProjectManager_LocalRepositoryIsFallbackRepository_SourceRepositoryIsFallbackContainingSharedRepositoryPassedToPackageManager ()
		{
			var primaryRepository = new FakePackageRepository ();
			var dependencyResolver = new FakePackageRepository ();
			var fallbackRepository = new FallbackRepository (primaryRepository, dependencyResolver);
			CreatePackageManager (fallbackRepository);

			var sourceRepository = packageManager.ProjectManager.SourceRepository as FallbackRepository;
			var aggregateRepository = sourceRepository.SourceRepository as AggregateRepository;
			Assert.AreEqual (dependencyResolver, sourceRepository.DependencyResolver);
			var secondaryRepository = aggregateRepository.Repositories.Last () as FakePackageRepository;
			Assert.AreEqual (2, aggregateRepository.Repositories.Count ());
			Assert.AreEqual (fakeSolutionSharedRepository, aggregateRepository.Repositories.First ());
			Assert.IsTrue (secondaryRepository.IsCloneOf (primaryRepository));
		}

		[Test]
		public void ProjectManager_InstanceCreated_LocalRepositoryIsPackageReferenceRepository ()
		{
			CreatePackageManager ();
			PackageReferenceRepository packageRefRepository = packageManager.ProjectManager.LocalRepository as PackageReferenceRepository;
			Assert.IsNotNull (packageRefRepository);
		}

		[Test]
		public void ProjectManager_InstanceCreated_LocalRepositoryIsRegisteredWithSharedRepository ()
		{
			CreateTestProject ();
			CreatePackageReferenceRepositoryHelper ();

			string expectedPath = @"c:\projects\Test\MyProject";
			packageRefRepositoryHelper.FakeProjectSystem.PathToReturnFromGetFullPath = expectedPath;

			CreatePackageManager (project, packageRefRepositoryHelper);

			string actualPath = fakeSolutionSharedRepository.PathPassedToRegisterRepository;

			Assert.AreEqual (expectedPath, actualPath);
		}

		[Test]
		public void InstallPackage_PackageInstancePassed_AddsReferenceToProject ()
		{
			CreatePackageManager ();
			FakePackage package = InstallPackage ();

			Assert.AreEqual (package, testableProjectManager.PackagePassedToAddPackageReference);
		}

		[Test]
		public void InstallPackage_PackageInstancePassed_DependenciesNotIgnoredWhenAddingReferenceToProject ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			InstallPackage ();

			Assert.IsFalse (testableProjectManager.IgnoreDependenciesPassedToAddPackageReference);
		}

		[Test]
		public void InstallPackage_PackageInstancePassed_PrereleaseVersionsNotAllowedWhenAddingReferenceToProject ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			InstallPackage ();

			Assert.IsFalse (testableProjectManager.AllowPrereleaseVersionsPassedToAddPackageReference);
		}

		[Test]
		public void InstallPackage_PackageDependenciesIgnored_IgnoreDependenciesPassedToProjectManager ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			InstallPackageAndIgnoreDependencies ();

			Assert.IsTrue (testableProjectManager.IgnoreDependenciesPassedToAddPackageReference);
		}

		[Test]
		public void InstallPackage_AllowPrereleaseVersions_AllowPrereleaseVersionsPassedToProjectManager ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			InstallPackageAndAllowPrereleaseVersions ();

			Assert.IsTrue (testableProjectManager.AllowPrereleaseVersionsPassedToAddPackageReference);
		}

		[Test]
		public void InstallPackage_PackageDependenciesIgnored_AddsReferenceToPackage ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			FakePackage package = InstallPackageAndIgnoreDependencies ();

			Assert.AreEqual (package, testableProjectManager.PackagePassedToAddPackageReference);
		}

		[Test]
		public void InstallPackage_PackageDependenciesNotIgnored_IgnoreDependenciesPassedToProjectManager ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			InstallPackageAndDoNotIgnoreDependencies ();

			Assert.IsFalse (testableProjectManager.IgnoreDependenciesPassedToAddPackageReference);
		}

		[Test]
		public void InstallPackage_PackageDependenciesNotIgnored_AddsReferenceToPackage ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			FakePackage package = InstallPackageAndDoNotIgnoreDependencies ();

			Assert.AreEqual (package, testableProjectManager.PackagePassedToAddPackageReference);
		}

		[Test]
		public void UninstallPackage_PackageInProjectLocalRepository_RemovesReferenceFromProject ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			FakePackage package = UninstallPackage ();

			Assert.AreEqual (package.Id, testableProjectManager.PackagePassedToRemovePackageReference.Id);
		}

		[Test]
		public void UninstallPackage_PackageInProjectLocalRepository_DoesNotRemoveReferenceForcefullyFromProject ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			UninstallPackage ();

			Assert.IsFalse (testableProjectManager.ForcePassedToRemovePackageReference);
		}

		[Test]
		public void UninstallPackage_PackageInProjectLocalRepository_DependenciesNotRemovedWhenPackageReferenceRemovedFromProject ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();
			UninstallPackage ();

			Assert.IsFalse (testableProjectManager.RemoveDependenciesPassedToRemovePackageReference);
		}

		[Test]
		public void UninstallPackage_PassingForceRemove_ReferenceForcefullyRemovedFromProject ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();

			UninstallPackageAndForceRemove ();

			Assert.IsTrue (testableProjectManager.ForcePassedToRemovePackageReference);
		}

		[Test]
		public void UninstallPackage_PassingRemoveDependencies_DependenciesRemovedWhenPackageReferenceRemovedFromProject ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();

			UninstallPackageAndRemoveDependencies ();

			Assert.IsTrue (testableProjectManager.RemoveDependenciesPassedToRemovePackageReference);
		}

		[Test]
		public void UninstallPackage_ProjectLocalRepositoryHasPackage_PackageRemovedFromProjectRepositoryBeforeSolutionRepository ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();

			FakePackage package = CreateFakePackage ();
			package.Id = "Test";

			testableProjectManager.FakeLocalRepository.FakePackages.Add (package);

			IPackage packageRemovedFromProject = null;
			packageManager.PackageUninstalled += (sender, e) => {
				packageRemovedFromProject = testableProjectManager.PackagePassedToRemovePackageReference;
			};
			packageManager.UninstallPackage (package);

			Assert.AreEqual ("Test", packageRemovedFromProject.Id);
		}

		[Test]
		public void UninstallPackage_PackageReferencedByNoProjects_PackageIsRemovedFromSharedSolutionRepository ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();

			FakePackage package = CreateFakePackage ();
			package.Id = "MyPackageId";

			testableProjectManager.FakeLocalRepository.FakePackages.Add (package);
			fakeSolutionSharedRepository.FakePackages.Add (package);

			packageManager.UninstallPackage (package);

			bool containsPackage = fakeSolutionSharedRepository.FakePackages.Contains (package);

			Assert.IsFalse (containsPackage);
		}

		[Test]
		public void UninstallPackage_PackageReferencedByTwoProjects_PackageIsNotRemovedFromSharedSolutionRepository ()
		{
			CreatePackageManager ();
			CreateTestableProjectManager ();

			var package = new FakePackage ("MyPackageId", "1.4.5.2");

			testableProjectManager.FakeLocalRepository.FakePackages.Add (package);
			fakeSolutionSharedRepository.FakePackages.Add (package);
			fakeSolutionSharedRepository.PackageIdsReferences.Add ("MyPackageId");

			packageManager.UninstallPackage (package);

			bool containsPackage = fakeSolutionSharedRepository.FakePackages.Contains (package);

			Assert.IsTrue (containsPackage);
			Assert.AreEqual ("MyPackageId", fakeSolutionSharedRepository.PackageIdPassedToIsReferenced);
			Assert.AreEqual (package.Version, fakeSolutionSharedRepository.VersionPassedToIsReferenced);
		}
	}
}


