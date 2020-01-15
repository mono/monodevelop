//
// TypeSystemServiceTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class TypeSystemServiceTests : IdeTestBase
	{
		[Test]
		public async Task SymlinkedFilesHaveProperdocumentRegistration ()
		{
			if (Platform.IsWindows)
				Assert.Ignore ("Symlinks not supported on Windows");

			FilePath solFile = Util.GetSampleProjectPath ("symlinked-source-file", "test-symlinked-file", "test-symlinked-file.sln");

			var solutionDirectory = Path.GetDirectoryName (solFile);
			var dataFile = Path.Combine (solutionDirectory, "SymlinkedFileData.txt");
			var data = File.ReadAllLines (dataFile);
			var symlinkFileName = Path.Combine (solutionDirectory, data [0]);
			var symlinkFileSource = Path.GetFullPath (Path.Combine (solutionDirectory, data [1]));

			File.Delete (symlinkFileName);
			Process.Start (new ProcessStartInfo ("ln", $"-s '{symlinkFileSource}' '{symlinkFileName}'") {
				UseShellExecute = false,
			}).WaitForExit ();

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var project = sol.GetAllProjects ().Single ();

					foreach (var file in project.Files) {
						Assert.IsNotNull (IdeApp.TypeSystemService.GetDocumentId (project, file.FilePath.ResolveLinks ()));
						if (file.FilePath.FileName.EndsWith ("SymlinkedFile.cs", StringComparison.Ordinal))
							Assert.IsNull (IdeServices.TypeSystemService.GetDocumentId (project, file.FilePath));
					}
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task MultiTargetFramework ()
		{
			FilePath solFile = Util.GetSampleProject ("multi-target-netframework", "multi-target.sln");

			CreateNuGetConfigFile (solFile.ParentDirectory);
			Util.RunMSBuild ($"/t:Restore /p:RestoreDisableParallel=true \"{solFile}\"");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var project = sol.GetAllProjects ().Single ();

					var projectIds = ws.CurrentSolution.ProjectIds.ToArray ();
					var projects = ws.CurrentSolution.Projects.ToArray ();

					var netframeworkProject = projects.FirstOrDefault (p => p.Name == "multi-target (net472)");
					var netstandardProject = projects.FirstOrDefault (p => p.Name == "multi-target (netstandard1.0)");

					// Should be two projects - one for each target framework.
					Assert.AreEqual (2, projectIds.Length);
					Assert.AreEqual (2, projects.Length);

					Assert.IsNotNull (netframeworkProject);
					Assert.IsNotNull (netstandardProject);

					// Check references.
					var mscorlibNetFramework = netframeworkProject.MetadataReferences
						.OfType<Microsoft.CodeAnalysis.PortableExecutableReference > ()
						.FirstOrDefault (r => Path.GetFileName (r.FilePath) == "mscorlib.dll");

					var systemCollectionsNetFramework = netframeworkProject.MetadataReferences
						.OfType<Microsoft.CodeAnalysis.PortableExecutableReference> ()
						.FirstOrDefault (r => Path.GetFileName (r.FilePath) == "System.Collections.dll");

					var mscorlibNetStandard = netstandardProject.MetadataReferences
						.OfType<Microsoft.CodeAnalysis.PortableExecutableReference> ()
						.FirstOrDefault (r => Path.GetFileName (r.FilePath) == "mscorlib.dll");

					var systemCollectionsNetStandard = netstandardProject.MetadataReferences
						.OfType<Microsoft.CodeAnalysis.PortableExecutableReference> ()
						.FirstOrDefault (r => Path.GetFileName (r.FilePath) == "System.Collections.dll");

					Assert.AreNotEqual (netframeworkProject.MetadataReferences.Count, netstandardProject.MetadataReferences.Count);

					Assert.IsNotNull (mscorlibNetFramework);
					Assert.IsNull (mscorlibNetStandard);

					Assert.IsNull (systemCollectionsNetFramework);
					Assert.IsNotNull (systemCollectionsNetStandard);

					// Check source files are correct for each framework.
					Assert.IsFalse (netframeworkProject.Documents.Any (d => d.Name == "MyClass-netstandard.cs"));
					Assert.IsTrue (netframeworkProject.Documents.Any (d => d.Name == "MyClass-netframework.cs"));
					Assert.IsTrue (netstandardProject.Documents.Any (d => d.Name == "MyClass-netstandard.cs"));
					Assert.IsFalse (netstandardProject.Documents.Any (d => d.Name == "MyClass-netframework.cs"));

					// Check compiler parameter information
					Assert.That (netframeworkProject.ParseOptions.PreprocessorSymbolNames, Contains.Item ("NET472"));
					Assert.That (netstandardProject.ParseOptions.PreprocessorSymbolNames, Contains.Item ("NETSTANDARD1_0"));

					Assert.That (netframeworkProject.CompilationOptions.SpecificDiagnosticOptions.Keys, Contains.Item ("NET12345"));
					Assert.IsFalse (netframeworkProject.CompilationOptions.SpecificDiagnosticOptions.Keys.Contains ("STA4433"));
					Assert.That (netstandardProject.CompilationOptions.SpecificDiagnosticOptions.Keys, Contains.Item ("STA4433"));
					Assert.IsFalse (netstandardProject.CompilationOptions.SpecificDiagnosticOptions.Keys.Contains ("NET12345"));

					// Ensure that facade assemblies are not added to the .NET Standard project. This would happen
					// if the .NET Framework was the first target framework listed in TargetFrameworks. The Project
					// would see a .NET Standard assembly was referenced and add the facade assemblies.
					var facadeFound = netstandardProject.MetadataReferences
						.OfType<Microsoft.CodeAnalysis.PortableExecutableReference> ()
						.Where (r => r.FilePath.IndexOf ("Facades", StringComparison.OrdinalIgnoreCase) >= 0)
						.Select (r => r.FilePath)
						.FirstOrDefault ();

					Assert.IsNull (facadeFound);
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task MultiTargetFramework_ProjectReferences ()
		{
			FilePath solFile = Util.GetSampleProject ("multi-target-project-ref", "multi-target.sln");

			CreateNuGetConfigFile (solFile.ParentDirectory);
			Util.RunMSBuild ($"/t:Restore /p:RestoreDisableParallel=true \"{solFile}\"");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var projectIds = ws.CurrentSolution.ProjectIds.ToArray ();
					var projects = ws.CurrentSolution.Projects.ToArray ();

					var netframeworkProject = projects.FirstOrDefault (p => p.Name == "multi-target (net471)");
					var netstandardProject = projects.FirstOrDefault (p => p.Name == "multi-target (netstandard1.0)");
					var netframeworkProjectRef = projects.FirstOrDefault (p => p.Name == "multi-target-ref (net472)");
					var netstandardProjectRef = projects.FirstOrDefault (p => p.Name == "multi-target-ref (netstandard1.4)");

					// Should be four projects - one for each target framework.
					Assert.AreEqual (4, projectIds.Length);
					Assert.AreEqual (4, projects.Length);

					Assert.IsNotNull (netframeworkProject);
					Assert.IsNotNull (netstandardProject);
					Assert.IsNotNull (netframeworkProjectRef);
					Assert.IsNotNull (netstandardProjectRef);

					// Check project references.
					var projectReferences = netstandardProjectRef.ProjectReferences.ToArray ();

					Assert.AreEqual (1, projectReferences.Length);
					Assert.AreEqual (netstandardProject.Id, projectReferences [0].ProjectId);

					projectReferences = netframeworkProjectRef.ProjectReferences.ToArray ();

					Assert.AreEqual (1, projectReferences.Length);
					Assert.AreEqual (netframeworkProject.Id, projectReferences [0].ProjectId);

				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task MultiTargetFramework_RemoveProject ()
		{
			FilePath solFile = Util.GetSampleProject ("multi-target-netframework", "multi-target.sln");

			CreateNuGetConfigFile (solFile.ParentDirectory);
			Util.RunMSBuild ($"/t:Restore /p:RestoreDisableParallel=true \"{solFile}\"");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var project = sol.GetAllProjects ().Single ();

					var projectIds = ws.CurrentSolution.ProjectIds.ToArray ();
					var projects = ws.CurrentSolution.Projects.ToArray ();

					var netframeworkProject = projects.FirstOrDefault (p => p.Name == "multi-target (net472)");
					var netstandardProject = projects.FirstOrDefault (p => p.Name == "multi-target (netstandard1.0)");

					// Should be two projects - one for each target framework.
					Assert.AreEqual (2, projectIds.Length);
					Assert.AreEqual (2, projects.Length);

					Assert.IsNotNull (netframeworkProject);
					Assert.IsNotNull (netstandardProject);

					sol.RootFolder.Items.Remove (project);
					await sol.SaveAsync (Util.GetMonitor ());

					projectIds = ws.CurrentSolution.ProjectIds.ToArray ();
					projects = ws.CurrentSolution.Projects.ToArray ();

					Assert.AreEqual (0, projectIds.Length);
					Assert.AreEqual (0, projects.Length);
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task MultiTargetFramework_ReloadProject_TargetFrameworksChanged ()
		{
			FilePath solFile = Util.GetSampleProject ("multi-target", "multi-target.sln");

			CreateNuGetConfigFile (solFile.ParentDirectory);
			Util.RunMSBuild ($"/t:Restore /p:RestoreDisableParallel=true \"{solFile}\"");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var project = sol.GetAllProjects ().Single ();

					var projectIds = ws.CurrentSolution.ProjectIds.ToArray ();
					var projects = ws.CurrentSolution.Projects.ToArray ();

					var netcoreProject = projects.FirstOrDefault (p => p.Name == "multi-target (netcoreapp1.1)");
					var netstandardProject = projects.FirstOrDefault (p => p.Name == "multi-target (netstandard1.0)");

					// Should be two projects - one for each target framework.
					Assert.AreEqual (2, projectIds.Length);
					Assert.AreEqual (2, projects.Length);

					Assert.IsNotNull (netcoreProject);
					Assert.IsNotNull (netstandardProject);

					var updatedProjectFileName = project.FileName.ChangeName ("multi-target-reload");

					string xml = File.ReadAllText (updatedProjectFileName);

					// Handle sharing violations when writing the project file by retrying a few times.
					int maxRetries = 10;
					for (int i = 0; i < maxRetries; ++i) {
						try {
							File.WriteAllText (project.FileName, xml);
							break;
						} catch (Exception ex) {
							if (i + 1 >= maxRetries) {
								ShowFileLockInformation (project.FileName);
								throw;
							} else {
								Console.WriteLine ("MultiTargetFramework_ReloadProject_TargetFrameworksChanged File.WriteAllText error: {0}", ex.Message);
								await Task.Delay (500);
							}
						}
					}

					await sol.RootFolder.ReloadItem (Util.GetMonitor (), project);

					// Try a few times since the type system needs time to reload
					const int timeout = 10000; // ms
					int howLong = 0;
					const int interval = 200; // ms

					while (true) {
						var newProjectIds = ws.CurrentSolution.ProjectIds.ToArray ();
						projects = ws.CurrentSolution.Projects.ToArray ();

						netcoreProject = projects.FirstOrDefault (p => p.Name == "multi-target (netcoreapp1.2)");
						netstandardProject = projects.FirstOrDefault (p => p.Name == "multi-target (netstandard1.3)");
						if (netcoreProject != null && netstandardProject != null) {
							Assert.AreEqual (2, newProjectIds.Length);
							Assert.AreEqual (2, projects.Length);
							return;
						}

						if (howLong >= timeout) {
							Assert.Fail ("Timed out waiting for type system information to be updated.");
						}

						await Task.Delay (interval);
						howLong += interval;
					}
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		void ShowFileLockInformation (FilePath fileName)
		{
			Console.WriteLine (
				"ShowFileLockInformation CurrentProcessId={0} lsof output:",
				Process.GetCurrentProcess ().Id);

			using (var process = new Process ()) {
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.CreateNoWindow = true;

				process.StartInfo.FileName = "lsof";
				//process.StartInfo.Arguments = " \"" + fileName.FileName + "\"";

				process.OutputDataReceived += (s, e) => Console.WriteLine (e.Data);

				process.Start ();
				process.BeginOutputReadLine ();
				process.WaitForExit ();
			}
		}

		[Test]
		public async Task CSharpFile_BuildActionNone_FileNotUsed ()
		{
			FilePath solFile = Util.GetSampleProject ("build-action-none", "build-action-none.sln");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var projects = ws.CurrentSolution.Projects.ToArray ();

					var project = projects.Single ();

					// Check that build action none .cs file is not used by the type system.
					Assert.IsFalse (project.Documents.Any (d => d.Name == "DoNotCompile.cs"));
					Assert.IsTrue (project.Documents.Any (d => d.Name == "Program.cs"));
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task ProjectReference ()
		{
			FilePath solFile = Util.GetSampleProject ("netstandard-project", "NetStandardTest.sln");

			CreateNuGetConfigFile (solFile.ParentDirectory);
			Util.RunMSBuild ($"/t:Restore /p:RestoreDisableParallel=true \"{solFile}\"");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var projects = ws.CurrentSolution.Projects.ToArray ();

					var netframeworkProject = projects.FirstOrDefault (p => p.Name == "NetStandardTest");
					var netstandardProject = projects.FirstOrDefault (p => p.Name == "Lib");
					var projectReferences = netframeworkProject.ProjectReferences.ToArray ();

					Assert.AreEqual (1, projectReferences.Length);
					Assert.AreEqual (netstandardProject.Id, projectReferences [0].ProjectId);
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task AdditionalFiles_EditorConfigFiles ()
		{
			FilePath solFile = Util.GetSampleProject ("additional-files", "additional-files.sln");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var project = sol.GetAllProjects ().Single ();

					var projectInfo = ws.CurrentSolution.Projects.Single ();
					var additionalDocs = projectInfo.AdditionalDocuments.ToArray ();
					var editorConfigDocs = projectInfo.AnalyzerConfigDocuments.ToArray ();

					FilePath expectedAdditionalFileName = project.BaseDirectory.Combine ("additional-file.txt");
					FilePath expectedEditorConfigFileName = solFile.ParentDirectory.Combine (".editorconfig");

					Assert.IsTrue (additionalDocs.Any (d => d.FilePath == expectedAdditionalFileName));
					Assert.IsTrue (editorConfigDocs.Any (d => d.FilePath == expectedEditorConfigFileName));

				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task EditorConfigFile_ModifiedExternally ()
		{
			FilePath solFile = Util.GetSampleProject ("additional-files", "additional-files.sln");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var project = sol.GetAllProjects ().Single ();

					FilePath editorConfigFileName = solFile.ParentDirectory.Combine (".editorconfig");
					var projectInfo = ws.CurrentSolution.Projects.Single ();
					var editorConfigDoc = projectInfo.AnalyzerConfigDocuments.Single (d => d.FilePath == editorConfigFileName);

					bool analyzerConfigDocumentChanged = false;
					ws.WorkspaceChanged += (sender, e) => {
						if (e.DocumentId == editorConfigDoc.Id &&
							e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.AnalyzerConfigDocumentChanged) {
							analyzerConfigDocumentChanged = true;
						}
					};

					// Add error style to .editorconfig
					string contents =
						"root = true\r\n" +
						"\r\n" +
						"[*.cs]\r\n" +
						"csharp_style_var_for_built_in_types = true:error\r\n";
					File.WriteAllText (editorConfigFileName, contents);
					FileService.NotifyFileChanged (editorConfigFileName);

					Func<bool> action = () => analyzerConfigDocumentChanged;
					await AssertIsTrueWithTimeout (action, "Timed out waiting for analyzer config file changed event", 10000);

				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		[Test]
		public async Task EditorConfigFile_ModifiedInTextEditor ()
		{
			FilePath solFile = Util.GetSampleProject ("additional-files", "additional-files.sln");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				try {
					var project = sol.GetAllProjects ().Single ();

					FilePath editorConfigFileName = solFile.ParentDirectory.Combine (".editorconfig");
					var textFileModel = new TextBufferFileModel ();
					var mimeType = MimeTypeCatalog.Instance.FindMimeTypeForFile (editorConfigFileName);
					textFileModel.CreateNew (editorConfigFileName, mimeType?.Id);

					var projectInfo = ws.CurrentSolution.Projects.Single ();
					Microsoft.CodeAnalysis.AnalyzerConfigDocument editorConfigDoc =
						projectInfo.AnalyzerConfigDocuments.Single (d => d.FilePath == editorConfigFileName);

					int analyzerConfigDocumentChangedCount = 0;
					ws.WorkspaceChanged += (sender, e) => {
						if (e.DocumentId == editorConfigDoc.Id &&
							e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.AnalyzerConfigDocumentChanged) {
							analyzerConfigDocumentChangedCount++;
						}
					};

					using (var fileRegistration = IdeApp.TypeSystemService.RegisterOpenDocument (
						null, // No owner.
						editorConfigFileName,
						textFileModel.TextBuffer)) {

						Assert.IsTrue (ws.IsDocumentOpen (editorConfigDoc.Id));

						Func<bool> action = () => analyzerConfigDocumentChangedCount == 1;
						await AssertIsTrueWithTimeout (action, "Timed out waiting for analyzer config file changed event on opening file", 100000);

						// Add error style to .editorconfig
						string contents =
							"root = true\r\n" +
							"\r\n" +
							"[*.cs]\r\n" +
							"csharp_style_var_for_built_in_types = true:error\r\n";
						textFileModel.SetText (contents);
						await textFileModel.Save ();

						action = () => analyzerConfigDocumentChangedCount == 2;
						await AssertIsTrueWithTimeout (action, "Timed out waiting for analyzer config file changed event", 100000);
					}
					// After the file registration is disposed the document should be closed.
					Assert.IsFalse (ws.IsDocumentOpen (editorConfigDoc.Id));
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
			}
		}

		async Task AssertIsTrueWithTimeout (Func<bool> action, string message, int timeout)
		{
			int howLong = 0;
			const int interval = 200; // ms

			while (!action ()) {
				if (howLong >= timeout) {
					Assert.Fail (message);
				}

				await Task.Delay (interval);
				howLong += interval;
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
	}
}
