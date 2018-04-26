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
		TaskCompletionSource<bool> fileChangesTask;
		FilePath waitingForFileChangeFileName;

		[SetUp]
		public void Init ()
		{
			fileChanges = new List<FileEventInfo> ();
			fileChangesTask = new TaskCompletionSource<bool> ();
			FileService.FileChanged += OnFileChanged;
		}

		[TearDown]
		public void TestTearDown ()
		{
			if (sol != null) {
				FileWatcherService.Remove (sol);
				sol.Dispose ();
			}

			FileService.FileChanged -= OnFileChanged;
		}

		void ClearFileChanges ()
		{
			fileChanges.Clear ();
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

		void AssertFileChanged (FilePath fileName)
		{
			var files = fileChanges.Select (fileChange => fileChange.FileName);
			Assert.That (files, Contains.Item (fileName));
		}

		Task WaitForFileChanged (FilePath fileName, int millisecondsTimeout = 2000)
		{
			return Task.WhenAny (Task.Delay (millisecondsTimeout), fileChangesTask.Task);
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
			ClearFileChanges ();
			FileWatcherService.Add (sol);

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
			ClearFileChanges ();
			FileWatcherService.Add (sol);

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
			ClearFileChanges ();
			FileWatcherService.Add (sol);

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
			ClearFileChanges ();
			FileWatcherService.Add (sol);
			FileWatcherService.Remove (sol);

			TextFileUtility.WriteText (file.FilePath, string.Empty, Encoding.UTF8);

			await WaitForFileChanged (file.FilePath);

			Assert.AreEqual (0, fileChanges.Count);
		}
	}
}
