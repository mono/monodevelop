//
// RegisteredPackageRepositoriesTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class RegisteredPackageRepositoriesTests
	{
		RegisteredPackageRepositories registeredRepositories;
		OneRegisteredPackageSourceHelper packageSourcesHelper;
		FakePackageRepositoryFactory fakeRepositoryCache;

		void CreateRegisteredPackageRepositories ()
		{
			CreatePackageSourcesHelper ();
			CreateRegisteredPackageRepositoriesWithExistingPackageSourcesHelper ();
		}

		void CreatePackageSourcesHelper ()
		{
			packageSourcesHelper = new OneRegisteredPackageSourceHelper ();
		}

		void CreateRegisteredPackageRepositoriesWithExistingPackageSourcesHelper ()
		{
			fakeRepositoryCache = new FakePackageRepositoryFactory ();
			registeredRepositories = new RegisteredPackageRepositories (fakeRepositoryCache, packageSourcesHelper.Options);	
		}

		[Test]
		public void RecentPackageRepository_PropertyAccessed_ReturnsRecentPackageRepositoryFromCache ()
		{
			CreateRegisteredPackageRepositories ();
			IRecentPackageRepository recentRepository = registeredRepositories.RecentPackageRepository;
			FakeRecentPackageRepository expectedRepository = fakeRepositoryCache.FakeRecentPackageRepository;

			Assert.AreEqual (expectedRepository, recentRepository);
		}

		[Test]
		public void CreateRepository_PackageSourceSpecified_CreatesRepositoryFromCache ()
		{
			CreateRegisteredPackageRepositories ();
			IPackageRepository repository = registeredRepositories.CreateRepository (new PackageSource ("a"));
			FakePackageRepository expectedRepository = fakeRepositoryCache.FakePackageRepository;

			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void CreateRepository_PackageSourceSpecified_PackageSourcePassedToCache ()
		{
			CreateRegisteredPackageRepositories ();
			var source = new PackageSource ("Test");
			registeredRepositories.CreateRepository (source);
			string actualSource = fakeRepositoryCache.FirstPackageSourcePassedToCreateRepository;

			Assert.AreEqual ("Test", actualSource);
		}

		[Test]
		public void CreateAggregateRepository_MethodCalled_ReturnsAggregateRepositoryCreatedFromCache ()
		{
			CreateRegisteredPackageRepositories ();
			IPackageRepository repository = registeredRepositories.CreateAggregateRepository ();
			FakePackageRepository expectedRepository = fakeRepositoryCache.FakeAggregateRepository;

			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void HasMultiplePackageSources_OnePackageSource_ReturnsFalse ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddOnePackageSource ();

			bool result = registeredRepositories.HasMultiplePackageSources;

			Assert.IsFalse (result);
		}

		[Test]
		public void HasMultiplePackageSources_TwoPackageSources_ReturnsTrue ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddTwoPackageSources ();

			bool result = registeredRepositories.HasMultiplePackageSources;

			Assert.IsTrue (result);
		}

		[Test]
		public void HasMultiplePackageSources_TwoPackageSourcesButOneIsDisabled_ReturnsFalse ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddTwoPackageSources ();
			packageSourcesHelper.RegisteredPackageSources [0].IsEnabled = false;

			bool result = registeredRepositories.HasMultiplePackageSources;

			Assert.IsFalse (result);
		}

		[Test]
		public void ActivePackageSource_TwoPackageSources_ByDefaultReturnsFirstPackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddTwoPackageSources ();

			PackageSource expectedPackageSource = packageSourcesHelper.RegisteredPackageSources [0];
			PackageSource packageSource = registeredRepositories.ActivePackageSource;

			Assert.AreEqual (expectedPackageSource, packageSource);
		}

		[Test]
		public void ActivePackageSource_ChangedToSecondRegisteredPackageSources_ReturnsSecondPackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddTwoPackageSources ();

			PackageSource expectedPackageSource = packageSourcesHelper.RegisteredPackageSources [1];
			registeredRepositories.ActivePackageSource = expectedPackageSource;
			PackageSource packageSource = registeredRepositories.ActivePackageSource;

			Assert.AreEqual (expectedPackageSource, packageSource);
		}

		[Test]
		public void ActivePackageSource_ChangedToNonNullPackageSource_SavedInOptions ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.Options.ActivePackageSource = null;
			var packageSource = new PackageSource ("http://source-url", "Test");
			packageSourcesHelper.Options.PackageSources.Add (packageSource);

			registeredRepositories.ActivePackageSource = packageSource;

			PackageSource actualPackageSource = packageSourcesHelper.Options.ActivePackageSource;

			Assert.AreEqual (packageSource, actualPackageSource);
		}

		[Test]
		public void ActivePackageSource_ActivePackageSourceNonNullInOptionsBeforeInstanceCreated_ActivePackageSourceReadFromOptions ()
		{
			CreatePackageSourcesHelper ();
			var packageSource = new PackageSource ("http://source-url", "Test");
			packageSourcesHelper.Options.PackageSources.Add (packageSource);
			packageSourcesHelper.Options.ActivePackageSource = packageSource;
			CreateRegisteredPackageRepositoriesWithExistingPackageSourcesHelper ();

			PackageSource actualPackageSource = registeredRepositories.ActivePackageSource;

			Assert.AreEqual (packageSource, actualPackageSource);
		}

		[Test]
		public void ActiveRepository_OneRegisteredSource_RepositoryCreatedFromRegisteredSource ()
		{
			CreateRegisteredPackageRepositories ();
			IPackageRepository activeRepository = registeredRepositories.ActiveRepository;

			string actualPackageSource = fakeRepositoryCache.FirstPackageSourcePassedToCreateRepository;
			PackageSource expectedPackageSource = packageSourcesHelper.PackageSource;

			Assert.AreEqual (expectedPackageSource.Source, actualPackageSource);
		}

		[Test]
		public void ActiveRepository_CalledTwice_RepositoryCreatedOnce ()
		{
			CreateRegisteredPackageRepositories ();
			IPackageRepository activeRepository = registeredRepositories.ActiveRepository;
			activeRepository = registeredRepositories.ActiveRepository;

			int count = fakeRepositoryCache.PackageSourcesPassedToCreateRepository.Count;

			Assert.AreEqual (1, count);
		}

		[Test]
		public void ActiveRepository_OneRegisteredSource_ReturnsPackageCreatedFromCache ()
		{
			CreateRegisteredPackageRepositories ();
			IPackageRepository activeRepository = registeredRepositories.ActiveRepository;

			IPackageRepository expectedRepository = fakeRepositoryCache.FakePackageRepository;

			Assert.AreEqual (expectedRepository, activeRepository);
		}

		[Test]
		public void ActivePackageRepository_ActivePackageSourceChangedToSecondRegisteredPackageSource_CreatesRepositoryUsingSecondPackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddTwoPackageSources ();

			PackageSource expectedPackageSource = packageSourcesHelper.Options.PackageSources [1];
			registeredRepositories.ActivePackageSource = expectedPackageSource;

			IPackageRepository repository = registeredRepositories.ActiveRepository;
			string packageSource = fakeRepositoryCache.FirstPackageSourcePassedToCreateRepository;

			Assert.AreEqual (expectedPackageSource.Source, packageSource);
		}

		[Test]
		public void ActiveRepository_ActivePackageSourceChangedAfterActivePackageRepositoryCreated_CreatesNewRepositoryUsingActivePackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddTwoPackageSources ();

			IPackageRepository initialRepository = registeredRepositories.ActiveRepository;
			fakeRepositoryCache.PackageSourcesPassedToCreateRepository.Clear ();

			PackageSource expectedPackageSource = packageSourcesHelper.Options.PackageSources [1];
			registeredRepositories.ActivePackageSource = expectedPackageSource;

			IPackageRepository repository = registeredRepositories.ActiveRepository;
			string packageSource = fakeRepositoryCache.FirstPackageSourcePassedToCreateRepository;

			Assert.AreEqual (expectedPackageSource.Source, packageSource);
		}

		[Test]
		public void ActiveRepository_ActivePackageSourceSetToSameValueAfterActivePackageRepositoryCreated_NewRepositoryNotCreated ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddOnePackageSource ();

			IPackageRepository initialRepository = registeredRepositories.ActiveRepository;
			fakeRepositoryCache.PackageSourcesPassedToCreateRepository.Clear ();

			PackageSource expectedPackageSource = packageSourcesHelper.Options.PackageSources [0];
			registeredRepositories.ActivePackageSource = expectedPackageSource;

			IPackageRepository repository = registeredRepositories.ActiveRepository;

			int count = fakeRepositoryCache.PackageSourcesPassedToCreateRepository.Count;

			Assert.AreEqual (0, count);
		}

		[Test]
		public void PackageSources_OnePackageSourceInOptions_ReturnsOnePackageSource ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddOnePackageSource ();

			RegisteredPackageSources packageSources = registeredRepositories.PackageSources;
			RegisteredPackageSources expectedPackageSources = packageSourcesHelper.Options.PackageSources;

			Assert.AreEqual (expectedPackageSources, packageSources);
		}

		[Test]
		public void ActivePackageRepository_ActivePackageSourceIsAggregate_ReturnsAggregatePackageRepository ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.AddTwoPackageSources ();

			registeredRepositories.ActivePackageSource = RegisteredPackageSourceSettings.AggregatePackageSource;

			IPackageRepository	repository = registeredRepositories.ActiveRepository;
			FakePackageRepository expectedRepository = fakeRepositoryCache.FakeAggregateRepository;

			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void ActivePackageSource_AllPackageSourcesCleared_ReturnsNullAndDoesNotThrowArgumentOutOfRangeException ()
		{
			CreateRegisteredPackageRepositories ();
			packageSourcesHelper.Options.ActivePackageSource = null;
			registeredRepositories.PackageSources.Clear ();

			PackageSource activePackageSource = registeredRepositories.ActivePackageSource;

			Assert.IsNull (activePackageSource);
		}
	}
}

