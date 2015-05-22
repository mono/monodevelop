// IBuildTarget.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System.Collections.Generic;
using MonoDevelop.Core;
using System.Threading.Tasks;
using System;

namespace MonoDevelop.Projects
{
	public interface IBuildTarget
	{
		/// <summary>
		/// Builds the target
		/// </summary>
		/// <param name="monitor">Monitor for tracking progress</param>
		/// <param name="configuration">Configuration to build</param>
		/// <param name="buildReferencedTargets">If set to <c>true</c> build referenced targets before building this one</param>
		Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferencedTargets = false, OperationContext operationContext = null);

		/// <summary>
		/// Cleans the targets
		/// </summary>
		/// <param name="monitor">Monitor for tracking progress</param>
		/// <param name="configuration">Configuration to clean</param>
		Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext = null);

		/// <summary>
		/// Executes the target
		/// </summary>
		/// <param name="monitor">Monitor for tracking progress</param>
		/// <param name="context">Execution context</param>
		/// <param name="configuration">Configuration to execute</param>
		Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration);

		/// <summary>
		/// Determines whether this target can be executed using the specified execution context and configuration.
		/// </summary>
		/// <returns><c>true</c> if this instance can be executed; otherwise, <c>false</c>.</returns>
		/// <param name="context">An execution context</param>
		/// <param name="configuration">Configuration to execute</param>
		bool CanExecute (ExecutionContext context, ConfigurationSelector configuration);

		/// <summary>
		/// Prepares the target for execution
		/// </summary>
		/// <returns>The execution.</returns>
		/// <param name="monitor">Monitor for tracking progress</param>
		/// <param name="context">Execution context</param>
		/// <param name="configuration">Configuration to execute</param>
		/// <remarks>This method can be called (it is not mandatory) before Execute() to give the target a chance
		/// to asynchronously prepare the execution that is going to be done later on. It can be used for example
		/// to start the simulator that is going to be used for execution. Calling this method is optional, and
		/// there is no guarantee that Execute() will actually be called.</remarks>
		Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration);

		[Obsolete ("This method will be removed in future releases")]
		bool NeedsBuilding (ConfigurationSelector configuration);

		/// <summary>
		/// Gets the name of the target
		/// </summary>
		/// <value>The name</value>
		string Name { get; }

		/// <summary>
		/// Gets the build targets that should be built before the project is executed.
		/// If the project itself is not included, it will not be built.
		/// </summary>
		IEnumerable<IBuildTarget> GetExecutionDependencies ();
	}
}
