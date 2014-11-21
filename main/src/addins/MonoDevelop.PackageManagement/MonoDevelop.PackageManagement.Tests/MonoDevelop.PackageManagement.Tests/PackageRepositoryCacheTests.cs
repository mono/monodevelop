//
// PackageRepositoryCacheTests.cs
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
	public class PackageRepositoryCacheTests
	{
		PackageRepositoryCache cache;
		FakePackageRepositoryFactory fakePackageRepositoryFactory;
		PackageSource nuGetPackageSource;
		OneRegisteredPackageSourceHelper packageSourcesHelper;
		RecentPackageInfo[] recentPackagesPassedToCreateRecentPackageRepository;
		FakePackageRepository fakeAggregateRepositoryPassedToCreateRecentPackageRepository;
		FakePackageRepository machineCache;

		void CreateCache ()
		{
			CreatePackageSources ();
			CreateCacheUsingPackageSources ();
		}

		void CreatePackageSources ()
		{
			packageSourcesHelper = new OneRegisteredPackageSourceHelper ();
		}

		void CreateCacheUsingPackageSources ()
		{
			fakePackageRepositoryFactory = new FakePackageRepositoryFactory ();
			CreateCacheUsingPackageSources (fakePackageRepositoryFactory);
		}

		void CreateCacheUsingPackageSources (ISharpDevelopPackageRepositoryFactory repositoryFactory)
		{
			nuGetPackageSource = new PackageSource ("http://nuget.org", "NuGet");
			machineCache = new FakePackageRepository ();
			cache = new PackageRepositoryCache (packageSourcesHelper.Options, machineCache, repositoryFactory);
		}

		FakePackageRepository AddFakePackageRepositoryForPackageSource (string source)
		{
			return fakePackageRepositoryFactory.AddFakePackageRepositoryForPackageSource (source);
		}

		IPackageRepository CreateRecentPackageRepositoryPassingAggregateRepository ()
		{
			recentPackagesPassedToCreateRecentPackageRepository = new RecentPackageInfo[0];
			fakeAggregateRepositoryPassedToCreateRecentPackageRepository = new FakePackageRepository ();

			return cache.CreateRecentPackageRepository (
				recentPackagesPassedToCreateRecentPackageRepository,
				fakeAggregateRepositoryPassedToCreateRecentPackageRepository);
		}

		RecentPackageInfo AddOneRecentPackage ()
		{
			var recentPackage = new RecentPackageInfo ("Id", new SemanticVersion ("1.0"));
			packageSourcesHelper.Options.RecentPackages.Add (recentPackage);
			return recentPackage;
		}

		[Test]
		public void CreateRepository_CacheCastToISharpDevelopPackageRepositoryFactory_CreatesPackageRepositoryUsingPackageRepositoryFactoryPassedInConstructor ()
		{
			CreateCache ();
			var factory = cache as ISharpDevelopPackageRepositoryFactory;
			IPackageRepository repository = factory.CreateRepository (nuGetPackageSource.Source);

			Assert.AreEqual (fakePackageRepositoryFactory.FakePackageRepository, repository);
		}

		[Test]
		public void CreateRepository_PackageSourcePassed_PackageSourceUsedToCreateRepository ()
		{
			CreateCache ();
			cache.CreateRepository (nuGetPackageSource.Source);

			string actualPackageSource = fakePackageRepositoryFactory.FirstPackageSourcePassedToCreateRepository;
			Assert.AreEqual (nuGetPackageSource.Source, actualPackageSource);
		}

		[Test]
		public void CreateRepository_RepositoryAlreadyCreatedForPackageSource_NoRepositoryCreated ()
		{
			CreateCache ();
			cache.CreateRepository (nuGetPackageSource.Source);
			fakePackageRepositoryFactory.PackageSourcesPassedToCreateRepository.Clear ();

			cache.CreateRepository (nuGetPackageSource.Source);

			Assert.AreEqual (0, fakePackageRepositoryFactory.PackageSourcesPassedToCreateRepository.Count);
		}

		[Test]
		public void CreateRepository_RepositoryAlreadyCreatedForPackageSource_RepositoryOriginallyCreatedIsReturned ()
		{
			CreateCache ();
			IPackageRepository originallyCreatedRepository = cache.CreateRepository (nuGetPackageSource.Source);
			fakePackageRepositoryFactory.PackageSourcesPassedToCreateRepository.Clear ();

			IPackageRepository repository = cache.CreateRepository (nuGetPackageSource.Source);

			Assert.AreSame (originallyCreatedRepository, repository);
		}

		[Test]
		public void CreateSharedRepository_MethodCalled_ReturnsSharedPackageRepository ()
		{
			CreateCache ();
			ISharedPackageRepository repository = cache.CreateSharedRepository (null, null, null);
			Assert.IsNotNull (repository);
		}

		[Test]
		public void CreatedSharedRepository_PathResolverPassed_PathResolverUsedToCreatedSharedRepository ()
		{
			CreateCache ();
			FakePackagePathResolver resolver = new FakePackagePathResolver ();
			cache.CreateSharedRepository (resolver, null, null);

			Assert.AreEqual (resolver, fakePackageRepositoryFactory.PathResolverPassedToCreateSharedRepository);
		}

		[Test]
		public void CreatedSharedRepository_FileSystemPassed_FileSystemUsedToCreatedSharedRepository ()
		{
			CreateCache ();
			FakeFileSystem fileSystem = new FakeFileSystem ();
			cache.CreateSharedRepository (null, fileSystem, null);

			Assert.AreEqual (fileSystem, fakePackageRepositoryFactory.FileSystemPassedToCreateSharedRepository);
		}

		[Test]
		public void CreatedSharedRepository_ConfigSettingsFileSystemPassed_FileSystemUsedToCreatedSharedRepository ()
		{
			CreateCache ();
			FakeFileSystem fileSystem = new FakeFileSystem ();
			cache.CreateSharedRepository (null, null, fileSystem);

			Assert.AreEqual (fileSystem, fakePackageRepositoryFactory.ConfigSettingsFileSystemPassedToCreateSharedRepository);
		}

		[Test]
		public void CreateAggregatePackageRepository_TwoRegisteredPackageRepositories_ReturnsAggregateRepositoryFromFactory ()
		{
			CreatePackageSources ();
			packageSourcesHelper.AddTwoPackageSources ("Source1", "Source2");
			CreateCacheUsingPackageSources ();

			IPackageRepository aggregateRepository = cache.CreateAggregateRepository ();
			FakePackageRepository expectedRepository = fakePackageRepositoryFactory.FakeAggregateRepository;

			Assert.AreEqual (expectedRepository, aggregateRepository);
		}

		[Test]
		public void CreateAggregatePackageRepository_TwoRegisteredPackageSourcesButOneDisabled_ReturnsAggregateRepositoryCreatedWithOnlyEnabledPackageSource ()
		{
			CreatePackageSources ();
			packageSourcesHelper.AddTwoPackageSources ("Source1", "Source2");
			packageSourcesHelper.RegisteredPackageSources [0].IsEnabled = false;
			CreateCacheUsingPackageSources ();
			AddFakePackageRepositoryForPackageSource ("Source1");
			FakePackageRepository repository2 = AddFakePackageRepositoryForPackageSource ("Source2");
			var expectedRepositories = new FakePackageRepository[] {
				repository2
			};

			cache.CreateAggregateRepository ();

			IEnumerable<IPackageRepository> repositoriesUsedToCreateAggregateRepository = 
				fakePackageRepositoryFactory.RepositoriesPassedToCreateAggregateRepository;

			var actualRepositoriesAsList = new List<IPackageRepository> (repositoriesUsedToCreateAggregateRepository);
			IPackageRepository[] actualRepositories = actualRepositoriesAsList.ToArray ();

			CollectionAssert.AreEqual (expectedRepositories, actualRepositories);
		}

		[Test]
		public void CreateAggregatePackageRepository_TwoRegisteredPackageRepositories_AllRegisteredRepositoriesUsedToCreateAggregateRepositoryFromFactory ()
		{
			CreatePackageSources ();
			packageSourcesHelper.AddTwoPackageSources ("Source1", "Source2");
			CreateCacheUsingPackageSources ();

			FakePackageRepository repository1 = AddFakePackageRepositoryForPackageSource ("Source1");
			FakePackageRepository repository2 = AddFakePackageRepositoryForPackageSource ("Source2");
			var expectedRepositories = new FakePackageRepository[] {
				repository1,
				repository2
			};

			cache.CreateAggregateRepository ();

			IEnumerable<IPackageRepository> repositoriesUsedToCreateAggregateRepository = 
				fakePackageRepositoryFactory.RepositoriesPassedToCreateAggregateRepository;

			var actualRepositoriesAsList = new List<IPackageRepository> (repositoriesUsedToCreateAggregateRepository);
			IPackageRepository[] actualRepositories = actualRepositoriesAsList.ToArray ();

			CollectionAssert.AreEqual (expectedRepositories, actualRepositories);
		}

		[Test]
		public void CreateAggregatePackageRepository_OnePackageRepositoryPassed_ReturnsAggregateRepositoryFromFactory ()
		{
			CreateCache ();

			var repositories = new FakePackageRepository[] {
				new FakePackageRepository ()
			};
			IPackageRepository aggregateRepository = cache.CreateAggregateRepository (repositories);

			FakePackageRepository expectedRepository = fakePackageRepositoryFactory.FakeAggregateRepository;

			Assert.AreEqual (expectedRepository, aggregateRepository);
		}

		[Test]
		public void CreateAggregatePackageRepository_OnePackageRepositoryPassed_RepositoryUsedToCreateAggregateRepository ()
		{
			CreateCache ();

			var repositories = new FakePackageRepository[] {
				new FakePackageRepository ()
			};
			cache.CreateAggregateRepository (repositories);

			IEnumerable<IPackageRepository> repositoriesUsedToCreateAggregateRepository = 
				fakePackageRepositoryFactory.RepositoriesPassedToCreateAggregateRepository;

			Assert.AreEqual (repositories, repositoriesUsedToCreateAggregateRepository);
		}

		[Test]
		public void RecentPackageRepository_NoRecentPackages_ReturnsRecentRepositoryCreatedByFactory ()
		{
			CreateCache ();
			IRecentPackageRepository repository = cache.RecentPackageRepository;
			FakeRecentPackageRepository expectedRepository = fakePackageRepositoryFactory.FakeRecentPackageRepository;

			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void RecentPackageRepository_NoRecentPackages_CreatedWithAggregateRepository ()
		{
			CreateCache ();
			IRecentPackageRepository repository = cache.RecentPackageRepository;

			IPackageRepository expectedRepository = MachineCache.Default;
			IPackageRepository actualRepository = fakePackageRepositoryFactory.AggregateRepositoryPassedToCreateRecentPackageRepository;

			Assert.AreEqual (expectedRepository, actualRepository);
		}

		[Test]
		public void RecentPackageRepository_OneRecentPackage_RecentPackageUsedToCreateRecentPackageRepository ()
		{
			CreateCache ();
			RecentPackageInfo recentPackage = AddOneRecentPackage ();

			IRecentPackageRepository repository = cache.RecentPackageRepository;

			IList<RecentPackageInfo> actualRecentPackages = fakePackageRepositoryFactory.RecentPackagesPassedToCreateRecentPackageRepository;

			var expectedRecentPackages = new RecentPackageInfo[] {
				recentPackage
			};

			Assert.AreEqual (expectedRecentPackages, actualRecentPackages);
		}

		[Test]
		public void RecentPackageRepository_PropertyAccessedTwice_AggregateRepositoryCreatedOnce ()
		{
			CreateCache ();
			IRecentPackageRepository repository = cache.RecentPackageRepository;
			fakePackageRepositoryFactory.RepositoriesPassedToCreateAggregateRepository = null;
			repository = cache.RecentPackageRepository;

			Assert.IsNull (fakePackageRepositoryFactory.RepositoriesPassedToCreateAggregateRepository);
		}

		[Test]
		public void CreateRecentPackageRepository_AggregateRepositoryPassedAndNoRecentPackagesPassed_UsesFactoryToCreateRepository ()
		{
			CreateCache ();
			IPackageRepository repository = CreateRecentPackageRepositoryPassingAggregateRepository ();

			FakeRecentPackageRepository expectedRepository = fakePackageRepositoryFactory.FakeRecentPackageRepository;

			Assert.AreEqual (expectedRepository, repository);
		}

		[Test]
		public void CreateRecentPackageRepository_AggregateRepositoryPassedAndNoRecentPackagesPassed_AggregateIsUsedToCreateRepository ()
		{
			CreateCache ();
			CreateRecentPackageRepositoryPassingAggregateRepository ();

			IPackageRepository actualRepository = fakePackageRepositoryFactory.AggregateRepositoryPassedToCreateRecentPackageRepository;

			Assert.AreEqual (fakeAggregateRepositoryPassedToCreateRecentPackageRepository, actualRepository);
		}

		[Test]
		public void CreateRecentPackageRepository_AggregateRepositoryPassedAndNoRecentPackagesPassed_RecentPackagesUsedToCreateRepository ()
		{
			CreateCache ();
			CreateRecentPackageRepositoryPassingAggregateRepository ();

			IList<RecentPackageInfo> recentPackages = fakePackageRepositoryFactory.RecentPackagesPassedToCreateRecentPackageRepository;

			Assert.AreEqual (recentPackagesPassedToCreateRecentPackageRepository, recentPackages);
		}

		[Test]
		public void CreateRecentPackageRepository_MethodCalledTwice_RecentPackageRepositoryCreatedOnce ()
		{
			CreateCache ();
			CreateRecentPackageRepositoryPassingAggregateRepository ();
			fakePackageRepositoryFactory.AggregateRepositoryPassedToCreateRecentPackageRepository = null;
			CreateRecentPackageRepositoryPassingAggregateRepository ();

			Assert.IsNull (fakePackageRepositoryFactory.AggregateRepositoryPassedToCreateRecentPackageRepository);
		}

		[Test]
		public void CreateRepository_NewRepositoryCreated_RepositoryCreatedEventFired ()
		{
			CreateCache ();
			PackageRepositoryFactoryEventArgs eventArgs = null;
			cache.RepositoryCreated += (sender, e) => eventArgs = e;

			cache.CreateRepository (nuGetPackageSource.Source);

			Assert.AreEqual (fakePackageRepositoryFactory.FakePackageRepository, eventArgs.Repository);
		}

		[Test]
		public void CreateRepository_RepositoryCreatedTwice_RepositoryCreatedEventIsNotFiredOnSecondCallToCreateRepository ()
		{
			CreateCache ();
			cache.CreateRepository (nuGetPackageSource.Source);
			PackageRepositoryFactoryEventArgs eventArgs = null;
			cache.RepositoryCreated += (sender, e) => eventArgs = e;

			cache.CreateRepository (nuGetPackageSource.Source);

			Assert.IsNull (eventArgs);
		}

		[Test]
		public void CreateAggregateRepository_SolutionClosedAndEnabledPackageSourcesChangedAfterCacheCreated_AggregateRepositoryContainsCorrectEnabledPackageRepositories ()
		{
			CreatePackageSources ();
			packageSourcesHelper.AddTwoPackageSources ("Source1", "Source2");
			CreateCacheUsingPackageSources ();
			FakePackageRepository source1Repo = AddFakePackageRepositoryForPackageSource ("Source1");
			FakePackageRepository source2Repo = AddFakePackageRepositoryForPackageSource ("Source2");
			fakePackageRepositoryFactory.CreateAggregrateRepositoryAction = (repositories) => {
				return new AggregateRepository (repositories);
			};
			var initialAggregateRepository = cache.CreateAggregateRepository () as AggregateRepository;
			var expectedInitialRepositories = new FakePackageRepository [] {
				source1Repo,
				source2Repo
			};
			List<IPackageRepository> actualInitialRepositories = initialAggregateRepository.Repositories.ToList ();
			packageSourcesHelper.Options.ProjectService.RaiseSolutionUnloadedEvent ();
			packageSourcesHelper.Options.PackageSources.Clear ();
			packageSourcesHelper.Options.PackageSources.Add (new PackageSource ("Source3"));
			FakePackageRepository source3Repo = AddFakePackageRepositoryForPackageSource ("Source3");
			var expectedRepositories = new FakePackageRepository [] {
				source3Repo
			};

			var aggregateRepository = cache.CreateAggregateRepository () as AggregateRepository;
			List<IPackageRepository> actualRepositories = aggregateRepository.Repositories.ToList ();

			CollectionAssert.AreEqual (expectedInitialRepositories, actualInitialRepositories);
			CollectionAssert.AreEqual (expectedRepositories, actualRepositories);
		}

		[Test]
		public void CreateAggregatePriorityRepository_NoAggregatePackageSources_ReturnsPriorityPackageRepositoryThatUsesMachineCache ()
		{
			CreateCache ();
			machineCache.AddFakePackageWithVersion ("MyPackage", "1.0");

			IPackageRepository repository = cache.CreateAggregateWithPriorityMachineCacheRepository ();
			bool exists = repository.Exists ("MyPackage", new SemanticVersion ("1.0"));

			Assert.IsInstanceOf<PriorityPackageRepository> (repository);
			Assert.IsTrue (exists);
		}

		[Test]
		public void CreateAggregatePriorityRepository_NoAggregatePackageSources_ReturnsPriorityPackageRepositoryThatUsesAggregateRepository ()
		{
			CreatePackageSources ();
			packageSourcesHelper.AddTwoPackageSources ("Source1", "Source2");
			CreateCacheUsingPackageSources ();
			fakePackageRepositoryFactory.FakeAggregateRepository.AddFakePackageWithVersion ("MyPackage", "1.0");

			IPackageRepository repository = cache.CreateAggregateWithPriorityMachineCacheRepository ();
			bool exists = repository.Exists ("MyPackage", new SemanticVersion ("1.0"));

			Assert.IsTrue (exists);
		}

		[Test]
		public void CreateAggregateRepository_OnePackageSourceHasInvalidUri_NoExceptionThrownWhenCreatingAggregateRepositoryAndSearchingForPackages ()
		{
			CreatePackageSources ();
			packageSourcesHelper.RegisteredPackageSources.Clear ();
			var invalidPackageSource = new PackageSource (String.Empty, "InvalidSource");
			packageSourcesHelper.RegisteredPackageSources.Add (invalidPackageSource);
			var factory = new SharpDevelopPackageRepositoryFactory ();
			CreateCacheUsingPackageSources (factory);
			IPackageRepository repository = cache.CreateAggregateRepository ();
			var aggregateRepository = (MonoDevelopAggregateRepository)repository;

			Assert.IsFalse (aggregateRepository.AnyFailures ());
			Assert.DoesNotThrow (() => repository.Search ("abc", false));
			Assert.IsTrue (aggregateRepository.AnyFailures ());
		}

		[Test]
		public void CreateAggregateRepository_OnePackageSourceHasInvalidUriAndSearchExecutedMultipleTimes_ExceptionThrownByPackageRepositoryIsOnlyRecordedOnce ()
		{
			CreatePackageSources ();
			packageSourcesHelper.RegisteredPackageSources.Clear ();
			var invalidPackageSource = new PackageSource (String.Empty, "InvalidSource");
			packageSourcesHelper.RegisteredPackageSources.Add (invalidPackageSource);
			var factory = new SharpDevelopPackageRepositoryFactory ();
			CreateCacheUsingPackageSources (factory);
			IPackageRepository repository = cache.CreateAggregateRepository ();
			var aggregateRepository = (MonoDevelopAggregateRepository)repository;

			repository.Search ("abc", false);
			repository.Search ("abc", false);
			repository.Search ("abc", false);

			Assert.IsTrue (aggregateRepository.AnyFailures ());
			Assert.AreEqual (1, aggregateRepository.GetAggregateException ().InnerExceptions.Count);
		}
	}
}

