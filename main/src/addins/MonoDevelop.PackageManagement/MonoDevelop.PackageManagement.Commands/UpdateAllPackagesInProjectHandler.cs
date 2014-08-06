//
// UpdateAllPackagesInProjectHandler.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement.Commands
{
	public class UpdateAllPackagesInProjectHandler : PackagesCommandHandler
	{
		protected override void Run ()
		{
			try {
				IPackageManagementProject project = PackageManagementServices.Solution.GetActiveProject ();
				RestoreBeforeUpdateAction.Restore (project, () => {
					DispatchService.GuiSyncDispatch (() => Update (project));
				});
			} catch (Exception ex) {
				ShowStatusBarError (ex);
			}
		}

		void Update (IPackageManagementProject project)
		{
			try {
				var updateAllPackages = new UpdateAllPackagesInProject (project);
				List<UpdatePackageAction> updateActions = updateAllPackages.CreateActions ().ToList ();
				ProgressMonitorStatusMessage progressMessage = CreateProgressMessage (updateActions, project);
				PackageManagementServices.BackgroundPackageActionRunner.Run (progressMessage, updateActions);
			} catch (Exception ex) {
				ShowStatusBarError (ex);
			}
		}

		ProgressMonitorStatusMessage CreateProgressMessage (List<UpdatePackageAction> updateActions, IPackageManagementProject project)
		{
			if (updateActions.Count == 1) {
				return ProgressMonitorStatusMessageFactory.CreateUpdatingSinglePackageMessage (updateActions.First ().PackageId, project);
			}
			return ProgressMonitorStatusMessageFactory.CreateUpdatingPackagesInProjectMessage (updateActions.Count, project);
		}

		void ShowStatusBarError (Exception ex)
		{
			ProgressMonitorStatusMessage message = ProgressMonitorStatusMessageFactory.CreateUpdatingPackagesInProjectMessage ();
			PackageManagementServices.BackgroundPackageActionRunner.ShowError (message, ex);
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = SelectedDotNetProjectHasPackages ();
		}
	}
}

