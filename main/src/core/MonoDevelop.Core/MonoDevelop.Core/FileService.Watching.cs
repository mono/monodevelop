//
// FileService.Watching.cs
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
using MonoDevelop.FSW;

namespace MonoDevelop.Core
{
	public static partial class FileService
	{
		public sealed class WatchingHandler
		{
			readonly PathTree watchingTree = new PathTree ();

			internal WatchingHandler () {}

			public IDisposable WatchForFilesCreated (string path, Action<FileEventArgs> handler)
			{
				// 2 options:
				// * one tree per notification kind - more memory but fewer traversal iterations when checking whether to notify
				// * use the same tree, register the notification kind in the registration/id.
				return new WatchingRegistration (watchingTree, path, handler);
			}

			internal void NotifyIfNeeded (string path)
			{
				// Find whether this path is being watched
				// Algorithm options:
				// Find child segment equal to ours - 1)
				// - found	->
				// - * still have children? advance segment, repeat 1)
				// - * no children? notify
				// - not found -> don't notify

				// Worst case - O(N_edges)
			}
		}
	}
}
