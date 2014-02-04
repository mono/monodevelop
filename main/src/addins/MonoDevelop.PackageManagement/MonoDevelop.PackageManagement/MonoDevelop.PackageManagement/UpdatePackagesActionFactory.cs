// 
// UpdatePackagesActionFactory.cs
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
	public class UpdatePackagesActionFactory
	{
		ILogger logger;
		IPackageManagementEvents packageManagementEvents;
		bool singleProjectSelected;
		IPackageManagementProject project;
		PackageManagementSelectedProjects selectedProjects;
		IEnumerable<IPackageFromRepository> packages;
		
		public UpdatePackagesActionFactory(ILogger logger, IPackageManagementEvents packageManagementEvents)
		{
			this.logger = logger;
			this.packageManagementEvents = packageManagementEvents;
		}
		
		public IUpdatePackagesAction CreateAction(
			PackageManagementSelectedProjects selectedProjects,
			IEnumerable<IPackageFromRepository> packages)
		{
			this.selectedProjects = selectedProjects;
			this.packages = packages;
			
			singleProjectSelected = selectedProjects.HasSingleProjectSelected();
			
			CreateProjectForDetermingPackageOperations();
			IUpdatePackagesAction action = CreateActionInternal();
			action.AddPackages(packages);
			action.Logger = logger;
			
			IEnumerable<PackageOperation> operations = GetPackageOperations(action);
			action.AddOperations(operations);
			
			return action;
		}
		
		IEnumerable<PackageOperation> GetPackageOperations(IUpdatePackagesAction action)
		{
			return project.GetUpdatePackagesOperations(packages, action);
		}
		
		IUpdatePackagesAction CreateActionInternal()
		{
			if (singleProjectSelected) {
				return project.CreateUpdatePackagesAction();
			} else {
				return new UpdateSolutionPackagesAction(selectedProjects.Solution, packageManagementEvents);
			}
		}
		
		void CreateProjectForDetermingPackageOperations()
		{
			IPackageRepository repository = packages.First().Repository;
			if (singleProjectSelected) {
				project = selectedProjects.GetSingleProjectSelected(repository);
			} else {
				project = selectedProjects.Solution.GetProjects(repository).First();
			}
			project.Logger = logger;
		}
	}
}