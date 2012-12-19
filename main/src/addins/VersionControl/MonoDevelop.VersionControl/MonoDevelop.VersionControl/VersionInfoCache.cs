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
using System;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.VersionControl
{
	class VersionInfoCache
	{
		Dictionary<FilePath,VersionInfo> fileStatus = new Dictionary<FilePath, VersionInfo> ();
		Dictionary<FilePath,DirectoryStatus> directoryStatus = new Dictionary<FilePath, DirectoryStatus> ();
		Repository repo;

		public VersionInfoCache (Repository repo)
		{
			this.repo = repo;
		}

		public void ClearCachedVersionInfo (FilePath rootPath)
		{
			rootPath = rootPath.CanonicalPath;
			lock (fileStatus) {
				foreach (var p in fileStatus.Where (e => e.Key.IsChildPathOf (rootPath) || e.Key == rootPath).ToArray ())
					p.Value.RequiresRefresh = true;
			}
			lock (directoryStatus) {
				foreach (var p in directoryStatus.Where (e => e.Key.IsChildPathOf (rootPath) || e.Key == rootPath).ToArray ())
					p.Value.RequiresRefresh = true;
			}
		}

		public VersionInfo GetStatus (FilePath localPath)
		{
			lock (fileStatus) {
				VersionInfo vi;
				fileStatus.TryGetValue (localPath.CanonicalPath, out vi);
				return vi;
			}
		}

		public DirectoryStatus GetDirectoryStatus (FilePath localPath)
		{
			lock (fileStatus) {
				DirectoryStatus vis;
				if (directoryStatus.TryGetValue (localPath.CanonicalPath, out vis))
					return vis;
				else
					return null;
			}
		}

		public void SetStatus (VersionInfo versionInfo, bool notify = true)
		{
			lock (fileStatus) {
				VersionInfo vi;
				if (fileStatus.TryGetValue (versionInfo.LocalPath, out vi) && vi.Equals (versionInfo)) {
					vi.RequiresRefresh = false;
					return;
				}
				versionInfo.Init (repo);
				fileStatus [versionInfo.LocalPath.CanonicalPath] = versionInfo;
			}
			if (notify)
				VersionControlService.NotifyFileStatusChanged (new FileUpdateEventArgs (repo, versionInfo.LocalPath, versionInfo.IsDirectory));
		}

		public void SetStatus (IEnumerable<VersionInfo> versionInfos)
		{
			FileUpdateEventArgs args = null;
			lock (fileStatus) {
				foreach (var versionInfo in versionInfos) {
					VersionInfo vi;
					if (fileStatus.TryGetValue (versionInfo.LocalPath.CanonicalPath, out vi) && vi.Equals (versionInfo)) {
						vi.RequiresRefresh = false;
						continue;
					}
					versionInfo.Init (repo);
					fileStatus [versionInfo.LocalPath.CanonicalPath] = versionInfo;
					var a = new FileUpdateEventArgs (repo, versionInfo.LocalPath, versionInfo.IsDirectory);
					if (args == null)
						args = a;
					else
						args.MergeWith (a);
				}
			}
			if (args != null) {
			//	Console.WriteLine ("Notifying Status " + string.Join (", ", args.Select (p => p.FilePath.FullPath)));
				VersionControlService.NotifyFileStatusChanged (args);
			}
		}

		public void SetDirectoryStatus (FilePath localDirectory, VersionInfo[] versionInfos, bool hasRemoteStatus)
		{
			lock (directoryStatus) {
				DirectoryStatus vis;
				if (directoryStatus.TryGetValue (localDirectory.CanonicalPath, out vis)) {
					if (versionInfos.Length == vis.FileInfo.Length && (hasRemoteStatus == vis.HasRemoteStatus)) {
						bool allEqual = false;
						for (int n=0; n<versionInfos.Length; n++) {
							if (!versionInfos[n].Equals (vis.FileInfo[n])) {
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
				directoryStatus [localDirectory.CanonicalPath] = new DirectoryStatus () { FileInfo = versionInfos, HasRemoteStatus = hasRemoteStatus };
				SetStatus (versionInfos);
			}
		}
	}

	class DirectoryStatus
	{
		public VersionInfo[] FileInfo { get; set; }
		public bool HasRemoteStatus { get; set; }
		public bool RequiresRefresh { get; set; }
	}
}

