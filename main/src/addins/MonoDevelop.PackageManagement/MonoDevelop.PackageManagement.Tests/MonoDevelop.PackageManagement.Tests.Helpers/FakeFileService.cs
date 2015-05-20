//
// FakeFileService.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.PackageManagement;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeFileService : IPackageManagementFileService
	{
		public string PathPassedToRemoveFile;
		public string PathPassedToRemoveDirectory;

		IDotNetProject project;

		public FakeFileService (IDotNetProject project)
		{
			this.project = project;
		}

		public void RemoveFile (string path)
		{
			PathPassedToRemoveFile = path;
		}

		public void RemoveDirectory (string path)
		{
			PathPassedToRemoveDirectory = path;
			project.Files.Clear ();
		}

		public void OnFileChanged (string path)
		{
		}

		public string FileNamePassedToOpenFile;
		public bool IsOpenFileCalled;

		public void OpenFile (string path)
		{
			IsOpenFileCalled = true;
			FileNamePassedToOpenFile = path;
		}

		public List<string> ExistingFileNames = new List<string> ();

		public bool FileExists (string path)
		{
			return ExistingFileNames.Contains (path);
		}
	}
}

