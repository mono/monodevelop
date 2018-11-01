//
// MonoDevelopMetadataReference.FileChangeTracker.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.TypeSystem
{
	partial class MonoDevelopMetadataReference
	{
		internal class FileChangeTracker : IDisposable
		{
			readonly Lazy<bool> initialized;

			public event EventHandler<FileEventArgs> UpdatedOnDisk;
			public FilePath Path { get; }

			public FileChangeTracker (FilePath path)
			{
				Path = path;

				initialized = new Lazy<bool> (() => {
					FileService.FileChanged += OnUpdatedOnDisk;
					FileWatcherService.WatchDirectories (this, new [] { Path });

					return true;
				});
			}

			public void EnsureSubscription ()
			{
				// This subscribes to the events.
				var _ = initialized.Value;
			}

			void OnUpdatedOnDisk (object sender, FileEventArgs args) => UpdatedOnDisk?.Invoke (this, args);

			// TODO: Maybe add a finalizer to check if we're not unsubscribing
			public void Dispose ()
			{
				FileWatcherService.WatchDirectories (this, null);
				FileService.FileChanged -= OnUpdatedOnDisk;
			}
		}
	}
}
