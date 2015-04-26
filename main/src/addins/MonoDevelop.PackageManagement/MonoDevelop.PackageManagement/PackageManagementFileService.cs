// 
// PackageManagementFileService.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
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

using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace ICSharpCode.PackageManagement
{
	public class PackageManagementFileService : IPackageManagementFileService
	{
		IPackageManagementEvents packageManagementEvents;

		public PackageManagementFileService ()
			: this (PackageManagementServices.PackageManagementEvents)
		{
		}

		public PackageManagementFileService (IPackageManagementEvents packageManagementEvents)
		{
			this.packageManagementEvents = packageManagementEvents;
		}

		public void RemoveFile(string path)
		{
			if (packageManagementEvents.OnFileRemoving (path)) {
				FileService.DeleteFile (path);
			}
		}
		
		public void RemoveDirectory(string path)
		{
			FileService.DeleteDirectory(path);
		}

		public void OnFileChanged (string path)
		{
			packageManagementEvents.OnFileChanged (path);
		}

		public bool FileExists (string path)
		{
			return File.Exists (path);
		}

		public void OpenFile (string path)
		{
			DispatchService.GuiSyncDispatch (() => {
				IdeApp.Workbench.OpenDocument (path, null, true);
			});
		}
	}
}
