// 
// ManagePackagesDialog.cs
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
using System.ComponentModel;
using System.Linq;

using Gdk;
using Gtk;
using ICSharpCode.PackageManagement;
using MonoDevelop.Ide;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PackagesWidget : Gtk.Bin
	{
		PackagesViewModel viewModel;
		List<PackageSource> packageSources;
		ListStore packageStore;
		CellRendererText treeViewColumnTextRenderer;
		const int PackageViewModelColumn = 2;
		
		public PackagesWidget ()
		{
			this.Build ();
			this.InitializeTreeView ();
		}
		
		void InitializeTreeView ()
		{
			packageStore = new ListStore (typeof (Pixbuf), typeof (string), typeof(PackageViewModel));
			packagesTreeView.Model = packageStore;
			packagesTreeView.AppendColumn (CreateTreeViewColumn ());
			packagesTreeView.Selection.Changed += PackagesTreeViewSelectionChanged;
			includePrereleaseCheckButton.Clicked += IncludePrereleaseCheckButtonClicked;
			
			AddSearchingMessageToTreeView ();
		}
		
		TreeViewColumn CreateTreeViewColumn ()
		{
			var column = new TreeViewColumn ();
			
			var iconRenderer = new CellRendererPixbuf ();
			column.PackStart (iconRenderer, false);
			column.AddAttribute (iconRenderer, "pixbuf", column: 0);
			
			treeViewColumnTextRenderer = new CellRendererText ();
			treeViewColumnTextRenderer.WrapMode = Pango.WrapMode.Word;
			treeViewColumnTextRenderer.WrapWidth = 250;
			
			column.PackStart (treeViewColumnTextRenderer, true);
			column.AddAttribute (treeViewColumnTextRenderer, "markup", column: 1);
			
			return column;
		}
		
		void AddSearchingMessageToTreeView ()
		{
			packageStore.AppendValues (
				ImageService.GetImage (Gtk.Stock.Info, IconSize.LargeToolbar),
				Mono.Unix.Catalog.GetString ("Searching..."),
				null);
		}
		
		void PackagesTreeViewSelectionChanged (object sender, EventArgs e)
		{
			ShowSelectedPackage ();
		}

		void IncludePrereleaseCheckButtonClicked (object sender, EventArgs e)
		{
			viewModel.IncludePrerelease = !viewModel.IncludePrerelease;
		}

		public void LoadViewModel (PackagesViewModel viewModel)
		{
			this.viewModel = viewModel;
			
			this.includePrereleaseCheckButton.Visible = viewModel.ShowPrerelease;
			
			this.packageSearchHBox.Visible = viewModel.IsSearchable;
			ClearSelectedPackageInformation ();
			PopulatePackageSources ();
			viewModel.PropertyChanged += ViewModelPropertyChanged;

			this.pagedResultsWidget.LoadPackagesViewModel (viewModel);
			this.pagedResultsHBox.Visible = viewModel.IsPaged;

			this.updateAllPackagesButtonBox.Visible = viewModel.IsUpdateAllPackagesEnabled;
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
			this.packageSourceComboBox.Visible = viewModel.ShowPackageSources;
			if (viewModel.ShowPackageSources) {
				for (int index = 0; index < PackageSources.Count; ++index) {
					PackageSource packageSource = PackageSources [index];
					this.packageSourceComboBox.InsertText (index, packageSource.Name);
				}
				
				this.packageSourceComboBox.Active = GetSelectedPackageSourceIndexFromViewModel ();
			}
		}
		
		int GetSelectedPackageSourceIndexFromViewModel ()
		{
			if (viewModel.SelectedPackageSource == null) {
				return -1;
			}
			
			return PackageSources.IndexOf (viewModel.SelectedPackageSource);
		}
		
		void PackageSourceChanged (object sender, EventArgs e)
		{
			viewModel.SelectedPackageSource = GetSelectedPackageSource ();
		}
		
		PackageSource GetSelectedPackageSource ()
		{
			if (this.packageSourceComboBox.Active == -1) {
				return null;
			}
			
			return PackageSources [this.packageSourceComboBox.Active];
		}
		
		void SearchButtonClicked (object sender, EventArgs e)
		{
			Search ();
		}
		
		void Search ()
		{
			viewModel.SearchTerms = this.packageSearchEntry.Text;
			viewModel.SearchCommand.Execute (null);
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
			TreeIter item;
			if (packagesTreeView.Selection.GetSelected (out item)) {
				return packageStore.GetValue (item, PackageViewModelColumn) as PackageViewModel;
			}
			return null;
		}
		
		void ShowPackageInformation (PackageViewModel packageViewModel)
		{
			this.packageVersionTextBox.Text = packageViewModel.Version.ToString ();
			this.packageCreatedByTextBox.Text = packageViewModel.GetAuthors ();
			this.packageLastUpdatedTextBox.Text = packageViewModel.GetLastPublishedDisplayText ();
			this.packageDownloadsTextBox.Text = packageViewModel.GetDownloadCountDisplayText ();
			this.packageDescriptionTextView.Buffer.Text = packageViewModel.Description;
			this.packageIdTextBox.Text = packageViewModel.Id;
			this.packageIdTextBox.Visible = packageViewModel.HasNoGalleryUrl;
			ShowUri (this.packageIdButton, packageViewModel.GalleryUrl, packageViewModel.Id);
			ShowUri (this.moreInformationButton, packageViewModel.ProjectUrl);
			ShowUri (this.viewLicenseTermsButton, packageViewModel.LicenseUrl);
			this.packageDependenciesListHBox.Visible = packageViewModel.HasDependencies;
			this.packageDependenciesNoneLabel.Visible = !packageViewModel.HasDependencies;
			this.packageDependenciesListLabel.Text = packageViewModel.GetPackageDependenciesDisplayText ();

			EnablePackageActionButtons (packageViewModel);
			
			this.packageInfoFrameVBox.Visible = true;
			this.managePackageButtonBox.Visible = true;
		}
		
		void ClearSelectedPackageInformation ()
		{
			this.packageInfoFrameVBox.Visible = false;
			this.managePackageButtonBox.Visible = false;
		}
		
		void ShowUri (HyperlinkWidget hyperlinkWidget, Uri uri, string label)
		{
			hyperlinkWidget.Label = label;
			ShowUri (hyperlinkWidget, uri);
		}
		
		void ShowUri (HyperlinkWidget hyperlinkWidget, Uri uri)
		{
			if (uri == null) {
				hyperlinkWidget.Visible = false;
			} else {
				hyperlinkWidget.Visible = true;
				hyperlinkWidget.Uri = uri.ToString ();
			}
		}
		
		void EnablePackageActionButtons (PackageViewModel packageViewModel)
		{
			this.addPackageButton.Visible = !packageViewModel.IsManaged;
			this.removePackageButton.Visible = !packageViewModel.IsManaged;
			this.managePackageButton.Visible = packageViewModel.IsManaged;
			
			this.addPackageButton.Sensitive = !packageViewModel.IsAdded;
			this.removePackageButton.Sensitive = packageViewModel.IsAdded;
		}
		
		void ViewModelPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			this.packageStore.Clear ();
			
			if (viewModel.HasError) {
				AddErrorToTreeView ();
			}
			
			if (viewModel.IsReadingPackages) {
				AddSearchingMessageToTreeView ();
			}
			
			foreach (PackageViewModel packageViewModel in viewModel.PackageViewModels) {
				AppendPackageToTreeView (packageViewModel);
			}

			this.pagedResultsHBox.Visible = viewModel.IsPaged;
			this.updateAllPackagesButtonBox.Visible = viewModel.IsUpdateAllPackagesEnabled;
		}

		void AddErrorToTreeView ()
		{
			packageStore.AppendValues (
				ImageService.GetImage (Gtk.Stock.DialogError, IconSize.LargeToolbar),
				viewModel.ErrorMessage,
				null);
		}
		
		void PackageSearchEntryActivated (object sender, EventArgs e)
		{
			Search ();
		}
		
		void AppendPackageToTreeView (PackageViewModel packageViewModel)
		{
			packageStore.AppendValues (
				ImageService.GetImage ("md-nuget-package", IconSize.Dnd),
				packageViewModel.GetDisplayTextMarkup (),
				packageViewModel);
		}
		
		void OnAddPackageButtonClicked (object sender, EventArgs e)
		{
			PackageViewModel packageViewModel = GetSelectedPackageViewModel ();
			packageViewModel.AddPackage ();
			EnablePackageActionButtons (packageViewModel);
		}
		
		void RemovePackageButtonClicked (object sender, EventArgs e)
		{
			PackageViewModel packageViewModel = GetSelectedPackageViewModel ();
			packageViewModel.RemovePackage ();
			EnablePackageActionButtons (packageViewModel);
		}
		
		void ManagePackagesButtonClicked (object sender, EventArgs e)
		{
			PackageViewModel packageViewModel = GetSelectedPackageViewModel ();
			packageViewModel.ManagePackage ();
		}
		
		void UpdateAllPackagesButtonClicked (object sender, EventArgs e)
		{
			viewModel.UpdateAllPackagesCommand.Execute (null);
		}
		
		protected override void OnDestroyed ()
		{
			if (viewModel != null) {
				viewModel.PropertyChanged -= ViewModelPropertyChanged;
			}
			base.OnDestroyed ();
		}
	}
}

