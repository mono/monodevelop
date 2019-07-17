//
// FileService.RegistrationHandler.cs
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
using System.Collections.Immutable;
using System.Diagnostics;

namespace MonoDevelop.Core
{
	public static partial class FileService
	{
		public sealed class RegistrationHandler
		{
			// Prefer one tree per notification kind, as opposed to batching them up, as it helps with dispatching events
			readonly ImmutableArray<FileWatcherInformation> watchMaps;

			internal RegistrationHandler ()
			{
				watchMaps = ImmutableArray.Create<FileWatcherInformation> (
					new FileWatcherInformation<FileEventArgs> (),		// Created
					new FileWatcherInformation<FileEventArgs> (),		// Changed
					new FileWatcherInformation<FileCopyEventArgs> (),	// Copied
					new FileWatcherInformation<FileCopyEventArgs> (),	// Moved
					new FileWatcherInformation<FileEventArgs> (),		// Removed
					new FileWatcherInformation<FileCopyEventArgs> ()	// Renamed
				);

				Debug.Assert (Enum.GetNames (typeof (EventDataKind)).Length == watchMaps.Length);
			}

			public IDisposable WatchCreated (FilePath path, Action<FileEventArgs> handler)
				=> GetMap<FileEventArgs> (EventDataKind.Created).WatchDirectory (path, handler);

			public IDisposable WatchRemoved (FilePath path, Action<FileEventArgs> handler)
				=> GetMap<FileEventArgs> (EventDataKind.Removed).WatchDirectory (path, handler);

			public IDisposable WatchRenamed (FilePath path, Action<FileCopyEventArgs> handler)
				=> GetMap<FileCopyEventArgs> (EventDataKind.Renamed).WatchDirectory (path, handler);

			internal void Notify<T> (EventDataKind kind, T args) where T:FileEventArgs
				=> GetMap<T> (kind).Notify (args);

			FileWatcherInformation<T> GetMap<T> (EventDataKind kind) where T:FileEventArgs
				=> (FileWatcherInformation<T>)watchMaps [(int)kind];
		}
	}
}
