// 
// ProjectTemplatePackageRepositoryCache.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
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
using System.Collections.Generic;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	/// <summary>
	/// Supports a configurable set of package repositories for project templates that can be
	/// different to the registered package repositories used with the Add Package Reference dialog.
	/// </summary>
	public class ProjectTemplatePackageRepositoryCache : IPackageRepositoryCache
	{
		IPackageRepositoryCache packageRepositoryCache;
		RegisteredProjectTemplatePackageSources registeredPackageSources;
		
		/// <summary>
		/// Creates a new instance of the ProjectTemplatePackageRepositoryCache.
		/// </summary>
		public ProjectTemplatePackageRepositoryCache(RegisteredProjectTemplatePackageSources registeredPackageSources)
		{
			this.packageRepositoryCache = new PackageRepositoryCache(registeredPackageSources.PackageSources, new List<RecentPackageInfo>());
			this.registeredPackageSources = registeredPackageSources;
		}
		
		public IRecentPackageRepository RecentPackageRepository {
			get { throw new NotImplementedException(); }
		}
		
		public IPackageRepository CreateAggregateRepository()
		{
			IEnumerable<IPackageRepository> repositories = GetRegisteredPackageRepositories();
			return CreateAggregateRepository(repositories);
		}
		
		IEnumerable<IPackageRepository> GetRegisteredPackageRepositories()
		{
			foreach (PackageSource packageSource in registeredPackageSources.PackageSources) {
				yield return CreateRepository(packageSource.Source);
			}
		}
		
		public ISharedPackageRepository CreateSharedRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem, IFileSystem configSettingsFileSystem)
		{
			throw new NotImplementedException();
		}
		
		public IRecentPackageRepository CreateRecentPackageRepository(IList<RecentPackageInfo> recentPackages, IPackageRepository aggregateRepository)
		{
			throw new NotImplementedException();
		}
		
		public IPackageRepository CreateAggregateRepository(IEnumerable<IPackageRepository> repositories)
		{
			return packageRepositoryCache.CreateAggregateRepository(repositories);
		}
		
		public IPackageRepository CreateRepository(string packageSource)
		{
			return packageRepositoryCache.CreateRepository(packageSource);
		}

		public IPackageRepository CreateAggregateWithPriorityMachineCacheRepository ()
		{
			return new PriorityPackageRepository (MachineCache.Default, CreateAggregateRepository ());
		}
	}
}
