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

using MonoDevelop.PackageManagement;
using NuGet;
using MonoDevelop.Core;

namespace ICSharpCode.PackageManagement
{
	public class AvailablePackagesViewModel : PackagesViewModel
	{
		ISolutionPackageRepository solutionPackageRepository;
		IPackageRepository repository;
		string errorMessage;
		IRecentPackageRepository recentPackageRepository;
		
		public AvailablePackagesViewModel (
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredPackageRepositories,
			IRecentPackageRepository recentPackageRepository,
			IPackageViewModelFactory packageViewModelFactory,
			ITaskFactory taskFactory)
			: base(registeredPackageRepositories, packageViewModelFactory, taskFactory)
		{
			this.solutionPackageRepository = solution.GetRepository ();
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

		protected override IQueryable<IPackage> GetPackages (PackageSearchCriteria search)
		{
			if (repository == null) {
				throw new ApplicationException (errorMessage);
			}

			if (search.IsPackageVersionSearch) {
				return repository
					.FindPackagesById (search.PackageId)
					.Where (package => IncludePrerelease || package.IsReleaseVersion ())
					.AsQueryable ();
			}

			if (IncludePrerelease) {
				return repository
					.Search (search.SearchText, new string[0], IncludePrerelease)
					.Where (package => package.IsAbsoluteLatestVersion);
			}
			return repository
				.Search (search.SearchText, new string[0], IncludePrerelease)
				.Where (package => package.IsLatestVersion);
		}
		
		/// <summary>
		/// Order packages by most downloaded first.
		/// </summary>
		protected override IQueryable<IPackage> OrderPackages (IQueryable<IPackage> packages, PackageSearchCriteria search)
		{
			if (search.IsPackageVersionSearch) {
				return packages.OrderByDescending (package => package.Version);
			}

			if (search.SearchText != null) {
				// Order by relevance for searches.
				return packages;
			}
			return packages.OrderByDescending(package => package.DownloadCount);
		}
		
		protected override IEnumerable<IPackage> GetFilteredPackagesBeforePagingResults (IQueryable<IPackage> allPackages, PackageSearchCriteria search)
		{
			if (search.IsPackageVersionSearch) {
				return base.GetFilteredPackagesBeforePagingResults (allPackages, search)
					.Where (package => search.IsVersionMatch (package.Version));
			}

			if (IncludePrerelease) {
				return base.GetFilteredPackagesBeforePagingResults(allPackages, search)
					.DistinctLast<IPackage>(PackageEqualityComparer.Id);
			}
			return base.GetFilteredPackagesBeforePagingResults(allPackages, search)
				.Where(package => package.IsReleaseVersion())
				.DistinctLast<IPackage>(PackageEqualityComparer.Id);
		}

		/// <summary>
		/// Package prioritization:
		/// 
		/// Recent packages first.
		/// Packages in solution.
		/// Packages from active package source.
		/// </summary>
		protected override IEnumerable<IPackage> PrioritizePackages (IEnumerable<IPackage> packages, PackageSearchCriteria search)
		{
			List<IPackage> prioritizedPackages = GetPrioritizedPackages (search).ToList ();

			foreach (IPackage package in prioritizedPackages) {
				yield return package;
			}

			foreach (IPackage package in packages) {
				if (!prioritizedPackages.Contains (package, PackageEqualityComparer.IdAndVersion)) {
					yield return package;
				}
			}
		}

		IEnumerable<IPackage> GetPrioritizedPackages (PackageSearchCriteria search)
		{
			if (search.IsPackageVersionSearch) {
				yield break;
			}

			List<IPackage> prioritizedPackages = GetRecentPackages (search).ToList ();

			if (PackageViewModels.Count == 0) {
				foreach (IPackage package in prioritizedPackages) {
					yield return package;
				}

				foreach (IPackage package in GetSolutionPackages (search)) {
					if (!prioritizedPackages.Contains (package, PackageEqualityComparer.IdAndVersion)) {
						prioritizedPackages.Add (package);
						yield return package;
					}
				}
			}
		}

		IEnumerable<IPackage> GetRecentPackages (PackageSearchCriteria search)
		{
			return recentPackageRepository.Search (search.SearchText, IncludePrerelease);
		}

		IEnumerable<IPackage> GetSolutionPackages (PackageSearchCriteria search)
		{
			try {
				return solutionPackageRepository
					.GetPackages ()
					.Find (search.SearchText)
					.FilterByPrerelease (IncludePrerelease)
					.ToList ();
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to get solution packages so these will not be shown.", ex);
				return Enumerable.Empty<IPackage> ();
			}
		}

		protected override PackageViewModel CreatePackageViewModel (IPackage package, PackageSearchCriteria search)
		{
			PackageViewModel viewModel = base.CreatePackageViewModel (package, search);
			viewModel.ShowVersionInsteadOfDownloadCount = search.IsPackageVersionSearch;
			return viewModel;
		}

		protected override string GetWarningMessage ()
		{
			var aggregateRepository = repository as MonoDevelopAggregateRepository;
			if (aggregateRepository != null) {
				if (aggregateRepository.AllFailed ()) {
					return GetAllPackageSourcesCouldNotBeReachedErrorMessage (aggregateRepository);
				} else if (aggregateRepository.AnyFailures ()) {
					return GetSomePackageSourcesCouldNotBeReachedErrorMessage (aggregateRepository);
				}
			}
			return String.Empty;
		}

		string GetAllPackageSourcesCouldNotBeReachedErrorMessage (MonoDevelopAggregateRepository repository)
		{
			string message = GettextCatalog.GetString ("All package sources could not be reached.");
			return new AggregateExceptionErrorMessage (message, repository.GetAggregateException ()).ToString ();
		}

		string GetSomePackageSourcesCouldNotBeReachedErrorMessage (MonoDevelopAggregateRepository repository)
		{
			string message = GettextCatalog.GetString ("Some package sources could not be reached.");
			return new AggregateExceptionErrorMessage (message, repository.GetAggregateException ()).ToString ();
		}
	}
}
