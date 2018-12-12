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
using System;

namespace MonoDevelop.PackageManagement
{
	static class MSBuildPackageSpecCreator
	{
		static MSBuildPackageSpecCreator ()
		{
			string value = Environment.GetEnvironmentVariable ("MONODEVELOP_NUGET_RESTORE_VERBOSE");
			VerboseLogging = !string.IsNullOrEmpty (value);
		}

		public static bool VerboseLogging { get; private set; }

		public static async Task<DependencyGraphSpec> GetDependencyGraphSpec (DotNetProject project, ConfigurationSelector configuration, ILogger logger)
		{
			if (logger == null)
				logger = NullLogger.Instance;
			logger.Log (LogLevel.Information, GettextCatalog.GetString ("Getting restore information for project {0}", project.FileName));

			using (var resultsPath = new TempFile (".output.dg")) {
				var context = new TargetEvaluationContext ();
				context.GlobalProperties.SetValue ("RestoreGraphOutputPath", resultsPath);

				using (var monitor = CreateProgressMonitor ()) {
					var result = await project.RunTarget (monitor, "GenerateRestoreGraphFile", configuration, context);
					if (result != null) {
						foreach (BuildError error in result.BuildResult.Errors) {
							if (error.IsWarning)
								logger.LogWarning (error.ToString ());
							else
								logger.LogError (error.ToString ());
						}
					}
				}
				return GetDependencyGraph (resultsPath);
			}
		}

		public static async Task<PackageSpec> CreatePackageSpec (DotNetProject project, ConfigurationSelector configuration, ILogger logger)
		{
			var spec = await GetDependencyGraphSpec (project, configuration, logger);
			return spec.GetProjectSpec (project.FileName);
		}

		static ProgressMonitor CreateProgressMonitor ()
		{
			if (VerboseLogging)
				return new LoggingProgressMonitor ();

			return new ProgressMonitor ();
		}

		public static DependencyGraphSpec GetDependencyGraph (string resultsPath)
		{
			var fileInfo = new FileInfo (resultsPath);
			if (fileInfo.Exists && fileInfo.Length != 0) {
				return DependencyGraphSpec.Load (resultsPath);
			} else {
				return new DependencyGraphSpec ();
			}
		}
	}
}
