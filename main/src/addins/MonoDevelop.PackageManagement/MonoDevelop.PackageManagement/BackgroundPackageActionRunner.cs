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
using MonoDevelop.Projects;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class BackgroundPackageActionRunner : IBackgroundPackageActionRunner
	{
		static MonoDevelop.Core.Instrumentation.Counter InstallPackageCounter = MonoDevelop.Core.Instrumentation.InstrumentationService.CreateCounter ("Package Installed", "Package Management", id:"PackageManagement.Package.Installed");
		static MonoDevelop.Core.Instrumentation.Counter UninstallPackageCounter = MonoDevelop.Core.Instrumentation.InstrumentationService.CreateCounter ("Package Uninstalled", "Package Management", id:"PackageManagement.Package.Uninstalled");

		IPackageManagementProgressMonitorFactory progressMonitorFactory;
		IPackageManagementEvents packageManagementEvents;
		IProgressProvider progressProvider;
		List<InstallPackageAction> pendingInstallActions = new List<InstallPackageAction> ();
		int runCount;

		public BackgroundPackageActionRunner (
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents,
			IProgressProvider progressProvider)
		{
			this.progressMonitorFactory = progressMonitorFactory;
			this.packageManagementEvents = packageManagementEvents;
			this.progressProvider = progressProvider;
		}

		public bool IsRunning {
			get { return runCount > 0; }
		}

		public IEnumerable<InstallPackageAction> PendingInstallActions {
			get { return pendingInstallActions; }
		}

		public IEnumerable<InstallPackageAction> PendingInstallActionsForProject (DotNetProject project)
		{
			return pendingInstallActions.Where (action => action.Project.DotNetProject == project);
		}

		public void Run (ProgressMonitorStatusMessage progressMessage, IPackageAction action)
		{
			Run (progressMessage, new IPackageAction [] { action });
		}

		public void Run (ProgressMonitorStatusMessage progressMessage, IEnumerable<IPackageAction> actions)
		{
			AddInstallActionsToPendingQueue (actions);
			packageManagementEvents.OnPackageOperationsStarting ();
			runCount++;
			BackgroundDispatch (() => TryRunActionsWithProgressMonitor (progressMessage, actions.ToList ()));
		}

		void AddInstallActionsToPendingQueue (IEnumerable<IPackageAction> actions)
		{
			foreach (InstallPackageAction action in actions.OfType<InstallPackageAction> ()) {
				pendingInstallActions.Add (action);
			}
		}

		public void RunAndWait (ProgressMonitorStatusMessage progressMessage, IEnumerable<IPackageAction> actions)
		{
			AddInstallActionsToPendingQueue (actions);
			packageManagementEvents.OnPackageOperationsStarting ();
			runCount++;
			BackgroundDispatchAndWait (() => TryRunActionsWithProgressMonitor (progressMessage, actions.ToList ()));
		}

		void TryRunActionsWithProgressMonitor (ProgressMonitorStatusMessage progressMessage, IList<IPackageAction> actions)
		{
			try {
				RunActionsWithProgressMonitor (progressMessage, actions);
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			} finally {
				GuiDispatch (() => runCount--);
			}
		}

		void RunActionsWithProgressMonitor (ProgressMonitorStatusMessage progressMessage, IList<IPackageAction> installPackageActions)
		{
			using (IProgressMonitor monitor = progressMonitorFactory.CreateProgressMonitor (progressMessage.Status)) {
				using (PackageManagementEventsMonitor eventMonitor = CreateEventMonitor (monitor)) {
					try {
						monitor.BeginTask (null, installPackageActions.Count);
						RunActionsWithProgressMonitor (monitor, installPackageActions);
						eventMonitor.ReportResult (progressMessage);
					} catch (Exception ex) {
						RemoveInstallActions (installPackageActions);
						eventMonitor.ReportError (progressMessage, ex);
					} finally {
						monitor.EndTask ();
						GuiDispatch (() => {
							RemoveInstallActions (installPackageActions);
							packageManagementEvents.OnPackageOperationsFinished ();
						});
					}
				}
			}
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

		void RunActionsWithProgressMonitor (IProgressMonitor monitor, IList<IPackageAction> packageActions)
		{
			foreach (IPackageAction action in packageActions) {
				action.Execute ();
				InstrumentPackageAction (action);
				monitor.Step (1);
			}
		}

		protected virtual void InstrumentPackageAction (IPackageAction action) 
		{
			try {
				var addAction = action as InstallPackageAction;
				if (addAction != null) {
					InstrumentPackageOperations (addAction.Operations);
					return;
				}

				var updateAction = action as UpdatePackageAction;
				if (updateAction != null) {
					InstrumentPackageOperations (updateAction.Operations);
					return;
				}

				var removeAction = action as UninstallPackageAction;
				if (removeAction != null) {
					var metadata = new Dictionary<string, string> ();

					metadata ["PackageId"] = removeAction.GetPackageId ();
					var version = removeAction.GetPackageVersion ();
					if (version != null)
						metadata ["PackageVersion"] = version.ToString ();

					UninstallPackageCounter.Inc (1, null, metadata);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Instrumentation Failure in PackageManagement", ex);
			}
		}

		static void InstrumentPackageOperations (IEnumerable<PackageOperation> operations)
		{
			foreach (var op in operations) {
				var metadata = new Dictionary<string, string> ();
				metadata ["PackageId"] = op.Package.Id;
				metadata ["Package"] = op.Package.Id + " v" + op.Package.Version.ToString ();

				switch (op.Action) {
				case PackageAction.Install: 
					InstallPackageCounter.Inc (1, null, metadata);
					break;
				case PackageAction.Uninstall:
					UninstallPackageCounter.Inc (1, null, metadata);
					break;
				}
			}
		}

		void RemoveInstallActions (IList<IPackageAction> installPackageActions)
		{
			foreach (InstallPackageAction action in installPackageActions.OfType <InstallPackageAction> ()) {
				pendingInstallActions.Remove (action);
			}
		}

		public void ShowError (ProgressMonitorStatusMessage progressMessage, Exception exception)
		{
			LoggingService.LogError (progressMessage.Error, exception);
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

		protected virtual void BackgroundDispatch (MessageHandler handler)
		{
			DispatchService.BackgroundDispatch (handler);
		}

		protected virtual void BackgroundDispatchAndWait (MessageHandler handler)
		{
			DispatchService.BackgroundDispatchAndWait (handler);
		}

		protected virtual void GuiDispatch (MessageHandler handler)
		{
			DispatchService.GuiDispatch (handler);
		}
	}
}

