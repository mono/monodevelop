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
using System.Linq;
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

		[Test]
		public async Task CheckAndBuildForExecute_SkipUpToDate ()
		{
			using (var executionDep = new ProjectWithExecutionDeps ("Dependency"))
			using (var executing = new ProjectWithExecutionDeps ("Executing"))
			using (var sln = CreateSimpleSolutionWithItems (executionDep, executing)) {

				executing.IsBuildUpToDate = true;
				executing.OverrideExecutionDependencies = new [] { executionDep };
				executionDep.ItemDependencies.Add (executing);

				var success = await IdeApp.ProjectOperations.CheckAndBuildForExecute (new [] { executing }, ConfigurationSelector.Default);
				Assert.IsTrue (success);
				Assert.IsTrue (executionDep.WasBuilt);

				// this is kinda hacky but until the tests ignore user prefs when run locally it's necessary
				Assert.AreEqual (!Runtime.Preferences.SkipBuildingUnmodifiedProjects, executing.WasBuilt);

				executionDep.WasBuilt = executing.WasBuilt = false;

				success = await IdeApp.ProjectOperations.CheckAndBuildForExecute (new [] { executing }, ConfigurationSelector.Default);
				Assert.IsTrue (success);
				Assert.IsFalse (executionDep.WasBuilt);
				Assert.IsFalse (executing.WasBuilt);
			}
		}

		[Test]
		public void GetBuildTargetsForExecution_ProjectIsExecutionTarget ()
		{
			using (var project = new ProjectWithExecutionDeps ("Executing")) {
				var targets = ProjectOperations.GetBuildTargetsForExecution (project, null);

				Assert.AreEqual (project, targets.Single ());
			}
		}

		[Test]
		public void GetBuildTargetsForExecution_Solution_SingleItemRunConfiguration ()
		{
			using (var project = new ProjectWithExecutionDeps ("Executing"))
			using (var sln = CreateSimpleSolutionWithItems (project)) {
				var runConfig = new SingleItemSolutionRunConfiguration (project, null);
				var targets = ProjectOperations.GetBuildTargetsForExecution (sln, runConfig);

				Assert.AreEqual (project, targets.Single ());
			}
		}

		[Test]
		public void GetBuildTargetsForExecution_Solution_MultiItemRunConfiguration ()
		{
			using (var executionDep = new ProjectWithExecutionDeps ("Dependency"))
			using (var executing = new ProjectWithExecutionDeps ("Executing"))
			using (var sln = CreateSimpleSolutionWithItems (executionDep, executing)) {
				var runConfig = new MultiItemSolutionRunConfiguration ();
				runConfig.Items.Add (new StartupItem (executing, null));
				runConfig.Items.Add (new StartupItem (executionDep, null));

				var targets = ProjectOperations.GetBuildTargetsForExecution (sln, runConfig);

				Assert.AreEqual (2, targets.Length);
				Assert.AreEqual (executing, targets [0]);
				Assert.AreEqual (executionDep, targets [1]);
			}
		}

		[Test]
		public void GetBuildTargetsForExecution_Solution_NoStartupItem_NoRunConfigurationPassed ()
		{
			using (var project = new ProjectWithExecutionDeps ("Executing"))
			using (var sln = CreateSimpleSolutionWithItems (project)) {
				sln.StartupConfiguration = null;
				var targets = ProjectOperations.GetBuildTargetsForExecution (sln, null);

				Assert.IsNull (sln.StartupItem);
				Assert.AreEqual (sln, targets.Single ());
			}
		}

		[Test]
		public void GetBuildTargetsForExecution_Solution_SingleStartupItem_NoRunConfigurationPassed ()
		{
			using (var project = new ProjectWithExecutionDeps ("Executing"))
			using (var sln = CreateSimpleSolutionWithItems (project)) {
				var runConfig = new SingleItemSolutionRunConfiguration (project, null);
				sln.StartupConfiguration = runConfig;

				var targets = ProjectOperations.GetBuildTargetsForExecution (sln, null);

				Assert.AreEqual (project, sln.StartupItem);
				Assert.AreEqual (project, targets.Single ());
			}
		}

		[Test]
		public void GetBuildTargetsForExecution_Solution_MultiStartupItems_NoRunConfigurationPassed ()
		{
			using (var executionDep = new ProjectWithExecutionDeps ("Dependency"))
			using (var executing = new ProjectWithExecutionDeps ("Executing"))
			using (var sln = CreateSimpleSolutionWithItems (executionDep, executing)) {
				var runConfig = new MultiItemSolutionRunConfiguration ();
				runConfig.Items.Add (new StartupItem (executing, null));
				runConfig.Items.Add (new StartupItem (executionDep, null));
				sln.StartupConfiguration = runConfig;

				var targets = ProjectOperations.GetBuildTargetsForExecution (sln, null);

				Assert.AreEqual (2, targets.Length);
				Assert.AreEqual (executing, targets [0]);
				Assert.AreEqual (executionDep, targets [1]);
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
			public bool WasBuilt { get; set; }
			public bool IsBuildUpToDate { get; set; }

			protected override IEnumerable<IBuildTarget> OnGetExecutionDependencies ()
			{
				return OverrideExecutionDependencies ?? base.OnGetExecutionDependencies ();
			}

			protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
			{
				IsBuildUpToDate = true;
				WasBuilt = true;
				return Task.FromResult (new BuildResult { BuildCount = 1 });
			}

			protected override bool OnFastCheckNeedsBuild (ConfigurationSelector configuration, TargetEvaluationContext context)
			{
				return !IsBuildUpToDate;
			}
		}

		[Test]
		public async Task BuildingForExecutionProperty ()
		{
			using (var proj = new CheckBuildingForExecutionPropertyProject ())
			using (var sln = CreateSimpleSolutionWithItems (proj)) {

				Assert.AreEqual (null, proj.BuildPropertyValue);
				Assert.AreEqual (null, proj.CheckPropertyValue);

				var success = await IdeApp.ProjectOperations.CheckAndBuildForExecute (new [] { proj }, ConfigurationSelector.Default);
				Assert.IsTrue (success);
				Assert.AreEqual (true, proj.BuildPropertyValue);
				Assert.AreEqual (true, proj.CheckPropertyValue);

				proj.CheckPropertyValue = null;
				proj.BuildPropertyValue = null;

				var result = await IdeApp.ProjectOperations.Build (proj).Task;
				Assert.IsFalse (result.Failed);
				Assert.AreEqual (false, proj.BuildPropertyValue);
				Assert.AreEqual (false, proj.CheckPropertyValue);
			}
		}

		class CheckBuildingForExecutionPropertyProject : Project
		{
			public CheckBuildingForExecutionPropertyProject ()
			{
				EnsureInitialized ();
			}

			public bool? BuildPropertyValue { get; set; }
			public bool? CheckPropertyValue { get; set; }

			protected override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
			{
				var ctx = operationContext as TargetEvaluationContext;
				BuildPropertyValue = ctx?.GlobalProperties?.GetValue<bool> ("IsBuildingForExecution") ?? false;
				return Task.FromResult (new BuildResult { BuildCount = 1 });
			}

			protected override bool OnFastCheckNeedsBuild (ConfigurationSelector configuration, TargetEvaluationContext context)
			{
				CheckPropertyValue = context.GlobalProperties.GetValue<bool> ("IsBuildingForExecution");
				return true;
			}
		}
	}
}
