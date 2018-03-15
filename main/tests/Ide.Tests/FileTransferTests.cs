//
// FileTransferTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using UnitTests;
using NUnit.Framework;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using System.IO;
using System.Threading.Tasks;

namespace MonoDevelop.Ide
{
	public class FileTransferTests: IdeTestBase
	{
		[Test]
		public async Task MoveEmptyFolder ()
		{
			string solFile = Util.GetSampleProject ("transfer-tests", "console-with-libs.sln");
			using (var sol = (Solution)await MonoDevelop.Projects.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var lib1 = (DotNetProject)sol.FindProjectByName ("library1");
				var lib2 = (DotNetProject)sol.FindProjectByName ("library2");

				var sourceDir = lib2.ItemDirectory.Combine ("f2-empty");
				var targetDir = lib1.ItemDirectory.Combine ("f2-empty");

				// Git can't commit empty folders, so that folder has a dummy file that needs to be deleted
				File.Delete (sourceDir.Combine ("delete-me"));

				Assert.IsTrue (lib2.Files.GetFile (sourceDir) != null);

				ProjectOperations.TransferFilesInternal (Util.GetMonitor (), lib2, sourceDir, lib1, targetDir, true, true);

				Assert.IsTrue (Directory.Exists (targetDir));
				Assert.IsFalse (Directory.Exists (sourceDir));

				Assert.IsTrue (lib1.Files.GetFile (targetDir) != null);
				Assert.IsFalse (lib2.Files.GetFile (sourceDir) != null);
			}
		}

		[Test]
		public async Task MoveFolder ()
		{
			string solFile = Util.GetSampleProject ("transfer-tests", "console-with-libs.sln");
			using (var sol = (Solution)await MonoDevelop.Projects.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var lib1 = (DotNetProject)sol.FindProjectByName ("library1");
				var lib2 = (DotNetProject)sol.FindProjectByName ("library2");

				var sourceDir = lib2.ItemDirectory.Combine ("f2");
				var targetDir = lib1.ItemDirectory.Combine ("f2");
				var sourceFile = sourceDir.Combine ("a.cs");
				var targetFile = targetDir.Combine ("a.cs");

				Assert.IsTrue (lib2.Files.GetFile (sourceDir) != null);
				Assert.IsTrue (lib2.Files.GetFile (sourceFile) != null);

				ProjectOperations.TransferFilesInternal (Util.GetMonitor (), lib2, sourceDir, lib1, targetDir, true, true);

				Assert.IsTrue (Directory.Exists (targetDir));
				Assert.IsTrue (File.Exists (targetFile));
				Assert.IsFalse (Directory.Exists (sourceDir));

				Assert.IsTrue (lib1.Files.GetFile (targetDir) != null);
				Assert.IsTrue (lib1.Files.GetFile (targetFile) != null);
				Assert.IsFalse (lib2.Files.GetFile (sourceDir) != null);
				Assert.IsFalse (lib2.Files.GetFile (sourceFile) != null);
			}
		}

		[Test]
		public async Task MoveFileFromOneFolderToAnotherAndSaveProject ()
		{
			string solFile = Util.GetSampleProject ("transfer-tests", "console-with-libs.sln");
			string targetFile;
			string sourceFile;

			using (var sol = (Solution)await MonoDevelop.Projects.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var lib2 = (DotNetProject)sol.FindProjectByName ("library2");

				var sourceDir = lib2.ItemDirectory.Combine ("f2");
				var targetDir = lib2.ItemDirectory.Combine ("f2-empty");
				sourceFile = sourceDir.Combine ("a.cs");
				targetFile = targetDir.Combine ("a.cs");

				// Git can't commit empty folders, so that folder has a dummy file that needs to be deleted
				File.Delete (targetDir.Combine ("delete-me"));

				Assert.IsTrue (lib2.Files.GetFile (sourceFile) != null);
				Assert.IsFalse (lib2.Files.GetFile (targetFile) != null);

				ProjectOperations.TransferFilesInternal (Util.GetMonitor (), lib2, sourceFile, lib2, targetFile, true, true);

				Assert.IsFalse (lib2.Files.GetFile (sourceFile) != null);
				Assert.IsTrue (lib2.Files.GetFile (targetFile) != null);

				await lib2.SaveAsync (Util.GetMonitor ());
			}
			using (var sol = (Solution)await MonoDevelop.Projects.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var reloadedLib2 = (DotNetProject)sol.FindProjectByName ("library2");

				Assert.IsTrue (reloadedLib2.Files.GetFile (targetFile) != null);
				Assert.IsFalse (reloadedLib2.Files.GetFile (sourceFile) != null);
			}
		}

		[Test]
		public async Task CopyFileFromOneFolderToAnotherAndSaveProject ()
		{
			string solFile = Util.GetSampleProject ("transfer-tests", "console-with-libs.sln");
			string targetFile;
			string sourceFile;

			using (var sol = (Solution)await MonoDevelop.Projects.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var lib2 = (DotNetProject)sol.FindProjectByName ("library2");

				var sourceDir = lib2.ItemDirectory.Combine ("f2");
				var targetDir = lib2.ItemDirectory.Combine ("f2-empty");
				sourceFile = sourceDir.Combine ("a.cs");
				targetFile = targetDir.Combine ("a.cs");

				// Git can't commit empty folders, so that folder has a dummy file that needs to be deleted
				File.Delete (targetDir.Combine ("delete-me"));

				Assert.IsTrue (lib2.Files.GetFile (sourceFile) != null);
				Assert.IsFalse (lib2.Files.GetFile (targetFile) != null);

				ProjectOperations.TransferFilesInternal (Util.GetMonitor (), lib2, sourceFile, lib2, targetFile, false, true);

				Assert.IsTrue (lib2.Files.GetFile (sourceFile) != null);
				Assert.IsTrue (lib2.Files.GetFile (targetFile) != null);
				await lib2.SaveAsync (Util.GetMonitor ());
			}

			using (var sol = (Solution)await MonoDevelop.Projects.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var reloadedLib2 = (DotNetProject)sol.FindProjectByName ("library2");
				Assert.IsTrue (reloadedLib2.Files.GetFile (targetFile) != null);
				Assert.IsTrue (reloadedLib2.Files.GetFile (sourceFile) != null);
			}
		}
	}
}

