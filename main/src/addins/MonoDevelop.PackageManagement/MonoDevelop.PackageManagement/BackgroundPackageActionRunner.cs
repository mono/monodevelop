﻿//
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
using System.Threading;
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
		Queue<ActionContext> pendingQueue = new Queue<ActionContext> ();

		struct ActionContext {
			public List<IPackageAction> Actions;
			public CancellationTokenSource CancellationTokenSource;
			public TaskCompletionSource<bool> TaskCompletionSource;
		};

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
			get { return pendingQueue.Any () || DispatcherIsDispatching (); }
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
			List<IPackageAction> actionsList = actions.ToList ();
			CancellationTokenSource cancellationTokenSource = CreateCancellationTokenForPendingActions (taskCompletionSource, actionsList);
			packageManagementEvents.OnPackageOperationsStarting ();

			BackgroundDispatch (() => {
				PackageManagementCredentialService.Reset ();
				TryRunActionsWithProgressMonitor (progressMessage, actionsList, taskCompletionSource, clearConsole, cancellationTokenSource);
				actionsList = null;
				cancellationTokenSource = null;
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

		CancellationTokenSource CreateCancellationTokenForPendingActions (
			TaskCompletionSource<bool> taskCompletionSource,
			List<IPackageAction> actions)
		{
			var context = new ActionContext {
				Actions = actions,
				CancellationTokenSource = new CancellationTokenSource (),
				TaskCompletionSource = taskCompletionSource
			};
			pendingQueue.Enqueue (context);
			return context.CancellationTokenSource;
		}

		void TryRunActionsWithProgressMonitor (
			ProgressMonitorStatusMessage progressMessage,
			IList<IPackageAction> actions,
			TaskCompletionSource<bool> taskCompletionSource,
			bool clearConsole,
			CancellationTokenSource cancellationTokenSource)
		{
			try {
				RunActionsWithProgressMonitor (progressMessage, actions, taskCompletionSource, clearConsole, cancellationTokenSource);
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			} finally {
				GuiDispatch (() => RemoveCancellationTokenSource ());
			}
		}

		/// <summary>
		/// This queue can be cleared in the Cancel method so check if any items are queued
		/// before removing anything.
		/// </summary>
		void RemoveCancellationTokenSource ()
		{
			if (pendingQueue.Any ())
				pendingQueue.Dequeue ();
		}

		void RunActionsWithProgressMonitor (
			ProgressMonitorStatusMessage progressMessage,
			IList<IPackageAction> installPackageActions,
			TaskCompletionSource<bool> taskCompletionSource,
			bool clearConsole,
			CancellationTokenSource cancellationTokenSource)
		{
			using (ProgressMonitor monitor = progressMonitorFactory.CreateProgressMonitor (progressMessage.Status, clearConsole, cancellationTokenSource)) {
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

		/// <summary>
		/// Cancels the current action being run and also removes any pending actions.
		/// </summary>
		public void Cancel ()
		{
			if (!pendingQueue.Any ())
				return;

			ClearDispatcher ();

			// Cancel the first item on the queue since this may be currently running.
			ActionContext context = pendingQueue.Dequeue ();
			context.CancellationTokenSource.Cancel ();

			// The rest of the items queued were not running but may have been added
			// due to a call to RunAsync so cancel their associated tasks.
			while (pendingQueue.Count > 0) {
				context = pendingQueue.Dequeue ();
				if (context.TaskCompletionSource != null)
					context.TaskCompletionSource.TrySetCanceled ();

				context.CancellationTokenSource.Dispose ();
			}
		}

		/// <summary>
		/// Returns information about the actions being run or queued to run.
		/// </summary>
		public PendingPackageActionsInformation GetPendingActionsInfo ()
		{
			var info = new PendingPackageActionsInformation ();
			foreach (ActionContext context in pendingQueue)
				info.Add (context.Actions);
			return info;
		}

		protected virtual void BackgroundDispatch (Action action)
		{
			PackageManagementBackgroundDispatcher.Dispatch (action);
		}

		protected virtual void GuiDispatch (Action handler)
		{
			Runtime.RunInMainThread (handler);
		}

		/// <summary>
		/// This will only remove queued actions not the action currently being run.
		/// </summary>
		protected virtual void ClearDispatcher ()
		{
			PackageManagementBackgroundDispatcher.Clear ();
		}

		protected virtual bool DispatcherIsDispatching ()
		{
			return PackageManagementBackgroundDispatcher.IsDispatching ();
		}
	}
}

