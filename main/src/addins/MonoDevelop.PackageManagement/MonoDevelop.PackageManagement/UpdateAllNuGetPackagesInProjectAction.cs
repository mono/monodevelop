//
// UpdateAllNuGetPackagesInProjectAction.cs
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
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

namespace MonoDevelop.PackageManagement
{
	internal class UpdateAllNuGetPackagesInProjectAction : IPackageAction, INuGetProjectActionsProvider
	{
		INuGetPackageManager packageManager;
		IPackageRestoreManager restoreManager;
		IMonoDevelopSolutionManager solutionManager;
		INuGetProjectContext context;
		IPackageManagementEvents packageManagementEvents;
		IDotNetProject dotNetProject;
		NuGetProject project;
		IEnumerable<NuGetProjectAction> actions;
		List<SourceRepository> primarySources;
		IEnumerable<PackageReference> packageReferences;
		string projectName;

		public UpdateAllNuGetPackagesInProjectAction (
			IMonoDevelopSolutionManager solutionManager,
			DotNetProject dotNetProject)
			: this (
				solutionManager,
				new DotNetProjectProxy (dotNetProject),
				new NuGetProjectContext (),
				new MonoDevelopNuGetPackageManager (solutionManager),
				new MonoDevelopPackageRestoreManager (solutionManager),
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public UpdateAllNuGetPackagesInProjectAction (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			INuGetProjectContext projectContext,
			INuGetPackageManager packageManager,
			IPackageRestoreManager restoreManager,
			IPackageManagementEvents packageManagementEvents)
		{
			this.solutionManager = solutionManager;
			this.dotNetProject = dotNetProject;
			this.context = projectContext;
			this.packageManager = packageManager;
			this.restoreManager = restoreManager;
			this.packageManagementEvents = packageManagementEvents;

			primarySources = solutionManager.CreateSourceRepositoryProvider ().GetRepositories ().ToList ();

			project = solutionManager.GetNuGetProject (dotNetProject);

			projectName = dotNetProject.Name;
		}

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			ExecuteAsync (cancellationToken).Wait ();
		}

		async Task ExecuteAsync (CancellationToken cancellationToken)
		{
			await RestoreAnyMissingPackages (cancellationToken);

			actions = await packageManager.PreviewUpdatePackagesAsync (
				project,
				CreateResolutionContext (),
				context,
				primarySources,
				new SourceRepository[0],
				cancellationToken);

			if (!actions.Any ()) {
				packageManagementEvents.OnNoUpdateFound (dotNetProject);
				return;
			}

			await CheckLicenses (cancellationToken);

			using (IDisposable fileMonitor = CreateFileMonitor ()) {
				using (IDisposable referenceMaintainer = CreateLocalCopyReferenceMaintainer ()) {
					await packageManager.ExecuteNuGetProjectActionsAsync (
						project,
						actions,
						context,
						cancellationToken);
				}
			}

			project.OnAfterExecuteActions (actions);

			await project.RunPostProcessAsync (context, cancellationToken);

			await OpenReadmeFiles (cancellationToken);
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		/// <summary>
		/// With NuGet v3 the IncludePrerelease flag does not need to be set to true on the
		/// resolution context in order to update pre-release NuGet packages. NuGet v3 will
		/// update pre-release NuGet packages to the latest pre-release version or latest
		/// stable version if that is a higher version. The IncludePrerelease flag is only
		/// required to allow a stable version to be updated to a pre-release version which
		/// is not what we want to do when updating all NuGet packages in a project.
		/// </summary>
		ResolutionContext CreateResolutionContext ()
		{
			return new ResolutionContext (
				DependencyBehavior.Lowest,
				false,
				false,
				VersionConstraints.None
			);
		}

		async Task RestoreAnyMissingPackages (CancellationToken cancellationToken)
		{
			var packages = await restoreManager.GetPackagesInSolutionAsync (
				solutionManager.SolutionDirectory,
				cancellationToken);

			var missingPackages = packages.Select (IsMissingForCurrentProject).ToList ();
			if (missingPackages.Any ()) {
				using (var monitor = new PackageRestoreMonitor (restoreManager, packageManagementEvents)) {
					await restoreManager.RestoreMissingPackagesAsync (
						solutionManager.SolutionDirectory,
						project,
						context,
						cancellationToken);
				}

				await RunInMainThread (() => dotNetProject.RefreshReferenceStatus ());

				packageManagementEvents.OnPackagesRestored ();
			}
		}

		bool IsMissingForCurrentProject (PackageRestoreData package)
		{
			return package.IsMissing && package.ProjectNames.Any (name => name == projectName);
		}

		protected virtual Task RunInMainThread (Action action)
		{
			return Runtime.RunInMainThread (action);
		}

		Task CheckLicenses (CancellationToken cancellationToken)
		{
			return NuGetPackageLicenseAuditor.AcceptLicenses (
				primarySources,
				actions,
				packageManager,
				GetLicenseAcceptanceService (),
				cancellationToken);
		}

		public IEnumerable<NuGetProjectAction> GetNuGetProjectActions ()
		{
			return actions;
		}

		protected virtual ILicenseAcceptanceService GetLicenseAcceptanceService ()
		{
			return new LicenseAcceptanceService ();
		}

		LocalCopyReferenceMaintainer CreateLocalCopyReferenceMaintainer ()
		{
			return new LocalCopyReferenceMaintainer (packageManagementEvents);
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

		Task OpenReadmeFiles (CancellationToken cancellationToken)
		{
			var packages = GetPackagesUpdated ().ToList ();
			return packageManager.OpenReadmeFiles (project, packages, context, cancellationToken);
		}

		IEnumerable<PackageIdentity> GetPackagesUpdated ()
		{
			return actions
				.Where (action => action.NuGetProjectActionType == NuGetProjectActionType.Install)
				.Select (action => action.PackageIdentity);
		}
	}
}
