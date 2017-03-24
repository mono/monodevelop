//
// RestoreNuGetPackagesInDotNetCoreProject.cs
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

using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.ProjectManagement.Projects;

namespace MonoDevelop.PackageManagement
{
	class RestoreNuGetPackagesInDotNetCoreProject : IPackageAction
	{
		IPackageManagementEvents packageManagementEvents;
		MonoDevelopBuildIntegratedRestorer packageRestorer;
		BuildIntegratedNuGetProject nugetProject;
		DotNetProject project;

		public RestoreNuGetPackagesInDotNetCoreProject (DotNetProject project)
		{
			this.project = project;
			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			nugetProject = solutionManager.GetNuGetProject (new DotNetProjectProxy (project)) as BuildIntegratedNuGetProject;

			packageRestorer = new MonoDevelopBuildIntegratedRestorer (solutionManager);
		}

		public bool ReloadProject { get; set; }

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			Task task = ExecuteAsync (cancellationToken);
			using (var restoreTask = new PackageRestoreTask (task)) {
				task.Wait ();
			}
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		async Task ExecuteAsync (CancellationToken cancellationToken)
		{
			await packageRestorer.RestorePackages (nugetProject, cancellationToken);

			packageManagementEvents.OnPackagesRestored ();
		}
	}
}
