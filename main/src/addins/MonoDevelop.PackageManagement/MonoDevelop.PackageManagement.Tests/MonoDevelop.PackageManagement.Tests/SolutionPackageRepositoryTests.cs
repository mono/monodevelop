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

using System.Linq;
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class SolutionPackageRepositoryTests
	{
		TestableSolutionPackageRepository repository;
		FakeSettingsProvider settingsProvider;
		FakeSolution solution;
		FakePackageRepositoryFactory fakeRepositoryFactory;

		void CreateSolution (string fileName)
		{
			solution = new FakeSolution (fileName);
		}

		void CreateFakeRepositoryFactory ()
		{
			fakeRepositoryFactory = new FakePackageRepositoryFactory ();
		}

		void CreateSettings ()
		{
			settingsProvider = new FakeSettingsProvider ();
		}

		void CreateRepository (ISolution solution, FakeSettingsProvider settings)
		{
			CreateFakeRepositoryFactory ();
			repository = new TestableSolutionPackageRepository (solution, fakeRepositoryFactory, settings);
		}

		void CreateRepository (ISolution solution)
		{
			CreateSettings ();
			CreateRepository (solution, settingsProvider);
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
	}
}
