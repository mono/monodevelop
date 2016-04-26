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
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;
using NuGet;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class BackgroundPackageActionRunnerTests
	{
		TestableBackgroundPackageActionRunner runner;
		FakeProgressMonitorFactory progressMonitorFactory;
		PackageManagementEvents packageManagementEvents;
		PackageManagementProgressProvider progressProvider;
		List<IPackageAction> actions;
		ProgressMonitorStatusMessage progressMessage;
		FakeProgressMonitor progressMonitor;
		FakePackageRepositoryFactoryEvents repositoryFactoryEvents;
		TestableInstrumentationService instrumentationService;

		void CreateRunner ()
		{
			actions = new List<IPackageAction> ();
			progressMessage = new ProgressMonitorStatusMessage ("Status", "Success", "Error", "Warning");
			packageManagementEvents = new PackageManagementEvents ();
			progressMonitorFactory = new FakeProgressMonitorFactory ();
			progressMonitor = progressMonitorFactory.ProgressMonitor;
			repositoryFactoryEvents = new FakePackageRepositoryFactoryEvents ();
			progressProvider = new PackageManagementProgressProvider (repositoryFactoryEvents, handler => {
				handler.Invoke ();
			});
			instrumentationService = new TestableInstrumentationService ();

			runner = new TestableBackgroundPackageActionRunner (
				progressMonitorFactory,
				packageManagementEvents,
				progressProvider,
				instrumentationService);
		}

		void Run ()
		{
			RunWithoutBackgroundDispatch ();
			runner.ExecuteBackgroundDispatch ();
		}

		void RunWithoutBackgroundDispatch ()
		{
			runner.Run (progressMessage, actions);
		}

		FakeInstallPackageAction AddInstallAction ()
		{
			var action = new FakeInstallPackageAction (new FakePackageManagementProject (), packageManagementEvents);
			action.Operations = new List <PackageOperation> ();
			action.Logger = new FakeLogger ();
			actions.Add (action);
			return action;
		}

		FakeUpdatePackageAction AddUpdateAction ()
		{
			var action = new FakeUpdatePackageAction (
				new FakePackageManagementProject (),
				packageManagementEvents,
				new FakeFileRemover (),
				new FakeLicenseAcceptanceService ());
			action.Operations = new List <PackageOperation> ();
			action.Logger = new FakeLogger ();
			actions.Add (action);
			return action;
		}

		FakeInstallPackageAction AddInstallActionWithPowerShellScript (string packageId = "Test")
		{
			FakeInstallPackageAction action = AddInstallAction ();
			var package = new FakePackage (packageId);
			package.AddFile (@"tools\install.ps1");
			var operations = new List<PackageOperation> ();
			operations.Add (new PackageOperation (package, PackageAction.Install));
			action.Operations = operations;
			action.Package = package;
			return action;
		}

		void AddInstallActionWithLicenseToAccept (string packageId = "Test")
		{
			FakeInstallPackageAction action = AddInstallAction ();
			var package = new FakePackage (packageId) {
				RequireLicenseAcceptance = true
			};
			var operations = new List<PackageOperation> ();
			operations.Add (new PackageOperation (package, PackageAction.Install));
			action.Operations = operations;
			action.Package = package;
			action.LicensesMustBeAccepted = false;
		}

		void AddInstallActionWithMissingPackageId (string packageId = "Unknown")
		{
			var action = new InstallPackageAction (new FakePackageManagementProject (), packageManagementEvents);
			action.PackageId = packageId;
			actions.Add (action);
		}

		void AddInstallActionWithCustomExecuteAction (Action executeAction)
		{
			FakeInstallPackageAction action = AddInstallAction ();
			action.ExecuteAction = executeAction;
		}

		FakeUninstallPackageAction AddUninstallAction ()
		{
			var action = new FakeUninstallPackageAction (new FakePackageManagementProject ());
			action.Package = new FakePackage ();
			action.Logger = new FakeLogger ();
			actions.Add (action);
			return action;
		}

		FakeInstallPackageAction AddInstallActionWithMSBuildTargetsFile (
			string packageId = "Test",
			PackageAction packageAction = PackageAction.Install)
		{
			FakeInstallPackageAction action = AddInstallAction ();
			var package = new FakePackage (packageId);
			package.AddFile (@"build\net40\" + packageId + ".targets");
			var operations = new List<PackageOperation> ();
			operations.Add (new PackageOperation (package, packageAction));
			action.Operations = operations;
			action.Package = package;
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
			Assert.AreEqual (packageId, instrumentationService.UninstallPackageMetadata["PackageId"]);
			Assert.AreEqual (packageVersion, instrumentationService.UninstallPackageMetadata["PackageVersion"]);
		}

		void AssertUninstallCounterIncrementedForPackageOperation (string packageId, string packageVersion)
		{
			AssertCounterIncrementedForPackage (instrumentationService.UninstallPackageMetadata, packageId, packageVersion);
		}

		void AssertUninstallCounterIncrementedForPackage (string packageId)
		{
			Assert.AreEqual (packageId, instrumentationService.UninstallPackageMetadata["PackageId"]);
			Assert.IsFalse (instrumentationService.UninstallPackageMetadata.ContainsKey ("PackageVersion"));
		}

		[Test]
		public void Run_OneInstallActionAndOneUninstallActionAndRunNotCompleted_InstallActionMarkedAsPending ()
		{
			CreateRunner ();
			InstallPackageAction expectedAction = AddInstallAction ();
			AddUninstallAction ();

			RunWithoutBackgroundDispatch ();

			Assert.AreEqual (expectedAction, runner.PendingInstallActions.Single ());
		}

		[Test]
		public void Run_OneInstallActionAndRunNotCompleted_PackageOperationsStartedEventRaisedAfterInstallActionMarkedAsPending ()
		{
			CreateRunner ();
			InstallPackageAction expectedAction = AddInstallAction ();
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
			FakeInstallPackageAction action1 = AddInstallAction ();
			FakeUninstallPackageAction action2 = AddUninstallAction ();

			Run ();

			Assert.IsTrue (action1.IsExecuteCalled);
			Assert.IsTrue (action2.IsExecuted);
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
		public void Run_OneInstallActionWithPowerShellScripts_WarningNotReportedToProgressMonitor ()
		{
			CreateRunner ();
			AddInstallActionWithPowerShellScript ();

			Run ();

			Assert.IsNull (progressMonitor.ReportedWarningMessage);
		}

		[Test]
		public void Run_OneInstallActionWithPowerShellScripts_WarningMessageLoggedInPackageConsole ()
		{
			CreateRunner ();
			AddInstallActionWithPowerShellScript ("Test");

			Run ();

			progressMonitor.AssertMessageIsLogged ("WARNING: Test Package contains PowerShell scripts which will not be run.");
		}

		[Test]
		public void Run_OneInstallActionWithLicenseToAccept_WarningReportedToProgressMonitor ()
		{
			CreateRunner ();
			AddInstallActionWithLicenseToAccept ();

			Run ();

			Assert.AreEqual ("Warning", progressMonitor.ReportedWarningMessage);
		}

		[Test]
		public void Run_OneInstallActionWithLicenseToAccept_WarningMessageLoggedInPackageConsole ()
		{
			CreateRunner ();
			AddInstallActionWithLicenseToAccept ("Test");

			Run ();

			progressMonitor.AssertMessageIsLogged ("The Test package has a license agreement");
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_ErrorReportedToProgressMonitor ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ("Unknown");

			Run ();

			Assert.AreEqual ("Error", progressMonitor.ReportedErrorMessage);
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_ErrorLoggedInPackageConsole ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ("Unknown");

			Run ();

			progressMonitor.AssertMessageIsLogged ("Unable to find package 'Unknown'.");
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_PackageOperationsFinishedEventFired ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ("Unknown");
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
			AddInstallActionWithMissingPackageId ("Unknown");
			string exceptionMessage = null;
			packageManagementEvents.PackageOperationError += (sender, e) => {
				exceptionMessage = e.Exception.Message;
			};

			Run ();

			Assert.AreEqual ("Unable to find package 'Unknown'.", exceptionMessage);
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_PackageConsoleDisplayedDueToError ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ("Unknown");

			Run ();

			Assert.IsTrue (runner.EventsMonitor.IsPackageConsoleShown);
			Assert.AreEqual (progressMonitor, runner.EventsMonitor.ProgressMonitorPassedToShowPackageConsole);
		}

		[Test]
		public void Run_OneInstallActionWithMissingPackageId_InstallPackageOperationsRemovedFromPendingListWhenPackageOperationErrorEventFired ()
		{
			CreateRunner ();
			AddInstallActionWithMissingPackageId ("Unknown");
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
		public void Run_ActionDownloadsTwoPackages_DownloadingMessageLoggedOnceForEachDownloadOperationByProgressMonitor ()
		{
			CreateRunner ();
			AddInstallActionWithCustomExecuteAction (() => {
				var repository = new FakePackageRepository ();
				repositoryFactoryEvents.RaiseRepositoryCreatedEvent (new PackageRepositoryFactoryEventArgs (repository));

				var progress = new ProgressEventArgs ("Download1", 100);
				repository.RaiseProgressAvailableEvent (progress);

				progress = new ProgressEventArgs ("Download2", 50);
				repository.RaiseProgressAvailableEvent (progress);

				progress = new ProgressEventArgs ("Download2", 100);
				repository.RaiseProgressAvailableEvent (progress);
			});

			Run ();

			progressMonitor.AssertMessageIsLogged ("Download1");
			progressMonitor.AssertMessageIsLogged ("Download2");
			progressMonitor.AssertMessageIsNotLogged ("Download2" + Environment.NewLine + "Download2");
		}

		[Test]
		public void Run_OneInstallActionWithPackageOperationWithCustomMSBuildTask_TypeSystemIsRefreshed ()
		{
			CreateRunner ();
			FakeInstallPackageAction action = AddInstallActionWithMSBuildTargetsFile ();
			action.ExecuteAction = () => {
				packageManagementEvents.OnParentPackageInstalled (action.Package, action.Project, action.Operations);
			};

			Run ();

			Assert.IsTrue (runner.EventsMonitor.IsTypeSystemRefreshed);
			Assert.AreEqual (action.Project, runner.EventsMonitor.ProjectsPassedToReconnectAssemblyReferences [0]);
			Assert.IsNotNull (action.Project);
		}

		[Test]
		public void Run_OneUninstallActionWithPackageOperationWithCustomMSBuildTask_TypeSystemIsNotRefreshed ()
		{
			CreateRunner ();
			FakeInstallPackageAction action = AddInstallActionWithMSBuildTargetsFile ("Test", PackageAction.Uninstall);
			action.ExecuteAction = () => {
				packageManagementEvents.OnParentPackageInstalled (action.Package, action.Project, action.Operations);
			};

			Run ();

			Assert.IsFalse (runner.EventsMonitor.IsTypeSystemRefreshed);
		}

		[Test]
		public void Run_OneInstallActionNoCustomMSBuildTask_TypeSystemIsNotRefreshed ()
		{
			CreateRunner ();
			FakeInstallPackageAction action = AddInstallActionWithPowerShellScript ();
			action.ExecuteAction = () => {
				packageManagementEvents.OnParentPackageInstalled (action.Package, action.Project, action.Operations);
			};

			Run ();

			Assert.IsFalse (runner.EventsMonitor.IsTypeSystemRefreshed);
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
			runner.CreateEventMonitorAction = (monitor, packageManagementEvents, progressProvider) => {
				throw new ApplicationException ("Error");
			};

			Run ();

			Assert.IsFalse (runner.IsRunning);
		}

		[Test]
		public void Instrumentation_OnePackageUninstalled_UninstallCounterIncremented ()
		{
			CreateRunner ();
			FakeUninstallPackageAction action = AddUninstallAction ();
			action.Package = new FakePackage ("Test", "1.2");

			Run ();

			AssertUninstallCounterIncrementedForPackage ("Test", "1.2");
		}

		[Test]
		public void Instrumentation_OnePackageUninstalledWithNoVersion_UninstallCounterIncremented ()
		{
			CreateRunner ();
			FakeUninstallPackageAction action = AddUninstallAction ();
			var package = new FakePackage ("Test");
			package.Version = null;
			action.Package = package;

			Run ();

			AssertUninstallCounterIncrementedForPackage ("Test");
		}

		[Test]
		public void Instrumentation_OnePackageInstalledWithTwoPackageOperations_UninstallCounterIncremented ()
		{
			CreateRunner ();
			FakeInstallPackageAction action = AddInstallAction ();
			action.Package = new FakePackage ("Test", "1.2");
			action.AddInstallPackageOperation ("Bar", "1.3");
			action.AddUninstallPackageOperation ("Foo", "1.1");

			Run ();

			AssertUninstallCounterIncrementedForPackageOperation ("Foo", "1.1");
			AssertInstallCounterIncrementedForPackage ("Bar", "1.3");
		}

		[Test]
		public void Instrumentation_OnePackageUpdatedWithTwoPackageOperations_UninstallCounterIncremented ()
		{
			CreateRunner ();
			FakeUpdatePackageAction action = AddUpdateAction ();
			action.Package = new FakePackage ("Test", "1.2");
			action.AddInstallPackageOperation ("Bar", "1.3");
			action.AddUninstallPackageOperation ("Foo", "1.1");

			Run ();

			AssertUninstallCounterIncrementedForPackageOperation ("Foo", "1.1");
			AssertInstallCounterIncrementedForPackage ("Bar", "1.3");
		}
	}
}

