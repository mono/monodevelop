// 
// PackagesViewModel.cs
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public abstract class PackagesViewModel : ViewModelBase<PackagesViewModel>, IDisposable, IPackageViewModelParent
	{
		Pages pages = new Pages();
		
		IRegisteredPackageRepositories registeredPackageRepositories;
		IPackageViewModelFactory packageViewModelFactory;
		ITaskFactory taskFactory;
		IEnumerable<IPackage> allPackages;
		ITask<PackagesForSelectedPageResult> task;
		bool includePrerelease;
		PackagesForSelectedPageQuery packagesForSelectedPageQuery;
		bool ignorePackageCheckedChanged;

		public PackagesViewModel(
			IRegisteredPackageRepositories registeredPackageRepositories,
			IPackageViewModelFactory packageViewModelFactory,
			ITaskFactory taskFactory)
		{
			this.registeredPackageRepositories = registeredPackageRepositories;
			this.packageViewModelFactory = packageViewModelFactory;
			this.taskFactory = taskFactory;
			
			PackageViewModels = new ObservableCollection<PackageViewModel>();
			CheckedPackageViewModels = new ObservableCollection <PackageViewModel> ();
			ErrorMessage = String.Empty;
			ClearPackagesOnPaging = true;

			CreateCommands();
		}
		
		void CreateCommands()
		{
			ShowNextPageCommand = new DelegateCommand(param => ShowNextPage());
			ShowPreviousPageCommand = new DelegateCommand(param => ShowPreviousPage());
			ShowPageCommand = new DelegateCommand(param => ExecuteShowPageCommand(param));
			SearchCommand = new DelegateCommand(param => Search());
			UpdateAllPackagesCommand = new DelegateCommand(param => UpdateAllPackages());
		}
		
		public ICommand ShowNextPageCommand { get; private set; }
		public ICommand ShowPreviousPageCommand { get; private set; }
		public ICommand ShowPageCommand { get; private set; }
		public ICommand SearchCommand { get; private set; }
		public ICommand UpdateAllPackagesCommand { get; private set; }
		
		public void Dispose()
		{
			OnDispose();
			CancelReadPackagesTask ();
			IsDisposed = true;
		}
		
		protected virtual void OnDispose()
		{
		}
		
		public bool IsDisposed { get; private set; }
		
		public bool HasError { get; private set; }
		public string ErrorMessage { get; private set; }
		
		public ObservableCollection<PackageViewModel> PackageViewModels { get; set; }
		
		public IRegisteredPackageRepositories RegisteredPackageRepositories {
			get { return registeredPackageRepositories; }
		}
		
		public bool IsReadingPackages { get; private set; }
		
		public void ReadPackages()
		{
			if (SelectedPackageSource == null) {
				return;
			}

			allPackages = null;
			pages.SelectedPageNumber = 1;
			IsLoadingNextPage = false;
			UpdateRepositoryBeforeReadPackagesTaskStarts();
			StartReadPackagesTask();
		}
		
		void StartReadPackagesTask(bool clearPackages = true)
		{
			IsReadingPackages = true;
			ClearError ();
			if (clearPackages) {
				ClearPackages ();
			}
			CancelReadPackagesTask();
			CreateReadPackagesTask();
			task.Start();
		}

		void ClearError ()
		{
			HasError = false;
			ErrorMessage = String.Empty;
		}
		
		protected virtual void UpdateRepositoryBeforeReadPackagesTaskStarts()
		{
		}
		
		void CancelReadPackagesTask()
		{
			if (task != null) {
				task.Cancel();
			}
		}
		
		void CreateReadPackagesTask()
		{
			var query = new PackagesForSelectedPageQuery (this, allPackages, SearchTerms);
			packagesForSelectedPageQuery = query;

			task = taskFactory.CreateTask(
				() => GetPackagesForSelectedPageResult(query),
				OnPackagesReadForSelectedPage);
		}
		
		PackagesForSelectedPageResult GetPackagesForSelectedPageResult(PackagesForSelectedPageQuery query)
		{
			IEnumerable<IPackage> packages = GetPackagesForSelectedPage(query);
			return new PackagesForSelectedPageResult(packages, query);
		}
		
		void OnPackagesReadForSelectedPage(ITask<PackagesForSelectedPageResult> task)
		{
			IsReadingPackages = false;
			IsLoadingNextPage = false;
			if (task.IsFaulted) {
				SaveError(task.Exception);
			} else if (task.IsCancelled || !IsCurrentQuery(task.Result)) {
				// Ignore.
				return;
			} else {
				SaveAnyWarnings ();
				UpdatePackagesForSelectedPage(task.Result);
			}
			base.OnPropertyChanged(null);
		}

		bool IsCurrentQuery(PackagesForSelectedPageResult result)
		{
			return packagesForSelectedPageQuery == result.Query;
		}

		void SaveError(AggregateException ex)
		{
			HasError = true;
			ErrorMessage = GetErrorMessage(ex);
			LoggingService.LogInfo("PackagesViewModel error", ex);
		}
		
		string GetErrorMessage(AggregateException ex)
		{
			var errorMessage = new AggregateExceptionErrorMessage(ex);
			return errorMessage.ToString();
		}

		void SaveAnyWarnings ()
		{
			string warning = GetWarningMessage ();
			if (!String.IsNullOrEmpty (warning)) {
				HasError = true;
				ErrorMessage = warning;
			}
		}

		protected virtual string GetWarningMessage ()
		{
			return String.Empty;
		}

		void UpdatePackagesForSelectedPage(PackagesForSelectedPageResult result)
		{
			pages.TotalItems = result.TotalPackages;
			pages.TotalItemsOnSelectedPage = result.TotalPackagesOnPage;
			TotalItems = result.TotalPackages;
			allPackages = result.AllPackages;

			UpdatePackageViewModels (PrioritizePackages (result), result.Query.SearchCriteria);
		}

		IEnumerable<IPackage> PrioritizePackages (PackagesForSelectedPageResult result)
		{
			return PrioritizePackages (result.Packages, result.Query.SearchCriteria);
		}

		protected virtual IEnumerable<IPackage> PrioritizePackages (IEnumerable<IPackage> packages, PackageSearchCriteria searchCriteria)
		{
			return packages;
		}

		IEnumerable<IPackage> GetPackagesForSelectedPage(PackagesForSelectedPageQuery query)
		{
			IEnumerable<IPackage> filteredPackages = GetFilteredPackagesBeforePagingResults(query);
			return GetPackagesForSelectedPage(filteredPackages, query);
		}
		
		IEnumerable<IPackage> GetFilteredPackagesBeforePagingResults(PackagesForSelectedPageQuery query)
		{
			if (query.AllPackages == null) {
				IQueryable<IPackage> packages = GetPackagesFromPackageSource(query.SearchCriteria);
				query.TotalPackages = packages.Count();
				query.AllPackages = GetFilteredPackagesBeforePagingResults (packages, query.SearchCriteria);
			}
			return query.AllPackages;
		}
		
		/// <summary>
		/// Returns the queryable object that will be used to query the NuGet online feed.
		/// </summary>
		public IQueryable<IPackage> GetPackagesFromPackageSource()
		{
			return GetPackagesFromPackageSource(new PackageSearchCriteria (SearchTerms));
		}

		IQueryable<IPackage> GetPackagesFromPackageSource (PackageSearchCriteria search)
		{
			IQueryable<IPackage> packages = GetPackages (search);
			return OrderPackages (packages, search);
		}

		protected virtual IQueryable<IPackage> OrderPackages (IQueryable<IPackage> packages, PackageSearchCriteria search)
		{
			return packages
				.OrderBy(package => package.Id);
		}

		IEnumerable<IPackage> GetPackagesForSelectedPage(IEnumerable<IPackage> allPackages, PackagesForSelectedPageQuery query)
		{
			return allPackages
				.Skip(query.Skip)
				.Take(query.Take);
		}
		
		/// <summary>
		/// Returns all the packages.
		/// </summary>
		protected virtual IQueryable<IPackage> GetAllPackages()
		{
			return null;
		}

		/// <summary>
		/// Returns packages filtered by search criteria.
		/// </summary>
		protected virtual IQueryable<IPackage> GetPackages (PackageSearchCriteria search)
		{
			return null;
		}
		
		/// <summary>
		/// Allows filtering of the packages before paging the results. Call base class method
		/// to run default filtering.
		/// </summary>
		protected virtual IEnumerable<IPackage> GetFilteredPackagesBeforePagingResults (IQueryable<IPackage> allPackages, PackageSearchCriteria search)
		{
			IEnumerable<IPackage> bufferedPackages = GetBufferedPackages(allPackages);
			return bufferedPackages;
		}
		
		IEnumerable<IPackage> GetBufferedPackages(IQueryable<IPackage> allPackages)
		{
			return allPackages.AsBufferedEnumerable(30);
		}
		
		void UpdatePackageViewModels (IEnumerable<IPackage> packages, PackageSearchCriteria search)
		{
			IEnumerable<PackageViewModel> currentViewModels = ConvertToPackageViewModels (packages, search);
			UpdatePackageViewModels(currentViewModels);
		}
		
		void UpdatePackageViewModels(IEnumerable<PackageViewModel> newPackageViewModels)
		{
			if (ClearPackagesOnPaging) {
				ClearPackages ();
			}
			PackageViewModels.AddRange(newPackageViewModels);
		}
		
		void ClearPackages()
		{
			PackageViewModels.Clear();
		}
		
		public IEnumerable<PackageViewModel> ConvertToPackageViewModels (IEnumerable<IPackage> packages, PackageSearchCriteria search)
		{
			foreach (IPackage package in packages) {
				PackageViewModel packageViewModel = CreatePackageViewModel (package, search);
				CheckNewPackageViewModelIfPreviouslyChecked (packageViewModel);
				yield return packageViewModel;
			}
		}
		
		protected virtual PackageViewModel CreatePackageViewModel (IPackage package, PackageSearchCriteria search)
		{
			PackageFromRepository packageFromRepository = CreatePackageFromRepository (package);
			return packageViewModelFactory.CreatePackageViewModel(this, packageFromRepository);
		}

		PackageFromRepository CreatePackageFromRepository (IPackage package)
		{
			var packageFromRepository = package as PackageFromRepository;
			if (packageFromRepository != null) {
				return packageFromRepository;
			}

			var repository = registeredPackageRepositories.ActiveRepository;
			return new PackageFromRepository(package, repository);
		}
		
		public int SelectedPageNumber {
			get { return pages.SelectedPageNumber; }
			set {
				if (pages.SelectedPageNumber != value) {
					pages.SelectedPageNumber = value;
					IsLoadingNextPage = true;
					StartReadPackagesTask(ClearPackagesOnPaging);
					base.OnPropertyChanged(null);
				}
			}
		}
		
		public int PageSize {
			get { return pages.PageSize; }
			set { pages.PageSize = value;  }
		}

		public int ItemsBeforeFirstPage {
			get { return pages.ItemsBeforeFirstPage; }
		}

		public bool IsPaged {
			get { return pages.IsPaged; }
		}
		
		public ObservableCollection<Page> Pages {
			get { return pages; }
		}
		
		public bool HasPreviousPage {
			get { return pages.HasPreviousPage; }
		}
		
		public bool HasNextPage {
			get { return pages.HasNextPage; }
		}
		
		public int MaximumSelectablePages {
			get { return pages.MaximumSelectablePages; }
			set { pages.MaximumSelectablePages = value; }
		}
		
		public int TotalItems { get; private set; }
		
		public void ShowNextPage()
		{
			SelectedPageNumber += 1;
		}
		
		public void ShowPreviousPage()
		{
			SelectedPageNumber -= 1;
		}
		
		void ExecuteShowPageCommand(object param)
		{
			int pageNumber = (int)param;
			ShowPage(pageNumber);
		}
		
		public void ShowPage(int pageNumber)
		{
			SelectedPageNumber = pageNumber;
		}
		
		public bool IsSearchable { get; set; }
		
		public string SearchTerms { get; set; }
		
		public void Search()
		{
			ReadPackages();
			OnPropertyChanged(null);
		}
		
		public bool ShowPackageSources { get; set; }
		
		public IEnumerable<PackageSource> PackageSources {
			get {
				if (registeredPackageRepositories.PackageSources.HasMultipleEnabledPackageSources) {
					yield return RegisteredPackageSourceSettings.AggregatePackageSource;
				}
				foreach (PackageSource packageSource in registeredPackageRepositories.PackageSources.GetEnabledPackageSources()) {
					yield return packageSource;
				}
			}
		}
		
		public PackageSource SelectedPackageSource {
			get { return registeredPackageRepositories.ActivePackageSource; }
			set {
				if (registeredPackageRepositories.ActivePackageSource != value) {
					registeredPackageRepositories.ActivePackageSource = value;
					ReadPackages();
					OnPropertyChanged(null);
				}
			}
		}
		
		public bool ShowUpdateAllPackages { get; set; }
		
		public bool IsUpdateAllPackagesEnabled {
			get {
				return ShowUpdateAllPackages && (TotalItems > 1);
			}
		}
		
		void UpdateAllPackages()
		{
			try {
				packageViewModelFactory.PackageManagementEvents.OnPackageOperationsStarting();
				TryUpdatingAllPackages();
			} catch (Exception ex) {
				ReportError(ex);
				LogError(ex);
			}
		}
		
		void LogError(Exception ex)
		{
			packageViewModelFactory
				.Logger
				.Log(MessageLevel.Error, ex.ToString());
		}
		
		void ReportError(Exception ex)
		{
			packageViewModelFactory
				.PackageManagementEvents
				.OnPackageOperationError(ex);
		}
		
		protected virtual void TryUpdatingAllPackages()
		{
		}
		
		protected IPackageActionRunner ActionRunner {
			get { return packageViewModelFactory.PackageActionRunner; }
		}
		
		public bool IncludePrerelease {
			get { return includePrerelease; }
			set {
				if (includePrerelease != value) {
					includePrerelease = value;
					ReadPackages();
					OnPropertyChanged(null);
				}
			}
		}
		
		public bool ShowPrerelease { get; set; }
		public bool ClearPackagesOnPaging { get; set; }
		public bool IsLoadingNextPage { get; private set; }

		public ObservableCollection<PackageViewModel> CheckedPackageViewModels { get; private set; }

		public void OnPackageCheckedChanged (PackageViewModel packageViewModel)
		{
			if (ignorePackageCheckedChanged)
				return;

			if (packageViewModel.IsChecked) {
				UncheckExistingCheckedPackageWithDifferentVersion (packageViewModel);
				CheckedPackageViewModels.Add (packageViewModel);
			} else {
				CheckedPackageViewModels.Remove (packageViewModel);
			}
		}

		void CheckNewPackageViewModelIfPreviouslyChecked (PackageViewModel packageViewModel)
		{
			ignorePackageCheckedChanged = true;
			try {
				packageViewModel.IsChecked = CheckedPackageViewModels.Contains (packageViewModel);
			} finally {
				ignorePackageCheckedChanged = false;
			}
		}

		void UncheckExistingCheckedPackageWithDifferentVersion (PackageViewModel packageViewModel)
		{
			PackageViewModel existingPackageViewModel = CheckedPackageViewModels
				.FirstOrDefault (item => item.Id == packageViewModel.Id);

			if (existingPackageViewModel != null) {
				CheckedPackageViewModels.Remove (existingPackageViewModel);
				existingPackageViewModel.IsChecked = false;
			}
		}
	}
}
