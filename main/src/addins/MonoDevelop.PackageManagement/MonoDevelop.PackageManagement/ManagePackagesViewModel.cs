//
// ManagePackagesViewModel.cs
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
using NuGet.Common;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.PackageManagement.UI;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class ManagePackagesViewModel : ViewModelBase<ManagePackagesViewModel>, INuGetUILogger
	{
		SourceRepositoryViewModel selectedPackageSource;
		ManagePackagesSearchResultViewModel selectedPackage;
		ManagePackagesPage pageSelected;
		IPackageSourceProvider packageSourceProvider;
		PackageItemLoader currentLoader;
		CancellationTokenSource cancellationTokenSource;
		List<SourceRepositoryViewModel> packageSources;
		bool includePrerelease;
		bool ignorePackageCheckedChanged;
		IMonoDevelopSolutionManager solutionManager;
		List<NuGetProject> nugetProjects;
		List<IDotNetProject> dotNetProjects;
		INuGetProjectContext projectContext;
		AggregatePackageSourceErrorMessage aggregateErrorMessage;
		NuGetPackageManager packageManager;
		RecentManagedNuGetPackagesRepository recentPackagesRepository;
		List<ManagePackagesProjectInfo> projectInformation = new List<ManagePackagesProjectInfo> ();
		Dictionary<string, List<ManageProjectViewModel>> cachedProjectViewModels =
			new Dictionary<string, List<ManageProjectViewModel>> (StringComparer.OrdinalIgnoreCase);

		public static ManagePackagesViewModel Create (
			RecentManagedNuGetPackagesRepository recentPackagesRepository,
			IDotNetProject project)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (IdeApp.ProjectOperations.CurrentSelectedSolution);
			var solution = new SolutionProxy (IdeApp.ProjectOperations.CurrentSelectedSolution);
			return new ManagePackagesViewModel (solutionManager, solution, recentPackagesRepository, project);
		}

		public ManagePackagesViewModel (
			IMonoDevelopSolutionManager solutionManager,
			ISolution solution,
			RecentManagedNuGetPackagesRepository recentPackagesRepository,
			IDotNetProject project)
			: this (
				solutionManager,
				solution,
				new NuGetProjectContext (solutionManager.Settings),
				recentPackagesRepository,
				project)
		{
		}

		public ManagePackagesViewModel (
			IMonoDevelopSolutionManager solutionManager,
			ISolution solution,
			INuGetProjectContext projectContext,
			RecentManagedNuGetPackagesRepository recentPackagesRepository,
			IDotNetProject project)
		{
			this.solutionManager = solutionManager;
			this.projectContext = projectContext;
			this.recentPackagesRepository = recentPackagesRepository;
			IsManagingSolution = project == null;
			PackageViewModels = new ObservableCollection<ManagePackagesSearchResultViewModel> ();
			CheckedPackageViewModels = new ObservableCollection<ManagePackagesSearchResultViewModel> ();
			ProjectViewModels = new ObservableCollection<ManageProjectViewModel> ();
			ErrorMessage = String.Empty;
			PageSelected = ManagePackagesPage.Browse;

			if (project != null) {
				dotNetProjects = new List<IDotNetProject> ();
				dotNetProjects.Add (project);
			} else {
				dotNetProjects = solution.GetAllProjects ().ToList ();
			}

			packageManager = new NuGetPackageManager (
				solutionManager.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
				solutionManager,
				new DeleteOnRestartManager ()
			);

			nugetProjects = dotNetProjects
				.Select (dotNetProject => solutionManager.GetNuGetProject (dotNetProject))
				.ToList ();

			GetPackagesInstalledInProjects ().Ignore ();
		}

		public bool IsManagingSolution { get; private set; }

		public IEnumerable<NuGetProject> NuGetProjects { 
			get { return nugetProjects; }
		}

		public IDotNetProject Project {
			get { return dotNetProjects[0]; }
		}

		public IEnumerable<IDotNetProject> DotNetProjects {
			get { return dotNetProjects; }
		}

		public string SearchTerms { get; set; }

		public ManagePackagesPage PageSelected {
			get { return pageSelected; }
			set {
				if (pageSelected != value) {
					pageSelected = value;
					CheckedPackageViewModels.Clear ();
					cachedProjectViewModels.Clear ();
				}
			}
		}

		public bool IsConsolidatePageSelected {
			get { return PageSelected == ManagePackagesPage.Consolidate; }
		}

		public bool IsInstalledPageSelected {
			get { return PageSelected == ManagePackagesPage.Installed; }
		}

		public bool IsBrowsePageSelected {
			get { return PageSelected == ManagePackagesPage.Browse; }
		}

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

		public ObservableCollection<ManagePackagesSearchResultViewModel> PackageViewModels { get; private set; }
		public ObservableCollection<ManagePackagesSearchResultViewModel> CheckedPackageViewModels { get; private set; }
		public ObservableCollection<ManageProjectViewModel> ProjectViewModels { get; private set; }

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
				true,
				nugetProjects);
			
			var loader = new PackageItemLoader (
				context,
				CreatePackageFeed (context),
				SearchTerms,
				IncludePrerelease
			);

			currentLoader = loader;

			return loader;
		}

		protected virtual IPackageFeed CreatePackageFeed (PackageLoadContext context)
		{
			if (PageSelected == ManagePackagesPage.Browse)
				return new MultiSourcePackageFeed (context.SourceRepositories, this);

			if (PageSelected == ManagePackagesPage.Installed)
				return new InstalledPackageFeed (context, CreatePackageMetadataProvider (), new NullLogger ());

			if (PageSelected == ManagePackagesPage.Updates) {
				return new UpdatePackageFeed (
					context,
					CreatePackageMetadataProvider (),
					new PackageSearchMetadataCache (),
					new NullLogger ());
			}

			if (PageSelected == ManagePackagesPage.Consolidate)
				return new ConsolidatePackageFeed (context, CreatePackageMetadataProvider (), new NullLogger ());

			throw new InvalidOperationException ("Unsupported package feed");
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

			foreach (ManagePackagesSearchResultViewModel packageViewModel in packages) {
				PackageViewModels.Add (packageViewModel);
			}
		}

		public IEnumerable<ManagePackagesSearchResultViewModel> ConvertToPackageViewModels (IEnumerable<PackageItemListViewModel> itemViewModels)
		{
			foreach (PackageItemListViewModel itemViewModel in itemViewModels) {
				ManagePackagesSearchResultViewModel packageViewModel = CreatePackageViewModel (itemViewModel);
				UpdatePackageViewModelIfPreviouslyChecked (packageViewModel);
				yield return packageViewModel;
			}
		}

		ManagePackagesSearchResultViewModel CreatePackageViewModel (PackageItemListViewModel viewModel)
		{
			bool showVersion = ShowPackageVersionInsteadOfDownloadCount ();
			return new ManagePackagesSearchResultViewModel (this, viewModel) {
				ShowVersionInsteadOfDownloadCount = showVersion,
				SelectLatestVersion = IsConsolidatePageSelected
			};
		}

		bool ShowPackageVersionInsteadOfDownloadCount ()
		{
			return PageSelected != ManagePackagesPage.Browse;
		}

		void ClearPackages ()
		{
			PackageViewModels.Clear();
		}

		public void OnPackageCheckedChanged (ManagePackagesSearchResultViewModel packageViewModel)
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

		void UpdatePackageViewModelIfPreviouslyChecked (ManagePackagesSearchResultViewModel packageViewModel)
		{
			ignorePackageCheckedChanged = true;
			try {
				ManagePackagesSearchResultViewModel existingPackageViewModel = GetExistingCheckedPackageViewModel (packageViewModel.Id);
				if (existingPackageViewModel != null) {
					packageViewModel.UpdateFromPreviouslyCheckedViewModel (existingPackageViewModel);
					CheckedPackageViewModels.Remove (existingPackageViewModel);
					CheckedPackageViewModels.Add (packageViewModel);
				}
			} finally {
				ignorePackageCheckedChanged = false;
			}
		}

		void UncheckExistingCheckedPackageWithDifferentVersion (ManagePackagesSearchResultViewModel packageViewModel)
		{
			ManagePackagesSearchResultViewModel existingPackageViewModel = GetExistingCheckedPackageViewModel (packageViewModel.Id);

			if (existingPackageViewModel != null) {
				CheckedPackageViewModels.Remove (existingPackageViewModel);
				existingPackageViewModel.IsChecked = false;
			}
		}

		ManagePackagesSearchResultViewModel GetExistingCheckedPackageViewModel (string packageId)
		{
			return CheckedPackageViewModels
				.FirstOrDefault (item => item.Id == packageId);
		}

		public IEnumerable<IPackageAction> CreateInstallPackageActions (
			ManagePackagesSearchResultViewModel packageViewModel,
			IEnumerable<IDotNetProject> projects)
		{
			bool firstInstall = true;
			foreach (IDotNetProject project in projects) {
				yield return new InstallNuGetPackageAction (
					SelectedPackageSource.GetSourceRepositories (),
					solutionManager,
					project,
					projectContext
				) {
					IncludePrerelease = IncludePrerelease,
					PackageId = packageViewModel.Id,
					Version = packageViewModel.SelectedVersion,
					LicensesMustBeAccepted = firstInstall
				};

				firstInstall = false;
			}
		}

		public IEnumerable<IPackageAction> CreateUninstallPackageActions (
			ManagePackagesSearchResultViewModel packageViewModel,
			IEnumerable<IDotNetProject> projects)
		{
			foreach (IDotNetProject project in projects) {
				if (IsPackageInstalledInProject (project, packageViewModel.Id)) {
					yield return new UninstallNuGetPackageAction (
						solutionManager,
						project
					) {
						PackageId = packageViewModel.Id,
					};
				}
			}
		}

		public IEnumerable<IPackageAction> CreateUpdatePackageActions (
			ManagePackagesSearchResultViewModel packageViewModel,
			IEnumerable<IDotNetProject> projects)
		{
			bool firstInstall = true;
			foreach (IDotNetProject project in projects) {
				if (IsPackageInstalledInProject (project, packageViewModel.Id)) {
					yield return new InstallNuGetPackageAction (
						SelectedPackageSource.GetSourceRepositories (),
						solutionManager,
						project,
						projectContext
					) {
						IncludePrerelease = IncludePrerelease,
						PackageId = packageViewModel.Id,
						Version = packageViewModel.SelectedVersion,
						LicensesMustBeAccepted = firstInstall
					};

					firstInstall = false;
				}
			}
		}

		public IEnumerable<IPackageAction> CreateConsolidatePackageActions (
			ManagePackagesSearchResultViewModel packageViewModel)
		{
			bool firstInstall = true;
			foreach (ManageProjectViewModel project in GetProjectViewModelsForPackage (packageViewModel)) {
				if (!CanConsolidate (project, packageViewModel)) {
					continue;
				}

				yield return new InstallNuGetPackageAction (
					SelectedPackageSource.GetSourceRepositories (),
					solutionManager,
					project.Project,
					projectContext
				) {
					IncludePrerelease = IncludePrerelease,
					PackageId = packageViewModel.Id,
					Version = packageViewModel.SelectedVersion,
					LicensesMustBeAccepted = firstInstall
				};

				firstInstall = false;
			}
		}

		bool IsPackageInstalledInProject (IDotNetProject project, string packageId)
		{
			var matchedProjectInfo = projectInformation.FirstOrDefault (p => p.Project == project);
			return matchedProjectInfo.Packages.Any (package => StringComparer.OrdinalIgnoreCase.Equals (packageId, package.Id));
		}

		bool CanConsolidate (ManageProjectViewModel projectViewModel, ManagePackagesSearchResultViewModel packageViewModel)
		{
			if (!projectViewModel.IsChecked)
				return false;

			if (IsPackageInstalledInProjectWithSameVersion (
				projectViewModel.ProjectInfo,
				packageViewModel.Id,
				packageViewModel.SelectedVersion)) {
				return false;
			}

			return true;
		}

		bool IsPackageInstalledInProjectWithSameVersion (ManagePackagesProjectInfo projectInfo, string packageId, NuGetVersion version)
		{
			var matchPackageId = new PackageIdentity (packageId, version);
			return projectInfo.Packages.Any (package => PackageIdentityComparer.Default.Equals (package, matchPackageId));
		}

		public ManagePackagesSearchResultViewModel SelectedPackage {
			get { return selectedPackage; }
			set {
				if (selectedPackage != value) {
					selectedPackage = value;
					OnSelectedPackageChanged ();
				}
			}
		}

		public bool IsOlderPackageInstalled (string id, NuGetVersion version)
		{
			foreach (ManagePackagesProjectInfo projectInfo in projectInformation) {
				if (projectInfo.Packages.Any (packageId => IsOlderPackageInstalled (packageId, id, version))) {
					return true;
				}
			}
			return false;
		}

		bool IsOlderPackageInstalled (PackageIdentity packageIdentity, string id, NuGetVersion version)
		{
			return packageIdentity.Id == id &&
				packageIdentity.Version < version;
		}

		protected virtual Task GetPackagesInstalledInProjects ()
		{
			return GetInstalledPackagesProjectInfo ()
				.ContinueWith (task => OnReadInstalledPackages (task), TaskScheduler.FromCurrentSynchronizationContext ());
		}

		async Task<List<ManagePackagesProjectInfo>> GetInstalledPackagesProjectInfo ()
		{
			var projectInfo = new List<ManagePackagesProjectInfo> ();
			for (int i = 0; i < nugetProjects.Count; ++i) {
				var nugetProject = nugetProjects[i];
				var packages = await nugetProject.GetInstalledPackagesAsync (CancellationToken.None);
				projectInfo.Add (new ManagePackagesProjectInfo (dotNetProjects[i], packages));
			}
			projectInfo.Sort ();
			return projectInfo;
		}

		void OnReadInstalledPackages (Task<List<ManagePackagesProjectInfo>> task)
		{
			try {
				if (task.IsFaulted) {
					LoggingService.LogError ("Unable to read installed packages.", task.Exception);
				} else {
					projectInformation = task.Result;
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

		public void LoadPackageMetadata (ManagePackagesSearchResultViewModel packageViewModel)
		{
			IPackageMetadataProvider provider = CreatePackageMetadataProvider ();

			packageViewModel.LoadPackageMetadata (provider, cancellationTokenSource.Token);
		}

		IPackageMetadataProvider CreatePackageMetadataProvider ()
		{
			return new MultiSourcePackageMetadataProvider (
				selectedPackageSource.GetSourceRepositories (),
				packageManager.PackagesFolderSourceRepository,
				packageManager.GlobalPackageFolderRepositories,
				nugetProjects.ToArray (),
				IsManagingSolution,
				new NullLogger ());
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
				foreach (ManagePackagesSearchResultViewModel packageViewModel in CheckedPackageViewModels) {
					recentPackagesRepository.AddPackage (packageViewModel, SelectedPackageSource.Name);
				}
			} else {
				recentPackagesRepository.AddPackage (SelectedPackage, SelectedPackageSource.Name);
			}
		}

		IEnumerable<ManagePackagesSearchResultViewModel> PrioritizePackages (IEnumerable<ManagePackagesSearchResultViewModel> packages)
		{
			var recentPackages = GetRecentPackages ().ToList ();

			foreach (ManagePackagesSearchResultViewModel package in recentPackages) {
				package.Parent = this;
				package.ResetForRedisplay (IncludePrerelease);
				yield return package;
			}

			foreach (ManagePackagesSearchResultViewModel package in packages) {
				if (!recentPackages.Contains (package, ManagedPackagesSearchResultViewModelComparer.Instance)) {
					yield return package;
				}
			}
		}

		IEnumerable<ManagePackagesSearchResultViewModel> GetRecentPackages ()
		{
			if (PackageViewModels.Count == 0 &&
				String.IsNullOrEmpty (SearchTerms) &&
				selectedPackageSource != null &&
				PageSelected == ManagePackagesPage.Browse) {
				return recentPackagesRepository.GetPackages (SelectedPackageSource.Name)
					.Where (SelectedVersionMatchesIncludePreleaseFilter);
			}

			return Enumerable.Empty<ManagePackagesSearchResultViewModel> ();
		}

		bool SelectedVersionMatchesIncludePreleaseFilter (ManagePackagesSearchResultViewModel package)
		{
			if (package.SelectedVersion.IsPrerelease) {
				return IncludePrerelease;
			}

			return true;
		}

		public IEnumerable<IDotNetProject> GetDotNetProjectsToSelect (IEnumerable<string> packageIds)
		{
			if (PageSelected == ManagePackagesPage.Browse) {
				return dotNetProjects;
			}

			return GetFilteredProjectsToSelect (packageIds);
		}

		IEnumerable<IDotNetProject> GetFilteredProjectsToSelect (IEnumerable<string> packageIds)
		{
			foreach (IDotNetProject project in dotNetProjects) {
				var matchedProjectInfo = projectInformation.FirstOrDefault (p => p.Project == project);
				if (matchedProjectInfo.Packages.Any (package => packageIds.Any (id => StringComparer.OrdinalIgnoreCase.Equals (package.Id, id)))) {
					yield return project;
				}
			}
		}

		void OnSelectedPackageChanged ()
		{
			if (!IsConsolidatePageSelected) {
				return;
			}

			ProjectViewModels.Clear ();

			if (SelectedPackage == null) {
				return;
			}

			List<ManageProjectViewModel> projectViewModels;
			if (!cachedProjectViewModels.TryGetValue (SelectedPackage.Id, out projectViewModels)) {
				projectViewModels = new List<ManageProjectViewModel> ();
				var uncheckedProjectViewModels = new List<ManageProjectViewModel> ();
				foreach (ManagePackagesProjectInfo projectInfo in projectInformation) {
					var projectViewModel = new ManageProjectViewModel (projectInfo, SelectedPackage.Id);
					if (projectViewModel.IsChecked)
						projectViewModels.Add (projectViewModel);
					else
						uncheckedProjectViewModels.Add (projectViewModel);
				}
				// Show projects that have the package installed before the other projects.
				projectViewModels.AddRange (uncheckedProjectViewModels);

				cachedProjectViewModels [SelectedPackage.Id] = projectViewModels;
			}

			foreach (ManageProjectViewModel projectViewModel in projectViewModels) {
				ProjectViewModels.Add (projectViewModel);
			}
		}

		public bool CanConsolidate ()
		{
			if (!IsConsolidatePageSelected) {
				return false;
			}

			if (CheckedPackageViewModels.Count == 0) {
				return CanConsolidateSelectedPackage ();
			}

			return CanConsolidateCheckedPackages ();
		}

		bool CanConsolidateSelectedPackage ()
		{
			if (SelectedPackage == null) {
				return false;
			}

			foreach (ManageProjectViewModel projectViewModel in ProjectViewModels) {
				if (CanConsolidate (projectViewModel, SelectedPackage)) {
					return true;
				}
			}

			return false;
		}

		bool CanConsolidateCheckedPackages ()
		{
			foreach (ManagePackagesSearchResultViewModel packageViewModel in CheckedPackageViewModels) {
				foreach (ManageProjectViewModel projectViewModel in GetProjectViewModelsForPackage (packageViewModel)) {
					if (CanConsolidate (projectViewModel, packageViewModel)) {
						return true;
					}
				}
			}

			return false;
		}

		IEnumerable<ManageProjectViewModel> GetProjectViewModelsForPackage (ManagePackagesSearchResultViewModel packageViewModel)
		{
			List<ManageProjectViewModel> projectViewModels;
			if (cachedProjectViewModels.TryGetValue (packageViewModel.Id, out projectViewModels)) {
				return projectViewModels;
			}
			return Enumerable.Empty<ManageProjectViewModel> ();
		}
	}
}

