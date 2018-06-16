//
// FileWatcherService.cs
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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public static class FileWatcherService
	{
		static readonly Dictionary<FilePath, FileWatcherWrapper> watchers = new Dictionary<FilePath, FileWatcherWrapper> ();
		static readonly List<WorkspaceItem> workspaceItems = new List<WorkspaceItem> ();
		static readonly Dictionary<object, List<FilePath>> monitoredDirectories = new Dictionary<object, List<FilePath>> ();
		static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();

		public static Task Add (WorkspaceItem item)
		{
			lock (watchers) {
				workspaceItems.Add (item);
				item.RootDirectoriesChanged += OnRootDirectoriesChanged;
				CancelUpdate ();
				return UpdateWatchersAsync ();
			}
		}

		static void CancelUpdate ()
		{
			cancellationTokenSource.Cancel ();
			cancellationTokenSource = new CancellationTokenSource ();
		}

		public static Task Remove (WorkspaceItem item)
		{
			lock (watchers) {
				if (workspaceItems.Remove (item)) {
					item.RootDirectoriesChanged -= OnRootDirectoriesChanged;
					CancelUpdate ();
					return UpdateWatchersAsync ();
				}
			}
			return Task.CompletedTask;
		}

		static void OnRootDirectoriesChanged (object sender, EventArgs e)
		{
			lock (watchers) {
				CancelUpdate ();
				UpdateWatchersAsync ().Ignore ();
			}
		}

		static Task UpdateWatchersAsync ()
		{
			CancellationToken token = cancellationTokenSource.Token;
			return Task.Run (() => {
				UpdateWatchers (workspaceItems, monitoredDirectories, token);
			});
		}

		static void UpdateWatchers (
			List<WorkspaceItem> currentWorkspaceItems,
			Dictionary<object, List<FilePath>> currentMonitoredDirectories,
			CancellationToken token)
		{
			List<FileWatcherWrapper> newWatchers = null;

			HashSet<FilePath> watchedDirectories = GetWatchedDirectories ();

			if (token.IsCancellationRequested)
				return;

			foreach (FilePath directory in GetRootDirectories (currentWorkspaceItems, currentMonitoredDirectories)) {
				if (!watchedDirectories.Remove (directory)) {
					if (Directory.Exists (directory)) {
						if (newWatchers == null)
							newWatchers = new List<FileWatcherWrapper> ();
						var watcher = new FileWatcherWrapper (directory);
						newWatchers.Add (watcher);
					}
				}
			}

			if (newWatchers == null && !watchedDirectories.Any ()) {
				// Unchanged.
				return;
			}

			lock (watchers) {
				if (token.IsCancellationRequested) {
					if (newWatchers != null) {
						foreach (FileWatcherWrapper watcher in newWatchers) {
							watcher.Dispose ();
						}
					}
					return;
				}

				// Remove file watchers no longer needed.
				foreach (FilePath directory in watchedDirectories) {
					Remove (directory);
				}

				if (newWatchers != null) {
					foreach (FileWatcherWrapper watcher in newWatchers) {
						watchers.Add (watcher.Path, watcher);
						watcher.EnableRaisingEvents = true;
					}
				}
			}
		}

		static HashSet<FilePath> GetWatchedDirectories ()
		{
			lock (watchers) {
				var directories = new HashSet<FilePath> ();
				foreach (FilePath directory in watchers.Keys) {
					directories.Add (directory);
				}
				return directories;
			}
		}

		static IEnumerable<FilePath> GetRootDirectories (
			List<WorkspaceItem> currentWorkspaceItems,
			Dictionary<object, List<FilePath>> currentMonitoredDirectories)
		{
			var directories = new HashSet<FilePath> ();

			foreach (WorkspaceItem item in currentWorkspaceItems) {
				foreach (FilePath directory in item.GetRootDirectories ()) {
					directories.Add (directory);
				}
			}

			foreach (var kvp in currentMonitoredDirectories) {
				foreach (var directory in kvp.Value)
					directories.Add (directory);
			}

			return Normalize (directories);
		}

		static void Remove (FilePath directory)
		{
			if (watchers.TryGetValue (directory, out FileWatcherWrapper watcher)) {
				watcher.EnableRaisingEvents = false;
				watcher.Dispose ();
				watchers.Remove (directory);
			}
		}

		internal static IEnumerable<FilePath> Normalize (IEnumerable<FilePath> directories)
		{
			var directorySet = new HashSet<FilePath> (directories);

			return directorySet.Where (d => {
				return directorySet.All (other => !d.IsChildPathOf (other));
			});
		}

		public static Task WatchDirectories (object id, IEnumerable<FilePath> directories)
		{
			lock (watchers) {
				if (directories == null)
					monitoredDirectories.Remove (id);
				else {
					directories = directories.Where (directory => !directory.IsNullOrEmpty);
					monitoredDirectories [id] = new List<FilePath> (directories);
				}

				CancelUpdate ();
				return UpdateWatchersAsync ();
			}
		}

		/// <summary>
		/// Used by unit tests to ensure the file watcher is up to date.
		/// </summary>
		internal static Task Update ()
		{
			lock (watchers) {
				CancelUpdate ();
				return UpdateWatchersAsync ();
			}
		}
	}

	class FileWatcherWrapper : IDisposable
	{
		FSW.FileSystemWatcher watcher;

		public FileWatcherWrapper (FilePath path)
		{
			Path = path;
			watcher = new FSW.FileSystemWatcher (path) {
				// Need LastWrite otherwise no file change events are generated by the native file watcher.
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
				IncludeSubdirectories = true,
				InternalBufferSize = 32768
			};

			watcher.Changed += OnFileChanged;
			watcher.Deleted += OnFileDeleted;
			watcher.Renamed += OnFileRenamed;
			watcher.Error += OnFileWatcherError;
		}

		public FilePath Path { get; }

		public bool EnableRaisingEvents {
			get { return watcher.EnableRaisingEvents; }
			set { watcher.EnableRaisingEvents = value; }
		}

		public void Dispose ()
		{
			watcher.Dispose ();
		}

		void OnFileChanged (object sender, FileSystemEventArgs e)
		{
			FileService.NotifyFileChanged (e.FullPath);
		}

		void OnFileDeleted (object sender, FileSystemEventArgs e)
		{
			// The native file watcher sometimes generates a Changed, Created and Deleted event in
			// that order from a single native file event. So check the file has been deleted before raising
			// a FileRemoved event.
			if (!File.Exists (e.FullPath))
				FileService.NotifyFileRemoved (e.FullPath);
		}

		/// <summary>
		/// File rename events have various problems.
		/// 1. They are sometimes raised out of order.
		/// 2. Sometimes the rename information is incorrect with the wrong file names being used.
		/// 3. Some applications use a rename to update the original file so these are turned into
		/// a change event and a remove event.
		/// </summary>
		void OnFileRenamed (object sender, RenamedEventArgs e)
		{
			// Some applications, such as TextEdit.app, will create a backup file
			// and then rename that to the original file. This results in no file
			// change event being generated by the file watcher. To handle this
			// a rename is treated as a file change for the destination file.
			FileService.NotifyFileChanged (e.FullPath);

			// Deleting a file with Finder will move the file to the ~/.Trashes
			// folder. To handle this a remove event is fired for the source
			// file being renamed. Also handle file events being received out of
			// order on saving a file in TextEdit.app - with a rename event of
			// the original file to the temp file being the last event even though
			// the original file still exists.
			if (File.Exists (e.OldFullPath))
				FileService.NotifyFileChanged (e.OldFullPath);
			else
				FileService.NotifyFileRemoved (e.OldFullPath);
		}

		void OnFileWatcherError (object sender, ErrorEventArgs e)
		{
			LoggingService.LogError ("FileService.FileWatcher error", e.GetException ());
		}
	}
}
