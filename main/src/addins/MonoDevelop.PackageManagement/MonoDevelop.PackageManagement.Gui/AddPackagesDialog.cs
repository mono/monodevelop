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
using NuGet;
using Xwt;
using Xwt.Drawing;
using Xwt.Formats;

namespace MonoDevelop.PackageManagement
{
	public partial class AddPackagesDialog
	{
		IBackgroundPackageActionRunner backgroundActionRunner;
		PackagesViewModel viewModel;
		List<PackageSource> packageSources;
		DataField<bool> packageCheckBoxActiveField = new DataField<bool> ();
		DataField<bool> packageCheckBoxVisibleField = new DataField<bool> ();
		DataField<Image> packageIconField = new DataField<Image> ();
		DataField<PackageViewModel> packageViewModelField = new DataField<PackageViewModel> ();
		ListStore packageStore;
		Image defaultPackageImage;
		TimeSpan searchDelayTimeSpan = TimeSpan.FromMilliseconds (500);
		IDisposable searchTimer;

		public AddPackagesDialog (PackagesViewModel viewModel)
			: this (viewModel, PackageManagementServices.BackgroundPackageActionRunner)
		{
		}

		public AddPackagesDialog (PackagesViewModel viewModel, IBackgroundPackageActionRunner backgroundActionRunner)
		{
			this.viewModel = viewModel;
			this.backgroundActionRunner = backgroundActionRunner;

			Build ();

			InitializeListView ();
			ShowLoadingMessage ();
			LoadViewModel ();

			this.showPrereleaseCheckBox.Clicked += ShowPrereleaseCheckBoxClicked;
			this.packageSourceComboBox.SelectionChanged += PackageSourceChanged;
			this.addPackagesButton.Clicked += AddPackagesButtonClicked;
			this.packageSearchEntry.Changed += PackageSearchEntryChanged;
			this.packageSearchEntry.Activated += PackageSearchEntryActivated;
		}

		protected override void Dispose (bool disposing)
		{
			viewModel.PropertyChanged -= ViewModelPropertyChanged;
			viewModel.Dispose ();
			DisposeExistingTimer ();
			base.Dispose (disposing);
		}

		void InitializeListView ()
		{
			packageStore = new ListStore (packageCheckBoxActiveField, packageCheckBoxVisibleField, packageIconField, packageViewModelField);
			packagesListView.DataSource = packageStore;

			AddPackageCheckBoxColumnToListView ();
			packagesListView.Columns.Add ("Icon", packageIconField);
			AddPackageDescriptionColumnToListView ();

			packagesListView.SelectionChanged += PackagesListViewSelectionChanged;
			packagesListView.RowActivated += PackagesListRowActivated;

			defaultPackageImage = Image.FromResource (typeof(AddPackagesDialog), "packageicon.png");
		}

		void AddPackageCheckBoxColumnToListView ()
		{
			var checkBoxCellView = new CheckBoxCellView {
				ActiveField = packageCheckBoxActiveField,
				Editable = true,
				VisibleField = packageCheckBoxVisibleField
			};
			var checkBoxColumn = new ListViewColumn ("Checked", checkBoxCellView);
			packagesListView.Columns.Add (checkBoxColumn);
		}

		void AddPackageDescriptionColumnToListView ()
		{
			var packageCellView = new PackageCellView {
				PackageField = packageViewModelField
			};
			var textColumn = new ListViewColumn ("Package", packageCellView);
			packagesListView.Columns.Add (textColumn);
		}

		void ShowLoadingMessage ()
		{
			packagesListView.Visible = false;
			loadingSpinnerFrame.Visible = true;
		}

		void HideLoadingMessage ()
		{
			loadingSpinnerFrame.Visible = false;
			packagesListView.Visible = true;
		}

		void ShowPrereleaseCheckBoxClicked (object sender, EventArgs e)
		{
			viewModel.IncludePrerelease = !viewModel.IncludePrerelease;
		}

		void LoadViewModel ()
		{
			ClearSelectedPackageInformation ();
			PopulatePackageSources ();
			viewModel.PropertyChanged += ViewModelPropertyChanged;

			viewModel.ReadPackages ();
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
				packageSourceComboBox.Items.Add (packageSource, GetPackageSourceName (packageSource));
			}

			this.packageSourceComboBox.SelectedItem = viewModel.SelectedPackageSource;
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
			viewModel.SelectedPackageSource = (PackageSource)packageSourceComboBox.SelectedItem;
		}
		
		void PackagesListViewSelectionChanged (object sender, EventArgs e)
		{
			ShowSelectedPackage ();
		}

		void ShowSelectedPackage ()
		{
			PackageViewModel packageViewModel = GetSelectedPackageViewModel ();
			if (packageViewModel != null) {
				ShowPackageInformation (packageViewModel);
			} else {
				ClearSelectedPackageInformation ();
			}
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
			this.packageVersionLabel.Markup = packageViewModel.GetVersionMarkup ();
			this.packageAuthor.Text = packageViewModel.GetAuthors ();
			this.packagePublishedDate.Text = packageViewModel.GetLastPublishedDisplayText ();
			this.packageDownloads.Text = packageViewModel.GetDownloadCountDisplayText ();
			this.packageDescription.LoadText (packageViewModel.Description, TextFormat.Plain);
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
			this.packageStore.Clear ();

			if (viewModel.HasError) {
				ShowErrorMessage ();
			} else {
				ClearErrorMessage ();
			}

			if (viewModel.IsReadingPackages) {
				ShowLoadingMessage ();
			} else {
				HideLoadingMessage ();
			}

			foreach (PackageViewModel packageViewModel in viewModel.PackageViewModels) {
				AppendPackageToListView (packageViewModel);
			}

			if (viewModel.PackageViewModels.Any ()) {
				packagesListView.SelectRow (0);
			}
		}

		void ShowErrorMessage ()
		{
			errorMessageLabel.Text = viewModel.ErrorMessage;
			errorMessageHBox.Visible = true;
		}

		void ClearErrorMessage ()
		{
			errorMessageHBox.Visible = false;
			errorMessageLabel.Text = "";
		}

		void AppendPackageToListView (PackageViewModel packageViewModel)
		{
			int row = packageStore.AddRow ();
			packageStore.SetValue (row, packageCheckBoxVisibleField, true);
			packageStore.SetValue (row, packageCheckBoxActiveField, false);
			packageStore.SetValue (row, packageIconField, defaultPackageImage);
			packageStore.SetValue (row, packageViewModelField, packageViewModel);
		}

		void AddPackagesButtonClicked (object sender, EventArgs e)
		{
			List<IPackageAction> packageActions = CreateInstallPackageActionsForSelectedPackages ();
			InstallPackages (packageActions);
		}

		void InstallPackages (List<IPackageAction> packageActions)
		{
			if (packageActions.Count > 0) {
				ProgressMonitorStatusMessage progressMessage = GetProgressMonitorStatusMessages (packageActions);
				backgroundActionRunner.Run (progressMessage, packageActions);
				Close ();
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
				return ProgressMonitorStatusMessageFactory.CreateInstallingSinglePackageMessage (
					packageActions.OfType<ProcessPackageAction> ().First ().Package.Id
				);
			}
			return ProgressMonitorStatusMessageFactory.CreateInstallingMultiplePackagesMessage (packageActions.Count);
		}

		List<PackageViewModel> GetSelectedPackageViewModels ()
		{
			List<PackageViewModel> packageViewModels = GetCheckedPackageViewModels ();
			if (packageViewModels.Count > 0) {
				return packageViewModels;
			}

			PackageViewModel selectedPackageViewModel = GetSelectedPackageViewModel ();
			if (selectedPackageViewModel != null) {
				packageViewModels.Add (selectedPackageViewModel);
			}
			return packageViewModels;
		}

		List<PackageViewModel> GetCheckedPackageViewModels ()
		{
			var packageViewModels = new List<PackageViewModel> ();
			for (int row = 0; row < viewModel.PackageViewModels.Count; ++row) {
				PackageViewModel packageViewModel = viewModel.PackageViewModels [row];
				if (packageStore.GetValue (row, packageCheckBoxActiveField)) {
					packageViewModels.Add (packageViewModel);
				}
			}
			return packageViewModels;
		}

		List<IPackageAction> CreateInstallPackageActions (IEnumerable<PackageViewModel> packageViewModels)
		{
			return packageViewModels.Select (viewModel => viewModel.CreateInstallPackageAction ()).ToList ();
		}

		void PackageSearchEntryChanged (object sender, EventArgs e)
		{
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
			PackageViewModel packageViewModel = packageStore.GetValue (e.RowIndex, packageViewModelField);
			InstallPackage (packageViewModel);
		}

		void InstallPackage (PackageViewModel packageViewModel)
		{
			if (packageViewModel != null) {
				List<IPackageAction> packageActions = CreateInstallPackageActions (new PackageViewModel [] { packageViewModel });
				InstallPackages (packageActions);
			}
		}

		void PackageSearchEntryActivated (object sender, EventArgs e)
		{
			PackageViewModel selectedPackageViewModel = GetSelectedPackageViewModel ();
			InstallPackage (selectedPackageViewModel);
		}
	}
}