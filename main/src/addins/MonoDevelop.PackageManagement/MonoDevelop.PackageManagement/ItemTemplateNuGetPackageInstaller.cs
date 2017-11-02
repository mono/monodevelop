//
// ItemTemplateNuGetPackageInstaller.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using NuGet.ProjectManagement;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	class ItemTemplateNuGetPackageInstaller : ItemTemplatePackageInstaller
	{
		IBackgroundPackageActionRunner backgroundPackageActionRunner;

		public ItemTemplateNuGetPackageInstaller ()
			: this (PackageManagementServices.BackgroundPackageActionRunner)
		{
		}

		public ItemTemplateNuGetPackageInstaller (
			IBackgroundPackageActionRunner backgroundPackageActionRunner)
		{
			this.backgroundPackageActionRunner = backgroundPackageActionRunner;
		}

		public override async Task Run (Project project, IEnumerable<TemplatePackageReference> packageReferences)
		{
			var dotNetProject = project as DotNetProject;
			if (dotNetProject == null)
				return;

			var installPackageActions = CreatePackageActions (dotNetProject, packageReferences);
			if (!installPackageActions.Any ())
				return;

			var progressMessage = GetProgressMonitorStatusMessage (installPackageActions);
			backgroundPackageActionRunner.Run (progressMessage, installPackageActions);
		}

		List<InstallNuGetPackageAction> CreatePackageActions (DotNetProject project, IEnumerable<TemplatePackageReference> packageReferences)
		{
			var repositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			var repositories = repositoryProvider.GetRepositories ().ToList ();
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);

			var installPackageActions = new List<InstallNuGetPackageAction> ();

			var context = new NuGetProjectContext {
				FileConflictResolution = FileConflictAction.IgnoreAll
			};

			foreach (var packageReference in packageReferences) {
				var action = new InstallNuGetPackageAction (
					repositories,
					solutionManager,
					new DotNetProjectProxy (project),
					context) {
					LicensesMustBeAccepted = false,
					OpenReadmeFile = false,
					PackageId = packageReference.Id,
					Version = new NuGetVersion (packageReference.Version)
				};

				installPackageActions.Add (action);
			}

			return installPackageActions;
		}

		ProgressMonitorStatusMessage GetProgressMonitorStatusMessage (List<InstallNuGetPackageAction> packageActions)
		{
			if (packageActions.Count == 1) {
				string packageId = packageActions.First ().PackageId;
				return ProgressMonitorStatusMessageFactory.CreateInstallingSinglePackageMessage (packageId);
			}
			return ProgressMonitorStatusMessageFactory.CreateInstallingMultiplePackagesMessage (packageActions.Count);
		}
	}
}
