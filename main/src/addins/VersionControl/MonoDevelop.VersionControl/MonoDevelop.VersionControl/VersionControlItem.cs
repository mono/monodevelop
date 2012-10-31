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
		FilePath path;
		bool isDirectory;
		IWorkspaceObject workspaceObject;
		Repository repository;
		VersionInfo versionInfo;

		public VersionControlItem (Repository repository, IWorkspaceObject workspaceObject, FilePath path, bool isDirectory, VersionInfo versionInfo)
		{
			this.path = path;
			this.repository = repository;
			this.workspaceObject = workspaceObject;
			this.isDirectory = isDirectory;
			this.versionInfo = versionInfo;
		}
		
		public IWorkspaceObject WorkspaceObject {
			get {
				return workspaceObject;
			}
		}
		
		public Repository Repository {
			get {
				return repository;
			}
		}
		
		public FilePath Path {
			get {
				return path;
			}
		}
		
		public bool IsDirectory {
			get {
				return isDirectory;
			}
		}
		
		public VersionInfo VersionInfo {
			get {
				if (versionInfo == null) {
					try {
						versionInfo = repository.GetVersionInfo (path);
						if (versionInfo == null)
							versionInfo = new VersionInfo (path, "", isDirectory, VersionStatus.Unversioned, null, VersionStatus.Unversioned, null);
					} catch (Exception ex) {
						LoggingService.LogError ("Version control query failed", ex);
						versionInfo = VersionInfo.CreateUnversioned (path, isDirectory);
					}
				}
				return versionInfo;
			}
		}
	}
}
