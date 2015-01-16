//
// CheckForUpdatesProgressMonitor.cs
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
	public class CheckForUpdatesProgressMonitor : IDisposable
	{
		ProgressMonitorStatusMessage progressMessage;
		ProgressMonitor progressMonitor;
		PackageUpdatesEventMonitor eventMonitor;

		public CheckForUpdatesProgressMonitor ()
			: this (
				PackageManagementServices.ProgressMonitorFactory,
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public CheckForUpdatesProgressMonitor (
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageEvents)
		{
			progressMessage = ProgressMonitorStatusMessageFactory.CreateCheckingForPackageUpdatesMessage ();
			this.progressMonitor = progressMonitorFactory.CreateProgressMonitor (progressMessage.Status);

			eventMonitor = new PackageUpdatesEventMonitor (progressMonitor, packageEvents);
		}

		public void Dispose ()
		{
			eventMonitor.Dispose ();
			progressMonitor.Dispose ();
		}

		public void ReportError (Exception ex)
		{
			LoggingService.LogInternalError (ex);
			if (IsGuiDispatchException (ex)) {
				progressMonitor.Log.WriteLine (ex.InnerException.Message);
			} else if (ex is AggregateException) {
				LogAggregateException ((AggregateException)ex);
			} else {
				progressMonitor.Log.WriteLine (ex.Message);
			}
			progressMonitor.ReportError (progressMessage.Error, null);
			ShowPackageConsole ();
		}

		static bool IsGuiDispatchException (Exception ex)
		{
			return (ex.InnerException != null) &&
				(ex.Message == "An exception was thrown while dispatching a method call in the UI thread.");
		}

		void LogAggregateException (AggregateException ex)
		{
			progressMonitor.Log.WriteLine (new AggregateExceptionErrorMessage (ex));
		}

		protected virtual void ShowPackageConsole ()
		{
			progressMonitor.ShowPackageConsole ();
		}

		public void ReportSuccess (bool anyUpdates)
		{
			if (anyUpdates) {
				progressMonitor.ReportSuccess (GettextCatalog.GetString ("Package updates are available."));
			} else if (eventMonitor.WarningReported) {
				progressMonitor.ReportWarning (progressMessage.Warning);
			} else {
				progressMonitor.ReportSuccess (progressMessage.Success);
			}
		}
	}
}

