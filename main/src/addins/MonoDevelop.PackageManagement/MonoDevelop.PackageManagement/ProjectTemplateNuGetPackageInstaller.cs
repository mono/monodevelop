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

using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using MonoDevelop.Ide;
using NuGet;
using System.Linq;

namespace MonoDevelop.PackageManagement
{
	public class ProjectTemplateNuGetPackageInstaller : ProjectTemplatePackageInstaller
	{
		IPackageManagementSolution packageManagementSolution;
		IPackageRepositoryCache packageRepositoryCache;
		IBackgroundPackageActionRunner backgroundPackageActionRunner;

		public ProjectTemplateNuGetPackageInstaller ()
			: this(
				PackageManagementServices.Solution,
				PackageManagementServices.ProjectTemplatePackageRepositoryCache,
				PackageManagementServices.BackgroundPackageActionRunner)
		{
		}

		public ProjectTemplateNuGetPackageInstaller (
			IPackageManagementSolution solution,
			IPackageRepositoryCache packageRepositoryCache,
			IBackgroundPackageActionRunner backgroundPackageActionRunner)
		{
			this.packageManagementSolution = solution;
			this.packageRepositoryCache = packageRepositoryCache;
			this.backgroundPackageActionRunner = backgroundPackageActionRunner;
		}

		public override void Run (Solution solution, IList<PackageReferencesForCreatedProject> packageReferencesForCreatedProjects)
		{
			List<IPackageAction> installPackageActions = CreatePackageActions (solution, packageReferencesForCreatedProjects);
			if (!installPackageActions.Any ())
				return;

			ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateInstallingProjectTemplatePackagesMessage ();
			backgroundPackageActionRunner.Run (progressMessage, installPackageActions);
		}

		List<IPackageAction> CreatePackageActions (Solution solution, IList<PackageReferencesForCreatedProject> packageReferencesForCreatedProjects)
		{
			List<IPackageAction> actions = CreateInstallPackageActions (solution, packageReferencesForCreatedProjects);
			if (actions.Any () && PackageManagementServices.Options.IsCheckForPackageUpdatesOnOpeningSolutionEnabled) {
				actions.Add (new CheckForUpdatedPackagesAction ());
			}
			return actions;
		}

		List<IPackageAction> CreateInstallPackageActions (Solution solution, IList<PackageReferencesForCreatedProject> packageReferencesForCreatedProjects)
		{
			var installPackageActions = new List<IPackageAction> ();

			foreach (PackageReferencesForCreatedProject packageReferences in packageReferencesForCreatedProjects) {
				var project = solution.GetAllProjects ().FirstOrDefault (p => p.Name == packageReferences.ProjectName) as DotNetProject;
				if (project != null) {
					installPackageActions.AddRange (CreateInstallPackageActions (project, packageReferences));
				}
			}

			return installPackageActions;
		}

		IEnumerable<InstallPackageAction> CreateInstallPackageActions (DotNetProject dotNetProject, PackageReferencesForCreatedProject projectPackageReferences)
		{
			IPackageManagementProject project = CreatePackageManagementProject (dotNetProject);
			foreach (ProjectTemplatePackageReference packageReference in projectPackageReferences.PackageReferences) {
				InstallPackageAction action = project.CreateInstallPackageAction ();
				action.PackageId = packageReference.Id;
				action.PackageVersion = GetPackageVersion (packageReference);

				yield return action;
			}
		}

		SemanticVersion GetPackageVersion (ProjectTemplatePackageReference packageReference)
		{
			if (!string.IsNullOrEmpty (packageReference.Version)) {
				return new SemanticVersion (packageReference.Version);
			}
			return null;
		}

		IPackageManagementProject CreatePackageManagementProject (DotNetProject project)
		{
			var dotNetProject = new DotNetProjectProxy (project);
			return packageManagementSolution.GetProject (packageRepositoryCache.CreateAggregateWithPriorityMachineCacheRepository (), dotNetProject);
		}
	}
}

