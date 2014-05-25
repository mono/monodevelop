//
// PackageCompatibilityRunner.cs
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
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class PackageCompatibilityRunner
	{
		IDotNetProject project;
		IPackageManagementProgressMonitorFactory progressMonitorFactory;
		ProgressMonitorStatusMessage progressMessage;
		IProgressMonitor progressMonitor;
		IPackageManagementEvents packageManagementEvents;
		IProgressProvider progressProvider;

		public PackageCompatibilityRunner (
			IDotNetProject project,
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
		{
			this.project = project;
			this.progressMonitorFactory = progressMonitorFactory;
			this.packageManagementEvents = packageManagementEvents;
			this.progressProvider = progressProvider;
		}

		public PackageCompatibilityRunner (IDotNetProject project)
			: this (
				project,
				PackageManagementServices.ProgressMonitorFactory,
				PackageManagementServices.PackageManagementEvents,
				PackageManagementServices.ProgressProvider)
		{
		}

		public void Run ()
		{
			DispatchService.BackgroundDispatch (() => RunInternal ());
		}

		void RunInternal ()
		{
			progressMessage = ProgressMonitorStatusMessageFactory.CreateCheckingPackageCompatibilityMessage ();

			using (progressMonitor = CreateProgressMonitor ()) {
				using (PackageManagementEventsMonitor eventMonitor = CreateEventMonitor (progressMonitor)) {
					try {
						CheckCompatibility ();
					} catch (Exception ex) {
						eventMonitor.ReportError (progressMessage, ex);
					}
				}
			}
		}

		IProgressMonitor CreateProgressMonitor ()
		{
			return progressMonitorFactory.CreateProgressMonitor (progressMessage.Status);
		}

		PackageManagementEventsMonitor CreateEventMonitor (IProgressMonitor monitor)
		{
			return CreateEventMonitor (monitor, packageManagementEvents, progressProvider);
		}

		protected virtual PackageManagementEventsMonitor CreateEventMonitor (
			IProgressMonitor monitor,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
		{
			return new PackageManagementEventsMonitor (monitor, packageManagementEvents, progressProvider);
		}

		void CheckCompatibility ()
		{
			var checker = new PackageCompatibilityChecker ();
			checker.CheckProjectPackages (project);

			if (checker.AnyPackagesRequireReinstallation ()) {
				checker.MarkPackagesForReinstallation ();
				packageManagementEvents.OnFileChanged (checker.PackageReferenceFileName);
				ReportPackageReinstallationWarning (checker.GetPackagesRequiringReinstallation ());
			} else {
				progressMonitor.ReportSuccess (progressMessage.Success);
			}
		}

		void ReportPackageReinstallationWarning (IEnumerable<string> packages)
		{
			string message = "The following NuGet packages were installed with a target framework that is different from the project's current target framework and should be reinstalled.";
			progressMonitor.Log.WriteLine (message);
			foreach (string package in packages) {
				progressMonitor.Log.WriteLine (package);
			}
			progressMonitor.ReportWarning (progressMessage.Warning);
			progressMonitor.ShowPackageConsole ();
		}
	}
}

