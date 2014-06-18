//
// PackageRestoreRunner.cs
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
using System.Linq;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement
{
	public class PackageRestoreRunner
	{
		IPackageManagementSolution solution;
		IPackageManagementProgressMonitorFactory progressMonitorFactory;
		IPackageManagementEvents packageManagementEvents;

		public PackageRestoreRunner()
			: this(
				PackageManagementServices.Solution,
				PackageManagementServices.ProgressMonitorFactory,
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public PackageRestoreRunner(
			IPackageManagementSolution solution,
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents)
		{
			this.solution = solution;
			this.progressMonitorFactory = progressMonitorFactory;
			this.packageManagementEvents = packageManagementEvents;
		}

		public void Run ()
		{
			ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInSolutionMessage ();
			IProgressMonitor progressMonitor = CreateProgressMonitor (progressMessage);

			try {
				RestorePackages(progressMonitor, progressMessage);
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
				progressMonitor.Log.WriteLine(ex.Message);
				progressMonitor.ReportError (progressMessage.Error, null);
				progressMonitor.ShowPackageConsole ();
				progressMonitor.Dispose();
				RestoreFailed = true;
			}
		}

		public bool RestoreFailed { get; private set; }

		IProgressMonitor CreateProgressMonitor (ProgressMonitorStatusMessage progressMessage)
		{
			return progressMonitorFactory.CreateProgressMonitor (progressMessage.Status);
		}

		void RestorePackages(IProgressMonitor progressMonitor, ProgressMonitorStatusMessage progressMessage)
		{
			var commandLine = new NuGetPackageRestoreCommandLine(solution);

			progressMonitor.Log.WriteLine(commandLine.ToString());

			RestorePackages(progressMonitor, progressMessage, commandLine);
		}

		void RestorePackages(
			IProgressMonitor progressMonitor,
			ProgressMonitorStatusMessage progressMessage,
			NuGetPackageRestoreCommandLine commandLine)
		{
			var aggregatedMonitor = (PackageManagementProgressMonitor)progressMonitor;

			IProcessAsyncOperation operation = Runtime.ProcessService.StartConsoleProcess (
				commandLine.Command,
				commandLine.Arguments,
				commandLine.WorkingDirectory,
				aggregatedMonitor.Console,
				(sender, e) => {
					using (progressMonitor) {
						OnPackageRestoreCompleted ((IAsyncOperation)sender, progressMonitor, progressMessage);
					}
				}
			);

			// Wait for console to finish just so check for updates does not run until
			// all packages are restored.
			operation.WaitForCompleted ();
		}

		void OnPackageRestoreCompleted (
			IAsyncOperation operation,
			IProgressMonitor progressMonitor,
			ProgressMonitorStatusMessage progressMessage)
		{
			if (operation.Success) {
				RefreshProjectReferences ();
				ForceCreationOfSharedRepositoriesConfigFile ();
			}
			ReportOutcome (operation, progressMonitor, progressMessage);
		}

		/// <summary>
		/// Creating package managers for all the projects will force the 
		/// repositories.config file to be created.
		/// </summary>
		void ForceCreationOfSharedRepositoriesConfigFile ()
		{
			var repository = PackageManagementServices.RegisteredPackageRepositories.CreateAggregateRepository ();
			solution.GetProjects (repository).ToList ();
		}

		void RefreshProjectReferences ()
		{
			DispatchService.GuiDispatch (() => {
				Solution solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
				if (solution != null) {
					foreach (DotNetProject project in solution.GetAllDotNetProjects ()) {
						project.RefreshReferenceStatus ();
					}
				}
			});
		}

		void ReportOutcome (
			IAsyncOperation operation,
			IProgressMonitor progressMonitor,
			ProgressMonitorStatusMessage progressMessage)
		{
			if (operation.Success) {
				progressMonitor.ReportSuccess (progressMessage.Success);
				packageManagementEvents.OnPackagesRestored ();
			} else {
				progressMonitor.ReportError (progressMessage.Error, null);
				progressMonitor.ShowPackageConsole ();
				RestoreFailed = true;
			}
		}
	}
}

