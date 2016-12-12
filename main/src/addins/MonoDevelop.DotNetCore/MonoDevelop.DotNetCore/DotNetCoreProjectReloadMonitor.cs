//
// DotNetCoreProjectReloadMonitor.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.PackageManagement;
using MonoDevelop.Core;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreProjectReloadMonitor
	{
		static readonly DotNetCoreProjectReloadMonitor monitor = new DotNetCoreProjectReloadMonitor ();

		DotNetCoreProjectReloadMonitor ()
		{
			PackageManagementServices.ProjectService.ProjectReloaded += ProjectReloaded;
		}

		public static void Initialize ()
		{
		}

		void ProjectReloaded (object sender, ProjectReloadedEventArgs e)
		{
			Runtime.AssertMainThread ();
			try {
				if (IsDotNetCoreProjectReloaded (e.NewProject)) {
					OnDotNetCoreProjectReloaded (e);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("DotNetCoreProjectReloadMonitor error", ex);
			}
		}

		bool IsDotNetCoreProjectReloaded (IDotNetProject project)
		{
			// Ignore reloads when NuGet package restore is running.
			if (PackageManagementServices.BackgroundPackageActionRunner.IsRunning)
				return false;

			return project.DotNetProject.HasFlavor<DotNetCoreProjectExtension> ();
		}

		void OnDotNetCoreProjectReloaded (ProjectReloadedEventArgs e)
		{
			var action = new RestoreNuGetPackagesInDotNetCoreProject (e.NewProject.DotNetProject);
			var message = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInProjectMessage ();
			PackageManagementServices.BackgroundPackageActionRunner.Run (message, action);
		}
	}
}
