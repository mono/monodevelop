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
		IPackageManagementSolution solution;
		IRegisteredPackageRepositories registeredPackageRepositories;
		BackgroundPackageActionRunner backgroundActionRunner;

		public PackageManagementProjectOperations (
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredPackageRepositories,
			BackgroundPackageActionRunner backgroundActionRunner,
			IPackageManagementEvents packageManagementEvents)
		{
			this.solution = solution;
			this.registeredPackageRepositories = registeredPackageRepositories;
			this.backgroundActionRunner = backgroundActionRunner;

			packageManagementEvents.ParentPackageInstalled += PackageInstalled;
			packageManagementEvents.ParentPackageUninstalled += PackageUninstalled;
		}

		public event EventHandler<PackageManagementPackageReferenceEventArgs> PackageReferenceAdded;
		public event EventHandler<PackageManagementPackageReferenceEventArgs> PackageReferenceRemoved;

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

		void InstallPackages (
			IEnumerable<SourceRepository> repositories,
			Project project,
			IEnumerable<PackageManagementPackageReference> packages,
			bool licensesAccepted)
		{
			List<INuGetPackageAction> actions = null;

			Runtime.RunInMainThread (() => {
				var repositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
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

			ProgressMonitorStatusMessage progressMessage = GetProgressMonitorStatusMessages (actions);
			backgroundActionRunner.Run (progressMessage, actions);
		}

		ProgressMonitorStatusMessage GetProgressMonitorStatusMessages (List<INuGetPackageAction> packageActions)
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
					var dotNetProject = (DotNetProject)project;
					var nugetProject = CreateNuGetProject (dotNetProject);

					var packagesBeingInstalled = GetPackagesBeingInstalled (dotNetProject).ToList ();

					var packages = await Task.Run (() => nugetProject.GetInstalledPackagesAsync (CancellationToken.None)).ConfigureAwait (false);

					var packageReferences = packages
						.Select (package => CreatePackageReference (package.PackageIdentity))
						.ToList ();

					packageReferences.AddRange (GetMissingPackagesBeingInstalled (packageReferences, packagesBeingInstalled));

					return packageReferences;
				}).Result;
			} catch (Exception ex) {
				LoggingService.LogError ("GetInstalledPackages error.", ex);
				throw ExceptionUtility.Unwrap (ex);
			}
		}

		NuGetProject CreateNuGetProject (DotNetProject project)
		{
			if (project.ParentSolution != null) {
				var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
				return solutionManager.GetNuGetProject (new DotNetProjectProxy (project));
			}

			return new MonoDevelopNuGetProjectFactory ().CreateNuGetProject (project);
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
				.Select (installAction => CreatePackageReference (installAction));
		}

		static PackageManagementPackageReference CreatePackageReference (PackageIdentity package)
		{
			return new PackageManagementPackageReference (package.Id, package.Version.ToString ());
		}

		static PackageManagementPackageReference CreatePackageReference (IInstallNuGetPackageAction installAction)
		{
			return new PackageManagementPackageReference (
				installAction.GetPackageId (), 
				GetNuGetVersionString (installAction.GetPackageVersion ()));
		}

		static string GetNuGetVersionString (NuGetVersion version)
		{
			if (version != null)
				return version.ToString ();

			return null;
		}

		void PackageUninstalled (object sender, ParentPackageOperationEventArgs e)
		{
			OnPackageReferencedRemoved (e);
		}

		void PackageInstalled (object sender, ParentPackageOperationEventArgs e)
		{
			OnPackageReferenceAdded (e);
		}

		void OnPackageReferencedRemoved (ParentPackageOperationEventArgs e)
		{
			var handler = PackageReferenceRemoved;
			if (handler != null) {
				handler (this, CreateEventArgs (e));
			}
		}

		void OnPackageReferenceAdded (ParentPackageOperationEventArgs e)
		{
			var handler = PackageReferenceAdded;
			if (handler != null) {
				handler (this, CreateEventArgs (e));
			}
		}

		PackageManagementPackageReferenceEventArgs CreateEventArgs (ParentPackageOperationEventArgs e)
		{
			return new PackageManagementPackageReferenceEventArgs (
				e.Project.DotNetProject,
				e.Package.Id,
				e.Package.Version.ToString ());
		}
	}
}

