//
// ProjectTemplateNuGetPackageInstaller.cs
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
	public class ProjectTemplateNuGetPackageInstaller : ProjectTemplatePackageInstaller
	{
		IPackageManagementSolution packageManagementSolution;
		IPackageRepositoryCache packageRepositoryCache;

		public ProjectTemplateNuGetPackageInstaller ()
			: this(
				PackageManagementServices.Solution,
				PackageManagementServices.ProjectTemplatePackageRepositoryCache)
		{
		}

		public ProjectTemplateNuGetPackageInstaller (
			IPackageManagementSolution solution,
			IPackageRepositoryCache packageRepositoryCache)
		{
			this.packageManagementSolution = solution;
			this.packageRepositoryCache = packageRepositoryCache;
		}

		public override void Run (IList<PackageReferencesForCreatedProject> packageReferencesForCreatedProjects)
		{
			DispatchService.BackgroundDispatch (() => InstallPackages (packageReferencesForCreatedProjects));
		}

		void InstallPackages (IList<PackageReferencesForCreatedProject> packageReferencesForCreatedProjects)
		{
			using (IProgressMonitor monitor = CreateProgressMonitor ()) {
				InstallPackagesIntoSolution (packageReferencesForCreatedProjects);
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

		void InstallPackagesIntoSolution (IList<PackageReferencesForCreatedProject> projectsPackageReferences)
		{
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			foreach (PackageReferencesForCreatedProject packageReferences in projectsPackageReferences) {
				var project = solution.GetAllProjects ().First (p => p.Name == packageReferences.ProjectName) as DotNetProject;
				if (project != null) {
					InstallPackagesIntoProject (project, packageReferences);
				}
			}
		}

		void InstallPackagesIntoProject (DotNetProject dotNetProject, PackageReferencesForCreatedProject projectPackageReferences)
		{
			IPackageManagementProject project = CreatePackageManagementProject (dotNetProject);
			foreach (ProjectTemplatePackageReference packageReference in projectPackageReferences.PackageReferences) {
				InstallPackageAction action = project.CreateInstallPackageAction ();
				action.PackageId = packageReference.Id;
				action.PackageVersion = new SemanticVersion (packageReference.Version);

				action.Execute ();
			}
		}

		IPackageManagementProject CreatePackageManagementProject (DotNetProject dotNetProject)
		{
			return packageManagementSolution.GetProject (packageRepositoryCache.CreateAggregateRepository (), dotNetProject);
		}
	}
}

