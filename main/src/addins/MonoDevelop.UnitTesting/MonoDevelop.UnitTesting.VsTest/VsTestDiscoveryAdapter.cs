//
// VsTestDiscoveryAdapter.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Payloads;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.UnitTesting.VsTest
{
	class VsTestDiscoveryAdapter : VsTestAdapter
	{
		ProgressMonitor monitor;
		Pad pad;
		public VsTestDiscoveryAdapter ()
		{
			monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				"TestDiscoveryConsole",
				GettextCatalog.GetString ("Test Discovery Console"),
				Stock.Console,
				false,
				true,
				false);
			pad = IdeApp.Workbench.ProgressMonitors.GetPadForMonitor (monitor);
		}

		ConcurrentQueue<DiscoveryJob> discoveryQueue = new ConcurrentQueue<DiscoveryJob> ();
		DiscoveryJob discoveryJobInProgress;

		public static VsTestDiscoveryAdapter Instance { get; } = new VsTestDiscoveryAdapter ();

		class DiscoveryJob
		{
			public Project project;
			public DiscoveredTests tests = new DiscoveredTests ();
			public TaskCompletionSource<DiscoveredTests> taskSource = new TaskCompletionSource<DiscoveredTests> ();
		}

		protected override void ProcessMessage (Message message)
		{
			switch (message.MessageType) {
			case MessageType.TestCasesFound:
				OnTestCasesFound (message);
				break;
			case MessageType.DiscoveryComplete:
				OnDiscoveryCompleted (message);
				break;
			case MessageType.TestMessage:
				OnTestMessage (message);
				break;
			default:
				base.ProcessMessage (message);
				break;
			}
		}

		public async Task<DiscoveredTests> DiscoverTestsAsync (Project project)
		{
			await Start ();
			var job = new DiscoveryJob () { project = project };
			discoveryQueue.Enqueue (job);
			ProcessDiscoveryQueue ();
			return await job.taskSource.Task;
		}

		void ProcessDiscoveryQueue ()
		{
			if (discoveryQueue.IsEmpty)
				return;
			if (discoveryJobInProgress != null)
				return;
			if (!discoveryQueue.TryDequeue (out var newJob))
				return;
			discoveryJobInProgress = newJob;
			var testAssemblyFile = discoveryJobInProgress.project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
			if (!File.Exists (testAssemblyFile)) {
				discoveryJobInProgress.taskSource.SetResult (discoveryJobInProgress.tests);
				discoveryJobInProgress = null;
				ProcessDiscoveryQueue ();
				return;
			}
			SendExtensionList (GetTestAdapters (discoveryJobInProgress.project).Split (';'));
			var message = new DiscoveryRequestPayload {
				Sources = new string [] { testAssemblyFile },
				RunSettings = GetRunSettings (discoveryJobInProgress.project)
			};
			communicationManager.SendMessage (MessageType.StartDiscovery, message);
		}

		void OnTestCasesFound (Message message)
		{
			var tests = dataSerializer.DeserializePayload<IEnumerable<TestCase>> (message);
			if (tests.Any ()) {
				discoveryJobInProgress.tests.Add (tests);
			}
		}

		void OnDiscoveryCompleted (Message message)
		{
			var discoveryCompletePayload = dataSerializer.DeserializePayload<DiscoveryCompletePayload> (message);
			if (discoveryCompletePayload.LastDiscoveredTests != null && discoveryCompletePayload.LastDiscoveredTests.Any ()) {
				discoveryJobInProgress.tests.Add (discoveryCompletePayload.LastDiscoveredTests);
			}
			discoveryJobInProgress.taskSource.SetResult (discoveryJobInProgress.tests);
			discoveryJobInProgress = null;
			ProcessDiscoveryQueue ();
		}

		void OnTestMessage (Message message)
		{
			var payload = dataSerializer.DeserializePayload<TestMessagePayload> (message);
			monitor.Log.WriteLine (payload.Message);
		}
	}
}
