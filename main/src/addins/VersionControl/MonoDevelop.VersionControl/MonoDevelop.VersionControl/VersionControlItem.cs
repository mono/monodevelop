// VersionControlItem.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	public class VersionControlItem
	{
		VersionInfo versionInfo;

		public VersionControlItem (Repository repository, IWorkspaceObject workspaceObject, FilePath path, bool isDirectory, VersionInfo versionInfo)
		{
			Path = path;
			Repository = repository;
			WorkspaceObject = workspaceObject;
			IsDirectory = isDirectory;
			this.versionInfo = versionInfo;
		}
		
		public IWorkspaceObject WorkspaceObject {
			get;
			private set;
		}
		
		public Repository Repository {
			get;
			private set;
		}
		
		public FilePath Path {
			get;
			private set;
		}
		
		public bool IsDirectory {
			get;
			private set;
		}
		
		public VersionInfo VersionInfo {
			get {
				if (versionInfo == null) {
					try {
						versionInfo = Repository.GetVersionInfo (Path, VersionInfoQueryFlags.IgnoreCache);
						if (versionInfo == null)
							versionInfo = new VersionInfo (Path, "", IsDirectory, VersionStatus.Unversioned, null, VersionStatus.Unversioned, null);
					} catch (Exception ex) {
						LoggingService.LogError ("Version control query failed", ex);
						versionInfo = VersionInfo.CreateUnversioned (Path, IsDirectory);
					}
				}
				return versionInfo;
			}
		}
	}
}
