//
// BackgroundPackageActionRunnerTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class BackgroundPackageActionRunnerTests
	{
		TestableBackgroundPackageActionRunner runner;
		FakeProgressMonitorFactory progressMonitorFactory;
		PackageManagementEvents packageManagementEvents;
		List<IPackageAction> actions;
		ProgressMonitorStatusMessage progressMessage;
		FakeProgressMonitor progressMonitor;
		TestableInstrumentationService instrumentationService;

		void CreateRunner ()
		{
			actions = new List<IPackageAction> ();
			progressMessage = new ProgressMonitorStatusMessage ("Status", "Success", "Error", "Warning");
			packageManagementEvents = new PackageManagementEvents ();
			progressMonitorFactory = new FakeProgressMonitorFactory ();
			progressMonitor = progressMonitorFactory.ProgressMonitor;
			instrumentationService = new TestableInstrumentationService ();

			runner = new TestableBackgroundPackageActionRunner (
				progressMonitorFactory,
				packageManagementEvents,
				instrumentationService);
		}

		void Run (bool clearConsole = true)
		{
			RunWithoutBackgroundDispatch (clearConsole);
			runner.ExecuteBackgroundDispatch ();
		}

		void RunWithoutBackgroundDispatch (bool clearConsole = true)
		{
			runner.Run (progressMessage, actions, clearConsole);
		}

		Task RunAsyncWithoutBackgroundDispatch (bool clearConsole = true)
		{
			return runner.RunAsync (progressMessage, actions, clearConsole);
		}

		Task RunAsync (bool clearConsole = true)
		{
			Task task = RunAsyncWithoutBackgroundDispatch (clearConsole);
			runner.ExecuteBackgroundDispatch ();
			return task;
		}

		TestableInstallNuGetPackageAction AddInstallAction ()
		{
			var action = new TestableInstallNuGetPackageAction (
				new FakeSourceRepositoryProvider ().Repositories,
				new FakeSolutionManager (),
				new FakeDotNetProject ());

			action.PackageId = "Test";

			actions.Add (action);

			return action;
		}

		TestableUpdateNuGetPackageAction AddUpdateAction ()
		{
			var action = new TestableUpdateNuGetPackageAction (
				new FakeSolutionManager (),
				new FakeDotNetProject ()
			);
			action.PackageId = "Test";

			actions.Add (action);

			return action;
		}

		void AddInstallActionWithMissingPackageId ()
		{
			var action = AddInstallAction ();
			action.PackageId = null;
		}

		void AddInstallActionWithCustomExecuteAction (Action executionAction)
		{
			var action = AddInstallAction ();
			action.PackageManager.BeforeExecuteAction = executionAction;
			var projectAction = new FakeNuGetProjectAction ("Test", "1.2", NuGetProjectActionType.Install);
			action.PackageManager.InstallActions.Add (projectAction);
		}

		TestableUninstallNuGetPackageAction AddUninstallAction ()
		{
			var action = new TestableUninstallNuGetPackageAction (
				new FakeSolutionManager (),
				new FakeDotNetProject ());

			action.PackageId = "Test";

			actions.Add (action);

			return action;
		}

		void AssertInstallCounterIncrementedForPackage (string packageId, string packageVersion)
		{
			AssertCounterIncrementedForPackage (instrumentationService.InstallPackageMetadata, packageId, packageVersion);
		}

		static void AssertCounterIncrementedForPackage (
			IDictionary<string, string> metadata,
			string packageId,
			string packageVersion)
		{
			string fullInfo = packageId + " v" + packageVersion;
			Assert.AreEqual (packageId, metadata["PackageId"]);
			Assert.AreEqual (fullInfo, metadata["Package"]);
		}

		void AssertUninstallCounterIncrementedForPackage (string packageId, string packageVersion)
		{
			AssertCounterIncrementedForPackage (instrumentationService.UninstallPackageMetadata, packageId, packageVersion);
		}

		void AssertUninstallCounterIncrementedForPackage (string packageId)
		{
			Assert.AreEqual (packageId, instrumentationService.UninstallPackageMetadata["PackageId"]);
			Assert.IsFalse (instrumentationService.UninstallPackageMetadata.ContainsKey ("PackageVersion"));
			Assert.IsFalse (instrumentationService.UninstallPackageMetadata.ContainsKey ("Package"));
		}

		void AddInstallPackageIntoProjectAction (FakeNuGetPackageManager packageManager, string packageId, string version)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, version, NuGetProjectActionType.Install);
			packageManager.InstallActions.Add (projectAction);
		}

		void AddUninstallPackageIntoProjectAction (FakeNuGetPackageManager packageManager, string packageId, string version)
		{
			var projectAction = new FakeNuGetProjectAction (packageId, version, NuGetProjectActionType.Uninstall);
			packageManager.InstallActions.Add (projectAction);
		}

		void CancelCurrentAction ()
		{
			progressMonitorFactory.ProgressMonitor.Cancel ();
		}

		void AddRestoreAction ()
		{
			var action = new FakeNuGetPackageAction ();
			action.ActionType = PackageActionType.Restore;

			actions.Add (action);
		}

		[Test]
		public void Run_OneInstallActionAndOneUninstallActionAndRunNotCompleted_InstallActionMarkedAsPending ()
		{
			CreateRunner ();
			var expectedAction = AddInstallAction ();
			AddUninstallAction ();

			RunWithoutBackgroundDispatch ();

			Assert.AreEqual (expectedAction, runner.PendingInstallActions.Single ());
		}

		[Test]
		public void Run_OneInstallActionAndRunNotCompleted_PackageOperationsStartedEventRaisedAfterInstallActionMarkedAsPending ()
		{
			CreateRunner ();
			var expectedAction = AddInstallAction ();
			List<IInstallNuGetPackageAction> actions = null;
			packageManagementEvents.PackageOperationsStarting += (sender, e) => {
				actions = runner.PendingInstallActions.ToList ();
			};

			RunWithoutBackgroundDispatch ();

			Assert.AreEqual (expectedAction, actions.Single ());
		}

		[Test]
		public void Run_OneInstallAction_ProgressMonitorCreatedWithInitialProgressStatus ()
		{
			CreateRunner ();
			AddInstallAction ();

			Run ();

			Assert.AreEqual ("Status", progressMonitorFactory.StatusText);
		}

		[Test]
		public void Run_OneInstallAction_PackageOperationsFinishedEventRaisedAfterPendingInstallActionsRemoved ()
		{
			CreateRunner ();
			AddInstallAction ();
			List<IInstallNuGetPackageAction> actions = null;
			packageManagementEvents.PackageOperationsFinished += (sender, e) => {
				actions = runner.PendingInstallActions.ToList ();
			};

			Run ();

			Assert.AreEqual (0, actions.Count);
		}

		[Test]
		public void Run_OneInstallAction_ProgressMonitorDisposed ()
		{
			CreateRunner ();
			AddInstallAction ();

			Run ();

			Assert.IsTrue (progressMonitor.IsDisposed);
		}

		[Test]
		public void Run_TwoActions_BeginsProgressMonitorTaskWithTwoItems ()
		{
			CreateRunner ();
			AddInstallAction ();
			AddUninstallAction ();

			Run ();

			Assert.AreEqual (2, progressMonitor.BeginTaskTotalWork);
		}

		[Test]
		public void Run_OneAction_ProgressMonitorEndTaskCalled ()
		{
			CreateRunner ();
			AddInstallAction ();

			Run ();

			Assert.IsTrue (progressMonitor.IsTaskEnded);
		}

		[Test]
		public void Run_TwoActions_BothActionsExecuted ()
		{
			CreateRunner ();
			var action1 = AddInstallAction ();
			var action2 = AddUninstallAction ();

			Run ();

			Assert.IsNotNull (action1.PackageManager.PreviewInstallProject);
			Assert.IsNotNull (action2.PackageManager.PreviewUninstallProject);
		}

		[Test]
		public void Run_TwoActions_ProgressStepCalledTwice ()
		{
			CreateRunner ();
			AddInstallAction ();
			AddUninstallAction ();

			Run ();

			Assert.AreEqual (2, progressMonitor.StepCalledCount);
			Assert.AreEqual (2, progressMonitor.TotalStepWork);
		}

		[Test]
		public void Run_OneActionSuccessfully_SuccessReportedToProgressMonitor ()
		{
			CreateRunner ();
			AddInstallAction ();

			Run ();

			Assert.AreEqual ("Success", progressMonitor.ReportedSuccessMessage);
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_ErrorReportedToProgressMonitor ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ();

			Run ();

			Assert.AreEqual ("Error", progressMonitor.ReportedErrorMessage);
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_ErrorLoggedInPackageConsole ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ();

			Run ();

			progressMonitor.AssertMessageIsLogged ("Value cannot be null.");
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_PackageOperationsFinishedEventFired ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ();
			bool eventFired = false;
			packageManagementEvents.PackageOperationsFinished += (sender, e) => {
				eventFired = true;
			};

			Run ();

			Assert.IsTrue (eventFired);
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_PackageOperationErrorEventFired ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ();
			string exceptionMessage = null;
			packageManagementEvents.PackageOperationError += (sender, e) => {
				exceptionMessage = e.Exception.GetBaseException ().Message;
			};

			Run ();

			Assert.IsTrue (exceptionMessage.Contains ("Value cannot be null."));
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_PackageConsoleDisplayedDueToError ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ();

			Run ();

			Assert.IsTrue (runner.EventsMonitor.IsPackageConsoleShown);
			Assert.AreEqual (progressMonitor, runner.EventsMonitor.ProgressMonitorPassedToShowPackageConsole);
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_InstallPackageOperationsRemovedFromPendingListWhenPackageOperationErrorEventFired ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ();
			int pendingInstallActionsCount = -1;
			packageManagementEvents.PackageOperationError += (sender, e) => {
				pendingInstallActionsCount = runner.PendingInstallActions.Count ();
			};

			Run ();

			Assert.AreEqual (0, pendingInstallActionsCount);
		}

		[Test]
		public void Run_ActionLogsPackageOperationMessage_ProgressMonitorLogsMessage ()
		{
			CreateRunner ();
			AddInstallActionWithCustomExecuteAction (() => {
				packageManagementEvents.OnPackageOperationMessageLogged (MessageLevel.Info, "Message");
			});

			Run ();

			progressMonitor.AssertMessageIsLogged ("Message");
		}

		[Test]
		public void Run_ActionChangesTwoFiles_FileServiceNotifiedOfFileChanges ()
		{
			CreateRunner ();
			string file1 = @"d:\projects\MyProject\packages.config".ToNativePath ();
			string file2 = @"d:\projects\MyProject\Scripts\jquery.js".ToNativePath ();
			AddInstallActionWithCustomExecuteAction (() => {
				packageManagementEvents.OnFileChanged (file1);
				packageManagementEvents.OnFileChanged (file2);
			});

			Run ();

			List<FilePath> filesChanged = runner.EventsMonitor.FilesChanged;
			Assert.AreEqual (2, filesChanged.Count);
			Assert.That (filesChanged, Contains.Item (new FilePath (file1)));
			Assert.That (filesChanged, Contains.Item (new FilePath (file2)));
		}

		[Test]
		public void IsRunning_NothingRunning_IsRunningIsFalse ()
		{
			CreateRunner ();

			Assert.IsFalse (runner.IsRunning);
		}

		[Test]
		public void IsRunning_OneUninstallActionAndRunNotCompleted_IsRunningIsTrue ()
		{
			CreateRunner ();
			AddUninstallAction ();

			RunWithoutBackgroundDispatch ();

			Assert.IsTrue (runner.IsRunning);
		}

		[Test]
		public void IsRunning_OneUninstallActionAndRunCompleted_IsRunningIsFalse ()
		{
			CreateRunner ();
			AddUninstallAction ();

			Run ();

			Assert.IsFalse (runner.IsRunning);
		}

		[Test]
		public void IsRunning_TwoRunsAndOneCompletes_IsRunningIsTrue ()
		{
			CreateRunner ();
			AddUninstallAction ();
			RunWithoutBackgroundDispatch ();
			actions.Clear ();
			AddInstallAction ();
			RunWithoutBackgroundDispatch ();

			runner.ExecuteSingleBackgroundDispatch ();

			Assert.IsTrue (runner.IsRunning);
		}

		[Test]
		public void IsRunning_TwoRunsAndBothComplete_IsRunningIsFalse ()
		{
			CreateRunner ();
			AddUninstallAction ();
			RunWithoutBackgroundDispatch ();
			actions.Clear ();
			AddInstallAction ();
			RunWithoutBackgroundDispatch ();

			runner.ExecuteSingleBackgroundDispatch ();
			runner.ExecuteSingleBackgroundDispatch ();

			Assert.IsFalse (runner.IsRunning);
		}

		[Test]
		public void IsRunning_ExceptionThrownRunningBackgroundDispatcher_IsRunningIsFalse ()
		{
			CreateRunner ();
			AddUninstallAction ();
			runner.CreateEventMonitorAction = (monitor, packageManagementEvents, completionSource) => {
				throw new ApplicationException ("Error");
			};

			Run ();

			Assert.IsFalse (runner.IsRunning);
		}

		[Test]
		public void Instrumentation_OnePackageUninstalled_UninstallCounterIncremented ()
		{
			CreateRunner ();
			var action = AddUninstallAction ();
			var projectAction = new FakeNuGetProjectAction ("Test", "1.2", NuGetProjectActionType.Uninstall);
			action.PackageManager.UninstallActions.Add (projectAction);

			Run ();

			AssertUninstallCounterIncrementedForPackage ("Test", "1.2");
		}

		[Test]
		public void Instrumentation_OnePackageUninstalledWithNoVersion_UninstallCounterIncremented ()
		{
			CreateRunner ();
			var action = AddUninstallAction ();
			var projectAction = new FakeNuGetProjectAction ("Test", null, NuGetProjectActionType.Uninstall);
			action.PackageManager.UninstallActions.Add (projectAction);

			Run ();

			AssertUninstallCounterIncrementedForPackage ("Test");
		}

		[Test]
		public void Instrumentation_OnePackageInstalledWithTwoPackageOperations_UninstallCounterIncremented ()
		{
			CreateRunner ();
			var action = AddInstallAction ();
			AddInstallPackageIntoProjectAction (action.PackageManager, "Bar", "1.3");
			AddUninstallPackageIntoProjectAction (action.PackageManager, "Foo", "1.1");

			Run ();

			AssertUninstallCounterIncrementedForPackage ("Foo", "1.1");
			AssertInstallCounterIncrementedForPackage ("Bar", "1.3");
		}

		[Test]
		public void Instrumentation_OnePackageUpdatedWithTwoPackageOperations_UninstallCounterIncremented ()
		{
			CreateRunner ();
			var action = AddUpdateAction ();
			var projectAction = new FakeNuGetProjectAction ("Bar", "1.3", NuGetProjectActionType.Install);
			action.PackageManager.UpdateActions.Add (projectAction);
			projectAction = new FakeNuGetProjectAction ("Foo", "1.1", NuGetProjectActionType.Uninstall);
			action.PackageManager.UpdateActions.Add (projectAction);

			Run ();

			AssertUninstallCounterIncrementedForPackage ("Foo", "1.1");
			AssertInstallCounterIncrementedForPackage ("Bar", "1.3");
		}

		[Test]
		public void Instrumentation_OnePackageInstalledWithOneInstallAndOneUninstallPackageActions_BothCountersIncremented ()
		{
			CreateRunner ();
			var action = AddInstallAction ();
			AddInstallPackageIntoProjectAction (action.PackageManager, "Bar", "1.3");
			AddUninstallPackageIntoProjectAction (action.PackageManager, "Foo", "1.1");
			actions.Add (action);

			Run ();

			AssertUninstallCounterIncrementedForPackage ("Foo", "1.1");
			AssertInstallCounterIncrementedForPackage ("Bar", "1.3");
		}

		[Test]
		public void Run_ClearConsoleIsTrue_ProgressMonitorWillClearConsole ()
		{
			CreateRunner ();
			AddInstallAction ();

			Run (clearConsole: true);

			Assert.IsTrue (progressMonitorFactory.ClearConsole);
		}

		[Test]
		public void Run_ClearConsoleIsFalse_ProgressMonitorWillNotClearConsole ()
		{
			CreateRunner ();
			AddInstallAction ();

			Run (clearConsole: false);

			Assert.IsFalse (progressMonitorFactory.ClearConsole);
		}

		/// <summary>
		/// Do not open the package console window after the package action is cancelled.
		/// This is done when other exceptions occur during package processing since it is
		/// done in the background and just showing an error in the status bar may not bring
		/// the error enough attention. However if the user cancels the action there is no
		/// need to show the package console after the install fails. This is also a workaround
		/// to prevent any open dialogs from being closed when the package console window is
		/// opened after the NuGet action is cancelled by the user.
		/// </summary>
		[Test]
		public void Run_OneInstallActionThatIsCancelledByUser_PackageConsoleNotDisplayed ()
		{
			CreateRunner ();
			AddInstallAction ();
			RunWithoutBackgroundDispatch ();
			CancelCurrentAction ();
			runner.ExecuteBackgroundDispatch ();

			Assert.IsFalse (runner.EventsMonitor.IsPackageConsoleShown);
		}

		[Test]
		public void RunAsync_OneActionSuccessfully_TaskIsCompleted ()
		{
			CreateRunner ();
			AddInstallAction ();

			Task task = RunAsync ();

			Assert.IsNull (task.Exception);
			Assert.IsFalse (task.IsFaulted);
			Assert.IsTrue (task.IsCompleted);
		}

		[Test]
		public void RunAsync_OneActionStartedButNotFinished_TaskIsNotCompleted ()
		{
			CreateRunner ();
			AddInstallAction ();

			Task task = RunAsyncWithoutBackgroundDispatch ();

			Assert.IsFalse (task.IsCompleted);
		}

		[Test]
		public void RunAsync_ExceptionThrownRunningBackgroundDispatcher_TaskIsFaulted ()
		{
			CreateRunner ();
			var action = AddInstallAction ();
			action.PackageManager.BeforePreviewInstallPackageAsyncAction = () => {
				throw new ApplicationException ("Error");
			};

			Task task = RunAsync ();

			Assert.IsNotNull (task.Exception);
			Assert.IsTrue (task.IsFaulted);
		}

		[Test]
		public void RunAsync_ClearConsoleIsTrue_ProgressMonitorWillClearConsole ()
		{
			CreateRunner ();
			AddInstallAction ();

			RunAsync (clearConsole: true);

			Assert.IsTrue (progressMonitorFactory.ClearConsole);
		}

		[Test]
		public void RunAsync_ClearConsoleIsFalse_ProgressMonitorWillNotClearConsole ()
		{
			CreateRunner ();
			AddInstallAction ();

			RunAsync (clearConsole: false);

			Assert.IsFalse (progressMonitorFactory.ClearConsole);
		}

		[Test]
		public void Run_OneInstallActionThatIsCancelledByUser_CancelledMessageLogged ()
		{
			CreateRunner ();
			var action = AddInstallAction ();
			RunWithoutBackgroundDispatch ();
			action.PackageManager.BeforePreviewInstallPackageAsyncAction = () => {
				CancelCurrentAction ();
			};
			runner.ExecuteBackgroundDispatch ();

			progressMonitor.AssertMessageIsLogged ("A task was canceled");
		}

		[Test]
		public void Cancel_OneInstallActionCancelledDuringProcessing_ErrorReported ()
		{
			CreateRunner ();
			var action = AddInstallAction ();
			action.PackageManager.BeforePreviewInstallPackageAsyncAction = () => {
				runner.Cancel ();
			};

			Run ();

			progressMonitor.AssertMessageIsLogged ("A task was canceled");
			Assert.IsFalse (runner.EventsMonitor.IsPackageConsoleShown);
		}

		[Test]
		public void Cancel_ThreeInstallActionsQueuedIndividuallyAndFirstCancelledDuringProcessing_QueuedActionsAreCleared ()
		{
			CreateRunner ();
			var action1 = AddInstallAction ();
			action1.PackageManager.BeforePreviewInstallPackageAsyncAction = () => {
				runner.Cancel ();
			};
			RunWithoutBackgroundDispatch ();
			actions.Clear ();
			var action2 = AddInstallAction ();
			RunWithoutBackgroundDispatch ();
			actions.Clear ();
			var action3 = AddInstallAction ();
			RunWithoutBackgroundDispatch ();

			runner.ExecuteSingleBackgroundDispatch ();

			progressMonitor.AssertMessageIsLogged ("A task was canceled");
			Assert.AreEqual (0, runner.BackgroundActionsQueued.Count);
			Assert.IsFalse (runner.IsRunning);
		}

		[Test]
		public void IsRunning_CancelledButBackgroundDispatcherStillRunning_ReturnsTrue ()
		{
			CreateRunner ();
			var action = AddInstallAction ();
			action.PackageManager.BeforePreviewInstallPackageAsyncAction = () => {
				runner.Cancel ();
			};
			runner.DispatcherIsDispatchingReturns = true;

			Run ();

			Assert.IsTrue (runner.IsRunning);
		}

		[Test]
		public void Cancel_ThreeRunAsyncActionsQueuedIndividuallyAndFirstCancelledDuringProcessing_QueuedTasksAreCancelled ()
		{
			CreateRunner ();
			var action = AddInstallAction ();
			action.PackageManager.BeforePreviewInstallPackageAsyncAction = () => {
				runner.Cancel ();
			};
			var action1 = AddInstallAction ();
			action1.PackageManager.BeforePreviewInstallPackageAsyncAction = () => {
				runner.Cancel ();
			};
			Task task1 = RunAsyncWithoutBackgroundDispatch ();
			actions.Clear ();
			var action2 = AddInstallAction ();
			Task task2 = RunAsyncWithoutBackgroundDispatch ();
			actions.Clear ();
			var action3 = AddInstallAction ();
			Task task3 = RunAsyncWithoutBackgroundDispatch ();

			runner.ExecuteSingleBackgroundDispatch ();

			Assert.IsNotNull (task1.Exception);
			Assert.IsTrue (task1.IsFaulted);
			Assert.IsTrue (task2.IsCanceled);
			Assert.IsTrue (task3.IsCanceled);
			Assert.IsFalse (runner.IsRunning);
		}

		[Test]
		public void GetPendingActionsInfo_InstallActionBeingRun_InstallPending ()
		{
			CreateRunner ();
			AddInstallAction ();
			RunWithoutBackgroundDispatch ();

			var info = runner.GetPendingActionsInfo ();

			Assert.IsTrue (info.IsInstallPending);
		}

		[Test]
		public void GetPendingActionsInfo_UninstallActionBeingRun_UninstallPending ()
		{
			CreateRunner ();
			AddUninstallAction ();
			RunWithoutBackgroundDispatch ();

			var info = runner.GetPendingActionsInfo ();

			Assert.IsTrue (info.IsUninstallPending);
		}

		[Test]
		public void GetPendingActionsInfo_RestoreActionBeingRun_RestorePending ()
		{
			CreateRunner ();
			AddRestoreAction ();
			RunWithoutBackgroundDispatch ();

			var info = runner.GetPendingActionsInfo ();

			Assert.IsTrue (info.IsRestorePending);
		}

		[Test]
		public void GetPendingActionsInfo_InstallAndUninstallActions_InstallAndUninstallPending ()
		{
			CreateRunner ();
			AddInstallAction ();
			AddUninstallAction ();

			RunWithoutBackgroundDispatch ();

			var info = runner.GetPendingActionsInfo ();

			Assert.IsTrue (info.IsInstallPending);
			Assert.IsTrue (info.IsUninstallPending);
			Assert.IsFalse (info.IsRestorePending);
		}

		[Test]
		public void Run_ImportRemoved_ProjectBuilderDisposed ()
		{
			CreateRunner ();
			var project = new FakeDotNetProject ();
			var solution = new FakeSolution ();
			solution.Projects.Add (project);
			project.ParentSolution = solution;
			AddInstallActionWithCustomExecuteAction (() => {
				packageManagementEvents.OnImportRemoved (project, "Test.targets");
			});

			Run ();

			Assert.IsTrue (project.IsProjectBuilderDisposed);
		}

		[Test]
		public void Run_ImportRemoved_ProjectIsReevaluated ()
		{
			CreateRunner ();
			var project = new FakeDotNetProject ();
			var solution = new FakeSolution ();
			solution.Projects.Add (project);
			project.ParentSolution = solution;
			AddInstallActionWithCustomExecuteAction (() => {
				packageManagementEvents.OnImportRemoved (project, "Test.targets");
			});

			Run ();

			Assert.IsTrue (project.IsReevaluated);
		}

		[Test]
		public void Run_ImportAdded_ProjectIsReevaluated ()
		{
			CreateRunner ();
			var project = new FakeDotNetProject ();
			var solution = new FakeSolution ();
			solution.Projects.Add (project);
			project.ParentSolution = solution;
			AddInstallActionWithCustomExecuteAction (() => {
				packageManagementEvents.OnImportAdded (project, "Test.targets");
			});

			Run ();

			Assert.IsTrue (project.IsReevaluated);
		}

		[Test]
		public void Run_NoImportAddedOrRemoved_ProjectIsNotReevaluated ()
		{
			CreateRunner ();
			var project = new FakeDotNetProject ();
			var solution = new FakeSolution ();
			solution.Projects.Add (project);
			project.ParentSolution = solution;
			AddInstallActionWithCustomExecuteAction (() => {});

			Run ();

			Assert.IsFalse (project.IsReevaluated);
		}
	}
}

