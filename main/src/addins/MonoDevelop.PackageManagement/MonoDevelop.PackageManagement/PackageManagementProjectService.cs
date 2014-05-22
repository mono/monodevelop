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
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

namespace ICSharpCode.PackageManagement
{
	public class PackageManagementProjectService : IPackageManagementProjectService
	{
		public PackageManagementProjectService()
		{
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

		public ISolution OpenSolution {
			get {
				Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
				if (solution != null) {
					return new SolutionProxy (solution);
				}
				return null;
			}
		}

		public IEnumerable<IDotNetProject> GetOpenProjects ()
		{
			Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution != null) {
				return solution
					.GetAllProjects ()
					.OfType<DotNetProject> ()
					.Select (project => new DotNetProjectProxy (project));
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
	}
}
