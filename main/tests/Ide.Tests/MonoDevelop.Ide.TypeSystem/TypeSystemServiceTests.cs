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
			Process.Start ("ln", $"-s '{symlinkFileSource}' '{symlinkFileName}'").WaitForExit ();

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
			RunMSBuild ($"/t:Restore /p:RestoreDisableParallel=true \"{solFile}\"");

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
				} finally {
					TypeSystemServiceTestExtensions.UnloadSolution (sol);
				}
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
	}
}
