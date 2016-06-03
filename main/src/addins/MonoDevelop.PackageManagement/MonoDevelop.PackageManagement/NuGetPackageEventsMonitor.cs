//
// NuGetPackageEventsMonitor.cs
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
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class NuGetPackageEventsMonitor : IDisposable
	{
		IDotNetProject project;
		PackageEvents packageEvents;
		PackageManagementEvents packageManagementEvents;

		public NuGetPackageEventsMonitor (IDotNetProject project)
			: this (
				project,
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public NuGetPackageEventsMonitor (
			IDotNetProject project,
			IPackageManagementEvents packageManagementEvents)
			: this (
				project,
				new PackageEventsProvider (),
				packageManagementEvents)
		{
		}

		public NuGetPackageEventsMonitor (
			IDotNetProject project,
			IPackageEventsProvider provider,
			IPackageManagementEvents packageManagementEvents)
		{
			this.project = project;
			packageEvents = provider.GetPackageEvents ();
			this.packageManagementEvents = (PackageManagementEvents)packageManagementEvents;

			packageEvents.PackageInstalled += PackageInstalled;
			packageEvents.PackageUninstalling += PackageUninstalling;
			packageEvents.PackageUninstalled += PackageUninstalled;
		}

		public void Dispose ()
		{
			packageEvents.PackageInstalled -= PackageInstalled;
			packageEvents.PackageUninstalling -= PackageUninstalling;
			packageEvents.PackageUninstalled -= PackageUninstalled;
		}

		void PackageInstalled (object sender, PackageEventArgs e)
		{
			packageManagementEvents.OnPackageInstalled (project, e);
		}

		void PackageUninstalling (object sender, PackageEventArgs e)
		{
			packageManagementEvents.OnPackageUninstalling (project, e);
		}

		void PackageUninstalled (object sender, PackageEventArgs e)
		{
			packageManagementEvents.OnPackageUninstalled (project, e);
		}
	}
}

