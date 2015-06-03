//
// OpenPackageReadMeMonitor.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using ICSharpCode.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class OpenPackageReadMeMonitor : IOpenPackageReadMeMonitor
	{
		IPackageManagementProject project;
		IPackageManagementFileService fileService;

		public OpenPackageReadMeMonitor (
			string packageId,
			IPackageManagementProject project,
			IPackageManagementEvents packageManagementEvents)
			: this (packageId, project, new PackageManagementFileService (packageManagementEvents))
		{
		}

		public OpenPackageReadMeMonitor (
			string packageId,
			IPackageManagementProject project,
			IPackageManagementFileService fileService)
		{
			PackageId = packageId;
			this.project = project;
			this.fileService = fileService;
			project.PackageInstalled += PackageInstalled;
		}

		public string PackageId { get; private set; }

		public bool IsDisposed { get; private set; }

		string ReadMeFile { get; set; }

		public void Dispose ()
		{
			if (IsDisposed) {
				return;
			}

			IsDisposed = true;
			project.PackageInstalled -= PackageInstalled;
		}

		void PackageInstalled (object sender, PackageOperationEventArgs e)
		{
			if (e.Package.Id != PackageId) {
				return;
			}

			ReadMeFile = FindReadMeFileInPackage (e.InstallPath, e.Package);
		}

		string FindReadMeFileInPackage (string installPath, IPackage package)
		{
			return package.GetFiles ()
				.Where (file => "readme.txt".Equals (file.Path, StringComparison.OrdinalIgnoreCase))
				.Select (file => Path.Combine (installPath, file.Path))
				.FirstOrDefault ();
		}

		public void OpenReadMeFile ()
		{
			if ((ReadMeFile != null) && fileService.FileExists (ReadMeFile)) {
				fileService.OpenFile (ReadMeFile);
			}
		}
	}
}