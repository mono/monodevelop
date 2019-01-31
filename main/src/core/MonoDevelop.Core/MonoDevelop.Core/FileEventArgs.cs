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
	// For backwards compat.
	public class FileCopyEventInfo
	{
		public FilePath SourceFile { get; }
		public FilePath TargetFile { get; }
		public bool IsDirectory { get; }

		protected FileCopyEventInfo (string sourceFile, string targetFile, bool isDirectory)
		{
			SourceFile = sourceFile;
			TargetFile = targetFile;
			IsDirectory = isDirectory;
		}
	}

	public class FileEventInfo : FileCopyEventInfo
	{
		public FilePath FileName => TargetFile;

		public FileEventInfo (string fileName, bool isDirectory) : base (fileName, fileName, isDirectory)
		{
		}

		public FileEventInfo (string sourceFile, string targetFile, bool isDirectory) : base (sourceFile, targetFile, isDirectory)
		{
		}
	}

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

		public FileEventArgs (IEnumerable<FileEventInfo> args) : base (args)
		{
		}
	}
	
	public class FileCopyEventArgs : FileEventArgs
	{
		public FileCopyEventArgs ()
		{
		}

		public FileCopyEventArgs (IEnumerable<FileEventInfo> args): base (args)
		{
		}

		public FileCopyEventArgs (FilePath sourceFile, FilePath targetFile, bool isDirectory)
		{
			Add (new FileEventInfo (sourceFile, targetFile, isDirectory));
		}

		/// <summary>
		/// Indicates whether or not user made this change in the IDE as opposed to externally.
		/// </summary>
		public bool IsExternal { get; set; }
	}
}