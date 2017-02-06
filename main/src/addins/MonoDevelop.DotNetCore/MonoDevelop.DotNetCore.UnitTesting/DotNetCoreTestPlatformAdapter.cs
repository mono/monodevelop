//
// DotNetCoreTestPlatformAdapter.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Payloads;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.UnitTesting;

namespace MonoDevelop.DotNetCore.UnitTesting
{
	class DotNetCoreTestPlatformAdapter
	{
		DiscoveredTests discoveredTests = new DiscoveredTests ();
		IDataSerializer dataSerializer = JsonDataSerializer.Instance;
		ProcessWrapper dotNetProcess;
		SocketCommunicationManager communicationManager;
		int clientConnectionTimeOut = 15000;
		Thread messageProcessingThread;
		bool stopping;
		string testAssemblyPath;
		TestContext testContext;
		TestResultBuilder testResultBuilder;

		public bool IsDiscoveringTests { get; private set; }

		public DiscoveredTests DiscoveredTests {
			get { return discoveredTests; }
		}

		public event EventHandler DiscoveryCompleted;

		void OnDiscoveryCompleted ()
		{
			IsDiscoveringTests = false;
			DiscoveryCompleted?.Invoke (this, new EventArgs ());
		}

		public event EventHandler DiscoveryFailed;

		void OnDiscoveryFailed ()
		{
			HasDiscoveryFailed = true;
			DiscoveryFailed?.Invoke (this, new EventArgs ());
		}

		public DotNetCorePath DotNetCorePath { get; set; } = new DotNetCorePath ();

		public void StartDiscovery (string testAssemblyPath)
		{
			try {
				StartDiscoveryInternal (testAssemblyPath);
			} catch (Exception ex) {
				OnDiscoveryFailed (ex);
			}
		}

		void StartDiscoveryInternal (string testAssemblyPath)
		{
			this.testAssemblyPath = testAssemblyPath;

			if (DotNetCorePath.IsMissing) {
				throw new ApplicationException (".NET Core is not installed.");
			}

			HasDiscoveryFailed = false;
			discoveredTests = new DiscoveredTests ();

			if (communicationManager == null) {
				Start ();
			} else {
				SendStartDiscoveryMessage ();
			}
		}

		void Start ()
		{
			communicationManager = new SocketCommunicationManager ();
			int port = communicationManager.HostServer ();

			dotNetProcess = StartDotNetProcess (port);

			stopping = false;
			IsDiscoveringTests = true;

			messageProcessingThread = 
				new Thread (ReceiveMessages) {
				IsBackground = true
			};
			messageProcessingThread.Start ();
		}

		ProcessWrapper StartDotNetProcess (int port)
		{
			return Runtime.ProcessService.StartProcess (
				DotNetCorePath.FileName,
				GetVSTestArguments (port),
				null,
				DotNetCoreProcessExited);
		}

		string GetVSTestArguments (int port)
		{
			return string.Format (
				"vstest /parentprocessid:{0} /port:{1}",
				Process.GetCurrentProcess ().Id,
				port
			);
		}

		void DotNetCoreProcessExited (object sender, EventArgs e)
		{
			Console.WriteLine ("Exited");
		}

		public void Stop ()
		{
			stopping = true;

			try {
				if (communicationManager != null) {
					communicationManager.StopServer ();
					communicationManager = null;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("TestPlatformCommunicationManager stop error.", ex);
			}

			try {
				if (dotNetProcess != null) {
					if (!dotNetProcess.HasExited) {
						dotNetProcess.Dispose ();
					}
					dotNetProcess = null;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("VSTest process dispose error.", ex);
			}
		}

		void ReceiveMessages ()
		{
			try {
				communicationManager.AcceptClientAsync ();
				bool success = communicationManager.WaitForClientConnection (clientConnectionTimeOut);

				if (!success) {
					LoggingService.LogError ("Timed out waiting for client connection.");
					OnDiscoveryFailed ();
					return;
				}
			} catch (Exception ex) {
				OnDiscoveryFailed (ex);
			}

			while (!stopping) {
				try {
					Message message = communicationManager.ReceiveMessage ();
					ProcessMessage (message);
				} catch (IOException) {
					// Ignore.
				} catch (Exception ex) {
					LoggingService.LogError ("TestPlatformAdapter receive message error.", ex);
				}
			}
		}

		void SendStartDiscoveryMessage ()
		{
			var message = new DiscoveryRequestPayload {
				Sources = new [] { testAssemblyPath },
				RunSettings = null
			};

			communicationManager.SendMessage (MessageType.StartDiscovery, message);
		}

		void ProcessMessage (Message message)
		{
			switch (message.MessageType) {
				case MessageType.SessionConnected:
				SendStartDiscoveryMessage ();
				break;

				case MessageType.TestCasesFound:
				OnTestCasesFound (message);
				break;

				case MessageType.DiscoveryComplete:
				OnDiscoveryCompleted (message);
				break;

				case MessageType.TestMessage:
				OnTestMessage (message);
				break;

				case MessageType.TestRunStatsChange:
				OnTestRunChanged (message);
				break;

				case MessageType.ExecutionComplete:
				OnTestRunComplete (message);
				break;
			}
		}

		void OnDiscoveryFailed (Exception ex)
		{
			LoggingService.LogError ("TestPlatformCommunicationManager error", ex);
			OnDiscoveryFailed ();
		}

		void OnTestCasesFound (Message message)
		{
			var tests = dataSerializer.DeserializePayload<IEnumerable<TestCase>> (message);
			discoveredTests.Add (tests);
		}

		void OnDiscoveryCompleted (Message message)
		{
			var discoveryCompletePayload = dataSerializer.DeserializePayload<DiscoveryCompletePayload> (message);

			if (discoveryCompletePayload.LastDiscoveredTests != null) {
				discoveredTests.Add (discoveryCompletePayload.LastDiscoveredTests);
			}

			OnDiscoveryCompleted ();
		}

		public bool HasDiscoveryFailed { get; set; }

		public void RunTests (IEnumerable<TestCase> testCases)
		{
			var message = new TestRunRequestPayload {
				TestCases = testCases.ToList (),
				RunSettings = null
			};
			communicationManager.SendMessage (MessageType.TestRunSelectedTestCasesDefaultHost, message);
		}

		public void RunTests (IEnumerable<string> testAssemblies)
		{
			var message = new TestRunRequestPayload {
				Sources = testAssemblies.ToList (),
				RunSettings = null
			};
			communicationManager.SendMessage (MessageType.TestRunAllSourcesWithDefaultHost, message);
		}

		public void RunTests (
			TestContext testContext,
			IDotNetCoreTestProvider testProvider,
			string testAssemblyPath)
		{
			try {
				IsRunningTests = true;
				testResultBuilder = new TestResultBuilder (testContext, testProvider);

				var tests = testProvider.GetTests ();
				if (tests == null) {
					RunTests (new [] { testAssemblyPath });
				} else {
					RunTests (tests);
				}

			} catch (Exception ex) {
				testContext.Monitor.ReportRuntimeError (
					GettextCatalog.GetString ("Failed to run tests."),
					ex);

				if (testResultBuilder != null)
					testResultBuilder.CreateFailure (ex);

				IsRunningTests = false;
			}
		}

		public bool IsRunningTests { get; private set; }

		public UnitTestResult TestResult {
			get { return testResultBuilder.TestResult; }
		}

		void OnTestMessage (Message message)
		{
			var currentContext = testContext;
			if (currentContext == null)
				return;

			var payload = dataSerializer.DeserializePayload<TestMessagePayload> (message);
			currentContext.Monitor.WriteGlobalLog (payload.Message);
		}

		void OnTestRunComplete (Message message)
		{
			var testRunCompletePayload = dataSerializer.DeserializePayload<TestRunCompletePayload> (message);
			testResultBuilder.OnTestRunComplete (testRunCompletePayload);

			IsRunningTests = false;
			testContext = null;
		}

		void OnTestRunChanged (Message message)
		{
			var eventArgs = dataSerializer.DeserializePayload<TestRunChangedEventArgs> (message);
			testResultBuilder.OnTestRunChanged (eventArgs);
		}

		public void CancelTestRun ()
		{
			if (IsRunningTests) {
				communicationManager.SendMessage (MessageType.CancelTestRun);
			}
		}
	}
}
