//
// Copyright (C) Microsoft Corp.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	partial class ProjectOperations
	{
		/// <summary>
		/// Represents a group of solution items being built together.
		/// </summary>
		class SolutionItemBuildBatch : IBuildTarget
		{
			string name;
			Solution sln;

			/// <summary>
			/// Simplifies a group of build targets
			/// </summary>
			public static IBuildTarget Create (IEnumerable<IBuildTarget> targets)
			{
				Solution sln = null;
				var buildTargets = new HashSet<SolutionItem> ();

				foreach (var target in targets) {
					if (target is SolutionItem si) {
						var parent = si.ParentSolution;
						if (parent == null) {
							throw new InvalidOperationException ("Items must be part of a solution");
						}
						if (sln != null && sln != parent) {
							throw new InvalidOperationException ("All items must be in the same solution");
						} else {
							sln = parent;
						}
						buildTargets.Add (si);
						continue;
					}
					if (target is Solution s) {
						if (sln != null && sln != s) {
							throw new InvalidOperationException ("All items must be in the same solution");
						}
						return s;
					}
				}
				if (buildTargets.Count == 1) {
					return buildTargets.First ();
				}
				return new SolutionItemBuildBatch (sln, buildTargets);
			}

			public ICollection<SolutionItem> Items { get; }

			SolutionItemBuildBatch (Solution sln, ICollection<SolutionItem> items)
			{
				this.sln = sln;
				Items = items;
			}

			public string Name => name ?? (name = string.Join (";", Items.Select (s => s.Name)));

			public bool CanBuild (ConfigurationSelector configuration)
			{
				return Items.All (item => ((IBuildTarget)item).CanBuild (configuration));
			}

			public Task<BuildResult> Build (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferencedTargets = false, OperationContext operationContext = null)
			{
				return sln.BuildItems (monitor, configuration, Items);
			}

			public Task<BuildResult> Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext = null)
			{
				return sln.CleanItems (monitor, configuration, Items);
			}

			public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
			{
				throw new NotSupportedException ("Execution not supported for build groups");
			}

			public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
			{
				throw new NotSupportedException ("Execution not supported for build groups");
			}

			public IEnumerable<IBuildTarget> GetExecutionDependencies ()
			{
				throw new NotSupportedException ("Execution not supported for build groups");
			}

			public Task PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
			{
				throw new NotSupportedException ("Execution not supported for build groups");
			}

			[Obsolete]
			public bool NeedsBuilding (ConfigurationSelector configuration)
			{
				return Items.Any (item => ((IBuildTarget)item).NeedsBuilding (configuration));
			}
		}
	}
}