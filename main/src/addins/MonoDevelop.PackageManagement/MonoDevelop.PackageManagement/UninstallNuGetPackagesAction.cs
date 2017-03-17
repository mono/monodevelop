﻿//
// UninstallNuGetPackagesAction.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	class UninstallNuGetPackagesAction : IPackageAction, INuGetProjectActionsProvider
	{
		INuGetPackageManager packageManager;
		IDotNetProject dotNetProject;
		NuGetProject project;
		INuGetProjectContext context;
		IPackageManagementEvents packageManagementEvents;
		IEnumerable<NuGetProjectAction> actions;
		List<string> packageIds = new List<string> ();

		public UninstallNuGetPackagesAction (
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

		public UninstallNuGetPackagesAction (
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
		}

		public void AddPackageIds (IEnumerable<string> packageIds)
		{
			this.packageIds.AddRange (packageIds);
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
			var buildIntegratedProject = project as IBuildIntegratedNuGetProject;

			actions = CreateUninstallActions ();

			var buildAction = await packageManager.PreviewBuildIntegratedProjectActionsAsync (
				buildIntegratedProject,
				actions,
				context,
				cancellationToken);

			project.OnBeforeUninstall (actions);

			await packageManager.ExecuteNuGetProjectActionsAsync (
				project,
				new [] { buildAction },
				context,
				cancellationToken);

			project.OnAfterExecuteActions (actions);

			await project.RunPostProcessAsync (context, cancellationToken);
		}

		IEnumerable<NuGetProjectAction> CreateUninstallActions ()
		{
			return packageIds.Select (CreateUninstallProjectAction).ToArray ();
		}

		NuGetProjectAction CreateUninstallProjectAction (string packageId)
		{
			var package = new PackageIdentity (packageId, null);
			return NuGetProjectAction.CreateUninstallProjectAction (package, project);
		}

		public IEnumerable<NuGetProjectAction> GetNuGetProjectActions ()
		{
			return actions ?? Enumerable.Empty<NuGetProjectAction> ();
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}
	}
}
