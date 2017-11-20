//
// DotNetCoreFileWatcherTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DotNetCoreFileWatcherTests : DotNetCoreTestBase
	{
		const int FileWatcherTimeout = 2000; // ms

		DotNetProject project;
		TaskCompletionSource<ProjectFile> fileAddedTaskCompletionSource;
		TaskCompletionSource<ProjectFile> fileRemovedTaskCompletionSource;
		TaskCompletionSource<FileCopyEventArgs> directoryRenamedCompletionSource;

		async Task<DotNetProject> OpenProject ()
		{
			string solutionFileName = Util.GetSampleProject ("DotNetCoreFileWatcherTests", "DotNetCoreFileWatcherTests.sln");
			var solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			project = solution.GetAllProjects ().Single () as DotNetProject;

			CreateNuGetConfigFile (solution.BaseDirectory);

			Assert.AreEqual (0, project.Files.Count);

			RunMSBuild ($"/t:Restore {solution.FileName}");

			return project;
		}

		/// <summary>
		/// Clear all other package sources and just use the main NuGet package source when
		/// restoring the packages for the project temlate tests.
		/// </summary>
		void CreateNuGetConfigFile (FilePath directory)
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

		Task<ProjectFile> WaitForSingleFileAdded (Project project)
		{
			fileAddedTaskCompletionSource = new TaskCompletionSource<ProjectFile> ();

			project.FileAddedToProject += OnSingleFileAddedToProject;

			return fileAddedTaskCompletionSource.Task;
		}

		void OnSingleFileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			project.FileAddedToProject -= OnSingleFileAddedToProject;

			try {
				var file = e.Single ().ProjectFile;

				fileAddedTaskCompletionSource.SetResult (file);
			} catch (Exception ex) {
				fileAddedTaskCompletionSource.SetException (ex);
			}
		}

		Task<ProjectFile> WaitForSingleFileRemoved (Project project)
		{
			fileRemovedTaskCompletionSource = new TaskCompletionSource<ProjectFile> ();

			project.FileRemovedFromProject += OnSingleFileRemovedFromProject;

			return fileRemovedTaskCompletionSource.Task;
		}

		void OnSingleFileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			project.FileRemovedFromProject -= OnSingleFileRemovedFromProject;

			try {
				var file = e.Single ().ProjectFile;

				fileRemovedTaskCompletionSource.SetResult (file);
			} catch (Exception ex) {
				fileRemovedTaskCompletionSource.SetException (ex);
			}
		}

		/// <summary>
		/// Currently a rename is treated as though a new file is added and one is removed.
		/// </summary>
		Task WaitForSingleFileRenamed (Project project)
		{
			fileRemovedTaskCompletionSource = new TaskCompletionSource<ProjectFile> ();

			project.FileRemovedFromProject += OnSingleFileRemovedFromProject;

			return fileRemovedTaskCompletionSource.Task;
		}

		static FilePath WriteFile (FilePath directory, string name, string contents = "")
		{
			var fileName = directory.Combine (name);
			File.WriteAllText (fileName, contents);

			return fileName;
		}

		async Task AssertFileAddedToProject (Task<ProjectFile> fileAddedTask, FilePath fileName, string expectedBuildAction)
		{
			await WaitWithTimeout (fileAddedTask, FileWatcherTimeout);

			var projectFile = fileAddedTask.Result;

			Assert.AreEqual (fileName, projectFile.FilePath);
			Assert.AreEqual (expectedBuildAction, projectFile.BuildAction);
			Assert.IsTrue (project.Files.Any (file => file.FilePath == fileName), $"File not added to project '{fileName}'");
		}

		async Task AssertFileRemovedFromProject (Task<ProjectFile> fileRemovedtask, FilePath fileName)
		{
			await WaitWithTimeout (fileRemovedtask, FileWatcherTimeout);

			var projectFile = fileRemovedtask.Result;

			Assert.AreEqual (fileName, projectFile.FilePath);
			Assert.IsFalse (project.Files.Any (file => file.FilePath == fileName), $"File not removed from project '{fileName}'");
		}

		async Task AssertFileRenamedInProject (Task fileRenamedtask, FilePath oldFileName, FilePath newFileName)
		{
			await WaitWithTimeout (fileRenamedtask, FileWatcherTimeout);

			Assert.IsTrue (project.Files.Any (file => file.FilePath == newFileName), $"File not added to project '{newFileName}'");
			Assert.IsFalse (project.Files.Any (file => file.FilePath == oldFileName), $"File not removed from project '{oldFileName}'");
		}

		static async Task WaitWithTimeout (Task task, int timeout)
		{
			var finishedTask = await Task.WhenAny (task, Task.Delay (timeout));
			if (finishedTask != task) {
				throw new ApplicationException ("Timed out waiting.");
			}
		}

		/// <summary>
		/// Currently this is handled by the file service raising a FileRenamed event.
		/// This then triggers the RootWorkspace to call the solution folder's RenameFileInProjects
		/// method.
		/// </summary>
		Task<FileCopyEventArgs> WaitForSingleDirectoryRenamed ()
		{
			directoryRenamedCompletionSource = new TaskCompletionSource<FileCopyEventArgs> ();

			FileService.FileRenamed += FileServiceOnFileRenamed;

			return directoryRenamedCompletionSource.Task;
		}

		void FileServiceOnFileRenamed (object sender, FileCopyEventArgs e)
		{
			FileService.FileRenamed -= FileServiceOnFileRenamed;

			try {
				var result = e.Single ();

				// Simulate what the RootWorkspace does on a rename.
				project.ParentSolution.RootFolder.RenameFileInProjects (result.SourceFile, result.TargetFile);

				directoryRenamedCompletionSource.SetResult (e);
			} catch (Exception ex) {
				directoryRenamedCompletionSource.SetException (ex);
			}
		}

		[Test]
		public async Task AddRenameRemoveSingleFile ()
		{
			var project = await OpenProject ();

			// Create new C# file.
			var fileAdded = WaitForSingleFileAdded (project);
			var newCSharpFilePath = WriteFile (project.BaseDirectory, "NewCSharpFile.cs");

			await AssertFileAddedToProject (fileAdded, newCSharpFilePath, "Compile");

			// Create text file.
			fileAdded = WaitForSingleFileAdded (project);
			var newTextFilePath = WriteFile (project.BaseDirectory, "NewTextFile.txt");

			await AssertFileAddedToProject (fileAdded, newTextFilePath, "None");

			// Create new C# file in new subdirectory.
			fileAdded = WaitForSingleFileAdded (project);
			var directory = project.BaseDirectory.Combine ("Src");
			Directory.CreateDirectory (directory);
			var newCSharpFile2Path = WriteFile (directory, "NewCSharpFile2.cs");

			await AssertFileAddedToProject (fileAdded, newCSharpFile2Path, "Compile");

			// Rename C# file.
			var fileRenamed = WaitForSingleFileRenamed (project);
			var renamedCSharpFileName = newCSharpFilePath.ChangeName ("RenamedCSharpFile");
			File.Move (newCSharpFilePath, renamedCSharpFileName);

			await AssertFileRenamedInProject (fileRenamed, newCSharpFilePath, renamedCSharpFileName);

			// Delete text file.
			var fileRemoved = WaitForSingleFileRemoved (project);
			File.Delete (newTextFilePath);

			await AssertFileRemovedFromProject (fileRemoved, newTextFilePath);

			// Delete directory containing one C# file.
			fileRemoved = WaitForSingleFileRemoved (project);
			Directory.Delete (directory, true);

			await AssertFileRemovedFromProject (fileRemoved, newCSharpFile2Path);
		}

		[Test]
		public async Task RenameDirectory ()
		{
			var project = await OpenProject ();

			// Create new C# file in new subdirectory.
			var fileAdded = WaitForSingleFileAdded (project);
			var directory = project.BaseDirectory.Combine ("Src");
			Directory.CreateDirectory (directory);
			var newCSharpFilePath = WriteFile (directory, "NewCSharpFile.cs");

			await AssertFileAddedToProject (fileAdded, newCSharpFilePath, "Compile");

			// Rename directory
			var directoryRenamed = WaitForSingleDirectoryRenamed ();
			var renamedDirectory = directory.ParentDirectory.Combine ("RenamedSrc");
			var renamedCSharpFileName = renamedDirectory.Combine ("NewCSharpFile.cs");
			Directory.Move (directory, renamedDirectory);

			await AssertFileRenamedInProject (directoryRenamed, newCSharpFilePath, renamedCSharpFileName);
			var fileCopy = directoryRenamed.Result.Single ();
			Assert.AreEqual (renamedDirectory, fileCopy.TargetFile);
			Assert.AreEqual (directory, fileCopy.SourceFile);
		}

		[Test]
		public async Task FileWrittenButAlreadyExistsInFilesCollection_DuplicateFileNotAdded ()
		{
			var project = await OpenProject ();

			var newCSharpFilePath = WriteFile (project.BaseDirectory, "NewCSharpFile.cs");
			var projectFile = new ProjectFile (newCSharpFilePath, BuildAction.Compile);
			project.AddFile (projectFile);

			var fileAdded = WaitForSingleFileAdded (project);

			var finishedTask = await Task.WhenAny (fileAdded, Task.Delay (500));

			Assert.AreEqual (1, project.Files.Count (file => file.FilePath == newCSharpFilePath));
			Assert.AreNotEqual (fileAdded, finishedTask);
		}
	}
}