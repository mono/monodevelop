//
// EditorConfigService.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.CodingConventions;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor
{
	static class EditorConfigService
	{
		public readonly static string MaxLineLengthConvention = "max_line_length";
		readonly static object contextCacheLock = new object ();
		readonly static ICodingConventionsManager codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager (new ConventionsFileManager());
		static ImmutableDictionary<string, Task<ICodingConventionContext>> contextCache = ImmutableDictionary<string, Task<ICodingConventionContext>>.Empty;

		public static Task<ICodingConventionContext> GetEditorConfigContext (string fileName, CancellationToken token = default (CancellationToken))
		{
			if (string.IsNullOrEmpty (fileName))
				return TaskUtil.Default<ICodingConventionContext> ();
			lock (contextCacheLock) {
				if (contextCache.TryGetValue (fileName, out Task<ICodingConventionContext> result))
					return result;
				try {
					result = codingConventionsManager.GetConventionContextAsync (fileName, token);
				} catch (OperationCanceledException) {
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting coding conventions,", e);
				}
				if (result == null)
					return TaskUtil.Default<ICodingConventionContext> ();
				contextCache = contextCache.SetItem (fileName, result);
				return result;
			} 
		}

		public static async Task RemoveEditConfigContext (string fileName)
		{
			Task<ICodingConventionContext> ctx;
			lock (contextCacheLock) {
				if (!contextCache.TryGetValue (fileName, out ctx))
					return;
				contextCache = contextCache.Remove(fileName);
			}
			if (ctx != null)
				(await ctx).Dispose ();
		}

		class ConventionsFileManager : IFileWatcher
		{
			Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher> ();

			public event ConventionsFileChangedAsyncEventHandler ConventionFileChanged;
			public event ContextFileMovedAsyncEventHandler ContextFileMoved;

			public ConventionsFileManager ()
			{
				FileService.FileChanged += FileService_FileChanged;
				FileService.FileRemoved += FileService_FileRemoved;
				FileService.FileMoved += FileService_FileMoved;
				FileService.FileRenamed += FileService_FileMoved;
			}

			void FileService_FileMoved (object sender, FileCopyEventArgs e)
			{
				foreach (var file in e) {
					if (watchers.TryGetValue (file.SourceFile, out FileSystemWatcher watcher)) {
						ContextFileMoved?.Invoke (this, new ContextFileMovedEventArgs (file.SourceFile, file.TargetFile));
						watcher.Dispose ();
						watchers.Remove (file.SourceFile);
						StartWatching (file.TargetFile.FileName, file.TargetFile.ParentDirectory);
					}
				}
			}

			void FileService_FileChanged (object sender, FileEventArgs e)
			{
				foreach (var file in e) {
					if (watchers.TryGetValue (file.FileName, out FileSystemWatcher watcher)) {
						ConventionFileChanged?.Invoke (this, new ConventionsFileChangeEventArgs (watcher.Filter, watcher.Path, ChangeType.FileModified));
					}
				}
			}

			void FileService_FileRemoved (object sender, FileEventArgs e)
			{
				lock (watchers) {
					foreach (var file in e) {
						if (watchers.TryGetValue (file.FileName, out FileSystemWatcher watcher)) {
							ConventionFileChanged?.Invoke (this, new ConventionsFileChangeEventArgs (watcher.Filter, watcher.Path, ChangeType.FileDeleted));
							watcher.Dispose ();
							watchers.Remove (file.FileName);
						}
					}
				}
			}

			public void Dispose ()
			{
				FileService.FileMoved -= FileService_FileMoved;
				FileService.FileRenamed -= FileService_FileMoved;
				FileService.FileRemoved -= FileService_FileRemoved;
				FileService.FileChanged -= FileService_FileChanged;
				lock (watchers) {
					foreach (var kv in watchers)
						kv.Value.Dispose ();
					watchers = null;
				}
			}

			void OnChanged (object source, FileSystemEventArgs e)
			{
				var watcher = (FileSystemWatcher)source;
				ConventionFileChanged?.Invoke (this, new ConventionsFileChangeEventArgs (watcher.Filter, watcher.Path, GetChangeType(e.ChangeType)));
			}

			static ChangeType GetChangeType(WatcherChangeTypes type)
			{
				switch (type) {
				case WatcherChangeTypes.Changed:
					return ChangeType.FileModified;
				case WatcherChangeTypes.Deleted:
					return ChangeType.FileDeleted;
				}
				return ChangeType.FileModified;
			}

			public void StartWatching (string fileName, string directoryPath)
			{
				lock (watchers) {
					var key = directoryPath + Path.DirectorySeparatorChar.ToString () + fileName;

					if (watchers.ContainsKey (key))
						return;

					var watcher = new FileSystemWatcher ();
					watcher.Path = directoryPath;
					watcher.Filter = fileName;
					watcher.Changed += OnChanged;
					watcher.Deleted += OnChanged;
					watcher.EnableRaisingEvents = true;
					watchers.Add (key, watcher);
				}
			}

			public void StopWatching (string fileName, string directoryPath)
			{
				lock (watchers) {
					var key = directoryPath + Path.DirectorySeparatorChar.ToString () + fileName;
					if (watchers.TryGetValue (key, out FileSystemWatcher watcher)) {
						watcher.Dispose ();
						watchers.Remove (key);
					}
				}
			}
		}
	}
}
