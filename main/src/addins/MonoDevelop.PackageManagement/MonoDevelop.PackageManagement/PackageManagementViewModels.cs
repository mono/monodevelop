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

namespace ICSharpCode.PackageManagement
{
	public class PackageManagementViewModels
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
			ThreadSafePackageManagementEvents packageManagementEvents = CreateThreadSafePackageManagementEvents();
			PackagesViewModels packagesViewModels = CreatePackagesViewModels(packageManagementEvents);

			managePackagesViewModel = 
				new ManagePackagesViewModel(
					packagesViewModels,
					new ManagePackagesViewTitle(solution),
					packageManagementEvents);
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
		
		ThreadSafePackageManagementEvents CreateThreadSafePackageManagementEvents()
		{
			return new ThreadSafePackageManagementEvents(
				PackageManagementServices.PackageManagementEvents);
		}
		
		PackagesViewModels CreatePackagesViewModels(IThreadSafePackageManagementEvents packageManagementEvents)
		{
			return new PackagesViewModels(
				solution,
				registeredPackageRepositories,
				packageManagementEvents,
				PackageManagementServices.PackageActionRunner,
				new PackageManagementTaskFactory());
		}
		
		public RegisteredPackageSourcesViewModel RegisteredPackageSourcesViewModel {
			get {
				if (registeredPackageSourcesViewModel == null) {
					registeredPackageSourcesViewModel = CreateRegisteredPackageSourcesViewModel();
				}
				return registeredPackageSourcesViewModel;
			}
		}
		
		RegisteredPackageSourcesViewModel CreateRegisteredPackageSourcesViewModel()
		{
			CreateRegisteredPackageRepositories();
			return new RegisteredPackageSourcesViewModel(registeredPackageRepositories);
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
