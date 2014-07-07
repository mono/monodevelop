// 
// PackageManagementOptionsViewModel.cs
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
using System.Linq;

namespace ICSharpCode.PackageManagement
{
	public class PackageManagementOptionsViewModel : ViewModelBase<PackageManagementOptionsViewModel>
	{
		PackageManagementOptions options;
		IRecentPackageRepository recentPackageRepository;
		IMachinePackageCache machinePackageCache;
		IProcess process;
		
		public PackageManagementOptionsViewModel(IRecentPackageRepository recentPackageRepository)
			: this(PackageManagementServices.Options, recentPackageRepository, new MachinePackageCache(), new Process())
		{
		}
		
		public PackageManagementOptionsViewModel(
			PackageManagementOptions options,
			IRecentPackageRepository recentPackageRepository,
			IMachinePackageCache machinePackageCache,
			IProcess process)
		{
			this.options = options;
			this.recentPackageRepository = recentPackageRepository;
			this.machinePackageCache = machinePackageCache;
			this.process = process;
			
			this.HasNoRecentPackages = !RecentPackageRepositoryHasPackages();
			this.HasNoCachedPackages = !MachinePackageCacheHasPackages();
			this.IsAutomaticPackageRestoreOnOpeningSolutionEnabled = options.IsAutomaticPackageRestoreOnOpeningSolutionEnabled;
			this.IsCheckForPackageUpdatesOnOpeningSolutionEnabled = options.IsCheckForPackageUpdatesOnOpeningSolutionEnabled;

			CreateCommands();
		}
		
		public bool HasNoRecentPackages { get; private set; }
		public bool HasNoCachedPackages { get; private set; }
		public bool IsAutomaticPackageRestoreOnOpeningSolutionEnabled { get; set; }
		public bool IsCheckForPackageUpdatesOnOpeningSolutionEnabled { get; set; }
		
		bool MachinePackageCacheHasPackages()
		{
			return machinePackageCache.GetPackages().Any();
		}
		
		bool RecentPackageRepositoryHasPackages()
		{
			return recentPackageRepository.HasRecentPackages;
		}
		
		void CreateCommands()
		{
			ClearRecentPackagesCommand =
				new DelegateCommand(param => ClearRecentPackages(), param => !HasNoRecentPackages);
			ClearCachedPackagesCommand =
				new DelegateCommand(param => ClearCachedPackages(), param => !HasNoCachedPackages);
			BrowseCachedPackagesCommand =
				new DelegateCommand(param => BrowseCachedPackages(), param => !HasNoCachedPackages);
		}
		
		public ICommand ClearRecentPackagesCommand { get; private set; }
		public ICommand ClearCachedPackagesCommand { get; private set; }
		public ICommand BrowseCachedPackagesCommand { get; private set; }
		
		public void ClearRecentPackages()
		{
			recentPackageRepository.Clear();
			HasNoRecentPackages = true;
			OnPropertyChanged(viewModel => viewModel.HasNoRecentPackages);
		}
		
		public void ClearCachedPackages()
		{
			machinePackageCache.Clear();
			HasNoCachedPackages = true;
			OnPropertyChanged(viewModel => viewModel.HasNoCachedPackages);
		}
		
		public void BrowseCachedPackages()
		{
			process.Start(machinePackageCache.Source);
		}
		
		public void SaveOptions()
		{
			options.IsAutomaticPackageRestoreOnOpeningSolutionEnabled = IsAutomaticPackageRestoreOnOpeningSolutionEnabled;
			options.IsCheckForPackageUpdatesOnOpeningSolutionEnabled = IsCheckForPackageUpdatesOnOpeningSolutionEnabled;
		}
	}
}
