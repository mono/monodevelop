//
// ProjectPackagesFolderNodeCommandHandler.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.PackageManagement.NodeBuilders;

namespace MonoDevelop.PackageManagement.Commands
{
	internal class ProjectPackagesFolderNodeCommandHandler : NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			var runner = new ManagePackagesDialogRunner ();
			runner.Run (IdeApp.ProjectOperations.CurrentSelectedProject);
		}

		[CommandUpdateHandler (PackagesFolderNodeCommands.ReinstallAllPackagesInProject)]
		void UpdateReinstallPackages (CommandInfo info)
		{
			var packagesFolderNode = (ProjectPackagesFolderNode)CurrentNode.DataItem;
			info.Visible = packagesFolderNode.AnyPackageReferencesRequiringReinstallation ();
		}

		[CommandHandler (PackagesFolderNodeCommands.ReinstallAllPackagesInProject)]
		public void ReinstallPackages ()
		{
			try {
				var packagesFolderNode = (ProjectPackagesFolderNode)CurrentNode.DataItem;
				List<ReinstallNuGetPackageAction> reinstallActions = CreateReinstallActions (packagesFolderNode).ToList ();
				ProgressMonitorStatusMessage progressMessage = CreateProgressMessage (reinstallActions);
				PackageManagementServices.BackgroundPackageActionRunner.Run (progressMessage, reinstallActions);
			} catch (Exception ex) {
				ShowStatusBarError (ex);
			}
		}

		IEnumerable<ReinstallNuGetPackageAction> CreateReinstallActions (ProjectPackagesFolderNode packagesFolderNode)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (packagesFolderNode.Project.ParentSolution);

			return packagesFolderNode.GetPackageReferencesNodes ()
				.Select (packageReferenceNode => CreateReinstallPackageAction (packageReferenceNode, solutionManager));
		}

		ReinstallNuGetPackageAction CreateReinstallPackageAction (
			PackageReferenceNode packageReference,
			IMonoDevelopSolutionManager solutionManager)
		{
			var action = new ReinstallNuGetPackageAction (
				packageReference.Project,
				solutionManager);

			action.PackageId = packageReference.Id;
			action.Version = packageReference.Version;

			return action;
		}

		ProgressMonitorStatusMessage CreateProgressMessage (IEnumerable<ReinstallNuGetPackageAction> actions)
		{
			if (actions.Count () == 1) {
				return ProgressMonitorStatusMessageFactory.CreateRetargetingSinglePackageMessage (actions.First ().PackageId);
			}
			return ProgressMonitorStatusMessageFactory.CreateRetargetingPackagesInProjectMessage (actions.Count ());
		}

		void ShowStatusBarError (Exception ex)
		{
			ProgressMonitorStatusMessage message = ProgressMonitorStatusMessageFactory.CreateRetargetingPackagesInProjectMessage ();
			PackageManagementServices.BackgroundPackageActionRunner.ShowError (message, ex);
		}
	}
}

