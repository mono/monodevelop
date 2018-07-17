//
// ProjectTargetFrameworkMonitor.cs
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

namespace MonoDevelop.PackageManagement
{
	internal class ProjectTargetFrameworkMonitor
	{
		List<MonitoredSolution> monitoredSolutions = new List<MonitoredSolution> ();

		public ProjectTargetFrameworkMonitor (IPackageManagementProjectService projectService)
		{
			projectService.SolutionLoaded += SolutionLoaded;
			projectService.SolutionUnloaded += SolutionUnloaded;
			projectService.ProjectReloaded += ProjectReloaded;
		}

		public event EventHandler<ProjectTargetFrameworkChangedEventArgs> ProjectTargetFrameworkChanged;

		protected virtual void OnProjectTargetFrameworkChanged (IDotNetProject project, bool isReload = false)
		{
			var handler = ProjectTargetFrameworkChanged;
			if (handler != null) {
				handler (this, new ProjectTargetFrameworkChangedEventArgs (project, isReload));
			}
		}

		void SolutionUnloaded (object sender, EventArgs e)
		{
			MonitoredSolution monitoredSolution = FindMonitoredSolution ((DotNetSolutionEventArgs)e);
			if (monitoredSolution == null)
				return;

			foreach (IDotNetProject project in monitoredSolution.Projects) {
				project.Modified -= ProjectModified;
				project.Saved -= ProjectSaved;
			}
			monitoredSolution.Projects.Clear ();

			monitoredSolution.Solution.ProjectAdded -= ProjectAdded;
			monitoredSolution.Solution.ProjectRemoved -= ProjectRemoved;
			monitoredSolutions.Remove (monitoredSolution);
		}

		MonitoredSolution FindMonitoredSolution (DotNetSolutionEventArgs eventArgs)
		{
			return FindMonitoredSolution (eventArgs.Solution);
		}

		MonitoredSolution FindMonitoredSolution (ISolution solutionToMatch)
		{
			if (monitoredSolutions.Count == 1)
				return monitoredSolutions [0];

			return monitoredSolutions.FirstOrDefault (monitoredSolution => monitoredSolution.Solution.FileName == solutionToMatch.FileName);
		}

		void SolutionLoaded (object sender, EventArgs e)
		{
			var solutionEventArgs = (DotNetSolutionEventArgs)e;
			ISolution solution = solutionEventArgs.Solution;
			solution.ProjectAdded += ProjectAdded;
			solution.ProjectRemoved += ProjectRemoved;
			List<IDotNetProject> projects = solution.GetAllProjects ().ToList ();

			foreach (IDotNetProject project in projects) {
				project.Modified += ProjectModified;
			}

			monitoredSolutions.Add (new MonitoredSolution {
				Solution = solution,
				Projects = projects
			});
		}

		void ProjectAdded (object sender, DotNetProjectEventArgs e)
		{
			MonitoredSolution monitoredSolution = FindMonitoredSolution ((ISolution)sender);
			e.Project.Modified += ProjectModified;
			monitoredSolution.Projects.Add (e.Project);
		}

		void ProjectModified (object sender, ProjectModifiedEventArgs e)
		{
			if (e.IsTargetFramework ()) {
				e.Project.Saved += ProjectSaved;
			}
		}

		void ProjectSaved (object sender, EventArgs e)
		{
			var project = (IDotNetProject)sender;
			project.Saved -= ProjectSaved;

			OnProjectTargetFrameworkChanged (project);
		}

		void ProjectReloaded (object sender, ProjectReloadedEventArgs e)
		{
			if (HasTargetFrameworkChanged (e.NewProject, e.OldProject)) {
				OnProjectTargetFrameworkChanged (e.NewProject, isReload: true);
			}
		}

		static bool HasTargetFrameworkChanged (IDotNetProject newProject, IDotNetProject oldProject)
		{
			if (newProject.TargetFrameworkMoniker != null) {
				return !newProject.TargetFrameworkMoniker.Equals (oldProject.TargetFrameworkMoniker);
			}
			return false;
		}

		void ProjectRemoved (object sender, DotNetProjectEventArgs e)
		{
			MonitoredSolution monitoredSolution = FindMonitoredSolution ((ISolution)sender);
			IDotNetProject matchedProject = monitoredSolution.Projects.FirstOrDefault (project => project.Equals (e.Project));
			if (matchedProject != null) {
				matchedProject.Modified -= ProjectModified;
				monitoredSolution.Projects.Remove (matchedProject);
			}
		}

		class MonitoredSolution
		{
			public ISolution Solution { get; set; }
			public EventHandler<DotNetProjectEventArgs> ProjectAddedHandler { get; set; }
			public List<IDotNetProject> Projects { get; set; }
		}
	}
}

