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
	public static partial class FileWatcherService
	{
		static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();
		static readonly Dictionary<FilePath, FileWatcherWrapper> watchers = new Dictionary<FilePath, FileWatcherWrapper> ();
		static readonly Logic logic = new Logic ();

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

		public static Task WatchDirectories (object id, IEnumerable<FilePath> directories)
		{
			lock (watchers) {
				return logic.WatchDirectories_NoLock (id, directories);
			}
		}

		/// <summary>
		/// Used by unit tests to ensure the file watcher is up to date.
		/// </summary>
		internal static Task Update ()
		{
			lock (watchers) {
				return logic.UpdateWatchersAsync ();
			}
		}
	}
}
