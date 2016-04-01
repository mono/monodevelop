//
// RestoreNuGetPackagesAction.cs
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;

namespace MonoDevelop.PackageManagement
{
	internal class RestoreNuGetPackagesAction : IPackageAction
	{
		IPackageRestoreManager restoreManager;
		ISolutionManager solutionManager;
		CancellationToken cancellationToken;
		IPackageManagementEvents packageManagementEvents;
		Solution solution;

		public RestoreNuGetPackagesAction (
			Solution solution,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			this.cancellationToken = cancellationToken;
			this.solution = solution;
			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			solutionManager = new MonoDevelopSolutionManager (solution);
			restoreManager = new PackageRestoreManager (
				SourceRepositoryProviderFactory.CreateSourceRepositoryProvider (),
				Settings.LoadDefaultSettings (null, null, null),
				solutionManager
			);
		}

		public void Execute ()
		{
			ExecuteAsync ().Wait ();
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		async Task ExecuteAsync ()
		{
			await restoreManager.RestoreMissingPackagesInSolutionAsync (
				solutionManager.SolutionDirectory,
				new NuGetProjectContext (),
				cancellationToken);

			await Runtime.RunInMainThread (() => RefreshProjectReferences ());

			packageManagementEvents.OnPackagesRestored ();
		}

		void RefreshProjectReferences ()
		{
			foreach (DotNetProject dotNetProject in solution.GetAllDotNetProjects ()) {
				dotNetProject.RefreshReferenceStatus ();
			}
		}
	}
}

