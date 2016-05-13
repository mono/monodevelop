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
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement.UI;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class AllPackagesViewModel : ViewModelBase<AllPackagesViewModel>, INuGetUILogger
	{
		SourceRepositoryViewModel selectedPackageSource;
		IPackageSourceProvider packageSourceProvider;
		PackageItemLoader currentLoader;
		CancellationTokenSource cancellationTokenSource;
		List<SourceRepositoryViewModel> packageSources;
		bool includePrerelease;
		bool ignorePackageCheckedChanged;
		IMonoDevelopSolutionManager solutionManager;
		NuGetProject nugetProject;
		IDotNetProject dotNetProject;
		NuGetProjectContext projectContext;
		List<PackageReference> packageReferences = new List<PackageReference> ();

		public AllPackagesViewModel ()
		{
			PackageViewModels = new ObservableCollection<PackageSearchResultViewModel> ();
			CheckedPackageViewModels = new ObservableCollection<PackageSearchResultViewModel> ();
			ErrorMessage = String.Empty;

			solutionManager = PackageManagementServices.Workspace.GetSolutionManager (IdeApp.ProjectOperations.CurrentSelectedSolution);
			projectContext = new NuGetProjectContext ();
			dotNetProject = new DotNetProjectProxy ((DotNetProject)IdeApp.ProjectOperations.CurrentSelectedProject);

			nugetProject = solutionManager.GetNuGetProject (dotNetProject);
			GetPackagesInstalledInProject ();
		}

		public string SearchTerms { get; set; }

		public IEnumerable<SourceRepositoryViewModel> PackageSources {
			get {
				if (packageSources == null) {
					packageSources = GetPackageSources ().ToList ();
				}
				return packageSources;
			}
		}

		IEnumerable<SourceRepositoryViewModel> GetPackageSources ()
		{
			//if (PackageManagementServices.RegisteredPackageRepositories.PackageSources.HasMultipleEnabledPackageSources) {
			//	yield return RegisteredPackageSourceSettings.AggregatePackageSource;
			//}
			ISourceRepositoryProvider provider = solutionManager.CreateSourceRepositoryProvider ();
			packageSourceProvider = provider.PackageSourceProvider;
			foreach (SourceRepository repository in provider.GetRepositories ()) {
				yield return new SourceRepositoryViewModel (repository);
			}
		}

		public SourceRepositoryViewModel SelectedPackageSource {
			get {
				if (selectedPackageSource == null) {
					selectedPackageSource = GetActivePackageSource ();
				}
				return selectedPackageSource;
			}
			set {
				if (selectedPackageSource != value) {
					selectedPackageSource = value;
					SaveActivePackageSource ();
					ReadPackages ();
					OnPropertyChanged (null);
				}
			}
		}

		SourceRepositoryViewModel GetActivePackageSource ()
		{
			if (packageSources == null)
				return null;

			if (!String.IsNullOrEmpty (packageSourceProvider.ActivePackageSourceName)) {
				SourceRepositoryViewModel packageSource = packageSources
					.FirstOrDefault (viewModel => String.Equals (viewModel.Name, packageSourceProvider.ActivePackageSourceName, StringComparison.CurrentCultureIgnoreCase));
				if (packageSource != null) {
					return packageSource;
				}
			}

			return packageSources.FirstOrDefault ();
		}

		void SaveActivePackageSource ()
		{
			if (selectedPackageSource == null || packageSourceProvider == null)
				return;

			packageSourceProvider.SaveActivePackageSource (selectedPackageSource.SourceRepository.PackageSource);
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

		public void Search ()
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
			currentLoader = null;
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
			var loader = currentLoader ?? CreatePackageLoader ();
			cancellationTokenSource = new CancellationTokenSource ();
			loader.LoadNextAsync (null, cancellationTokenSource.Token)
				.ContinueWith (t => OnPackagesRead (t, loader), TaskScheduler.FromCurrentSynchronizationContext ());
		}

		PackageItemLoader CreatePackageLoader ()
		{
			var context = new PackageLoadContext (
				new [] { selectedPackageSource.SourceRepository },
				false,
				nugetProject);
			
			var packageFeed = new MultiSourcePackageFeed (context.SourceRepositories, this);
			var loader = new PackageItemLoader (
				context,
				packageFeed,
				SearchTerms,
				IncludePrerelease
			);

			currentLoader = loader;

			return loader;
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

		void OnPackagesRead (Task task, PackageItemLoader loader)
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
				UpdatePackagesForSelectedPage (loader);
			}
			base.OnPropertyChanged (null);
		}

		bool IsCurrentQuery (PackageItemLoader loader)
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

		void UpdatePackagesForSelectedPage (PackageItemLoader loader)
		{
			HasNextPage = loader.State.LoadingStatus == LoadingStatus.Ready;

			UpdatePackageViewModels (loader.GetCurrent ());
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
				UpdatePackageViewModelIfPreviouslyChecked (packageViewModel);
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

		void UpdatePackageViewModelIfPreviouslyChecked (PackageSearchResultViewModel packageViewModel)
		{
			ignorePackageCheckedChanged = true;
			try {
				PackageSearchResultViewModel existingPackageViewModel = GetExistingCheckedPackageViewModel (packageViewModel.Id);
				if (existingPackageViewModel != null) {
					packageViewModel.UpdateFromPreviouslyCheckedViewModel (existingPackageViewModel);
					CheckedPackageViewModels.Remove (existingPackageViewModel);
					CheckedPackageViewModels.Add (packageViewModel);
				}
			} finally {
				ignorePackageCheckedChanged = false;
			}
		}

		void UncheckExistingCheckedPackageWithDifferentVersion (PackageSearchResultViewModel packageViewModel)
		{
			PackageSearchResultViewModel existingPackageViewModel = GetExistingCheckedPackageViewModel (packageViewModel.Id);

			if (existingPackageViewModel != null) {
				CheckedPackageViewModels.Remove (existingPackageViewModel);
				existingPackageViewModel.IsChecked = false;
			}
		}

		PackageSearchResultViewModel GetExistingCheckedPackageViewModel (string packageId)
		{
			return CheckedPackageViewModels
				.FirstOrDefault (item => item.Id == packageId);
		}

		public IPackageAction CreateInstallPackageAction (PackageSearchResultViewModel packageViewModel)
		{
			return new InstallNuGetPackageAction (
				SelectedPackageSource.SourceRepository,
				solutionManager,
				dotNetProject,
				projectContext
			) {
				IncludePrerelease = IncludePrerelease,
				PackageId = packageViewModel.Id,
				Version = packageViewModel.SelectedVersion
			};
		}

		public PackageSearchResultViewModel SelectedPackage { get; set; }

		public bool IsOlderPackageInstalled (string id, NuGetVersion version)
		{
			return packageReferences.Any (packageReference => IsOlderPackageInstalled (packageReference, id, version));
		}

		bool IsOlderPackageInstalled (PackageReference packageReference, string id, NuGetVersion version)
		{
			return packageReference.PackageIdentity.Id == id &&
				packageReference.PackageIdentity.Version < version;
		}

		void GetPackagesInstalledInProject ()
		{
			nugetProject
				.GetInstalledPackagesAsync (CancellationToken.None)
				.ContinueWith (task => OnReadInstalledPackages (task), TaskScheduler.FromCurrentSynchronizationContext ());
		}

		void OnReadInstalledPackages (Task<IEnumerable<PackageReference>> task)
		{
			try {
				if (task.IsFaulted) {
					LoggingService.LogError ("Unable to read installed packages.", task.Exception);
				} else {
					packageReferences = task.Result.ToList ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("OnReadInstalledPackages", ex);
			}
		}

		void INuGetUILogger.Log (MessageLevel level, string message, params object [] args)
		{
		}

		void INuGetUILogger.ReportError (string message)
		{
		}

		void INuGetUILogger.Start ()
		{
		}

		void INuGetUILogger.End ()
		{
		}
	}
}

