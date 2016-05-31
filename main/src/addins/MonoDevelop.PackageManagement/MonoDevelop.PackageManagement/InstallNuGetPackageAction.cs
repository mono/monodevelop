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
		NuGetPackageManager packageManager;
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
		{
			this.primarySources = primarySources.ToList ();
			this.secondarySources = secondarySources?.ToList ();
			this.dotNetProject = dotNetProject;
			this.context = projectContext;

			project = solutionManager.GetNuGetProject (dotNetProject);

			LicensesMustBeAccepted = true;
			PreserveLocalCopyReferences = true;

			var restartManager = new DeleteOnRestartManager ();

			packageManager = new NuGetPackageManager (
				solutionManager.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
				solutionManager,
				restartManager
			);
		}

		public string PackageId { get; set; }
		public NuGetVersion Version { get; set; }
		public bool IncludePrerelease { get; set; }
		public bool LicensesMustBeAccepted { get; set; }
		public bool PreserveLocalCopyReferences { get; set; }

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			using (var monitor = new NuGetPackageEventsMonitor (dotNetProject)) {
				ExecuteAsync (cancellationToken).Wait ();
			}
		}

		async Task ExecuteAsync (CancellationToken cancellationToken)
		{
			if (Version == null) {
				Version = await GetLatestPackageVersion (PackageId, cancellationToken);
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

			NuGetPackageManager.SetDirectInstall (identity, context);

			using (IDisposable fileMonitor = CreateFileMonitor ()) {
				using (IDisposable referenceMaintainer = CreateLocalCopyReferenceMaintainer ()) {
					await packageManager.ExecuteNuGetProjectActionsAsync (
						project,
						actions,
						context,
						cancellationToken);
				}
			}

			NuGetPackageManager.ClearDirectInstall (context);

			project.OnAfterExecuteActions (actions);

			await project.RunPostProcessAsync (context, cancellationToken);
		}

		Task<NuGetVersion> GetLatestPackageVersion (string packageId, CancellationToken cancellationToken)
		{
			return NuGetPackageManager.GetLatestVersionAsync (
				packageId,
				project,
				CreateResolutionContext (),
				primarySources,
				new ProjectContextLogger (context),
				cancellationToken);
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		ResolutionContext CreateResolutionContext ()
		{
			return new ResolutionContext (
				DependencyBehavior.Lowest,
				IncludePrerelease,
				true,
				VersionConstraints.None
			);
		}

		public bool IsForProject (DotNetProject project)
		{
			return dotNetProject.DotNetProject == project;
		}

		Task CheckLicenses (CancellationToken cancellationToken)
		{
			return NuGetPackageLicenseAuditor.AcceptLicenses (primarySources, actions, packageManager, cancellationToken);
		}

		IDisposable CreateLocalCopyReferenceMaintainer ()
		{
			if (PreserveLocalCopyReferences) {
				return new LocalCopyReferenceMaintainer (PackageManagementServices.PackageManagementEvents);
			}

			return new NullDisposable ();
		}

		public IEnumerable<NuGetProjectAction> GetNuGetProjectActions ()
		{
			return actions;
		}

		IDisposable CreateFileMonitor ()
		{
			return new PreventPackagesConfigFileBeingRemovedOnUpdateMonitor (
				PackageManagementServices.PackageManagementEvents,
				new FileRemover ());
		}
	}
}

