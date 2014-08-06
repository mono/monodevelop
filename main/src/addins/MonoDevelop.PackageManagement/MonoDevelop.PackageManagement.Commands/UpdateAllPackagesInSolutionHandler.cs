//
// UpdateAllPackagesInSolutionCommandHandler.cs
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
	public class UpdateAllPackagesInSolutionHandler : PackagesCommandHandler
	{
		protected override void Run ()
		{
			try {
				UpdateAllPackagesInSolution updateAllPackages = CreateUpdateAllPackagesInSolution ();
				ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateUpdatingPackagesInSolutionMessage (updateAllPackages.Projects);
				RestoreBeforeUpdateAction.Restore (updateAllPackages.Projects, () => {
					DispatchService.GuiSyncDispatch (() => {
						Update (updateAllPackages, progressMessage);
					});
				});
			} catch (Exception ex) {
				ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateUpdatingPackagesInSolutionMessage ();
				PackageManagementServices.BackgroundPackageActionRunner.ShowError (progressMessage, ex);
			}
		}

		void Update (UpdateAllPackagesInSolution updateAllPackages, ProgressMonitorStatusMessage progressMessage)
		{
			try {
				List<UpdatePackageAction> updateActions = updateAllPackages.CreateActions ().ToList ();
				PackageManagementServices.BackgroundPackageActionRunner.Run (progressMessage, updateActions);
			} catch (Exception ex) {
				PackageManagementServices.BackgroundPackageActionRunner.ShowError (progressMessage, ex);
			}
		}

		UpdateAllPackagesInSolution CreateUpdateAllPackagesInSolution ()
		{
			return new UpdateAllPackagesInSolution (
				PackageManagementServices.Solution,
				PackageManagementServices.PackageRepositoryCache.CreateAggregateRepository ());
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = SelectedDotNetProjectOrSolutionHasPackages ();
		}
	}
}

