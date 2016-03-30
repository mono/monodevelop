//
// AllPackagesViewModel.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NuGet.Configuration;
using NuGet.PackageManagement.UI;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class AllPackagesViewModel : ViewModelBase<AllPackagesViewModel>
	{
		PackageLoader currentLoader;
		CancellationTokenSource cancellationTokenSource;
		int currentIndex;
		bool includePrerelease;
		bool ignorePackageCheckedChanged;

		public AllPackagesViewModel ()
		{
			PackageViewModels = new ObservableCollection<PackageSearchResultViewModel> ();
			CheckedPackageViewModels = new ObservableCollection<PackageSearchResultViewModel> ();
			ErrorMessage = String.Empty;
		}

		public string SearchTerms { get; set; }

		public IEnumerable<NuGet.PackageSource> PackageSources {
			get {
				//if (PackageManagementServices.RegisteredPackageRepositories.PackageSources.HasMultipleEnabledPackageSources) {
				//	yield return RegisteredPackageSourceSettings.AggregatePackageSource;
				//}
				foreach (NuGet.PackageSource packageSource in PackageManagementServices.RegisteredPackageRepositories.PackageSources.GetEnabledPackageSources()) {
					yield return packageSource;
				}
			}
		}

		public NuGet.PackageSource SelectedPackageSource {
			get { return PackageManagementServices.RegisteredPackageRepositories.ActivePackageSource; }
			set {
				if (PackageManagementServices.RegisteredPackageRepositories.ActivePackageSource != value) {
					PackageManagementServices.RegisteredPackageRepositories.ActivePackageSource = value;
					ReadPackages ();
					OnPropertyChanged (null);
				}
			}
		}

		public ObservableCollection<PackageSearchResultViewModel> PackageViewModels { get; private set; }
		public ObservableCollection<PackageSearchResultViewModel> CheckedPackageViewModels { get; private set; }

		public bool HasError { get; private set; }
		public string ErrorMessage { get; private set; }

		public bool IsLoadingNextPage { get; private set; }
		public bool IsReadingPackages { get; private set; }
		public bool HasNextPage { get; private set; }

		public bool IncludePrerelease {
			get { return includePrerelease; }
			set {
				if (includePrerelease != value) {
					includePrerelease = value;
					ReadPackages ();
					OnPropertyChanged (null);
				}
			}
		}

		public void Dispose()
		{
			OnDispose ();
			CancelReadPackagesTask ();
			IsDisposed = true;
		}

		protected virtual void OnDispose()
		{
		}

		public bool IsDisposed { get; private set; }

		public void Search()
		{
			ReadPackages ();
			OnPropertyChanged (null);
		}

		public void ReadPackages ()
		{
			if (SelectedPackageSource == null) {
				return;
			}

			HasNextPage = false;
			IsLoadingNextPage = false;
			//UpdateRepositoryBeforeReadPackagesTaskStarts ();
			StartReadPackagesTask ();
		}

		void StartReadPackagesTask (bool clearPackages = true)
		{
			IsReadingPackages = true;
			ClearError ();
			if (clearPackages) {
				ClearPackages ();
			}
			CancelReadPackagesTask ();
			CreateReadPackagesTask ();
		}

		void CancelReadPackagesTask()
		{
			if (cancellationTokenSource != null) {
				cancellationTokenSource.Cancel ();
				cancellationTokenSource.Dispose ();
				cancellationTokenSource = null;
			}
		}

		void CreateReadPackagesTask()
		{
			var repositories = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			var all = repositories.GetRepositories ().ToList ();

			var repo = all[0];
			var option = new PackageLoaderOption (IncludePrerelease, Pages.DefaultPageSize);
			var loader = new PackageLoader (
				option,
				false,
				null,
				new NuGetProject [0],
				repo,
				SearchTerms
			);
			currentLoader = loader;
			cancellationTokenSource = new CancellationTokenSource ();
			loader.LoadItemsAsync (currentIndex, cancellationTokenSource.Token)
				.ContinueWith (t => OnPackagesRead (t, loader));
		}

		void ClearError ()
		{
			HasError = false;
			ErrorMessage = String.Empty;
		}

		public void ShowNextPage ()
		{
			IsLoadingNextPage = true;
			StartReadPackagesTask (false);
			base.OnPropertyChanged (null);
		}

		void OnPackagesRead (Task<LoadResult> task, PackageLoader loader)
		{
			IsReadingPackages = false;
			IsLoadingNextPage = false;
			if (task.IsFaulted) {
				SaveError (task.Exception);
			} else if (task.IsCanceled || !IsCurrentQuery (loader)) {
				// Ignore.
				return;
			} else {
				SaveAnyWarnings ();
				UpdatePackagesForSelectedPage (task.Result);
			}
			base.OnPropertyChanged (null);
		}

		bool IsCurrentQuery (PackageLoader loader)
		{
			return currentLoader == loader;
		}

		void SaveError (AggregateException ex)
		{
			HasError = true;
			ErrorMessage = GetErrorMessage (ex);
			LoggingService.LogInfo ("PackagesViewModel error", ex);
		}

		string GetErrorMessage (AggregateException ex)
		{
			var errorMessage = new AggregateExceptionErrorMessage (ex);
			return errorMessage.ToString ();
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

		void UpdatePackagesForSelectedPage (LoadResult result)
		{
			currentIndex = result.NextStartIndex;
			HasNextPage = result.HasMoreItems;

			UpdatePackageViewModels (result.Items);
		}

		void UpdatePackageViewModels (IEnumerable<PackageItemListViewModel> newPackageViewModels)
		{
			foreach (PackageSearchResultViewModel packageViewModel in ConvertToPackageViewModels (newPackageViewModels)) {
				PackageViewModels.Add (packageViewModel);
			}
		}

		public IEnumerable<PackageSearchResultViewModel> ConvertToPackageViewModels (IEnumerable<PackageItemListViewModel> itemViewModels)
		{
			foreach (PackageItemListViewModel itemViewModel in itemViewModels) {
				PackageSearchResultViewModel packageViewModel = CreatePackageViewModel (itemViewModel);
				CheckNewPackageViewModelIfPreviouslyChecked (packageViewModel);
				yield return packageViewModel;
			}
		}

		PackageSearchResultViewModel CreatePackageViewModel (PackageItemListViewModel viewModel)
		{
			return new PackageSearchResultViewModel (this, viewModel);
		}

		void ClearPackages ()
		{
			PackageViewModels.Clear();
		}

		public void OnPackageCheckedChanged (PackageSearchResultViewModel packageViewModel)
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

		void CheckNewPackageViewModelIfPreviouslyChecked (PackageSearchResultViewModel packageViewModel)
		{
			ignorePackageCheckedChanged = true;
			try {
				packageViewModel.IsChecked = CheckedPackageViewModels.Contains (packageViewModel);
			} finally {
				ignorePackageCheckedChanged = false;
			}
		}

		void UncheckExistingCheckedPackageWithDifferentVersion (PackageSearchResultViewModel packageViewModel)
		{
			PackageSearchResultViewModel existingPackageViewModel = CheckedPackageViewModels
				.FirstOrDefault (item => item.Id == packageViewModel.Id);

			if (existingPackageViewModel != null) {
				CheckedPackageViewModels.Remove (existingPackageViewModel);
				existingPackageViewModel.IsChecked = false;
			}
		}
	}
}

