//
// NuGetAwareProjectPackageRestoreManager.cs
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
using System.Threading.Tasks;
using System.Threading;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	class NuGetAwareProjectPackageRestoreManager
	{
		IMonoDevelopSolutionManager solutionManager;

		public NuGetAwareProjectPackageRestoreManager (
			IMonoDevelopSolutionManager solutionManager)
		{
			this.solutionManager = solutionManager;
		}

		public Task RestoreMissingPackagesAsync (
			INuGetAwareProject project,
			INuGetProjectContext context,
			CancellationToken token)
		{
			return project.RestorePackagesAsync (solutionManager, context, token);
		}

		public async Task RestoreMissingPackagesAsync (
			IEnumerable<INuGetAwareProject> projects,
			NuGetProjectContext context,
			CancellationToken token)
		{
			foreach (INuGetAwareProject project in projects) {
				await RestoreMissingPackagesAsync (project, context, token);
			}
		}

		public async Task<IEnumerable<INuGetAwareProject>> GetProjectsRequiringRestore (
			IEnumerable<INuGetAwareProject> projects)
		{
			var projectsToBeRestored = new List<INuGetAwareProject> ();

			foreach (INuGetAwareProject project in projects) {
				if (await project.HasMissingPackages (solutionManager))
					projectsToBeRestored.Add (project);
			}

			return projectsToBeRestored;
		}
	}
}
