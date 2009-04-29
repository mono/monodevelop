// 
// ISvnClient.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Subversion
{
	public interface ISvnClient: IDisposable
	{
		string Version { get; }
		IList List (string pathorurl, bool recurse, LibSvnClient.Rev revision);
		IList Status (string path, LibSvnClient.Rev revision);
		IList Status (string path, LibSvnClient.Rev revision, bool descendDirs, bool changedItemsOnly, bool remoteStatus);
		IList Log (string path, LibSvnClient.Rev revisionStart, LibSvnClient.Rev revisionEnd);
		string GetPathUrl (string path);
		string Cat (string pathorurl, LibSvnClient.Rev revision);
		void Cat (string pathorurl, LibSvnClient.Rev revision, Stream stream);
		void Update (string path, bool recurse, IProgressMonitor monitor);
		void Revert (string[] paths, bool recurse, IProgressMonitor monitor);
		void Resolve (string path, bool recurse, IProgressMonitor monitor);
		void Add (string path, bool recurse, IProgressMonitor monitor);
		void Checkout (string url, string path, LibSvnClient.Rev rev, bool recurse, IProgressMonitor monitor);
		void Commit (string[] paths, string message, IProgressMonitor monitor);
		void Mkdir (string[] paths, string message, IProgressMonitor monitor);
		void Delete (string path, bool force, IProgressMonitor monitor);
		void Move (string srcPath, string destPath, LibSvnClient.Rev revision, bool force, IProgressMonitor monitor);
		void Lock (IProgressMonitor monitor, string comment, bool stealLock, params string[] paths);
		void Unlock (IProgressMonitor monitor, bool breakLock, params string[] paths);
		string PathDiff (string path1, LibSvnClient.Rev revision1, string path2, LibSvnClient.Rev revision2, bool recursive);
		void RevertToRevision (string path, int revision);
		void RevertRevision (string path, int revision);
	}
}
