//
// PackageRestoreMonitor.cs
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
using MonoDevelop.Core;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class PackageRestoreMonitor : IDisposable
	{
		IPackageRestoreManager packageRestoreManager;
		IPackageManagementEvents packageManagementEvents;

		public PackageRestoreMonitor (IPackageRestoreManager packageRestoreManager)
			: this (
				packageRestoreManager,
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public PackageRestoreMonitor (
			IPackageRestoreManager packageRestoreManager,
			IPackageManagementEvents packageManagementEvents)
		{
			this.packageRestoreManager = packageRestoreManager;
			this.packageManagementEvents = packageManagementEvents;

			packageRestoreManager.PackageRestoreFailedEvent += PackageRestoreFailed;
		}

		public bool RestoreFailed { get; private set; }

		public void Dispose ()
		{
			packageRestoreManager.PackageRestoreFailedEvent -= PackageRestoreFailed;

			if (RestoreFailed) {
				throw new ApplicationException (GettextCatalog.GetString ("Package restore failed."));
			}
		}

		void PackageRestoreFailed (object sender, PackageRestoreFailedEventArgs e)
		{
			RestoreFailed = true;

			foreach (string projectName in e.ProjectNames) {
				LogFailure (projectName, e.Exception);
			}
		}

		void LogFailure (string projectName, Exception exception)
		{
			packageManagementEvents.OnPackageOperationMessageLogged (
				MessageLevel.Info,
				GettextCatalog.GetString ("Package restore failed for project {0}: {1}"),
				projectName,
				exception.Message);
		}
	}
}

