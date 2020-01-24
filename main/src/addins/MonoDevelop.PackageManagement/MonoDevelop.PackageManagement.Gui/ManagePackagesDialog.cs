//
// ManagePackagesDialog.cs
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
using System.Linq;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NuGet.Versioning;
using Xwt;
using Xwt.Drawing;
using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;

namespace MonoDevelop.PackageManagement
{
	internal partial class ManagePackagesDialog
	{
		IBackgroundPackageActionRunner backgroundActionRunner;
		ManagePackagesViewModel viewModel;
		List<SourceRepositoryViewModel> packageSources;
		DataField<ManagePackagesSearchResultViewModel> packageViewModelField = new DataField<ManagePackagesSearchResultViewModel> ();
		DataField<Image> packageImageField = new DataField<Image> ();
		DataField<bool> packageCheckBoxField = new DataField<bool> ();
		DataField<string> packageCheckA11yField = new DataField<string> ();
		DataField<string> packageDescriptionA11yField = new DataField<string> ();
		ListStore packageStore;
		ManagePackagesCellView packageCellView;
		CheckBoxCellView packageCheckView;
		TimeSpan searchDelayTimeSpan = TimeSpan.FromMilliseconds (500);
		IDisposable searchTimer;
		SourceRepositoryViewModel dummyPackageSourceRepresentingConfigureSettingsItem =
			new SourceRepositoryViewModel (GettextCatalog.GetString ("Configure Sources..."));
		ImageLoader imageLoader = new ImageLoader ();
		bool loadingMessageVisible;
		bool ignorePackageVersionChanges;
		const string IncludePrereleaseUserPreferenceName = "NuGet.AddPackagesDialog.IncludePrerelease";
		TimeSpan populatePackageVersionsDelayTimeSpan = TimeSpan.FromMilliseconds (500);
		int packageVersionsAddedCount;
		IDisposable populatePackageVersionsTimer;
		const int MaxVersionsToPopulate = 100;
		DataField<bool> projectCheckedField;
		DataField<string> projectNameField;
		DataField<string> packageVersionField;
		DataField<ManageProjectViewModel> projectField;
		CheckBoxCellView projectCheckBoxCellView;
		ListStore projectStore;

		public ManagePackagesDialog (ManagePackagesViewModel viewModel, string initialSearch = null)
			: this (
				viewModel,
				initialSearch,
				PackageManagementServices.BackgroundPackageActionRunner)
		{
		}

		public ManagePackagesDialog (
			ManagePackagesViewModel viewModel,
			string initialSearch,
			IBackgroundPackageActionRunner backgroundActionRunner)
		{
			this.viewModel = viewModel;
			this.backgroundActionRunner = backgroundActionRunner;

			Build ();

			consolidateLabel.Visible = viewModel.IsManagingSolution;
			UpdateDialogTitle ();
			UpdatePackageSearchEntryWithInitialText (initialSearch);

			InitializeListView ();
			UpdateAddPackagesButton ();
			ShowLoadingMessage ();
			LoadViewModel (initialSearch);

			closeButton.Clicked += CloseButtonClicked;
			showPrereleaseCheckBox.Clicked += ShowPrereleaseCheckBoxClicked;
			packageSourceComboBox.SelectionChanged += PackageSourceChanged;
			addPackagesButton.Clicked += AddPackagesButtonClicked;
			packageSearchEntry.Changed += PackageSearchEntryChanged;
			packageVersionComboBox.SelectionChanged += PackageVersionChanged;
			imageLoader.Loaded += ImageLoaded;

			browseLabel.ButtonPressed += BrowseLabelButtonPressed;
			browseLabel.KeyPressed += BrowseLabelKeyPressed;
			installedLabel.ButtonPressed += InstalledLabelButtonPressed;
			installedLabel.KeyPressed += InstalledLabelKeyPressed;
			updatesLabel.ButtonPressed += UpdatesLabelButtonPressed;
			updatesLabel.KeyPressed += UpdatesLabelKeyPressed;
			consolidateLabel.ButtonPressed += ConsolidateLabelButtonPressed;
			consolidateLabel.KeyPressed += ConsolidateLabelKeyPressed;
			UpdateTabAccessibility ();
			UpdatePackageResultsPageLabels ();
		}

		void UpdateTabAccessibility ()
		{
			if (tabGroup.Surface.ToolkitEngine.Type == ToolkitType.Gtk) {
				if (consolidateLabel.Parent.Surface.NativeWidget is Gtk.Container a11yGroup) {
					a11yGroup.Accessible.SetRole (AtkCocoa.Roles.AXTabGroup);
					var children = a11yGroup.Children;
					var tabs = new List<Atk.Object> (children.Length);
					foreach (var child in children) {
						if (!child.Visible)
							continue;
						if (child is Gtk.EventBox box && box.Child is Gtk.Label) {
							box.Accessible.SetTitleUIElement (box.Child.Accessible);
							box.Child.Accessible.SetShouldIgnore (true);
						}
						var tab = child.Accessible;
						tab.SetRole (AtkCocoa.Roles.AXRadioButton);
						tab.SetSubRole (AtkCocoa.SubRoles.AXTabButton);
						tab.SetValue (false);
						tabs.Add (tab);
					}
					a11yGroup.Accessible.SetTabs (tabs.ToArray ());
				}
			}
		}

		public bool ShowPreferencesForPackageSources { get; private set; }

		protected override void Dispose (bool disposing)
		{
			closeButton.Clicked -= CloseButtonClicked;
			currentPackageVersionLabel.BoundsChanged -= PackageVersionLabelBoundsChanged;

			showPrereleaseCheckBox.Clicked -= ShowPrereleaseCheckBoxClicked;
			packageSourceComboBox.SelectionChanged -= PackageSourceChanged;
			addPackagesButton.Clicked -= AddPackagesButtonClicked;
			packageSearchEntry.Changed -= PackageSearchEntryChanged;
			packageVersionComboBox.SelectionChanged -= PackageVersionChanged;

			browseLabel.ButtonPressed -= BrowseLabelButtonPressed;
			browseLabel.KeyPressed -= BrowseLabelKeyPressed;
			installedLabel.ButtonPressed -= InstalledLabelButtonPressed;
			installedLabel.KeyPressed -= InstalledLabelKeyPressed;
			updatesLabel.ButtonPressed -= UpdatesLabelButtonPressed;
			updatesLabel.KeyPressed -= UpdatesLabelKeyPressed;
			consolidateLabel.ButtonPressed -= ConsolidateLabelButtonPressed;
			consolidateLabel.KeyPressed -= ConsolidateLabelKeyPressed;

			imageLoader.Loaded -= ImageLoaded;
			imageLoader.Dispose ();

			RemoveSelectedPackagePropertyChangedEventHandler ();
			viewModel.PropertyChanged -= ViewModelPropertyChanged;
			viewModel.Dispose ();
			DisposeExistingTimer ();
			DisposePopulatePackageVersionsTimer ();
			packageStore.Clear ();
			projectStore?.Clear ();
			viewModel = null;

			base.Dispose (disposing);
		}

		void UpdateDialogTitle ()
		{
			if (viewModel.IsManagingSolution)
				return;

			Title = GettextCatalog.GetString ("Manage NuGet Packages – {0}", viewModel.Project.Name);
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
			packageStore = new ListStore (packageImageField, packageViewModelField, packageCheckBoxField, packageCheckA11yField, packageDescriptionA11yField);
			packagesListView.DataSource = packageStore;

			AddCellViewsToListView ();
						
			packagesListView.SelectionChanged += PackagesListViewSelectionChanged;
			packagesListView.RowActivated += PackagesListRowActivated;
			packagesListView.VerticalScrollControl.ValueChanged += PackagesListViewScrollValueChanged;

			if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk) {
				// TODO: unlike Xwt.TreeView, Xwt.ListView has no UseAlternatingRowColors property
				var gtkTreeView = packagesListView.Surface.NativeWidget as Gtk.Widget;
				if (gtkTreeView is Gtk.ScrolledWindow scroll)
					gtkTreeView = scroll.Child;
				if (gtkTreeView is Gtk.TreeView tree)
					tree.RulesHint = true;
			}
		}

		void AddCellViewsToListView ()
		{
			var checkColumn = new ListViewColumn (GettextCatalog.GetString ("Add Package"));

			packageCheckView = new CheckBoxCellView (packageCheckBoxField) { Editable = true };
			packageCheckView.AccessibleFields.Label = packageCheckA11yField;
			packageCheckView.Toggled += PackageCheckCellViewPackageChecked;

			// HACK: Xwt has no custom cell padding, so we need to add an empty label for spacing
			var spaceText = new AccessibleSpacerCellView ();
			spaceText.AccessibleFields.Label = packageCheckA11yField;
			checkColumn.Views.Add (spaceText);
			checkColumn.Views.Add (packageCheckView);
			packagesListView.Columns.Add (checkColumn);

			packageCellView = new ManagePackagesCellView {
				PackageField = packageViewModelField,
				ImageField = packageImageField,
				CellWidth = 446
			};
			packageCellView.AccessibleFields.Label = packageDescriptionA11yField;

			var textColumn = new ListViewColumn ("Package", packageCellView);
			packagesListView.Columns.Add (textColumn);
		}

		void InitializeProjectsListView ()
		{
			projectStore?.Clear ();

			// Recreate the list view each time. This is a workaround for the
			// list view not displaying items on re-populating if it has been sorted.
			if (projectsListView != null) {
				projectsListViewVBox.Remove (projectsListView);
				projectsListView.Dispose ();
			}

			projectStore?.Dispose ();

			projectCheckedField = new DataField<bool> ();
			projectNameField = new DataField<string> ();
			packageVersionField = new DataField<string> ();
			projectField = new DataField<ManageProjectViewModel> ();
			projectStore = new ListStore (projectCheckedField, projectNameField, packageVersionField, projectField);

			projectsListView = new ListView ();
			projectsListView.DataSource = projectStore;
			projectsListView.Accessible.LabelWidget = projectsListViewLabel;

			// Selected project check box column.
			if (projectCheckBoxCellView != null)
				projectCheckBoxCellView.Toggled -= ProjectCheckBoxCellViewToggled;
			projectCheckBoxCellView = new CheckBoxCellView ();
			projectCheckBoxCellView.ActiveField = projectCheckedField;
			projectCheckBoxCellView.Editable = true;
			projectCheckBoxCellView.Toggled += ProjectCheckBoxCellViewToggled;
			var column = new ListViewColumn (GettextCatalog.GetString ("Project"), projectCheckBoxCellView) {
				CanResize = true,
				SortDataField = projectNameField
			};

			// Project name.
			var textCellView = new TextCellView ();
			textCellView.TextField = projectNameField;
			column.Views.Add (textCellView);
			projectsListView.Columns.Add (column);

			// Package version column
			textCellView = new TextCellView ();
			textCellView.TextField = packageVersionField;
			column = new ListViewColumn (GettextCatalog.GetString ("Version"), textCellView) {
				CanResize = true,
				SortDataField = packageVersionField
			};
			projectsListView.Columns.Add (column);

			// Add list view to dialog.
			projectsListViewVBox.PackStart (projectsListView, true, true);
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
				loadingSpinnerLabel.Text = GettextCatalog.GetString ("Loading package list...");
			} else {
				loadingSpinnerLabel.Text = GettextCatalog.GetString ("Searching packages...");
			}
		}

		void ShowNoPackagesFoundMessage ()
		{
			if (!String.IsNullOrWhiteSpace (packageSearchEntry.Text)) {
				packagesListView.Visible = false;
				noPackagesFoundFrame.Visible = true;

				IdeApp.Workbench.RootWindow.Accessible.MakeAccessibilityAnnouncement (noPackagesFoundLabel.Text);
			}
		}

		void CloseButtonClicked (object sender, EventArgs e)
		{
			Close ();
		}

		void ShowPrereleaseCheckBoxClicked (object sender, EventArgs e)
		{
			viewModel.IncludePrerelease = !viewModel.IncludePrerelease;

			SaveIncludePrereleaseUserPreference ();
		}

		void SaveIncludePrereleaseUserPreference ()
		{
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution != null) {
				if (viewModel.IncludePrerelease) {
					solution.UserProperties.SetValue (IncludePrereleaseUserPreferenceName, viewModel.IncludePrerelease);
				} else {
					solution.UserProperties.RemoveValue (IncludePrereleaseUserPreferenceName);
				}
				solution.SaveUserProperties ();
			}
		}

		bool GetIncludePrereleaseUserPreference ()
		{
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution != null) {
				return solution.UserProperties.GetValue (IncludePrereleaseUserPreferenceName, false);
			}

			return false;
		}

		void LoadViewModel (string initialSearch)
		{
			viewModel.SearchTerms = initialSearch;

			viewModel.IncludePrerelease = GetIncludePrereleaseUserPreference ();
			showPrereleaseCheckBox.Active = viewModel.IncludePrerelease;

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
			this.currentPackageVersionHBox.Visible = false;
			this.packageVersionsHBox.Visible = false;
			projectStore?.Clear ();
		}

		void RemoveSelectedPackagePropertyChangedEventHandler ()
		{
			if (viewModel.SelectedPackage != null) {
				viewModel.SelectedPackage.PropertyChanged -= SelectedPackageViewModelChanged;
				viewModel.SelectedPackage = null;
			}
		}

		List<SourceRepositoryViewModel> PackageSources {
			get {
				if (packageSources == null) {
					packageSources = viewModel.PackageSources.ToList ();
				}
				return packageSources;
			}
		}

		void PopulatePackageSources ()
		{
			foreach (SourceRepositoryViewModel packageSource in PackageSources) {
				AddPackageSourceToComboBox (packageSource);
			}

			AddPackageSourceToComboBox (dummyPackageSourceRepresentingConfigureSettingsItem);

			packageSourceComboBox.SelectedItem = viewModel.SelectedPackageSource;
		}

		void AddPackageSourceToComboBox (SourceRepositoryViewModel packageSource)
		{
			packageSourceComboBox.Items.Add (packageSource, packageSource.Name);
		}

		void PackageSourceChanged (object sender, EventArgs e)
		{
			var selectedPackageSource = (SourceRepositoryViewModel)packageSourceComboBox.SelectedItem;
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
			RemoveSelectedPackagePropertyChangedEventHandler ();

			ManagePackagesSearchResultViewModel packageViewModel = GetSelectedPackageViewModel ();
			viewModel.SelectedPackage = packageViewModel;
			if (packageViewModel != null) {
				ShowPackageInformation (packageViewModel);
			} else {
				ClearSelectedPackageInformation ();
			}
			UpdateAddPackagesButton ();
		}

		ManagePackagesSearchResultViewModel GetSelectedPackageViewModel ()
		{
			if (packagesListView.SelectedRow != -1) {
				return packageStore.GetValue (packagesListView.SelectedRow, packageViewModelField);
			}
			return null;
		}

		void ShowPackageInformation (ManagePackagesSearchResultViewModel packageViewModel)
		{
			bool consolidate = viewModel.IsConsolidatePageSelected;

			if (consolidate) {
				projectsListViewLabel.Text = GettextCatalog.GetString ("Select projects and a version for a consolidation.");
			} else {
				// Use the package id and not the package title to prevent a pango crash if the title
				// contains Chinese characters.
				this.packageNameLabel.Markup = packageViewModel.GetIdMarkup ();
				this.packageAuthor.Text = packageViewModel.Author;
				this.packagePublishedDate.Text = packageViewModel.GetLastPublishedDisplayText ();
				this.packageDownloads.Text = packageViewModel.GetDownloadCountDisplayText ();
				this.packageDescription.Text = packageViewModel.Description;
				this.packageId.Text = packageViewModel.Id;
				this.packageId.Visible = packageViewModel.HasNoGalleryUrl;
				ShowUri (this.packageIdLink, packageViewModel.GalleryUrl, packageViewModel.Id);
				ShowUri (this.packageProjectPageLink, packageViewModel.ProjectUrl);
				ShowUri (this.packageLicenseLink, packageViewModel.LicenseUrl);

				PopulatePackageDependencies (packageViewModel);
			}

			if (viewModel.IsInstalledPageSelected) {
				packageVersionsLabel.WidthRequest = -1;
				currentPackageVersionHBox.Visible = false;
				packageVersionsHBox.Visible = false;
			} else if (viewModel.IsUpdatesPageSelected) {
				PopulatePackageVersions (packageViewModel);
				ShowCurrentPackageVersion (packageViewModel);
				packageVersionsHBox.Visible = true;
			} else {
				packageVersionsLabel.WidthRequest = -1;
				currentPackageVersionHBox.Visible = false;
				PopulatePackageVersions (packageViewModel);
				packageVersionsHBox.Visible = true;
			}

			foreach (Widget child in packageInfoVBox.Children) {
				child.Visible = !consolidate;
			}

			if (consolidate) {
				PopulateProjectList ();
			} else {
				projectStore?.Clear ();
			}

			projectsListViewLabel.Visible = consolidate;
			projectsListViewVBox.Visible = consolidate;
			this.packageInfoVBox.Visible = true;

			packageViewModel.PropertyChanged += SelectedPackageViewModelChanged;
			viewModel.LoadPackageMetadata (packageViewModel);
		}

		void ShowCurrentPackageVersion (ManagePackagesSearchResultViewModel packageViewModel)
		{
			if (maxPackageVersionLabelWidth.HasValue) {
				currentPackageVersionLabel.WidthRequest = maxPackageVersionLabelWidth.Value;
				packageVersionsLabel.WidthRequest = maxPackageVersionLabelWidth.Value;
			}

			currentPackageVersion.Text = packageViewModel.GetCurrentPackageVersionText ();

			currentPackageVersionInfoPopoverWidget.Message = packageViewModel.GetCurrentPackageVersionAdditionalText ();
			currentPackageVersionInfoPopoverWidget.Visible = !string.IsNullOrEmpty (currentPackageVersionInfoPopoverWidget.Message);

			currentPackageVersionHBox.Visible = !string.IsNullOrEmpty (currentPackageVersion.Text);
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
				IdeApp.Workbench.RootWindow.Accessible.MakeAccessibilityAnnouncement (loadingSpinnerLabel.Text);
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
			ShowLoadingMessage ();
			ShrinkImageCache ();
			DisposePopulatePackageVersionsTimer ();
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
				ManagePackagesSearchResultViewModel packageViewModel = viewModel.PackageViewModels [row];
				AppendPackageToListView (packageViewModel);
				LoadPackageImage (row, packageViewModel);
			}

			if (packagesListViewWasEmpty && (packageStore.RowCount > 0)) {
				packagesListView.SelectRow (0);

				string message = string.IsNullOrWhiteSpace (packageSearchEntry.Text)
					? GettextCatalog.GetString ("Packages loaded")
					: GettextCatalog.GetString ("Search completed");
				IdeApp.Workbench.RootWindow.Accessible.MakeAccessibilityAnnouncement (message);
			}

			if (!viewModel.IsReadingPackages && (packageStore.RowCount == 0)) {
				ShowNoPackagesFoundMessage ();
			}
		}

		void AppendPackageToListView (ManagePackagesSearchResultViewModel packageViewModel)
		{
			int row = packageStore.AddRow ();
			var accessibleDescription = StringBuilderCache.Allocate (packageViewModel.Id);
			if (packageViewModel.HasDownloadCount) {
				accessibleDescription.Append (", ");
				if (packageViewModel.ShowVersionInsteadOfDownloadCount) {
					accessibleDescription.Append (GettextCatalog.GetString ("Version"));
					accessibleDescription.Append (" ");
					accessibleDescription.Append (packageViewModel.GetDownloadCountOrVersionDisplayText ());
				} else {
					accessibleDescription.Append (packageViewModel.GetDownloadCountOrVersionDisplayText ());
					accessibleDescription.Append (" ");
					accessibleDescription.Append (GettextCatalog.GetString ("Downloads"));
				}
			}
			if (!string.IsNullOrEmpty (packageViewModel.Summary))
				accessibleDescription.Append (", ").Append (packageViewModel.Summary);
			packageStore.SetValues (row,
				packageViewModelField, packageViewModel,
				packageCheckBoxField, packageViewModel.IsChecked,
				packageCheckA11yField, packageViewModel.Name,
				packageDescriptionA11yField, StringBuilderCache.ReturnAndFree (accessibleDescription));
		}

		void LoadPackageImage (int row, ManagePackagesSearchResultViewModel packageViewModel)
		{
			if (packageViewModel.HasIconUrl) {
				imageLoader.LoadFrom (packageViewModel.IconUrl, row);
			}
		}

		static bool IsOddRow (int row)
		{
			return (row % 2) == 0;
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
				ManagePackagesSearchResultViewModel packageViewModel = packageStore.GetValue (row, packageViewModelField);
				if (packageViewModel != null) {
					return uri == packageViewModel.IconUrl;
				}
			}
			return false;
		}

		void AddPackagesButtonClicked (object sender, EventArgs e)
		{
			try {
				if (viewModel.IsConsolidatePageSelected) {
					List<ManagePackagesSearchResultViewModel> packageViewModels = GetSelectedPackageViewModels ();
					List<IPackageAction> packageActions = viewModel.CreateConsolidatePackageActions (packageViewModels);
					RunPackageActions (packageActions);
				} else {
					var projects = SelectProjects ().ToList ();
					if (projects.Any ()) {
						List<IPackageAction> packageActions = CreatePackageActionsForSelectedPackages (projects);
						RunPackageActions (packageActions);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Adding packages failed.", ex);
				ShowErrorMessage (ex.Message);
			}
		}

		IEnumerable<IDotNetProject> SelectProjects ()
		{
			return SelectProjects (GetSelectedPackageViewModels ());
		}

		IEnumerable<IDotNetProject> SelectProjects (ManagePackagesSearchResultViewModel packageViewModel)
		{
			return SelectProjects (new [] { packageViewModel });
		}

		IEnumerable<IDotNetProject> SelectProjects (IEnumerable<ManagePackagesSearchResultViewModel> packageViewModels)
		{
			if (!viewModel.IsManagingSolution)
				return viewModel.DotNetProjects;

			var selectProjectsViewModel = new SelectProjectsViewModel (
				GetFilteredDotNetProjectsToSelect (packageViewModels),
				GetPackagesCountForAddPackagesButtonLabel (),
				viewModel.PageSelected);

			return Toolkit.NativeEngine.Invoke (() => {
				using (var dialog = new SelectProjectsDialog (selectProjectsViewModel)) {
					Command result = dialog.Run (this);
					if (result == Command.Ok) {
						return dialog.GetSelectedProjects ();
					} else {
						return Enumerable.Empty<IDotNetProject> ();
					}
				}
			});

		}

		/// <summary>
		/// Remove projects that do not make sense based on the currently selected filter.
		/// If we are on the Installed page that do not include any projects that do not have
		/// the selected NuGet package installed.
		/// </summary>
		IEnumerable<IDotNetProject> GetFilteredDotNetProjectsToSelect (IEnumerable<ManagePackagesSearchResultViewModel> packageViewModels)
		{
			if (viewModel.PageSelected != ManagePackagesPage.Browse) {
				var packageIds = packageViewModels.Select (pvm => pvm.Id).ToList ();
				return viewModel.GetDotNetProjectsToSelect (packageIds);
			}

			return viewModel.DotNetProjects;
		}

		void RunPackageActions (List<IPackageAction> packageActions)
		{
			if (packageActions.Count > 0) {
				ProgressMonitorStatusMessage progressMessage = GetProgressMonitorStatusMessages (packageActions);
				backgroundActionRunner.Run (progressMessage, packageActions);

				if (viewModel.PageSelected == ManagePackagesPage.Browse) {
					viewModel.OnInstallingSelectedPackages ();
				}
				Close ();
			}
		}

		List<IPackageAction> CreatePackageActionsForSelectedPackages (IEnumerable<IDotNetProject> selectedProjects)
		{
			List<ManagePackagesSearchResultViewModel> packageViewModels = GetSelectedPackageViewModels ();
			if (packageViewModels.Count > 0) {
				return viewModel.CreatePackageActions (packageViewModels, selectedProjects);
			}
			return new List<IPackageAction> ();
		}

		ProgressMonitorStatusMessage GetProgressMonitorStatusMessages (List<IPackageAction> packageActions)
		{
			if (viewModel.PageSelected == ManagePackagesPage.Browse) {
				return GetProgressMonitorInstallMessages (packageActions);
			} else if (viewModel.PageSelected == ManagePackagesPage.Installed) {
				return GetProgressMonitorUninstallMessages (packageActions);
			} else if (viewModel.PageSelected == ManagePackagesPage.Updates) {
				return GetProgressMonitorUpdateMessages (packageActions);
			} else if (viewModel.PageSelected == ManagePackagesPage.Consolidate) {
				return GetProgressMonitorConsolidateMessages (packageActions);
			}
			return null;
		}

		ProgressMonitorStatusMessage GetProgressMonitorInstallMessages (List<IPackageAction> packageActions)
		{
			if (packageActions.Count == 1) {
				string packageId = packageActions.Cast<INuGetPackageAction> ().First ().PackageId;
				if (OlderPackageInstalledThanPackageSelected ()) {
					return ProgressMonitorStatusMessageFactory.CreateUpdatingSinglePackageMessage (packageId);
				}
				return ProgressMonitorStatusMessageFactory.CreateInstallingSinglePackageMessage (packageId);
			}
			return ProgressMonitorStatusMessageFactory.CreateInstallingMultiplePackagesMessage (packageActions.Count);
		}

		static ProgressMonitorStatusMessage GetProgressMonitorUninstallMessages (List<IPackageAction> packageActions)
		{
			int count = packageActions.Count;
			if (count == 1) {
				string packageId = packageActions.Cast<INuGetPackageAction> ().First ().PackageId;
				return ProgressMonitorStatusMessageFactory.CreateRemoveSinglePackageMessage (packageId);
			}

			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Removing {0} packages...", count),
				GettextCatalog.GetString ("{0} packages successfully removed.", count),
				GettextCatalog.GetString ("Could not remove packages."),
				GettextCatalog.GetString ("{0} packages removed with warnings.", count)
			);
		}

		static ProgressMonitorStatusMessage GetProgressMonitorUpdateMessages (List<IPackageAction> packageActions)
		{
			int count = packageActions.Count;
			if (count == 1) {
				if (packageActions [0] is UpdateMultipleNuGetPackagesAction updateMultiplePackagesAction) {
					count = updateMultiplePackagesAction.PackagesToUpdate.Count ();
					if (count == 1) {
						return ProgressMonitorStatusMessageFactory.CreateUpdatingSinglePackageMessage (
							updateMultiplePackagesAction.PackagesToUpdate.First ().Id);
					}
				} else {
					string packageId = packageActions.Cast<INuGetPackageAction> ().First ().PackageId;
					return ProgressMonitorStatusMessageFactory.CreateUpdatingSinglePackageMessage (packageId);
				}
			}

			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Updating {0} packages...", count),
				GettextCatalog.GetString ("{0} packages successfully updated.", count),
				GettextCatalog.GetString ("Could not update packages."),
				GettextCatalog.GetString ("{0} packages updated with warnings.", count)
			);
		}

		static ProgressMonitorStatusMessage GetProgressMonitorConsolidateMessages (List<IPackageAction> packageActions)
		{
			int count = packageActions.Count;
			if (count == 1) {
				string packageId = packageActions.Cast<INuGetPackageAction> ().First ().PackageId;
				return new ProgressMonitorStatusMessage (
					GettextCatalog.GetString ("Consolidating {0}...", packageId),
					GettextCatalog.GetString ("{0} successfully consolidated.", packageId),
					GettextCatalog.GetString ("Could not consolidate {0}.", packageId),
					GettextCatalog.GetString ("{0} consolidated with warnings.", packageId)
				);
			}

			return new ProgressMonitorStatusMessage (
				GettextCatalog.GetString ("Consolidating {0} packages...", count),
				GettextCatalog.GetString ("{0} packages successfully consolidated.", count),
				GettextCatalog.GetString ("Could not consolidate packages."),
				GettextCatalog.GetString ("{0} packages consolidated with warnings.", count)
			);
		}

		List<ManagePackagesSearchResultViewModel> GetSelectedPackageViewModels ()
		{
			List<ManagePackagesSearchResultViewModel> packageViewModels = viewModel.CheckedPackageViewModels.ToList ();
			if (packageViewModels.Count > 0) {
				return packageViewModels;
			}

			ManagePackagesSearchResultViewModel selectedPackageViewModel = GetSelectedPackageViewModel ();
			if (selectedPackageViewModel != null) {
				packageViewModels.Add (selectedPackageViewModel);
			}
			return packageViewModels;
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
			viewModel.Search ();

			IdeApp.Workbench.RootWindow.Accessible.MakeAccessibilityAnnouncement (loadingSpinnerLabel.Text);

			return false;
		}

		void PackageCheckCellViewPackageChecked (object sender, WidgetEventArgs e)
		{
			PackagesListRowActivated (sender, new ListViewRowEventArgs (packagesListView.CurrentEventRow));
		}

		void PackagesListRowActivated (object sender, ListViewRowEventArgs e)
		{
			ManagePackagesSearchResultViewModel packageViewModel = packageStore.GetValue (e.RowIndex, packageViewModelField);
			packageViewModel.IsChecked = !packageViewModel.IsChecked;
			packageStore.SetValue (e.RowIndex, packageCheckBoxField, packageViewModel.IsChecked);
			UpdateAddPackagesButton ();
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

		void UpdateAddPackagesButton ()
		{
			addPackagesButton.Label = GetAddPackagesButtonLabel ();
			addPackagesButton.Sensitive = IsAddPackagesButtonEnabled ();
		}

		string GetAddPackagesButtonLabel ()
		{
			int packagesSelectedCount = GetPackagesCountForAddPackagesButtonLabel ();
			if (viewModel.PageSelected == ManagePackagesPage.Browse) {
				string label = GettextCatalog.GetPluralString ("Add Package", "Add Packages", packagesSelectedCount);
				if (PackagesCheckedCount <= 1 && OlderPackageInstalledThanPackageSelected ()) {
					label = GettextCatalog.GetString ("Update Package");
				}
				return label;
			} else if (viewModel.PageSelected == ManagePackagesPage.Installed) {
				return GettextCatalog.GetPluralString ("Uninstall Package", "Uninstall Packages", packagesSelectedCount);
			} else if (viewModel.PageSelected == ManagePackagesPage.Updates) {
				return GettextCatalog.GetPluralString ("Update Package", "Update Packages", packagesSelectedCount);
			} else if (viewModel.PageSelected == ManagePackagesPage.Consolidate) {
				return GettextCatalog.GetPluralString ("Consolidate Package", "Consolidate Packages", packagesSelectedCount);
			}

			throw new NotImplementedException ("Unknown package results page");
		}

		int GetPackagesCountForAddPackagesButtonLabel ()
		{
			if (PackagesCheckedCount > 1)
				return PackagesCheckedCount;

			return 1;
		}

		bool OlderPackageInstalledThanPackageSelected ()
		{
			if (PackagesCheckedCount != 0) {
				return false;
			}

			ManagePackagesSearchResultViewModel selectedPackageViewModel = GetSelectedPackageViewModel ();
			if (selectedPackageViewModel != null) {
				return selectedPackageViewModel.IsOlderPackageInstalled ();
			}
			return false;
		}

		bool IsAddPackagesButtonEnabled ()
		{
			if (loadingMessageVisible)
				return false;

			if (!IsAtLeastOnePackageSelected ())
				return false;

			if (viewModel.IsConsolidatePageSelected)
				return viewModel.CanConsolidate ();

			return true;
		}

		bool IsAtLeastOnePackageSelected ()
		{
			return (PackagesCheckedCount) >= 1 || (packagesListView.SelectedRow != -1);
		}

		int PackagesCheckedCount {
			get { return viewModel.CheckedPackageViewModels.Count; }
		}

		void SelectedPackageViewModelChanged (object sender, PropertyChangedEventArgs e)
		{
			try {
				if (e.PropertyName == "Versions") {
					PopulatePackageVersions (viewModel.SelectedPackage);
				} else {
					if (!viewModel.IsConsolidatePageSelected) {
						packagePublishedDate.Text = viewModel.SelectedPackage.GetLastPublishedDisplayText ();
						PopulatePackageDependencies (viewModel.SelectedPackage);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error loading package versions.", ex);
			}
		}

		void PopulatePackageVersions (ManagePackagesSearchResultViewModel packageViewModel)
		{
			DisposePopulatePackageVersionsTimer ();

			ignorePackageVersionChanges = true;
			try {
				packageVersionComboBox.Items.Clear ();
				if (packageViewModel.Versions.Any ()) {
					NuGetVersion latestStableVersion = packageViewModel.Versions.FirstOrDefault (v => !v.IsPrerelease);
					int count = 0;
					foreach (NuGetVersion version in packageViewModel.Versions) {
						count++;
						if (count > MaxVersionsToPopulate) {
							packageVersionsAddedCount = count - 1;
							if (version >= packageViewModel.SelectedVersion) {
								AddPackageVersionToComboBox (packageViewModel.SelectedVersion);
							}
							PopulatePackageVersionsAfterDelay ();
							break;
						}
						AddPackageVersionToComboBox (version, latestStableVersion == version);
					}
				} else {
					AddPackageVersionToComboBox (packageViewModel.Version);
				}
				packageVersionComboBox.SelectedItem = packageViewModel.SelectedVersion;
			} finally {
				ignorePackageVersionChanges = false;
			}
		}

		void AddPackageVersionToComboBox (NuGetVersion version, bool latestStable = false)
		{
			string versionLabel = version.ToString ();
			if (latestStable)
				versionLabel += " " + GettextCatalog.GetString ("(latest stable)");
			packageVersionComboBox.Items.Add (version, versionLabel);
		}

		void PackageVersionChanged (object sender, EventArgs e)
		{
			if (ignorePackageVersionChanges || viewModel.SelectedPackage == null)
				return;

			viewModel.SelectedPackage.SelectedVersion = (NuGetVersion)packageVersionComboBox.SelectedItem;
			UpdateAddPackagesButton ();
		}

		void PopulatePackageDependencies (ManagePackagesSearchResultViewModel packageViewModel)
		{
			if (packageViewModel.IsDependencyInformationAvailable) {
				this.packageDependenciesHBox.Visible = true;
				this.packageDependenciesListHBox.Visible = packageViewModel.HasDependencies;
				this.packageDependenciesNoneLabel.Visible = !packageViewModel.HasDependencies;
				this.packageDependenciesList.Text = packageViewModel.GetPackageDependenciesDisplayText ();
			} else {
				this.packageDependenciesHBox.Visible = false;
				this.packageDependenciesListHBox.Visible = false;
				this.packageDependenciesNoneLabel.Visible = false;
				this.packageDependenciesList.Text = String.Empty;
			}
		}

		void PopulatePackageVersionsAfterDelay ()
		{
			populatePackageVersionsTimer = Application.TimeoutInvoke (populatePackageVersionsDelayTimeSpan, PopulateMorePackageVersions);
		}

		void DisposePopulatePackageVersionsTimer ()
		{
			if (populatePackageVersionsTimer != null) {
				populatePackageVersionsTimer.Dispose ();
				populatePackageVersionsTimer = null;
			}
		}

		bool PopulateMorePackageVersions ()
		{
			ManagePackagesSearchResultViewModel packageViewModel = viewModel?.SelectedPackage;
			if (populatePackageVersionsTimer == null || packageViewModel == null) {
				return false;
			}

			int count = 0;
			foreach (NuGetVersion version in packageViewModel.Versions.Skip (packageVersionsAddedCount)) {
				count++;

				if (count > MaxVersionsToPopulate) {
					packageVersionsAddedCount += count - 1;
					return true;
				}

				AddPackageVersionToComboBox (version);
			}

			return false;
		}

		void UpdatePackageResultsPageLabels ()
		{
			UpdatePackageResultsLabel (ManagePackagesPage.Browse, browseLabel);
			UpdatePackageResultsLabel (ManagePackagesPage.Installed, installedLabel);
			UpdatePackageResultsLabel (ManagePackagesPage.Updates, updatesLabel);
			UpdatePackageResultsLabel (ManagePackagesPage.Consolidate, consolidateLabel);
		}

		void UpdatePackageResultsLabel (ManagePackagesPage page, CustomButtonLabel label)
		{
			string text = (string)label.Tag;
			if (page == viewModel.PageSelected) {
				UpdatePackageResultsLabelA11y (label, true);
				label.Markup = string.Format ("<b><u>{0}</u></b>", text);
			} else {
				UpdatePackageResultsLabelA11y (label, false);
				label.Markup = text;
			}
		}

		static void UpdatePackageResultsLabelA11y (Widget label, bool active)
		{
			if (label.Surface.ToolkitEngine.Type == ToolkitType.Gtk) {
				var widget = label.Surface.NativeWidget as Gtk.Widget;
				if (widget != null) {
					widget.Accessible.SetValue (active);
					// FIXME: Accessible.SetValue has no effect, so set the role description instead
					widget.Accessible.SetRole (AtkCocoa.Roles.AXRadioButton, active ? "selected tab" : "tab");
				}
			}
		}

		void UpdatePackageResultsLabel (ManagePackagesPage page, Button label)
		{
			string text = (string)label.Tag;
			if (page == viewModel.PageSelected) {
				label.Markup = string.Format ("<b><u>{0}</u></b>", text);
			} else {
				label.Markup = text;
			}
		}

		void BrowseLabelButtonPressed (object sender, EventArgs e)
		{
			viewModel.PageSelected = ManagePackagesPage.Browse;
			OnPackageResultsPageSelected ();
		}

		void BrowseLabelKeyPressed (object sender, KeyEventArgs e)
		{
			if (e.Modifiers == ModifierKeys.None && (e.Key == Key.Return || e.Key == Key.Space || e.Key == Key.NumPadEnter)) {
				BrowseLabelButtonPressed (sender, e);
			}
		}

		void InstalledLabelButtonPressed (object sender, EventArgs e)
		{
			viewModel.PageSelected = ManagePackagesPage.Installed;
			OnPackageResultsPageSelected ();
		}

		void InstalledLabelKeyPressed (object sender, KeyEventArgs e)
		{
			if (e.Modifiers == ModifierKeys.None && (e.Key == Key.Return || e.Key == Key.Space || e.Key == Key.NumPadEnter)) {
				InstalledLabelButtonPressed (sender, e);
			}
		}

		void UpdatesLabelButtonPressed (object sender, EventArgs e)
		{
			viewModel.PageSelected = ManagePackagesPage.Updates;
			OnPackageResultsPageSelected ();
		}

		void UpdatesLabelKeyPressed (object sender, KeyEventArgs e)
		{
			if (e.Modifiers == ModifierKeys.None && (e.Key == Key.Return || e.Key == Key.Space || e.Key == Key.NumPadEnter)) {
				UpdatesLabelButtonPressed (sender, e);
			}
		}

		void ConsolidateLabelButtonPressed (object sender, EventArgs e)
		{
			viewModel.PageSelected = ManagePackagesPage.Consolidate;
			OnPackageResultsPageSelected ();
		}

		void ConsolidateLabelKeyPressed (object sender, KeyEventArgs e)
		{
			if (e.Modifiers == ModifierKeys.None && (e.Key == Key.Return || e.Key == Key.Space || e.Key == Key.NumPadEnter)) {
				ConsolidateLabelButtonPressed (sender, e);
			}
		}

		void OnPackageResultsPageSelected ()
		{
			UpdatePackageResultsPageLabels ();
			ClearErrorMessage ();
			ClearPackages ();
			UpdateAddPackagesButton ();
			Search ();
		}

		void PopulateProjectList ()
		{
			InitializeProjectsListView ();

			foreach (ManageProjectViewModel project in viewModel.ProjectViewModels) {
				int row = projectStore.AddRow ();
				projectStore.SetValues (
					row,
					projectCheckedField,
					project.IsChecked,
					projectNameField,
					project.ProjectName,
					packageVersionField,
					project.PackageVersion,
					projectField,
					project);
			}
		}

		void ProjectCheckBoxCellViewToggled (object sender, WidgetEventArgs e)
		{
			int row = projectsListView.CurrentEventRow;
			if (row == -1)
				return;

			ManageProjectViewModel selectedProject = projectStore.GetValue (row, projectField);
			if (selectedProject == null)
				return;

			selectedProject.IsChecked = !selectedProject.IsChecked;

			UpdateAddPackagesButton ();
		}
	}
}