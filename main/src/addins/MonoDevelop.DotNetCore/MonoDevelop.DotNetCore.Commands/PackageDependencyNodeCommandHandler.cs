//
// PackageDependencyNodeCommandHandler.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.DotNetCore.NodeBuilders;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.PackageManagement;
using MonoDevelop.PackageManagement.Commands;

namespace MonoDevelop.DotNetCore.Commands
{
	class PackageDependencyNodeCommandHandler : NodeCommandHandler
	{
		public override void DeleteItem ()
		{
			var node = (PackageDependencyNode)CurrentNode.DataItem;
			ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateRemoveSinglePackageMessage (node.Name);

			try {
				RemovePackage (node, progressMessage);
			} catch (Exception ex) {
				PackageManagementServices.BackgroundPackageActionRunner.ShowError (progressMessage, ex);
			}
		}

		void RemovePackage (PackageDependencyNode node, ProgressMonitorStatusMessage progressMessage)
		{
			IPackageAction action = PackageReferenceNodeCommandHandler.CreateUninstallPackageAction (node.Project, node.Name);
			PackageManagementServices.BackgroundPackageActionRunner.Run (progressMessage, action);
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		public void UpdateRemoveItem (CommandInfo info)
		{
			var node = (PackageDependencyNode)CurrentNode.DataItem;
			if (!node.CanBeRemoved) {
				info.Visible = false;
			} else {
				info.Enabled = CanDeleteMultipleItems ();
				info.Text = GettextCatalog.GetString ("Remove");
			}
		}

		public override void DeleteMultipleItems ()
		{
			int nodeCount = CurrentNodes.Count ();
			if (nodeCount == 1) {
				DeleteItem ();
			} else if (nodeCount > 0) {
				var nodes = CurrentNodes.Select (node => (PackageDependencyNode)node.DataItem).ToArray ();
				RemoveMultiplePackages (nodes);
			}
		}

		void RemoveMultiplePackages (PackageDependencyNode[] nodes)
		{
			ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateRemovingPackagesFromProjectMessage (nodes.Length);

			try {
				RemoveMultiplePackage (nodes, progressMessage);
			} catch (Exception ex) {
				PackageManagementServices.BackgroundPackageActionRunner.ShowError (progressMessage, ex);
			}
		}

		void RemoveMultiplePackage (PackageDependencyNode[] nodes, ProgressMonitorStatusMessage progressMessage)
		{
			var project = nodes[0].Project;
			string[] packageIds = nodes.Select (node => node.Name).ToArray ();
			IPackageAction action = PackageReferenceNodeCommandHandler.CreateUninstallPackagesAction (project, packageIds);
			PackageManagementServices.BackgroundPackageActionRunner.Run (progressMessage, action);
		}

		[CommandUpdateHandler (PackageReferenceNodeCommands.UpdatePackage)]
		void CheckCanUpdatePackage (CommandInfo info)
		{
			var node = (PackageDependencyNode)CurrentNode.DataItem;
			info.Visible = node.CanBeRemoved;
		}

		[CommandHandler (PackageReferenceNodeCommands.UpdatePackage)]
		public void UpdatePackage ()
		{
			var node = (PackageDependencyNode)CurrentNode.DataItem;
			var project = new DotNetProjectProxy (node.Project);

			PackageReferenceNodeCommandHandler.UpdatePackage (
				project,
				node.Name,
				!node.IsReleaseVersion ());
		}
	}
}
