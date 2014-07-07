//
// TestablePackageCompatibilityRunner.cs
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
using NuGet;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class TestablePackageCompatibilityRunner : PackageCompatibilityRunner
	{
		MessageHandler backgroundDispatcher;

		public TestablePackageCompatibilityRunner (
			IDotNetProject project,
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredRepositories,
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
			: base (
				project,
				solution,
				registeredRepositories,
				progressMonitorFactory,
				packageManagementEvents,
				progressProvider)
		{
			PackageReferenceFile = new PackageReferenceFile (FileSystem, "packages.config");
		}

		public void ExecuteBackgroundDispatch ()
		{
			backgroundDispatcher.Invoke ();
		}

		protected override void BackgroundDispatch (MessageHandler handler)
		{
			backgroundDispatcher = handler;
		}

		protected override PackageManagementEventsMonitor CreateEventMonitor (
			IProgressMonitor monitor,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
		{
			EventsMonitor = new TestablePackageManagementEventsMonitor (monitor, packageManagementEvents, progressProvider);
			return EventsMonitor;
		}

		public TestablePackageManagementEventsMonitor EventsMonitor;

		protected override ProgressMonitorStatusMessage CreateCheckingPackageCompatibilityMessage ()
		{
			ProgressStatusMessage = new ProgressMonitorStatusMessage ("Status", "Success", "Error", "Warning");
			return ProgressStatusMessage;
		}

		public ProgressMonitorStatusMessage ProgressStatusMessage;

		protected override PackageCompatibilityChecker CreatePackageCompatibilityChecker (
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredRepositories)
		{
			return new TestablePackageCompatibilityChecker (solution, registeredRepositories) {
				PackageReferenceFile = PackageReferenceFile
			};
		}

		public PackageReferenceFile PackageReferenceFile;
		public FakeFileSystem FileSystem = new FakeFileSystem ();

		public bool PackageConsoleIsShown;

		protected override void ShowPackageConsole (IProgressMonitor progressMonitor)
		{
			PackageConsoleIsShown = true;
		}
	}
}

