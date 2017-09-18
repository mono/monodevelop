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
using NuGet.PackageManagement;
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
		INuGetProjectContext projectContext;
		List<PackageReference> packageReferences = new List<PackageReference> ();
		AggregatePackageSourceErrorMessage aggregateErrorMessage;
		NuGetPackageManager packageManager;
		RecentNuGetPackagesRepository recentPackagesRepository;

		public static AllPackagesViewModel Create (RecentNuGetPackagesRepository recentPackagesRepository)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (IdeApp.ProjectOperations.CurrentSelectedSolution);
			var dotNetProject = new DotNetProjectProxy ((DotNetProject)IdeApp.ProjectOperations.CurrentSelectedProject);
			return new AllPackagesViewModel (solutionManager, dotNetProject, recentPackagesRepository);
		}

		public static AllPackagesViewModel Create (
			DotNetProject project,
			RecentNuGetPackagesRepository recentPackagesRepository)
		{
			if (project == null)
				return Create (recentPackagesRepository);

			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			var dotNetProject = new DotNetProjectProxy (project);
			return new AllPackagesViewModel (solutionManager, dotNetProject, recentPackagesRepository);
		}

		public AllPackagesViewModel (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			RecentNuGetPackagesRepository recentPackagesRepository)
			: this (
				solutionManager,
				dotNetProject,
				new NuGetProjectContext (),
				recentPackagesRepository)
		{
		}

		public AllPackagesViewModel (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			INuGetProjectContext projectContext,
			RecentNuGetPackagesRepository recentPackagesRepository)
		{
			this.solutionManager = solutionManager;
			this.dotNetProject = dotNetProject;
			this.projectContext = projectContext;
			this.recentPackagesRepository = recentPackagesRepository;
			PackageViewModels = new ObservableCollection<PackageSearchResultViewModel> ();
			CheckedPackageViewModels = new ObservableCollection<PackageSearchResultViewModel> ();
			ErrorMessage = String.Empty;

			packageManager = new NuGetPackageManager (
				solutionManager.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
				solutionManager,
				new DeleteOnRestartManager ()
			);

			nugetProject = solutionManager.GetNuGetProject (dotNetProject);
			GetPackagesInstalledInProject ();
		}

		public NuGetProject NuGetProject { 
			get { return nugetProject; }
		}

		public IDotNetProject Project {
			get { return dotNetProject; }
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
			ISourceRepositoryProvider provider = solutionManager.CreateSourceRepositoryProvider ();
			packageSourceProvider = provider.PackageSourceProvider;
			var repositories = provider.GetRepositories ().ToList ();

			if (repositories.Count > 1) {
				yield return new AggregateSourceRepositoryViewModel (repositories);
			}

			foreach (SourceRepository repository in repositories) {
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
					.FirstOrDefault (viewModel => String.Equals (viewModel.PackageSource.Name, packageSourceProvider.ActivePackageSourceName, StringComparison.CurrentCultureIgnoreCase));
				if (packageSource != null) {
					return packageSource;
				}
			}

			return packageSources.FirstOrDefault (packageSource => !packageSource.IsAggregate);
		}

		void SaveActivePackageSource ()
		{
			if (selectedPackageSource == null || packageSourceProvider == null)
				return;

			packageSourceProvider.SaveActivePackageSource (selectedPackageSource.PackageSource);
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
				CancelReadPackagesTask ();
				ClearPackages ();
			}
			CreateReadPackagesTask ();
		}

		void CancelReadPackagesTask()
		{
			if (cancellationTokenSource != null) {
				// Cancel on another thread since CancellationTokenSource.Cancel can sometimes
				// take up to a second on Mono and we do not want to block the UI thread.
				var tokenSource = cancellationTokenSource;
				Task.Run (() => {
					try {
						tokenSource.Cancel ();
						tokenSource.Dispose ();
					} catch (Exception ex) {
						LoggingService.LogError ("Unable to cancel task.", ex);
					}
				});
				cancellationTokenSource = null;
			}
		}

		protected virtual Task CreateReadPackagesTask()
		{
			var loader = currentLoader ?? CreatePackageLoader ();
			cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource ();
			return LoadPackagesAsync (loader, cancellationTokenSource.Token)
				.ContinueWith (t => OnPackagesRead (t, loader), TaskScheduler.FromCurrentSynchronizationContext ());
		}

		PackageItemLoader CreatePackageLoader ()
		{
			var context = new PackageLoadContext (
				selectedPackageSource.GetSourceRepositories (),
				false,
				nugetProject);
			
			var loader = new PackageItemLoader (
				context,
				CreatePackageFeed (context.SourceRepositories),
				SearchTerms,
				IncludePrerelease
			);

			currentLoader = loader;

			return loader;
		}

		protected virtual IPackageFeed CreatePackageFeed (IEnumerable<SourceRepository> sourceRepositories)
		{
			return new MultiSourcePackageFeed (sourceRepositories, this);
		}

		protected virtual Task LoadPackagesAsync (PackageItemLoader loader, CancellationToken token)
		{
			return Task.Run (async () => {
				await loader.LoadNextAsync (null, token);

				while (loader.State.LoadingStatus == LoadingStatus.Loading) {
					token.ThrowIfCancellationRequested ();
					await loader.UpdateStateAsync (null, token);
				}
			});
		}

		void ClearError ()
		{
			HasError = false;
			ErrorMessage = String.Empty;
			aggregateErrorMessage = new AggregatePackageSourceErrorMessage (GetTotalPackageSources ());
		}

		int GetTotalPackageSources ()
		{
			if (selectedPackageSource != null) {
				return selectedPackageSource.GetSourceRepositories ().Count ();
			}
			return 0;
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
			var packages = ConvertToPackageViewModels (newPackageViewModels).ToList ();
			packages = PrioritizePackages (packages).ToList ();

			foreach (PackageSearchResultViewModel packageViewModel in packages) {
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
				SelectedPackageSource.GetSourceRepositories (),
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

		protected virtual Task GetPackagesInstalledInProject ()
		{
			return nugetProject
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
			if (level == MessageLevel.Error) {
				string fullErrorMessage = String.Format (message, args);
				AppendErrorMessage (fullErrorMessage);
			}
		}

		void AppendErrorMessage (string message)
		{
			aggregateErrorMessage.AddError (message);
			ErrorMessage = aggregateErrorMessage.ErrorMessage;
			HasError = true;
			OnPropertyChanged (null);
		}

		public void LoadPackageMetadata (PackageSearchResultViewModel packageViewModel)
		{
			var provider = new MultiSourcePackageMetadataProvider (
				selectedPackageSource.GetSourceRepositories (),
				packageManager.PackagesFolderSourceRepository,
				packageManager.GlobalPackageFolderRepositories,
				new [] { NuGetProject },
				false,
				new NuGet.Common.NullLogger ());

			packageViewModel.LoadPackageMetadata (provider, cancellationTokenSource.Token);
		}

		public void OnInstallingSelectedPackages ()
		{
			try {
				UpdateRecentPackages ();
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to update recent packages", ex);
			}
		}

		void UpdateRecentPackages ()
		{
			if (SelectedPackageSource == null)
				return;

			if (CheckedPackageViewModels.Any ()) {
				foreach (PackageSearchResultViewModel packageViewModel in CheckedPackageViewModels) {
					recentPackagesRepository.AddPackage (packageViewModel, SelectedPackageSource.Name);
				}
			} else {
				recentPackagesRepository.AddPackage (SelectedPackage, SelectedPackageSource.Name);
			}
		}

		IEnumerable<PackageSearchResultViewModel> PrioritizePackages (IEnumerable<PackageSearchResultViewModel> packages)
		{
			var recentPackages = GetRecentPackages ().ToList ();

			foreach (PackageSearchResultViewModel package in recentPackages) {
				package.Parent = this;
				package.ResetForRedisplay (IncludePrerelease);
				yield return package;
			}

			foreach (PackageSearchResultViewModel package in packages) {
				if (!recentPackages.Contains (package, PackageSearchResultViewModelComparer.Instance)) {
					yield return package;
				}
			}
		}

		IEnumerable<PackageSearchResultViewModel> GetRecentPackages ()
		{
			if (PackageViewModels.Count == 0 &&
				String.IsNullOrEmpty (SearchTerms) &&
				selectedPackageSource != null) {
				return recentPackagesRepository.GetPackages (SelectedPackageSource.Name)
					.Where (SelectedVersionMatchesIncludePreleaseFilter);
			}

			return Enumerable.Empty<PackageSearchResultViewModel> ();
		}

		bool SelectedVersionMatchesIncludePreleaseFilter (PackageSearchResultViewModel package)
		{
			if (package.SelectedVersion.IsPrerelease) {
				return IncludePrerelease;
			}

			return true;
		}
	}
}

