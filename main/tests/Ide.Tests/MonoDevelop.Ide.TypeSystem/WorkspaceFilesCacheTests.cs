//
// WorkspaceFilesCacheTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	[RequireService(typeof(RootWorkspace))]
	public class WorkspaceFilesCacheTests : IdeTestBase
	{
		static FilePath GetProjectCacheFile (FilePath cacheDir, Project project, string configuration, string framework = null)
		{
			if (string.IsNullOrEmpty (framework))
				return cacheDir.Combine (project.FileName.FileNameWithoutExtension + "-" + configuration + ".json");

			return cacheDir.Combine (project.FileName.FileNameWithoutExtension + "-" + configuration + "-" + framework + ".json");
		}

		async Task<ProjectCacheInfo> WaitForProjectInfoCacheToChange (
			Solution sol,
			Project p,
			ProjectCacheInfo oldCacheInfo)
		{
			const int timeout = 10000; // ms
			int howLong = 0;
			const int interval = 200; // ms

			while (true) {
				var cacheInfo = GetProjectCacheInfo (sol, p);
				if (cacheInfo != null) {
					if (!oldCacheInfo.Equals (cacheInfo)) {
						return cacheInfo;
					}
				}

				if (howLong >= timeout) {
					Assert.Fail ("Timed out waiting for project info cache file to be updated");
				}

				await Task.Delay (interval);
				howLong += interval;
			}
		}

		static ProjectCacheInfo GetProjectCacheInfo (Solution sol, Project p, string framework = null)
		{
			var ws = IdeServices.TypeSystemService.GetWorkspace (sol);
			var cache = new WorkspaceFilesCache ();
			cache.Load (sol);

			if (cache.TryGetCachedItems (p, ws.MetadataReferenceManager, ws.ProjectMap, framework, out ProjectCacheInfo cacheInfo))
				return cacheInfo;
			return null;
		}

		[Test]
		public async Task TestWorkspaceFilesCacheCreation ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			var sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			var p = sol.FindProjectByName ("ConsoleProject") as DotNetProject;

			// Check cache created for active configuration.
			var cacheDirectory = sol.GetPreferencesDirectory ().Combine ("project-cache");
			var projectCacheFile = GetProjectCacheFile (cacheDirectory, p, "Debug");
			Assert.IsTrue (File.Exists (projectCacheFile));

			var cacheInfo = GetProjectCacheInfo (sol, p);
			Assert.IsNotNull (cacheInfo);

			// Check cached source files.
			foreach (var projectFile in p.Files) {
				var matchedProjectFile = cacheInfo.SourceFiles.FirstOrDefault (f => projectFile.FilePath == f.FilePath);
				Assert.IsNotNull (matchedProjectFile, "File not found: " + projectFile.FilePath);
			}
			Assert.AreEqual (p.Files.Count, cacheInfo.SourceFiles.Length);

			// Check cached references.
			foreach (var reference in p.References) {
				var matchedReference = cacheInfo.References.FirstOrDefault (r => r.FilePath.FileNameWithoutExtension == reference.Include);
				Assert.IsNotNull (matchedReference, "Reference not found: " + reference.Include);
			}
		}

		[Test]
		public async Task TestWorkspaceFilesCacheCreation_MultiTargetFramework ()
		{
			FilePath solFile = Util.GetSampleProject ("multi-target-netframework", "multi-target.sln");

			CreateNuGetConfigFile (solFile.ParentDirectory);
			RunMSBuild ($"/t:Restore /p:RestoreDisableParallel=true \"{solFile}\"");

			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			var sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			var p = sol.GetAllProjects ().Single () as DotNetProject;

			// Check cache created for active configuration.
			var cacheDirectory = sol.GetPreferencesDirectory ().Combine ("project-cache");
			var netStandardProjectCacheFile = GetProjectCacheFile (cacheDirectory, p, "Debug", "netstandard1.0");
			Assert.IsTrue (File.Exists (netStandardProjectCacheFile));

			var netFrameworkProjectCacheFile = GetProjectCacheFile (cacheDirectory, p, "Debug", "net472");
			Assert.IsTrue (File.Exists (netFrameworkProjectCacheFile));

			var netFrameworkCacheInfo = GetProjectCacheInfo (sol, p, "net472");
			Assert.IsNotNull (netFrameworkCacheInfo);

			var netStandardCacheInfo = GetProjectCacheInfo (sol, p, "netstandard1.0");
			Assert.IsNotNull (netStandardCacheInfo);

			// Check cached references.
			Assert.IsFalse (netFrameworkCacheInfo.References.Any (d => d.FilePath.FileName == "System.Collections.dll"));
			Assert.IsTrue (netFrameworkCacheInfo.References.Any (d => d.FilePath.FileName == "mscorlib.dll"));
			Assert.IsTrue (netStandardCacheInfo.References.Any (d => d.FilePath.FileName == "System.Collections.dll"));
			Assert.IsFalse (netStandardCacheInfo.References.Any (d => d.FilePath.FileName == "mscorlib.dll"));

			// Check cached source files.
			Assert.IsFalse (netFrameworkCacheInfo.SourceFiles.Any (d => d.FilePath.FileName == "MyClass-netstandard.cs"));
			Assert.IsTrue (netFrameworkCacheInfo.SourceFiles.Any (d => d.FilePath.FileName == "MyClass-netframework.cs"));
			Assert.IsTrue (netStandardCacheInfo.SourceFiles.Any (d => d.FilePath.FileName == "MyClass-netstandard.cs"));
			Assert.IsFalse (netStandardCacheInfo.SourceFiles.Any (d => d.FilePath.FileName == "MyClass-netframework.cs"));
		}

		[Test]
		public async Task TestWorkspaceFilesCacheUpdated_ProjectReferencedAdded ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			var sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			var p = sol.FindProjectByName ("ConsoleProject") as DotNetProject;
			var cacheDirectory = sol.GetPreferencesDirectory ().Combine ("project-cache");
			var projectCacheFile = GetProjectCacheFile (cacheDirectory, p, "Debug");
			Assert.IsTrue (File.Exists (projectCacheFile));

			var cacheInfo = GetProjectCacheInfo (sol, p);
			Assert.IsNotNull (cacheInfo);

			Assert.IsFalse (p.References.Any (r => StringComparer.OrdinalIgnoreCase.Equals (r.Include, "System.Net.Http")));

			var systemNetHttpReference = ProjectReference.CreateCustomReference (ReferenceType.Package, "System.Net.Http");
			p.References.Add (systemNetHttpReference);
			await p.SaveAsync (Util.GetMonitor ());

			// Check for updated cache file.
			var updatedCacheInfo = await WaitForProjectInfoCacheToChange (sol, p, cacheInfo);

			// Check cached references.
			var matchedReference = updatedCacheInfo.References.FirstOrDefault (r => r.FilePath.FileNameWithoutExtension == systemNetHttpReference.Include);
			Assert.IsNotNull (matchedReference, "System.Net.Http reference not found");
		}

		[Test]
		public async Task TestWorkspaceFilesCacheUpdated_NetStandardProject_CSharpFileAddedExternally ()
		{
			string solFile = Util.GetSampleProject ("netstandard-sdk", "netstandard-sdk.sln");

			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			var sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			var p = sol.FindProjectByName ("netstandard-sdk") as DotNetProject;

			try {
				// Check cache created for active configuration.
				var cacheDirectory = sol.GetPreferencesDirectory ().Combine ("project-cache");
				var projectCacheFile = GetProjectCacheFile (cacheDirectory, p, "Debug");
				Assert.IsTrue (File.Exists (projectCacheFile));

				var cacheInfo = GetProjectCacheInfo (sol, p);
				Assert.IsNotNull (cacheInfo);

				Assert.IsFalse (p.Files.Any (f => f.FilePath.FileName == "NewCSharpFile.cs"));

				await FileWatcherService.Add (sol);

				var newCSharpFileName = p.BaseDirectory.Combine ("NewCSharpFile.cs");
				File.WriteAllText (newCSharpFileName, "class NewCSharpFile {}");

				var updatedCacheInfo = await WaitForProjectInfoCacheToChange (sol, p, cacheInfo);

				// Check cached source files.
				var matchedProjectFile = updatedCacheInfo.SourceFiles.FirstOrDefault (f => f.FilePath == newCSharpFileName);
				Assert.IsNotNull (matchedProjectFile, "File not found: " + newCSharpFileName);

				// Check type system has new C# file.
				var ws = IdeServices.TypeSystemService.GetWorkspace (sol);
				var projectId = ws.GetProjectId (p);
				var docId = ws.GetDocumentId (projectId, newCSharpFileName);
				var doc = ws.GetDocument (docId);
				Assert.AreEqual (newCSharpFileName.ToString (), doc.FilePath);
			} finally {
				await FileWatcherService.Remove (sol);
			}
		}

		/// <summary>
		/// Tests that the cache is used before MSBuild on loading the solution with the type system service.
		/// </summary>
		[Test]
		public async Task TestWorkspaceFilesCache_UsedOnLoad ()
		{
			// First we create the cache by loading the solution.
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			var sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			var p = sol.FindProjectByName ("ConsoleProject") as DotNetProject;
			// Check cache created for active configuration.
			var cacheInfo = GetProjectCacheInfo (sol, p);
			Assert.IsNotNull (cacheInfo);

			await IdeServices.Workspace.Close (false);

			// Load the solution again.
			var fn = new CustomItemNode<DelayGetReferencesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			p = sol.FindProjectByName ("ConsoleProject") as DotNetProject;
			try {
				// Check cache created for active configuration.
				cacheInfo = GetProjectCacheInfo (sol, p);
				Assert.IsNotNull (cacheInfo);

				var ws = IdeServices.TypeSystemService.GetWorkspace (sol);

				var projectId = ws.GetProjectId (p);
				foreach (var file in p.Files) {
					var docId = ws.GetDocumentId (projectId, file.FilePath);
					var doc = ws.GetDocument (docId);
					Assert.IsNotNull (doc);
				}

				foreach (var reference in p.References) {
					var analysisProject = ws.CurrentSolution.GetProject (projectId);
					var matchedReference = analysisProject
							.MetadataReferences
							.OfType<MonoDevelopMetadataReference.Snapshot> ()
							.FirstOrDefault (r => ((FilePath)r.FilePath).FileNameWithoutExtension == reference.Include);
					Assert.IsNotNull (matchedReference, "Reference not found: " + reference.Include);
				}

				var ext = p.GetFlavor<DelayGetReferencesProjectExtension> ();
				ext.TaskCompletionSource.TrySetResult (true);
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
				TypeSystemServiceTestExtensions.UnloadSolution (sol);
			}
		}

		[Test]
		public async Task TestWorkspaceFilesCache_MultiTargetProjectReference_UsedOnLoad ()
		{
			// First we create the cache by loading the solution.
			string solFile = Util.GetSampleProject ("multi-target-project-ref", "multi-target.sln");

			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			var sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			var project = sol.FindProjectByName ("multi-target-ref") as DotNetProject;

			// Check cache created for active configuration.
			var cacheInfo = GetProjectCacheInfo (sol, project, "netstandard1.4");
			Assert.IsNotNull (cacheInfo);
			cacheInfo = GetProjectCacheInfo (sol, project, "net472");
			Assert.IsNotNull (cacheInfo);

			await IdeServices.Workspace.Close (false);

			// Load the solution again.
			var fn = new CustomItemNode<DelayGetReferencesProjectExtension> ();
			WorkspaceObject.RegisterCustomExtension (fn);

			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			project = sol.FindProjectByName ("multi-target-ref") as DotNetProject;
			var referencedProject = sol.FindProjectByName ("multi-target") as DotNetProject;
			try {
				var ws = IdeServices.TypeSystemService.GetWorkspace (sol);

				var projectIds = ws.CurrentSolution.ProjectIds.ToArray ();
				var projects = ws.CurrentSolution.Projects.ToArray ();

				var netframeworkProject = projects.FirstOrDefault (p => p.Name == "multi-target (net471)");
				var netstandardProject = projects.FirstOrDefault (p => p.Name == "multi-target (netstandard1.0)");
				var netframeworkProjectRef = projects.FirstOrDefault (p => p.Name == "multi-target-ref (net472)");
				var netstandardProjectRef = projects.FirstOrDefault (p => p.Name == "multi-target-ref (netstandard1.4)");

				Assert.AreEqual (4, projectIds.Length);
				Assert.AreEqual (4, projects.Length);

				// Check project references.
				var projectReferences = netstandardProjectRef.ProjectReferences.ToArray ();

				Assert.AreEqual (1, projectReferences.Length);
				Assert.AreEqual (netstandardProject.Id, projectReferences [0].ProjectId);

				projectReferences = netframeworkProjectRef.ProjectReferences.ToArray ();

				Assert.AreEqual (1, projectReferences.Length);
				Assert.AreEqual (netframeworkProject.Id, projectReferences [0].ProjectId);

				var ext = project.GetFlavor<DelayGetReferencesProjectExtension> ();
				ext.TaskCompletionSource.TrySetResult (true);
			} finally {
				WorkspaceObject.UnregisterCustomExtension (fn);
				TypeSystemServiceTestExtensions.UnloadSolution (sol);
			}
		}

		/// <summary>
		/// Tests that the cache, when it was available on initial load, is updated later on when the cache
		/// is out of date compared with the project information returned from MSBuild. For example, the
		/// .csproj file was edited outside the IDE before it was loaded.
		/// </summary>
		[Test]
		public async Task TestWorkspaceFilesCache_CacheOutOfDate_CacheUpdatedFromMSBuold ()
		{
			// First we create the cache by loading the solution.
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			FilePath projectFileName = null;
			ProjectCacheInfo cacheInfo = null;

			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			var sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			var p = sol.FindProjectByName ("ConsoleProject") as DotNetProject;
			projectFileName = p.FileName;
			cacheInfo = GetProjectCacheInfo (sol, p);
			Assert.IsNotNull (cacheInfo);
			Assert.IsFalse (cacheInfo.References.Any (r => StringComparer.OrdinalIgnoreCase.Equals (r.FilePath.FileNameWithoutExtension, "System.Net")));

			await IdeServices.Workspace.Close ();

			// Project file changed so cache is now invalid.
			var updatedProjectFile = projectFileName + ".reference-added";
			File.Copy (updatedProjectFile, projectFileName, overwrite: true);

			// Reload project and check cache is updated.
			await IdeServices.Workspace.OpenWorkspaceItem (solFile);
			await IdeServices.TypeSystemService.ProcessPendingLoadOperations ();
			sol = IdeServices.Workspace.GetAllItems<Solution> ().First ();
			IdeServices.Workspace.ActiveConfigurationId = sol.DefaultConfigurationId;

			p = sol.FindProjectByName ("ConsoleProject") as DotNetProject;
			var updatedCacheInfo = await WaitForProjectInfoCacheToChange (sol, p, cacheInfo);
			Assert.IsTrue (updatedCacheInfo.References.Any (r => r.FilePath.FileNameWithoutExtension == "System.Net"));
			Assert.IsTrue (updatedCacheInfo.References.Any (r => r.FilePath.FileNameWithoutExtension == "System.Xml.Linq"));
			Assert.IsTrue (p.References.Any (r => r.Include == "System.Net"));
			Assert.IsTrue (p.References.Any (r => r.Include == "System.Xml.Linq"));

			var ws = IdeServices.TypeSystemService.GetWorkspace (sol);
			var projectId = ws.GetProjectId (p);
			foreach (var reference in p.References) {
				var analysisProject = ws.CurrentSolution.GetProject (projectId);
				var matchedReference = analysisProject
						.MetadataReferences
						.OfType<MonoDevelopMetadataReference.Snapshot> ()
						.FirstOrDefault (r => ((FilePath)r.FilePath).FileNameWithoutExtension == reference.Include);
				Assert.IsNotNull (matchedReference, "Reference not found: " + reference.Include);
			}
		}

		/// <summary>
		/// Clear all other package sources and just use the main NuGet package source when
		/// restoring the packages for the project tests.
		/// </summary>
		static void CreateNuGetConfigFile (FilePath directory)
		{
			var fileName = directory.Combine ("NuGet.Config");

			string xml =
				"<configuration>\r\n" +
				"  <packageSources>\r\n" +
				"    <clear />\r\n" +
				"    <add key=\"NuGet v3 Official\" value=\"https://api.nuget.org/v3/index.json\" />\r\n" +
				"  </packageSources>\r\n" +
				"</configuration>";

			File.WriteAllText (fileName, xml);
		}

		void RunMSBuild (string arguments)
		{
			var process = Process.Start ("msbuild", arguments);
			Assert.IsTrue (process.WaitForExit (240000), "Timed out waiting for MSBuild.");
			Assert.AreEqual (0, process.ExitCode, $"msbuild {arguments} failed");
		}

		class CustomItemNode<T> : SolutionItemExtensionNode where T : new()
		{
			public override object CreateInstance ()
			{
				return new T ();
			}
		}

		class DelayGetReferencesProjectExtension : DotNetProjectExtension
		{
			public TaskCompletionSource<bool> TaskCompletionSource = new TaskCompletionSource<bool> ();

			protected internal override async Task<List<AssemblyReference>> OnGetReferencedAssemblies (ConfigurationSelector configuration)
			{
				await TaskCompletionSource.Task;
				return await base.OnGetReferencedAssemblies (configuration);
			}
		}
	}
}
