//
// RestoreAndUninstallNuGetPackageAction.cs
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class RestoreAndUninstallNuGetPackageAction : INuGetPackageAction, INuGetProjectActionsProvider
	{
		RestoreNuGetPackagesInProjectAction restoreAction;
		UninstallNuGetPackageAction uninstallAction;
		IDotNetProject dotNetProject;
		MSBuildNuGetProject nugetProject;
		PackagePathResolver packagePathResolver;
		CancellationToken cancellationToken;

		public RestoreAndUninstallNuGetPackageAction (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject dotNetProject,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			this.dotNetProject = dotNetProject;
			this.cancellationToken = cancellationToken;

			nugetProject = solutionManager.GetNuGetProject (dotNetProject) as MSBuildNuGetProject;
			packagePathResolver = new PackagePathResolver (nugetProject.GetPackagesFolderPath (solutionManager));

			restoreAction = new RestoreNuGetPackagesInProjectAction (
				dotNetProject.DotNetProject,
				nugetProject,
				solutionManager,
				cancellationToken);

			uninstallAction = new UninstallNuGetPackageAction (solutionManager, dotNetProject, cancellationToken);
		}

		public string PackageId { get; set; }
		public NuGetVersion Version { get; set; }

		public void Execute ()
		{
			ExecuteAsync ().Wait ();
		}

		async Task ExecuteAsync ()
		{
			try {
				restoreAction.Execute ();
			} catch (Exception ex) {
				bool result = await OnRestoreFailure ();
				if (!result) {
					throw ex;
				}
				return;
			}

			uninstallAction.PackageId = PackageId;
			uninstallAction.Execute ();
		}

		public IEnumerable<NuGetProjectAction> GetNuGetProjectActions ()
		{
			return uninstallAction.GetNuGetProjectActions () ?? new NuGetProjectAction[0];
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		async Task<bool> OnRestoreFailure ()
		{
			bool result = await Runtime.RunInMainThread (async () => {
				if (ShouldForceUninstall ()) {
					await ForceUninstall ();
					return true;
				}
				return false;
			});

			if (result) {
				return await nugetProject.PackagesConfigNuGetProject.UninstallPackageAsync (
					new PackageIdentity (PackageId, Version),
					new NuGetProjectContext (),
					cancellationToken
				);
			}
			return result;
		}

		bool ShouldForceUninstall ()
		{
			AlertButton result = MessageService.AskQuestion (
				GettextCatalog.GetString ("Unable to restore {0} package before removing. Do you want to force the NuGet package to be removed?", PackageId),
				GettextCatalog.GetString ("Forcing a NuGet package to be removed may break the build."),
				AlertButton.Yes,
				AlertButton.No);

			return result == AlertButton.Yes;
		}

		Task ForceUninstall ()
		{
			var uninstaller = new NuGetPackageUninstaller (dotNetProject, packagePathResolver);
			return uninstaller.ForceUninstall (PackageId, Version);
		}
	}
}

