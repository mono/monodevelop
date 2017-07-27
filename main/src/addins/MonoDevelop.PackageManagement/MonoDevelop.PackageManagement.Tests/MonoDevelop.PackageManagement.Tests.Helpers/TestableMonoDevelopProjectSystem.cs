//
// TestableMonoDevelopProjectSystem.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Threading.Tasks;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class TestableMonoDevelopProjectSystem : MonoDevelopMSBuildNuGetProjectSystem
	{
		public string PathPassedToPhysicalFileSystemAddFile;
		public Stream StreamPassedToPhysicalFileSystemAddFile;
		public FakeFileService FakeFileService;
		public FakePackageManagementProjectService FakeProjectService;
		public PackageManagementEvents PackageManagementEvents;
		public string FileNamePassedToLogDeletedFile;
		public FileNameAndDirectory FileNameAndDirectoryPassedToLogDeletedFileFromDirectory;
		public string DirectoryPassedToLogDeletedDirectory;
		public ReferenceAndProjectName ReferenceAndProjectNamePassedToLogAddedReferenceToProject;
		public ReferenceAndProjectName ReferenceAndProjectNamePassedToLogRemovedReferenceFromProject;
		public FileNameAndProjectName FileNameAndProjectNamePassedToLogAddedFileToProject;
		public FakeNuGetPackageNewImportsHandler NewImportsHandler;
		public FakeNuGetProjectContext FakeNuGetProjectContext;

		public static Action<Action> GuiSyncDispatcher = handler => handler.Invoke ();
		public static Func<Func<Task>,Task> GuiSyncDispatcherFunc = handler => handler.Invoke();
		public static Func<Func<Task<bool>>,Task<bool>> GuiSyncDispatcherReturnBoolFunc = handler => handler.Invoke ();

		public TestableMonoDevelopProjectSystem (IDotNetProject project)
			: this (
				project,
				new FakeNuGetProjectContext (),
				new FakeFileService (project),
				new PackageManagementEvents ())
		{
		}

		TestableMonoDevelopProjectSystem (
			IDotNetProject project,
			FakeNuGetProjectContext context,
			IPackageManagementFileService fileService,
			PackageManagementEvents packageManagementEvents)
			: base (
				project,
				context,
				fileService,
				packageManagementEvents,
				GuiSyncDispatcher,
				GuiSyncDispatcherFunc,
				GuiSyncDispatcherReturnBoolFunc)
		{
			FakeNuGetProjectContext = context;
			FakeFileService = (FakeFileService)fileService;
			PackageManagementEvents = packageManagementEvents;
		}

		protected override void PhysicalFileSystemAddFile (string path, Stream stream)
		{
			PathPassedToPhysicalFileSystemAddFile = path;
			StreamPassedToPhysicalFileSystemAddFile = stream;
		}

		protected override void LogDeletedFile (string fileName)
		{
			FileNamePassedToLogDeletedFile = fileName;
		}

		protected override void LogDeletedFileFromDirectory (string fileName, string directory)
		{
			FileNameAndDirectoryPassedToLogDeletedFileFromDirectory = new FileNameAndDirectory (fileName, directory);
		}

		protected override void LogDeletedDirectory (string folder)
		{
			DirectoryPassedToLogDeletedDirectory = folder;
		}

		protected override void LogAddedReferenceToProject (string referenceName, string projectName)
		{
			ReferenceAndProjectNamePassedToLogAddedReferenceToProject = 
				new ReferenceAndProjectName (referenceName, projectName);
		}

		protected override void LogRemovedReferenceFromProject (string referenceName, string projectName)
		{
			ReferenceAndProjectNamePassedToLogRemovedReferenceFromProject = 
				new ReferenceAndProjectName (referenceName, projectName);
		}

		protected override void LogAddedFileToProject (string fileName, string projectName)
		{
			FileNameAndProjectNamePassedToLogAddedFileToProject =
				new FileNameAndProjectName (fileName, projectName);
		}

		protected override INuGetPackageNewImportsHandler CreateNewImportsHandler ()
		{
			NewImportsHandler = new FakeNuGetPackageNewImportsHandler ();
			return NewImportsHandler;
		}

		Dictionary<string, IEnumerable<string>> enumeratedDirectories = new Dictionary<string, IEnumerable<string>> ();

		public void AddDirectoriesForPath (string path, params string[] directories)
		{
			enumeratedDirectories[path] = directories;
		}

		protected override IEnumerable<string> EnumerateDirectories (string path)
		{
			IEnumerable<string> directories;
			if (enumeratedDirectories.TryGetValue (path, out directories)) {
				return directories;
			}
			return new string[0];
		}

		Dictionary<string, IEnumerable<string>> enumeratedFiles = new Dictionary<string, IEnumerable<string>> ();

		public void AddFilesForPath (string path, string searchPattern, SearchOption searchOption, params string[] files)
		{
			string key = path + searchPattern + searchOption.ToString ();
			enumeratedFiles[key] = files;
		}

		protected override IEnumerable<string> EnumerateFiles (string path, string searchPattern, SearchOption searchOption)
		{
			IEnumerable<string> files;
			string key = path + searchPattern + searchOption.ToString ();
			if (enumeratedFiles.TryGetValue (key, out files)) {
				return files;
			}
			return new string[0];
		}
	}
}


