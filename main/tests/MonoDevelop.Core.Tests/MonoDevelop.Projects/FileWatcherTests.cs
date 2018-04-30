//
// FileWatcherTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class FileWatcherTests : TestBase
	{
		Solution sol;
		List<FileEventInfo> fileChanges;
		List<FileEventInfo> filesRemoved;
		TaskCompletionSource<bool> fileChangesTask;
		FilePath waitingForFileChangeFileName;
		TaskCompletionSource<bool> fileRemovedTask;
		FilePath waitingForFileToBeRemoved;

		[SetUp]
		public void Init ()
		{
			fileChanges = new List<FileEventInfo> ();
			fileChangesTask = new TaskCompletionSource<bool> ();

			filesRemoved = new List<FileEventInfo> ();
			fileRemovedTask = new TaskCompletionSource<bool> ();

			FileService.FileChanged += OnFileChanged;
			FileService.FileRemoved += OnFileRemoved;
		}

		[TearDown]
		public void TestTearDown ()
		{
			if (sol != null) {
				sol.Dispose ();
			}

			FileService.FileChanged -= OnFileChanged;
			FileService.FileRemoved -= OnFileRemoved;
		}

		void ClearFileEventsCaptured ()
		{
			fileChanges.Clear ();
			filesRemoved.Clear ();
		}

		void OnFileChanged (object sender, FileEventArgs e)
		{
			fileChanges.AddRange (e);

			if (waitingForFileChangeFileName.IsNotNull) {
				if (fileChanges.Any (file => file.FileName == waitingForFileChangeFileName)) {
					fileChangesTask.SetResult (true);
				}
			}
		}

		void OnFileRemoved (object sender, FileEventArgs e)
		{
			filesRemoved.AddRange (e);

			if (waitingForFileToBeRemoved.IsNotNull) {
				if (filesRemoved.Any (file => file.FileName == waitingForFileToBeRemoved)) {
					fileRemovedTask.SetResult (true);
				}
			}
		}

		void AssertFileChanged (FilePath fileName)
		{
			var files = fileChanges.Select (fileChange => fileChange.FileName);
			Assert.That (files, Contains.Item (fileName));
		}

		Task WaitForFileChanged (FilePath fileName, int millisecondsTimeout = 2000)
		{
			return Task.WhenAny (Task.Delay (millisecondsTimeout), fileChangesTask.Task);
		}

		void AssertFileRemoved (FilePath fileName)
		{
			var files = filesRemoved.Select (fileChange => fileChange.FileName);
			Assert.That (files, Contains.Item (fileName));
		}

		Task WaitForFileRemoved (FilePath fileName, int millisecondsTimeout = 2000)
		{
			return Task.WhenAny (Task.Delay (millisecondsTimeout), fileRemovedTask.Task);
		}

		[Test]
		public void IsNativeMacFileWatcher ()
		{
			Assert.AreEqual (Platform.IsMac, FSW.FileSystemWatcher.IsMac);
		}

		/// <summary>
		/// Original code seems to generate the FileChanged event twice for the project file.
		/// </summary>
		[Test]
		public async Task SaveProject_AfterModifying ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			p.DefaultNamespace = "Test";
			ClearFileEventsCaptured ();

			await p.SaveAsync (Util.GetMonitor ());

			AssertFileChanged (p.FileName);
			Assert.IsFalse (p.NeedsReload);
		}

		[Test]
		public async Task SaveProjectFileExternally ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			p.DefaultNamespace = "Test";
			ClearFileEventsCaptured ();

			string xml = p.MSBuildProject.SaveToString ();
			File.WriteAllText (p.FileName, xml);

			await WaitForFileChanged (p.FileName);

			AssertFileChanged (p.FileName);
		}

		[Test]
		public async Task SaveFileInProjectExternally ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			var file = p.Files.First (f => f.FilePath.FileName == "Program.cs");
			ClearFileEventsCaptured ();

			TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);

			await WaitForFileChanged (file.FilePath);

			AssertFileChanged (file.FilePath);
		}

		[Test]
		public async Task SaveFileInProjectExternallyAfterSolutionNotWatched_NoFileChangeEventsFired ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			var file = p.Files.First (f => f.FilePath.FileName == "Program.cs");
			ClearFileEventsCaptured ();
			sol.Dispose ();
			sol = null;

			TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);

			await WaitForFileChanged (file.FilePath);

			Assert.AreEqual (0, fileChanges.Count);
		}

		[Test]
		public async Task DeleteProjectFileUsingFileService ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			ClearFileEventsCaptured ();
			var file = p.Files.First (f => f.FilePath.FileName == "Program.cs");

			FileService.DeleteFile (file.FilePath);

			await WaitForFileRemoved (file.FilePath);

			AssertFileRemoved (file.FilePath);
		}

		[Test]
		public async Task DeleteProjectFileExternally ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			ClearFileEventsCaptured ();
			var file = p.Files.First (f => f.FilePath.FileName == "Program.cs");

			File.Delete (file.FilePath);

			await WaitForFileRemoved (file.FilePath);

			AssertFileRemoved (file.FilePath);
		}

		[Test]
		public async Task SaveProjectFileExternally_ProjectOutsideSolutionDirectory ()
		{
			FilePath rootProject = Util.GetSampleProject ("FileWatcherTest", "Root.csproj");
			string solFile = rootProject.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p1 = (DotNetProject) sol.Items [0];
			var p2 = (DotNetProject) sol.Items [1];
			var file1 = p1.Files.First (f => f.FilePath.FileName == "MyClass.cs");
			var file2 = p2.Files.First (f => f.FilePath.FileName == "MyClass.cs");
			ClearFileEventsCaptured ();

			TextFileUtility.WriteText (file1.FilePath, string.Empty, Encoding.UTF8);
			TextFileUtility.WriteText (file2.FilePath, string.Empty, Encoding.UTF8);
			await WaitForFileChanged (file2.FilePath);

			AssertFileChanged (file1.FilePath);
			AssertFileChanged (file2.FilePath);
		}

		[Test]
		public async Task SaveProjectFileExternally_FileOutsideSolutionDirectory ()
		{
			FilePath rootProject = Util.GetSampleProject ("FileWatcherTest", "Root.csproj");
			string solFile = rootProject.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest2.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			var file = p.Files.First (f => f.FilePath.FileName == "MyClass.cs");
			ClearFileEventsCaptured ();

			TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);
			await WaitForFileChanged (file.FilePath);

			AssertFileChanged (file.FilePath);
			Assert.IsFalse (file.FilePath.IsChildPathOf (p.BaseDirectory));
			Assert.IsFalse (file.FilePath.IsChildPathOf (sol.BaseDirectory));
		}

		[Test]
		public async Task SaveProjectFileExternally_TwoSolutionsOpened_NoCommonDirectories ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			var file1 = p.Files.First (f => f.FilePath.FileName == "Program.cs");
			solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");
			using (var sol2 = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p2 = sol2.GetAllProjects ().First (project => project.FileName.FileName == "console-with-libs.csproj");
				var file2 = p2.Files.First (f => f.FilePath.FileName == "Program.cs");
				ClearFileEventsCaptured ();

				TextFileUtility.WriteText (file1.FilePath, string.Empty, Encoding.UTF8);
				TextFileUtility.WriteText (file2.FilePath, string.Empty, Encoding.UTF8);
				await WaitForFileChanged (file2.FilePath);

				AssertFileChanged (file1.FilePath);
				AssertFileChanged (file2.FilePath);
			}
		}

		[Test]
		public async Task SaveProjectFileExternally_TwoSolutionsOpen_SolutionsHaveCommonDirectories ()
		{
			FilePath rootProject = Util.GetSampleProject ("FileWatcherTest", "Root.csproj");
			string solFile = rootProject.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p1 = (DotNetProject) sol.Items [0];
			var file1 = p1.Files.First (f => f.FilePath.FileName == "MyClass.cs");
			solFile = rootProject.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest2.sln");
			using (var sol2 = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p2 = (DotNetProject) sol.Items [0];
				var file2 = p2.Files.First (f => f.FilePath.FileName == "MyClass.cs");
				ClearFileEventsCaptured ();

				TextFileUtility.WriteText (file1.FilePath, string.Empty, Encoding.UTF8);
				TextFileUtility.WriteText (file2.FilePath, string.Empty, Encoding.UTF8);
				await WaitForFileChanged (file2.FilePath);

				AssertFileChanged (file1.FilePath);
				AssertFileChanged (file2.FilePath);
			}
		}

		[Test]
		public async Task DeleteProjectFileExternally_TwoSolutionsOpen_FileDeletedFromCommonDirectory ()
		{
			string solFile = Util.GetSampleProject ("FileWatcherTest", "Root.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			solFile = sol.BaseDirectory.Combine ("FileWatcherTest", "FileWatcherTest.sln");
			using (var sol2 = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = (DotNetProject) sol2.Items [0];
				ClearFileEventsCaptured ();
				var file1 = p.Files.First (f => f.FilePath.FileName == "MyClass.cs");
				var file2 = p.Files.First (f => f.FilePath.FileName == "AssemblyInfo.cs");

				File.Delete (file1.FilePath);
				File.Delete (file2.FilePath);

				// Wait for second file so we can detect multiple delete events for the
				// first file deleted.
				await WaitForFileRemoved (file2.FilePath);

				AssertFileRemoved (file1.FilePath);
				AssertFileRemoved (file2.FilePath);
				Assert.AreEqual (1, filesRemoved.Count (fileChange => fileChange.FileName == file1.FilePath));
			}
		}

		/// <summary>
		/// Same as previous test but the solutions are added in a different order
		/// </summary>
		/// <returns>The project file externally two solutions open file deleted from common directory2.</returns>
		[Test]
		public async Task DeleteProjectFileExternally_TwoSolutionsOpen_FileDeletedFromCommonDirectory2 ()
		{
			FilePath rootSolFile = Util.GetSampleProject ("FileWatcherTest", "Root.sln");
			string solFile = rootSolFile.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			using (var sol2 = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), rootSolFile)) {
				var p = (DotNetProject) sol.Items [0];
				ClearFileEventsCaptured ();
				var file1 = p.Files.First (f => f.FilePath.FileName == "MyClass.cs");
				var file2 = p.Files.First (f => f.FilePath.FileName == "AssemblyInfo.cs");

				File.Delete (file1.FilePath);
				File.Delete (file2.FilePath);

				// Wait for second file so we can detect multiple delete events for the
				// first file deleted.
				await WaitForFileRemoved (file2.FilePath);

				AssertFileRemoved (file1.FilePath);
				AssertFileRemoved (file2.FilePath);
				Assert.AreEqual (1, filesRemoved.Count (fileChange => fileChange.FileName == file1.FilePath));
			}
		}

		[Test]
		public void NormalizeDirectories1 ()
		{
			FilePath fileName = Util.GetSampleProject ("FileWatcherTest", "Root.sln");
			FilePath rootDirectory = fileName.ParentDirectory;

			var directories = new [] {
				rootDirectory.Combine ("a"),
				rootDirectory,
				rootDirectory.Combine ("c")
			};

			var normalized = FileWatcherService.Normalize (directories).ToArray ();

			Assert.AreEqual (1, normalized.Length);
			Assert.That (normalized, Contains.Item (rootDirectory));
		}

		[Test]
		public void NormalizeDirectories2 ()
		{
			FilePath fileName = Util.GetSampleProject ("FileWatcherTest", "Root.sln");
			FilePath rootDirectory = fileName.ParentDirectory;

			var bDirectory = rootDirectory.Combine ("..", "b").FullPath;
			var dDirectory = rootDirectory.Combine ("..", "d").FullPath;

			var directories = new [] {
				rootDirectory.Combine ("a"),
				bDirectory,
				rootDirectory,
				rootDirectory.Combine ("c"),
				dDirectory
			};

			var normalized = FileWatcherService.Normalize (directories).ToArray ();

			Assert.AreEqual (3, normalized.Length);
			Assert.That (normalized, Contains.Item (rootDirectory));
			Assert.That (normalized, Contains.Item (bDirectory));
			Assert.That (normalized, Contains.Item (dDirectory));
		}
	}
}
