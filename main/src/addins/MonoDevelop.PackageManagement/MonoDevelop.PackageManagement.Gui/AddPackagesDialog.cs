//
// AddPackagesDialog.cs
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
using System.ComponentModel;
using System.Linq;
using ICSharpCode.PackageManagement;
using Mono.Unix;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet;
using Xwt;
using Xwt.Drawing;
using Xwt.Formats;
using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;

namespace MonoDevelop.PackageManagement
{
	public partial class AddPackagesDialog
	{
		IBackgroundPackageActionRunner backgroundActionRunner;
		IRecentPackageRepository recentPackageRepository;
		ManagePackagesViewModel parentViewModel;
		PackagesViewModel viewModel;
		List<PackageSource> packageSources;
		DataField<bool> packageHasBackgroundColorField = new DataField<bool> ();
		DataField<PackageViewModel> packageViewModelField = new DataField<PackageViewModel> ();
		DataField<Image> packageImageField = new DataField<Image> ();
		DataField<double> packageCheckBoxAlphaField = new DataField<double> ();
		const double packageCheckBoxSemiTransarentAlpha = 0.6;
		ListStore packageStore;
		PackageCellView packageCellView;
		TimeSpan searchDelayTimeSpan = TimeSpan.FromMilliseconds (500);
		IDisposable searchTimer;
		PackageSource dummyPackageSourceRepresentingConfigureSettingsItem =
			new PackageSource ("", Catalog.GetString ("Configure Sources..."));
		ImageLoader imageLoader = new ImageLoader ();
		bool loadingMessageVisible;

		public AddPackagesDialog (ManagePackagesViewModel parentViewModel, string initialSearch = null)
			: this (
				parentViewModel,
				initialSearch,
				PackageManagementServices.BackgroundPackageActionRunner,
				PackageManagementServices.RecentPackageRepository)
		{
		}

		public AddPackagesDialog (
			ManagePackagesViewModel parentViewModel,
			string initialSearch,
			IBackgroundPackageActionRunner backgroundActionRunner,
			IRecentPackageRepository recentPackageRepository)
		{
			this.parentViewModel = parentViewModel;
			this.viewModel = parentViewModel.AvailablePackagesViewModel;
			this.backgroundActionRunner = backgroundActionRunner;
			this.recentPackageRepository = recentPackageRepository;

			Build ();

			UpdatePackageSearchEntryWithInitialText (initialSearch);

			InitializeListView ();
			UpdateAddPackagesButton ();
			ShowLoadingMessage ();
			LoadViewModel (initialSearch);

			this.showPrereleaseCheckBox.Clicked += ShowPrereleaseCheckBoxClicked;
			this.packageSourceComboBox.SelectionChanged += PackageSourceChanged;
			this.addPackagesButton.Clicked += AddPackagesButtonClicked;
			this.packageSearchEntry.Changed += PackageSearchEntryChanged;
			this.packageSearchEntry.Activated += PackageSearchEntryActivated;
			imageLoader.Loaded += ImageLoaded;
		}

		public bool ShowPreferencesForPackageSources { get; private set; }

		protected override void Dispose (bool disposing)
		{
			imageLoader.Loaded -= ImageLoaded;
			imageLoader.Dispose ();

			viewModel.PropertyChanged -= ViewModelPropertyChanged;
			parentViewModel.Dispose ();
			DisposeExistingTimer ();
			base.Dispose (disposing);
		}

		void UpdatePackageSearchEntryWithInitialText (string initialSearch)
		{
			packageSearchEntry.Text = initialSearch;
			if (!String.IsNullOrEmpty (initialSearch)) {
				packageSearchEntry.CursorPosition = initialSearch.Length;
			}
		}

		public string SearchText {
			get { return packageSearchEntry.Text; }
		}

		void InitializeListView ()
		{
			packageStore = new ListStore (packageHasBackgroundColorField, packageCheckBoxAlphaField, packageImageField, packageViewModelField);
			packagesListView.DataSource = packageStore;

			AddPackageCellViewToListView ();

			packagesListView.SelectionChanged += PackagesListViewSelectionChanged;
			packagesListView.RowActivated += PackagesListRowActivated;
			packagesListView.VerticalScrollControl.ValueChanged += PackagesListViewScrollValueChanged;
		}

		void AddPackageCellViewToListView ()
		{
			packageCellView = new PackageCellView {
				PackageField = packageViewModelField,
				HasBackgroundColorField = packageHasBackgroundColorField,
				CheckBoxAlphaField = packageCheckBoxAlphaField,
				ImageField = packageImageField,
				CellWidth = 535
			};
			var textColumn = new ListViewColumn ("Package", packageCellView);
			packagesListView.Columns.Add (textColumn);

			packageCellView.PackageChecked += PackageCellViewPackageChecked;
		}

		void ShowLoadingMessage ()
		{
			UpdateSpinnerLabel ();
			noPackagesFoundFrame.Visible = false;
			packagesListView.Visible = false;
			loadingSpinnerFrame.Visible = true;
			loadingMessageVisible = true;
		}

		void HideLoadingMessage ()
		{
			loadingSpinnerFrame.Visible = false;
			packagesListView.Visible = true;
			noPackagesFoundFrame.Visible = false;
			loadingMessageVisible = false;
		}

		void UpdateSpinnerLabel ()
		{
			if (String.IsNullOrWhiteSpace (packageSearchEntry.Text)) {
				loadingSpinnerLabel.Text = Catalog.GetString ("Loading package list...");
			} else {
				loadingSpinnerLabel.Text = Catalog.GetString ("Searching packages...");
			}
		}

		void ShowNoPackagesFoundMessage ()
		{
			if (!String.IsNullOrWhiteSpace (packageSearchEntry.Text)) {
				packagesListView.Visible = false;
				noPackagesFoundFrame.Visible = true;
			}
		}

		void ShowPrereleaseCheckBoxClicked (object sender, EventArgs e)
		{
			viewModel.IncludePrerelease = !viewModel.IncludePrerelease;
		}

		void LoadViewModel (string initialSearch)
		{
			viewModel.ClearPackagesOnPaging = false;
			viewModel.SearchTerms = initialSearch;

			ClearSelectedPackageInformation ();
			PopulatePackageSources ();
			viewModel.PropertyChanged += ViewModelPropertyChanged;

			if (viewModel.SelectedPackageSource != null) {
				viewModel.ReadPackages ();
			} else {
				HideLoadingMessage ();
			}
		}

		void ClearSelectedPackageInformation ()
		{
			this.packageInfoVBox.Visible = false;
		}

		List<PackageSource> PackageSources {
			get {
				if (packageSources == null) {
					packageSources = viewModel.PackageSources.ToList ();
				}
				return packageSources;
			}
		}

		void PopulatePackageSources ()
		{
			foreach (PackageSource packageSource in PackageSources) {
				AddPackageSourceToComboBox (packageSource);
			}

			AddPackageSourceToComboBox (dummyPackageSourceRepresentingConfigureSettingsItem);

			packageSourceComboBox.SelectedItem = viewModel.SelectedPackageSource;
		}

		void AddPackageSourceToComboBox (PackageSource packageSource)
		{
			packageSourceComboBox.Items.Add (packageSource, GetPackageSourceName (packageSource));
		}

		string GetPackageSourceName (PackageSource packageSource)
		{
			if (packageSource.IsAggregate ()) {
				return Catalog.GetString ("All Sources");
			}
			return packageSource.Name;
		}

		void PackageSourceChanged (object sender, EventArgs e)
		{
			var selectedPackageSource = (PackageSource)packageSourceComboBox.SelectedItem;
			if (selectedPackageSource == dummyPackageSourceRepresentingConfigureSettingsItem) {
				ShowPreferencesForPackageSources = true;
				Close ();
			} else {
				viewModel.SelectedPackageSource = selectedPackageSource;
			}
		}
		
		void PackagesListViewSelectionChanged (object sender, EventArgs e)
		{
			try {
				ShowSelectedPackage ();
			} catch (Exception ex) {
				LoggingService.LogError ("Error showing selected package.", ex);
				ShowErrorMessage (ex.Message);
			}
		}

		void ShowSelectedPackage ()
		{
			PackageViewModel packageViewModel = GetSelectedPackageViewModel ();
			if (packageViewModel != null) {
				ShowPackageInformation (packageViewModel);
			} else {
				ClearSelectedPackageInformation ();
			}
			UpdateAddPackagesButton ();
		}

		PackageViewModel GetSelectedPackageViewModel ()
		{
			if (packagesListView.SelectedRow != -1) {
				return packageStore.GetValue (packagesListView.SelectedRow, packageViewModelField);
			}
			return null;
		}

		void ShowPackageInformation (PackageViewModel packageViewModel)
		{
			this.packageNameLabel.Markup = packageViewModel.GetNameMarkup ();
			this.packageVersionLabel.Text = packageViewModel.Version.ToString ();
			this.packageAuthor.Text = packageViewModel.GetAuthors ();
			this.packagePublishedDate.Text = packageViewModel.GetLastPublishedDisplayText ();
			this.packageDownloads.Text = packageViewModel.GetDownloadCountDisplayText ();
			this.packageDescription.Text = packageViewModel.Description;
			this.packageId.Text = packageViewModel.Id;
			this.packageId.Visible = packageViewModel.HasNoGalleryUrl;
			ShowUri (this.packageIdLink, packageViewModel.GalleryUrl, packageViewModel.Id);
			ShowUri (this.packageProjectPageLink, packageViewModel.ProjectUrl);
			ShowUri (this.packageLicenseLink, packageViewModel.LicenseUrl);
			this.packageDependenciesListHBox.Visible = packageViewModel.HasDependencies;
			this.packageDependenciesNoneLabel.Visible = !packageViewModel.HasDependencies;
			this.packageDependenciesList.Text = packageViewModel.GetPackageDependenciesDisplayText ();

			this.packageInfoVBox.Visible = true;
		}

		void ShowUri (LinkLabel linkLabel, Uri uri, string label)
		{
			linkLabel.Text = label;
			ShowUri (linkLabel, uri);
		}

		void ShowUri (LinkLabel linkLabel, Uri uri)
		{
			if (uri == null) {
				linkLabel.Visible = false;
			} else {
				linkLabel.Visible = true;
				linkLabel.Uri = uri;
			}
		}

		void ViewModelPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			try {
				ShowPackages ();
			} catch (Exception ex) {
				LoggingService.LogError ("Error showing packages.", ex);
				ShowErrorMessage (ex.Message);
			}
		}

		void ShowPackages ()
		{
			if (viewModel.HasError) {
				ShowErrorMessage (viewModel.ErrorMessage);
			} else {
				ClearErrorMessage ();
			}

			if (viewModel.IsLoadingNextPage) {
				// Show spinner?
			} else if (viewModel.IsReadingPackages) {
				ClearPackages ();
			} else {
				HideLoadingMessage ();
			}

			if (!viewModel.IsLoadingNextPage) {
				AppendPackagesToListView ();
			}

			UpdateAddPackagesButton ();
		}

		void ClearPackages ()
		{
			packageStore.Clear ();
			ResetPackagesListViewScroll ();
			UpdatePackageListViewSelectionColor ();
			ShowLoadingMessage ();
			ShrinkImageCache ();
		}

		void ResetPackagesListViewScroll ()
		{
			packagesListView.VerticalScrollControl.Value = 0;
		}

		void ShowErrorMessage (string message)
		{
			errorMessageLabel.Text = message;
			errorMessageHBox.Visible = true;
		}

		void ClearErrorMessage ()
		{
			errorMessageHBox.Visible = false;
			errorMessageLabel.Text = "";
		}

		void ShrinkImageCache ()
		{
			imageLoader.ShrinkImageCache ();
		}

		void AppendPackagesToListView ()
		{
			bool packagesListViewWasEmpty = (packageStore.RowCount == 0);

			for (int row = packageStore.RowCount; row < viewModel.PackageViewModels.Count; ++row) {
				PackageViewModel packageViewModel = viewModel.PackageViewModels [row];
				AppendPackageToListView (packageViewModel);
				LoadPackageImage (row, packageViewModel);
			}

			if (packagesListViewWasEmpty && (packageStore.RowCount > 0)) {
				packagesListView.SelectRow (0);
			}

			if (!viewModel.IsReadingPackages && (packageStore.RowCount == 0)) {
				ShowNoPackagesFoundMessage ();
			}
		}

		void AppendPackageToListView (PackageViewModel packageViewModel)
		{
			int row = packageStore.AddRow ();
			packageStore.SetValue (row, packageHasBackgroundColorField, IsOddRow (row));
			packageStore.SetValue (row, packageCheckBoxAlphaField, GetPackageCheckBoxAlpha ());
			packageStore.SetValue (row, packageViewModelField, packageViewModel);
		}

		void LoadPackageImage (int row, PackageViewModel packageViewModel)
		{
			if (packageViewModel.HasIconUrl) {
				// Workaround: Image loading is incorrectly being done on GUI thread
				// since the wrong synchronization context seems to be used. So
				// here we switch to a background thread and then back to the GUI thread.
				DispatchService.BackgroundDispatch (() => LoadImage (packageViewModel.IconUrl, row));
			}
		}

		void LoadImage (Uri iconUrl, int row)
		{
			// Put it back on the GUI thread so the correct synchronization context
			// is used. The image loading will be done on a background thread.
			DispatchService.GuiDispatch (() => imageLoader.LoadFrom (iconUrl, row));
		}

		bool IsOddRow (int row)
		{
			return (row % 2) == 0;
		}

		double GetPackageCheckBoxAlpha ()
		{
			if (PackagesCheckedCount == 0) {
				return packageCheckBoxSemiTransarentAlpha;
			}
			return 1;
		}

		void ImageLoaded (object sender, ImageLoadedEventArgs e)
		{
			if (!e.HasError) {
				int row = (int)e.State;
				if (IsValidRowAndUrl (row, e.Uri)) {
					packageStore.SetValue (row, packageImageField, e.Image);
				}
			}
		}

		bool IsValidRowAndUrl (int row, Uri uri)
		{
			if (row < packageStore.RowCount) {
				PackageViewModel packageViewModel = packageStore.GetValue (row, packageViewModelField);
				if (packageViewModel != null) {
					return uri == packageViewModel.IconUrl;
				}
			}
			return false;
		}

		void AddPackagesButtonClicked (object sender, EventArgs e)
		{
			try {
				List<IPackageAction> packageActions = CreateInstallPackageActionsForSelectedPackages ();
				InstallPackages (packageActions);
			} catch (Exception ex) {
				LoggingService.LogError ("Adding packages failed.", ex);
				ShowErrorMessage (ex.Message);
			}
		}

		void InstallPackages (List<IPackageAction> packageActions)
		{
			if (packageActions.Count > 0) {
				AddRecentPackages (packageActions);

				ProgressMonitorStatusMessage progressMessage = GetProgressMonitorStatusMessages (packageActions);
				backgroundActionRunner.Run (progressMessage, packageActions);
				Close ();
			}
		}

		void AddRecentPackages (List<IPackageAction> packageActions)
		{
			foreach (InstallPackageAction action in packageActions.OfType<InstallPackageAction> ()) {
				recentPackageRepository.AddPackage (action.Package);
			}
		}

		List<IPackageAction> CreateInstallPackageActionsForSelectedPackages ()
		{
			List<PackageViewModel> packageViewModels = GetSelectedPackageViewModels ();
			if (packageViewModels.Count > 0) {
				return CreateInstallPackageActions (packageViewModels);
			}
			return new List<IPackageAction> ();
		}

		ProgressMonitorStatusMessage GetProgressMonitorStatusMessages (List<IPackageAction> packageActions)
		{
			if (packageActions.Count == 1) {
				string packageId = packageActions.OfType<ProcessPackageAction> ().First ().Package.Id;
				if (OlderPackageInstalledThanPackageSelected ()) {
					return ProgressMonitorStatusMessageFactory.CreateUpdatingSinglePackageMessage (packageId);
				}
				return ProgressMonitorStatusMessageFactory.CreateInstallingSinglePackageMessage (packageId);
			}
			return ProgressMonitorStatusMessageFactory.CreateInstallingMultiplePackagesMessage (packageActions.Count);
		}

		List<PackageViewModel> GetSelectedPackageViewModels ()
		{
			List<PackageViewModel> packageViewModels = viewModel.CheckedPackageViewModels.ToList ();
			if (packageViewModels.Count > 0) {
				return packageViewModels;
			}

			PackageViewModel selectedPackageViewModel = GetSelectedPackageViewModel ();
			if (selectedPackageViewModel != null) {
				packageViewModels.Add (selectedPackageViewModel);
			}
			return packageViewModels;
		}

		List<IPackageAction> CreateInstallPackageActions (IEnumerable<PackageViewModel> packageViewModels)
		{
			return packageViewModels.Select (viewModel => viewModel.CreateInstallPackageAction ()).ToList ();
		}

		void PackageSearchEntryChanged (object sender, EventArgs e)
		{
			ClearErrorMessage ();
			ClearPackages ();
			UpdateAddPackagesButton ();
			SearchAfterDelay ();
		}

		void SearchAfterDelay ()
		{
			DisposeExistingTimer ();
			searchTimer = Application.TimeoutInvoke (searchDelayTimeSpan, Search);
		}

		void DisposeExistingTimer ()
		{
			if (searchTimer != null) {
				searchTimer.Dispose ();
			}
		}

		bool Search ()
		{
			viewModel.SearchTerms = this.packageSearchEntry.Text;
			viewModel.SearchCommand.Execute (null);

			return false;
		}

		void PackagesListRowActivated (object sender, ListViewRowEventArgs e)
		{
			if (PackagesCheckedCount > 0) {
				AddPackagesButtonClicked (sender, e);
			} else {
				PackageViewModel packageViewModel = packageStore.GetValue (e.RowIndex, packageViewModelField);
				InstallPackage (packageViewModel);
			}
		}

		void InstallPackage (PackageViewModel packageViewModel)
		{
			try {
				if (packageViewModel != null) {
					List<IPackageAction> packageActions = CreateInstallPackageActions (new PackageViewModel [] { packageViewModel });
					InstallPackages (packageActions);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Installing package failed.", ex);
				ShowErrorMessage (ex.Message);
			}
		}

		void PackageSearchEntryActivated (object sender, EventArgs e)
		{
			if (loadingMessageVisible)
				return;

			if (PackagesCheckedCount > 0) {
				AddPackagesButtonClicked (sender, e);
			} else {
				PackageViewModel selectedPackageViewModel = GetSelectedPackageViewModel ();
				InstallPackage (selectedPackageViewModel);
			}
		}

		void PackagesListViewScrollValueChanged (object sender, EventArgs e)
		{
			if (viewModel.IsLoadingNextPage) {
				return;
			}

			if (IsScrollBarNearEnd (packagesListView.VerticalScrollControl)) {
				if (viewModel.HasNextPage) {
					viewModel.ShowNextPage ();
				}
			}
		}

		bool IsScrollBarNearEnd (ScrollControl scrollControl)
		{
			double currentValue = scrollControl.Value;
			double maxValue = scrollControl.UpperValue;
			double pageSize = scrollControl.PageSize;

			return (currentValue / (maxValue - pageSize)) > 0.7;
		}

		void PackageCellViewPackageChecked (object sender, PackageCellViewEventArgs e)
		{
			UpdateAddPackagesButton ();
			UpdatePackageListViewSelectionColor ();
			UpdatePackageListViewCheckBoxAlpha ();
		}

		void UpdateAddPackagesButton ()
		{
			if (PackagesCheckedCount > 1) {
				addPackagesButton.Label = Catalog.GetString ("Add Packages");
			} else {
				if (OlderPackageInstalledThanPackageSelected ()) {
					addPackagesButton.Label = Catalog.GetString ("Update Package");
				} else {
					addPackagesButton.Label = Catalog.GetString ("Add Package");
				}
			}
			addPackagesButton.Sensitive = IsAddPackagesButtonEnabled ();
		}

		void UpdatePackageListViewSelectionColor ()
		{
			packageCellView.UseStrongSelectionColor = (PackagesCheckedCount == 0);
		}

		void UpdatePackageListViewCheckBoxAlpha ()
		{
			if (PackagesCheckedCount > 1)
				return;

			double alpha = GetPackageCheckBoxAlpha ();
			for (int row = 0; row < packageStore.RowCount; ++row) {
				packageStore.SetValue (row, packageCheckBoxAlphaField, alpha);
			}
		}

		bool OlderPackageInstalledThanPackageSelected ()
		{
			if (PackagesCheckedCount != 0) {
				return false;
			}

			PackageViewModel selectedPackageViewModel = GetSelectedPackageViewModel ();
			if (selectedPackageViewModel != null) {
				return selectedPackageViewModel.IsOlderPackageInstalled ();
			}
			return false;
		}

		bool IsAddPackagesButtonEnabled ()
		{
			return !loadingMessageVisible && IsAtLeastOnePackageSelected ();
		}

		bool IsAtLeastOnePackageSelected ()
		{
			return (PackagesCheckedCount) >= 1 || (packagesListView.SelectedRow != -1);
		}

		int PackagesCheckedCount {
			get { return viewModel.CheckedPackageViewModels.Count; }
		}
	}
}