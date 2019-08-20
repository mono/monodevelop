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
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.VersionControl
{
	class VersionInfoCache : IDisposable
	{
		readonly ConcurrentDictionary<FilePath, VersionInfo> fileStatus = new ConcurrentDictionary<FilePath, VersionInfo> ();
		readonly ConcurrentDictionary<FilePath, DirectoryStatus> directoryStatus = new ConcurrentDictionary<FilePath, DirectoryStatus> ();
		Repository repo;

		public VersionInfoCache (Repository repo)
		{
			this.repo = repo;
		}

		public void ClearCachedVersionInfo (FilePath rootPath)
		{
			FileUpdateEventArgs args = null;
			var canonicalPath = rootPath.CanonicalPath;

			foreach (var p in fileStatus.Where (e => e.Key.IsChildPathOf (rootPath) || e.Key == canonicalPath)) {
				p.Value.RequiresRefresh = true;

				var a = new FileUpdateEventArgs (repo, p.Value.LocalPath, p.Value.IsDirectory);
				if (args == null)
					args = a;
				else
					args.MergeWith (a);
			}

			foreach (var p in directoryStatus.Where (e => e.Key.IsChildPathOf (rootPath) || e.Key == canonicalPath)) {
				p.Value.RequiresRefresh = true;
			}

			if (args != null) {
				//	Console.WriteLine ("Notifying Status " + string.Join (", ", args.Select (p => p.FilePath.FullPath)));
				VersionControlService.NotifyFileStatusChanged (args);
			}
		}

		public VersionInfo GetStatus (FilePath localPath)
		{
			fileStatus.TryGetValue (localPath, out var vi);

			return vi;
		}

		public DirectoryStatus GetDirectoryStatus (FilePath localPath)
		{
			if (directoryStatus.TryGetValue (localPath.CanonicalPath, out var vis))
				return vis;
			return null;
		}

		public async Task SetStatusAsync (VersionInfo versionInfo, bool notify = true, CancellationToken cancellationToken = default)
		{
			if (!versionInfo.IsInitialized)
				await versionInfo.InitAsync (repo, cancellationToken);

			if (fileStatus.TryGetValue (versionInfo.LocalPath, out var vi) && vi.Equals (versionInfo)) {
				vi.RequiresRefresh = false;
				return;
			}

			fileStatus [versionInfo.LocalPath] = versionInfo;

			if (notify)
				VersionControlService.NotifyFileStatusChanged (new FileUpdateEventArgs (repo, versionInfo.LocalPath, versionInfo.IsDirectory));
		}

		public async Task SetStatusAsync (IEnumerable<VersionInfo> versionInfos, CancellationToken cancellationToken = default)
		{
			FileUpdateEventArgs args = null;

			foreach (var versionInfo in versionInfos) {
				if (!versionInfo.IsInitialized)
					await versionInfo.InitAsync (repo, cancellationToken);

				if (fileStatus.TryGetValue (versionInfo.LocalPath, out var vi) && vi.Equals (versionInfo)) {
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

			if (args != null) {
				//	Console.WriteLine ("Notifying Status " + string.Join (", ", args.Select (p => p.FilePath.FullPath)));
				VersionControlService.NotifyFileStatusChanged (args);
			}
		}

		public async Task SetDirectoryStatusAsync (FilePath localDirectory, VersionInfo [] versionInfos, bool hasRemoteStatus, CancellationToken cancellationToken = default)
		{
			if (directoryStatus.TryGetValue (localDirectory.CanonicalPath, out var vis)) {
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
			await SetStatusAsync (versionInfos, cancellationToken: cancellationToken);
		}

		public void Dispose ()
		{
			if (fileStatus != null) {
				fileStatus.Clear ();
			}
			if (directoryStatus != null) {
				directoryStatus.Clear ();
			}
			repo = null;
		}
	}

	class DirectoryStatus
	{
		public VersionInfo [] FileInfo { get; set; }
		public bool HasRemoteStatus { get; set; }
		public bool RequiresRefresh { get; set; }
	}
}