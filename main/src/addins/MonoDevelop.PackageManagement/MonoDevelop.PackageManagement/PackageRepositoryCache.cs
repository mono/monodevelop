// 
// PackageRepositoryCache.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012-2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MonoDevelop.PackageManagement;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class PackageRepositoryCache : IPackageRepositoryCache, IPackageRepositoryFactoryEvents
	{
		ISharpDevelopPackageRepositoryFactory factory;
		RegisteredPackageSources packageSources;
		PackageManagementOptions options;
		IList<RecentPackageInfo> recentPackages;
		IRecentPackageRepository recentPackageRepository;
		IPackageRepository machineCache;
		ConcurrentDictionary<string, IPackageRepository> repositories =
			new ConcurrentDictionary<string, IPackageRepository>();
		
		public PackageRepositoryCache (
			PackageManagementOptions options,
			IPackageRepository machineCache,
			ISharpDevelopPackageRepositoryFactory factory)
		{
			this.options = options;
			this.machineCache = machineCache;
			this.factory = factory;
			this.recentPackages = options.RecentPackages;
		}

		public PackageRepositoryCache (
			PackageManagementOptions options,
			ISharpDevelopPackageRepositoryFactory factory)
			: this (
				options,
				MachineCache.Default,
				factory)
		{
		}
		
		public PackageRepositoryCache (PackageManagementOptions options)
			: this(
				options,
				new SharpDevelopPackageRepositoryFactory ())
		{
		}

		public PackageRepositoryCache (
			RegisteredPackageSources packageSources,
			IList<RecentPackageInfo> recentPackages)
		{
			this.factory = new SharpDevelopPackageRepositoryFactory ();
			this.recentPackages = recentPackages;
			this.packageSources = packageSources;
		}

		public event EventHandler<PackageRepositoryFactoryEventArgs> RepositoryCreated;
		
		public IPackageRepository CreateRepository(string packageSource)
		{
			IPackageRepository repository = GetExistingRepository(packageSource);
			if (repository != null) {
				return repository;
			}
			return CreateNewCachedRepository(packageSource);
		}
		
		IPackageRepository GetExistingRepository(string packageSource)
		{
			IPackageRepository repository = null;
			if (repositories.TryGetValue(packageSource, out repository)) {
				return repository;
			}
			return null;
		}
		
		IPackageRepository CreateNewCachedRepository(string packageSource)
		{
			IPackageRepository repository = factory.CreateRepository(packageSource);
			repositories.TryAdd(packageSource, repository);
			
			OnPackageRepositoryCreated(repository);
			
			return repository;
		}
		
		void OnPackageRepositoryCreated(IPackageRepository repository)
		{
			if (RepositoryCreated != null) {
				RepositoryCreated(this, new PackageRepositoryFactoryEventArgs(repository));
			}
		}
		
		public ISharedPackageRepository CreateSharedRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem, IFileSystem configSettingsFileSystem)
		{
			return factory.CreateSharedRepository(pathResolver, fileSystem, configSettingsFileSystem);
		}
		
		public IPackageRepository CreateAggregateRepository()
		{
			IEnumerable<IPackageRepository> allRepositories = CreateAllEnabledRepositories();
			return CreateAggregateRepository(allRepositories);
		}
		
		IEnumerable<IPackageRepository> CreateAllEnabledRepositories()
		{
			foreach (PackageSource source in PackageSources.GetEnabledPackageSources ()) {
				yield return CreateRepositoryIgnoringFailures (source.Source);
			}
		}

		RegisteredPackageSources PackageSources {
			get {
				if (packageSources != null) {
					return packageSources;
				}
				return options.PackageSources;
			}
		}
		
		public IPackageRepository CreateAggregateRepository(IEnumerable<IPackageRepository> repositories)
		{
			return factory.CreateAggregateRepository(repositories);
		}
		
		public IRecentPackageRepository RecentPackageRepository {
			get {
				CreateRecentPackageRepository();
				return recentPackageRepository;
			}
		}
		
		void CreateRecentPackageRepository()
		{
			if (recentPackageRepository == null) {
				CreateRecentPackageRepository(recentPackages, NuGet.MachineCache.Default);
			}
		}
		
		public IRecentPackageRepository CreateRecentPackageRepository(
			IList<RecentPackageInfo> recentPackages,
			IPackageRepository aggregateRepository)
		{
			if (recentPackageRepository == null) {
				recentPackageRepository = factory.CreateRecentPackageRepository(recentPackages, aggregateRepository);
			}
			return recentPackageRepository;
		}

		public IPackageRepository CreateAggregateWithPriorityMachineCacheRepository ()
		{
			return new PriorityPackageRepository (machineCache, CreateAggregateRepository ());
		}

		IPackageRepository CreateRepositoryIgnoringFailures (string packageSource)
		{
			try {
				return CreateRepository (packageSource);
			} catch (Exception ex) {
				// Deliberately caching the failing package source so the
				// AggregateRepository only reports its failure once.
				var repository = new FailingPackageRepository (packageSource, ex);
				repositories.TryAdd(packageSource, repository);
				return repository;
			}
		}
	}
}
