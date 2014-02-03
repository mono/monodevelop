// 
// RecentPackagesViewModel.cs
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
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class RecentPackagesViewModel : PackagesViewModel
	{
		IPackageManagementEvents packageManagementEvents;
		IPackageRepository recentPackageRepository;
		
		public RecentPackagesViewModel(
			IPackageManagementEvents packageManagementEvents,
			IRegisteredPackageRepositories registeredPackageRepositories,
			IPackageViewModelFactory packageViewModelFactory,
			ITaskFactory taskFactory)
			: base(registeredPackageRepositories, packageViewModelFactory, taskFactory)
		{
			this.packageManagementEvents = packageManagementEvents;
			
			recentPackageRepository = registeredPackageRepositories.RecentPackageRepository;
			
			packageManagementEvents.ParentPackageInstalled += ParentPackageInstalled;
			packageManagementEvents.ParentPackageUninstalled += ParentPackageUninstalled;
		}
		
		void ParentPackageInstalled(object sender, EventArgs e)
		{
			ReadPackages();
		}
		
		void ParentPackageUninstalled(object sender, EventArgs e)
		{
			ReadPackages();
		}
		
		protected override void OnDispose()
		{
			packageManagementEvents.ParentPackageInstalled -= ParentPackageInstalled;
			packageManagementEvents.ParentPackageUninstalled -= ParentPackageUninstalled;
		}
		
		protected override IQueryable<IPackage> GetAllPackages()
		{
			return recentPackageRepository.GetPackages();
		}
	}
}
