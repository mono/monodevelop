//
// FileChangeTracker.cs
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
using System.Diagnostics;
using System.IO;

namespace MonoDevelop.FSW
{
	/// <summary>
	/// Helper class for working with FileSystemWatcher to observe any change on single file.
	/// </summary>
	public class FileChangeTracker : IDisposable
	{
		public event EventHandler UpdatedOnDisk;

		FileSystemWatcher watcher;

		public string FilePath { get; }

		public FileChangeTracker (string filePath)
		{
			FilePath = filePath;
			watcher = new FileSystemWatcher (Path.GetDirectoryName (filePath), Path.GetFileName (filePath)) {
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
			};

			watcher.Changed += OnChanged;
			watcher.Created += OnChanged;
			watcher.Deleted += OnChanged;
			watcher.Renamed += OnRenamed;
			watcher.EnableRaisingEvents = true;

			Debug.Assert (watcher.EnableRaisingEvents);
		}


		void OnRenamed (object sender, RenamedEventArgs e)
		{
			UpdatedOnDisk?.Invoke (this, e);
		}

		void OnChanged (object sender, FileSystemEventArgs e)
		{
			UpdatedOnDisk?.Invoke (this, e);
		}

		public void Dispose ()
		{
			if (watcher != null) {
				watcher.Changed -= OnChanged;
				watcher.Created -= OnChanged;
				watcher.Deleted -= OnChanged;
				watcher.Renamed -= OnRenamed;
				watcher.Dispose ();
				watcher = null;
			}
		}
	}
}
