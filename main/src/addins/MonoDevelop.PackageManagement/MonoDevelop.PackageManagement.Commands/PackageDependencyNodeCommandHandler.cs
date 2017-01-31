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
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.PackageManagement.NodeBuilders;

namespace MonoDevelop.PackageManagement.Commands
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
			IPackageAction action = CreateUninstallPackageAction (node);
			PackageManagementServices.BackgroundPackageActionRunner.Run (progressMessage, action);
		}

		IPackageAction CreateUninstallPackageAction (PackageDependencyNode node)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (node.Project.ParentSolution);
			return new UninstallNuGetPackageAction (solutionManager, new DotNetProjectProxy (node.Project)) {
				PackageId = node.Name
			};
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		public void UpdateRemoveItem (CommandInfo info)
		{
			var node = (PackageDependencyNode)CurrentNode.DataItem;
			info.Enabled = CanDeleteMultipleItems () && node.CanBeRemoved;
			info.Text = GettextCatalog.GetString ("Remove");
		}

		public override bool CanDeleteMultipleItems ()
		{
			return !MultipleSelectedNodes;
		}
	}
}
