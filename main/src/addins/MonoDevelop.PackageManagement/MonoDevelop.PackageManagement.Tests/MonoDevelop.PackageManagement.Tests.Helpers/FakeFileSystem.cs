//
// FakeFileSystem.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Text;
using NuGet;
using System.Collections.Generic;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeFileSystem : IFileSystem
	{
		public bool FileExistsReturnValue;
		public string PathPassedToFileExists;
		public string FileToReturnFromOpenFile;
		public string PathToReturnFromGetFullPath;
		public bool DirectoryExistsReturnValue;
		public string PathPassedToDirectoryExists;

		public ILogger Logger { get; set; }

		public string Root { get; set; }

		public FakeFileSystem ()
		{
			FileExistsAction = path => {
				PathPassedToFileExists = path;
				return FileExistsReturnValue;
			};
		}

		public void DeleteDirectory (string path, bool recursive)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<string> GetFiles (string path)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<string> GetFiles (string path, string filter)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<string> GetDirectories (string path)
		{
			throw new NotImplementedException ();
		}

		public string GetFullPath (string path)
		{
			return PathToReturnFromGetFullPath;
		}

		public void DeleteFile (string path)
		{
		}

		public Func<string, bool> FileExistsAction;

		public bool FileExists (string path)
		{
			return FileExistsAction (path);
		}

		public bool DirectoryExists (string path)
		{
			PathPassedToDirectoryExists = path;
			return DirectoryExistsReturnValue;
		}

		public void AddFile (string path, Stream stream)
		{
			throw new NotImplementedException ();
		}

		public Stream OpenFile (string path)
		{
			byte[] bytes = UTF8Encoding.UTF8.GetBytes (FileToReturnFromOpenFile);
			return new MemoryStream (bytes);
		}

		public DateTimeOffset GetLastModified (string path)
		{
			throw new NotImplementedException ();
		}

		public DateTimeOffset GetCreated (string path)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<string> GetFiles (string path, string filter, bool recursive)
		{
			throw new NotImplementedException ();
		}

		public DateTimeOffset GetLastAccessed (string path)
		{
			throw new NotImplementedException ();
		}

		public void AddFile (string path, Action<Stream> writeToStream)
		{
			var memoryStream = new MemoryStream();
			writeToStream (memoryStream);
			memoryStream = new MemoryStream (memoryStream.ToArray ());
			FilesAdded [path] = memoryStream;
		}

		public Dictionary<string, MemoryStream> FilesAdded = 
			new Dictionary<string, MemoryStream> ();

		public string LastAddedFile;

		public void MakeFileWritable (string path)
		{
			throw new NotImplementedException ();
		}

		public Stream CreateFile (string path)
		{
			throw new NotImplementedException ();
		}

		public void DeleteFiles (IEnumerable<IPackageFile> files, string rootDir)
		{
		}

		public void AddFiles (IEnumerable<IPackageFile> files, string rootDir)
		{
		}

		public void MoveFile (string source, string destination)
		{
			throw new NotImplementedException ();
		}
	}
}

