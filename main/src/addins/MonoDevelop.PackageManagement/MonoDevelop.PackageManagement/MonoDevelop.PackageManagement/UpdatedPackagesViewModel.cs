// 
// UpdatedPackagesViewModel.cs
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
using System.Linq;

using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class UpdatedPackagesViewModel : PackagesViewModel
	{
		PackageManagementSelectedProjects selectedProjects;
		UpdatedPackages updatedPackages;
		string errorMessage = String.Empty;
		ILogger logger;
		IPackageManagementEvents packageManagementEvents;
		
		public UpdatedPackagesViewModel(
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredPackageRepositories,
			UpdatedPackageViewModelFactory packageViewModelFactory,
			ITaskFactory taskFactory)
			: base(
				registeredPackageRepositories,
				packageViewModelFactory,
				taskFactory)
		{
			this.selectedProjects = new PackageManagementSelectedProjects(solution);
			this.logger = packageViewModelFactory.Logger;
			this.packageManagementEvents = packageViewModelFactory.PackageManagementEvents;
			
			packageManagementEvents.ParentPackagesUpdated += PackagesUpdated;
			
			ShowPackageSources = true;
			ShowUpdateAllPackages = true;
			ShowPrerelease = true;
		}
		
		void PackagesUpdated(object sender, EventArgs e)
		{
			ReadPackages();
		}
		
		protected override void OnDispose()
		{
			packageManagementEvents.ParentPackagesUpdated -= PackagesUpdated;
		}
		
		protected override void UpdateRepositoryBeforeReadPackagesTaskStarts()
		{
			try {
				IPackageRepository repository = RegisteredPackageRepositories.ActiveRepository;
				IQueryable<IPackage> installedPackages = GetInstalledPackages(repository);
				updatedPackages = new UpdatedPackages(installedPackages, repository);
			} catch (Exception ex) {
				errorMessage = ex.Message;
			}
		}
		
		IQueryable<IPackage> GetInstalledPackages(IPackageRepository aggregateRepository)
		{
			return selectedProjects.GetInstalledPackages(aggregateRepository);
		}
		
		protected override IQueryable<IPackage> GetAllPackages()
		{
			if (updatedPackages == null) {
				ThrowSavedException();
			}
			return GetUpdatedPackages();
		}
		
		void ThrowSavedException()
		{
			throw new ApplicationException(errorMessage);
		}
		
		IQueryable<IPackage> GetUpdatedPackages()
		{
			return updatedPackages.GetUpdatedPackages(IncludePrerelease).AsQueryable();
		}
		
		protected override void TryUpdatingAllPackages()
		{
			List<IPackageFromRepository> packages = GetPackagesFromViewModels().ToList();
			using (IDisposable operation = StartUpdateOperation(packages.First())) {
				var factory = new UpdatePackagesActionFactory(logger, packageManagementEvents);
				IUpdatePackagesAction action = factory.CreateAction(selectedProjects, packages);
				ActionRunner.Run(action);
			}
		}
		
		IDisposable StartUpdateOperation(IPackageFromRepository package)
		{
			return package.Repository.StartUpdateOperation();
		}
		
		IEnumerable<IPackageFromRepository> GetPackagesFromViewModels()
		{
			return PackageViewModels.Select(viewModel => viewModel.GetPackage() as IPackageFromRepository);
		}
	}
}