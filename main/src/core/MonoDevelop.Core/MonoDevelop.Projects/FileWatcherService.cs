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
using Microsoft.Extensions.ObjectPool;
using MonoDevelop.Core;
using MonoDevelop.FSW;

namespace MonoDevelop.Projects
{
	public static class FileWatcherService
	{
		// Keep previous behaviour with a maximum of 8 watchers for Linux, as it still uses one thread per watcher.
		// Use more watchers for finer grained directory notifications for other platforms.
		static readonly int maxWatchers = Platform.IsMac ? 32 : 8;

		static readonly PathTree tree = new PathTree ();
		static readonly Dictionary<FilePath, FileWatcherWrapper> watchers = new Dictionary<FilePath, FileWatcherWrapper> ();
		static readonly Dictionary<object, HashSet<FilePath>> monitoredDirectories = new Dictionary<object, HashSet<FilePath>> ();
		static readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim (LockRecursionPolicy.NoRecursion);
		static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();

		public static Task Add (WorkspaceItem item)
		{
			using (readerWriterLock.Write ()) {
				return Watch_NoLock (item, registerEvent: true);
			}
		}

		static Task Watch_NoLock (WorkspaceObject item, bool registerEvent)
		{
			Debug.Assert (readerWriterLock.IsWriteLockHeld);

			var toWatch = ComputeItems (item, registerEvent);

			bool modified = false;
			foreach (var (id, set) in toWatch) {
				modified |= RegisterDirectoriesInTree_NoLock (id, set);
			}

			return modified ? UpdateWatchersAsync () : Task.CompletedTask;
		}

		internal static List<(object id, HashSet<FilePath> set)> ComputeItems (WorkspaceObject item, bool registerEvent)
		{
			var toAdd = new List<(object, HashSet<FilePath>)> ();

			foreach (var toRegister in item.GetAllItems<WorkspaceObject> ()) {
				if (registerEvent && toRegister is WorkspaceItem workspaceItem) {
					workspaceItem.RootDirectoriesChanged += OnRootDirectoriesChanged;
				}

				toAdd.Add ((toRegister, GetPathsToWatch (toRegister)));
			}

			return toAdd;
		}

		internal static HashSet<FilePath> GetPathsToWatch (WorkspaceObject item)
		{
			HashSet<FilePath> set = null;
			if (ShouldAddToSet (set, item.ItemDirectory)) {
				set ??= new HashSet<FilePath> ();
				set.Add (item.ItemDirectory);
			}

			if (item is IWorkspaceFileObject container) {
				foreach (var file in container.GetItemFiles (true)) {
					if (ShouldAddToSet (set, file)) {
						set ??= new HashSet<FilePath> ();
						set.Add (file.ParentDirectory);
					}
				}
			}

			return set;

			static bool ShouldAddToSet (HashSet<FilePath> set, FilePath path)
			{
				// Do a double lookup instead of iterating everything first, if we already added a project.
				if (path.IsNullOrEmpty)
					return false;

				if (set != null) {
					foreach (var directory in set) {
						if (path.IsChildPathOf (directory))
							return false;
					}
				}
				return true;
			}
		}

		public static Task Remove (WorkspaceItem item)
		{
			using (readerWriterLock.Write ()) {
				return Remove_NoLock (item);
			}
		}

		static Task Remove_NoLock (WorkspaceObject item)
		{
			Debug.Assert (readerWriterLock.IsWriteLockHeld);

			bool modified = false;
			foreach (var child in item.GetAllItems<WorkspaceObject> ()) {
				modified |= RegisterDirectoriesInTree_NoLock (child, null);

				if (child is WorkspaceItem workspaceItem)
					workspaceItem.RootDirectoriesChanged -= OnRootDirectoriesChanged;
			}
			return modified ? UpdateWatchersAsync () : Task.CompletedTask;
		}

		static void OnRootDirectoriesChanged (object sender, WorkspaceItem.RootDirectoriesChangedEventArgs args)
		{
			using (readerWriterLock.Write ()) {
				if (args.SourceItem is WorkspaceObject item) {
					if (args.IsRemove) {
						Remove_NoLock (item).Ignore ();
					} else {
						Watch_NoLock (item, args.IsAdd).Ignore ();
					}
				}
			}
		}

		static Task UpdateWatchersAsync ()
		{
			cancellationTokenSource.Cancel ();
			cancellationTokenSource = new CancellationTokenSource ();
			CancellationToken token = cancellationTokenSource.Token;

			return Task.Run (() => UpdateWatchers (token));
		}
		static Dictionary<FilePath, PathTreeNode> newWatchers = new Dictionary<FilePath, PathTreeNode>();
		static List<FilePath> toRemove = new List<FilePath> ();

		static void UpdateWatchers (CancellationToken token)
		{
			if (token.IsCancellationRequested)
				return;
			using (readerWriterLock.Write ()) {
				if (token.IsCancellationRequested)
					return;
				newWatchers.Clear ();
				foreach (var node in tree.Normalize (maxWatchers)) {
					if (token.IsCancellationRequested)
						return;
					var dir = node.GetPath ().ToString ();
					if (Directory.Exists (dir))
						newWatchers.Add (dir, node);
				}
				if (newWatchers.Count == 0 && watchers.Count == 0) {
					// Unchanged.
					return;
				}
				toRemove.Clear ();
				foreach (var kvp in watchers) {
					var directory = kvp.Key;
					if (!newWatchers.ContainsKey (directory))
						toRemove.Add (directory);
				}

				// After this point, the watcher update is real and a destructive operation, so do not use the token.
				if (token.IsCancellationRequested)
					return;

				// First remove the watchers, so we don't spin too many threads.
				foreach (var directory in toRemove) {
					RemoveWatcher_NoLock (directory);
				}

				// Add the new ones.
				foreach (var kvp in newWatchers) {
					var path = kvp.Key;
					// Don't modify a watcher that already exists.
					if (watchers.ContainsKey (path)) {
						continue;
					}
					var watcher = new FileWatcherWrapper (path, kvp.Value, readerWriterLock);
					watchers.Add (path, watcher);
					try {
						watcher.EnableRaisingEvents = true;
 					} catch (UnauthorizedAccessException e) {
						LoggingService.LogWarning ("Access to " + path + " denied. Stopping file watcher.", e);
						watcher.Dispose ();
						watchers.Remove (path);
					}
				}

			}
		}

		static void RemoveWatcher_NoLock (FilePath directory)
		{
			Debug.Assert (readerWriterLock.IsWriteLockHeld);

			if (watchers.TryGetValue (directory, out FileWatcherWrapper watcher)) {
				watcher.EnableRaisingEvents = false;
				watcher.Dispose ();
				watchers.Remove (directory);
			}
		}

		public static Task WatchDirectories (object id, IEnumerable<FilePath> directories)
		{
			using (readerWriterLock.Write ()) {
				return WatchDirectories_NoLock (id, directories);
			}
		}

		static Task WatchDirectories_NoLock (object id, IEnumerable<FilePath> directories)
		{
			Debug.Assert (readerWriterLock.IsWriteLockHeld);

			HashSet<FilePath> set = null;
			if (directories != null)
				set = new HashSet<FilePath> (directories.Where (x => !x.IsNullOrEmpty));

			if (RegisterDirectoriesInTree_NoLock (id, set))
				return UpdateWatchersAsync ();
			return Task.CompletedTask;
		}

		static bool RegisterDirectoriesInTree_NoLock (object id, HashSet<FilePath> set)
		{
			Debug.Assert (readerWriterLock.IsWriteLockHeld);

			// Remove paths subscribed for this id.

			bool modified = false;

			if (monitoredDirectories.TryGetValue (id, out var oldDirectories)) {
				HashSet<FilePath> toRemove = null;
				if (set != null) {
					toRemove = new HashSet<FilePath> (oldDirectories);
					// Remove the old ones which are not in the new set.
					toRemove.ExceptWith (set);
				} else
					toRemove = oldDirectories;

				foreach (var dir in toRemove) {
					var node = tree.RemoveNode (dir, id, out bool wasRemoved);
					modified |= wasRemoved;
				}
			}

			// Remove the current registered directories
			monitoredDirectories.Remove (id);
			if (set == null)
				return modified;

			HashSet<FilePath> toAdd = null;
			if (oldDirectories != null) {
				toAdd = new HashSet<FilePath> (set);
				toAdd.ExceptWith (oldDirectories);
			} else
				toAdd = set;

			// Apply new ones if we have any
			if (set.Count > 0) {
				monitoredDirectories [id] = set;
				foreach (var path in toAdd) {
					tree.AddNode (path, id, out bool isNew);

					// We have only modified the tree if there is any new pathtree node item added
					modified |= isNew;
				}
			}
			return modified;
		}

		/// <summary>
		/// Used by unit tests to ensure the file watcher is up to date.
		/// </summary>
		internal static Task Update ()
		{
			using (readerWriterLock.Write ()) {
				return UpdateWatchersAsync ();
			}
		}

		internal static FileChangeTimings Timings { get; } = new FileChangeTimings ();
	}

	internal class FileChangeTimings
	{
		readonly ObjectPool<Stopwatch> watchPool = ObjectPool.Create<Stopwatch> ();
		readonly long [] timings = new long [Enum.GetNames (typeof (FileService.EventDataKind)).Length];

		internal TimeSpan GetTimings (FileService.EventDataKind kind)
			=> TimeSpan.FromTicks (timings [(int)kind]);

		internal Stopwatch Get () => watchPool.Get ();

		internal void Add (Stopwatch sw, FileService.EventDataKind kind)
		{
			Interlocked.Add (ref timings [(int)kind], sw.Elapsed.Ticks);
			watchPool.Return (sw);
		}
	}

	sealed class FileWatcherWrapper : IDisposable
	{
		readonly FileSystemWatcher watcher;
		readonly PathTreeNode rootNode;
		readonly ReaderWriterLockSlim readerWriterLock;

		public FileWatcherWrapper (FilePath path, PathTreeNode rootNode, ReaderWriterLockSlim readerWriterLock)
		{
			Path = path;

			this.rootNode = rootNode;
			this.readerWriterLock = readerWriterLock;

			watcher = new FileSystemWatcher (path) {
				// Need LastWrite otherwise no file change events are generated by the native file watcher.
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
				IncludeSubdirectories = true,
				InternalBufferSize = 32768
			};

			watcher.Changed += OnFileChanged;
			watcher.Created += OnFileCreated;
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
			watcher.Changed -= OnFileChanged;
			watcher.Created -= OnFileCreated;
			watcher.Deleted -= OnFileDeleted;
			watcher.Renamed -= OnFileRenamed;
			watcher.Error -= OnFileWatcherError;
			watcher.Dispose ();
		}

		static void OnFileChanged (object sender, FileSystemEventArgs e)
		{
			FileService.NotifyFileChanged (e.FullPath);
		}

		void OnFileCreated (object sender, FileSystemEventArgs e)
		{
			using (readerWriterLock.Read ()) {
				var sw = FileWatcherService.Timings.Get ();
				sw.Restart ();
				NotifyNode (rootNode, e.FullPath, (id, path) => {
					if (id is Project project) {
						project.OnFileCreated (path);
					}
				});
				sw.Stop ();
				FileWatcherService.Timings.Add (sw, FileService.EventDataKind.Created);
			}

			FileService.NotifyFileCreated (e.FullPath);

			// The native file watcher sometimes generates a single Created event for a file when it is renamed
			// from a non-monitored directory to a monitored directory. So this is turned into a Changed
			// event so the file will be reloaded.
			FileService.NotifyFileChanged (e.FullPath);
		}

		void OnFileDeleted (object sender, FileSystemEventArgs e)
		{
			// The native file watcher sometimes generates a Changed, Created and Deleted event in
			// that order from a single native file event. So check the file has been deleted before raising
			// a FileRemoved event.
			if (!File.Exists (e.FullPath) && !Directory.Exists (e.FullPath)) {
				using (readerWriterLock.Read ()) {
					var sw = FileWatcherService.Timings.Get ();
					sw.Restart ();
					NotifyNode (rootNode, e.FullPath, (id, path) => {
						if (id is Project project)
							project.OnFileDeleted (path);
					});
					sw.Stop ();
					FileWatcherService.Timings.Add (sw, FileService.EventDataKind.Removed);
				}

				FileService.NotifyFileRemoved (e.FullPath);
			}
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
			using (readerWriterLock.Read ()) {
				var sw = FileWatcherService.Timings.Get ();
				sw.Restart ();
				NotifyNode (rootNode, (e.OldFullPath, e.FullPath), (id, state) => {
					if (id is Project project)
						project.OnFileRenamed (state.OldFullPath, state.FullPath);
				});
				sw.Stop ();
				FileWatcherService.Timings.Add (sw, FileService.EventDataKind.Renamed);
			}

			FileService.NotifyFileRenamedExternally (e.OldFullPath, e.FullPath);
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

		static void OnFileWatcherError (object sender, ErrorEventArgs e)
		{
			LoggingService.LogError ("FileService.FileWatcher error", e.GetException ());
		}

		static void NotifyNode<T> (PathTreeNode node, T value, Action<object, T> handler)
		{
			foreach (var id in node.Ids) {
				handler (id, value);
			}

			for (node = node.FirstChild; node != null; node = node.Next) {
				NotifyNode (node, value, handler);
			}
		}
	}
}
