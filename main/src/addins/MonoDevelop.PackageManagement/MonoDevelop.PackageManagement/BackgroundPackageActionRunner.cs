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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement
{
	internal class BackgroundPackageActionRunner : IBackgroundPackageActionRunner
	{
		IPackageManagementProgressMonitorFactory progressMonitorFactory;
		IPackageManagementEvents packageManagementEvents;
		PackageManagementInstrumentationService instrumentationService;
		List<IInstallNuGetPackageAction> pendingInstallActions = new List<IInstallNuGetPackageAction> ();
		int runCount;

		public BackgroundPackageActionRunner (
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents)
			: this (
				progressMonitorFactory,
				packageManagementEvents,
				new PackageManagementInstrumentationService ())
		{
		}

		public BackgroundPackageActionRunner (
			IPackageManagementProgressMonitorFactory progressMonitorFactory,
			IPackageManagementEvents packageManagementEvents,
			PackageManagementInstrumentationService instrumentationService)
		{
			this.progressMonitorFactory = progressMonitorFactory;
			this.packageManagementEvents = packageManagementEvents;
			this.instrumentationService = instrumentationService;
		}

		public bool IsRunning {
			get { return runCount > 0; }
		}

		public IEnumerable<IInstallNuGetPackageAction> PendingInstallActions {
			get { return pendingInstallActions; }
		}

		public IEnumerable<IInstallNuGetPackageAction> PendingInstallActionsForProject (DotNetProject project)
		{
			return pendingInstallActions.Where (action => action.IsForProject (project));
		}

		public void Run (ProgressMonitorStatusMessage progressMessage, IPackageAction action)
		{
			Run (progressMessage, action, clearConsole: !IsRunning);
		}

		public void Run (ProgressMonitorStatusMessage progressMessage, IPackageAction action, bool clearConsole)
		{
			Run (progressMessage, new IPackageAction [] { action }, clearConsole);
		}

		public void Run (ProgressMonitorStatusMessage progressMessage, IEnumerable<IPackageAction> actions)
		{
			Run (progressMessage, actions, clearConsole: !IsRunning);
		}

		public void Run (
			ProgressMonitorStatusMessage progressMessage,
			IEnumerable<IPackageAction> actions,
			bool clearConsole)
		{
			Run (progressMessage, actions, null, clearConsole);
		}

		void Run (
			ProgressMonitorStatusMessage progressMessage,
			IEnumerable<IPackageAction> actions,
			TaskCompletionSource<bool> taskCompletionSource,
			bool clearConsole)
		{
			AddInstallActionsToPendingQueue (actions);
			packageManagementEvents.OnPackageOperationsStarting ();
			runCount++;

			List<IPackageAction> actionsList = actions.ToList ();
			BackgroundDispatch (() => {
				PackageManagementCredentialService.Reset ();
				TryRunActionsWithProgressMonitor (progressMessage, actionsList, taskCompletionSource, clearConsole);
				actionsList = null;
				progressMessage = null;
			});
		}

		public Task RunAsync (ProgressMonitorStatusMessage progressMessage, IEnumerable<IPackageAction> actions)
		{
			return RunAsync (progressMessage, actions, clearConsole: !IsRunning);
		}

		public Task RunAsync (
			ProgressMonitorStatusMessage progressMessage,
			IEnumerable<IPackageAction> actions,
			bool clearConsole)
		{
			var taskCompletionSource = new TaskCompletionSource<bool> ();
			Run (progressMessage, actions, taskCompletionSource, clearConsole);
			return taskCompletionSource.Task;
		}

		void AddInstallActionsToPendingQueue (IEnumerable<IPackageAction> actions)
		{
			foreach (IInstallNuGetPackageAction action in actions.OfType<IInstallNuGetPackageAction> ()) {
				pendingInstallActions.Add (action);
			}
		}

		void TryRunActionsWithProgressMonitor (
			ProgressMonitorStatusMessage progressMessage,
			IList<IPackageAction> actions,
			TaskCompletionSource<bool> taskCompletionSource,
			bool clearConsole)
		{
			try {
				RunActionsWithProgressMonitor (progressMessage, actions, taskCompletionSource, clearConsole);
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			} finally {
				GuiDispatch (() => runCount--);
			}
		}

		void RunActionsWithProgressMonitor (
			ProgressMonitorStatusMessage progressMessage,
			IList<IPackageAction> installPackageActions,
			TaskCompletionSource<bool> taskCompletionSource,
			bool clearConsole)
		{
			using (ProgressMonitor monitor = progressMonitorFactory.CreateProgressMonitor (progressMessage.Status, clearConsole)) {
				using (PackageManagementEventsMonitor eventMonitor = CreateEventMonitor (monitor, taskCompletionSource)) {
					try {
						monitor.BeginTask (null, installPackageActions.Count);
						RunActionsWithProgressMonitor (monitor, installPackageActions);
						eventMonitor.ReportResult (progressMessage);
					} catch (Exception ex) {
						RemoveInstallActions (installPackageActions);
						bool showPackageConsole = !monitor.CancellationToken.IsCancellationRequested;
						eventMonitor.ReportError (progressMessage, ex, showPackageConsole);
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

		PackageManagementEventsMonitor CreateEventMonitor (ProgressMonitor monitor, TaskCompletionSource<bool> taskCompletionSource)
		{
			return CreateEventMonitor (monitor, packageManagementEvents, taskCompletionSource);
		}

		protected virtual PackageManagementEventsMonitor CreateEventMonitor (
			ProgressMonitor monitor,
			IPackageManagementEvents packageManagementEvents,
			TaskCompletionSource<bool> taskCompletionSource)
		{
			return new PackageManagementEventsMonitor (monitor, packageManagementEvents, taskCompletionSource);
		}

		void RunActionsWithProgressMonitor (ProgressMonitor monitor, IList<IPackageAction> packageActions)
		{
			foreach (IPackageAction action in packageActions) {
				action.Execute (monitor.CancellationToken);
				instrumentationService.InstrumentPackageAction (action);
				monitor.Step (1);
			}
		}

		void RemoveInstallActions (IList<IPackageAction> installPackageActions)
		{
			foreach (IInstallNuGetPackageAction action in installPackageActions.OfType<IInstallNuGetPackageAction> ()) {
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
			using (ProgressMonitor monitor = progressMonitorFactory.CreateProgressMonitor (progressMessage.Status)) {
				monitor.Log.WriteLine (error);
				monitor.ReportError (progressMessage.Error, null);
				monitor.ShowPackageConsole ();
			}
		}

		protected virtual void BackgroundDispatch (Action action)
		{
			PackageManagementBackgroundDispatcher.Dispatch (action);
		}

		protected virtual void GuiDispatch (Action handler)
		{
			Runtime.RunInMainThread (handler);
		}
	}
}

