//
// PackageManagementProjectTests.cs
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

using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementProjectTests
	{
		FakePackageManagerFactory fakePackageManagerFactory;
		FakePackageRepository fakeSourceRepository;
		FakeDotNetProject fakeProject;
		PackageManagementProject project;
		FakeProjectManager fakeProjectManager;
		FakePackageManager fakePackageManager;
		PackageManagementEvents packageManagementEvents;

		void CreateProject ()
		{
			fakeSourceRepository = new FakePackageRepository ();
			CreateProject (fakeSourceRepository);
		}

		void CreateProject (IPackageRepository sourceRepository)
		{
			fakePackageManagerFactory = new FakePackageManagerFactory ();
			fakePackageManager = fakePackageManagerFactory.FakePackageManager;
			fakeProjectManager = fakePackageManager.FakeProjectManager;
			fakeProject = new FakeDotNetProject ();
			packageManagementEvents = new PackageManagementEvents ();

			project = new PackageManagementProject (
				sourceRepository,
				fakeProject,
				packageManagementEvents,
				fakePackageManagerFactory);
		}

		[Test]
		public void IsInstalled_PackageIsInstalled_ReturnsTrue ()
		{
			CreateProject ();
			fakeProjectManager.IsInstalledReturnValue = true;
			var package = new FakePackage ("Test");

			bool installed = project.IsPackageInstalled (package);

			Assert.IsTrue (installed);
		}

		[Test]
		public void IsInstalled_PackageIsNotInstalled_ReturnsFalse ()
		{
			CreateProject ();
			fakeProjectManager.IsInstalledReturnValue = false;
			var package = new FakePackage ("Test");

			bool installed = project.IsPackageInstalled (package);

			Assert.IsFalse (installed);
		}

		[Test]
		public void IsInstalled_PackageIsInstalled_PackagePassedToProjectManager ()
		{
			CreateProject ();
			fakeProjectManager.IsInstalledReturnValue = false;
			var expectedPackage = new FakePackage ("Test");

			project.IsPackageInstalled (expectedPackage);
			IPackage actualPackage = fakeProjectManager.PackagePassedToIsInstalled;

			Assert.AreEqual (expectedPackage, actualPackage);
		}

		[Test]
		public void Constructor_RepositoryAndProjectPassed_RepositoryUsedToCreatePackageManager ()
		{
			CreateProject ();
			IPackageRepository actualrepository = fakePackageManagerFactory.PackageRepositoryPassedToCreatePackageManager;

			Assert.AreEqual (fakeSourceRepository, actualrepository);
		}

		[Test]
		public void Constructor_RepositoryAndProjectPassed_ProjectUsedToCreatePackageManager ()
		{
			CreateProject ();
			var actualProject = fakePackageManagerFactory.ProjectPassedToCreateRepository;

			Assert.AreEqual (fakeProject, actualProject);
		}

		[Test]
		public void GetPackages_ProjectManagerLocalRepositoryHasTwoPackages_ReturnsTwoPackages ()
		{
			CreateProject ();
			FakePackageRepository repository = fakeProjectManager.FakeLocalRepository;
			FakePackage packageA = repository.AddFakePackage ("A");
			FakePackage packageB = repository.AddFakePackage ("B");

			IQueryable<IPackage> actualPackages = project.GetPackages ();

			var expectedPackages = new FakePackage[] {
				packageA,
				packageB
			};

			PackageCollectionAssert.AreEqual (expectedPackages, actualPackages);
		}

		[Test]
		public void Logger_SetLogger_LoggerOnPackageManagerIsSet ()
		{
			CreateProject ();
			var expectedLogger = new FakeLogger ();

			project.Logger = expectedLogger;

			Assert.AreEqual (expectedLogger, fakePackageManager.Logger);
		}

		[Test]
		public void Logger_GetLogger_LoggerOnPackageManagerIsReturned ()
		{
			CreateProject ();

			ILogger logger = project.Logger;
			ILogger expectedLogger = fakePackageManager.Logger;

			Assert.AreEqual (expectedLogger, logger);
		}

		[Test]
		public void SourceRepository_NewInstance_ReturnsRepositoryUsedToCreateInstance ()
		{
			CreateProject ();
			IPackageRepository repository = project.SourceRepository;

			Assert.AreEqual (fakeSourceRepository, repository);
		}

		[Test]
		public void Logger_SetLogger_ProjectManagerUsesLogger ()
		{
			CreateProject ();
			ILogger expectedLogger = new NullLogger ();
			project.Logger = expectedLogger;
			ILogger actualLogger = fakePackageManager.ProjectManager.Logger;

			Assert.AreEqual (expectedLogger, actualLogger);
		}

		[Test]
		public void Logger_SetLogger_ProjectManagerProjectSystemUsesLogger ()
		{
			CreateProject ();
			ILogger expectedLogger = new NullLogger ();
			project.Logger = expectedLogger;
			ILogger actualLogger = fakePackageManager.ProjectManager.Project.Logger;

			Assert.AreEqual (expectedLogger, actualLogger);
		}

		[Test]
		public void Logger_SetLogger_PackageManagerFileSystemUsesLogger ()
		{
			CreateProject ();
			ILogger expectedLogger = new NullLogger ();
			project.Logger = expectedLogger;
			ILogger actualLogger = fakePackageManager.FileSystem.Logger;

			Assert.AreEqual (expectedLogger, actualLogger);
		}

		[Test]
		public void Logger_GetLogger_ReturnsLogger ()
		{
			CreateProject ();
			ILogger expectedLogger = new NullLogger ();
			project.Logger = expectedLogger;
			ILogger actualLogger = project.Logger;

			Assert.AreEqual (expectedLogger, actualLogger);
		}

		[Test]
		public void PackageInstalled_PackagerManagerPackageInstalledEventFired_EventFiresWithPackage ()
		{
			CreateProject ();
			PackageOperationEventArgs eventArgs = null;
			project.PackageInstalled += (sender, e) => eventArgs = e;

			var expectedEventArgs = new PackageOperationEventArgs (new FakePackage (), null, String.Empty);
			fakePackageManager.FirePackageInstalled (expectedEventArgs);

			Assert.AreEqual (expectedEventArgs, eventArgs);
		}

		[Test]
		public void PackageUninstalled_PackagerManagerPackageUninstalledEventFired_EventFiresWithPackage ()
		{
			CreateProject ();
			PackageOperationEventArgs eventArgs = null;
			project.PackageUninstalled += (sender, e) => eventArgs = e;

			var expectedEventArgs = new PackageOperationEventArgs (new FakePackage (), null, String.Empty);
			fakePackageManager.FirePackageUninstalled (expectedEventArgs);

			Assert.AreEqual (expectedEventArgs, eventArgs);
		}

		[Test]
		public void PackageReferenceAdded_ProjectManagerPackageReferenceAddedEventFired_EventFiresWithPackage ()
		{
			CreateProject ();
			PackageOperationEventArgs eventArgs = null;
			project.PackageReferenceAdded += (sender, e) => eventArgs = e;

			var expectedPackage = new FakePackage ();
			fakeProjectManager.FirePackageReferenceAdded (expectedPackage);

			Assert.AreEqual (expectedPackage, eventArgs.Package);
		}

		[Test]
		public void PackageReferenceRemoved_ProjectManagerPackageReferenceRemovedEventFired_EventFiresWithPackage ()
		{
			CreateProject ();
			PackageOperationEventArgs eventArgs = null;
			project.PackageReferenceRemoved += (sender, e) => eventArgs = e;

			var expectedPackage = new FakePackage ();
			fakeProjectManager.FirePackageReferenceRemoved (expectedPackage);

			Assert.AreEqual (expectedPackage, eventArgs.Package);
		}

		[Test]
		public void Name_MSBuildProjectNameIsSet_ReturnsMSBuildProjectName ()
		{
			CreateProject ();
			fakeProject.Name = "MyProject";

			string name = project.Name;

			Assert.AreEqual ("MyProject", name);
		}

		[Test]
		public void IsInstalled_PackageIdPassedAndPackageIsInstalled_ReturnsTrue ()
		{
			CreateProject ();
			fakeProjectManager.IsInstalledReturnValue = true;

			bool installed = project.IsPackageInstalled ("Test");

			Assert.IsTrue (installed);
		}

		[Test]
		public void IsInstalled_PackageIdPassedAndPackageIsNotInstalled_ReturnsFalse ()
		{
			CreateProject ();
			fakeProjectManager.IsInstalledReturnValue = false;

			bool installed = project.IsPackageInstalled ("Test");

			Assert.IsFalse (installed);
		}

		[Test]
		public void IsInstalled_PackageIdPassedPackageIsInstalled_PackageIdPassedToProjectManager ()
		{
			CreateProject ();
			fakeProjectManager.IsInstalledReturnValue = false;

			project.IsPackageInstalled ("Test");
			string id = fakeProjectManager.PackageIdPassedToIsInstalled;

			Assert.AreEqual ("Test", id);
		}

		[Test]
		public void GetPackagesInReverseDependencyOrder_TwoPackages_ReturnsPackagesFromProjectLocalRepositoryInCorrectOrder ()
		{
			CreateProject ();
			FakePackage packageA = fakeProjectManager.FakeLocalRepository.AddFakePackageWithVersion ("A", "1.0");
			FakePackage packageB = fakeProjectManager.FakeLocalRepository.AddFakePackageWithVersion ("B", "1.0");

			packageB.DependenciesList.Add (new PackageDependency ("A"));

			var expectedPackages = new FakePackage[] {
				packageB,
				packageA
			};

			IEnumerable<IPackage> packages = project.GetPackagesInReverseDependencyOrder ();

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void HasOlderPackageInstalled_PackageIsInstalled_ReturnsTrue ()
		{
			CreateProject ();
			fakeProjectManager.HasOlderPackageInstalledReturnValue = true;
			var package = new FakePackage ("Test");

			bool installed = project.HasOlderPackageInstalled (package);

			Assert.IsTrue (installed);
		}

		[Test]
		public void HasOlderPackageInstalled_PackageIsNotInstalled_ReturnsFalse ()
		{
			CreateProject ();
			fakeProjectManager.HasOlderPackageInstalledReturnValue = false;
			var package = new FakePackage ("Test");

			bool installed = project.HasOlderPackageInstalled (package);

			Assert.IsFalse (installed);
		}

		[Test]
		public void Logger_SetLoggerWhenSourceRepositoryIsAggregateRepository_LoggerOnAggregateRepositoryIsSet ()
		{
			var aggregateRepository = new AggregateRepository (new FakePackageRepository [0]);
			CreateProject (aggregateRepository);
			var expectedLogger = new FakeLogger ();

			project.Logger = expectedLogger;

			Assert.AreEqual (expectedLogger, aggregateRepository.Logger);
		}

		[Test]
		public void GetPackageReferences_ProjectManagerHasOnePackageReference_ReturnsOnePackageReference ()
		{
			CreateProject ();
			fakeProjectManager.AddPackageReference ("MyPackage", "1.2.3.4");

			List<PackageReference> packageReferences = project.GetPackageReferences ().ToList ();
			PackageReference packageReference = packageReferences.FirstOrDefault ();

			Assert.AreEqual ("MyPackage", packageReference.Id);
			Assert.AreEqual ("1.2.3.4", packageReference.Version.ToString ());
			Assert.AreEqual (1, packageReferences.Count);
		}

		[Test]
		public void AnyUnrestoredPackages_LocalRepositoryHasPackagesForEachPackageReference_ReturnsFalse ()
		{
			CreateProject ();
			fakeProjectManager.AddPackageReference ("MyPackage", "1.2.3.4");
			fakeProjectManager.FakeLocalRepository.AddFakePackageWithVersion ("MyPackage", "1.2.3.4");

			bool result = project.AnyUnrestoredPackages ();

			Assert.IsFalse (result);
		}

		[Test]
		public void AnyUnrestoredPackages_LocalRepositoryHasNoPackagesAndProjectHasOnePackageReference_ReturnsTrue ()
		{
			CreateProject ();
			fakeProjectManager.AddPackageReference ("MyPackage", "1.2.3.4");

			bool result = project.AnyUnrestoredPackages ();

			Assert.IsTrue (result);
		}

		[Test]
		public void AnyUnrestoredPackages_LocalRepositoryHasDifferentPackageVersion_ReturnsTrue ()
		{
			CreateProject ();
			fakeProjectManager.AddPackageReference ("MyPackage", "1.2.3.4");
			fakeProjectManager.FakeLocalRepository.AddFakePackageWithVersion ("MyPackage", "1.0");

			bool result = project.AnyUnrestoredPackages ();

			Assert.IsTrue (result);
		}

		[Test]
		public void ConstraintProvider_LocalRepositoryDoesNotImplementIConstraintProvider_ReturnsNullConstraintProviderInstance ()
		{
			CreateProject ();

			IPackageConstraintProvider provider = project.ConstraintProvider;

			Assert.AreEqual (NullConstraintProvider.Instance, provider);
		}

		[Test]
		public void ConstraintProvider_LocalRepositoryImplementsIConstraintProvider_ReturnsLocalRepository ()
		{
			CreateProject ();
			var localRepository = new FakePackageRepositoryWithConstraintProvider ();
			fakeProjectManager.FakeLocalRepository = localRepository;

			IPackageConstraintProvider provider = project.ConstraintProvider;

			Assert.AreEqual (localRepository, provider);
		}
	}
}


