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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class ProjectTemplateNuGetPackageInstaller : ProjectTemplatePackageInstaller
	{
		IBackgroundPackageActionRunner backgroundPackageActionRunner;

		public ProjectTemplateNuGetPackageInstaller ()
			: this(PackageManagementServices.BackgroundPackageActionRunner)
		{
		}

		public ProjectTemplateNuGetPackageInstaller (
			IBackgroundPackageActionRunner backgroundPackageActionRunner)
		{
			this.backgroundPackageActionRunner = backgroundPackageActionRunner;
		}

		public override void Run (Solution solution, IList<PackageReferencesForCreatedProject> packageReferencesForCreatedProjects)
		{
			List<IPackageAction> installPackageActions = CreatePackageActions (solution, packageReferencesForCreatedProjects);
			if (!installPackageActions.Any ())
				return;

			ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateInstallingProjectTemplatePackagesMessage ();
			PackageManagementMSBuildExtension.PackageRestoreTask =
				backgroundPackageActionRunner.RunAsync (progressMessage, installPackageActions, clearConsole: false);
		}

		List<IPackageAction> CreatePackageActions (Solution solution, IList<PackageReferencesForCreatedProject> packageReferencesForCreatedProjects)
		{
			List<IPackageAction> actions = CreateInstallPackageActions (solution, packageReferencesForCreatedProjects);
			if (actions.Any () && PackageManagementServices.Options.IsCheckForPackageUpdatesOnOpeningSolutionEnabled) {
				actions.Add (new CheckForUpdatedPackagesAction (solution));
			}
			return actions;
		}

		internal List<IPackageAction> CreateInstallPackageActions (Solution solution, IList<PackageReferencesForCreatedProject> packageReferencesForCreatedProjects)
		{
			var repositoryProvider = new ProjectTemplateSourceRepositoryProvider ();

			var installPackageActions = new List<IPackageAction> ();

			foreach (PackageReferencesForCreatedProject packageReferences in packageReferencesForCreatedProjects) {
				var project = solution.GetAllProjects ().FirstOrDefault (p => p.Name == packageReferences.ProjectName) as DotNetProject;
				if (project != null) {
					installPackageActions.AddRange (CreateInstallPackageActions (project, packageReferences, repositoryProvider));
				}
			}

			return installPackageActions;
		}

		IEnumerable<InstallNuGetPackageAction> CreateInstallPackageActions (
			DotNetProject dotNetProject,
			PackageReferencesForCreatedProject projectPackageReferences,
			ProjectTemplateSourceRepositoryProvider repositoryProvider)
		{
			foreach (ProjectTemplatePackageReference packageReference in projectPackageReferences.PackageReferences) {
				var action = CreateInstallNuGetPackageAction (dotNetProject, repositoryProvider, packageReference);
				action.PackageId = packageReference.Id;
				action.Version = GetPackageVersion (packageReference);

				yield return action;
			}
		}

		InstallNuGetPackageAction CreateInstallNuGetPackageAction (
			DotNetProject dotNetProject,
			ProjectTemplateSourceRepositoryProvider repositoryProvider,
			ProjectTemplatePackageReference packageReference)
		{
			var primaryRepositories = repositoryProvider.GetRepositories (packageReference).ToList ();
			var secondaryRepositories = GetSecondaryRepositories (primaryRepositories, packageReference);

			var context = new NuGetProjectContext {
				FileConflictResolution = FileConflictAction.IgnoreAll
			};
			return new InstallNuGetPackageAction (
				primaryRepositories,
				secondaryRepositories,
				PackageManagementServices.Workspace.GetSolutionManager (dotNetProject.ParentSolution),
				new DotNetProjectProxy (dotNetProject),
				context) {
				LicensesMustBeAccepted = packageReference.RequireLicenseAcceptance,
				OpenReadmeFile = false
			};
		}

		/// <summary>
		/// If the package is a local package then we prevent NuGet from using online package sources
		/// defined in the NuGet.Config file by using the returning the primaryRepositories. 
		/// Returning null allows all enabled package sources to be used when resolving dependencies.
		/// </summary>
		static IEnumerable<SourceRepository> GetSecondaryRepositories (
			IEnumerable<SourceRepository> primaryRepositories, ProjectTemplatePackageReference packageReference)
		{
			if (packageReference.IsLocalPackage || packageReference.Directory.IsNotNull) {
				return primaryRepositories;
			}
			return null;
		}

		NuGetVersion GetPackageVersion (ProjectTemplatePackageReference packageReference)
		{
			if (!string.IsNullOrEmpty (packageReference.Version)) {
				return new NuGetVersion (packageReference.Version);
			}
			return null;
		}
	}
}

