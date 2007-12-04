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
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.Core.FileSystem
{
	public class DefaultFileSystemExtension: FileSystemExtension
	{
		public override bool CanHandlePath (string path, bool isDirectory)
		{
			return true;
		}
		
		public override void CopyFile (string source, string dest, bool overwrite)
		{
			File.Copy (source, dest, overwrite);
		}
		
		public override void RenameFile (string file, string newName)
		{
			File.Move (file, newName);
		}
		
		public override void MoveFile (string source, string dest)
		{
			File.Move (source, dest);
		}
		
		public override void DeleteFile (string file)
		{
			File.Delete (file);
		}
		
		public override void CreateDirectory (string path)
		{
			Directory.CreateDirectory (path);
		}
		
		public override void CopyDirectory (string sourcePath, string destPath)
		{
			CopyDirectory (sourcePath, destPath, "");
		}
		
		void CopyDirectory (string src, string dest, string subdir)
		{
			string destDir = Path.Combine (dest, subdir);
	
			if (!Directory.Exists (destDir))
				FileService.CreateDirectory (destDir);
	
			foreach (string file in Directory.GetFiles (src))
				FileService.CopyFile (file, Path.Combine (destDir, Path.GetFileName (file)));
	
			foreach (string dir in Directory.GetDirectories (src))
				CopyDirectory (dir, dest, Path.Combine (subdir, Path.GetFileName (dir)));
		}
		
		public override void RenameDirectory (string path, string newName)
		{
			Directory.Move (path, newName);
		}
		
		public override void MoveDirectory (string source, string dest)
		{
			Directory.Move (source, dest);
		}
		
		public override void DeleteDirectory (string path)
		{
			Directory.Delete (path, true);
		}
		
		public override bool RequestFileEdit (string file)
		{
			return true;
		}
		
		public override void NotifyFileChanged (string file)
		{
		}
	}
}
