//
// MSBuildPackageSpecCreator.cs
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

using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NuGet.Common;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;

namespace MonoDevelop.PackageManagement
{
	static class MSBuildPackageSpecCreator
	{
		public static async Task<PackageSpec> CreatePackageSpec (DotNetProject project, ILogger logger)
		{
			using (var resultsPath = new TempFile (".output.dg")) {
				var context = new TargetEvaluationContext ();
				context.GlobalProperties.SetValue ("RestoreGraphOutputPath", resultsPath);

				ConfigurationSelector config = IdeApp.Workspace?.ActiveConfiguration ?? ConfigurationSelector.Default;

				var result = await project.RunTarget (new ProgressMonitor (), "GenerateRestoreGraphFile", config, context);
				if (result != null) {
					foreach (BuildError error in result.BuildResult.Errors) {
						if (error.IsWarning)
							logger.LogWarning (error.ToString ());
						else
							logger.LogError (error.ToString ());
					}
				}
				var spec = GetDependencyGraph (resultsPath);
				return spec.GetProjectSpec (project.FileName);
			}
		}

		static DependencyGraphSpec GetDependencyGraph (string resultsPath)
		{
			if (File.Exists (resultsPath) && new FileInfo (resultsPath).Length != 0) {
				return DependencyGraphSpec.Load (resultsPath);
			} else {
				return new DependencyGraphSpec ();
			}
		}
	}
}
