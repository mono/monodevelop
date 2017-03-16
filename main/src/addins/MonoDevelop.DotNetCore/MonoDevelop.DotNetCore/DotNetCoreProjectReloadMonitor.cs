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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.PackageManagement;
using MonoDevelop.PackageManagement.Commands;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreProjectReloadMonitor
	{
		static readonly DotNetCoreProjectReloadMonitor monitor = new DotNetCoreProjectReloadMonitor ();

		DotNetCoreProjectReloadMonitor ()
		{
			if (IdeApp.IsInitialized) {
				PackageManagementServices.ProjectService.ProjectReloaded += ProjectReloaded;
				FileService.FileChanged += FileChanged;
			}
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
			DotNetCoreProjectBuilderMaintainer.OnProjectReload (e);
			RestorePackagesInProjectHandler.Run (e.NewProject.DotNetProject);
		}

		async void FileChanged (object sender, FileEventArgs e)
		{
			Runtime.AssertMainThread ();

			try {
				if (!PackageManagementServices.BackgroundPackageActionRunner.IsRunning) {
					await OnFileChanged (e);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("DotNetCoreProjectReloadMonitor error", ex);
			}
		}

		async Task OnFileChanged (IEnumerable<FileEventInfo> files)
		{
			var projectFiles = GetProjectFiles (files);
			if (!projectFiles.Any ())
				return;

			var unknownProjects = GetUnknownProjects ();
			if (!unknownProjects.Any ())
				return;

			var projectsToReload = GetProjectsToReload (unknownProjects, projectFiles);
			if (!projectsToReload.Any ())
				return;

			var reloadedProjects = await ReloadProjects (projectsToReload);

			if (reloadedProjects.Any ()) {
				RestorePackages (reloadedProjects);
			}
		}

		IEnumerable<FileEventInfo> GetProjectFiles (IEnumerable<FileEventInfo> files)
		{
			return files.Where (file => file.FileName.HasSupportedDotNetCoreProjectFileExtension ())
				.ToList ();
		}

		/// <summary>
		/// Gets all unknown projects that are not deliberately unloaded.
		/// Deliberately unloaded projects have Enabled set to false.
		/// </summary>
		IEnumerable<UnknownSolutionItem> GetUnknownProjects ()
		{
			return IdeApp.Workspace.GetAllItems<UnknownSolutionItem> ()
				.Where (project => project.Enabled)
				.ToList ();
		}

		IEnumerable<UnknownSolutionItem> GetProjectsToReload (
			IEnumerable<UnknownSolutionItem> unknownProjects,
			IEnumerable<FileEventInfo> projectFiles)
		{
			var projectFileNames = projectFiles.Select (projectFile => projectFile.FileName).ToList ();
			return unknownProjects.Where (project => projectFileNames.Contains (project.FileName)).ToList ();
		}

		async Task<IEnumerable<DotNetProject>> ReloadProjects (IEnumerable<UnknownSolutionItem> projectsToReload)
		{
			var reloadedProjects = new List<DotNetProject> ();
			using (ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
				foreach (var project in projectsToReload) {
					var reloadedProject = (await project.ParentFolder.ReloadItem (monitor, project)) as DotNetProject;
					if (reloadedProject != null && reloadedProject.HasFlavor<DotNetCoreProjectExtension> ()) {
						reloadedProjects.Add (reloadedProject);
					}
				}
			}
			return reloadedProjects;
		}

		void RestorePackages (IEnumerable<DotNetProject> projects)
		{
			var actions = projects.Select (project => new RestoreNuGetPackagesInDotNetCoreProject (project)).ToList ();
			var message = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInProjectMessage ();
			PackageManagementServices.BackgroundPackageActionRunner.Run (message, actions);
		}
	}
}
