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
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class PackageRestoreRunner
	{
		IPackageManagementSolution solution;
		IPackageManagementProgressMonitorFactory progressMonitorFactory;
		IPackageManagementEvents packageManagementEvents;
		IProgressProvider progressProvider;

		public PackageRestoreRunner()
			: this(
				PackageManagementServices.Solution,
				PackageManagementServices.ProgressMonitorFactory,
				PackageManagementServices.PackageManagementEvents,
				PackageManagementServices.ProgressProvider)
		{
		}

		public PackageRestoreRunner(
			IPackageManagementSolution solution,
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
		{
			this.solution = solution;
			this.progressMonitorFactory = progressMonitorFactory;
			this.packageManagementEvents = packageManagementEvents;
			this.progressProvider = progressProvider;
		}

		public void Run ()
		{
			ProgressMonitorStatusMessage progressMessage = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInSolutionMessage ();

			using (IProgressMonitor progressMonitor = CreateProgressMonitor (progressMessage)) {
				using (PackageManagementEventsMonitor eventMonitor = CreateEventMonitor (progressMonitor)) {
					try {
						RestorePackages (progressMonitor, progressMessage);
					} catch (Exception ex) {
						LoggingService.LogInternalError (ex);
						progressMonitor.Log.WriteLine (ex.Message);
						progressMonitor.ReportError (progressMessage.Error, null);
						progressMonitor.ShowPackageConsole ();
						progressMonitor.Dispose ();
						RestoreFailed = true;
					}
				}
			}
		}

		public bool RestoreFailed { get; private set; }

		IProgressMonitor CreateProgressMonitor (ProgressMonitorStatusMessage progressMessage)
		{
			return progressMonitorFactory.CreateProgressMonitor (progressMessage.Status);
		}

		PackageManagementEventsMonitor CreateEventMonitor (IProgressMonitor monitor)
		{
			return new PackageManagementEventsMonitor (monitor, packageManagementEvents, progressProvider);
		}

		void RestorePackages(IProgressMonitor progressMonitor, ProgressMonitorStatusMessage progressMessage)
		{
			var action = new RestorePackagesAction (solution, packageManagementEvents);
			action.Execute ();

			RefreshProjectReferences ();
			ForceCreationOfSharedRepositoriesConfigFile ();

			progressMonitor.ReportSuccess (progressMessage.Success);
			packageManagementEvents.OnPackagesRestored ();
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
	}
}

