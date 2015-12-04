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
		IPackageManagementSolution solution;
		IRegisteredPackageRepositories registeredRepositories;
		IPackageManagementProgressMonitorFactory progressMonitorFactory;
		ProgressMonitorStatusMessage progressMessage;
		IProgressMonitor progressMonitor;
		IPackageManagementEvents packageManagementEvents;
		IProgressProvider progressProvider;

		public PackageCompatibilityRunner (
			IDotNetProject project,
			IPackageManagementSolution solution,
			IRegisteredPackageRepositories registeredRepositories,
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
		{
			this.project = project;
			this.solution = solution;
			this.registeredRepositories = registeredRepositories;
			this.progressMonitorFactory = progressMonitorFactory;
			this.packageManagementEvents = packageManagementEvents;
			this.progressProvider = progressProvider;
		}

		public PackageCompatibilityRunner (IDotNetProject project)
			: this (
				project,
				PackageManagementServices.Solution,
				PackageManagementServices.RegisteredPackageRepositories,
				PackageManagementServices.ProgressMonitorFactory,
				PackageManagementServices.PackageManagementEvents,
				PackageManagementServices.ProgressProvider)
		{
		}

		public void Run ()
		{
			BackgroundDispatch (() => RunInternal ());
		}

		protected virtual void BackgroundDispatch (MessageHandler handler)
		{
			DispatchService.BackgroundDispatch (() => RunInternal ());
		}

		void RunInternal ()
		{
			progressMessage = CreateCheckingPackageCompatibilityMessage ();

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

		protected virtual ProgressMonitorStatusMessage CreateCheckingPackageCompatibilityMessage ()
		{
			return ProgressMonitorStatusMessageFactory.CreateCheckingPackageCompatibilityMessage ();
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
			PackageCompatibilityChecker checker = CreatePackageCompatibilityChecker (solution, registeredRepositories);
			checker.CheckProjectPackages (project);

			if (checker.AnyPackagesRequireReinstallation ()) {
				MarkPackagesForReinstallation (checker);
				ReportPackageReinstallationWarning (checker);
			} else {
				if (checker.PackagesMarkedForReinstallationInPackageReferenceFile ()) {
					MarkPackagesForReinstallation (checker);
				}
				progressMonitor.ReportSuccess (progressMessage.Success);
			}
		}

		protected virtual PackageCompatibilityChecker CreatePackageCompatibilityChecker (IPackageManagementSolution solution, IRegisteredPackageRepositories registeredRepositories)
		{
			return new PackageCompatibilityChecker (solution, registeredRepositories);
		}

		void MarkPackagesForReinstallation (PackageCompatibilityChecker checker)
		{
			checker.MarkPackagesForReinstallation ();
			packageManagementEvents.OnFileChanged (checker.PackageReferenceFileName);
		}

		void ReportPackageReinstallationWarning (PackageCompatibilityChecker checker)
		{
			checker.GenerateReport (progressMonitor.Log);
			if (checker.AnyIncompatiblePackages ()) {
				progressMonitor.ReportError (GettextCatalog.GetString ("Incompatible packages found."), null);
			} else {
				progressMonitor.ReportWarning (progressMessage.Warning);
			}
			ShowPackageConsole (progressMonitor);
		}

		protected virtual void ShowPackageConsole (IProgressMonitor progressMonitor)
		{
			progressMonitor.ShowPackageConsole ();
		}
	}
}

