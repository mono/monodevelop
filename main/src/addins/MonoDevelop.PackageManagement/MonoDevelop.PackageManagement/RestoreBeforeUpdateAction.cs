//
// RestoreBeforeUpdateAction.cs
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
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement
{
	public class RestoreBeforeUpdateAction
	{
		IPackageManagementProjectService projectService;
		IBackgroundPackageActionRunner backgroundRunner;

		public RestoreBeforeUpdateAction ()
			: this (
				PackageManagementServices.ProjectService,
				PackageManagementServices.BackgroundPackageActionRunner)
		{
		}

		public RestoreBeforeUpdateAction (
			IPackageManagementProjectService projectService,
			IBackgroundPackageActionRunner backgroundRunner)
		{
			this.projectService = projectService;
			this.backgroundRunner = backgroundRunner;
		}

		public static void Restore (
			IPackageManagementProject project,
			Action afterRestore)
		{
			var runner = new RestoreBeforeUpdateAction ();
			runner.RestoreProjectPackages (project.DotNetProject, afterRestore);
		}

		public static void Restore (
			IEnumerable<IPackageManagementProject> projects,
			Action afterRestore)
		{
			var runner = new RestoreBeforeUpdateAction ();
			runner.RestoreAllPackagesInSolution (
				projects.Select (project => project.DotNetProject),
				afterRestore);
		}

		public void RestoreAllPackagesInSolution (
			IEnumerable<DotNetProject> projects,
			Action afterRestore)
		{
			var restorer = new PackageRestorer (projects);
			Restore (restorer, afterRestore);
		}

		void Restore (PackageRestorer restorer, Action afterRestore)
		{
			ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesBeforeUpdateMessage ();

			DispatchService.BackgroundDispatch (() => {
				restorer.Restore (progressMessage);
				if (!restorer.RestoreFailed) {
					afterRestore ();
				}
			});
		}

		public void RestoreProjectPackages (DotNetProject project, Action afterRestore)
		{
			var restorer = new PackageRestorer (project);
			Restore (restorer, afterRestore);
		}
	}
}

