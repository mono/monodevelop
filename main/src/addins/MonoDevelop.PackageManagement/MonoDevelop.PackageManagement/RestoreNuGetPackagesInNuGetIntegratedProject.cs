//
// RestoreNuGetPackagesInNuGetIntegratedProject.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.ProjectManagement.Projects;

namespace MonoDevelop.PackageManagement
{
	internal class RestoreNuGetPackagesInNuGetIntegratedProject : IPackageAction
	{
		IDotNetProject project;
		BuildIntegratedNuGetProject nugetProject;
		IMonoDevelopSolutionManager solutionManager;
		IMonoDevelopBuildIntegratedRestorer packageRestorer;
		IPackageManagementEvents packageManagementEvents;
		List<BuildIntegratedNuGetProject> referencingProjects;

		public RestoreNuGetPackagesInNuGetIntegratedProject (
			DotNetProject project,
			BuildIntegratedNuGetProject nugetProject,
			IMonoDevelopSolutionManager solutionManager,
			bool restoreTransitiveProjectReferences = false)
			: this (
				new DotNetProjectProxy (project),
				nugetProject,
				solutionManager,
				new MonoDevelopBuildIntegratedRestorer (solutionManager),
				restoreTransitiveProjectReferences)
		{
		}

		public RestoreNuGetPackagesInNuGetIntegratedProject (
			IDotNetProject project,
			BuildIntegratedNuGetProject nugetProject,
			IMonoDevelopSolutionManager solutionManager,
			IMonoDevelopBuildIntegratedRestorer packageRestorer,
			bool restoreTransitiveProjectReferences)
		{
			this.project = project;
			this.nugetProject = nugetProject;
			this.solutionManager = solutionManager;
			this.packageRestorer = packageRestorer;
			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			if (restoreTransitiveProjectReferences)
				IncludeTransitiveProjectReferences ();
		}

		public PackageActionType ActionType {
			get { return PackageActionType.Restore; }
		}

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			Task task = ExecuteAsync (cancellationToken);
			using (var restoreTask = new PackageRestoreTask (task)) {
				task.Wait ();
			}
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		async Task ExecuteAsync (CancellationToken cancellationToken)
		{
			if (referencingProjects == null)
				await packageRestorer.RestorePackages (nugetProject, cancellationToken);
			else
				await RestoreMultiplePackages (cancellationToken);

			await Runtime.RunInMainThread (() => project.RefreshReferenceStatus ());

			packageManagementEvents.OnPackagesRestored ();
		}

		/// <summary>
		/// Execute will restore packages for all projects that transitively reference the project
		/// passed to the constructor of this restore action if this method is passed.
		/// </summary>
		void IncludeTransitiveProjectReferences ()
		{
			var projects = project.DotNetProject.GetReferencingProjects ();
			if (!projects.Any ())
				return;

			referencingProjects = new List<BuildIntegratedNuGetProject> ();
			foreach (var referencingProject in projects) {
				var projectProxy = new DotNetProjectProxy (referencingProject);
				var currentNuGetProject = solutionManager.GetNuGetProject (projectProxy) as BuildIntegratedNuGetProject;
				if (currentNuGetProject != null) {
					referencingProjects.Add (currentNuGetProject);
				}
			}
		}

		Task RestoreMultiplePackages (CancellationToken cancellationToken)
		{
			var projects = new List<BuildIntegratedNuGetProject> ();
			projects.Add (nugetProject);
			projects.AddRange (referencingProjects);

			return packageRestorer.RestorePackages (projects, cancellationToken);
		}
	}
}

