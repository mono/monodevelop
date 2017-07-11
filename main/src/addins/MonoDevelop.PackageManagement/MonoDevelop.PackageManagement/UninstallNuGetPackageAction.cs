//
// UninstallNuGetPackageAction.cs
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
using NuGet.PackageManagement;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class UninstallNuGetPackageAction : INuGetPackageAction, INuGetProjectActionsProvider
	{
		INuGetPackageManager packageManager;
		IDotNetProject dotNetProject;
		NuGetProject project;
		INuGetProjectContext context;
		IPackageManagementEvents packageManagementEvents;
		IEnumerable<NuGetProjectAction> actions;

		public UninstallNuGetPackageAction (
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

		public UninstallNuGetPackageAction (
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

			IsErrorWhenPackageNotInstalled = true;

			project = solutionManager.GetNuGetProject (dotNetProject);
		}

		public string PackageId { get; set; }
		public bool ForceRemove { get; set; }
		public bool RemoveDependencies { get; set; }
		public bool IsErrorWhenPackageNotInstalled { get; set; }

		public PackageActionType ActionType {
			get { return PackageActionType.Uninstall; }
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
			if (!IsErrorWhenPackageNotInstalled) {
				bool installed = await IsPackageInstalled (cancellationToken);
				if (!installed) {
					ReportPackageNotInstalled ();
					return;
				}
			}

			actions = await packageManager.PreviewUninstallPackageAsync (
				project,
				PackageId,
				CreateUninstallationContext (),
				context,
				cancellationToken);

			project.OnBeforeUninstall (actions);

			await packageManager.ExecuteNuGetProjectActionsAsync (
				project,
				actions,
				context,
				cancellationToken);

			project.OnAfterExecuteActions (actions);

			await project.RunPostProcessAsync (context, cancellationToken);
		}

		async Task<bool> IsPackageInstalled (CancellationToken cancellationToken)
		{
			var installedPackages = await project.GetInstalledPackagesAsync (cancellationToken);
			var packageReference = installedPackages.FirstOrDefault (package => package.PackageIdentity.Id.Equals (PackageId, StringComparison.InvariantCultureIgnoreCase));

			return packageReference?.PackageIdentity != null;
		}

		void ReportPackageNotInstalled ()
		{
			string message = GettextCatalog.GetString (
				"Package '{0}' has already been uninstalled from project '{1}'",
				PackageId,
				dotNetProject.Name);

			context.Log (MessageLevel.Info, message);
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		UninstallationContext CreateUninstallationContext ()
		{
			return new UninstallationContext (RemoveDependencies, ForceRemove);
		}

		public IEnumerable<NuGetProjectAction> GetNuGetProjectActions ()
		{
			return actions ?? Enumerable.Empty<NuGetProjectAction> ();
		}
	}
}

