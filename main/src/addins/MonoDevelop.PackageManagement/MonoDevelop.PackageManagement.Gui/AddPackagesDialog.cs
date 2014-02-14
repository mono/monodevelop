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
		IPackageActionRunner backgroundActionRunner;
		PackagesViewModel viewModel;
		List<PackageSource> packageSources;
		DataField<bool> packageCheckBoxActiveField = new DataField<bool> ();
		DataField<bool> packageCheckBoxVisibleField = new DataField<bool> ();
		DataField<Image> packageIconField = new DataField<Image> ();
		DataField<string> packageDescriptionField = new DataField<string> ();
		DataField<PackageViewModel> packageViewModelField = new DataField<PackageViewModel> ();
		ListStore packageStore;
		Image defaultPackageImage;
		TimeSpan searchDelayTimeSpan = TimeSpan.FromMilliseconds (500);
		IDisposable searchTimer;

		public AddPackagesDialog (PackagesViewModel viewModel)
			: this (viewModel, PackageManagementServices.BackgroundPackageActionRunner)
		{
		}

		public AddPackagesDialog (PackagesViewModel viewModel, IPackageActionRunner backgroundActionRunner)
		{
			this.viewModel = viewModel;
			this.backgroundActionRunner = backgroundActionRunner;

			Build ();

			InitializeListView ();
			LoadViewModel ();

			this.showPrereleaseCheckBox.Clicked += ShowPrereleaseCheckBoxClicked;
			this.packageSourceComboBox.SelectionChanged += PackageSourceChanged;
			this.addPackagesButton.Clicked += AddPackagesButtonClicked;
			this.packageSearchEntry.Changed += PackageSearchEntryChanged;
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
			packageStore = new ListStore (packageCheckBoxActiveField, packageCheckBoxVisibleField, packageIconField, packageDescriptionField, packageViewModelField);
			packagesListView.DataSource = packageStore;

			AddPackageCheckBoxColumnToListView ();
			packagesListView.Columns.Add ("Icon", packageIconField);
			AddPackageDescriptionColumnToListView ();

			packagesListView.SelectionChanged += PackagesListViewSelectionChanged;

			defaultPackageImage = Image.FromResource (typeof(AddPackagesDialog), "packageicon.png");

			AddSearchingMessageToListView ();
		}

		void AddPackageCheckBoxColumnToListView ()
		{
			var checkBoxCellView = new CheckBoxCellView {
				ActiveField = packageCheckBoxActiveField,
				Editable = true,
				VisibleField = packageCheckBoxVisibleField
			};
			//checkBoxCellView.Toggled
			var checkBoxColumn = new ListViewColumn ("Checked", checkBoxCellView);
			packagesListView.Columns.Add (checkBoxColumn);
		}

		void AddPackageDescriptionColumnToListView ()
		{
			var textCellView = new TextCellView {
				MarkupField = packageDescriptionField,
			};
			var textColumn = new ListViewColumn ("Text", textCellView);
			packagesListView.Columns.Add (textColumn);
		}

		void AddSearchingMessageToListView ()
		{
			AddMessageToListView (StockIcons.Information, Catalog.GetString ("Searching..."));
		}

		void AddMessageToListView (Image image, string message)
		{
			int row = packageStore.AddRow ();
			packageStore.SetValue (row, packageIconField, defaultPackageImage);
			//packageStore.SetValue (row, packageIconField, image);
			packageStore.SetValue (row, packageDescriptionField, message);
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
			this.packageNameLabel.Markup = GetBoldText (packageViewModel.Name);
			this.packageVersionLabel.Markup = GetBoldText (packageViewModel.Version.ToString ());
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

		string GetBoldText (string text)
		{
			return String.Format ("<b>{0}</b>", text);
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
				AddErrorToTreeView ();
			}

			if (viewModel.IsReadingPackages) {
				AddSearchingMessageToListView ();
			}

			foreach (PackageViewModel packageViewModel in viewModel.PackageViewModels) {
				AppendPackageToListView (packageViewModel);
			}

			if (viewModel.PackageViewModels.Any ()) {
				packagesListView.SelectRow (0);
			}
		}

		void AddErrorToTreeView ()
		{
			AddMessageToListView (StockIcons.Error, viewModel.ErrorMessage);
		}

		void AppendPackageToListView (PackageViewModel packageViewModel)
		{
			int row = packageStore.AddRow ();
			packageStore.SetValue (row, packageCheckBoxVisibleField, true);
			packageStore.SetValue (row, packageCheckBoxActiveField, false);
			packageStore.SetValue (row, packageIconField, defaultPackageImage);
			packageStore.SetValue (row, packageDescriptionField, packageViewModel.GetDisplayTextMarkup ());
			packageStore.SetValue (row, packageViewModelField, packageViewModel);
		}

		void AddPackagesButtonClicked (object sender, EventArgs e)
		{
			List<IPackageAction> packageActions = CreateInstallPackageActionsForSelectedPackages ();
			if (packageActions.Count > 0) {
				backgroundActionRunner.Run (packageActions);
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

		List<IPackageAction> CreateInstallPackageActions (List<PackageViewModel> packageViewModels)
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
	}
}