// 
// PackageManagementProjectService.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
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
using System.Collections.Generic;
using MonoDevelop.Ide;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

namespace ICSharpCode.PackageManagement
{
	public class PackageManagementProjectService : IPackageManagementProjectService
	{
		public PackageManagementProjectService ()
		{
			IdeApp.Workspace.SolutionLoaded += (sender, e) => OnSolutionLoaded (e.Solution);
			IdeApp.Workspace.SolutionUnloaded += (sender, e) => OnSolutionUnloaded (e.Solution);
		}

		public event EventHandler SolutionLoaded;


		void OnSolutionLoaded (Solution solution)
		{
			solution.SolutionItemAdded += SolutionItemAdded;

			OpenSolution = new SolutionProxy (solution);

			EventHandler handler = SolutionLoaded;
			if (handler != null) {
				handler (this, new DotNetSolutionEventArgs (OpenSolution));
			}
		}

		public event EventHandler SolutionUnloaded;

		void OnSolutionUnloaded (Solution solution)
		{
			solution.SolutionItemAdded -= SolutionItemAdded;
			OpenSolution = null;

			var handler = SolutionUnloaded;
			if (handler != null) {
				var proxy = new SolutionProxy (solution);
				handler (this, new DotNetSolutionEventArgs (proxy));
			}
		}

		public IProject CurrentProject {
			get {
				Project project = IdeApp.ProjectOperations.CurrentSelectedProject;
				if (project != null) {
					if (project is DotNetProject) {
						return new DotNetProjectProxy ((DotNetProject)project);
					}
					return new ProjectProxy (project);
				}
				return null;
			}
		}

		public ISolution OpenSolution { get; private set; }

		public IEnumerable<IDotNetProject> GetOpenProjects ()
		{
			if (OpenSolution != null) {
				return OpenSolution.GetAllProjects ();
			}
			return new IDotNetProject [0];
		}

		public IProjectBrowserUpdater CreateProjectBrowserUpdater()
		{
			return new ThreadSafeProjectBrowserUpdater();
		}
		
		public string GetDefaultCustomToolForFileName(ProjectFile projectItem)
		{
			return String.Empty;
			//return CustomToolsService.GetCompatibleCustomToolNames(projectItem).FirstOrDefault();
		}

		public event EventHandler<ProjectReloadedEventArgs> ProjectReloaded;

		void SolutionItemAdded (object sender, SolutionItemChangeEventArgs e)
		{
			if (!e.Reloading)
				return;

			var handler = ProjectReloaded;
			if (handler != null) {
				ProjectReloadedEventArgs reloadedEventArgs = ProjectReloadedEventArgs.Create (e);
				if (reloadedEventArgs != null) {
					handler (this, reloadedEventArgs);
				}
			}
		}
	}
}
