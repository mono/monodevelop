//
// CreateNuGetPackageBuildTarget.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Packaging
{
	class CreateNuGetPackageBuildTarget : IBuildTarget
	{
		DotNetProject project;

		public CreateNuGetPackageBuildTarget (DotNetProject project)
		{
			this.project = project;
		}

		public string Name {
			get { return project.Name; }
		}

		/// <summary>
		/// If the project is a NuGet packaging project then just use the normal build target.
		/// This ensures that the dependent projects are built.
		/// 
		/// Otherwise the Pack target is called.
		/// </summary>
		public async Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferencedTargets = false, OperationContext operationContext = null)
		{
			if (project is PackagingProject) {
				return await project.Build (monitor, configuration, buildReferencedTargets, new TargetEvaluationContext (operationContext));
			} else {
				return await Pack (monitor, configuration, buildReferencedTargets, operationContext);
			}
		}

		async Task<BuildResult> Pack (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferencedTargets, OperationContext operationContext)
		{
			var result = new BuildResult ();

			// Build the project and any dependencies first.
			if (buildReferencedTargets && (await project.GetReferencedItems (configuration, monitor.CancellationToken)).Any ()) {
				result = await project.Build (monitor, configuration, buildReferencedTargets, operationContext);
				if (result.Failed)
					return result;
			}

			// Generate the NuGet package by calling the Pack target.
			var packResult = (await project.RunTarget (monitor, "Pack", configuration, new TargetEvaluationContext (operationContext))).BuildResult;
			return result.Append (packResult);
		}

		public bool CanBuild (ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}

		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}

		public Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext = null)
		{
			throw new NotImplementedException ();
		}

		public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<IBuildTarget> GetExecutionDependencies ()
		{
			throw new NotImplementedException ();
		}

		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}

		public Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			throw new NotImplementedException ();
		}
	}
}
