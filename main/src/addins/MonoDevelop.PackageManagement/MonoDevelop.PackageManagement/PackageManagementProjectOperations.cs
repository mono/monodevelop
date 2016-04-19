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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Configuration;
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
			List<INuGetPackageAction> actions = null;

			Runtime.RunInMainThread (() => {
				var repositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
				var repository = repositoryProvider.CreateRepository (new PackageSource (packageSourceUrl));
				var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
				var dotNetProject = new DotNetProjectProxy ((DotNetProject)project);
				var context = new NuGetProjectContext ();

				actions = packages.Select (packageReference => {
					var action = new InstallNuGetPackageAction (
						repository,
						solutionManager,
						dotNetProject,
						context);
					action.PackageId = packageReference.Id;
					action.Version = new NuGetVersion (packageReference.Version);
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
			return Runtime.RunInMainThread (() => {
				string url = RegisteredPackageSources.DefaultPackageSourceUrl;
				var repository = registeredPackageRepositories.CreateRepository (new NuGet.PackageSource (url));
				IPackageManagementProject packageManagementProject = solution.GetProject (repository, new DotNetProjectProxy ((DotNetProject)project));

				var packages = packageManagementProject
					.GetPackageReferences ()
					.Select (packageReference => new PackageManagementPackageReference (packageReference.Id, packageReference.Version.ToString ()))
					.ToList ();

				packages.AddRange (GetMissingPackagesBeingInstalled (packages, (DotNetProject)project));
				return packages;
			}).Result;
		}

		IEnumerable<PackageManagementPackageReference> GetMissingPackagesBeingInstalled (
			IEnumerable<PackageManagementPackageReference> existingPackages,
			DotNetProject project)
		{
			return GetPackagesBeingInstalled (project)
				.Where (package => !existingPackages.Any (existingPackage => existingPackage.Id == package.Id));
		}

		static IEnumerable<PackageManagementPackageReference> GetPackagesBeingInstalled (DotNetProject project)
		{
			return PackageManagementServices.BackgroundPackageActionRunner.PendingInstallActionsForProject (project)
				.Select (installAction => new PackageManagementPackageReference (
					installAction.GetPackageId (), 
					installAction.GetPackageVersion ().ToString ()));
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

