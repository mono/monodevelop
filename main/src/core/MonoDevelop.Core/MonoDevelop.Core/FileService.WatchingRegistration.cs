//
// FileService.Watching.Registration.cs
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
		sealed class WatchingRegistration : IDisposable
		{
			PathTree tree;
			PathTreeNode node;

			// TODO: Maybe just pass a FilePath, cause each arg will contain items we are not looking at.
			Action<FileEventArgs> handler;

			public WatchingRegistration (PathTree tree, string path, Action<FileEventArgs> handler)
			{
				this.tree = tree;
				this.handler = handler;

				tree.AddNode (path, this);
			}

			public void Notify (FileEventArgs args) => handler.Invoke (args);

			public void Dispose ()
			{
				if (tree != null) {
					tree.RemoveNode (node, this);
					tree = null;

					node = null;
					handler = null;
				}
			}
		}
	}
}
