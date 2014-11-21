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
using ICSharpCode.PackageManagement;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class PackageManagementProjectOperations : IPackageManagementProjectOperations
	{
		IPackageManagementSolution solution;
		IRegisteredPackageRepositories registeredPackageRepositories;
		BackgroundPackageActionRunner backgroundActionRunner;
		IPackageManagementEvents packageManagementEvents;

		public PackageManagementProjectOperations (
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredPackageRepositories,
			BackgroundPackageActionRunner backgroundActionRunner,
			IPackageManagementEvents packageManagementEvents)
		{
			this.solution = solution;
			this.registeredPackageRepositories = registeredPackageRepositories;
			this.backgroundActionRunner = backgroundActionRunner;
			this.packageManagementEvents = packageManagementEvents;

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
			List<IPackageAction> actions = null;

			DispatchService.GuiSyncDispatch (() => {
				IPackageRepository repository = CreatePackageRepository (packageSourceUrl);
				IPackageManagementProject packageManagementProject = solution.GetProject (repository, new DotNetProjectProxy ((DotNetProject)project));
				actions = packages.Select (packageReference => {
					InstallPackageAction action = packageManagementProject.CreateInstallPackageAction ();
					action.PackageId = packageReference.Id;
					action.PackageVersion = new SemanticVersion (packageReference.Version);
					return (IPackageAction)action;
				}).ToList ();
			});

			ProgressMonitorStatusMessage progressMessage = GetProgressMonitorStatusMessages (actions);
			backgroundActionRunner.RunAndWait (progressMessage, actions);
		}

		IPackageRepository CreatePackageRepository (string packageSourceUrl)
		{
			IPackageRepository repository = registeredPackageRepositories.CreateRepository (new PackageSource (packageSourceUrl));
			return new PriorityPackageRepository (MachineCache.Default, repository);
		}

		ProgressMonitorStatusMessage GetProgressMonitorStatusMessages (List<IPackageAction> packageActions)
		{
			if (packageActions.Count == 1) {
				string packageId = packageActions.OfType<ProcessPackageAction> ().First ().PackageId;
				return ProgressMonitorStatusMessageFactory.CreateInstallingSinglePackageMessage (packageId);
			}
			return ProgressMonitorStatusMessageFactory.CreateInstallingMultiplePackagesMessage (packageActions.Count);
		}

		public IEnumerable<PackageManagementPackageReference> GetInstalledPackages (Project project)
		{
			List<PackageManagementPackageReference> packageReferences = null;
		
			DispatchService.GuiSyncDispatch (() => {
				string url = RegisteredPackageSources.DefaultPackageSourceUrl;
				var repository = registeredPackageRepositories.CreateRepository (new PackageSource (url));
				IPackageManagementProject packageManagementProject = solution.GetProject (repository, new DotNetProjectProxy ((DotNetProject)project));
				packageReferences = packageManagementProject
					.GetPackageReferences ()
					.Select (packageReference => new PackageManagementPackageReference (packageReference.Id, packageReference.Version.ToString ()))
					.ToList ();
			});

			return packageReferences;
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

