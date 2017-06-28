//
// UpdatedNuGetPackagesInWorkspace.cs
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
using MonoDevelop.Core;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class UpdatedNuGetPackagesInWorkspace : IUpdatedNuGetPackagesInWorkspace
	{
		CheckForNuGetPackageUpdatesTaskRunner taskRunner;
		IPackageManagementEvents packageManagementEvents;
		List<ISolution> pendingSolutions = new List<ISolution> ();
		List<UpdatedNuGetPackagesInProject> projectsWithUpdatedPackages = new List<UpdatedNuGetPackagesInProject> ();

		public UpdatedNuGetPackagesInWorkspace (
			IPackageManagementEvents packageManagementEvents)
		{
			this.packageManagementEvents = packageManagementEvents;
			this.taskRunner = CreateTaskRunner ();
		}

		protected virtual CheckForNuGetPackageUpdatesTaskRunner CreateTaskRunner ()
		{
			return new CheckForNuGetPackageUpdatesTaskRunner (this);
		}

		public void Clear ()
		{
			taskRunner.Stop ();
			projectsWithUpdatedPackages = new List<UpdatedNuGetPackagesInProject> ();
			pendingSolutions = new List<ISolution> ();
		}

		public void Clear (ISolution solution)
		{
			projectsWithUpdatedPackages.RemoveAll (project => project.ParentSolution.Equals (solution));
			pendingSolutions.RemoveAll (pendingSolution => pendingSolution.Equals (solution));
		}

		public void CheckForUpdates (ISolution solution)
		{
			CheckForUpdates (solution, null);
		}

		public void CheckForUpdates (ISolution solution, ISourceRepositoryProvider sourceRepositoryProvider)
		{
			GuiDispatch (() => {

				if (taskRunner.IsRunning) {
					pendingSolutions.Add (solution);
					return;
				}

				taskRunner.Start (GetProjects (solution), sourceRepositoryProvider);
			});
		}

		IEnumerable<IDotNetProject> GetProjects (ISolution solution)
		{
			if (solution != null) {
				foreach (IDotNetProject project in solution.GetAllProjects ()) {
					yield return project;
				}
			}
		}

		public void CheckForUpdatesCompleted (IEnumerable<UpdatedNuGetPackagesInProject> projects)
		{
			projectsWithUpdatedPackages.AddRange (projects.ToList ());

			if (AnyUpdates ()) {
				packageManagementEvents.OnUpdatedPackagesAvailable ();
			}

			if (pendingSolutions.Any ()) {
				var solution = pendingSolutions[0];
				pendingSolutions.RemoveAt (0);
				CheckForUpdates (solution);
			}
		}

		public UpdatedNuGetPackagesInProject GetUpdatedPackages (IDotNetProject project)
		{
			UpdatedNuGetPackagesInProject updatedPackages = projectsWithUpdatedPackages
				.FirstOrDefault (item => item.Project.Equals (project));

			if (updatedPackages != null) {
				return updatedPackages;
			}
			return new UpdatedNuGetPackagesInProject (project);
		}

		public bool AnyUpdates ()
		{
			return GuiSyncDispatch (() => {
				return projectsWithUpdatedPackages.Any ();
			});
		}

		protected virtual void GuiDispatch (Action action)
		{
			Runtime.RunInMainThread (action).Wait ();
		}

		T GuiSyncDispatch<T> (Func<T> action)
		{
			T result = default(T);
			GuiDispatch (() => result = action ());
			return result;
		}
	}
}

