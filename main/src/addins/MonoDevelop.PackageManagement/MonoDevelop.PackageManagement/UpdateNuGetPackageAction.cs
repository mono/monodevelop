//
// UpdateNuGetPackageAction.cs
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
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

namespace MonoDevelop.PackageManagement
{
	internal class UpdateNuGetPackageAction : INuGetPackageAction, INuGetProjectActionsProvider
	{
		INuGetPackageManager packageManager;
		IDotNetProject dotNetProject;
		NuGetProject project;
		List<SourceRepository> primarySources;
		ISourceRepositoryProvider sourceRepositoryProvider;
		IEnumerable<NuGetProjectAction> actions;
		IPackageManagementEvents packageManagementEvents;
		INuGetProjectContext context;

		public UpdateNuGetPackageAction (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject)
			: this (
				solutionManager,
				dotNetProject,
				new NuGetProjectContext (),
				new MonoDevelopNuGetPackageManager (solutionManager),
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public UpdateNuGetPackageAction (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			INuGetProjectContext projectContext,
			INuGetPackageManager packageManager,
			IPackageManagementEvents packageManagementEvents)
		{
			this.dotNetProject = dotNetProject;
			this.context = projectContext;
			this.packageManager = packageManager;
			this.packageManagementEvents = packageManagementEvents;

			project = solutionManager.GetNuGetProject (dotNetProject);

			sourceRepositoryProvider = solutionManager.CreateSourceRepositoryProvider ();
			primarySources = sourceRepositoryProvider.GetRepositories ().ToList ();
		}

		public string PackageId { get; set; }
		public bool IncludePrerelease { get; set; }

		public PackageActionType ActionType {
			get { return PackageActionType.Install; }
		}

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			using (var monitor = new NuGetPackageEventsMonitor (dotNetProject, packageManagementEvents)) {
				ExecuteAsync (cancellationToken).Wait ();
			}
		}

		async Task ExecuteAsync (CancellationToken cancellationToken)
		{
			actions = await packageManager.PreviewUpdatePackagesAsync (
				PackageId,
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

			SetDirectInstall ();

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

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		ResolutionContext CreateResolutionContext ()
		{
			return new ResolutionContext (
				DependencyBehavior.Lowest,
				IncludePrerelease,
				false,
				VersionConstraints.None
			);
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

		void SetDirectInstall ()
		{
			var matchedAction = actions.FirstOrDefault (IsInstallActionForPackageBeingUpdated);
			if (matchedAction != null) {
				packageManager.SetDirectInstall (matchedAction.PackageIdentity, context);
			}
		}

		bool IsInstallActionForPackageBeingUpdated (NuGetProjectAction action)
		{
			return action.PackageIdentity.Id == PackageId &&
				action.NuGetProjectActionType == NuGetProjectActionType.Install;
		}

		protected virtual ILicenseAcceptanceService GetLicenseAcceptanceService ()
		{
			return new LicenseAcceptanceService ();
		}

		LocalCopyReferenceMaintainer CreateLocalCopyReferenceMaintainer ()
		{
			return new LocalCopyReferenceMaintainer (packageManagementEvents);
		}

		public IEnumerable<NuGetProjectAction> GetNuGetProjectActions ()
		{
			return actions;
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

