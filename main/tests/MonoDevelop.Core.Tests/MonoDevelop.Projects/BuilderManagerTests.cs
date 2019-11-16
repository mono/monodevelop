//
// BuilderManagerTests.cs
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NUnit.Framework;
using UnitTests;
using System.Linq;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public class BuilderManagerTests: TestBase
	{
		// The solution used in this test fixture uses a special task that can
		// pause the build until signaled to continue. The task creates a file
		// named 'sync-event' when executed, and pauses execution until a file
		// named 'continue-event' is created next to it. This is used to pause
		// a project in the middle of a build, so that concurrency can be tested.
		// The SyncBuildProject project uses this task in a post-build target,
		// so the project build will be paused until signaled to conitnue.
		// The FileSync target can also uses that task.

		const int timeoutMs = 30000;

		[Test]
		public async Task ConcurrentLongOperations ()
		{
			// When a long operation is running and a new one is requested, a new builder is created

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

				var project1 = sol.Items.FirstOrDefault (p => p.Name == "SyncBuildProject");
				var project2 = sol.Items.FirstOrDefault (p => p.Name == "App");

				InitBuildSyncEvent (project1);

				// Start the build
				var build1 = project1.Build (Util.GetMonitor (), sol.Configurations [0].Selector);

				// Wait for the build to reach the sync task
				await WaitForBuildSyncEvent (project1);

				// The build is now in progess. Start a new build

				var build2 = project2.Build (Util.GetMonitor (), sol.Configurations [0].Selector);
				if (await Task.WhenAny (build2, Task.Delay (timeoutMs)) != build2)
					Assert.Fail ("Build did not start");

				// The second build should finish before the first one

				SignalBuildToContinue (project1);

				if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
					Assert.Fail ("Build did not end in time");
				
				Assert.AreEqual (0, build1.Result.ErrorCount);
				Assert.AreEqual (0, build2.Result.ErrorCount);
			}
		}

		[Test]
		public async Task ConcurrentLongAndShortOperations ()
		{
			// Tests that when a short operation is started while a long operation is in progress,
			// a new builder is created to execute the short operation

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

				var project1 = sol.Items.FirstOrDefault (p => p.Name == "SyncBuildProject");
				var project2 = (Project)sol.Items.FirstOrDefault (p => p.Name == "App");

				InitBuildSyncEvent (project1);

				// Start the build
				var build1 = project1.Build (Util.GetMonitor (), sol.Configurations [0].Selector);

				// Wait for the build to reach the sync task
				await WaitForBuildSyncEvent (project1);

				// The build is now in progess. Start a short operation.
				var context = new TargetEvaluationContext {
					BuilderQueue = BuilderQueue.ShortOperations
				};
				var build2 = project2.RunTarget (Util.GetMonitor (), "QuickTarget", sol.Configurations [0].Selector, context);

				if (await Task.WhenAny (build2, Task.Delay (timeoutMs)) != build2)
					Assert.Fail ("Build did not start");

				SignalBuildToContinue (project1);

				if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
					Assert.Fail ("Build did not end in time");
				
				Assert.AreEqual (0, build1.Result.ErrorCount);
				Assert.NotNull (build2.Result);
			}
		}

		[Test]
		public async Task ShortOperationsInSingleBuilder ()
		{
			// Tests that targets using BuilderQueue.ShortOperations share the same builder
			// and don't spawn a new builder when one of them is being executed and another
			// one starts executing.

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

				var project1 = (Project)sol.Items.FirstOrDefault (p => p.Name == "SyncBuildProject");
				var project2 = (Project)sol.Items.FirstOrDefault (p => p.Name == "App");

				InitBuildSyncEvent (project1);

				// Start the first target. The FileSync target will pause until signaled to continue.
				// Select the ShortOperations build queue.
				var context = new TargetEvaluationContext {
					BuilderQueue = BuilderQueue.ShortOperations
				};
				var build1 = project1.RunTarget (Util.GetMonitor (), "FileSync", sol.Configurations [0].Selector, context);

				// Wait for the build to reach the sync task
				await WaitForBuildSyncEvent (project1);

				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);

				// The build is now in progess. Run a new target.

				context = new TargetEvaluationContext {
					BuilderQueue = BuilderQueue.ShortOperations
				};
				var build2 = project2.RunTarget (Util.GetMonitor (), "QuickTarget", sol.Configurations [0].Selector, context);

				// Wait a bit. This should be enough to ensure the build has started.
				await Task.Delay (1000);

				// The RunTarget request should be queued, not new builder should be spawned
				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);

				// Continue building the first project
				SignalBuildToContinue (project1);

				// The first build should end now
				if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
					Assert.Fail ("Build did not end in time");
				
				// And now the second build should end
				if (await Task.WhenAny (build2, Task.Delay (timeoutMs)) != build2)
					Assert.Fail ("Build did not end in time");
				
				Assert.NotNull (build1.Result);
				Assert.NotNull (build2.Result);
				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
			}
		}

		[Test]
		public async Task ConcurrentShortAndLongOperations ()
		{
			// If a builder is running a short operation and a long operation is started,
			// that long operation will wait for the sort operation to finish
			// and will use the same builder, instead of starting a new one.

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

				var project1 = (Project)sol.Items.FirstOrDefault (p => p.Name == "SyncBuildProject");
				var project2 = (Project)sol.Items.FirstOrDefault (p => p.Name == "App");

				InitBuildSyncEvent (project1);

				// Start the first target. The FileSync target will pause until signaled to continue.
				// Select the ShortOperations build queue.
				var context = new TargetEvaluationContext {
					BuilderQueue = BuilderQueue.ShortOperations
				};
				var build1 = project1.RunTarget (Util.GetMonitor (), "FileSync", sol.Configurations [0].Selector, context);

				// Wait for the build to reach the sync task
				await WaitForBuildSyncEvent (project1);

				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);

				// The build is now in progess. Run a new target.

				context = new TargetEvaluationContext {
					BuilderQueue = BuilderQueue.LongOperations
				};
				var build2 = project2.RunTarget (Util.GetMonitor (), "QuickTarget", sol.Configurations [0].Selector, context);

				// Wait a bit. This should be enough to ensure the build has started.
				await Task.Delay (1000);

				// The RunTarget request should be queued, not new builder should be spawned
				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);

				// Continue building the first project
				SignalBuildToContinue (project1);

				// The first build should end now
				if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
					Assert.Fail ("Build did not end in time");

				// And now the second build should end
				if (await Task.WhenAny (build2, Task.Delay (timeoutMs)) != build2)
					Assert.Fail ("Build did not end in time");

				Assert.NotNull (build1.Result);
				Assert.NotNull (build2.Result);
				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
			}
		}

		[Test]
		public async Task ConcurrentShortAndBuildOperations ()
		{
			// If a builder is running a short operation and a build is started,
			// the build operation will wait for the sort operation to finish
			// and will use the same builder, instead of starting a new one.
			// Also, the build session should not start until the short operation
			// is finished.

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

				var project1 = (Project)sol.Items.FirstOrDefault (p => p.Name == "SyncBuildProject");
				var project2 = (Project)sol.Items.FirstOrDefault (p => p.Name == "App");

				InitBuildSyncEvent (project1);

				// Start the first target. The FileSync target will pause until signaled to continue.
				// Select the ShortOperations build queue.
				var context = new TargetEvaluationContext {
					BuilderQueue = BuilderQueue.ShortOperations
				};
				var build1 = project1.RunTarget (Util.GetMonitor (), "FileSync", sol.Configurations [0].Selector, context);

				// Wait for the build to reach the sync task
				await WaitForBuildSyncEvent (project1);

				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);

				// The build is now in progess. Run a new target.

				var build2 = project2.Build (Util.GetMonitor (), sol.Configurations [0].Selector);

				// Wait a bit. This should be enough to ensure the build has started.
				await Task.Delay (1000);

				// The RunTarget request should be queued, no new builder should be spawned
				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);

				// Continue building the first project
				SignalBuildToContinue (project1);

				// The first build should end now
				if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
					Assert.Fail ("Build did not end in time");

				// And now the second build should end
				if (await Task.WhenAny (build2, Task.Delay (timeoutMs)) != build2)
					Assert.Fail ("Build did not end in time");

				Assert.NotNull (build1.Result);
				Assert.AreEqual (0, build2.Result.ErrorCount);
				      
				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
			}
		}

		[Test]
		public async Task DisposingSolutionDisposesBuilder ()
		{
			// When a solution is disposed, all builders bound to the solution should be shut down

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
			FilePath projectFile;

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var project = (DotNetProject)sol.Items.FirstOrDefault (p => p.Name == "App");
				projectFile = project.FileName;
				await project.GetReferencedAssemblies (sol.Configurations [0].Selector);
				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
				Assert.AreEqual (1, await RemoteBuildEngineManager.CountActiveBuildersForProject (project.FileName));
			}
			Assert.AreEqual (0, await RemoteBuildEngineManager.CountActiveBuildersForProject (projectFile));
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);
		}

		[Test]
		public async Task DisposingProjectDisposesProjectBuilder ()
		{
			// When a project is disposed, all project builders should be shut down

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
			FilePath projectFile;

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var project = (DotNetProject)sol.Items.FirstOrDefault (p => p.Name == "App");
				projectFile = project.FileName;
				await project.GetReferencedAssemblies (sol.Configurations [0].Selector);
				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
				Assert.AreEqual (1, await RemoteBuildEngineManager.CountActiveBuildersForProject (project.FileName));

				sol.RootFolder.Items.Remove (project);
				project.Dispose ();

				Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
				Assert.AreEqual (1, RemoteBuildEngineManager.EnginesCount);
				Assert.AreEqual (0, await RemoteBuildEngineManager.CountActiveBuildersForProject (project.FileName));
			}
			Assert.AreEqual (0, await RemoteBuildEngineManager.CountActiveBuildersForProject (projectFile));
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);
			Assert.AreEqual (0, RemoteBuildEngineManager.EnginesCount);
		}

		/// <summary>
		/// Tests that a project builder that is marked as shutdown but currently running is not used for a new msbuild
		/// target. This prevents the project builder being disposed whilst the second msbuild target is being run.
		/// Tests that a null reference exception is not thrown by the project builder.
		/// </summary>
		[Test]
		public async Task ReloadProject_ProjectDisposedWhilstTargetRunning_AnotherTargetRun ()
		{
			// When a long operation is running and a new one is requested, a new builder is created

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

				var project1 = (Project)sol.Items.FirstOrDefault (p => p.Name == "SyncBuildProject");

				InitBuildSyncEvent (project1);

				// Start the build. Use RunTargetInternal to avoid the BindTask which will cancel the build on project dispose.
				var build1 = project1.RunTargetInternal (Util.GetMonitor (), "Build", sol.Configurations [0].Selector);

				// Wait for the build to reach the sync task
				await WaitForBuildSyncEvent (project1);

				// The build is now in progess. Simulate reloading the project.
				using (var project2 = (Project)await sol.RootFolder.ReloadItem (Util.GetMonitor (), project1)) {
					var builderTask = project2.GetProjectBuilder (CancellationToken.None, null, allowBusy: true);

					// Allow second build to finish and dispose its project builder. Have to do this here otherwise
					// GetProjectBuilder will hang waiting for a connection response back after it creates a new
					// project builder.
					SignalBuildToContinue (project1);
					if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
						Assert.Fail ("Build did not end in time");

					Assert.AreEqual (0, build1.Result.BuildResult.ErrorCount);

					using (var builder = await builderTask) {

						// Sanity check. We should only have one project builder and one build engine.
						Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
						Assert.AreEqual (1, RemoteBuildEngineManager.EnginesCount);
						Assert.AreEqual (1, await RemoteBuildEngineManager.CountActiveBuildersForProject (project2.FileName));

						var configs = project2.GetConfigurations (sol.Configurations [0].Selector, false);

						var build2 = await Task.Run (() => {
							// Previously this would throw a NullReferenceException since the builder has been disposed
							// and the engine is null.
							return builder.Run (
								configs,
								new StringWriter (),
								new MSBuildLogger (),
								MSBuildVerbosity.Quiet,
								null,
								new [] { "ResolveAssemblyReferences" },
								new string [0],
								new string [0],
								new System.Collections.Generic.Dictionary<string, string> (),
								CancellationToken.None);
						});

						Assert.AreEqual (0, build2.Errors.Length);
					}
				}
			}
		}

		[Test]
		public async Task ExtraBuilderAutoShutdown ()
		{
			// When an extra builder is created, it should be shut down when it is
			// not used anymore, after a delay

			var currentDelay = RemoteBuildEngineManager.EngineDisposalDelay;

			try {
				RemoteBuildEngineManager.EngineDisposalDelay = 400;

				await RemoteBuildEngineManager.RecycleAllBuilders ();
				Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

				FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
				using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

					var project1 = sol.Items.FirstOrDefault (p => p.Name == "SyncBuildProject");
					var project2 = sol.Items.FirstOrDefault (p => p.Name == "App");

					InitBuildSyncEvent (project1);

					// Start the build
					var build1 = project1.Build (Util.GetMonitor (), sol.Configurations [0].Selector);

					// Wait for the build to reach the sync task
					await WaitForBuildSyncEvent (project1);

					Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (0, await RemoteBuildEngineManager.CountActiveBuildersForProject (project2.FileName));

					// The build is now in progess. Start a new build

					var build2 = project2.Build (Util.GetMonitor (), sol.Configurations [0].Selector);
					if (await Task.WhenAny (build2, Task.Delay (timeoutMs)) != build2)
						Assert.Fail ("Build did not start");
					
					Assert.AreEqual (2, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (1, await RemoteBuildEngineManager.CountActiveBuildersForProject (project2.FileName));

					// Build engine disposal delay is set to 400ms, so it should be gone after waiting the following wait.

					await Task.Delay (500);

					// The second builder should now be gone

					Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (1, RemoteBuildEngineManager.EnginesCount);
					Assert.AreEqual (0, await RemoteBuildEngineManager.CountActiveBuildersForProject (project2.FileName));

					SignalBuildToContinue (project1);

					if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
						Assert.Fail ("Build did not end in time");
				}
			} finally {
				RemoteBuildEngineManager.EngineDisposalDelay = currentDelay;
			}
		}

		[Test]
		public async Task AtLeastOneBuilderPersolution ()
		{
			// There should always be at least one builder running per solution,
			// until the solution is explicitly disposed

			var currentDelay = RemoteBuildEngineManager.EngineDisposalDelay;

			try {
				RemoteBuildEngineManager.EngineDisposalDelay = 400;

				await RemoteBuildEngineManager.RecycleAllBuilders ();
				Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

				FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
				using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

					var project1 = sol.Items.FirstOrDefault (p => p.Name == "SyncBuildProject");
					var project2 = sol.Items.FirstOrDefault (p => p.Name == "App");

					InitBuildSyncEvent (project1);

					// Start the build
					var build1 = project1.Build (Util.GetMonitor (), sol.Configurations [0].Selector);

					// Wait for the build to reach the sync task
					await WaitForBuildSyncEvent (project1);

					Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (0, await RemoteBuildEngineManager.CountActiveBuildersForProject (project2.FileName));

					// The build is now in progess. Start a new build

					var build2 = project2.Build (Util.GetMonitor (), sol.Configurations [0].Selector);

					if (await Task.WhenAny (build2, Task.Delay (timeoutMs)) != build2)
						Assert.Fail ("Build did not start");

					Assert.AreEqual (2, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (2, RemoteBuildEngineManager.EnginesCount);
					Assert.AreEqual (1, await RemoteBuildEngineManager.CountActiveBuildersForProject (project1.FileName));
					Assert.AreEqual (1, await RemoteBuildEngineManager.CountActiveBuildersForProject (project2.FileName));

					SignalBuildToContinue (project1);

					if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
						Assert.Fail ("Build did not end in time");

					// Build engine disposal delay is set to 400ms, so unused
					// builders should go away after a 500ms wait.

					await Task.Delay (500);

					// There should be at least one builder left

					Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (1, RemoteBuildEngineManager.EnginesCount);

				}
			} finally {
				RemoteBuildEngineManager.EngineDisposalDelay = currentDelay;
			}

			// All builders should be gone now
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);
			Assert.AreEqual (0, RemoteBuildEngineManager.EnginesCount);
		}

		[Test]
		public async Task RecycleBuildersIsGraceful ()
		{
			// RecycleBuilders can be called in the middle of a build and that should not interrupt
			// the build. Builders will be shut down when the build is finished.

			var currentDelay = RemoteBuildEngineManager.EngineDisposalDelay;

			try {
				RemoteBuildEngineManager.EngineDisposalDelay = 400;

				await RemoteBuildEngineManager.RecycleAllBuilders ();
				Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

				FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
				using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

					var project1 = sol.Items.FirstOrDefault (p => p.Name == "SyncBuildProject");
					var project2 = sol.Items.FirstOrDefault (p => p.Name == "App");

					InitBuildSyncEvent (project1);

					// Start the build
					var build1 = project1.Build (Util.GetMonitor (), sol.Configurations [0].Selector);

					// Wait for the build to reach the sync task
					await WaitForBuildSyncEvent (project1);

					Assert.AreEqual (1, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (0, await RemoteBuildEngineManager.CountActiveBuildersForProject (project2.FileName));

					// The build is now in progess. Start a new build

					var build2 = project2.Build (Util.GetMonitor (), sol.Configurations [0].Selector);
					if (await Task.WhenAny (build2, Task.Delay (timeoutMs)) != build2)
						Assert.Fail ("Build did not start");

					// There should be one builder for each build

					Assert.AreEqual (2, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (2, RemoteBuildEngineManager.EnginesCount);

					// Ask for all active builders to be shut down

					await RemoteBuildEngineManager.RecycleAllBuilders ();

					// There is a build in progress, so there must still be one engine running

					Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (1, RemoteBuildEngineManager.EnginesCount);

					SignalBuildToContinue (project1);

					if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
						Assert.Fail ("Build did not end in time");
					
					Assert.AreEqual (0, build1.Result.ErrorCount);
					Assert.AreEqual (0, build2.Result.ErrorCount);

					// The builder that was running the build and was shutdown should be immediately stopped after build finishes
					Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);
					Assert.AreEqual (0, RemoteBuildEngineManager.EnginesCount);

				}
			} finally {
				RemoteBuildEngineManager.EngineDisposalDelay = currentDelay;
			}
		}

		[Test]
		public async Task ParallelBuilds ()
		{
			// Check that the project system can start two builds in parallel.

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			var currentSetting = Runtime.Preferences.ParallelBuild.Value;
			try {
				Runtime.Preferences.ParallelBuild.Set (true);

				FilePath solFile = Util.GetSampleProject ("builder-manager-tests", "builder-manager-tests.sln");
				using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {

					// DepMain depends on both Dep1 and Dep2.

					var demMain = sol.Items.FirstOrDefault (p => p.Name == "DepMain");
					var dep1 = sol.Items.FirstOrDefault (p => p.Name == "Dep1");
					var dep2 = sol.Items.FirstOrDefault (p => p.Name == "Dep2");

					InitBuildSyncEvent (dep1);
					InitBuildSyncEvent (dep2);

					// Start the build
					var build1 = demMain.Build (Util.GetMonitor (), sol.Configurations [0].Selector, true);

					// Wait for sync signal from projects Dep1 and Dep2, which will mean that
					// both projects started building in parallel

					var syncAll = Task.WhenAll (WaitForBuildSyncEvent (dep1), WaitForBuildSyncEvent (dep2));

					//NOTE: this has a longer timeout because otherwise it sometimes times out
					//maybe something to do with the fact it compiles tasks
					if (await Task.WhenAny (syncAll, Task.Delay (timeoutMs)) != syncAll)
						Assert.Fail ("Not all builds were started");

					// Finish the build
					SignalBuildToContinue (dep1);
					SignalBuildToContinue (dep2);

					if (await Task.WhenAny (build1, Task.Delay (timeoutMs)) != build1)
						Assert.Fail ("Build did not end in time");

					Assert.AreEqual (0, build1.Result.ErrorCount);
				}
			} finally {
				Runtime.Preferences.ParallelBuild.Set (currentSetting);
			}
		}
		void InitBuildSyncEvent (SolutionItem p)
		{
			var file = p.FileName.ParentDirectory.Combine ("sync-event");
			if (File.Exists (file))
				File.Delete (file);
			file = p.FileName.ParentDirectory.Combine ("continue-event");
			if (File.Exists (file))
				File.Delete (file);
		}

		async Task WaitForBuildSyncEvent (SolutionItem p)
		{
			var file = p.FileName.ParentDirectory.Combine ("sync-event");
			while (!File.Exists (file)) {
				await Task.Delay (50);
			}
		}

		void SignalBuildToContinue (SolutionItem p)
		{
			var file = p.FileName.ParentDirectory.Combine ("continue-event");
			File.WriteAllText (file, "");
		}
	}
}
