//
// FileWatcherService.PerformanceLogic.cs
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.FSW;

namespace MonoDevelop.Projects
{
	public static partial class FileWatcherService
	{
#if !FSW_PERFORMANT_IMPLEMENTATION //!MAC && !WIN32
		class Logic
		{
			// We don't want more than 8 threads for FileSystemWatchers.
			const int maxWatchers = 8;

			readonly PathTree tree = new PathTree ();
			readonly Dictionary<object, HashSet<FilePath>> monitoredDirectories = new Dictionary<object, HashSet<FilePath>> ();

			HashSet<FilePath> newWatchers = new HashSet<FilePath> ();
			List<FilePath> toRemove = new List<FilePath> ();

			internal Task UpdateWatchersAsync ()
			{
				cancellationTokenSource.Cancel ();
				cancellationTokenSource = new CancellationTokenSource ();
				CancellationToken token = cancellationTokenSource.Token;

				return Task.Run (() => UpdateWatchers (token));
			}

			internal Task WatchDirectories_NoLock (object id, IEnumerable<FilePath> directories)
			{
				HashSet<FilePath> set = null;
				if (directories != null)
					set = new HashSet<FilePath> (directories.Where (x => !x.IsNullOrEmpty));

				if (RegisterDirectoriesInTree_NoLock (id, set))
					return UpdateWatchersAsync ();
				return Task.CompletedTask;
			}

			void UpdateWatchers (CancellationToken token)
			{
				if (token.IsCancellationRequested)
					return;
				lock (watchers) {
					if (token.IsCancellationRequested)
						return;
					newWatchers.Clear ();
					foreach (var node in tree.Normalize (maxWatchers)) {
						if (token.IsCancellationRequested)
							return;
						var dir = node.GetPath ().ToString ();
						if (Directory.Exists (dir))
							newWatchers.Add (dir);
					}
					if (newWatchers.Count == 0 && watchers.Count == 0) {
						// Unchanged.
						return;
					}
					toRemove.Clear ();
					foreach (var kvp in watchers) {
						var directory = kvp.Key;
						if (!newWatchers.Contains (directory))
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
					foreach (var path in newWatchers) {
						// Don't modify a watcher that already exists.
						if (watchers.ContainsKey (path)) {
							continue;
						}
						var watcher = new FileWatcherWrapper (path);
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
				Debug.Assert (Monitor.IsEntered (watchers));

				if (watchers.TryGetValue (directory, out FileWatcherWrapper watcher)) {
					watcher.EnableRaisingEvents = false;
					watcher.Dispose ();
					watchers.Remove (directory);
				}
			}


			bool RegisterDirectoriesInTree_NoLock (object id, HashSet<FilePath> set)
			{
				Debug.Assert (Monitor.IsEntered (watchers));

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
						var node = tree.RemoveNode (dir, id);

						bool wasRemoved = node != null && !node.IsLive;
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
		}
#endif
	}
}
