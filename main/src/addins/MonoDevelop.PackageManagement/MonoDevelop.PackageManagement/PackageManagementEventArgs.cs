//
// PackageManagementEventArgs.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementEventArgs : EventArgs
	{
		public PackageManagementEventArgs (
			IDotNetProject project,
			PackageEventArgs e)
			: this (project, e.Identity, e.InstallPath)
		{
			PackageFilePath = GetPackageFilePath (e);
		}

		public PackageManagementEventArgs (
			IDotNetProject project,
			PackageIdentity package,
			string installPath)
		{
			Project = project;
			Package = package;
			InstallPath = installPath;
		}

		public IDotNetProject Project { get; private set; }
		public PackageIdentity Package { get; private set; }
		public string InstallPath { get; private set; }
		public string PackageFilePath { get; private set; }

		public string Id {
			get { return Package.Id; }
		}

		public NuGetVersion Version {
			get { return Package.Version; }
		}

		static string GetPackageFilePath (PackageEventArgs e)
		{
			var folderNuGetProject = e.Project as FolderNuGetProject;
			return folderNuGetProject?.GetInstalledPackageFilePath (e.Identity);
		}
	}
}

