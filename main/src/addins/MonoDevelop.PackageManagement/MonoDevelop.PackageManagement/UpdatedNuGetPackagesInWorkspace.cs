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
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class UpdatedNuGetPackagesInWorkspace : IUpdatedNuGetPackagesInWorkspace
	{
		IPackageManagementEvents packageManagementEvents;
		CheckForNuGetPackageUpdatesTaskRunner taskRunner;
		List<ParentPackageOperationDuringCheckForUpdates> packageOperationsDuringCheckForUpdates = new List<ParentPackageOperationDuringCheckForUpdates> ();
		List<UpdatedNuGetPackagesInProject> projectsWithUpdatedPackages = new List<UpdatedNuGetPackagesInProject> ();

		class ParentPackageOperationDuringCheckForUpdates
		{
			public ParentPackageOperationDuringCheckForUpdates (ParentPackageOperationEventArgs eventArgs, bool isInstall)
			{
				EventArgs = eventArgs;
				IsInstall = isInstall;
			}

			public ParentPackageOperationEventArgs EventArgs { get; set; }
			public bool IsInstall { get; set; }
		}

		public UpdatedNuGetPackagesInWorkspace (
			IPackageManagementEvents packageManagementEvents)
		{
			this.packageManagementEvents = packageManagementEvents;
			this.taskRunner = new CheckForNuGetPackageUpdatesTaskRunner (this);

			this.packageManagementEvents.ParentPackageInstalled += PackageInstalled;
			this.packageManagementEvents.ParentPackageUninstalled += PackageUninstalled;
		}

		void PackageInstalled (object sender, ParentPackageOperationEventArgs e)
		{
			RefreshUpdatedPackages (e, true);
		}

		void PackageUninstalled (object sender, ParentPackageOperationEventArgs e)
		{
			RefreshUpdatedPackages (e, false);
		}

		void RefreshUpdatedPackages (ParentPackageOperationEventArgs e, bool installed)
		{
			GuiDispatch (() => {
				if (taskRunner.IsRunning) {
					packageOperationsDuringCheckForUpdates.Add (
						new ParentPackageOperationDuringCheckForUpdates (e, installed));
				} else {
					RemoveUpdatedPackages (e, installed);
				}
			});
		}

		void RemoveUpdatedPackages (ParentPackageOperationEventArgs e, bool installed)
		{
			UpdatedNuGetPackagesInProject updatedPackages = GetUpdatedPackages (e.Project.Project);
			if (updatedPackages.AnyPackages ()) {
				if (!installed) {
					updatedPackages.RemovePackage (new PackageIdentity (
						e.Package.Id,
						new NuGetVersion (e.Package.Version.ToString ())));
				}
				updatedPackages.RemoveUpdatedPackages (e.Project.GetPackageReferences ());
			}
		}

		public void Clear ()
		{
			taskRunner.Stop ();
			projectsWithUpdatedPackages = new List<UpdatedNuGetPackagesInProject> ();
			packageOperationsDuringCheckForUpdates = new List<ParentPackageOperationDuringCheckForUpdates> ();
		}

		public void CheckForUpdates ()
		{
			GuiDispatch (() => {
				Clear ();
				taskRunner.Start (GetProjects ());
			});
		}

		IEnumerable<DotNetProject> GetProjects ()
		{
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution != null) {
				foreach (DotNetProject project in solution.GetAllDotNetProjects ()) {
					yield return project;
				}
			}
		}

		public void CheckForUpdatesCompleted (IEnumerable<UpdatedNuGetPackagesInProject> projects)
		{
			projectsWithUpdatedPackages = projects.ToList ();

			RemovePackagesUpdatedDuringCheckForUpdates ();

			if (AnyUpdates ()) {
				packageManagementEvents.OnUpdatedPackagesAvailable ();
			}
		}

		void RemovePackagesUpdatedDuringCheckForUpdates ()
		{
			foreach (ParentPackageOperationDuringCheckForUpdates operation in packageOperationsDuringCheckForUpdates) {
				RemoveUpdatedPackages (operation.EventArgs, operation.IsInstall);
			}
			packageOperationsDuringCheckForUpdates.Clear ();
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

