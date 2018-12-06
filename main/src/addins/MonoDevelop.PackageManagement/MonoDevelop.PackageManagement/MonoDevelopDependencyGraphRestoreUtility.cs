//
// MonoDevelopDependencyGraphRestoreUtility.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Threading;
using System.Threading.Tasks;
using NuGet.CommandLine;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.ProjectModel;

namespace MonoDevelop.PackageManagement
{
	static class MonoDevelopDependencyGraphRestoreUtility
	{
		/// <summary>
		/// Ten PackageReference projects is about when it starts to take the same time to run the
		/// GenerateRestoreGraphFile MSBuild target ten times, using the MSBuild host, as it does to use the
		/// MSBuildUtility to run MSBuild out of process. The assumption is that if there are a lot of projects
		/// then a complicated dependency graph is likely (such as with OrchardCore) and it is faster to use
		/// MSBuildUtility to get the package dependency graph for the entire solution instead of for each project
		/// with the MSBuild host.
		/// </summary>
		static readonly int MaxSupportedProjectsForMSBuildHost = 10;

		public static Task<DependencyGraphSpec> GetSolutionRestoreSpec (
			IMonoDevelopSolutionManager solutionManager,
			IEnumerable<BuildIntegratedNuGetProject> projects,
			DependencyGraphCacheContext context,
			CancellationToken cancellationToken)
		{
			if (projects.Count () > MaxSupportedProjectsForMSBuildHost) {
				return MSBuildUtility.GetSolutionRestoreSpec (solutionManager.Solution, projects, solutionManager.Configuration, context.Logger, cancellationToken);
			}

			return DependencyGraphRestoreUtility.GetSolutionRestoreSpec (solutionManager, context);
		}

		public static async Task<DependencyGraphSpec> GetSolutionRestoreSpec (
			IMonoDevelopSolutionManager solutionManager,
			BuildIntegratedNuGetProject project,
			DependencyGraphCacheContext context,
			CancellationToken cancellationToken)
		{
			// NuGet when restoring a single project will request the DependencyGraphSpec from every project in the
			// solution so we obtain this up front so it can be cached and avoid getting the package spec
			// individually for each project. To do this we need all the BuildIntegratedNuGetProjects in the solution.
			var projects = await solutionManager.GetNuGetProjectsAsync ();
			var buildIntegratedProjects = projects.OfType<BuildIntegratedNuGetProject> ().ToList ();
			return await GetSolutionRestoreSpec (solutionManager, buildIntegratedProjects, context, cancellationToken);
		}
	}
}
