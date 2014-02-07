//
// ProjectTemplatePackageInstaller.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using MonoDevelop.Ide;
using NuGet;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System.Linq;

namespace MonoDevelop.PackageManagement
{
	public class ProjectTemplatePackageInstaller : IProjectTemplatePackageInstaller
	{
		public void Run (IWorkspaceFileObject item, IList<ProjectTemplatePackageReferenceCollection> packageReferences)
		{
			DispatchService.BackgroundDispatch (() => InstallPackages (item, packageReferences));
		}

		void InstallPackages (IWorkspaceFileObject item, IList<ProjectTemplatePackageReferenceCollection> packageReferences)
		{
			using (IProgressMonitor monitor = CreateProgressMonitor ()) {
				var solution = item as Solution;
				var project = item as DotNetProject;
				if (solution != null) {
					InstallPackagesIntoSolution (packageReferences);
				} else if (project != null) {
					InstallPackagesIntoProject (item, packageReferences [0]);
				}
			}
		}

		IProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Installing Packages..."),
				Stock.StatusSolutionOperation,
				true,
				false,
				false);
		}

		void InstallPackagesIntoSolution (IList<ProjectTemplatePackageReferenceCollection> packageReferences)
		{
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			for (int i = 0; i < solution.RootFolder.Items.Count; ++i) {
				var project = solution.RootFolder.Items [i] as DotNetProject;
				if (project != null) {
					InstallPackagesIntoProject (project, packageReferences [i]);
				}
			}
		}

		void InstallPackagesIntoProject (DotNetProject dotNetProject, ProjectTemplatePackageReferenceCollection packageReferences)
		{
			IPackageManagementProject project = PackageManagementServices.Solution.GetProject (PackageManagementServices.RegisteredPackageRepositories.ActiveRepository, dotNetProject);
			foreach (ProjectTemplatePackageReference packageReference in packageReferences) {
				InstallPackageAction action = project.CreateInstallPackageAction ();
				action.PackageId = packageReference.Id;
				action.PackageVersion = new SemanticVersion (packageReference.Version);

				action.Execute ();
			}
		}

		void InstallPackagesIntoProject (IWorkspaceFileObject project, ProjectTemplatePackageReferenceCollection packageReferences)
		{
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			var dotNetProject = solution.GetAllProjects ().FirstOrDefault (p => p.Name == project.Name) as DotNetProject;

			InstallPackagesIntoProject (dotNetProject, packageReferences);
		}
	}
}

