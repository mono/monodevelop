//
// PreventPackagesConfigFileBeingRemovedOnUpdateMonitor.cs
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
using ICSharpCode.PackageManagement;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	/// <summary>
	/// When updating a package the packages.config file may be removed, if all packages are 
	/// uninstalled during the update, which causes the version control system to mark the file as
	/// deleted. During an update the packages.config file will be recreated so the version control
	/// system should not mark it as deleted. This monitor class looks for the packages.config file
	/// being removed, cancels the standard file deletion call to FileService.RemoveFile, and
	/// removes the file itself.
	/// </summary>
	public class PreventPackagesConfigFileBeingRemovedOnUpdateMonitor : IDisposable
	{
		IPackageManagementEvents packageManagementEvents;
		IFileRemover fileRemover;

		public PreventPackagesConfigFileBeingRemovedOnUpdateMonitor (
			IPackageManagementEvents packageManagementEvents,
			IFileRemover fileRemover)
		{
			this.packageManagementEvents = packageManagementEvents;
			this.fileRemover = fileRemover;

			packageManagementEvents.FileRemoving += FileRemoving;
		}

		void FileRemoving (object sender, FileRemovingEventArgs e)
		{
			if (e.FileName.IsPackagesConfigFileName ()) {
				e.IsCancelled = true;
				fileRemover.RemoveFile (e.FileName);
			}
		}

		public void Dispose ()
		{
			packageManagementEvents.FileRemoving -= FileRemoving;
		}
	}
}

