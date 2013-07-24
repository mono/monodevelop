// DefaultFileSystemExtension.cs
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
	internal class DefaultFileSystemExtension: FileSystemExtension
	{
		public override bool CanHandlePath (FilePath path, bool isDirectory)
		{
			return true;
		}
		
		public override void CopyFile (FilePath source, FilePath dest, bool overwrite)
		{
			File.Copy (source, dest, overwrite);
		}
		
		public override void RenameFile (FilePath file, string newName)
		{
			File.Move (file, Path.Combine (file.ParentDirectory, newName));
		}
		
		public override void MoveFile (FilePath source, FilePath dest)
		{
			File.Move (source, dest);
		}
		
		public override void DeleteFile (FilePath file)
		{
			File.Delete (file);
		}
		
		public override void CreateDirectory (FilePath path)
		{
			Directory.CreateDirectory (path);
		}
		
		public override void CopyDirectory (FilePath sourcePath, FilePath destPath)
		{
			CopyDirectory (sourcePath, destPath, "");
		}
		
		void CopyDirectory (FilePath src, FilePath dest, FilePath subdir)
		{
			string destDir = Path.Combine (dest, subdir);
	
			if (!Directory.Exists (destDir))
				FileService.CreateDirectory (destDir);
	
			foreach (string file in Directory.GetFiles (src))
				FileService.CopyFile (file, Path.Combine (destDir, Path.GetFileName (file)));
	
			foreach (string dir in Directory.GetDirectories (src))
				CopyDirectory (dir, dest, Path.Combine (subdir, Path.GetFileName (dir)));
		}
		
		public override void RenameDirectory (FilePath path, string newName)
		{
			Directory.Move (path, newName);
		}
		
		public override FilePath ResolveFullPath (FilePath path)
		{
			return Path.GetFullPath (path);
		}
		
		public override void MoveDirectory (FilePath source, FilePath dest)
		{
			Directory.Move (source, dest);
		}
		
		public override void DeleteDirectory (FilePath path)
		{
			Directory.Delete (path, true);
		}
		
		public override void RequestFileEdit (IEnumerable<FilePath> files)
		{
		}
		
		public override void NotifyFilesChanged (IEnumerable<FilePath> file)
		{
		}
	}
}
