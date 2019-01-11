using System.Collections.Generic;
//
// FileEventArgs.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace MonoDevelop.Core
{
	public class FileEventArgs : EventArgsChain<FileEventInfo>
	{
		public FileEventArgs ()
		{
		}

		public FileEventArgs (FilePath fileName, bool isDirectory)
		{
			Add (new FileEventInfo (fileName, isDirectory));
		}

		public FileEventArgs (IEnumerable<FilePath> files, bool isDirectory)
		{
			foreach (var f in files)
				Add (new FileEventInfo (f, isDirectory));
		}
	}
	
	public class FileEventInfo
	{
		public FilePath FileName { get; }

		public bool IsDirectory { get; }

		public FileEventInfo (FilePath fileName, bool isDirectory)
		{
			this.FileName = fileName;
			this.IsDirectory = isDirectory;
		}
	}
	
	public class FileCopyEventArgs : EventArgsChain<FileCopyEventInfo>
	{
		public FileCopyEventArgs (IEnumerable<FileCopyEventInfo> args): base (args)
		{
		}
		
		public FileCopyEventArgs (FilePath sourceFile, FilePath targetFile, bool isDirectory)
		{
			Add (new FileCopyEventInfo (sourceFile, targetFile, isDirectory));
		}
		
		public FileCopyEventArgs ()
		{
		}

		/// <summary>
		/// Indicates whether or not user made this change in the IDE as opposed to externally.
		/// </summary>
		public bool IsExternal { get; set; }
	}
	
	public class FileCopyEventInfo : System.EventArgs
	{
		public FilePath SourceFile { get; }

		public FilePath TargetFile { get; }

		public bool IsDirectory { get; }

		public FileCopyEventInfo (FilePath sourceFile, FilePath targetFile, bool isDirectory)
		{
			this.SourceFile = sourceFile;
			this.TargetFile = targetFile;
			this.IsDirectory = isDirectory;
		}
	}
}