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
	internal class InstallNuGetPackageAction : INuGetPackageAction, IInstallNuGetPackageAction
	{
		List<SourceRepository> primarySources;
		List<SourceRepository> secondarySources = new List<SourceRepository> ();
		NuGetPackageManager packageManager;
		NuGetProject project;
		NuGetProjectContext context;
		IDotNetProject dotNetProject;
		CancellationToken cancellationToken;

		public InstallNuGetPackageAction (
			SourceRepository sourceRepository,
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			NuGetProjectContext projectContext,
			CancellationToken cancellationToken = default(CancellationToken))
			: this (
				new [] { sourceRepository },
				solutionManager,
				dotNetProject,
				projectContext,
				cancellationToken)
		{
		}

		public InstallNuGetPackageAction (
			IEnumerable<SourceRepository> primarySources,
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			NuGetProjectContext projectContext,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			this.primarySources = primarySources.ToList ();
			this.cancellationToken = cancellationToken;
			this.dotNetProject = dotNetProject;
			this.context = projectContext;

			project = solutionManager.GetNuGetProject (dotNetProject);

			LicensesMustBeAccepted = true;

			var restartManager = new DeleteOnRestartManager ();

			packageManager = new NuGetPackageManager (
				SourceRepositoryProviderFactory.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
				solutionManager,
				restartManager
			);
		}

		public string PackageId { get; set; }
		public NuGetVersion Version { get; set; }
		public bool IncludePrerelease { get; set; }
		public bool LicensesMustBeAccepted { get; set; }

		public void Execute ()
		{
			ExecuteAsync ().Wait ();
		}

		async Task ExecuteAsync ()
		{
			var identity = new PackageIdentity (PackageId, Version);

			var actions = await packageManager.PreviewInstallPackageAsync (
				project,
				identity,
				CreateResolutionContext (),
				context,
				primarySources,
				secondarySources,
				cancellationToken);

			if (LicensesMustBeAccepted) {
				await CheckLicenses (actions);
			}

			NuGetPackageManager.SetDirectInstall (identity, context);

			using (IDisposable referenceMaintainer = CreateLocalCopyReferenceMaintainer ()) {
				await packageManager.ExecuteNuGetProjectActionsAsync (
					project,
					actions,
					context,
					cancellationToken);
			}

			NuGetPackageManager.ClearDirectInstall (context);
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

		Task CheckLicenses (IEnumerable<NuGetProjectAction> actions)
		{
			return NuGetPackageLicenseAuditor.AcceptLicenses (primarySources, actions, cancellationToken);
		}

		LocalCopyReferenceMaintainer CreateLocalCopyReferenceMaintainer ()
		{
			return new LocalCopyReferenceMaintainer (PackageManagementServices.PackageManagementEvents);
		}
	}
}

