// 
// PackageManagementViewModels.cs
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
using NuGet;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementViewModels
	{
		ManagePackagesViewModel managePackagesViewModel;
		RegisteredPackageSourcesViewModel registeredPackageSourcesViewModel;
		PackageManagementOptionsViewModel packageManagementOptionsViewModel;
		IPackageManagementSolution solution;
		IRegisteredPackageRepositories registeredPackageRepositories;
		
		public ManagePackagesViewModel ManagePackagesViewModel {
			get {
				CreateManagePackagesViewModel();
				return managePackagesViewModel;
			}
		}
		
		void CreateManagePackagesViewModel()
		{
			CreateRegisteredPackageRepositories();
			CreateSolution();
			PackagesViewModels packagesViewModels = CreatePackagesViewModels();

			managePackagesViewModel = 
				new ManagePackagesViewModel(
					packagesViewModels,
					new ManagePackagesViewTitle(solution),
					PackageManagementServices.PackageManagementEvents);
		}
		
		void CreateRegisteredPackageRepositories()
		{
			if (registeredPackageRepositories == null) {
				registeredPackageRepositories = PackageManagementServices.RegisteredPackageRepositories;
			}
		}
		
		void CreateSolution()
		{
			if (solution == null) {
				solution = PackageManagementServices.Solution;
			}
		}
		
		PackagesViewModels CreatePackagesViewModels()
		{
			return new PackagesViewModels(
				solution,
				registeredPackageRepositories,
				PackageManagementServices.PackageManagementEvents,
				PackageManagementServices.PackageActionRunner,
				new PackageManagementTaskFactory());
		}
		
		public PackageManagementOptionsViewModel PackageManagementOptionsViewModel {
			get {
				if (packageManagementOptionsViewModel == null) {
					CreateRegisteredPackageRepositories();
					IRecentPackageRepository recentRepository = registeredPackageRepositories.RecentPackageRepository;
					packageManagementOptionsViewModel = new PackageManagementOptionsViewModel(recentRepository);
				}
				return packageManagementOptionsViewModel;
			}
		}
	}
}
