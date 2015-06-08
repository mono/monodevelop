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
using System.IO;
using ICSharpCode.PackageManagement;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class TestableMonoDevelopProjectSystem : SharpDevelopProjectSystem
	{
		public string PathPassedToPhysicalFileSystemAddFile;
		public Stream StreamPassedToPhysicalFileSystemAddFile;
		public Action<Stream> ActionPassedToPhysicalFileSystemAddFile;
		public FakeFileService FakeFileService;
		public FakePackageManagementProjectService FakeProjectService;
		public PackageManagementEvents PackageManagementEvents;
		public FakeLogger FakeLogger;
		public string FileNamePassedToLogDeletedFile;
		public FileNameAndDirectory FileNameAndDirectoryPassedToLogDeletedFileFromDirectory;
		public string DirectoryPassedToLogDeletedDirectory;
		public ReferenceAndProjectName ReferenceAndProjectNamePassedToLogAddedReferenceToProject;
		public ReferenceAndProjectName ReferenceAndProjectNamePassedToLogRemovedReferenceFromProject;
		public FileNameAndProjectName FileNameAndProjectNamePassedToLogAddedFileToProject;

		public static Action<MessageHandler> GuiSyncDispatcher = handler => handler.Invoke ();

		public TestableMonoDevelopProjectSystem (IDotNetProject project)
			: this (
				project,
				new FakeFileService (project),
				new FakePackageManagementProjectService (),
				new PackageManagementEvents (),
				new FakeLogger ())
		{
		}

		TestableMonoDevelopProjectSystem (
			IDotNetProject project,
			IPackageManagementFileService fileService,
			IPackageManagementProjectService projectService,
			PackageManagementEvents packageManagementEvents,
			FakeLogger logger)
			: base (project, fileService, projectService, packageManagementEvents, GuiSyncDispatcher)
		{
			FakeFileService = (FakeFileService)fileService;
			FakeProjectService = (FakePackageManagementProjectService)projectService;
			PackageManagementEvents = packageManagementEvents;
			Logger = logger;
		}

		protected override void PhysicalFileSystemAddFile (string path, Stream stream)
		{
			PathPassedToPhysicalFileSystemAddFile = path;
			StreamPassedToPhysicalFileSystemAddFile = stream;
		}

		protected override void PhysicalFileSystemAddFile (string path, Action<Stream> writeToStream)
		{
			PathPassedToPhysicalFileSystemAddFile = path;
			ActionPassedToPhysicalFileSystemAddFile = writeToStream;
		}

		protected override void LogDeletedFile (string fileName)
		{
			FileNamePassedToLogDeletedFile = fileName;
		}

		protected override void LogDeletedFileFromDirectory (string fileName, string directory)
		{
			FileNameAndDirectoryPassedToLogDeletedFileFromDirectory = new FileNameAndDirectory (fileName, directory);
		}

		protected override void LogDeletedDirectory (string directory)
		{
			DirectoryPassedToLogDeletedDirectory = directory;
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
	}
}


