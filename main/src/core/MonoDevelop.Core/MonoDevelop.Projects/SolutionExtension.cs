//
// SolutionExtension.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Projects.Formats.MSBuild;

namespace MonoDevelop.Projects
{
	public class SolutionExtension: WorkspaceItemExtension
	{
		SolutionExtension next;

		protected Solution Solution {
			get { return (Solution) base.Item; }
		}

		internal protected override bool SupportsItem (WorkspaceItem item)
		{
			return item is Solution;
		}

		internal protected virtual Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return next.Build (monitor, configuration);
		}

		internal protected virtual Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return next.Clean (monitor, configuration);
		}

		internal protected virtual Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return next.Execute (monitor, context, configuration);
		}

		internal protected virtual bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return next.CanExecute (context, configuration);
		}

		internal protected virtual IEnumerable<ExecutionTarget> GetExecutionTargets (Solution solution, ConfigurationSelector configuration)
		{
			return next.GetExecutionTargets (solution, configuration);
		}

		internal protected virtual bool NeedsBuilding (ConfigurationSelector configuration)
		{
			return next.NeedsBuilding (configuration);
		}

		internal protected virtual void OnReadSolution (ProgressMonitor monitor, SlnFile file)
		{
			next.OnReadSolution (monitor, file);
		}

		internal protected virtual void OnWriteSolution (ProgressMonitor monitor, SlnFile file)
		{
			next.OnWriteSolution (monitor, file);
		}
	}
}

