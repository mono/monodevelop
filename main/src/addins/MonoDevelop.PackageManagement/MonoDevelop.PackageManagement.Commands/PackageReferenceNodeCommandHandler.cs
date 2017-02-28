//
// PackageReferenceNodeCommandHandler.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.PackageManagement.NodeBuilders;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement.Commands
{
	internal class PackageReferenceNodeCommandHandler : NodeCommandHandler
	{
		public override void DeleteItem ()
		{
			var packageReferenceNode = (PackageReferenceNode)CurrentNode.DataItem;
			ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateRemoveSinglePackageMessage (packageReferenceNode.Id);

			try {
				RemovePackage (packageReferenceNode, progressMessage);
			} catch (Exception ex) {
				PackageManagementServices.BackgroundPackageActionRunner.ShowError (progressMessage, ex);
			}
		}

		void RemovePackage (PackageReferenceNode packageReferenceNode, ProgressMonitorStatusMessage progressMessage)
		{
			IPackageAction action = CreateUninstallPackageAction (packageReferenceNode);
			PackageManagementServices.BackgroundPackageActionRunner.Run (progressMessage, action);
		}

		IPackageAction CreateUninstallPackageAction (PackageReferenceNode packageReferenceNode)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (packageReferenceNode.Project.ParentSolution);
			if (packageReferenceNode.NeedsRestoreBeforeUninstall ()) {
				return new RestoreAndUninstallNuGetPackageAction (solutionManager, packageReferenceNode.Project) {
					PackageId = packageReferenceNode.Id,
					Version = packageReferenceNode.Version
				};
			}

			return new UninstallNuGetPackageAction (solutionManager, packageReferenceNode.Project) {
				PackageId = packageReferenceNode.Id
			};
		}

		static internal IPackageAction CreateUninstallPackageAction (DotNetProject project, string packageId)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			return new UninstallNuGetPackageAction (solutionManager, new DotNetProjectProxy (project)) {
				PackageId = packageId
			};
		}

		internal static IPackageAction CreateUninstallPackagesAction (DotNetProject project, string[] packageIds)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			var action = new UninstallNuGetPackagesAction (solutionManager, new DotNetProjectProxy (project));
			action.AddPackageIds (packageIds);
			return action;
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		public void UpdateRemoveItem (CommandInfo info)
		{
			info.Enabled = CanDeleteMultipleItems ();
			info.Text = GettextCatalog.GetString ("Remove");
		}

		public override bool CanDeleteMultipleItems ()
		{
			return !MultipleSelectedNodes;
		}

		[CommandHandler (PackageReferenceNodeCommands.UpdatePackage)]
		public void UpdatePackage ()
		{
			var packageReferenceNode = (PackageReferenceNode)CurrentNode.DataItem;

			UpdatePackage (
				packageReferenceNode.Project,
				packageReferenceNode.Id,
				!packageReferenceNode.IsReleaseVersion ());
		}

		internal static void UpdatePackage (IDotNetProject project, string packageId, bool includePrerelease)
		{
			try {
				var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
				var action = new UpdateNuGetPackageAction (solutionManager, project) {
					PackageId = packageId,
					IncludePrerelease = includePrerelease
				};

				ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateUpdatingSinglePackageMessage (packageId, project);
				UpdatePackage (progressMessage, action);
			} catch (Exception ex) {
				ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateUpdatingSinglePackageMessage (packageId);
				PackageManagementServices.BackgroundPackageActionRunner.ShowError (progressMessage, ex);
			}
		}

		static void UpdatePackage (ProgressMonitorStatusMessage progressMessage, IPackageAction action)
		{
			try {
				PackageManagementServices.BackgroundPackageActionRunner.Run (progressMessage, action);
			} catch (Exception ex) {
				PackageManagementServices.BackgroundPackageActionRunner.ShowError (progressMessage, ex);
			}
		}

		[CommandUpdateHandler (PackageReferenceNodeCommands.ShowPackageVersion)]
		public void UpdateShowPackageVersionItem (CommandInfo info)
		{
			var packageReferenceNode = (PackageReferenceNode)CurrentNode.DataItem;
			info.Enabled = false;
			info.Text = packageReferenceNode.GetPackageVersionLabel ();
		}

		[CommandUpdateHandler (PackageReferenceNodeCommands.ReinstallPackage)]
		public void UpdateReinstallPackageItem (CommandInfo info)
		{
			var packageReferenceNode = (PackageReferenceNode)CurrentNode.DataItem;
			info.Visible = packageReferenceNode.IsReinstallNeeded;
		}

		[CommandHandler (PackageReferenceNodeCommands.ReinstallPackage)]
		public void ReinstallPackage ()
		{
			var packageReferenceNode = (PackageReferenceNode)CurrentNode.DataItem;
			var reinstaller = new PackageReinstaller ();
			reinstaller.Run (packageReferenceNode);
		}

		Solution GetSelectedSolution ()
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			if (project != null) {
				return project.ParentSolution;
			}
			return IdeApp.ProjectOperations.CurrentSelectedSolution;
		}
	}
}

