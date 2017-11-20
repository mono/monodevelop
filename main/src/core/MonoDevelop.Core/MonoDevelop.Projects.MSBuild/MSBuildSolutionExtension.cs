//
// MSBuildSolutionExtension.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.MSBuild
{
	/// <summary>
	/// This class starts MSBuild build sessions when a build operation starts in the solution
	/// </summary>
	[ExportProjectModelExtension]
	class MSBuildSolutionExtension: SolutionExtension
	{
		public static readonly object MSBuildProjectOperationId = typeof (MSBuildSolutionExtension);

		internal protected override Task OnBeginBuildOperation (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			// If the context is a TargetEvaluationContext with a specific msbuild verbosity, use it
			// otherwise use the global setting
			var targetContext = operationContext as TargetEvaluationContext;
			var verbosity = targetContext != null ? targetContext.LogVerbosity : Runtime.Preferences.MSBuildVerbosity.Value;
			var logger = targetContext != null ? new ProxyLogger (null, targetContext.Loggers) : null;

			// Start the build session
			object sessionId = RemoteBuildEngineManager.StartBuildSession (monitor, logger, verbosity, GetSolutionConfigurations (configuration));

			// Store the session handle in the context, so that it can be later used to
			// add builds to the session.
			operationContext.SessionData [MSBuildProjectOperationId] = sessionId;

			return base.OnBeginBuildOperation (monitor, configuration, operationContext);
		}

		internal protected override async Task OnEndBuildOperation (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext, BuildResult result)
		{
			// Remove the session from the context and notify the build engine
			// manager that the session has finished.
			var id = operationContext.SessionData [MSBuildProjectOperationId];
			operationContext.SessionData.Remove (MSBuildProjectOperationId);
			await RemoteBuildEngineManager.EndBuildSession (id);
			await base.OnEndBuildOperation (monitor, configuration, operationContext, result);
		}

		ProjectConfigurationInfo [] GetSolutionConfigurations (ConfigurationSelector configuration)
		{
			List<ProjectConfigurationInfo> configs = new List<ProjectConfigurationInfo> ();
			var sc = Solution.GetConfiguration (configuration);
			foreach (var p in Solution.GetAllProjects ()) {
				var c = p.GetConfiguration (configuration);
				configs.Add (new ProjectConfigurationInfo () {
					ProjectFile = p.FileName,
					Configuration = c != null ? c.Name : "",
					Platform = c != null ? GetExplicitPlatform (c) : "",
					ProjectGuid = p.ItemId,
					Enabled = sc == null || sc.BuildEnabledForItem (p)
				});
			}
			return configs.ToArray ();
		}

		//for some reason, MD internally handles "AnyCPU" as "", but we need to be explicit when
		//passing it to the build engine
		static string GetExplicitPlatform (SolutionItemConfiguration configObject)
		{
			if (string.IsNullOrEmpty (configObject.Platform)) {
				return "AnyCPU";
			}
			return configObject.Platform;
		}

		public override void Dispose ()
		{
			if (Item is Solution)
				// Dispose all builders bound to the solution being disposed
				RemoteBuildEngineManager.UnloadSolution (Solution.FileName).Ignore ();
			
			base.Dispose ();
		}
	}
}
