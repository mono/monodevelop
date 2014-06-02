//
// SharpDevelopPackageManagerFactoryTests.cs
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
using ICSharpCode.PackageManagement;
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class MonoDevelopPackageManagerFactoryTests
	{
		SharpDevelopPackageManagerFactory factory;
		IPackageManager packageManager;
		FakePackageRepository fakePackageRepository;
		FakeDotNetProject testProject;
		PackageManagementOptions options;
		FakePackageRepositoryFactory fakePackageRepositoryFactory;
		FakeProjectSystemFactory fakeProjectSystemFactory;

		void CreateFactory ()
		{
			options = new TestablePackageManagementOptions ();
			fakePackageRepositoryFactory = new FakePackageRepositoryFactory ();
			fakeProjectSystemFactory = new FakeProjectSystemFactory ();
			factory = new SharpDevelopPackageManagerFactory (fakePackageRepositoryFactory, fakeProjectSystemFactory, options);
		}

		void CreateTestProject ()
		{
			testProject = ProjectHelper.CreateTestProject ();
			var solution = new FakeSolution {
				BaseDirectory = @"c:\projects\MyProject\".ToNativePath ()
			};
			testProject.ParentSolution = solution;
		}

		void CreatePackageManager ()
		{
			fakePackageRepository = new FakePackageRepository ();
			packageManager = factory.CreatePackageManager (fakePackageRepository, testProject);
		}

		[Test]
		public void CreatePackageManager_ProjectAndSolutionHaveDifferentFolders_PackageManagerLocalRepositoryIsSharedRepository ()
		{
			CreateFactory ();
			CreateTestProject ();
			CreatePackageManager ();
			ISharedPackageRepository sharedRepository = packageManager.LocalRepository as ISharedPackageRepository;

			Assert.IsNotNull (sharedRepository);
		}

		[Test]
		public void CreatePackageManager_PackagesSolutionFolderDefinedInOptions_SharedLocalRepositoryFileSystemRootIsSolutionFolder ()
		{
			CreateFactory ();
			CreateTestProject ();
			options.PackagesDirectory = "MyPackages";
			CreatePackageManager ();

			string expectedRoot = @"c:\projects\MyProject\MyPackages".ToNativePath ();
			string actualRoot = fakePackageRepositoryFactory.FileSystemPassedToCreateSharedRepository.Root;
			Assert.AreEqual (expectedRoot, actualRoot);
		}

		[Test]
		public void CreatePackageManager_PackagesSolutionFolderDefinedInOptions_SharedLocalRepositoryPackagePathResolverCreatedWithPackagesFolderInsideSolutionFolder ()
		{
			CreateFactory ();
			CreateTestProject ();
			options.PackagesDirectory = "MyPackages";
			CreatePackageManager ();

			FakePackage package = new FakePackage ("Test.Package");
			package.Version = new SemanticVersion (1, 0, 0, 0);
			string expectedDirectory = @"c:\projects\MyProject\MyPackages\Test.Package.1.0.0.0".ToNativePath ();
			string actualDirectory = 
				fakePackageRepositoryFactory
					.PathResolverPassedToCreateSharedRepository
					.GetInstallPath (package);

			Assert.AreEqual (expectedDirectory, actualDirectory);
		}

		[Test]
		public void CreatePackageManager_PackagesSolutionFolderDefinedInOptions_LocalRepositoryFileSystemIsPackageManagerFileSystem ()
		{
			CreateFactory ();
			CreateTestProject ();
			CreatePackageManager ();

			Assert.AreEqual (packageManager.FileSystem, fakePackageRepositoryFactory.FileSystemPassedToCreateSharedRepository);
		}

		[Test]
		public void CreatePackageManager_PackagesSolutionFolderDefinedInOptions_PackageManagerPathResolverUsesPackagesFolderInsideSolutionFolder ()
		{
			CreateFactory ();
			CreateTestProject ();
			options.PackagesDirectory = "packages";
			CreatePackageManager ();

			var package = new FakePackage ("TestPackage", "1.0.0.0");

			string expectedDirectory = @"c:\projects\MyProject\packages\TestPackage.1.0.0.0".ToNativePath ();

			SharpDevelopPackageManager sharpDevelopPackageManager = packageManager as SharpDevelopPackageManager;
			string actualDirectory = sharpDevelopPackageManager.PathResolver.GetInstallPath (package);

			Assert.AreEqual (expectedDirectory, actualDirectory);
		}
	}
}

