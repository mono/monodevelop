//
// PackageUpdateChecker.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	public class PackageUpdateChecker
	{
		IUpdatedPackagesInSolution updatedPackagesInSolution;
		IPackageManagementProgressMonitorFactory progressMonitorFactory;

		public PackageUpdateChecker ()
			: this (
				PackageManagementServices.UpdatedPackagesInSolution,
				PackageManagementServices.ProgressMonitorFactory)
		{
		}

		public PackageUpdateChecker (
			IUpdatedPackagesInSolution updatedPackagesInSolution,
			IPackageManagementProgressMonitorFactory progressMonitorFactory)
		{
			this.updatedPackagesInSolution = updatedPackagesInSolution;
			this.progressMonitorFactory = progressMonitorFactory;
		}

		public void Run ()
		{
			try {
				CheckForPackageUpdatesWithProgressMonitor ();
			} catch (Exception ex) {
				LoggingService.LogInternalError ("PackageUpdateChecker error.", ex);
			}
		}

		void CheckForPackageUpdatesWithProgressMonitor ()
		{
			ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateCheckingForPackageUpdatesMessage ();
			using (ProgressMonitor progressMonitor = CreateProgressMonitor (progressMessage)) {
				try {
					using (var eventMonitor = new PackageUpdatesEventMonitor (progressMonitor)) {
						CheckForPackageUpdates (progressMonitor, progressMessage, eventMonitor);
					}
				} catch (Exception ex) {
					LoggingService.LogInternalError (ex);
					progressMonitor.Log.WriteLine (ex.Message);
					progressMonitor.ReportError (progressMessage.Error, null);
					progressMonitor.ShowPackageConsole ();
				}
			}
		}

		ProgressMonitor CreateProgressMonitor (ProgressMonitorStatusMessage progressMessage)
		{
			return progressMonitorFactory.CreateProgressMonitor (progressMessage.Status);
		}

		void CheckForPackageUpdates (
			ProgressMonitor progressMonitor,
			ProgressMonitorStatusMessage progressMessage,
			PackageUpdatesEventMonitor eventMonitor)
		{
			updatedPackagesInSolution.CheckForUpdates ();
			if (updatedPackagesInSolution.AnyUpdates ()) {
				progressMonitor.ReportSuccess (GettextCatalog.GetString ("Package updates are available."));
			} else if (eventMonitor.WarningReported) {
				progressMonitor.ReportWarning (progressMessage.Warning);
			} else {
				progressMonitor.ReportSuccess (progressMessage.Success);
			}
		}
	}
}

