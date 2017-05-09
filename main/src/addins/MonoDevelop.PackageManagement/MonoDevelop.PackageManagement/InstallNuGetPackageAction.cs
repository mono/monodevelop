//
// InstallNuGetPackageAction.cs
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
using MonoDevelop.Projects;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class InstallNuGetPackageAction : INuGetPackageAction, IInstallNuGetPackageAction, INuGetProjectActionsProvider
	{
		List<SourceRepository> primarySources;
		List<SourceRepository> secondarySources;
		INuGetPackageManager packageManager;
		IPackageManagementEvents packageManagementEvents;
		NuGetProject project;
		INuGetProjectContext context;
		IDotNetProject dotNetProject;
		IEnumerable<NuGetProjectAction> actions;

		public InstallNuGetPackageAction (
			IEnumerable<SourceRepository> primarySources,
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			INuGetProjectContext projectContext)
			: this (
				primarySources,
				null,
				solutionManager,
				dotNetProject,
				projectContext)
		{
		}

		public InstallNuGetPackageAction (
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			INuGetProjectContext projectContext)
			: this (
				primarySources,
				secondarySources,
				solutionManager,
				dotNetProject,
				projectContext,
				new MonoDevelopNuGetPackageManager (solutionManager),
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public InstallNuGetPackageAction (
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			INuGetProjectContext projectContext,
			INuGetPackageManager packageManager,
			IPackageManagementEvents packageManagementEvents)
		{
			this.primarySources = primarySources.ToList ();
			this.secondarySources = secondarySources?.ToList ();
			this.dotNetProject = dotNetProject;
			this.context = projectContext;
			this.packageManager = packageManager;
			this.packageManagementEvents = packageManagementEvents;

			project = solutionManager.GetNuGetProject (dotNetProject);

			LicensesMustBeAccepted = true;
			PreserveLocalCopyReferences = true;
			OpenReadmeFile = true;
		}

		public string PackageId { get; set; }
		public NuGetVersion Version { get; set; }
		public bool IncludePrerelease { get; set; }
		public bool LicensesMustBeAccepted { get; set; }
		public bool PreserveLocalCopyReferences { get; set; }
		public bool OpenReadmeFile { get; set; }

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			try {
				using (var monitor = new NuGetPackageEventsMonitor (dotNetProject, packageManagementEvents)) {
					ExecuteAsync (cancellationToken).Wait ();
				}
			} catch (AggregateException ex) {
				Exception baseException = ex.GetBaseException ();
				if (baseException.InnerException is PackageAlreadyInstalledException) {
					context.Log (MessageLevel.Info, baseException.Message);
				} else {
					throw;
				}
			}
		}

		async Task ExecuteAsync (CancellationToken cancellationToken)
		{
			if (Version == null) {
				ResolvedPackage resolvedPackage = await GetLatestPackageVersion (PackageId, cancellationToken);
				Version = resolvedPackage?.LatestVersion;
			}

			var identity = new PackageIdentity (PackageId, Version);

			actions = await packageManager.PreviewInstallPackageAsync (
				project,
				identity,
				CreateResolutionContext (),
				context,
				primarySources,
				secondarySources,
				cancellationToken);

			if (LicensesMustBeAccepted) {
				await CheckLicenses (cancellationToken);
			}

			if (ShouldOpenReadmeFile (identity)) {
				packageManager.SetDirectInstall (identity, context);
			}

			using (IDisposable fileMonitor = CreateFileMonitor ()) {
				using (IDisposable referenceMaintainer = CreateLocalCopyReferenceMaintainer ()) {
					await packageManager.ExecuteNuGetProjectActionsAsync (
						project,
						actions,
						context,
						cancellationToken);
				}
			}

			packageManager.ClearDirectInstall (context);

			project.OnAfterExecuteActions (actions);

			await project.RunPostProcessAsync (context, cancellationToken);
		}

		Task<ResolvedPackage> GetLatestPackageVersion (string packageId, CancellationToken cancellationToken)
		{
			return packageManager.GetLatestVersionAsync (
				packageId,
				project,
				CreateResolutionContext (includeUnlisted: false),
				primarySources,
				new ProjectContextLogger (context),
				cancellationToken);
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		ResolutionContext CreateResolutionContext (bool includeUnlisted = true)
		{
			return new ResolutionContext (
				DependencyBehavior.Lowest,
				IncludePrerelease || IsPrereleasePackageBeingInstalled (),
				includeUnlisted,
				VersionConstraints.None
			);
		}

		bool IsPrereleasePackageBeingInstalled ()
		{
			return Version?.IsPrerelease == true;
		}

		public bool IsForProject (DotNetProject project)
		{
			return dotNetProject.DotNetProject == project;
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

		protected virtual ILicenseAcceptanceService GetLicenseAcceptanceService ()
		{
			return new LicenseAcceptanceService ();
		}

		IDisposable CreateLocalCopyReferenceMaintainer ()
		{
			if (PreserveLocalCopyReferences) {
				return new LocalCopyReferenceMaintainer (packageManagementEvents);
			}

			return new NullDisposable ();
		}

		public IEnumerable<NuGetProjectAction> GetNuGetProjectActions ()
		{
			return actions ?? Enumerable.Empty<NuGetProjectAction> ();
		}

		bool ShouldOpenReadmeFile (PackageIdentity identity)
		{
			return OpenReadmeFile && !packageManager.PackageExistsInPackagesFolder (identity);
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
	}
}

