//
// FileWatcherServiceTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class FileWatcherServiceTests : FileWatcherTestBase
	{
		static MockWorkspaceItem CreateWorkspaceItem (string name, [CallerMemberName]string hint = null)
		{
			var dir = UnitTests.Util.CreateTmpDir (hint);

			return new MockWorkspaceItem {
				BaseDirectory = dir
			};
		}

		static Workspace SetupWorkspace (int projectCount)
		{
			var result = new Workspace ();

			var solution = CreateWorkspaceItem ("item");
			result.Items.Add (solution);

			for (int i = 0; i < projectCount; ++i) {
				var project = new MockProject (solution.BaseDirectory.Combine ("project" + i, "project_file"));
				solution.Children.Add (project);
				solution.OnRootDirectoriesChanged (project, isRemove: false, isAdd: true);
			}

			return result;
		}

		async Task TestAll (MockProject project)
		{
			await FileWatcherService.Update ();

			project.Reset ();

			var testFile = project.BaseDirectory.Combine ("test_file");
			// Create -> Change -> Rename -> Delete
			File.WriteAllText (testFile, "");
			File.Move (testFile, testFile + 2);
			File.Delete (testFile + 2);

			await WaitForFilesRemoved (new FilePath[] { testFile, testFile + 2 });
		}

		static void AssertData (MockProject project, Constraint constraint)
		{
			Assert.That (project.Created.Count, constraint);
			Assert.That (project.Deleted.Count, constraint);
		}

		[Test]
		public async Task TestCone ()
		{
			using var workspace = SetupWorkspace (2);

			await FileWatcherService.Add (workspace);

			var projects = workspace.GetAllItems<MockProject> ().ToArray ();

			await TestAll (projects [0]);

			// Check we have data for project0, but not project 1
			AssertData (projects [0], Is.GreaterThan (0));
			AssertData (projects [1], Is.EqualTo (0));
		}

		[Test]
		public async Task TestConeOuter_GetItemFiles ()
		{
			using var workspace = SetupWorkspace (1);

			var project = workspace.GetAllItems<MockProject> ().Single ();

			var newDir = project.BaseDirectory.ParentDirectory.Combine ("new_dir");
			Directory.CreateDirectory (newDir);
			var createFile = newDir.Combine ("file");

			await FileWatcherService.Add (workspace);

			// Create and validate it's not notified
			File.WriteAllText (createFile, "");
			await WaitForFileChanged (createFile);

			Assert.AreEqual (0, project.Created.Count);

			File.Delete (createFile);

			// Add it to item collection and check it is notified now
			project.Items.Add (new ProjectFile (createFile));
			workspace.GetAllItems<MockWorkspaceItem> ().Single ().OnRootDirectoriesChanged (project, false, false);

			await FileWatcherService.Update ();

			File.WriteAllText (createFile, "");
			await WaitForFileChanged (createFile);

			Assert.AreEqual (1, project.Created.Count);
		}

		[Test]
		public async Task TestUnsubscribe ()
		{
			using var workspace = SetupWorkspace (1);

			await FileWatcherService.Add (workspace);

			var solution = workspace.GetAllItems<MockWorkspaceItem> ().Single ();
			var project = workspace.GetAllItems<MockProject> ().Single ();
			solution.OnRootDirectoriesChanged (project, isRemove: true, isAdd: false);

			await TestAll (project);
			AssertData (project, Is.EqualTo (0));
		}

		[Test]
		public async Task TestNotificationsSubscribedIncremental ()
		{
			using var workspace = SetupWorkspace (0);

			await FileWatcherService.Add (workspace);

			var item = workspace.GetAllItems<MockWorkspaceItem> ().Single ();
			var projectFile = item.BaseDirectory.Combine ("project", "project_file");
			var project = new MockProject (projectFile);
			item.OnRootDirectoriesChanged (project, isRemove: false, isAdd: true);

			await TestAll (project);
			AssertData (project, Is.GreaterThan (0));
		}

		class MockWorkspaceItem : WorkspaceItem
		{
			public List<MockProject> Children { get; } = new List<MockProject> ();

			public MockWorkspaceItem()
			{
				Initialize (this);
			}

			protected override IEnumerable<WorkspaceObject> OnGetChildren () => Children;
		}
		
		class MockProject : Project
		{
			public List<FilePath> Created { get; } = new List<FilePath> ();
			public List<FilePath> Deleted { get; } = new List<FilePath> ();
			public List<(FilePath, FilePath)> Renamed { get; } = new List<(FilePath, FilePath)> ();

			FilePath fileName;
			public MockProject (FilePath fileName)
			{
				Directory.CreateDirectory (fileName.ParentDirectory);
				File.WriteAllText (fileName, "");
				this.fileName = fileName;

				Initialize (this);
			}

			public override FilePath FileName { get => fileName; set => fileName = value; }

			public void Reset()
			{
				Created.Clear ();
				Deleted.Clear ();
				Renamed.Clear ();
			}

			internal override void OnFileCreated (FilePath filePath) => Created.Add (filePath);

			internal override void OnFileDeleted (FilePath filePath) => Deleted.Add (filePath);

			internal override void OnFileRenamed (FilePath sourceFile, FilePath targetFile) => Renamed.Add ((sourceFile, targetFile));
		}
	}
}
