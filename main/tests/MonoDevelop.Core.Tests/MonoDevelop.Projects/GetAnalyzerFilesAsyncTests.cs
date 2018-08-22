//
// GetAnalyzerFilesAsyncTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class GetAnalyzerFilesAsyncTests : TestBase
	{
		[Test ()]
		public async Task GetAnalyzerFilesFromProjectWithImportedCompileItems ()
		{
			string projectFile = Util.GetSampleProject ("project-with-corecompiledepends", "project-with-imported-files.csproj");
			using (var project = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {

				var analyzerFiles = await project.GetAnalyzerFilesAsync (project.Configurations [0].Selector);

				var msg = "GetAnalyzerFilesAsync should include imported items";
				Assert.AreEqual (1, analyzerFiles.Length, msg);

				Assert.IsTrue (analyzerFiles.Any (f => f.FileName == "Foo.dll"));
			}
		}

		[Test ()]
		public async Task GetAnalyzerFilesFromProjectWithAddedCompileItems ()
		{
			string projectFile = Util.GetSampleProject ("project-with-corecompiledepends", "project.csproj");
			using (var project = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {

				var analyzerFiles = await project.GetAnalyzerFilesAsync (project.Configurations [0].Selector);

				var msg = "When a project does specify CoreCompileDependsOn that adds Analyzer items, then GetAnalyzerFiles should include the added item(s)";
				Assert.AreEqual (2, analyzerFiles.Length, msg);

				msg = "When a project does specify CoreCompileDependsOn that adds Analyzer items, then GetAnalyzerFiles should also include the non-generated items";
				Assert.IsTrue (analyzerFiles.Any (f => f.FileName == "Foo.dll"));
				Assert.IsTrue (analyzerFiles.Any (f => f.FileName == "GeneratedFile.dll"));
			}
		}

		[Test ()]
		public async Task GetAnalyzerFilesFromProjectWithoutCoreCompileDependsOn ()
		{
			string projectFile = Util.GetSampleProject ("project-without-corecompiledepends", "project.csproj");
			using (var project = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {

				var analyzerFiles = await project.GetAnalyzerFilesAsync (project.Configurations [0].Selector);

				const string msg = "When a project does not specify CoreCompileDependsOn, then GetAnalyzerFilesAsync should be identical to Files";

				Assert.AreEqual (1, analyzerFiles.Length, msg);
				Assert.IsTrue (analyzerFiles.Any (f => f.FileName == "Foo.dll"));
			}
		}

		[Test ()]
		public async Task FilesWithConfigurationCondition ()
		{
			string projectFile = Util.GetSampleProject ("project-with-corecompiledepends", "project-with-conditioned-file.csproj");
			using (var project = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {

				var analyzerFiles = await project.GetAnalyzerFilesAsync (project.Configurations ["Debug|x86"].Selector);

				Assert.IsTrue (analyzerFiles.Any (f => f.FileName == "Conditioned.dll"));

				analyzerFiles = await project.GetAnalyzerFilesAsync (project.Configurations ["Release|x86"].Selector);

				Assert.IsFalse (analyzerFiles.Any (f => f.FileName == "Conditioned.dll"));
			}
		}

		[Test]
		public async Task ImportWithCoreCompileDependsOnAddedAfterAnalyzerFilesCached ()
		{
			string projectFile = Util.GetSampleProject ("project-with-corecompiledepends", "consoleproject.csproj");
			using (var project = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {

				var analyzerFiles = await project.GetAnalyzerFilesAsync (project.Configurations [0].Selector);

				Assert.AreEqual (0, analyzerFiles.Length);

				string modifiedHint = null;
				project.Modified += (sender, args) => modifiedHint = args.First ().Hint;

				var before = new MSBuildItem (); // Ensures import added at end of project.
				project.MSBuildProject.AddNewImport ("consoleproject-import.targets", null, before);
				Assert.AreEqual ("Files", modifiedHint);

				analyzerFiles = await project.GetAnalyzerFilesAsync (project.Configurations [0].Selector);

				Assert.IsTrue (analyzerFiles.Any (f => f.FileName == "GeneratedAnalyzer.g.dll"));

				modifiedHint = null;
				project.MSBuildProject.RemoveImport ("consoleproject-import.targets");
				Assert.AreEqual ("Files", modifiedHint);

				analyzerFiles = await project.GetAnalyzerFilesAsync (project.Configurations [0].Selector);

				Assert.IsFalse (analyzerFiles.Any (f => f.FileName == "GeneratedAnalyzer.g.dll"));
				Assert.AreEqual (0, analyzerFiles.Count ());
			}
		}
	}
}