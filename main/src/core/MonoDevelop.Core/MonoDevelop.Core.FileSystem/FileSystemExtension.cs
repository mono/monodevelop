// FileSystemExtension.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.Core.FileSystem
{
	public abstract class FileSystemExtension
	{
		FileSystemExtension next;
		
		internal FileSystemExtension Next {
			get { return next; }
			set { next = value; }
		}
		
		public abstract bool CanHandlePath (FilePath path, bool isDirectory);
		
		FileSystemExtension GetNextForPath (FilePath file, bool isDirectory)
		{
			FileSystemExtension nx = next;
			while (nx != null && !nx.CanHandlePath (file, isDirectory))
				nx = nx.next;
			return nx;
		}
		
		public virtual void CopyFile (FilePath source, FilePath dest, bool overwrite)
		{
			GetNextForPath (dest, false).CopyFile (source, dest, overwrite);
		}
		
		public virtual void RenameFile (FilePath file, string newName)
		{
			GetNextForPath (file, false).RenameFile (file, newName);
		}
		
		public virtual FilePath ResolveFullPath (FilePath path)
		{
			return GetNextForPath (path, true).ResolveFullPath (path);
		}
		
		public virtual void MoveFile (FilePath source, FilePath dest)
		{
			FileSystemExtension srcExt = GetNextForPath (source, false);
			FileSystemExtension dstExt = GetNextForPath (dest, false);
			
			if (srcExt == dstExt) {
				// Everything can be handled by the same file system
				srcExt.MoveFile (source, dest);
			} else {
				// If the file system of the source and dest files are
				// different, decompose the Move operation into a Copy
				// and Delete, so every file system can handle its part
				dstExt.CopyFile (source, dest, true);
				srcExt.DeleteFile (source);
			}
		}
		
		public virtual void DeleteFile (FilePath file)
		{
			GetNextForPath (file, false).DeleteFile (file);
		}
		
		public virtual void CreateDirectory (FilePath path)
		{
			GetNextForPath (path, true).CreateDirectory (path);
		}
		
		public virtual void CopyDirectory (FilePath sourcePath, FilePath destPath)
		{
			GetNextForPath (destPath, true).CopyDirectory (sourcePath, destPath);
		}
		
		public virtual void RenameDirectory (FilePath path, string newName)
		{
			GetNextForPath (path, true).RenameDirectory (path, newName);
		}
		
		public virtual void MoveDirectory (FilePath sourcePath, FilePath destPath)
		{
			FileSystemExtension srcExt = GetNextForPath (sourcePath, true);
			FileSystemExtension dstExt = GetNextForPath (destPath, true);
			
			if (srcExt == dstExt) {
				// Everything can be handled by the same file system
				srcExt.MoveDirectory (sourcePath, destPath);
			} else {
				// If the file system of the source and dest files are
				// different, decompose the Move operation into a Copy
				// and Delete, so every file system can handle its part
				dstExt.CopyDirectory (sourcePath, destPath);
				srcExt.DeleteDirectory (sourcePath);
			}
		}
		
		public virtual void DeleteDirectory (FilePath path)
		{
			GetNextForPath (path, true).DeleteDirectory (path);
		}

		public virtual void RequestFileEdit (IEnumerable<FilePath> files)
		{
#pragma warning disable 618
			foreach (var f in files) {
				if (!RequestFileEdit (f))
					throw new UserException (GettextCatalog.GetString ("File '{0}' can't be modified", f.FileName));
			}
#pragma warning restore 618

			foreach (var fg in files.GroupBy (f => GetNextForPath (f, false)))
				fg.Key.RequestFileEdit (fg);
		}

		[Obsolete ("This will be removed. Override RequestFileEdit (IEnumerable<FilePath>) instead")]
		public virtual bool RequestFileEdit (FilePath file)
		{
			return true;
		}
		
		public virtual void NotifyFilesChanged (IEnumerable<FilePath> files)
		{
			foreach (var fsFiles in files.GroupBy (f => GetNextForPath (f, false)))
				fsFiles.Key.NotifyFilesChanged (files);
		}
	}

	class DummyFileSystemExtension: FileSystemExtension
	{
		public override bool CanHandlePath (FilePath path, bool isDirectory)
		{
			return false;
		}
	}
}
