//
// Copyright (c) 2018 Microsoft Corp
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
using System.Diagnostics;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.Ide.Projects
{
	[TestFixture]
	public class ProjectOperationsTests : IdeTestBase
	{
		[TestFixtureSetUp]
		public void SetUp ()
		{
			if (!IdeApp.IsInitialized) {
				IdeApp.Initialize (new ProgressMonitor ());
			}
		}

		Solution CreateSimpleSolutionWithItems (params SolutionItem[] items)
		{
			var sln = new Solution ();
			foreach (var item in items) {
				sln.RootFolder.AddItem (item);
			}
			var cfg = sln.AddConfiguration ("Debug", true);
			foreach (var item in items) {
				cfg.GetEntryForItem (item).Build = true;
			}
			return sln;
		}

		[Test]
		public async Task CheckAndBuildForExecute_ProjectOnly ()
		{
			using (var other = new ProjectWithExecutionDeps ("Other"))
			using (var executing = new ProjectWithExecutionDeps ("Executing"))
			using (var sln = CreateSimpleSolutionWithItems (other, executing)) {

				var success = await IdeApp.ProjectOperations.CheckAndBuildForExecute (new [] { executing }, ConfigurationSelector.Default);

				Assert.IsTrue (success);
				Assert.IsFalse (other.WasBuilt);
				Assert.IsTrue (executing.WasBuilt);
			}
		}

		[Test]
		public async Task CheckAndBuildForExecute_DependencyButNotSelf ()
		{
			using (var executionDep = new ProjectWithExecutionDeps ("Dependency"))
			using (var executing = new ProjectWithExecutionDeps ("Executing"))
			using (var sln = CreateSimpleSolutionWithItems (executionDep, executing)) {

				executing.OverrideExecutionDependencies = new [] { executionDep };

				var success = await IdeApp.ProjectOperations.CheckAndBuildForExecute (new [] { executing }, ConfigurationSelector.Default);

				Assert.IsTrue (success);
				Assert.IsTrue (executionDep.WasBuilt);
				Assert.False (executing.WasBuilt);
			}
		}

		[Test]
		public async Task CheckAndBuildForExecute_DependencyReferencesExecuting ()
		{
			using (var executionDep = new ProjectWithExecutionDeps ("Dependency"))
			using (var executing = new ProjectWithExecutionDeps ("Executing"))
			using (var sln = CreateSimpleSolutionWithItems (executionDep, executing)) {

				executing.OverrideExecutionDependencies = new [] { executionDep };
				executionDep.ItemDependencies.Add (executing);

				var success = await IdeApp.ProjectOperations.CheckAndBuildForExecute (new [] { executing }, ConfigurationSelector.Default);
				Assert.IsTrue (success);
				Assert.IsTrue (executionDep.WasBuilt);
				Assert.IsTrue (executing.WasBuilt);
			}
		}

		[DebuggerDisplay ("Project {Name}")]
		class ProjectWithExecutionDeps : Project
		{
			public ProjectWithExecutionDeps (string name) : base ("foo")
			{
				EnsureInitialized ();
				Name = name;
			}

			public IBuildTarget[] OverrideExecutionDependencies { get; set; }
			public bool WasBuilt { get; private set; }

			protected override IEnumerable<IBuildTarget> OnGetExecutionDependencies ()
			{
				return OverrideExecutionDependencies ?? base.OnGetExecutionDependencies ();
			}

			protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
			{
				WasBuilt = true;
				return Task.FromResult (new BuildResult { BuildCount = 1 });
			}
		}
	}
}
