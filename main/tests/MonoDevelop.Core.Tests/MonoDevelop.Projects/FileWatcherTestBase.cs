//
// FileWatcherTestBase.cs
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	public abstract class FileWatcherTestBase : TestBase
	{
		protected List<FileEventInfo> fileChanges;
		protected List<FileEventInfo> filesRemoved;
		protected TaskCompletionSource<bool> fileChangesTask;
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
			OnTestTearDown ();

			FileService.FileChanged -= OnFileChanged;
			FileService.FileRemoved -= OnFileRemoved;
		}

		protected virtual void OnTestTearDown ()
		{
		}

		protected void ClearFileEventsCaptured ()
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

		protected void AssertFileChanged (FilePath fileName)
		{
			var files = fileChanges.Select (fileChange => fileChange.FileName);
			Assert.That (files, Contains.Item (fileName));
		}

		protected Task WaitForFileChanged (FilePath fileName, int millisecondsTimeout = 2000)
		{
			return WaitForFilesChanged (new [] { fileName }, millisecondsTimeout);
		}

		/// <summary>
		/// File change events seem to happen out of order sometimes so allow the tests to
		/// specify a set of files.
		/// </summary>
		protected Task WaitForFilesChanged (FilePath [] fileNames, int millisecondsTimeout = 2000)
		{
			waitingForFileChangeFileNames = new List<FilePath> (fileNames);
			return Task.WhenAny (Task.Delay (millisecondsTimeout), fileChangesTask.Task);
		}

		protected void AssertFileRemoved (FilePath fileName)
		{
			var files = filesRemoved.Select (fileChange => fileChange.FileName);
			Assert.That (files, Contains.Item (fileName));
		}

		protected Task WaitForFileRemoved (FilePath fileName, int millisecondsTimeout = 2000)
		{
			return WaitForFilesRemoved (new [] { fileName }, millisecondsTimeout);
		}

		/// <summary>
		/// File remove events seem to happen out of order sometimes so allow the tests to
		/// specify a set of files.
		/// </summary>
		protected Task WaitForFilesRemoved (FilePath [] fileNames, int millisecondsTimeout = 2000)
		{
			waitingForFilesToBeRemoved = new List<FilePath> (fileNames);
			return Task.WhenAny (Task.Delay (millisecondsTimeout), fileRemovedTask.Task);
		}

		// Ensures the FileService.FileChanged event fires for the project's FileStatusTracker
		// first so the WaitForFileChanged method finishes after the project has updated its
		// NeedsReload property.
		protected void ResetFileServiceChangedEventHandler ()
		{
			FileService.FileChanged -= OnFileChanged;
			FileService.FileChanged += OnFileChanged;
		}
	}
}
