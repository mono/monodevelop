//
// VersionInfoCache.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;

namespace MonoDevelop.VersionControl
{
	class VersionInfoCache : IDisposable
	{
		ReaderWriterLockSlim fileLock = new ReaderWriterLockSlim();
		Dictionary<FilePath,VersionInfo> fileStatus = new Dictionary<FilePath, VersionInfo> ();
		ReaderWriterLockSlim directoryLock = new ReaderWriterLockSlim ();
		Dictionary<FilePath,DirectoryStatus> directoryStatus = new Dictionary<FilePath, DirectoryStatus> ();
		Repository repo;

		public VersionInfoCache (Repository repo)
		{
			this.repo = repo;
		}

		public void ClearCachedVersionInfo (FilePath rootPath)
		{
			var canonicalPath = rootPath.CanonicalPath;

			try {
				fileLock.EnterWriteLock ();
				foreach (var p in fileStatus.Where (e => e.Key.IsChildPathOf (rootPath) || e.Key == canonicalPath))
					p.Value.RequiresRefresh = true;
			} finally {
				fileLock.ExitWriteLock ();
			}

			try {
				directoryLock.EnterWriteLock ();
				foreach (var p in directoryStatus.Where (e => e.Key.IsChildPathOf (rootPath) || e.Key == canonicalPath))
					p.Value.RequiresRefresh = true;
			} finally {
				directoryLock.ExitWriteLock ();
			}
		}

		public VersionInfo GetStatus (FilePath localPath)
		{
			try {
				fileLock.EnterReadLock ();

				VersionInfo vi;
				fileStatus.TryGetValue (localPath, out vi);
				return vi;
			} finally {
				fileLock.ExitReadLock ();
			}
		}

		public DirectoryStatus GetDirectoryStatus (FilePath localPath)
		{
			try {
				directoryLock.EnterReadLock ();

				DirectoryStatus vis;
				if (directoryStatus.TryGetValue (localPath.CanonicalPath, out vis))
					return vis;
				return null;
			} finally {
				directoryLock.ExitReadLock ();
			}
		}

		public void SetStatus (VersionInfo versionInfo, bool notify = true)
		{
			try {
				fileLock.EnterWriteLock ();

				if (!versionInfo.IsInitialized)
					versionInfo.Init (repo);
				VersionInfo vi;
				if (fileStatus.TryGetValue (versionInfo.LocalPath, out vi) && vi.Equals (versionInfo)) {
					vi.RequiresRefresh = false;
					return;
				}
				fileStatus [versionInfo.LocalPath] = versionInfo;
			} finally {
				fileLock.ExitWriteLock ();
			}

			if (notify)
				VersionControlService.NotifyFileStatusChanged (new FileUpdateEventArgs (repo, versionInfo.LocalPath, versionInfo.IsDirectory));
		}

		public void SetStatus (IEnumerable<VersionInfo> versionInfos)
		{
			FileUpdateEventArgs args = null;

			try {
				fileLock.EnterWriteLock ();
				foreach (var versionInfo in versionInfos) {
					if (!versionInfo.IsInitialized)
						versionInfo.Init (repo);
					VersionInfo vi;
					if (fileStatus.TryGetValue (versionInfo.LocalPath, out vi) && vi.Equals (versionInfo)) {
						vi.RequiresRefresh = false;
						continue;
					}
					fileStatus [versionInfo.LocalPath] = versionInfo;
					var a = new FileUpdateEventArgs (repo, versionInfo.LocalPath, versionInfo.IsDirectory);
					if (args == null)
						args = a;
					else
						args.MergeWith (a);
				}
			} finally {
				fileLock.ExitWriteLock ();
			}
			if (args != null) {
			//	Console.WriteLine ("Notifying Status " + string.Join (", ", args.Select (p => p.FilePath.FullPath)));
				VersionControlService.NotifyFileStatusChanged (args);
			}
		}

		public void SetDirectoryStatus (FilePath localDirectory, VersionInfo[] versionInfos, bool hasRemoteStatus)
		{
			try {
				directoryLock.EnterWriteLock ();

				DirectoryStatus vis;
				if (directoryStatus.TryGetValue (localDirectory.CanonicalPath, out vis)) {
					if (versionInfos.Length == vis.FileInfo.Length && (hasRemoteStatus == vis.HasRemoteStatus)) {
						bool allEqual = true;
						for (int n = 0; n < versionInfos.Length; n++) {
							if (!versionInfos [n].Equals (vis.FileInfo [n])) {
								allEqual = false;
								break;
							}
						}
						if (allEqual) {
							vis.RequiresRefresh = false;
							return;
						}
					}
				}
				directoryStatus [localDirectory.CanonicalPath] = new DirectoryStatus { FileInfo = versionInfos, HasRemoteStatus = hasRemoteStatus };
				SetStatus (versionInfos);
			} finally {
				directoryLock.ExitWriteLock ();
			}
		}

		public void Dispose ()
		{
			if (fileLock != null) {
				fileLock.Dispose ();
				fileLock = null;
			}
			if (directoryLock != null) {
				directoryLock.Dispose ();
				directoryLock = null;
			}
			if (fileStatus != null) {
				fileStatus.Clear ();
				fileStatus = null;
			}
			if (directoryStatus != null) {
				directoryStatus.Clear ();
				directoryStatus = null;
			}
			repo = null;
		}
	}

	class DirectoryStatus
	{
		public VersionInfo[] FileInfo { get; set; }
		public bool HasRemoteStatus { get; set; }
		public bool RequiresRefresh { get; set; }
	}
}

