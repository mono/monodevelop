//
// RestoreNuGetPackagesInProjectAction.cs
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
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class RestoreNuGetPackagesInProjectAction : IPackageAction
	{
		IMonoDevelopSolutionManager solutionManager;
		IPackageManagementEvents packageManagementEvents;
		PackageRestoreManager restoreManager;
		NuGetProject nugetProject;
		DotNetProject project;

		public RestoreNuGetPackagesInProjectAction (
			DotNetProject project,
			NuGetProject nugetProject,
			IMonoDevelopSolutionManager solutionManager)
		{
			this.project = project;
			this.nugetProject = nugetProject;
			this.solutionManager = solutionManager;

			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			restoreManager = new PackageRestoreManager (
				solutionManager.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
				solutionManager
			);
		}

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
			using (var monitor = new PackageRestoreMonitor (restoreManager)) {
				using (var cacheContext = new SourceCacheContext ()) {
					var downloadContext = new PackageDownloadContext (cacheContext);
					await restoreManager.RestoreMissingPackagesAsync (
						solutionManager.SolutionDirectory,
						nugetProject,
						new NuGetProjectContext (),
						downloadContext,
						cancellationToken);
				}
			}

			await Runtime.RunInMainThread (() => RefreshProjectReferences ());

			packageManagementEvents.OnPackagesRestored ();
		}

		void RefreshProjectReferences ()
		{
			foreach (DotNetProject dotNetProject in project.ParentSolution.GetAllDotNetProjects ()) {
				dotNetProject.RefreshReferenceStatus ();
			}
		}
	}
}

