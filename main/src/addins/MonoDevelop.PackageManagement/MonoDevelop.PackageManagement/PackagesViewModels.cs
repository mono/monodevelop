// 
// PackagesViewModels.cs
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

namespace ICSharpCode.PackageManagement
{
	public class PackagesViewModels : IDisposable
	{
		public PackagesViewModels(
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredPackageRepositories,
			IThreadSafePackageManagementEvents packageManagementEvents,
			IPackageActionRunner actionRunner,
			ITaskFactory taskFactory)
		{
			var packageViewModelFactory = new PackageViewModelFactory(solution, packageManagementEvents, actionRunner);
			//var updatedPackageViewModelFactory = new UpdatedPackageViewModelFactory(packageViewModelFactory);
			//var installedPackageViewModelFactory = new InstalledPackageViewModelFactory(packageViewModelFactory);

			IRecentPackageRepository recentPackageRepository = PackageManagementServices.RecentPackageRepository;
			AvailablePackagesViewModel = new AvailablePackagesViewModel (solution, registeredPackageRepositories, recentPackageRepository, packageViewModelFactory, taskFactory);
			//InstalledPackagesViewModel = new InstalledPackagesViewModel(solution, packageManagementEvents, registeredPackageRepositories, installedPackageViewModelFactory, taskFactory);
			//UpdatedPackagesViewModel = new UpdatedPackagesViewModel(solution, registeredPackageRepositories, updatedPackageViewModelFactory, taskFactory);
			//RecentPackagesViewModel = new RecentPackagesViewModel(packageManagementEvents, registeredPackageRepositories, packageViewModelFactory, taskFactory);
		}
		
		public AvailablePackagesViewModel AvailablePackagesViewModel { get; private set; }
		public InstalledPackagesViewModel InstalledPackagesViewModel { get; private set; }
		public RecentPackagesViewModel RecentPackagesViewModel { get; private set; }
		public UpdatedPackagesViewModel UpdatedPackagesViewModel { get; private set; }
		
		public void ReadPackages()
		{
			AvailablePackagesViewModel.ReadPackages();
			//InstalledPackagesViewModel.ReadPackages();
			//UpdatedPackagesViewModel.ReadPackages();
			//RecentPackagesViewModel.ReadPackages();
		}
		
		public void Dispose()
		{
			AvailablePackagesViewModel.Dispose();
			//InstalledPackagesViewModel.Dispose();
			//RecentPackagesViewModel.Dispose();
			//UpdatedPackagesViewModel.Dispose();
		}
	}
}
