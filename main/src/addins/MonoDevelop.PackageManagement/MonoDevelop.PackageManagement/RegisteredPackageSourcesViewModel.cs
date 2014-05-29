// 
// RegisteredPackageSourcesViewModel.cs
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
using System.IO;
using System.Linq;

using MonoDevelop.PackageManagement;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class RegisteredPackageSourcesViewModel : ViewModelBase<RegisteredPackageSourcesViewModel>, IDisposable, IPackageSourceProvider
	{
		ObservableCollection<PackageSourceViewModel> packageSourceViewModels = 
			new ObservableCollection<PackageSourceViewModel>();
		RegisteredPackageSources packageSources;
		IRegisteredPackageRepositories registeredPackageRepositories;
		IFolderBrowser folderBrowser;
		PackageSourceViewModelChecker packageSourceChecker = new PackageSourceViewModelChecker ();
		
		DelegateCommand addPackageSourceCommmand;
		DelegateCommand removePackageSourceCommand;
		DelegateCommand movePackageSourceUpCommand;
		DelegateCommand movePackageSourceDownCommand;
		DelegateCommand browsePackageFolderCommand;
		DelegateCommand updatePackageSourceCommand;
		
		RegisteredPackageSource newPackageSource = new RegisteredPackageSource();
		PackageSourceViewModel selectedPackageSourceViewModel;
		bool isEditingSelectedPackageSource;
		
		public RegisteredPackageSourcesViewModel(
			IRegisteredPackageRepositories registeredPackageRepositories)
			: this(registeredPackageRepositories, new FolderBrowser())
		{
		}
		
		public RegisteredPackageSourcesViewModel(
			IRegisteredPackageRepositories registeredPackageRepositories,
			IFolderBrowser folderBrowser)
		{
			this.packageSources = registeredPackageRepositories.PackageSources;
			this.folderBrowser = folderBrowser;
			this.registeredPackageRepositories = registeredPackageRepositories;

			packageSourceChecker.PackageSourceChecked += PackageSourceChecked;
			CreateCommands();
		}

		void PackageSourceChecked (object sender, PackageSourceViewModelCheckedEventArgs e)
		{
			e.PackageSource.IsValid = e.IsValid;
			e.PackageSource.ValidationFailureMessage = e.ValidationFailureMessage;
			OnPackageSourceChanged (e.PackageSource);
		}
		
		void CreateCommands()
		{
			addPackageSourceCommmand =
				new DelegateCommand(param => AddPackageSource(),
					param => CanAddPackageSource);
			
			removePackageSourceCommand =
				new DelegateCommand(param => RemovePackageSource(),
					param => CanRemovePackageSource);

			updatePackageSourceCommand =
				new DelegateCommand(param => UpdatePackageSource(),
					param => CanUpdatePackageSource);

			movePackageSourceUpCommand =
				new DelegateCommand(param => MovePackageSourceUp(),
					param => CanMovePackageSourceUp);
			
			movePackageSourceDownCommand =
				new DelegateCommand(param => MovePackageSourceDown(),
					param => CanMovePackageSourceDown);
			
			browsePackageFolderCommand =
				new DelegateCommand(param => BrowsePackageFolder());
		}
		
		public ICommand AddPackageSourceCommand {
			get { return addPackageSourceCommmand; }
		}
		
		public ICommand RemovePackageSourceCommand {
			get { return removePackageSourceCommand; }
		}
		
		public ICommand MovePackageSourceUpCommand {
			get { return movePackageSourceUpCommand; }
		}
		
		public ICommand MovePackageSourceDownCommand {
			get { return movePackageSourceDownCommand; }
		}
		
		public ICommand BrowsePackageFolderCommand {
			get { return browsePackageFolderCommand; }
		}

		public ICommand UpdatePackageSourceCommand {
			get { return updatePackageSourceCommand; }
		}

		public ObservableCollection<PackageSourceViewModel> PackageSourceViewModels {
			get { return packageSourceViewModels; }
		}
		
		public void Load()
		{
			ReplaceExistingPackageSourceCredentialProvider ();

			foreach (PackageSource packageSource in packageSources) {
				AddPackageSourceToViewModel(packageSource);
			}
		}

		/// <summary>
		/// Use this class as the source of package source credentials and disable any
		/// prompt for credentials.
		/// </summary>
		void ReplaceExistingPackageSourceCredentialProvider ()
		{
			HttpClient.DefaultCredentialProvider = new SettingsCredentialProvider (NullCredentialProvider.Instance, this);
		}
		
		void AddPackageSourceToViewModel(PackageSource packageSource)
		{
			var packageSourceViewModel = new PackageSourceViewModel(packageSource);
			packageSourceViewModels.Add(packageSourceViewModel);

			packageSourceChecker.Check (packageSourceViewModel);
		}
		
		public void Save()
		{
			Save (packageSourceViewModels);
		}
		
		public string NewPackageSourceName {
			get { return newPackageSource.Name; }
			set {
				newPackageSource.Name = value;
				OnPropertyChanged(viewModel => viewModel.NewPackageSourceName);
			}
		}
		
		public string NewPackageSourceUrl {
			get { return newPackageSource.Source; }
			set {
				newPackageSource.Source = value;
				OnPropertyChanged(viewModel => viewModel.NewPackageSourceUrl);
			}
		}

		public string NewPackageSourceUserName {
			get { return newPackageSource.UserName; }
			set {
				newPackageSource.UserName = value;
				OnPropertyChanged(viewModel => viewModel.NewPackageSourceUserName);
			}
		}

		public string NewPackageSourcePassword {
			get { return newPackageSource.Password; }
			set {
				newPackageSource.Password = value;
				OnPropertyChanged(viewModel => viewModel.NewPackageSourcePassword);
			}
		}
		
		public PackageSourceViewModel SelectedPackageSourceViewModel {
			get { return selectedPackageSourceViewModel; }
			set {
				selectedPackageSourceViewModel = value;
				OnPropertyChanged(viewModel => viewModel.SelectedPackageSourceViewModel);
				OnPropertyChanged(viewModel => viewModel.CanAddPackageSource);
			}
		}
		
		public void AddPackageSource()
		{
			AddNewPackageSourceToViewModel();
			SelectLastPackageSourceViewModel();
		}
		
		void AddNewPackageSourceToViewModel()
		{
			var packageSource = newPackageSource.ToPackageSource();
			packageSource.IsEnabled = true;
			AddPackageSourceToViewModel(packageSource);
		}
		
		void SelectLastPackageSourceViewModel()
		{
			SelectedPackageSourceViewModel = GetLastPackageSourceViewModel();
		}
		
		public bool CanAddPackageSource {
			get { return !IsEditingSelectedPackageSource && NewPackageSourceHasUrl && NewPackageSourceHasName; }
		}
		
		bool NewPackageSourceHasUrl {
			get { return !String.IsNullOrEmpty(NewPackageSourceUrl); }
		}
		
		bool NewPackageSourceHasName {
			get { return !String.IsNullOrEmpty(NewPackageSourceName); }
		}
		
		public void RemovePackageSource()
		{
			RemoveSelectedPackageSourceViewModel();
		}
		
		public bool CanRemovePackageSource {
			get { return IsPackageSourceSelected(); }
		}
		
		void RemoveSelectedPackageSourceViewModel()
		{
			packageSourceViewModels.Remove(selectedPackageSourceViewModel);
		}
		
		public void MovePackageSourceUp()
		{
			int selectedPackageSourceIndex = GetSelectedPackageSourceViewModelIndex();
			int destinationPackageSourceIndex = selectedPackageSourceIndex--;
			packageSourceViewModels.Move(selectedPackageSourceIndex, destinationPackageSourceIndex);
		}
		
		int GetSelectedPackageSourceViewModelIndex()
		{
			return packageSourceViewModels.IndexOf(selectedPackageSourceViewModel);
		}
		
		public bool CanMovePackageSourceUp {
			get {
				return HasAtLeastTwoPackageSources() &&
					IsPackageSourceSelected() &&
					!IsFirstPackageSourceSelected();
			}
		}
		
		bool IsPackageSourceSelected()
		{
			return selectedPackageSourceViewModel != null;
		}
		
		bool IsFirstPackageSourceSelected()
		{
			return selectedPackageSourceViewModel == packageSourceViewModels[0];
		}
		
		public void MovePackageSourceDown()
		{
			int selectedPackageSourceIndex = GetSelectedPackageSourceViewModelIndex();
			int destinationPackageSourceIndex = selectedPackageSourceIndex++;
			packageSourceViewModels.Move(selectedPackageSourceIndex, destinationPackageSourceIndex);			
		}
		
		public bool CanMovePackageSourceDown {
			get {
				return HasAtLeastTwoPackageSources() &&
					IsPackageSourceSelected() &&
					!IsLastPackageSourceSelected();
			}
		}
		
		bool HasAtLeastTwoPackageSources()
		{
			return packageSourceViewModels.Count >= 2;
		}
		
		bool IsLastPackageSourceSelected()
		{
			PackageSourceViewModel lastViewModel = GetLastPackageSourceViewModel();
			return lastViewModel == selectedPackageSourceViewModel;
		}
		
		PackageSourceViewModel GetLastPackageSourceViewModel()
		{
			return packageSourceViewModels.Last();
		}
		
		public void BrowsePackageFolder()
		{
			string folder = folderBrowser.SelectFolder();
			if (folder != null) {
				UpdateNewPackageSourceUsingSelectedFolder(folder);
			}
		}
		
		void UpdateNewPackageSourceUsingSelectedFolder(string folder)
		{
			NewPackageSourceUrl = folder;
			NewPackageSourceName = GetPackageSourceNameFromFolder(folder);
		}
		
		string GetPackageSourceNameFromFolder(string folder)
		{
			return Path.GetFileName(folder);
		}

		public bool IsEditingSelectedPackageSource {
			get { return isEditingSelectedPackageSource; }
			set {
				isEditingSelectedPackageSource = value;
				if (isEditingSelectedPackageSource) {
					NewPackageSourceName = selectedPackageSourceViewModel.Name;
					NewPackageSourceUrl = selectedPackageSourceViewModel.SourceUrl;
					NewPackageSourceUserName = selectedPackageSourceViewModel.UserName;
					NewPackageSourcePassword = selectedPackageSourceViewModel.Password;
				} else {
					NewPackageSourceUrl = String.Empty;
					NewPackageSourceName = String.Empty;
					NewPackageSourceUserName = String.Empty;
					NewPackageSourcePassword = String.Empty;
				}
			}
		}

		public bool CanUpdatePackageSource {
			get {
				return IsEditingSelectedPackageSource &&
					NewPackageSourceHasUrl &&
					NewPackageSourceHasName;
			}
		}

		public void UpdatePackageSource ()
		{
			selectedPackageSourceViewModel.Name = NewPackageSourceName;
			selectedPackageSourceViewModel.SourceUrl = NewPackageSourceUrl;
			selectedPackageSourceViewModel.UserName = NewPackageSourceUserName;
			selectedPackageSourceViewModel.Password = NewPackageSourcePassword;

			OnPackageSourceChanged (selectedPackageSourceViewModel);

			packageSourceChecker.Check (selectedPackageSourceViewModel);
		}

		public void Save (IEnumerable<PackageSourceViewModel> packageSourceViewModels)
		{
			registeredPackageRepositories.UpdatePackageSources (
				packageSourceViewModels.Select (viewModel => viewModel.GetPackageSource ()));
		}

		public event EventHandler<PackageSourceViewModelChangedEventArgs> PackageSourceChanged;

		void OnPackageSourceChanged (PackageSourceViewModel packageSource)
		{
			if (PackageSourceChanged != null) {
				PackageSourceChanged (this, new PackageSourceViewModelChangedEventArgs (packageSource));
			}
		}

		public void Dispose ()
		{
			try {
				packageSourceChecker.Dispose ();
			} finally {
				PackageManagementServices.InitializeCredentialProvider ();
			}
		}

		/// <summary>
		/// This is called by NuGet's credential provider when a request needs 
		/// a username and password. We return the current package sources
		/// stored in Preferences with the latest usernames and passwords.
		/// </summary>
		IEnumerable<PackageSource> IPackageSourceProvider.LoadPackageSources ()
		{
			return packageSourceViewModels.Select (viewModel => viewModel.GetPackageSource ());
		}

		void IPackageSourceProvider.SavePackageSources (IEnumerable<PackageSource> sources)
		{
		}

		void IPackageSourceProvider.DisablePackageSource (PackageSource source)
		{
		}

		bool IPackageSourceProvider.IsPackageSourceEnabled (PackageSource source)
		{
			return true;
		}
	}
}
