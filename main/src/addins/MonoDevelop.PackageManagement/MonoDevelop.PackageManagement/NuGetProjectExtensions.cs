//
// NuGetProjectExtensions.cs
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal static class NuGetProjectExtensions
	{
		public static FilePath GetPackagesFolderPath (this NuGetProject project, IMonoDevelopSolutionManager solutionManager)
		{
			if (project is ProjectJsonBuildIntegratedProjectSystem ) {
				string globalPackagesPath = SettingsUtility.GetGlobalPackagesFolder (solutionManager.Settings);

				return new FilePath (globalPackagesPath).FullPath;
			}

			string path = PackagesFolderPathUtility.GetPackagesFolderPath (solutionManager, solutionManager.Settings);
			return new FilePath (path).FullPath;
		}

		/// <summary>
		/// PostProcessAsync is not run for BuildIntegratedNuGetProjects so we run it directly after
		/// running a NuGet action.
		/// </summary>
		public static Task RunPostProcessAsync (this NuGetProject project, INuGetProjectContext context, CancellationToken token)
		{
			var buildIntegratedProject = project as IBuildIntegratedNuGetProject;
			if (buildIntegratedProject != null) {
				return buildIntegratedProject.PostProcessAsync (context, token);
			}

			return Task.FromResult (0);
		}

		public static void OnAfterExecuteActions (this NuGetProject project, IEnumerable<NuGetProjectAction> actions)
		{
			var buildIntegratedProject = project as IBuildIntegratedNuGetProject;
			if (buildIntegratedProject != null) {
				buildIntegratedProject.OnAfterExecuteActions (actions);
			}
		}

		public static void OnBeforeUninstall (this NuGetProject project, IEnumerable<NuGetProjectAction> actions)
		{
			var buildIntegratedProject = project as IBuildIntegratedNuGetProject;
			if (buildIntegratedProject != null) {
				buildIntegratedProject.OnBeforeUninstall (actions);
			}
		}
	}
}

