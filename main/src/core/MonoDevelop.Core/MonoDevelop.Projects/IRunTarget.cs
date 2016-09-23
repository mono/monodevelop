//
// IRunTarget.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;
using System.Threading.Tasks;
using System;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects
{
	public interface IRunTarget
	{
		/// <summary>
		/// Executes the target
		/// </summary>
		/// <param name="monitor">Monitor for tracking progress</param>
		/// <param name="context">Execution context</param>
		/// <param name="configuration">Configuration to execute</param>
		/// <param name="runConfiguration">Run configuration to use</param>
		Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, RunConfiguration runConfiguration);

		/// <summary>
		/// Determines whether this target can be executed using the specified execution context and configuration.
		/// </summary>
		/// <returns><c>true</c> if this instance can be executed; otherwise, <c>false</c>.</returns>
		/// <param name="context">An execution context</param>
		/// <param name="configuration">Configuration to execute</param>
		/// <param name="runConfiguration">Run configuration to use</param>
		bool CanExecute (ExecutionContext context, ConfigurationSelector configuration, RunConfiguration runConfiguration);

		/// <summary>
		/// Prepares the target for execution
		/// </summary>
		/// <returns>The execution.</returns>
		/// <param name="monitor">Monitor for tracking progress</param>
		/// <param name="context">Execution context</param>
		/// <param name="configuration">Configuration to execute</param>
		/// <param name="runConfiguration">Run configuration to use</param>
		/// <remarks>This method can be called (it is not mandatory) before Execute() to give the target a chance
		/// to asynchronously prepare the execution that is going to be done later on. It can be used for example
		/// to start the simulator that is going to be used for execution. Calling this method is optional, and
		/// there is no guarantee that Execute() will actually be called.</remarks>
		Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, RunConfiguration runConfiguration);

		/// <summary>
		/// Gets the run configurations that can be used to execute this item
		/// </summary>
		/// <returns>The run configurations.</returns>
		IEnumerable<RunConfiguration> GetRunConfigurations ();

		/// <summary>
		/// Gets the execution targets available for this item
		/// </summary>
		/// <returns>The execution targets.</returns>
		/// <param name="configuration">Configuration to execute</param>
		/// <param name="runConfiguration">Run configuration to use</param>
		IEnumerable<ExecutionTarget> GetExecutionTargets (ConfigurationSelector configuration, RunConfiguration runConfiguration);
	}
}
