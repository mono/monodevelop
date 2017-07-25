//
// RestorePackagesInProjectHandler.cs
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
using MonoDevelop.Projects;
using NuGet.ProjectManagement.Projects;

namespace MonoDevelop.PackageManagement.Commands
{
	internal class RestorePackagesInProjectHandler : PackagesCommandHandler
	{
		protected override void Run ()
		{
			Run (GetSelectedDotNetProject ());
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = SelectedDotNetProjectHasPackages ();
		}

		public static void Run (DotNetProject project)
		{
			Run (project, false);
		}

		public static void Run (DotNetProject project, bool restoreTransitiveProjectReferences)
		{
			try {
				ProgressMonitorStatusMessage message = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInProjectMessage ();
				IPackageAction action = CreateRestorePackagesAction (project, restoreTransitiveProjectReferences);
				PackageManagementServices.BackgroundPackageActionRunner.Run (message, action);
			} catch (Exception ex) {
				ShowStatusBarError (ex);
			}
		}

		static IPackageAction CreateRestorePackagesAction (DotNetProject project, bool restoreTransitiveProjectReferences)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			var nugetProject = solutionManager.GetNuGetProject (new DotNetProjectProxy (project));

			var buildIntegratedProject = nugetProject as BuildIntegratedNuGetProject;
			if (buildIntegratedProject != null) {
				return new RestoreNuGetPackagesInNuGetIntegratedProject (
					project,
					buildIntegratedProject,
					solutionManager,
					restoreTransitiveProjectReferences);
			}

			var nugetAwareProject = project as INuGetAwareProject;
			if (nugetAwareProject != null) {
				return new RestoreNuGetPackagesInNuGetAwareProjectAction (project, solutionManager);
			}

			return new RestoreNuGetPackagesInProjectAction (project, nugetProject, solutionManager);
		}

		static void ShowStatusBarError (Exception ex)
		{
			ProgressMonitorStatusMessage message = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInProjectMessage ();
			PackageManagementServices.BackgroundPackageActionRunner.ShowError (message, ex);
		}
	}
}

