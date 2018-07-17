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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.FSW;

namespace MonoDevelop.Projects
{
	public static class FileWatcherService
	{
		// We don't want more than 8 threads for FileSystemWatchers.
		const int maxWatchers = 8;

		static readonly PathTree tree = new PathTree ();
		static readonly Dictionary<FilePath, FileWatcherWrapper> watchers = new Dictionary<FilePath, FileWatcherWrapper> ();
		static readonly Dictionary<object, HashSet<FilePath>> monitoredDirectories = new Dictionary<object, HashSet<FilePath>> ();
		static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();

		public static Task Add (WorkspaceItem item)
		{
			lock (watchers) {
				item.RootDirectoriesChanged += OnRootDirectoriesChanged;
				return WatchDirectories (item, item.GetRootDirectories ());
			}
		}

		public static Task Remove (WorkspaceItem item)
		{
			lock (watchers) {
				item.RootDirectoriesChanged -= OnRootDirectoriesChanged;
				return WatchDirectories (item, null);
			}
		}

		static void OnRootDirectoriesChanged (object sender, EventArgs args)
		{
			lock (watchers) {
				var item = (WorkspaceItem)sender;
				WatchDirectories (item, item.GetRootDirectories ()).Ignore ();
			}
		}

		static Task UpdateWatchersAsync ()
		{
			cancellationTokenSource.Cancel ();
			cancellationTokenSource = new CancellationTokenSource ();
			CancellationToken token = cancellationTokenSource.Token;

			return Task.Run (() => UpdateWatchers (token));
		}

		static void UpdateWatchers (CancellationToken token)
		{
			if (token.IsCancellationRequested)
				return;

			lock (watchers) {
				if (token.IsCancellationRequested)
					return;

				var newPathsToWatch = tree.Normalize (maxWatchers).Select (node => (FilePath)node.FullPath);
				var newWatchers = new HashSet<FilePath> (newPathsToWatch.Where (dir => Directory.Exists (dir)));
				if (newWatchers.Count == 0 && watchers.Count == 0) {
					// Unchanged.
					return;
				}

				List<FilePath> toRemove;
				if (newWatchers.Count == 0)
					toRemove = watchers.Keys.ToList ();
				else {
					toRemove = new List<FilePath> ();
					foreach (var kvp in watchers) {
						var directory = kvp.Key;
						if (!newWatchers.Contains (directory))
							toRemove.Add (directory);
					}
				}

				// After this point, the watcher update is real and a destructive operation, so do not use the token.
				if (token.IsCancellationRequested)
					return;
				
				// First remove the watchers, so we don't spin too many threads.
				foreach (var directory in toRemove) {
					RemoveWatcher_NoLock (directory);
				}

				// Add the new ones.
				if (newWatchers.Count == 0)
					return;

				foreach (var path in newWatchers) {
					// Don't modify a watcher that already exists.
					if (watchers.ContainsKey (path)) {
						continue;
					}

					var watcher = new FileWatcherWrapper (path);
					watchers.Add (path, watcher);
					watcher.EnableRaisingEvents = true;
				}

			}
		}

		static void RemoveWatcher_NoLock (FilePath directory)
		{
			Debug.Assert (Monitor.IsEntered (watchers));

			if (watchers.TryGetValue (directory, out FileWatcherWrapper watcher)) {
				watcher.EnableRaisingEvents = false;
				watcher.Dispose ();
				watchers.Remove (directory);
			}
		}

		public static Task WatchDirectories (object id, IEnumerable<FilePath> directories)
		{
			lock (watchers) {
				if (RegisterDirectoriesInTree_NoLock (id, directories))
					return UpdateWatchersAsync ();
				return Task.CompletedTask;
			}
		}

		static bool RegisterDirectoriesInTree_NoLock (object id, IEnumerable<FilePath> directories)
		{
			Debug.Assert (Monitor.IsEntered (watchers));

			// Remove paths subscribed for this id.
			// TODO: Only modify those which need to be modified, don't register/unregister with no reason.

			bool modified = false;

			if (monitoredDirectories.TryGetValue (id, out var oldDirectories)) {
				foreach (var dir in oldDirectories) {
					var node = tree.RemoveNode (dir, id);

					bool wasRemoved = node != null && !node.IsLive;
					modified |= wasRemoved;
				}
			}

			// Remove the current registered directories
			monitoredDirectories.Remove (id);
			if (directories == null)
				return modified;

			// Apply new ones if we have any
			var set = new HashSet<FilePath> (directories.Where(x => !x.IsNullOrEmpty));

			if (set.Count > 0) {
				monitoredDirectories [id] = set;
				foreach (var path in set) {
					tree.AddNode (path, id);
				}

				// If we reached here, we have added at least 1 node, and the tree has changed.
				modified = true;
			}
			return modified;
		}

		/// <summary>
		/// Used by unit tests to ensure the file watcher is up to date.
		/// </summary>
		internal static Task Update ()
		{
			lock (watchers) {
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
