//
// GetSourceFilesAsyncTests.cs
//
// Author:
//       Greg Munn <greg.munn@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc
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
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class GetSourceFilesAsyncTests : TestBase
	{
		TaskCompletionSource<object> fileChangeNotification;

		[SetUp]
		public void TestSetUp ()
		{
			fileChangeNotification = new TaskCompletionSource<object> ();
			FileService.FileChanged -= FileService_FileChanged;
			FileService.FileChanged += FileService_FileChanged;
		}

		[TearDown]
		public void TestTearDown ()
		{
			FileService.FileChanged -= FileService_FileChanged;
			fileChangeNotification = null;
		}

		[Test()]
		public async Task GetSourceFilesFromProjectWithImportedCompileItems()
		{
			string projectFile = Util.GetSampleProject ("project-with-corecompiledepends", "project-with-imported-files.csproj");
			var project = (Project) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);

			var projectFiles = project.Files.Where (f => f.Subtype != Subtype.Directory).ToList ();
			var sourceFiles = await project.GetSourceFilesAsync (project.Configurations[0].Selector);

			var msg = "GetSourceFilesAsync should include imported items";
			Assert.AreEqual (projectFiles.Count + 1, sourceFiles.Length, msg);

			msg = "When a project does specify CoreCompileDependsOn that adds Compile items, then GetSourceFilesAsync should include all of Files as well";
			foreach (var file in projectFiles) {
				var sourceFile = sourceFiles.FirstOrDefault (sf => sf.FilePath == file.FilePath);
				Assert.IsNotNull (sourceFile, msg);
			}
			Assert.IsTrue (sourceFiles.Any (f => f.FilePath.FileName == "Foo.cs"));
			project.Dispose ();
		}

		[Test()]
		public async Task GetSourceFilesFromProjectWithAddedCompileItems()
		{
			string projectFile = Util.GetSampleProject ("project-with-corecompiledepends", "project.csproj");
			var project = (Project) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);

			var projectFiles = project.Files.Where (f => f.Subtype != Subtype.Directory).ToList ();
			var sourceFiles = await project.GetSourceFilesAsync (project.Configurations[0].Selector);

			var msg = "When a project does specify CoreCompileDependsOn that adds Compile items, then GetSourceFilesAsync should include the added item(s)";
			Assert.AreEqual (projectFiles.Count + 2, sourceFiles.Length, msg);

			msg = "When a project does specify CoreCompileDependsOn that adds Compile items, then GetSourceFilesAsync should include all of Files as well";
			foreach (var file in projectFiles) {
				var sourceFile = sourceFiles.FirstOrDefault (sf => sf.FilePath == file.FilePath);
				Assert.IsNotNull (sourceFile, msg + ": " + file.FilePath.FileName + " not found");
			}
			project.Dispose ();
		}

		[Test()]
		public async Task FileChangeCalledWhenPerformGeneratorAsyncInvoked()
		{
			string projectFile = Util.GetSampleProject ("project-with-corecompiledepends", "project.csproj");
			var project = (Project) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);

			// FIXME: we need to mimic the project load step because MonoDevelopWorkspace calls this on project load
			// this has the effect of initialising the cache for GetSourceFilesAsync, which makes change detection work once the project has loaded
			// for when PerformGeneratorAsync is called. this also causes the file to be written on load if it is not present, which doesn't
			// help us simulate the target changing the file
			await project.GetSourceFilesAsync (project.Configurations[0].Selector);

			// clean it out so that the call to `PerformGeneratorAsync` can create it instead, that's what we want to test
			var generatedFileName = Path.Combine (project.ItemDirectory, "GeneratedFile.g.cs");
			if (File.Exists (generatedFileName)) {
				File.Delete (generatedFileName);
			}

			await project.PerformGeneratorAsync (project.Configurations[0].Selector, "UpdateGeneratedFiles");

			// we need to wait for the file notification to be posted
			await Task.Run (() => {
				fileChangeNotification.Task.Wait (TimeSpan.FromMilliseconds (10000));
			});
			          
			Assert.IsTrue (fileChangeNotification.Task.IsCompleted, "Performing the generator should have fired a file change event");
			project.Dispose ();
		}

		[Test()]
		public async Task GetSourceFilesFromProjectWithoutCoreCompileDependsOn ()
		{
			string projectFile = Util.GetSampleProject ("project-without-corecompiledepends", "project.csproj");
			var project = (Project) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);

			var projectFiles = project.Files.Where (f => f.Subtype != Subtype.Directory).ToList ();
			var sourceFiles = await project.GetSourceFilesAsync (project.Configurations[0].Selector);

			const string msg = "When a project does not specify CoreCompileDependsOn, then GetSourceFilesAsync should be identical to Files";

			Assert.AreEqual (projectFiles.Count, sourceFiles.Length, msg);

			foreach (var file in projectFiles) {
				var sourceFile = sourceFiles.FirstOrDefault (sf => sf.FilePath == file.FilePath);
				Assert.IsNotNull (sourceFile, msg);
			}
			project.Dispose ();
		}

		void FileService_FileChanged (object sender, FileEventArgs e)
		{
			var tcs = fileChangeNotification;
			if (tcs != null) {
				tcs.TrySetResult (null);
			}
		}

		[Test ()]
		public async Task FilesWithConfigurationCondition ()
		{
			string projectFile = Util.GetSampleProject ("project-with-corecompiledepends", "project-with-conditioned-file.csproj");
			var project = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile);

			var projectFiles = project.Files.Where (f => f.Subtype != Subtype.Directory).ToList ();
			var sourceFiles = await project.GetSourceFilesAsync (project.Configurations ["Debug|x86"].Selector);

			Assert.IsTrue (sourceFiles.Any (f => f.FilePath.FileName == "Conditioned.cs"));

			sourceFiles = await project.GetSourceFilesAsync (project.Configurations ["Release|x86"].Selector);

			Assert.IsFalse (sourceFiles.Any (f => f.FilePath.FileName == "Conditioned.cs"));
			project.Dispose ();
		}
	}
}