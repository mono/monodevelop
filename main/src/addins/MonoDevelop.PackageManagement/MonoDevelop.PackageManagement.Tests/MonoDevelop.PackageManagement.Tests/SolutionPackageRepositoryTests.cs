//
// SolutionPackageRepositoryTests.cs
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
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class SolutionPackageRepositoryTests
	{
		TestableSolutionPackageRepository repository;
		TestablePackageManagementOptions options;
		FakeSolution solution;
		FakePackageRepositoryFactory fakeRepositoryFactory;
		FakeSharedPackageRepository fakeSharedRepository;

		void CreateSolution (string fileName)
		{
			solution = new FakeSolution (fileName);
		}

		void CreateFakeRepositoryFactory ()
		{
			fakeRepositoryFactory = new FakePackageRepositoryFactory ();
			fakeSharedRepository = fakeRepositoryFactory.FakeSharedRepository;
		}

		void CreateOptions ()
		{
			options = new TestablePackageManagementOptions ();
		}

		void CreateRepository (ISolution solution, TestablePackageManagementOptions options)
		{
			CreateFakeRepositoryFactory ();
			repository = new TestableSolutionPackageRepository (solution, fakeRepositoryFactory, options);
		}

		void CreateRepository (ISolution solution)
		{
			CreateOptions ();
			CreateRepository (solution, options);
		}

		void CreateRepository ()
		{
			CreateSolution (@"d:\projects\test\myproject\myproject.sln");
			CreateRepository (solution);
		}

		FakePackage AddPackageToSharedRepository (string packageId)
		{
			FakeSharedPackageRepository sharedRepository = fakeRepositoryFactory.FakeSharedRepository;
			return sharedRepository.AddFakePackage (packageId);
		}

		FakePackage AddPackageToSharedRepository (string packageId, string version)
		{
			FakeSharedPackageRepository sharedRepository = fakeRepositoryFactory.FakeSharedRepository;
			return sharedRepository.AddFakePackageWithVersion (packageId, version);
		}

		PackageReference CreatePackageReference (string packageId, string packageVersion)
		{
			SemanticVersion version = null;
			if (packageVersion != null) {
				version = new SemanticVersion (packageVersion);
			}

			return new PackageReference (
				packageId,
				version,
				null,
				null,
				false,
				false
			);
		}

		void AddFileToLocalRepositoryLookupPath (PackageReference packageReference, string filePath)
		{
			filePath = filePath.ToNativePath ();
			var packageName = new PackageName (packageReference.Id, packageReference.Version);
			repository.LocalPackageRepository.AddPackageLookupPath (packageName, filePath);
		}

		[Test]
		public void GetInstallPath_GetInstallPathForPackage_ReturnsPackagePathInsideSolutionPackagesRepository ()
		{
			CreateSolution (@"d:\projects\Test\MySolution\MyProject.sln");
			CreateOptions ();
			options.PackagesDirectory = "MyPackages";
			CreateRepository (solution, options);

			var package = FakePackage.CreatePackageWithVersion ("MyPackage", "1.0.1.40");

			string installPath = repository.GetInstallPath (package);

			string expectedInstallPath = 
				@"d:\projects\Test\MySolution\MyPackages\MyPackage.1.0.1.40".ToNativePath ();

			Assert.AreEqual (expectedInstallPath, installPath);
		}

		[Test]
		public void GetPackagesByDependencyOrder_OnePackageInSharedRepository_ReturnsOnePackage ()
		{
			CreateRepository ();
			AddPackageToSharedRepository ("Test");

			List<FakePackage> expectedPackages = fakeSharedRepository.FakePackages;

			List<IPackage> actualPackages = repository.GetPackagesByDependencyOrder ().ToList ();

			PackageCollectionAssert.AreEqual (expectedPackages, actualPackages);
		}

		[Test]
		public void GetPackagesByDependencyOrder_OnePackageInSharedRepository_SharedRepositoryCreatedWithPathResolverForSolutionPackagesFolder ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);
			FakePackage package = AddPackageToSharedRepository ("Test", "1.0");

			repository.GetPackagesByDependencyOrder ().ToList ();

			IPackagePathResolver pathResolver = fakeRepositoryFactory.PathResolverPassedToCreateSharedRepository;
			string installPath = pathResolver.GetInstallPath (package);

			string expectedInstallPath = @"d:\projects\myproject\packages\Test.1.0".ToNativePath ();

			Assert.AreEqual (expectedInstallPath, installPath);
		}

		[Test]
		public void Constructor_CreateInstance_SharedRepositoryCreatedWithFileSystemForSolutionPackagesFolder ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);

			IFileSystem fileSystem = fakeRepositoryFactory.FileSystemPassedToCreateSharedRepository;
			string rootPath = fileSystem.Root;

			string expectedRootPath = @"d:\projects\myproject\packages".ToNativePath ();

			Assert.AreEqual (expectedRootPath, rootPath);
		}

		[Test]
		public void Constructor_CreateInstance_SharedRepositoryCreatedWithConfigSettingsFileSystemForSolutionNuGetFolder ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);

			IFileSystem fileSystem = fakeRepositoryFactory.ConfigSettingsFileSystemPassedToCreateSharedRepository;
			string rootPath = fileSystem.Root;

			string expectedRootPath = @"d:\projects\myproject\.nuget".ToNativePath ();

			Assert.AreEqual (expectedRootPath, rootPath);
		}

		[Test]
		public void GetPackagesByDependencyOrder_TwoPackagesInSharedRepositoryFirstPackageDependsOnSecond_ReturnsSecondPackageFirst ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);
			FakePackage firstPackage = AddPackageToSharedRepository ("First");
			firstPackage.AddDependency ("Second");
			FakePackage secondPackage = AddPackageToSharedRepository ("Second");

			List<IPackage> actualPackages = repository.GetPackagesByDependencyOrder ().ToList ();

			var expectedPackages = new IPackage[] {
				secondPackage,
				firstPackage
			};

			Assert.AreEqual (expectedPackages, actualPackages);
		}

		[Test]
		public void GetPackagesByReverseDependencyOrder_TwoPackagesInSharedRepositorySecondPackageDependsOnFirst_ReturnsSecondPackageFirst ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);
			FakePackage firstPackage = AddPackageToSharedRepository ("First");
			FakePackage secondPackage = AddPackageToSharedRepository ("Second");
			secondPackage.AddDependency ("First");

			List<IPackage> actualPackages = repository.GetPackagesByReverseDependencyOrder ().ToList ();

			var expectedPackages = new IPackage[] {
				secondPackage,
				firstPackage
			};

			Assert.AreEqual (expectedPackages, actualPackages);
		}

		[Test]
		public void IsInstalled_PackageIsInSharedRepository_ReturnsTrue ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);
			FakePackage firstPackage = AddPackageToSharedRepository ("First");

			bool installed = repository.IsInstalled (firstPackage);

			Assert.IsTrue (installed);
		}

		[Test]
		public void IsInstalled_PackageIsNotInSharedRepository_ReturnsFalse ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);
			FakePackage testPackage = new FakePackage ("Test");

			bool installed = repository.IsInstalled (testPackage);

			Assert.IsFalse (installed);
		}

		[Test]
		public void GetPackages_OnePackageIsInSharedRepository_ReturnsOnePackage ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);
			FakePackage firstPackage = AddPackageToSharedRepository ("First");

			IQueryable<IPackage> packages = repository.GetPackages ();

			var expectedPackages = new FakePackage[] {
				firstPackage
			};

			PackageCollectionAssert.AreEqual (expectedPackages, packages);
		}

		[Test]
		public void IsRestored_PackageReferenceHasNullVersion_ReturnsFalse ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);
			PackageReference packageReference = CreatePackageReference ("MyPackage", null);

			bool restored = repository.IsRestored (packageReference);

			Assert.IsFalse (restored);
		}

		[Test]
		public void IsRestored_OnePackageLookupPathForPackageReference_ReturnTrue ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);
			PackageReference packageReference = CreatePackageReference ("MyPackage", "1.2.3.4");
			AddFileToLocalRepositoryLookupPath (
				packageReference,
				@"d:\projects\myproject\packages\MyPackage.1.2.3.4\MyPackage.1.2.3.4.nupkg");

			bool restored = repository.IsRestored (packageReference);

			Assert.IsTrue (restored);
		}

		[Test]
		public void IsRestored_NoPackageLookupPathsForPackageReference_ReturnFalse ()
		{
			CreateSolution (@"d:\projects\myproject\myproject.sln");
			CreateRepository (solution);
			PackageReference packageReference = CreatePackageReference ("MyPackage", "1.2.3.4");

			bool restored = repository.IsRestored (packageReference);

			Assert.IsFalse (restored);
		}
	}
}
