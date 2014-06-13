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
		List<UpdatedPackagesInProject> projectsWithUpdatedPackages = new List<UpdatedPackagesInProject> ();

		public UpdatedPackagesInSolution (
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredPackageRepositories,
			IPackageManagementEvents packageManagementEvents)
		{
			this.solution = solution;
			this.registeredPackageRepositories = registeredPackageRepositories;
			this.packageManagementEvents = packageManagementEvents;

			this.packageManagementEvents.ParentPackageInstalled += PackageInstalled;
		}

		void PackageInstalled (object sender, ParentPackageOperationEventArgs e)
		{
			GuiDispatch (() => {
				RefreshUpdatedPackages (e);
			});
		}

		void RefreshUpdatedPackages (ParentPackageOperationEventArgs e)
		{
			UpdatedPackagesInProject updatedPackages = GetUpdatedPackages (e.Project.Project);
			updatedPackages.RemovePackage (e.Package);
		}

		public void Clear ()
		{
			projectsWithUpdatedPackages = new List<UpdatedPackagesInProject> ();
		}

		public void CheckForUpdates ()
		{
			foreach (IPackageManagementProject project in GetProjects ()) {
				CheckForUpdates (project);
			}

			if (AnyUpdates ()) {
				packageManagementEvents.OnUpdatedPackagesAvailable ();
			}
		}

		IEnumerable<IPackageManagementProject> GetProjects ()
		{
			return GuiSyncDispatch (() => {
				IPackageRepository repository = registeredPackageRepositories.CreateAggregateRepository ();
				return solution.GetProjects (repository).ToList ();
			});
		}

		void CheckForUpdates (IPackageManagementProject project)
		{
			LogCheckingForUpdates (project.Name);

			var updatedPackages = new UpdatedPackages (project, project.SourceRepository);
			List<IPackage> packages = updatedPackages.GetUpdatedPackages ().ToList ();

			if (packages.Any ()) {
				GuiDispatch (() => {
					projectsWithUpdatedPackages.Add (new UpdatedPackagesInProject (project.Project, packages));
				});
			}

			LogPackagesFound (packages.Count);
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
	}
}

