// 
// RestorePackagesHandler.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement.Commands
{
	internal class RestorePackagesHandler : PackagesCommandHandler
	{
		protected override void Run ()
		{
			Run (GetSelectedSolution ());
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = SelectedDotNetProjectOrSolutionHasPackages ();
		}

		public static void Run (Solution solution)
		{
			RestorePackages (solution, action => {});
		}

		static void RestorePackages (Solution solution, Action<RestoreNuGetPackagesAction> modifyRestoreAction)
		{
			Runtime.AssertMainThread ();

			try {
				ProgressMonitorStatusMessage message = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInSolutionMessage ();
				var action = new RestoreNuGetPackagesAction (solution);
				modifyRestoreAction (action);
				PackageManagementServices.BackgroundPackageActionRunner.Run (message, action);
			} catch (Exception ex) {
				ShowStatusBarError (ex);
			}
		}

		static void ShowStatusBarError (Exception ex)
		{
			ProgressMonitorStatusMessage message = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInSolutionMessage ();
			PackageManagementServices.BackgroundPackageActionRunner.ShowError (message, ex);
		}

		public static void RestoreBuildIntegratedNuGetProjects (Solution solution)
		{
			RestorePackages (solution, action => action.RestorePackagesConfigProjects = false);
		}
	}
}
