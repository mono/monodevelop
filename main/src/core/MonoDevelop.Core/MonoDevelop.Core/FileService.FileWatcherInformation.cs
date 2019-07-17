//
// FileWatcherInformation_Initial.cs
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
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using MonoDevelop.FSW;

namespace MonoDevelop.Core
{
	abstract class FileWatcherInformation
	{
		protected static readonly ObjectPool<Stopwatch> watchPool = ObjectPool.Create<Stopwatch> ();
		protected readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim (LockRecursionPolicy.NoRecursion);
		protected readonly PathTree pathTree = new PathTree ();
		protected long timings;
	}

	sealed class FileWatcherInformation<T> : FileWatcherInformation where T:FileEventArgs
	{
		internal void Notify (T args)
		{
			readerWriterLock.EnterReadLock ();
			var sw = watchPool.Get ();
			try {
				sw.Restart ();
				foreach (var arg in args) {
					var path = arg.FileName;

					// TODO: Optimize this so we do callback when looking up nodes, so we avoid useless work.

					// Find if the node has any registration, and if it does, recursively notify parents
					var node = pathTree.FindNodeContaining (path);
					if (node == null)
						continue;

					while (node != null) {
						if (node.IsLive) {
							foreach (WatchingRegistration id in node.Ids) {
								id.Notify (args);
							}
						}
						node = node.Parent;
					}
				}
				sw.Stop ();
			} finally {
				readerWriterLock.ExitReadLock ();

				// Add the timing results.
				Interlocked.Add (ref timings, sw.Elapsed.Ticks);
				watchPool.Return (sw);
			}
		}

		// TODO: Add a WatchFile, which monitors known files
		// This can help with the leakage of directory paths for different

		internal IDisposable WatchDirectory (FilePath path, Action<T> handler)
			=> new WatchingRegistration (this, path, handler);

		sealed class WatchingRegistration : IDisposable
		{
			FileWatcherInformation<T> parent;
			PathTreeNode node;

			// TODO: Maybe just pass a FilePath, cause each arg will contain items we are not looking at.
			Action<T> handler;

			public WatchingRegistration (FileWatcherInformation<T> parent, FilePath path, Action<T> handler)
			{
				this.parent = parent;
				this.handler = handler;

				parent.readerWriterLock.EnterWriteLock ();
				try {
					node = parent.pathTree.AddNode (path, this);
				} finally {
					parent.readerWriterLock.ExitWriteLock ();
				}
			}

			public void Notify (T args) => handler.Invoke (args);

			public void Dispose ()
			{
				if (parent != null) {
					parent.readerWriterLock.EnterWriteLock ();
					try {
						parent.pathTree.RemoveNode (node, this, out _);
					} finally {
						parent.readerWriterLock.ExitWriteLock ();
					}

					parent = null;
					node = null;
					handler = args => { };
				}
			}
		}
	}
}
