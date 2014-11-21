//
// FakePackageRepositoryFactory.cs
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
using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakePackageRepositoryFactory : IPackageRepositoryCache
	{
		public List<string> PackageSourcesPassedToCreateRepository
			= new List<string> ();

		public string FirstPackageSourcePassedToCreateRepository {
			get { return PackageSourcesPassedToCreateRepository [0]; }
		}

		public FakePackageRepository FakePackageRepository = new FakePackageRepository ();

		public Dictionary<string, FakePackageRepository> FakePackageRepositories =
			new Dictionary<string, FakePackageRepository> ();

		public FakePackageRepositoryFactory ()
		{
			CreateAggregrateRepositoryAction = (repositories) => {
				return FakeAggregateRepository;
			};
		}

		public IPackageRepository CreateRepository (string packageSource)
		{
			PackageSourcesPassedToCreateRepository.Add (packageSource);

			FakePackageRepository repository = null;
			if (FakePackageRepositories.TryGetValue (packageSource, out repository)) {
				return repository;
			}

			return FakePackageRepository;
		}

		public IPackagePathResolver PathResolverPassedToCreateSharedRepository;
		public IFileSystem FileSystemPassedToCreateSharedRepository;
		public IFileSystem ConfigSettingsFileSystemPassedToCreateSharedRepository;
		public FakeSharedPackageRepository FakeSharedRepository = new FakeSharedPackageRepository ();

		public ISharedPackageRepository CreateSharedRepository (IPackagePathResolver pathResolver, IFileSystem fileSystem, IFileSystem configSettingsFileSystem)
		{
			PathResolverPassedToCreateSharedRepository = pathResolver;
			FileSystemPassedToCreateSharedRepository = fileSystem;
			ConfigSettingsFileSystemPassedToCreateSharedRepository = configSettingsFileSystem;
			return FakeSharedRepository;
		}

		public FakePackageRepository FakeAggregateRepository = new FakePackageRepository ();

		public IPackageRepository CreateAggregateRepository ()
		{
			return FakeAggregateRepository;
		}

		public FakeRecentPackageRepository FakeRecentPackageRepository = new FakeRecentPackageRepository ();
		public IList<RecentPackageInfo> RecentPackagesPassedToCreateRecentPackageRepository;
		public IPackageRepository AggregateRepositoryPassedToCreateRecentPackageRepository;

		public IRecentPackageRepository CreateRecentPackageRepository (
			IList<RecentPackageInfo> recentPackages,
			IPackageRepository aggregateRepository)
		{
			RecentPackagesPassedToCreateRecentPackageRepository = recentPackages;
			AggregateRepositoryPassedToCreateRecentPackageRepository = aggregateRepository;
			return FakeRecentPackageRepository;
		}

		public IEnumerable<IPackageRepository> RepositoriesPassedToCreateAggregateRepository;
		public Func<IEnumerable<IPackageRepository>, IPackageRepository> CreateAggregrateRepositoryAction;

		public IPackageRepository CreateAggregateRepository (IEnumerable<IPackageRepository> repositories)
		{
			RepositoriesPassedToCreateAggregateRepository = repositories;
			return CreateAggregrateRepositoryAction (repositories);
		}

		public FakePackageRepository AddFakePackageRepositoryForPackageSource (string source)
		{
			var repository = new FakePackageRepository ();
			FakePackageRepositories.Add (source, repository);
			return repository;
		}

		public IRecentPackageRepository RecentPackageRepository {
			get { return FakeRecentPackageRepository; }
		}

		public FakePackageRepository FakePriorityPackageRepository = new FakePackageRepository ();

		public IPackageRepository CreateAggregateWithPriorityMachineCacheRepository ()
		{
			return FakePriorityPackageRepository;
		}
	}
}

