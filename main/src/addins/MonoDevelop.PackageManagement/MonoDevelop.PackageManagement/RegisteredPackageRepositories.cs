// 
// RegisteredPackageRepositories.cs
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
using System.Linq;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class RegisteredPackageRepositories : IRegisteredPackageRepositories
	{
		IPackageRepositoryCache repositoryCache;
		PackageManagementOptions options;
		PackageSource activePackageSource;
		IPackageRepository activePackageRepository;
		
		public RegisteredPackageRepositories(
			IPackageRepositoryCache repositoryCache,
			PackageManagementOptions options)
		{
			this.repositoryCache = repositoryCache;
			this.options = options;
		}
		
		public IRecentPackageRepository RecentPackageRepository {
			get { return repositoryCache.RecentPackageRepository; }
		}
		
		public IPackageRepository CreateRepository(PackageSource source)
		{
			return repositoryCache.CreateRepository(source.Source);
		}
		
		public IPackageRepository CreateAggregateRepository()
		{
			return repositoryCache.CreateAggregateRepository();
		}
		
		public RegisteredPackageSources PackageSources {
			get { return options.PackageSources; }
		}
		
		public bool HasMultiplePackageSources {
			get { return PackageSources.HasMultipleEnabledPackageSources; }
		}
		
		public PackageSource ActivePackageSource {
			get {
				activePackageSource = options.ActivePackageSource;
				if (activePackageSource == null) {
					List<PackageSource> enabledPackageSources = 
						options.PackageSources.GetEnabledPackageSources ().ToList ();
					if (enabledPackageSources.Any ()) {
						ActivePackageSource = enabledPackageSources [0];
					}
				}
				return activePackageSource;
			}
			set {
				if (activePackageSource != value) {
					activePackageSource = value;
					options.ActivePackageSource = value;
					activePackageRepository = null;
				}
			}
		}
		
		public IPackageRepository ActiveRepository {
			get {
				if (activePackageRepository == null) {
					CreateActiveRepository();
				}
				return activePackageRepository;
			}
		}
		
		void CreateActiveRepository()
		{
			if (ActivePackageSource == null)
				return;

			if (ActivePackageSource.IsAggregate()) {
				activePackageRepository = CreateAggregateRepository();
			} else {
				activePackageRepository = repositoryCache.CreateRepository(ActivePackageSource.Source);
			}
		}

		public void UpdatePackageSources (IEnumerable<PackageSource> updatedPackageSources)
		{
			List<PackageSource> packageSourcesBackup = PackageSources.ToList ();

			try {
				PackageSources.Clear ();
				foreach (PackageSource updatedPackageSource in updatedPackageSources) {
					PackageSources.Add (updatedPackageSource);
				}

				UpdateActivePackageSource ();
				UpdateActivePackageRepository ();
			} catch (Exception) {
				PackageSources.AddRange (packageSourcesBackup);
				UpdateActivePackageSource ();

				throw;
			}
		}

		void UpdateActivePackageSource ()
		{
			if (activePackageSource == null)
				return;

			if (activePackageSource.IsAggregate ()) {
				if (!HasMultiplePackageSources) {
					ActivePackageSource = null;
				}
			} else {
				PackageSource matchedPackageSource = PackageSources
					.GetEnabledPackageSources ()
					.FirstOrDefault (packageSource => packageSource.Equals (activePackageSource));

				if (matchedPackageSource == null) {
					ActivePackageSource = null;
				}
			}
		}

		void UpdateActivePackageRepository ()
		{
			if (activePackageSource == null)
				return;

			if (activePackageSource.IsAggregate ()) {
				// Force recreation of AggregateRepository to reset any
				// failing package repositories.
				activePackageRepository = null;
			}
		}
	}
}
