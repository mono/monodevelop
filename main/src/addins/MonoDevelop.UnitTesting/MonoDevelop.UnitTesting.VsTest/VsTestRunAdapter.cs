//
// VsTestRunAdapter.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Payloads;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.Projects;

namespace MonoDevelop.UnitTesting.VsTest
{
	class VsTestRunAdapter : VsTestAdapter
	{
		public VsTestRunAdapter ()
		{
		}

		RunOrDebugJob runJobInProgress;

		public static VsTestRunAdapter Instance { get; } = new VsTestRunAdapter ();

		class RunOrDebugJob
		{
			public TestContext TestContext { get; }
			public Project Project { get; }
			public TestResultBuilder TestResultBuilder { get; }
			public TaskCompletionSource<UnitTestResult> TaskSource { get; }
			public ProcessAsyncOperation ProcessOperation { get; set; }

			public RunOrDebugJob (TestContext testContext, IVsTestTestProvider rootTest)
			{
				TestContext = testContext;
				TestResultBuilder = new TestResultBuilder (testContext, rootTest);
				Project = rootTest.Project;
				TaskSource = TestResultBuilder.TaskSource;
			}
		}

		protected override void ProcessMessage (Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Message message)
		{
			switch (message.MessageType) {
			case MessageType.TestMessage:
				OnTestMessage (message);
				break;
			case MessageType.TestRunStatsChange:
				OnTestRunChanged (message);
				break;
			case MessageType.ExecutionComplete:
				OnTestRunComplete (message);
				break;
			case MessageType.CustomTestHostLaunch:
				OnCustomTestLaunch (message);
				break;
			default:
				base.ProcessMessage (message);
				break;
			}
		}

		void RunTests (Project project, IEnumerable<TestCase> testCases)
		{
			var message = new TestRunRequestPayload {
				TestCases = testCases.ToList (),
				RunSettings = GetRunSettings (project)
			};
			communicationManager.SendMessage (MessageType.TestRunSelectedTestCasesDefaultHost, message);
		}

		UnitTestResult ReportRunFailure (TestContext testContext, Exception exception)
		{
			testContext.Monitor.ReportRuntimeError (exception.Message, exception);
			return UnitTestResult.CreateFailure (exception);
		}

		UnitTestResult HandleMissingAssemblyOnRun (TestContext testContext, string testAssemblyPath)
		{
			var exception = new FileNotFoundException (
				GettextCatalog.GetString ("Unable to run tests. Assembly not found '{0}'", testAssemblyPath),
				testAssemblyPath);

			return ReportRunFailure (testContext, exception);
		}

		void RunTests (Project project)
		{
			var message = new TestRunRequestPayload {
				Sources = new List<string> (new [] { GetAssemblyFileName (project) }),
				RunSettings = GetRunSettings (project)
			};
			communicationManager.SendMessage (MessageType.TestRunAllSourcesWithDefaultHost, message);
		}

		public async Task<UnitTestResult> RunTests (
			UnitTest test,
			TestContext testContext,
			IVsTestTestProvider testProvider)
		{
			await Start ();
			try {
				runJobInProgress = new RunOrDebugJob (testContext, testProvider);
				testContext.Monitor.CancellationToken.Register (CancelTestRun);
				var tests = testProvider.GetTests ();
				if (testContext.ExecutionContext.ExecutionHandler != null) {
					if (tests == null) {
						GetProcessStartInfo (testProvider.Project);
					} else {
						GetProcessStartInfo (testProvider.Project, tests);
					}
				} else {
					if (tests == null) {
						RunTests (testProvider.Project);
					} else {
						RunTests (testProvider.Project, tests);
					}
				}
				return await runJobInProgress.TaskSource.Task;
			} catch (OperationCanceledException) {
				return runJobInProgress.TestResultBuilder.TestResult;
			} catch (Exception ex) {
				testContext.Monitor.ReportRuntimeError (
					GettextCatalog.GetString ("Failed to run tests."),
					ex);

				if (runJobInProgress.TestResultBuilder != null)
					runJobInProgress.TestResultBuilder.CreateFailure (ex);
				return runJobInProgress.TestResultBuilder.TestResult;
			}
		}

		void OnTestMessage (Message message)
		{
			var payload = dataSerializer.DeserializePayload<TestMessagePayload> (message);
			runJobInProgress.TestContext.Monitor.WriteGlobalLog (payload.Message + Environment.NewLine);
		}

		void OnTestRunComplete (Message message)
		{
			var testRunCompletePayload = dataSerializer.DeserializePayload<TestRunCompletePayload> (message);
			runJobInProgress.TestResultBuilder.OnTestRunComplete (testRunCompletePayload);
		}

		void OnTestRunChanged (Message message)
		{
			var eventArgs = dataSerializer.DeserializePayload<TestRunChangedEventArgs> (message);
			runJobInProgress.TestResultBuilder.OnTestRunChanged (eventArgs);
		}

		void CancelTestRun ()
		{
			runJobInProgress?.TaskSource?.TrySetCanceled ();

			try {
				communicationManager.SendMessage (MessageType.CancelTestRun);
			} catch (Exception ex) {
				LoggingService.LogError ("CancelTestRun error.", ex);
			}

			try {
				if (runJobInProgress?.ProcessOperation != null) {
					if (!runJobInProgress.ProcessOperation.IsCompleted)
						runJobInProgress.ProcessOperation.Cancel ();
					runJobInProgress.ProcessOperation = null;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("CancelTestRun error.", ex);
			}
		}

		void GetProcessStartInfo (Project project, IEnumerable<TestCase> testCases)
		{
			var message = new TestRunRequestPayload {
				TestCases = testCases.ToList (),
				RunSettings = GetRunSettings (project)
			};
			communicationManager.SendMessage (MessageType.GetTestRunnerProcessStartInfoForRunSelected, message);
		}

		void GetProcessStartInfo (Project project)
		{
			var message = new TestRunRequestPayload {
				Sources = new List<string> (new [] { GetAssemblyFileName (project) }),
				RunSettings = GetRunSettings (project)
			};
			communicationManager.SendMessage (MessageType.GetTestRunnerProcessStartInfoForRunAll, message);
		}

		void OnCustomTestLaunch (Message message)
		{
			var launchAckPayload = new CustomHostLaunchAckPayload {
				HostProcessId = -1
			};

			try {
				var startInfo = dataSerializer.DeserializePayload<TestProcessStartInfo> (message);
				launchAckPayload.HostProcessId = StartCustomTestHost (startInfo, runJobInProgress.TestContext);
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to start custom test host.", ex);
				launchAckPayload.ErrorMessage = ex.Message;
				runJobInProgress.TestContext.Monitor.ReportRuntimeError (GettextCatalog.GetString ("Unable to start test host."), ex);
			} finally {
				communicationManager.SendMessage (MessageType.CustomTestHostLaunchCallback, launchAckPayload);
			}
		}

		int StartCustomTestHost (TestProcessStartInfo startInfo, TestContext currentTestContext)
		{
			OperationConsole console = currentTestContext.ExecutionContext.ConsoleFactory.CreateConsole (
				OperationConsoleFactory.CreateConsoleOptions.Default.WithTitle (GettextCatalog.GetString ("Unit Tests")));
			ExecutionCommand command;

			if (runJobInProgress.Project is DotNetProject dnp) {
				if (dnp.HasFlavor<DotNetCoreProjectExtension> ()) {
					command = new DotNetCoreExecutionCommand (
						startInfo.WorkingDirectory,
						startInfo.FileName,
						startInfo.Arguments
					) {
						EnvironmentVariables = startInfo.EnvironmentVariables
					};
					((DotNetCoreExecutionCommand)command).Command = startInfo.FileName;
					((DotNetCoreExecutionCommand)command).Arguments = startInfo.Arguments;
				} else {
					var portArgument = startInfo.Arguments.IndexOf (" --port", StringComparison.Ordinal);
					var assembly = startInfo.Arguments.Remove (portArgument - 1);
					var arguments = startInfo.Arguments.Substring (portArgument + 1);
					command = new DotNetExecutionCommand (
						assembly,
						arguments,
						startInfo.WorkingDirectory,
						startInfo.EnvironmentVariables
					);
				}
			} else {
				command = new NativeExecutionCommand (
						startInfo.WorkingDirectory,
						startInfo.FileName,
						startInfo.Arguments,
						startInfo.EnvironmentVariables);
			}

			runJobInProgress.ProcessOperation = currentTestContext.ExecutionContext.ExecutionHandler.Execute (command, console);


			//This is horrible hack...
			//VSTest wants us to send it PID of process our debugger just started so it can kill it when tests are finished
			//VSCode debug protocol doesn't give us PID of debugee
			//But we must give VSTest valid PID or it won't work... In past we gave it IDE PID, but with new versions of
			//VSTest it means it will kill our IDE... Hence give it PID 1 and hope it won't kill it
			return 1;
		}
	}
}
