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
// FIXME: Has a few quirks, regarding watching directories that do not exist.
#if FSW_PERFORMANT_IMPLEMENTATION //MAC || WIN32
		class Logic
		{
			readonly Dictionary<FileWatcherWrapper, HashSet<object>> refCountedWatchers = new Dictionary<FileWatcherWrapper, HashSet<object>> ();
			readonly Dictionary<object, HashSet<FileWatcherWrapper>> monitoredDirectories = new Dictionary<object, HashSet<FileWatcherWrapper>> ();

			internal Task UpdateWatchersAsync ()
			{
				// NOP.
				return Task.CompletedTask;
			}

			internal Task WatchDirectories_NoLock (object id, IEnumerable<FilePath> directories)
			{
				HashSet<FilePath> set = null;
				if (directories != null)
					set = new HashSet<FilePath> (directories.Where (x => !x.IsNullOrEmpty));

				RegisterDirectoriesInTree_NoLock (id, set);
				return Task.CompletedTask;
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

			void RefWatcher (object id, FilePath path)
			{
				HashSet<object> objSet;
				// We already have a watcher here.
				if (!watchers.TryGetValue (path, out var watcher)) {
					watcher = new FileWatcherWrapper (path);

					try {
						watcher.EnableRaisingEvents = true;
					} catch (UnauthorizedAccessException e) {
						LoggingService.LogWarning ("Access to " + path + " denied. Stopping file watcher.", e);
						watcher.Dispose ();
						return;
					}

					refCountedWatchers [watcher] = objSet = new HashSet<object> ();
					watchers.Add (path, watcher);
				} else {
					objSet = refCountedWatchers [watcher];
				}

				objSet.Add (id);
			}

			void UnrefWatcher (object id, FileWatcherWrapper watcher)
			{
				var objSet = refCountedWatchers [watcher];
				if (!objSet.Remove (id))
					return;

				// The watcher refcount is zero.
				if (objSet.Count == 0) {
					RemoveWatcher_NoLock (watcher.Path);
				}
				return;
			}

			void ClearWatchers (object id)
			{
				if (!monitoredDirectories.TryGetValue (id, out var watchersForId))
					return;

				foreach (var watcher in watchersForId) {
					UnrefWatcher (id, watcher);
				}

				// Remove the id mapping
				monitoredDirectories.Remove (id);
			}

			void RegisterDirectoriesInTree_NoLock (object id, HashSet<FilePath> set)
			{
				Debug.Assert (Monitor.IsEntered (watchers));

				// unsubscribe fast-path removal
				if (set == null) {
					ClearWatchers (id);
					return;
				}

				HashSet<FilePath> toAdd = null;
				if (monitoredDirectories.TryGetValue (id, out var oldWatchers)) {
					var toRemove = new HashSet<FilePath> (oldWatchers.Select(x => x.Path));
					// Remove the old ones which are not in the new set.
					toRemove.ExceptWith (set);

					foreach (var path in toRemove) {
						var watcher = watchers [path];

						UnrefWatcher (id, watcher);
					}

					toAdd = new HashSet<FilePath> (set);
					toAdd.ExceptWith (oldWatchers.Select (x => x.Path));
				} else
					toAdd = set;

				// Unchanged will not be in this set, so just add new ones, by refing one
				foreach (var path in toAdd) {
					RefWatcher (id, path);
				}
			}
		}
#endif
	}
}
