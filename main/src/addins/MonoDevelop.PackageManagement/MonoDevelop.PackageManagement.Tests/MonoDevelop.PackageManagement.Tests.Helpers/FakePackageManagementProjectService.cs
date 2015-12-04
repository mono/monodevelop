//
// FakePackageManagementProjectService.cs
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
using ICSharpCode.PackageManagement;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakePackageManagementProjectService : IPackageManagementProjectService
	{
		public IProject CurrentProject { get; set; }
		public ISolution OpenSolution { get; set; }

		public ISolution SavedSolution;

		public void Save (ISolution solution)
		{
			SavedSolution = solution;
		}

		public List<IDotNetProject> OpenProjects = new List<IDotNetProject> ();

		public IEnumerable<IDotNetProject> GetOpenProjects ()
		{
			return OpenProjects;
		}

		public IProjectBrowserUpdater CreateProjectBrowserUpdater ()
		{
			throw new NotImplementedException ();
		}

		Dictionary<string, string> defaultCustomTools = new Dictionary<string, string> ();

		public void AddDefaultCustomToolForFileName (string fileName, string customTool)
		{
			defaultCustomTools.Add (fileName, customTool);
		}

		public string GetDefaultCustomToolForFileName (ProjectFile projectItem)
		{
			if (defaultCustomTools.ContainsKey (projectItem.FilePath.ToString ())) {
				return defaultCustomTools [projectItem.FilePath.ToString ()];
			}
			return String.Empty;
		}

		public event EventHandler SolutionLoaded;
		public event EventHandler SolutionUnloaded;

		public void RaiseSolutionLoadedEvent ()
		{
			RaiseSolutionLoadedEvent (new FakeSolution ());
		}

		public void RaiseSolutionLoadedEvent (ISolution solution)
		{
			if (SolutionLoaded != null) {
				SolutionLoaded (this, new DotNetSolutionEventArgs (solution));
			}
		}

		public void RaiseSolutionUnloadedEvent ()
		{
			RaiseSolutionUnloadedEvent (new FakeSolution ());
		}

		public void RaiseSolutionUnloadedEvent (ISolution solution)
		{
			if (SolutionUnloaded != null) {
				SolutionUnloaded (this, new DotNetSolutionEventArgs (solution));
			}
		}

		public event EventHandler<ProjectReloadedEventArgs> ProjectReloaded;

		public void RaiseProjectReloadedEvent (IDotNetProject oldProject, IDotNetProject newProject)
		{
			if (ProjectReloaded != null) {
				ProjectReloaded (this, new ProjectReloadedEventArgs (oldProject, newProject));
			}
		}
	}
}


