//
// AnalyzerPackageMonitor.cs
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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using NuGet.Packaging;

namespace MonoDevelop.PackageManagement
{
	internal class AnalyzerPackageMonitor
	{
		IPackageManagementEvents packageManagementEvents;
		List<string> uninstalledFiles = new List<string> ();

		public AnalyzerPackageMonitor ()
		{
			packageManagementEvents = PackageManagementServices.PackageManagementEvents;
			packageManagementEvents.PackageInstalled += PackageInstalled;
			packageManagementEvents.PackageUninstalling += PackageUninstalling;
			packageManagementEvents.PackageUninstalled += PackageUninstalled;
			packageManagementEvents.PackageOperationError += PackageOperationError;
		}

		void PackageInstalled (object sender, PackageManagementEventArgs e)
		{
			try {
				//var files = GetFiles (e);
				//Runtime.RunInMainThread (() => {
				//	AnalyzerPackageService.AddPackageFiles (e.Project.DotNetProject, files);
				//});
			} catch (Exception ex) {
				LoggingService.LogError ("AnalyzerPackageMonitor error.", ex);
			}
		}

		/// <summary>
		/// TODO: Need to handle when an uninstall fails.
		/// </summary>
		void PackageUninstalling (object sender, PackageManagementEventArgs e)
		{
			try {
				uninstalledFiles = GetFiles (e).ToList ();
			} catch (Exception ex) {
				LoggingService.LogError ("AnalyzerPackageMonitor error.", ex);
			}
		}

		void PackageUninstalled (object sender, PackageManagementEventArgs e)
		{
			try {
				//Runtime.RunInMainThread (() => {
				//	AnalyzerPackageService.RemovePackageFiles (e.Project.DotNetProject, uninstalledFiles);
				//}).Wait ();
				uninstalledFiles = new List<string> ();
			} catch (Exception ex) {
				LoggingService.LogError ("AnalyzerPackageMonitor error.", ex);
			}
		}

		void PackageOperationError (object sender, EventArgs e)
		{
			uninstalledFiles = new List<string> ();
		}

		IEnumerable<string> GetFiles (PackageManagementEventArgs e)
		{
			if (String.IsNullOrEmpty (e.PackageFilePath))
				return new string[0];

			using (var packageStream = File.OpenRead (e.PackageFilePath)) {
				var zipArchive = new ZipArchive (packageStream); 

				using (var packageReader = new PackageArchiveReader (zipArchive)) {
					return packageReader
						.GetFiles ()
						.Select (file => Path.GetFullPath (Path.Combine (e.InstallPath, file)))
						.ToList ();
				}
			}
		}
	}
}

