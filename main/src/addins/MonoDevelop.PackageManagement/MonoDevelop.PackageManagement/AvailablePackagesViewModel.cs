// 
// AvailablePackagesViewModel.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
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
	public class AvailablePackagesViewModel : PackagesViewModel
	{
		IPackageRepository repository;
		string errorMessage;
		IRecentPackageRepository recentPackageRepository;
		
		public AvailablePackagesViewModel(
			IRegisteredPackageRepositories registeredPackageRepositories,
			IRecentPackageRepository recentPackageRepository,
			IPackageViewModelFactory packageViewModelFactory,
			ITaskFactory taskFactory)
			: base(registeredPackageRepositories, packageViewModelFactory, taskFactory)
		{
			this.recentPackageRepository = recentPackageRepository;

			IsSearchable = true;
			ShowPackageSources = true;
			ShowPrerelease = true;
		}
		
		protected override void UpdateRepositoryBeforeReadPackagesTaskStarts()
		{
			try {
				repository = RegisteredPackageRepositories.ActiveRepository;
			} catch (Exception ex) {
				repository = null;
				errorMessage = ex.Message;
			}
		}
		
		protected override IQueryable<IPackage> GetPackages(string search)
		{
			if (repository == null) {
				throw new ApplicationException(errorMessage);
			}
			if (IncludePrerelease) {
				return repository
					.Search (search, new string[0], IncludePrerelease)
					.Where (package => package.IsAbsoluteLatestVersion);
			}
			return repository
				.Search (search, new string[0], IncludePrerelease)
				.Where (package => package.IsLatestVersion);
		}
		
		/// <summary>
		/// Order packages by most downloaded first.
		/// </summary>
		protected override IQueryable<IPackage> OrderPackages(IQueryable<IPackage> packages)
		{
			if (GetSearchCriteria () != null) {
				// Order by relevance for searches.
				return packages;
			}
			return packages.OrderByDescending(package => package.DownloadCount);
		}
		
		protected override IEnumerable<IPackage> GetFilteredPackagesBeforePagingResults(IQueryable<IPackage> allPackages)
		{
			if (IncludePrerelease) {
				return base.GetFilteredPackagesBeforePagingResults(allPackages)
					.DistinctLast<IPackage>(PackageEqualityComparer.Id);
			}
			return base.GetFilteredPackagesBeforePagingResults(allPackages)
				.Where(package => package.IsReleaseVersion())
				.DistinctLast<IPackage>(PackageEqualityComparer.Id);
		}

		protected override IEnumerable<IPackage> PrioritizePackages (IEnumerable<IPackage> packages, string searchCriteria)
		{
			List<IPackage> recentPackages = GetRecentPackages (searchCriteria).ToList ();

			if (PackageViewModels.Count == 0) {
				foreach (IPackage package in recentPackages) {
					yield return package;
				}
			}

			foreach (IPackage package in packages) {
				if (!recentPackages.Contains (package, PackageEqualityComparer.IdAndVersion)) {
					yield return package;
				}
			}
		}

		IEnumerable<IPackage> GetRecentPackages (string searchCriteria)
		{
			return recentPackageRepository.Search (searchCriteria, IncludePrerelease);
		}
	}
}
