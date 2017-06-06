//
// PackageManagementStartupHandler.cs
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NuGet.Common;
using System.Collections.Generic;

namespace MonoDevelop.PackageManagement.Commands
{
	internal class PackageManagementStartupHandler : CommandHandler
	{
		protected override void Run ()
		{
			ClearUpdatedPackages ();
			IdeApp.Workspace.SolutionLoaded += SolutionLoaded;
			IdeApp.Workspace.SolutionUnloaded += SolutionUnloaded;
			IdeApp.Workspace.ItemUnloading += WorkspaceItemUnloading;
			IdeApp.Workspace.LastWorkspaceItemClosed += LastWorkspaceItemClosed;
			FileService.FileChanged += FileChanged;
		}

		async void SolutionLoaded (object sender, SolutionEventArgs e)
		{
			try {
				if (ShouldRestorePackages) {
					await RestoreAndCheckForUpdates (e.Solution);
				} else if (ShouldCheckForUpdates && AnyProjectHasPackages (e.Solution)) {
					CheckForUpdates (e.Solution);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("PackageManagementStartupHandler error", ex);
			}
		}

		bool ShouldRestorePackages {
			get { return PackageManagementServices.Options.IsAutomaticPackageRestoreOnOpeningSolutionEnabled; }
		}

		bool ShouldCheckForUpdates {
			get { return PackageManagementServices.Options.IsCheckForPackageUpdatesOnOpeningSolutionEnabled; }
		}

		void ClearUpdatedPackagesInSolution (Solution solution)
		{
			PackageManagementServices.UpdatedPackagesInWorkspace.Clear (new SolutionProxy (solution));
		}

		void ClearUpdatedPackages ()
		{
			PackageManagementServices.UpdatedPackagesInWorkspace.Clear ();
		}

		void SolutionUnloaded (object sender, SolutionEventArgs e)
		{
			ClearUpdatedPackagesInSolution (e.Solution);
		}

		void LastWorkspaceItemClosed (object sender, EventArgs e)
		{
			ClearUpdatedPackages ();
			PackageManagementCredentialService.Reset ();
		}

		async Task RestoreAndCheckForUpdates (Solution solution)
		{
			bool checkUpdatesAfterRestore = ShouldCheckForUpdates && AnyProjectHasPackages (solution);

			var action = new RestoreAndCheckForUpdatesAction (solution) {
				CheckForUpdatesAfterRestore = checkUpdatesAfterRestore
			};
			bool packagesToRestore = await action.HasMissingPackages ();
			if (packagesToRestore) {
				ProgressMonitorStatusMessage message = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInSolutionMessage ();
				PackageManagementServices.BackgroundPackageActionRunner.Run (message, action);
			} else if (checkUpdatesAfterRestore) {
				CheckForUpdates (solution);
			}
		}

		bool AnyProjectHasPackages (Solution solution)
		{
			return solution
				.GetAllProjectsWithPackages ()
				.Any ();
		}

		void CheckForUpdates (Solution solution)
		{
			CheckForUpdates (new SolutionProxy (solution));
		}

		void CheckForUpdates (ISolution solution)
		{
			try {
				PackageManagementServices.UpdatedPackagesInWorkspace.CheckForUpdates (solution);
			} catch (Exception ex) {
				LoggingService.LogError ("Check for NuGet package updates error.", ex);
			}
		}

		void WorkspaceItemUnloading (object sender, ItemUnloadingEventArgs e)
		{
			try {
				if (PackageManagementServices.BackgroundPackageActionRunner.IsRunning) {
					MessageService.ShowMessage (GettextCatalog.GetString ("Unable to close the solution when NuGet packages are being processed."));
					e.Cancel = true;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error on unloading workspace item.", ex);
			}
		}

		//auto-restore project.json files when they're saved
		void FileChanged (object sender, FileEventArgs e)
		{
			if (PackageManagementServices.BackgroundPackageActionRunner.IsRunning)
				return;

			List<DotNetProject> projects = null;

			//collect all the projects with modified project.json files
			foreach (var eventInfo in e) {
				if (ProjectJsonPathUtilities.IsProjectConfig (eventInfo.FileName)) {
					var directory = eventInfo.FileName.ParentDirectory;
					foreach (var project in IdeApp.Workspace.GetAllItems<DotNetProject> ().Where (p => p.BaseDirectory == directory)) {
						if (projects == null) {
							projects = new List<DotNetProject> ();
						}
						projects.Add (project);
					}
				}
			}

			if (projects == null) {
				return;
			}

			//queue up in a timeout in case this was kicked off from a command
			GLib.Timeout.Add (0, () => {
				if (projects.Count == 1) {
					var project = projects [0];
					//check the project is still open
					if (IdeApp.Workspace.GetAllItems<DotNetProject> ().Any (p => p == project)) {
						RestorePackagesInProjectHandler.Run (projects [0]);
					}
				} else {
					var solution = projects [0].ParentSolution;
					//check the solution is still open
					if (IdeApp.Workspace.GetAllItems<Solution> ().Any (s => s == solution)) {
						RestorePackagesHandler.Run (solution);
					}
				}
				//TODO: handle project.json changing in multiple solutions at once
				return false;
			});
		}
	}
}
