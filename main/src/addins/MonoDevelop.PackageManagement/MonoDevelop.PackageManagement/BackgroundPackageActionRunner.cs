//
// BackgroundPackageActionRunner.cs
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
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class BackgroundPackageActionRunner : IBackgroundPackageActionRunner
	{
		IPackageManagementProgressMonitorFactory progressMonitorFactory;
		IPackageManagementEvents packageManagementEvents;

		public BackgroundPackageActionRunner (
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents)
		{
			this.progressMonitorFactory = progressMonitorFactory;
			this.packageManagementEvents = packageManagementEvents;
		}

		public void Run (ProgressMonitorStatusMessage progressMessage, IPackageAction action)
		{
			Run (progressMessage, new IPackageAction [] { action });
		}

		public void Run (ProgressMonitorStatusMessage progressMessage, IEnumerable<IPackageAction> actions)
		{
			DispatchService.BackgroundDispatch (() => RunActionsWithProgressMonitor (progressMessage, actions.ToList ()));
		}

		void RunActionsWithProgressMonitor (ProgressMonitorStatusMessage progressMessage, IList<IPackageAction> installPackageActions)
		{
			using (IProgressMonitor monitor = progressMonitorFactory.CreateProgressMonitor (progressMessage.Status)) {
				using (var eventMonitor = new PackageManagementEventsMonitor (monitor, packageManagementEvents)) {
					try {
						monitor.BeginTask (null, installPackageActions.Count);
						RunActionsWithProgressMonitor (monitor, installPackageActions);
						eventMonitor.ReportResult (progressMessage);
					} catch (Exception ex) {
						LoggingService.LogInternalError (ex);
						monitor.Log.WriteLine (ex.Message);
						monitor.ReportError (progressMessage.Error, null);
						monitor.ShowPackageConsole ();
					} finally {
						monitor.EndTask ();
					}
				}
			}
		}

		void RunActionsWithProgressMonitor (IProgressMonitor monitor, IList<IPackageAction> packageActions)
		{
			if (!AcceptPackageLicenses (packageActions))
				return;

			foreach (IPackageAction action in packageActions) {
				CheckForPowerShellScripts (action);
				action.Execute ();
				monitor.Step (1);
			}
		}

		bool AcceptPackageLicenses (IList<IPackageAction> packageActions)
		{
			var packagesWithLicenses = new PackagesRequiringLicenseAcceptance ();
			List<IPackage> packages = packagesWithLicenses.GetPackagesRequiringLicenseAcceptance (packageActions).ToList ();
			if (packages.Any ()) {
				return packageManagementEvents.OnAcceptLicenses (packages);
			}

			return true;
		}

		void CheckForPowerShellScripts (IPackageAction action)
		{
			if (action.HasPackageScriptsToRun ()) {
				ReportPowerShellScriptWarning ();
			}
		}

		void ReportPowerShellScriptWarning ()
		{
			string message = GettextCatalog.GetString ("Package contains PowerShell scripts which will not be run.");
			packageManagementEvents.OnPackageOperationMessageLogged (MessageLevel.Warning, message);
		}

		public void ShowError (ProgressMonitorStatusMessage progressMessage, Exception exception)
		{
			LoggingService.LogInternalError (progressMessage.Status, exception);
			ShowError (progressMessage, exception.Message);
		}

		public void ShowError (ProgressMonitorStatusMessage progressMessage, string error)
		{
			using (IProgressMonitor monitor = progressMonitorFactory.CreateProgressMonitor (progressMessage.Status)) {
				monitor.Log.WriteLine (error);
				monitor.ReportError (progressMessage.Error, null);
				monitor.ShowPackageConsole ();
			}
		}
	}
}

