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
using ICSharpCode.PackageManagement;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement.Commands
{
	public class PackageManagementStartupHandler : CommandHandler
	{
		IPackageManagementProjectService projectService;

		public PackageManagementStartupHandler ()
		{
			projectService = PackageManagementServices.ProjectService;
		}

		protected override void Run ()
		{
			projectService.SolutionLoaded += SolutionLoaded;
			projectService.SolutionUnloaded += SolutionUnloaded;
			IdeApp.Workspace.ItemUnloading += WorkspaceItemUnloading;
		}

		void SolutionLoaded (object sender, EventArgs e)
		{
			ClearUpdatedPackagesInSolution ();

			if (ShouldRestorePackages) {
				RestoreAndCheckForUpdates ();
			} else if (ShouldCheckForUpdates && AnyProjectHasPackages ()) {
				// Use background dispatch even though the check is not done on the
				// background dispatcher thread so that the solution load completes before
				// the check for updates starts. Otherwise the check for updates finishes
				// before the solution loads and the status bar never reports that
				// package updates were being checked.
				DispatchService.BackgroundDispatch (() => {
					CheckForUpdates ();
				});
			}
		}

		bool ShouldRestorePackages {
			get { return PackageManagementServices.Options.IsAutomaticPackageRestoreOnOpeningSolutionEnabled; }
		}

		bool ShouldCheckForUpdates {
			get { return PackageManagementServices.Options.IsCheckForPackageUpdatesOnOpeningSolutionEnabled; }
		}

		void ClearUpdatedPackagesInSolution ()
		{
			PackageManagementServices.UpdatedPackagesInSolution.Clear ();
		}

		void SolutionUnloaded (object sender, EventArgs e)
		{
			ClearUpdatedPackagesInSolution ();
		}

		void RestoreAndCheckForUpdates ()
		{
			bool checkUpdatesAfterRestore = ShouldCheckForUpdates && AnyProjectHasPackages ();

			var restorer = new PackageRestorer (projectService.OpenSolution.Solution);
			DispatchService.BackgroundDispatch (() => {
				restorer.Restore ();
				if (checkUpdatesAfterRestore && !restorer.RestoreFailed) {
					CheckForUpdates ();
				}
			});
		}

		bool AnyProjectHasPackages ()
		{
			return projectService
				.OpenSolution
				.Solution
				.GetAllProjectsWithPackages ()
				.Any ();
		}

		void CheckForUpdates ()
		{
			try {
				PackageManagementServices.UpdatedPackagesInSolution.CheckForUpdates ();
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
	}
}

