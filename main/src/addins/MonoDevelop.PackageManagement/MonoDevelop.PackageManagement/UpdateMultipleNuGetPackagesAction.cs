//
// UpdateMultipleNuGetPackagesAction.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

namespace MonoDevelop.PackageManagement
{
	class UpdateMultipleNuGetPackagesAction : IPackageAction, INuGetProjectActionsProvider
	{
		INuGetProjectContext context;
		IMonoDevelopSolutionManager solutionManager;
		INuGetPackageManager packageManager;
		IPackageRestoreManager restoreManager;
		IEnumerable<NuGetProjectAction> actions;
		List<IDotNetProject> dotNetProjects = new List<IDotNetProject> ();
		List<NuGetProject> projects = new List<NuGetProject> ();
		List<PackageIdentity> packagesToUpdate = new List<PackageIdentity> ();
		List<SourceRepository> primarySources;
		List<SourceRepository> secondarySources;
		IPackageManagementEvents packageManagementEvents;

		public UpdateMultipleNuGetPackagesAction (
			IEnumerable<SourceRepository> primarySources,
			IMonoDevelopSolutionManager solutionManager,
			INuGetProjectContext context)
			: this (
				primarySources,
				solutionManager,
				context,
				new MonoDevelopNuGetPackageManager (solutionManager),
				new MonoDevelopPackageRestoreManager (solutionManager),
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public UpdateMultipleNuGetPackagesAction (
			IEnumerable<SourceRepository> primarySources,
			IMonoDevelopSolutionManager solutionManager,
			INuGetProjectContext context,
			INuGetPackageManager packageManager,
			IPackageRestoreManager restoreManager,
			IPackageManagementEvents packageManagementEvents)
		{
			this.solutionManager = solutionManager;
			this.context = context;
			this.packageManager = packageManager;
			this.restoreManager = restoreManager;
			this.packageManagementEvents = packageManagementEvents;

			this.primarySources = primarySources.ToList ();
			secondarySources = solutionManager.CreateSourceRepositoryProvider ().GetRepositories ().ToList ();
		}

		public PackageActionType ActionType => PackageActionType.Install;

		/// <summary>
		/// Used for testing to disable the license service check.
		/// </summary>
		internal bool LicensesMustBeAccepted { get; set; } = true;

		public void AddProject (IDotNetProject project)
		{
			dotNetProjects.Add (project);
			projects.Add (solutionManager.GetNuGetProject (project));
		}

		public void AddPackageToUpdate (PackageIdentity package)
		{
			packagesToUpdate.Add (package);
		}

		internal IList<IDotNetProject> DotNetProjects => dotNetProjects;
		internal IList<PackageIdentity> PackagesToUpdate => packagesToUpdate;

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			ExecuteAsync (cancellationToken).Wait ();
		}

		public IEnumerable<NuGetProjectAction> GetNuGetProjectActions ()
		{
			return actions;
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		async Task ExecuteAsync (CancellationToken cancellationToken)
		{
			using (var sourceCacheContext = new SourceCacheContext ()) {
				await RestoreAnyMissingPackagesAsync (sourceCacheContext, cancellationToken);

				var resolutionContext = CreateResolutionContext (sourceCacheContext);

				actions = await packageManager.PreviewUpdatePackagesAsync (
					packagesToUpdate,
					projects,
					resolutionContext,
					context,
					primarySources,
					secondarySources,
					cancellationToken);

				if (!actions.Any ()) {
					foreach (IDotNetProject project in dotNetProjects) {
						packageManagementEvents.OnNoUpdateFound (project);
					}
					return;
				}

				if (LicensesMustBeAccepted) {
					await CheckLicensesAsync (cancellationToken);
				}

				using (IDisposable fileMonitor = CreateFileMonitor ()) {
					using (var referenceMaintainer = new ProjectReferenceMaintainerCollection (projects)) {
						await packageManager.ExecuteNuGetProjectActionsAsync (
							projects,
							actions,
							context,
							sourceCacheContext,
							cancellationToken);

						await referenceMaintainer.ApplyChangesAsync ();
					}
				}

				OnAfterExecutionActions ();

				await RunPostProcessAsync (cancellationToken);

				await OpenReadmeFilesAsync (cancellationToken);
			}
		}

		async Task RestoreAnyMissingPackagesAsync (SourceCacheContext sourceCacheContext, CancellationToken cancellationToken)
		{
			var packages = await restoreManager.GetPackagesInSolutionAsync (
				solutionManager.SolutionDirectory,
				cancellationToken);

			if (!packages.Any (package => IsMissingForProject (package)))
				return;

			using (var monitor = new PackageRestoreMonitor (restoreManager, packageManagementEvents)) {
				var downloadContext = new PackageDownloadContext (sourceCacheContext);
				await restoreManager.RestoreMissingPackagesAsync (
					solutionManager.SolutionDirectory,
					packages,
					context,
					downloadContext,
					cancellationToken);
			}

			await Runtime.RunInMainThread (() => {
				foreach (IDotNetProject dotNetProject in dotNetProjects) {
					dotNetProject.RefreshReferenceStatus ();
				}
			});

			packageManagementEvents.OnPackagesRestored ();
		}

		bool IsMissingForProject (PackageRestoreData package)
		{
			if (!package.IsMissing)
				return false;

			foreach (string projectName in package.ProjectNames) {
				foreach (IDotNetProject dotNetProject in dotNetProjects) {
					if (dotNetProject.Name == projectName) {
						return true;
					}
				}
			}
			return false;
		}

		ResolutionContext CreateResolutionContext (SourceCacheContext sourceCacheContext)
		{
			bool includePrerelease = packagesToUpdate
				.Where (package => package.Version.IsPrerelease)
				.Any ();

			return new ResolutionContext (
				DependencyBehavior.Lowest,
				includePrerelease,
				true, // includeUnlisted. Visual Studio on Windows sets this to true.
				VersionConstraints.None,
				new GatherCache (),
				sourceCacheContext);
		}

		IDisposable CreateFileMonitor ()
		{
			return new PreventPackagesConfigFileBeingRemovedOnUpdateMonitor (
				packageManagementEvents,
				GetFileRemover ());
		}

		protected virtual IFileRemover GetFileRemover ()
		{
			return new FileRemover ();
		}

		Task CheckLicensesAsync (CancellationToken cancellationToken)
		{
			return NuGetPackageLicenseAuditor.AcceptLicenses (
				primarySources,
				actions,
				packageManager,
				GetLicenseAcceptanceService (),
				cancellationToken);
		}

		protected virtual ILicenseAcceptanceService GetLicenseAcceptanceService ()
		{
			return new LicenseAcceptanceService ();
		}

		void OnAfterExecutionActions ()
		{
			if (projects.Count == 1) {
				projects [0].OnAfterExecuteActions (actions);
				return;
			}

			foreach (NuGetProject project in projects) {
				var projectActions = actions
					.Where (action => action.Project == project)
					.ToArray ();
				if (projectActions.Any ()) {
					project.OnAfterExecuteActions (projectActions);
				}
			}
		}

		Task RunPostProcessAsync (CancellationToken cancellationToken)
		{
			return packageManager.RunPostProcessAsync (projects, context, cancellationToken);
		}

		Task OpenReadmeFilesAsync (CancellationToken cancellationToken)
		{
			var packages = GetPackagesUpdated ();
			return packageManager.OpenReadmeFiles (projects, packages, context, cancellationToken);
		}

		IEnumerable<PackageIdentity> GetPackagesUpdated ()
		{
			return actions
				.Where (action => action.NuGetProjectActionType == NuGetProjectActionType.Install)
				.Select (action => action.PackageIdentity)
				.Distinct ();
		}
	}
}
