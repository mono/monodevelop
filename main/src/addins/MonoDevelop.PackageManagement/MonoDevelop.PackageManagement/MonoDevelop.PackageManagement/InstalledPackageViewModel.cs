// 
// InstalledPackageViewModel.cs
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
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class InstalledPackageViewModel : PackageViewModel
	{
		public InstalledPackageViewModel(
			IPackageViewModelParent parent,
			IPackageFromRepository package,
			SelectedProjectsForInstalledPackages selectedProjects,
			IPackageManagementEvents packageManagementEvents,
			IPackageActionRunner actionRunner,
			ILogger logger)
			: base(parent, package, selectedProjects, packageManagementEvents, actionRunner, logger)
		{
		}
		
		public override IList<ProcessPackageAction> GetProcessPackageActionsForSelectedProjects(
			IList<IPackageManagementSelectedProject> selectedProjects)
		{
			var actions = new List<ProcessPackageAction>();
			foreach (IPackageManagementSelectedProject selectedProject in selectedProjects) {
				ProcessPackageAction action = CreatePackageAction(selectedProject);
				if (action != null) {
					actions.Add(action);
				}
			}
			return actions;
		}
		
		ProcessPackageAction CreatePackageAction(IPackageManagementSelectedProject selectedProject)
		{
			if (selectedProject.IsSelected) {
				return base.CreateInstallPackageAction(selectedProject);
			}
			return CreateUninstallPackageActionForSelectedProject(selectedProject);
		}
		
		ProcessPackageAction CreateUninstallPackageActionForSelectedProject(IPackageManagementSelectedProject selectedProject)
		{
			ProcessPackageAction action = base.CreateUninstallPackageAction(selectedProject);
			if (IsPackageInstalled(action.Project)) {
				return action;
			}
			return null;
		}
		
		bool IsPackageInstalled(IPackageManagementProject project)
		{
			IPackage package = GetPackage();
			return project.IsPackageInstalled(package);
		}
		
		protected override bool AnyProjectsSelected(IList<IPackageManagementSelectedProject> projects)
		{
			return true;
		}
	}
}
