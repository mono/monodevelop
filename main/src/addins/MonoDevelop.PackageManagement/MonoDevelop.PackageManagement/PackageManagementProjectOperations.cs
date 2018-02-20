//
// PackageManagementProjectOperations.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementProjectOperations : IPackageManagementProjectOperations
	{
		BackgroundPackageActionRunner backgroundActionRunner;

		public PackageManagementProjectOperations (
			BackgroundPackageActionRunner backgroundActionRunner,
			IPackageManagementEvents packageManagementEvents)
		{
			this.backgroundActionRunner = backgroundActionRunner;

			packageManagementEvents.PackageInstalled += PackageInstalled;
			packageManagementEvents.PackageUninstalled += PackageUninstalled;
			packageManagementEvents.PackagesRestored += OnPackagesRestored;
		}

		public event EventHandler<PackageManagementPackageReferenceEventArgs> PackageReferenceAdded;
		public event EventHandler<PackageManagementPackageReferenceEventArgs> PackageReferenceRemoved;
		public event EventHandler PackagesRestored;

		public void InstallPackages (
			string packageSourceUrl,
			Project project,
			IEnumerable<PackageManagementPackageReference> packages)
		{
			InstallPackages (packageSourceUrl, project, packages, licensesAccepted: false);
		}

		public void InstallPackages (
			string packageSourceUrl,
			Project project,
			IEnumerable<PackageManagementPackageReference> packages,
			bool licensesAccepted)
		{
			var repositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			var repository = repositoryProvider.CreateRepository (new PackageSource (packageSourceUrl));

			InstallPackages (new [] { repository }, project, packages, licensesAccepted);
		}

		public void InstallPackages (
			Project project,
			IEnumerable<PackageManagementPackageReference> packages)
		{
			var repositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			var repositories = repositoryProvider.GetRepositories ().ToList ();
			InstallPackages (repositories, project, packages, licensesAccepted: false);
		}

		/// <summary>
		/// Installs NuGet packages into the selected project using the enabled package sources.
		/// </summary>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		public Task InstallPackagesAsync (Project project, IEnumerable<PackageManagementPackageReference> packages)
		{
			var repositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			var repositories = repositoryProvider.GetRepositories ().ToList ();

			var actions = CreateInstallActions (repositories, project, packages, licensesAccepted: false).ToList ();

			ProgressMonitorStatusMessage progressMessage = GetInstallingStatusMessages (actions);
			return backgroundActionRunner.RunAsync (progressMessage, actions);
		}

		/// <summary>
		/// Installs NuGet packages into the selected project using the enabled package sources.
		/// </summary>
		/// <param name="project">Project.</param>
		/// <param name="packages">Packages.</param>
		public Task InstallPackagesAsync (Project project, IEnumerable<PackageManagementPackageReference> packages, bool licensesAccepted)
		{
			var repositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			var repositories = repositoryProvider.GetRepositories ().ToList ();

			var actions = CreateInstallActions (repositories, project, packages, licensesAccepted).ToList ();

			ProgressMonitorStatusMessage progressMessage = GetInstallingStatusMessages (actions);
			return backgroundActionRunner.RunAsync (progressMessage, actions);
		}

		void InstallPackages (
			IEnumerable<SourceRepository> repositories,
			Project project,
			IEnumerable<PackageManagementPackageReference> packages,
			bool licensesAccepted)
		{
			var actions = CreateInstallActions (repositories, project, packages, licensesAccepted).ToList ();
			ProgressMonitorStatusMessage progressMessage = GetInstallingStatusMessages (actions);
			backgroundActionRunner.Run (progressMessage, actions);
		}

		internal IEnumerable<INuGetPackageAction> CreateInstallActions (
			string packageSourceUrl,
			Project project,
			IEnumerable<PackageManagementPackageReference> packages)
		{
			var repositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			var repository = repositoryProvider.CreateRepository (new PackageSource (packageSourceUrl));

			return CreateInstallActions (new [] { repository }, project, packages, false);
		}

		IEnumerable<INuGetPackageAction> CreateInstallActions (
			IEnumerable<SourceRepository> repositories,
			Project project,
			IEnumerable<PackageManagementPackageReference> packages,
			bool licensesAccepted)
		{
			List<INuGetPackageAction> actions = null;

			Runtime.RunInMainThread (() => {
				var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
				var dotNetProject = new DotNetProjectProxy ((DotNetProject)project);
				var context = new NuGetProjectContext ();

				actions = packages.Select (packageReference => {
					var action = new InstallNuGetPackageAction (
						repositories,
						solutionManager,
						dotNetProject,
						context);
					action.PackageId = packageReference.Id;
					action.Version = packageReference.GetNuGetVersion ();
					action.LicensesMustBeAccepted = !licensesAccepted;
					return (INuGetPackageAction)action;
				}).ToList ();
			}).Wait ();

			return actions;
		}

		public Task InstallPackagesAsync (
			string packageSourceUrl,
			Project project,
			IEnumerable<PackageManagementPackageReference> packages)
		{
			var repositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			var repository = repositoryProvider.CreateRepository (new PackageSource (packageSourceUrl));
			var repositories = new [] { repository };

			var actions = CreateInstallActions (repositories, project, packages, licensesAccepted: false).ToList ();

			ProgressMonitorStatusMessage progressMessage = GetInstallingStatusMessages (actions);
			return backgroundActionRunner.RunAsync (progressMessage, actions);
		}

		ProgressMonitorStatusMessage GetInstallingStatusMessages (List<INuGetPackageAction> packageActions)
		{
			if (packageActions.Count == 1) {
				string packageId = packageActions.OfType<INuGetPackageAction> ().First ().PackageId;
				return ProgressMonitorStatusMessageFactory.CreateInstallingSinglePackageMessage (packageId);
			}
			return ProgressMonitorStatusMessageFactory.CreateInstallingMultiplePackagesMessage (packageActions.Count);
		}

		public IEnumerable<PackageManagementPackageReference> GetInstalledPackages (Project project)
		{
			try {
				return Runtime.RunInMainThread (async () => {
					if (!IsValidProject (project)) {
						return Enumerable.Empty<PackageManagementPackageReference> ();
					}

					var dotNetProject = (DotNetProject)project;
					var solutionManager = GetSolutionManager (dotNetProject);
					var nugetProject = CreateNuGetProject (solutionManager, dotNetProject);
					var pathResolver = CreatePathResolver (solutionManager);

					var packagesBeingInstalled = GetPackagesBeingInstalled (dotNetProject).ToList ();

					var packages = await Task.Run (() => nugetProject.GetInstalledPackagesAsync (CancellationToken.None)).ConfigureAwait (false);

					var packageReferences = packages
						.Select (package => CreatePackageReference (package.PackageIdentity, nugetProject, pathResolver))
						.ToList ();

					packageReferences.AddRange (GetMissingPackagesBeingInstalled (packageReferences, packagesBeingInstalled));

					return packageReferences;
				}).Result;
			} catch (Exception ex) {
				LoggingService.LogError ("GetInstalledPackages error.", ex);
				throw ExceptionUtility.Unwrap (ex);
			}
		}

		static bool IsValidProject (Project project)
		{
			return project is DotNetProject &&
				!String.IsNullOrEmpty (project.Name);
		}

		IMonoDevelopSolutionManager GetSolutionManager (DotNetProject project)
		{
			if (project.ParentSolution != null) {
				return PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			}
			return null;
		}

		NuGetProject CreateNuGetProject (IMonoDevelopSolutionManager solutionManager, DotNetProject project)
		{
			if (solutionManager != null) {
				return solutionManager.GetNuGetProject (new DotNetProjectProxy (project));
			}

			return new MonoDevelopNuGetProjectFactory ().CreateNuGetProject (project);
		}

		PackageManagementPathResolver CreatePathResolver (IMonoDevelopSolutionManager solutionManager)
		{
			if (solutionManager != null)
				return new PackageManagementPathResolver (solutionManager);
			return new PackageManagementPathResolver ();
		}

		IEnumerable<PackageManagementPackageReference> GetMissingPackagesBeingInstalled (
			IEnumerable<PackageManagementPackageReference> existingPackages,
			IEnumerable<PackageManagementPackageReference> packagesBeingInstalled)
		{
			return packagesBeingInstalled
				.Where (package => !existingPackages.Any (existingPackage => existingPackage.Id == package.Id));
		}

		static IEnumerable<PackageManagementPackageReference> GetPackagesBeingInstalled (DotNetProject project)
		{
			return PackageManagementServices.BackgroundPackageActionRunner.PendingInstallActionsForProject (project)
				.Select (CreatePackageReference);
		}

		static PackageManagementPackageReference CreatePackageReference (
			PackageIdentity package,
			NuGetProject nugetProject,
			PackageManagementPathResolver pathResolver)
		{
			return new PackageManagementPackageReference (
				package.Id,
				package.Version.ToString (),
				pathResolver.GetPackageInstallPath (nugetProject, package));
		}

		static PackageManagementPackageReference CreatePackageReference (IInstallNuGetPackageAction installAction)
		{
			return new PackageManagementPackageReference (
				installAction.PackageId, 
				GetNuGetVersionString (installAction.Version));
		}

		static string GetNuGetVersionString (NuGetVersion version)
		{
			if (version != null)
				return version.ToString ();

			return null;
		}

		void PackageUninstalled (object sender, PackageManagementEventArgs e)
		{
			OnPackageReferencedRemoved (e);
		}

		void PackageInstalled (object sender, PackageManagementEventArgs e)
		{
			OnPackageReferenceAdded (e);
		}

		void OnPackagesRestored (object sender, EventArgs e)
		{
			PackagesRestored?.Invoke (sender, e);
		}

		void OnPackageReferencedRemoved (PackageManagementEventArgs e)
		{
			var handler = PackageReferenceRemoved;
			if (handler != null) {
				handler (this, CreateEventArgs (e));
			}
		}

		void OnPackageReferenceAdded (PackageManagementEventArgs e)
		{
			var handler = PackageReferenceAdded;
			if (handler != null) {
				handler (this, CreateEventArgs (e));
			}
		}

		PackageManagementPackageReferenceEventArgs CreateEventArgs (PackageManagementEventArgs e)
		{
			return new PackageManagementPackageReferenceEventArgs (
				e.Project.DotNetProject,
				e.Package.Id,
				e.Package.Version.ToString ());
		}

		public void UninstallPackages (
			Project project,
			IEnumerable<string> packages,
			bool removeDependencies = false)
		{
			var actions = CreateUninstallActions (project, packages, removeDependencies).ToList ();
			ProgressMonitorStatusMessage progressMessage = GetUninstallingStatusMessages (actions);
			backgroundActionRunner.Run (progressMessage, actions);
		}

		IEnumerable<INuGetPackageAction> CreateUninstallActions (
			Project project,
			IEnumerable<string> packages,
			bool removeDependencies)
		{
			List<INuGetPackageAction> actions = null;

			Runtime.RunInMainThread (() => {
				var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
				var dotNetProject = new DotNetProjectProxy ((DotNetProject)project);

				actions = packages.Select (packageId => {
					var action = new UninstallNuGetPackageAction (
						solutionManager,
						dotNetProject);
					action.PackageId = packageId;
					action.RemoveDependencies = removeDependencies;
					action.IsErrorWhenPackageNotInstalled = false;
					return (INuGetPackageAction)action;
				}).ToList ();
			}).Wait ();

			return actions;
		}

		ProgressMonitorStatusMessage GetUninstallingStatusMessages (List<INuGetPackageAction> packageActions)
		{
			if (packageActions.Count == 1) {
				string packageId = packageActions.OfType<INuGetPackageAction> ().First ().PackageId;
				return ProgressMonitorStatusMessageFactory.CreateRemoveSinglePackageMessage (packageId);
			}
			return ProgressMonitorStatusMessageFactory.CreateRemovingMultiplePackagesMessage (packageActions.Count);
		}

		public Task UninstallPackagesAsync (
			Project project,
			IEnumerable<string> packages,
			bool removeDependencies = false)
		{
			var actions = CreateUninstallActions (project, packages, removeDependencies).ToList ();
			ProgressMonitorStatusMessage progressMessage = GetUninstallingStatusMessages (actions);
			return backgroundActionRunner.RunAsync (progressMessage, actions);
		}
	}
}

