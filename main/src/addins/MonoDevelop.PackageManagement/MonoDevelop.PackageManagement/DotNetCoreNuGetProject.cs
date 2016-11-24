//
// DotNetCoreNuGetProject.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	class DotNetCoreNuGetProject : NuGetProject, INuGetIntegratedProject, IHasDotNetProject
	{
		DotNetProject project;
		bool modified;

		public DotNetCoreNuGetProject (
			DotNetProject project,
			IEnumerable<string> targetFrameworks)
		{
			this.project = project;

			var targetFramework = NuGetFramework.UnsupportedFramework;

			if (targetFrameworks.Count () == 1) {
				targetFramework = NuGetFramework.Parse (targetFrameworks.First ());
			}

			InternalMetadata.Add (NuGetProjectMetadataKeys.TargetFramework, targetFramework);
			InternalMetadata.Add (NuGetProjectMetadataKeys.Name, project.Name);
			InternalMetadata.Add (NuGetProjectMetadataKeys.FullPath, project.BaseDirectory);
		}

		internal DotNetProject DotNetProject {
			get { return project; }
		}

		public static NuGetProject Create (DotNetProject project)
		{
			var targetFrameworks = project.GetDotNetCoreTargetFrameworks ();
			if (targetFrameworks.Any ())
				return new DotNetCoreNuGetProject (project, targetFrameworks);

			return null;
		}

		public virtual Task SaveProject ()
		{
			return project.SaveAsync (new ProgressMonitor ());
		}

		public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync (CancellationToken token)
		{
			return Task.FromResult (GetPackageReferences ());
		}

		IEnumerable<PackageReference> GetPackageReferences ()
		{
			return project.Items.OfType<ProjectPackageReference> ()
				.Select (projectItem => projectItem.CreatePackageReference ())
				.ToList ();
		}

		public override async Task<bool> InstallPackageAsync (
			PackageIdentity packageIdentity,
			DownloadResourceResult downloadResourceResult,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			bool added = await Runtime.RunInMainThread (() => {
				return AddPackageReference (packageIdentity, nuGetProjectContext);
			});

			if (added) {
				await SaveProject ();
				modified = true;
			}

			return added;
		}

		bool AddPackageReference (PackageIdentity packageIdentity, INuGetProjectContext context)
		{
			ProjectPackageReference packageReference = project.GetPackageReference (packageIdentity);
			if (packageReference != null) {
				context.Log (MessageLevel.Warning, GettextCatalog.GetString ("Package '{0}' already exists in project '{1}'", packageIdentity, project.Name));
				return false;
			}

			packageReference = ProjectPackageReference.Create (packageIdentity);
			project.Items.Add (packageReference);

			return true;
		}

		public override async Task<bool> UninstallPackageAsync (
			PackageIdentity packageIdentity,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			bool removed = await Runtime.RunInMainThread (() => {
				return RemovePackageReference (packageIdentity, nuGetProjectContext);
			});

			if (removed) {
				await SaveProject ();
				modified = true;
			}

			return removed;
		}

		bool RemovePackageReference (PackageIdentity packageIdentity, INuGetProjectContext context)
		{
			ProjectPackageReference packageReference = project.GetPackageReference (packageIdentity);

			if (packageReference == null) {
				context.Log (MessageLevel.Warning, GettextCatalog.GetString ("Package '{0}' does not exist in project '{1}'", packageIdentity, project.Name));
				return false;
			}

			project.Items.Remove (packageReference);

			return true;
		}

		public async Task<IEnumerable<NuGetProjectAction>> PreviewInstallPackageAsync (PackageIdentity packageIdentity, IEnumerable<NuGetProjectAction> actions)
		{
			await CheckPackageNotAlreadyInstalled (packageIdentity);

			return await AddUninstallActionsForExistingPackages (actions);
		}

		public Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackageAsync (IEnumerable<NuGetProjectAction> actions)
		{
			return AddUninstallActionsForExistingPackages (actions);
		}

		async Task CheckPackageNotAlreadyInstalled (PackageIdentity packageIdentity)
		{
			var installedPackages = await GetInstalledPackagesAsync (CancellationToken.None);
			if (installedPackages.Select (package => package.PackageIdentity).Contains (packageIdentity)) {
				string alreadyInstalledMessage = GettextCatalog.GetString ("Package '{0}' already exists in project '{1}'", packageIdentity, project.Name);
				throw new InvalidOperationException (
					alreadyInstalledMessage,
					new PackageAlreadyInstalledException (alreadyInstalledMessage));
			}
		}

		// Need to add uninstall actions for existing NuGet packages installed in the project
		// here otherwise the rollback will not add back the originally installed NuGet packages.
		async Task<IEnumerable<NuGetProjectAction>> AddUninstallActionsForExistingPackages (IEnumerable<NuGetProjectAction> actions)
		{
			var packagesBeingInstalled = actions
				.Where (action => action.NuGetProjectActionType == NuGetProjectActionType.Install)
				.Select (action => action.PackageIdentity)
				.ToList ();

			if (!packagesBeingInstalled.Any ())
				return actions;

			var packagesBeingUninstalled = actions
				.Where (action => action.NuGetProjectActionType == NuGetProjectActionType.Uninstall)
				.Select (action => action.PackageIdentity)
				.ToList ();

			var packageReferences = await GetInstalledPackagesAsync (CancellationToken.None);
			var packagesToUninstall = packageReferences
				.Select (packageReference => packageReference.PackageIdentity)
				.Where (package => IsDifferentVersionBeingInstalled (packagesBeingInstalled, package))
				.ToList ();

			packagesToUninstall = packagesToUninstall
				.Where (package => !packagesBeingUninstalled.Contains (package))
				.ToList ();

			if (!packagesToUninstall.Any ())
				return actions;

			var modifiedActions = actions.ToList ();
			modifiedActions.AddRange (packagesToUninstall.Select (package => NuGetProjectAction.CreateUninstallProjectAction (package, this)));

			return modifiedActions;
		}

		static bool IsDifferentVersionBeingInstalled (IEnumerable<PackageIdentity> packages, PackageIdentity otherPackage)
		{
			return packages.Any (package => {
				return StringComparer.OrdinalIgnoreCase.Equals (package.Id, otherPackage.Id) &&
					!package.Equals (otherPackage);
			});
		}

		public async Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackagesAsync (
			INuGetPackageManager packageManager,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			var installPackages = await GetInstalledPackagesAsync (token);

			var log = new LoggerAdapter (nuGetProjectContext);
			var actions = new List<NuGetProjectAction>();

			foreach (PackageReference installedPackage in installPackages) {
				NuGetVersion latestVersion = await packageManager.GetLatestVersionAsync(
					installedPackage.PackageIdentity.Id,
					this,
					resolutionContext,
					primarySources,
					log,
					token);

				if (latestVersion != null && latestVersion > installedPackage.PackageIdentity.Version) {
					actions.Add(NuGetProjectAction.CreateUninstallProjectAction (
						installedPackage.PackageIdentity,
						this));

					actions.Add (NuGetProjectAction.CreateInstallProjectAction (
						new PackageIdentity (installedPackage.PackageIdentity.Id, latestVersion),
						primarySources.FirstOrDefault (),
						this));
				}
			}

			return actions;
		}

		public async Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackagesAsync (
			string packageId,
			INuGetPackageManager packageManager,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			var log = new LoggerAdapter (nuGetProjectContext);

			NuGetVersion latestVersion = await packageManager.GetLatestVersionAsync (
				packageId,
				this,
				resolutionContext,
				primarySources,
				log,
				token);

			if (latestVersion == null) {
				throw new InvalidOperationException (GettextCatalog.GetString ("Unknown package '{0}'", packageId));
			}

			var installPackages = await GetInstalledPackagesAsync (token);
			var packageIdentity = new PackageIdentity (packageId, latestVersion);

			if (!IsDifferentVersionBeingInstalled (installPackages.Select (p => p.PackageIdentity), packageIdentity))
				return new NuGetProjectAction[0];

			SourceRepository sourceRepository = primarySources.First ();

			var action = NuGetProjectAction.CreateInstallProjectAction (packageIdentity, sourceRepository, this);
			return new [] { action };
		}

		public override Task PreProcessAsync (INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			modified = false;
			return base.PreProcessAsync (nuGetProjectContext, token);
		}

		/// <summary>
		/// Restore after executing the project actions to ensure the NuGet packages are
		/// supported by the project.
		/// </summary>
		public override async Task PostProcessAsync (INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			if (!modified)
				return;;

			var packageRestorer = new MonoDevelopDotNetCorePackageRestorer (project);
			await packageRestorer.RestorePackages (token);

			PackageManagementServices.PackageManagementEvents.OnPackagesRestored ();
		}
	}
}
