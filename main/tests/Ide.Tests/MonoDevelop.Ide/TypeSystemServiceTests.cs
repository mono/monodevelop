//
// TypeSystemServiceTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
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
using NUnit.Framework;
using System.Collections.Generic;
using UnitTests;
using Mono.Addins;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.IncrementalCaches;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SolutionSize;
using System.IO;
using System.Collections.Immutable;
using System.Text;

namespace MonoDevelop.Ide
{
	[TestFixture]
	[RequireService(typeof(RootWorkspace))]
	class TypeSystemServiceTests : IdeTestBase
	{
		class TrackTestProject : DotNetProject
		{
			readonly string type;
			protected override void OnGetTypeTags (HashSet<string> types)
			{
				types.Add (type);
			}

			protected override DotNetCompilerParameters OnCreateCompilationParameters (DotNetProjectConfiguration config, ConfigurationKind kind)
			{
				throw new NotImplementedException ();
			}

			protected override ClrVersion [] OnGetSupportedClrVersions ()
			{
				throw new NotImplementedException ();
			}

			public TrackTestProject (string language, string type) : base(language)
			{
				this.type = type;
				Initialize (this);
			}

		}

		[Test]
		public void TestOuptutTracking_ProjectType ()
		{
			IdeApp.TypeSystemService.AddOutputTrackingNode (new TypeSystemOutputTrackingNode { ProjectType = "TestProjectType" });

			Assert.IsFalse (IdeApp.TypeSystemService.IsOutputTrackedProject (new TrackTestProject ("C#", "Bar")));
			Assert.IsTrue (IdeApp.TypeSystemService.IsOutputTrackedProject (new TrackTestProject ("C#", "TestProjectType")));
		}

		[Test]
		public void TestOuptutTracking_LanguageName ()
		{
			IdeApp.TypeSystemService.AddOutputTrackingNode (new TypeSystemOutputTrackingNode { LanguageName = "IL" });

			Assert.IsTrue (IdeApp.TypeSystemService.IsOutputTrackedProject (new TrackTestProject ("IL", "Bar")));
		}

		[Test]
		public async Task ProjectReferencingOutputTrackedReference()
		{
			string solFile = Util.GetSampleProject("csharp-app-fsharp-lib", "csappfslib.sln");
			using (Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var fsharpLibrary = sol.Items.FirstOrDefault (pr => pr.Name == "fslib") as DotNetProject;
				Assert.IsTrue (IdeApp.TypeSystemService.IsOutputTrackedProject (fsharpLibrary));
			}
		}

		[Test]
		public async Task TestWorkspacePersistentStorageLocationService ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			using (Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var storageLocationService = (MonoDevelopPersistentStorageLocationService)ws.Services.GetService<IPersistentStorageLocationService> ();
					Assert.That (storageLocationService.TryGetStorageLocation (ws.CurrentSolution.Id), Is.Not.Null.Or.Empty);
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task TestWorkspacePersistentStorage ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			using (Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var storageLocationService = (MonoDevelopPersistentStorageLocationService)ws.Services.GetService<IPersistentStorageLocationService> ();
					var storageLocation = System.IO.Path.Combine (
						storageLocationService.TryGetStorageLocation (ws.CurrentSolution.Id),
						"sqlite3",
						"storage.ide");

					if (System.IO.File.Exists (storageLocation))
						System.IO.File.Delete (storageLocation);

					var solutionSizeTracker = (IIncrementalAnalyzerProvider)Composition.CompositionManager.Instance.GetExportedValue<ISolutionSizeTracker> ();

					// This will return the tracker, since it's a singleton.
					var analyzer = solutionSizeTracker.CreateIncrementalAnalyzer (ws);

					// We need this hack because we can't guess when the work coordinator will run the incremental analyzers.
					await analyzer.NewSolutionSnapshotAsync (ws.CurrentSolution, CancellationToken.None);

					foreach (var projectFile in sol.GetAllProjects ().SelectMany (x => x.Files.Where (file => file.BuildAction == BuildAction.Compile))) {
						var projectId = ws.GetProjectId (projectFile.Project);
						var docId = ws.GetDocumentId (projectId, projectFile.FilePath);
						var doc = ws.GetDocument (docId);
						if (!doc.SupportsSyntaxTree)
							continue;

						await Microsoft.CodeAnalysis.FindSymbols.SyntaxTreeIndex.PrecalculateAsync (doc, CancellationToken.None);
					}

					var fi = new System.IO.FileInfo (storageLocation);
					Assert.That (fi.Length, Is.GreaterThan (0));
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task TestWorkspacePersistentStorageImplementation ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			var streamName1 = "PersistentService_Solution_WriteReadDifferentInstances1";
			var streamName2 = "PersistentService_Solution_WriteReadDifferentInstances2";

			using (Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var persistentStorageService = ws.Services.GetService<IPersistentStorageService> ();
					Assert.That (persistentStorageService, Is.TypeOf (typeof (Microsoft.CodeAnalysis.SQLite.SQLitePersistentStorageService)));

					if (!(persistentStorageService is Microsoft.CodeAnalysis.SQLite.SQLitePersistentStorageService sqlitePersistentStorageService))
						return;

					var solutionSizeTracker = (IIncrementalAnalyzerProvider)Composition.CompositionManager.Instance.GetExportedValue<ISolutionSizeTracker> ();
					// This will return the tracker, since it's a singleton.
					var analyzer = solutionSizeTracker.CreateIncrementalAnalyzer (ws);

					// We need this hack because we can't guess when the work coordinator will run the incremental analyzers.
					await analyzer.NewSolutionSnapshotAsync (ws.CurrentSolution, CancellationToken.None);

					// Due to the nature of roslyn returning a new wrapper every time we request the storage, do a reflection check.
					const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
					var fieldInfo = sqlitePersistentStorageService.GetType ().BaseType.GetField ("_currentPersistentStorage", flags);

					using (var persistentStorage = sqlitePersistentStorageService.GetStorage (ws.CurrentSolution, checkBranchId: false)) {
						Assert.That (persistentStorage, Is.Not.TypeOf (typeof (NoOpPersistentStorage)));

						Assert.True (await persistentStorage.WriteStreamAsync (streamName1, EncodeString ("MyString")));
						Assert.True (await persistentStorage.WriteStreamAsync (streamName2, EncodeString ("MyString2")));
					}

					var initialFieldValue = fieldInfo.GetValue (sqlitePersistentStorageService);

					using (var persistentStorage = sqlitePersistentStorageService.GetStorage (ws.CurrentSolution, checkBranchId: false)) {
						Assert.That (persistentStorage, Is.Not.TypeOf (typeof (NoOpPersistentStorage)));
					}

					var secondFieldValue = fieldInfo.GetValue (sqlitePersistentStorageService);

					Assert.AreSame (initialFieldValue, secondFieldValue);

					using (var persistentStorage = sqlitePersistentStorageService.GetStorage (ws.CurrentSolution, checkBranchId: false)) {
						Assert.AreEqual ("MyString", ReadStringToEnd (await persistentStorage.ReadStreamAsync (streamName1)));
						Assert.AreEqual ("MyString2", ReadStringToEnd (await persistentStorage.ReadStreamAsync (streamName2)));
					}

				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}

			Stream EncodeString (string text)
			{
				var bytes = Encoding.UTF8.GetBytes (text);
				var stream = new MemoryStream (bytes);
				return stream;
			}

			string ReadStringToEnd (Stream stream)
			{
				using (stream) {
					var bytes = new byte [stream.Length];
					int count = 0;
					while (count < stream.Length) {
						count = stream.Read (bytes, count, (int)stream.Length - count);
					}

					return Encoding.UTF8.GetString (bytes);
				}
			}
		}

		[Test]
		public async Task TestStorageDataIsNotRecomputed ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			var checkSum1 = await RunTest (usedCache: false);
			var checkSum2 = await RunTest (usedCache: true);

			Assert.AreEqual (checkSum1, checkSum2);

			async Task<Microsoft.CodeAnalysis.Checksum> RunTest (bool usedCache)
			{
				var initial = Logger.GetLogger ();

				using (Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
				using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
					try {
						var persistentStorageService = ws.Services.GetService<IPersistentStorageService> ();
						Assert.That (persistentStorageService, Is.TypeOf (typeof (Microsoft.CodeAnalysis.SQLite.SQLitePersistentStorageService)));

						if (!(persistentStorageService is Microsoft.CodeAnalysis.SQLite.SQLitePersistentStorageService sqlitePersistentStorageService))
							return null;

						var solutionSizeTracker = (IIncrementalAnalyzerProvider)Composition.CompositionManager.Instance.GetExportedValue<ISolutionSizeTracker> ();
						// This will return the tracker, since it's a singleton.
						var analyzer = solutionSizeTracker.CreateIncrementalAnalyzer (ws);

						// We need this hack because we can't guess when the work coordinator will run the incremental analyzers.
						await analyzer.NewSolutionSnapshotAsync (ws.CurrentSolution, CancellationToken.None);

						var storageLogger = new StorageCheckingLogger ();
						var aggregateLogger = AggregateLogger.Create (initial, storageLogger);
						Logger.SetLogger (aggregateLogger);

						var provider = new SymbolTreeInfoIncrementalAnalyzerProvider ();
						var cacheService = (Microsoft.CodeAnalysis.FindSymbols.SymbolTree.ISymbolTreeInfoCacheService)provider.CreateService (ws.Services);

						var incrementalAnalyzer = provider.CreateIncrementalAnalyzer (ws);

						var project = sol.GetAllProjects ().Single ();
						var roslynProject = IdeServices.TypeSystemService.GetProject (project);

						await incrementalAnalyzer.AnalyzeProjectAsync (roslynProject, default, default, CancellationToken.None);

						Assert.That (storageLogger.QueriedCount, Is.GreaterThan (0));
						if (usedCache) {
							Assert.AreEqual (storageLogger.QueriedCount, storageLogger.UsedCacheCount);
						} else
							Assert.AreEqual (storageLogger.QueriedCount, storageLogger.CreatedCount);

						Assert.IsNotNull (await cacheService.TryGetSourceSymbolTreeInfoAsync (roslynProject, CancellationToken.None));

						return await Microsoft.CodeAnalysis.FindSymbols.SymbolTreeInfo.GetSourceSymbolsChecksumAsync (roslynProject, CancellationToken.None);
					} finally {
						Logger.SetLogger (initial);
						TypeSystemServiceTestExtensions.UnloadSolution (sol);
					}
				}
			}
		}

		class StorageCheckingLogger : ILogger
		{
			public int QueriedCount { get; private set; }
			public int CreatedCount { get; private set; }
			public int UsedCacheCount => QueriedCount - CreatedCount;

			public bool IsEnabled (FunctionId functionId) => true;

			public void Log (FunctionId functionId, LogMessage logMessage)
			{
				// nothing
			}

			public void LogBlockEnd (FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
			{
				// nothing
			}

			public void LogBlockStart (FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
			{
				if (functionId == FunctionId.SymbolTreeInfo_TryLoadOrCreate)
					QueriedCount++;
				else if (functionId == FunctionId.SymbolTreeInfo_Create)
					CreatedCount++;
			}
		}

		[Test]
		public async Task TestWorkspaceImmediatelyAvailable ()
		{
			await IdeServices.Workspace.Close (false, true, true);
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			var tcs = new TaskCompletionSource<bool> ();
			IdeServices.Workspace.SolutionLoaded += (s, e) => {
				var workspace = IdeApp.TypeSystemService.GetWorkspace (e.Solution);
				Assert.IsNotNull (workspace);
				Assert.AreNotSame (workspace, IdeApp.TypeSystemService.emptyWorkspace);
				workspace.Dispose ();
				tcs.SetResult (true);
			};
			try {
				if (!await IdeServices.Workspace.OpenWorkspaceItem (solFile))
					Assert.Fail ("Solution load failed");
				if (await Task.WhenAny (tcs.Task, Task.Delay (10000)) != tcs.Task)
					Assert.Fail ("Solution did not load");
			} finally {
				await IdeServices.Workspace.Close (false);
			}
		}

		[Test]
		public async Task TestSolutionLoadedOnce ()
		{
			// Fix for VSTS 603762 - LoadProject is called twice on solution load due to configuration change.

			MonoDevelopWorkspace workspace;
			bool reloaded = false;
			bool solutionLoaded = false;
			bool workspaceLoaded = false;

			await IdeServices.Workspace.Close (saveWorkspacePreferencies: false, closeProjectFiles: false, force: true);

			IdeServices.Workspace.SolutionLoaded += (s, e) => {
				workspace = IdeServices.TypeSystemService.GetWorkspace (e.Solution);
				workspace.WorkspaceChanged += (sender, ea) => {
					// If SolutionReloaded event is raised while opening the solution, we are doing something wrong
					if (ea.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionReloaded)
						reloaded = true;
				};
				workspace.WorkspaceLoaded += (sender, ev) => workspaceLoaded = true;
				solutionLoaded = true;
			};

			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			// Generate a user prefs file

			string prefsPath = Path.Combine (Path.GetDirectoryName (solFile), ".vs", "ConsoleProject", "xs");
			Directory.CreateDirectory (prefsPath);
			File.WriteAllText (Path.Combine (prefsPath, "UserPrefs.xml"), "<Properties><MonoDevelop.Ide.Workspace ActiveConfiguration='Release' /></Properties>");

			try {
				await IdeServices.Workspace.OpenWorkspaceItem (solFile);

				// Check that the user prefs file has been loaded
				Assert.AreEqual ("Release", IdeServices.Workspace.ActiveConfiguration.ToString ());

				// Wait for the roslyn workspace to be loaded
				int timeout = 100;
				while (!workspaceLoaded && --timeout > 0)
					await Task.Delay (100);

				if (timeout <= 0)
						Assert.Fail ("Workspace did not load");

			} finally {
				await IdeServices.Workspace.Close (false);
			}

			Assert.IsTrue (solutionLoaded);
			Assert.IsFalse (reloaded);
		}


		/// <summary>
		/// Tests the project's references are updated if the project is modified whilst it is being
		/// added to its parent solution.
		/// </summary>
		[Test]
		public async Task ProjectModifiedWhilstBeingAddedToSolution ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				try {
					var workspace = await TypeSystemServiceTestExtensions.LoadSolution (sol);

					var project = new ModifyReferencesDuringGetSourceFilesDotNetProject ();
					project.FileName = Path.Combine (sol.BaseDirectory, "ProjectModifiedWhilstBeingAddedToSolution.csproj");
					await project.SaveAsync (Util.GetMonitor ());
					var reference = ProjectReference.CreateCustomReference (ReferenceType.Package, "System.ComponentModel.Composition");

					var assemblyFileName = typeof (System.Xml.Linq.XName).Assembly.Location;
					project.AddExtraReference (new AssemblyReference (assemblyFileName));

					var projectLoadedTask = new TaskCompletionSource<bool> ();
					workspace.WorkspaceChanged += (sender, e) => {
						if (e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectReloaded) {
							projectLoadedTask.TrySetResult (true);
						}
					};

					sol.RootFolder.AddItem (project);

					if (await Task.WhenAny (projectLoadedTask.Task, Task.Delay (5000)) != projectLoadedTask.Task)
						Assert.Fail ("Timeout waiting for project to be reloaded by the type system service.");

					var projectId = workspace.GetProjectId (project);
					var projectInfo = workspace.CurrentSolution.GetProject (projectId);
					var metadataReference = projectInfo.MetadataReferences
						.OfType<Microsoft.CodeAnalysis.PortableExecutableReference> ()
						.FirstOrDefault (r => r.FilePath.EndsWith ("System.Xml.Linq.dll", StringComparison.Ordinal));

					Assert.IsNotNull (metadataReference, "System.Xml.Linq reference missing from type system information");
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		/// <summary>
		/// Tests that if the parser takes a long time to return a projection it is ignored instead
		/// of being used by the type system.
		/// </summary>
		[Test]
		public async Task CancelledParsedProjectionIsIgnored ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			var parsers = IdeApp.TypeSystemService.Parsers;
			try {
				var projectionParser = new TypeSystemParserNode ();
				projectionParser.BuildActions = new [] { "Compile" };
				projectionParser.MimeType = "text/csharp";

				var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
				var field = typeof (TypeExtensionNode).GetField ("type", flags);
				field.SetValue (projectionParser, typeof (ProjectionParser));

				var newParsers = new List<TypeSystemParserNode> ();
				newParsers.Add (projectionParser);
				IdeApp.TypeSystemService.Parsers = newParsers;

				using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
				using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
					try {
						var source1 = new CancellationTokenSource ();
						var source2 = new CancellationTokenSource ();

						var options = new ParseOptions {
							FileName = "first.xaml.cs"
						};
						var task1 = IdeApp.TypeSystemService.ParseProjection (options, "text/csharp", source1.Token);
						source1.Cancel ();

						options = new ParseOptions {
							FileName = "second.xaml.cs"
						};
						var task2 = IdeApp.TypeSystemService.ParseProjection (options, "text/csharp", source2.Token);

						var result1 = await task1;
						var result2 = await task2;

						Assert.IsNotNull (result2);
						Assert.IsNull (result1);
					} finally {
						TypeSystemServiceTestExtensions.UnloadSolution (sol);
					}
				}
			} finally {
				IdeApp.TypeSystemService.Parsers = parsers;
			}
		}

		class ProjectionParser : TypeSystemParser
		{
			public override Task<ParsedDocument> Parse (ParseOptions options, CancellationToken cancellationToken = default)
			{
				throw new NotImplementedException ();
			}

			public override bool CanGenerateProjection (string mimeType, string buildAction, string [] supportedLanguages)
			{
				return true;
			}

			public async override Task<ParsedDocumentProjection> GenerateParsedDocumentProjection (ParseOptions options, CancellationToken cancellationToken = default)
			{
				string fileName = Path.GetFileName (options.FileName);
				if (fileName == "first.xaml.cs") {
					await Task.Delay (2000).ConfigureAwait (false);
				}
				var projections = new List<Editor.Projection.Projection> ();
				var projection = new ParsedDocumentProjection (
					new DefaultParsedDocument (options.FileName),
					projections.AsReadOnly ()
				);
				return projection;
			}
		}

		class ModifyReferencesDuringGetSourceFilesDotNetProject : DotNetProject
		{
			public ModifyReferencesDuringGetSourceFilesDotNetProject ()
				: base ("C#")
			{
				// Set the TypeGuid to be a C# project so the default file extension will
				// be .csproj and not .mdproj
				TypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
				Initialize (this);
			}

			protected override DotNetCompilerParameters OnCreateCompilationParameters (DotNetProjectConfiguration config, ConfigurationKind kind)
			{
				throw new NotImplementedException ();
			}

			protected override ClrVersion [] OnGetSupportedClrVersions ()
			{
				throw new NotImplementedException ();
			}

			protected override async Task<ImmutableArray<ProjectFile>> OnGetSourceFiles (ProgressMonitor monitor, ConfigurationSelector configuration)
			{
				var files = await base.OnGetSourceFiles (monitor, configuration);
				if (!notifiedReferencesChanged) {
					await Task.Run (async () => {
						await Runtime.RunInMainThread (() => {
							notifiedReferencesChanged = true;
							NotifyModified ("References");
						});
					});
				}
				return files;
			}

			internal void AddExtraReference (AssemblyReference reference)
			{
				extraReferences.Add (reference);
			}

			List<AssemblyReference> extraReferences = new List<AssemblyReference> ();
			bool notifiedReferencesChanged;

			protected internal async override Task<List<AssemblyReference>> OnGetReferencedAssemblies (ConfigurationSelector configuration)
			{
				var refs = await base.OnGetReferencedAssemblies (configuration);
				if (notifiedReferencesChanged) {
					refs.AddRange (extraReferences);
				}
				return refs;
			}
		}
	}
}
