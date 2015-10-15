//
// UpdatedPackagesInSolution.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class UpdatedPackagesInSolution : IUpdatedPackagesInSolution
	{
		IPackageManagementSolution solution;
		IRegisteredPackageRepositories registeredPackageRepositories;
		IPackageManagementEvents packageManagementEvents;
		CheckForUpdatesTaskRunner taskRunner;
		List<ParentPackageOperationDuringCheckForUpdates> packageOperationsDuringCheckForUpdates = new List<ParentPackageOperationDuringCheckForUpdates> ();
		List<UpdatedPackagesInProject> projectsWithUpdatedPackages = new List<UpdatedPackagesInProject> ();

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

		public UpdatedPackagesInSolution (
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredPackageRepositories,
			IPackageManagementEvents packageManagementEvents)
			: this (
				solution,
				registeredPackageRepositories,
				packageManagementEvents,
				new CheckForUpdatesTaskRunner ())
		{
		}

		public UpdatedPackagesInSolution (
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredPackageRepositories,
			IPackageManagementEvents packageManagementEvents,
			CheckForUpdatesTaskRunner taskRunner)
		{
			this.solution = solution;
			this.registeredPackageRepositories = registeredPackageRepositories;
			this.packageManagementEvents = packageManagementEvents;
			this.taskRunner = taskRunner;

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
			UpdatedPackagesInProject updatedPackages = GetUpdatedPackages (e.Project.Project);
			if (updatedPackages.AnyPackages ()) {
				if (!installed) {
					updatedPackages.RemovePackage (e.Package);
				}
				updatedPackages.RemoveUpdatedPackages (e.Project.GetPackageReferences ());
			}
		}

		public void Clear ()
		{
			taskRunner.Stop ();
			projectsWithUpdatedPackages = new List<UpdatedPackagesInProject> ();
			packageOperationsDuringCheckForUpdates = new List<ParentPackageOperationDuringCheckForUpdates> ();
		}

		public void CheckForUpdates ()
		{
			GuiDispatch (() => {
				Clear ();
				var task = new CheckForUpdatesTask (this, GetProjectsWithPackages ());
				taskRunner.Start (task);
			});
		}

		public void CheckForUpdatesCompleted (CheckForUpdatesTask task)
		{
			projectsWithUpdatedPackages = task.ProjectsWithUpdatedPackages.ToList ();

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

		IEnumerable<IPackageManagementProject> GetProjectsWithPackages ()
		{
			return GetProjects ().Where (project => HasPackages (project));
		}

		IEnumerable<IPackageManagementProject> GetProjects ()
		{
			IPackageRepository repository = registeredPackageRepositories.CreateAggregateRepository ();
			return solution.GetProjects (repository).ToList ();
		}

		bool HasPackages (IPackageManagementProject project)
		{
			return FileExists (project.Project.GetPackagesConfigFilePath ());
		}

		public UpdatedPackagesInProject CheckForUpdates (IPackageManagementProject project)
		{
			LogCheckingForUpdates (project.Name);

			project.Logger = new PackageManagementLogger (packageManagementEvents);
			var updatedPackages = new UpdatedPackages (project, project.SourceRepository);
			List<IPackage> packages = updatedPackages.GetUpdatedPackages ().ToList ();

			LogPackagesFound (packages.Count);

			return new UpdatedPackagesInProject (project.Project, packages);
		}

		void LogCheckingForUpdates (string projectName)
		{
			Log (GettextCatalog.GetString ("Checking {0} for updates...", projectName));
		}

		void LogPackagesFound (int count)
		{
			if (count == 1) {
				Log (GettextCatalog.GetString ("{0} update found.", count));
			} else {
				Log (GettextCatalog.GetString ("{0} updates found.", count));
			}
		}

		void Log (string message)
		{
			packageManagementEvents.OnPackageOperationMessageLogged (MessageLevel.Info, message);
		}

		public UpdatedPackagesInProject GetUpdatedPackages (IDotNetProject project)
		{
			UpdatedPackagesInProject updatedPackages = projectsWithUpdatedPackages
				.FirstOrDefault (item => item.Project.Equals (project));

			if (updatedPackages != null) {
				return updatedPackages;
			}
			return new UpdatedPackagesInProject (project);
		}

		public bool AnyUpdates ()
		{
			return GuiSyncDispatch (() => {
				return projectsWithUpdatedPackages.Any ();
			});
		}

		protected virtual void GuiDispatch (MessageHandler handler)
		{
			DispatchService.GuiSyncDispatch (handler);
		}

		T GuiSyncDispatch<T> (Func<T> action)
		{
			T result = default(T);
			GuiDispatch (() => result = action ());
			return result;
		}

		protected virtual bool FileExists (string path)
		{
			return File.Exists (path);
		}
	}
}

