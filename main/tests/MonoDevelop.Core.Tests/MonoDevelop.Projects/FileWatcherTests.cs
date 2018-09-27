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

using System;
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
		List<FilePath> waitingForFileChangeFileNames;
		TaskCompletionSource<bool> fileRemovedTask;
		List<FilePath> waitingForFilesToBeRemoved;

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

			waitingForFilesToBeRemoved = null;
			waitingForFileChangeFileNames = null;

			fileChangesTask = new TaskCompletionSource<bool> ();
			fileRemovedTask = new TaskCompletionSource<bool> ();
		}

		void OnFileChanged (object sender, FileEventArgs e)
		{
			fileChanges.AddRange (e);

			if (waitingForFileChangeFileNames != null) {
				lock (fileChangesTask) {
					int removedCount = waitingForFileChangeFileNames.RemoveAll (file => {
						return fileChanges.Any (fileChange => fileChange.FileName == file);
					});
					if (removedCount > 0 && !waitingForFileChangeFileNames.Any ()) {
						fileChangesTask.TrySetResult (true);
					}
				}
			}
		}

		void OnFileRemoved (object sender, FileEventArgs e)
		{
			filesRemoved.AddRange (e);

			if (waitingForFilesToBeRemoved != null) {
				lock (fileRemovedTask) {
					int removedCount = waitingForFilesToBeRemoved.RemoveAll (file => {
						return filesRemoved.Any (fileChange => fileChange.FileName == file);
					});
					if (removedCount > 0 && !waitingForFilesToBeRemoved.Any ()) {
						fileRemovedTask.TrySetResult (true);
					}
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
			return WaitForFilesChanged (new [] { fileName }, millisecondsTimeout);
		}

		/// <summary>
		/// File change events seem to happen out of order sometimes so allow the tests to
		/// specify a set of files.
		/// </summary>
		Task WaitForFilesChanged (FilePath[] fileNames, int millisecondsTimeout = 2000)
		{
			waitingForFileChangeFileNames = new List<FilePath> (fileNames);
			return Task.WhenAny (Task.Delay (millisecondsTimeout), fileChangesTask.Task);
		}

		void AssertFileRemoved (FilePath fileName)
		{
			var files = filesRemoved.Select (fileChange => fileChange.FileName);
			Assert.That (files, Contains.Item (fileName));
		}

		Task WaitForFileRemoved (FilePath fileName, int millisecondsTimeout = 2000)
		{
			return WaitForFilesRemoved (new [] { fileName }, millisecondsTimeout);
		}

		/// <summary>
		/// File remove events seem to happen out of order sometimes so allow the tests to
		/// specify a set of files.
		/// </summary>
		Task WaitForFilesRemoved (FilePath[] fileNames, int millisecondsTimeout = 2000)
		{
			waitingForFilesToBeRemoved = new List<FilePath> (fileNames);
			return Task.WhenAny (Task.Delay (millisecondsTimeout), fileRemovedTask.Task);
		}

		// Ensures the FileService.FileChanged event fires for the project's FileStatusTracker
		// first so the WaitForFileChanged method finishes after the project has updated its
		// NeedsReload property.
		void ResetFileServiceChangedEventHandler ()
		{
			FileService.FileChanged -= OnFileChanged;
			FileService.FileChanged += OnFileChanged;
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
			ResetFileServiceChangedEventHandler ();
			await FileWatcherService.Add (sol);

			await p.SaveAsync (Util.GetMonitor ());

			await WaitForFileChanged (p.FileName);

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
			// Ensure FileService.FileChanged event is handled after the project handles it.
			ResetFileServiceChangedEventHandler ();
			await FileWatcherService.Add (sol);
			SolutionFolderItem reloadRequiredEventFiredProject = null;
			sol.ItemReloadRequired += (sender, e) => {
				reloadRequiredEventFiredProject = e.SolutionItem;
			};

			string xml = p.MSBuildProject.SaveToString ();
			File.WriteAllText (p.FileName, xml);

			await WaitForFileChanged (p.FileName);

			AssertFileChanged (p.FileName);
			Assert.IsTrue (p.NeedsReload);
			Assert.AreEqual (p, reloadRequiredEventFiredProject);
		}

		[Test]
		public async Task SaveSolutionFileExternally ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			ClearFileEventsCaptured ();
			// Ensure FileService.FileChanged event is handled after the solution handles it.
			ResetFileServiceChangedEventHandler ();
			await FileWatcherService.Add (sol);
			WorkspaceItem reloadRequiredEventFiredSolution = null;
			sol.ReloadRequired += (sender, e) => {
				reloadRequiredEventFiredSolution = e.Item;
			};

			File.WriteAllText (sol.FileName, "test");

			await WaitForFileChanged (sol.FileName);

			AssertFileChanged (sol.FileName);
			Assert.IsTrue (sol.NeedsReload);
			Assert.AreEqual (sol, reloadRequiredEventFiredSolution);
		}

		[Test]
		public async Task SaveFileInProjectExternally ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			var file = p.Files.First (f => f.FilePath.FileName == "Program.cs");
			ClearFileEventsCaptured ();
			await FileWatcherService.Add (sol);

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
			await FileWatcherService.Add (sol);
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
			await FileWatcherService.Add (sol);
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
			await FileWatcherService.Add (sol);
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
			await FileWatcherService.Add (sol);
			ClearFileEventsCaptured ();

			TextFileUtility.WriteText (file1.FilePath, string.Empty, Encoding.UTF8);
			TextFileUtility.WriteText (file2.FilePath, string.Empty, Encoding.UTF8);
			await WaitForFilesChanged (new [] { file1.FilePath, file2.FilePath});

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
			await FileWatcherService.Add (sol);

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
				await FileWatcherService.Add (sol);
				await FileWatcherService.Add (sol2);

				TextFileUtility.WriteText (file1.FilePath, string.Empty, Encoding.UTF8);
				TextFileUtility.WriteText (file2.FilePath, string.Empty, Encoding.UTF8);
				await WaitForFilesChanged (new [] { file1.FilePath, file2.FilePath });

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
				await FileWatcherService.Add (sol);
				await FileWatcherService.Add (sol2);

				TextFileUtility.WriteText (file1.FilePath, string.Empty, Encoding.UTF8);
				TextFileUtility.WriteText (file2.FilePath, string.Empty, Encoding.UTF8);
				await WaitForFilesChanged (new [] { file1.FilePath, file2.FilePath });

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
				await FileWatcherService.Add (sol);
				await FileWatcherService.Add (sol2);
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
				await FileWatcherService.Add (sol);
				await FileWatcherService.Add (sol2);
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
		public async Task DeleteProjectFileExternally_TwoSolutionsOpen_OneSolutionDisposed ()
		{
			FilePath rootSolFile = Util.GetSampleProject ("FileWatcherTest", "Root.sln");
			string solFile = rootSolFile.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			ProjectFile file;
			using (var sol2 = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), rootSolFile)) {
				var p = (DotNetProject) sol.Items [0];
				ClearFileEventsCaptured ();
				await FileWatcherService.Add (sol);
				await FileWatcherService.Add (sol2);
				file = p.Files.First (f => f.FilePath.FileName == "MyClass.cs");
			}

			// Disposing the solution does not wait for the file watchers to be updated
			// so force the update.
			await FileWatcherService.Update ();

			// Delete after disposing the root solution
			File.Delete (file.FilePath);

			await WaitForFileRemoved (file.FilePath);

			AssertFileRemoved (file.FilePath);
		}

		[Test]
		public async Task WatchDirectories_TwoFilesChanged_OneClosed ()
		{
			FilePath rootSolFile = Util.GetSampleProject ("FileWatcherTest", "Root.sln");
			var file1 = rootSolFile.ParentDirectory.Combine ("FileWatcherTest", "MyClass.cs");
			var file2 = rootSolFile.ParentDirectory.Combine ("Library", "Properties", "AssemblyInfo.cs");
			var id = new object ();

			try {
				var directories = new [] {
					file1.ParentDirectory,
					file2.ParentDirectory
				};
				await FileWatcherService.WatchDirectories (id, directories);

				TextFileUtility.WriteText (file1, string.Empty, Encoding.UTF8);
				TextFileUtility.WriteText (file2, string.Empty, Encoding.UTF8);
				await WaitForFilesChanged (new [] { file1, file2 });

				AssertFileChanged (file1);
				AssertFileChanged (file2);

				// Unwatch one directory.
				directories = new [] {
					file1.ParentDirectory
				};
				await FileWatcherService.WatchDirectories (id, directories);
				ClearFileEventsCaptured ();
				fileChangesTask = new TaskCompletionSource<bool> ();

				TextFileUtility.WriteText (file2, string.Empty, Encoding.UTF8);
				TextFileUtility.WriteText (file1, string.Empty, Encoding.UTF8);
				await WaitForFilesChanged (new [] { file1, file2 });

				AssertFileChanged (file1);
				Assert.IsFalse (fileChanges.Any (f => f.FileName == file2));
			} finally {
				await FileWatcherService.WatchDirectories (id, null);
			}
		}

		[Test]
		public async Task WatchDirectories_SolutionOpen_TwoFilesDeleted ()
		{
			FilePath rootSolFile = Util.GetSampleProject ("FileWatcherTest", "Root.sln");
			string solFile = rootSolFile.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var file1 = rootSolFile.ParentDirectory.Combine ("FileWatcherTest", "MyClass.cs");
			var file2 = rootSolFile.ParentDirectory.Combine ("Library", "Properties", "AssemblyInfo.cs");
			var directories = new [] {
				file1.ParentDirectory,
				file2.ParentDirectory
			};
			var id = new object ();
			try {
				await FileWatcherService.WatchDirectories (id, directories);
				ClearFileEventsCaptured ();
				await FileWatcherService.Add (sol);

				File.Delete (file1);
				File.Delete (file2);

				await WaitForFilesRemoved (new [] { file1, file2 });

				AssertFileRemoved (file1);
				AssertFileRemoved (file2);
				Assert.AreEqual (1, filesRemoved.Count (fileChange => fileChange.FileName == file1));
				Assert.AreEqual (1, filesRemoved.Count (fileChange => fileChange.FileName == file2));
			} finally {
				await FileWatcherService.WatchDirectories (id, null);
			}
		}

		[Test]
		public async Task AddNewProjectToSolution_ChangeFileInNewProject ()
		{
			FilePath rootProject = Util.GetSampleProject ("FileWatcherTest", "Root.csproj");
			string solFile = rootProject.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest3.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];
			var otherFile = p.Files.First (f => f.FilePath.FileName == "MyClass.cs");
			await FileWatcherService.Add (sol);
			var libraryProjectFile = rootProject.ParentDirectory.Combine ("Library", "Library.csproj");
			var p2 = (DotNetProject) await sol.RootFolder.AddItem (Util.GetMonitor (), libraryProjectFile);
			await sol.SaveAsync (Util.GetMonitor ());
			var file = p2.Files.First (f => f.FilePath.FileName == "MyClass.cs");
			await FileWatcherService.Update ();
			ClearFileEventsCaptured ();

			TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);
			await WaitForFileChanged (file.FilePath);

			AssertFileChanged (file.FilePath);
			Assert.IsFalse (file.FilePath.IsChildPathOf (sol.BaseDirectory));

			sol.RootFolder.Items.Remove (p2);
			p2.Dispose ();
			await sol.SaveAsync (Util.GetMonitor ());
			await FileWatcherService.Update ();
			ClearFileEventsCaptured ();

			TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);
			TextFileUtility.WriteText (otherFile.FilePath, string.Empty, Encoding.UTF8);
			await WaitForFilesChanged (new [] { file.FilePath, otherFile.FilePath });
			Assert.IsFalse (fileChanges.Any (f => f.FileName == file.FilePath));
		}

		[Test]
		public async Task AddSolutionToWorkspace_ChangeFileInAddedSolution ()
		{
			FilePath rootProject = Util.GetSampleProject ("FileWatcherTest", "Root.csproj");
			string workspaceFile = rootProject.ParentDirectory.Combine ("Workspace", "FileWatcherTest.mdw");
			using (var workspace = (Workspace) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), workspaceFile)) {
				await FileWatcherService.Add (workspace);
				string solFile = rootProject.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest3.sln");
				sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (DotNetProject)sol.Items [0];
				var file = p.Files.First (f => f.FilePath.FileName == "MyClass.cs");
				workspace.Items.Add (sol);
				await workspace.SaveAsync (Util.GetMonitor ());
				var otherFile = workspace.FileName.ParentDirectory.Combine ("test.txt");
				ClearFileEventsCaptured ();

				// Make sure file watcher is updated since adding the new solution
				// will start a task and may not have finished. So force an update here.
				await FileWatcherService.Update ();

				TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);
				await WaitForFileChanged (file.FilePath);

				AssertFileChanged (file.FilePath);
				Assert.IsFalse (file.FilePath.IsChildPathOf (workspace.BaseDirectory));

				workspace.Items.Remove (sol);
				await workspace.SaveAsync (Util.GetMonitor ());
				await FileWatcherService.Update ();
				ClearFileEventsCaptured ();

				TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);
				TextFileUtility.WriteText (otherFile, string.Empty, Encoding.UTF8);
				await WaitForFilesChanged (new [] { file.FilePath, otherFile });
				Assert.IsFalse (fileChanges.Any (f => f.FileName == file.FilePath));
			}
		}

		[Test]
		public async Task AddFile_FileOutsideSolutionDirectory ()
		{
			FilePath rootProject = Util.GetSampleProject ("FileWatcherTest", "Root.csproj");
			string solFile = rootProject.ParentDirectory.Combine ("FileWatcherTest", "FileWatcherTest3.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			var file2 = p.Files.First (f => f.FilePath.FileName == "MyClass.cs");
			ClearFileEventsCaptured ();
			await FileWatcherService.Add (sol);
			var newFile = rootProject.ParentDirectory.Combine ("Library", "MyClass.cs");
			var file = new ProjectFile (newFile);
			file.Link = "LinkedMyClass.cs";
			p.AddFile (file);
			await FileWatcherService.Update ();
			ClearFileEventsCaptured ();

			TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);
			await WaitForFileChanged (file.FilePath);

			AssertFileChanged (file.FilePath);
			Assert.IsFalse (file.FilePath.IsChildPathOf (p.BaseDirectory));
			Assert.IsFalse (file.FilePath.IsChildPathOf (sol.BaseDirectory));

			// After removing the file no events should be generated for the file.
			p.Files.Remove (file);
			await FileWatcherService.Update ();
			ClearFileEventsCaptured ();

			TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);
			TextFileUtility.WriteText (file2.FilePath, string.Empty, Encoding.UTF8);
			await WaitForFilesChanged (new [] { file.FilePath, file2.FilePath });

			AssertFileChanged (file2.FilePath);
			Assert.IsFalse (fileChanges.Any (f => f.FileName == file.FilePath));
		}

		/// <summary>
		/// TextEdit.app will sometimes make a backup copy of a file and then rename it to
		/// the original file if it was changed whilst it was open. This results in the document
		/// not being reloaded in the text editor. So we handle this by treating the rename
		/// as a file change for the target file.
		/// </summary>
		[Test]
		public async Task ExternalRenameTemporaryFileToFileInProject ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			var file = p.Files.First (f => f.FilePath.FileName == "Program.cs");
			var tempFile = file.FilePath.ChangeExtension (".cs-temp");
			File.WriteAllText (tempFile, "test");
			ClearFileEventsCaptured ();
			await FileWatcherService.Add (sol);

			FileService.SystemRename (tempFile, file.FilePath);

			await WaitForFileChanged (file.FilePath);

			AssertFileChanged (file.FilePath);
		}

		/// <summary>
		/// The native file watcher sometimes has one event (Changed|Created|Deleted) which it turns into three
		/// events in the order Changed, Created and Deleted. This test checks that the final Delete event is
		/// ignored if the file exists. File.WriteAllText when the file does not exist seems to be a way
		/// to trigger this behaviour on the Mac. Note that this does sometimes works - File.WriteAllText does
		/// not always cause the native file watcher to generate the delete event.
		/// </summary>
		[Test]
		public async Task WriteAllText_FileDoesNotExist_NativeFileWatcherGeneratesChangedCreatedDeletedEvents ()
		{
			FilePath rootProject = Util.GetSampleProject ("FileWatcherTest", "Root.csproj");
			var testFile = rootProject.ParentDirectory.Combine ("test1.txt");
			ClearFileEventsCaptured ();
			var id = new object ();
			try {
				await FileWatcherService.WatchDirectories (id, new [] { rootProject.ParentDirectory });

				File.WriteAllText (testFile, DateTime.Now.ToString ());
				File.WriteAllText (testFile, "test1");
				File.WriteAllText (rootProject, "test2");

				await WaitForFilesChanged (new [] { rootProject, testFile });

				AssertFileChanged (rootProject);

				// Check the delete event is not generated for the file being created and written to.
				Assert.IsFalse (filesRemoved.Any (file => file.FileName == testFile));
			} finally {
				await FileWatcherService.WatchDirectories (id, null);
			}
		}

		[Test]
		public async Task WatchDirectories_EmptyDirectory_NoExceptionThrown ()
		{
			var directories = new [] {
				FilePath.Empty
			};

			var id = new object ();
			try {
				await FileWatcherService.WatchDirectories (id, directories);
			} finally {
				await FileWatcherService.WatchDirectories (id, null);
			}
		}

		/// <summary>
		/// Native file watcher will throw an ArgumentException if the Directory does not exist.
		/// </summary>
		[Test]
		public async Task WatchDirectories_DirectoryDoesNotExist_NoExceptionThrown ()
		{
			FilePath rootProject = Util.GetSampleProject ("FileWatcherTest", "Root.csproj");
			var invalidDirectory = rootProject.Combine ("Invalid");
			var directories = new [] {
				invalidDirectory
			};

			var id = new object ();
			try {
				await FileWatcherService.WatchDirectories (id, directories);
				Assert.IsFalse (Directory.Exists (invalidDirectory));
			} finally {
				await FileWatcherService.WatchDirectories (id, null);
			}
		}

		/// <summary>
		/// Deleting a file using Finder on the Mac will move the file to the ~/.Trash folder.
		/// This used to be detected checking the file existed on switching back to the IDE.
		/// To handle this the rename event is treated as a delete of the source file.
		/// </summary>
		[Test]
		public async Task MoveFileToTrash_RenamesFile ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject) sol.Items [0];
			var file = p.Files.First (f => f.FilePath.FileName == "Program.cs");
			var tempFile = file.FilePath.ChangeExtension (".cs-temp");
			ClearFileEventsCaptured ();
			await FileWatcherService.Add (sol);

			FileService.SystemRename (file.FilePath, tempFile);

			await WaitForFileRemoved (file.FilePath);

			AssertFileRemoved (file.FilePath);
		}

		[Test]
		public async Task SaveProjectFileExternally_ProjectInSolutionFolder ()
		{
			string solFile = Util.GetSampleProject ("ProjectInSolutionFolder", "ProjectInSolutionFolder.sln");
			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];
			p.DefaultNamespace = "Test";
			ClearFileEventsCaptured ();
			// Ensure FileService.FileChanged event is handled after the project handles it.
			ResetFileServiceChangedEventHandler ();
			await FileWatcherService.Add (sol);
			SolutionFolderItem reloadRequiredEventFiredProject = null;
			sol.ItemReloadRequired += (sender, e) => {
				reloadRequiredEventFiredProject = e.SolutionItem;
			};

			string xml = p.MSBuildProject.SaveToString ();
			File.WriteAllText (p.FileName, xml);

			await WaitForFileChanged (p.FileName);

			AssertFileChanged (p.FileName);
			Assert.IsTrue (p.NeedsReload);
			Assert.AreEqual (p, reloadRequiredEventFiredProject);
		}
	}
}
