//
// VsTestAdapter.cs
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using MonoDevelop.Projects;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace MonoDevelop.UnitTesting.VsTest
{
	abstract class VsTestAdapter
	{
		protected IDataSerializer dataSerializer = JsonDataSerializer.Instance;
		ProcessWrapper vsTestConsoleExeProcess;
		protected SocketCommunicationManager communicationManager;
		int clientConnectionTimeOut = 15000;
		Thread messageProcessingThread;
		CancellationTokenSource restartTokenSource = new CancellationTokenSource ();

		internal static string GetRunSettings (Project project)
		{
			return "<RunSettings>" + new Microsoft.VisualStudio.TestPlatform.ObjectModel.RunConfiguration () {
				TargetFrameworkVersion = Framework.FromString ((project as DotNetProject)?.TargetFramework?.Id?.ToString ()),
				DisableAppDomain = true,
				ShouldCollectSourceInformation = false
			}.ToXml ().OuterXml + "</RunSettings>";
		}

		void Restart ()
		{
			startTask = null;

			restartTokenSource.Cancel ();
			restartTokenSource = new CancellationTokenSource ();

			try {
				if (communicationManager != null) {
					communicationManager.StopServer ();
					communicationManager = null;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("TestPlatformCommunicationManager stop error.", ex);
			}

			try {
				if (vsTestConsoleExeProcess != null) {
					if (!vsTestConsoleExeProcess.HasExited) {
						vsTestConsoleExeProcess.Dispose ();
					}
					vsTestConsoleExeProcess = null;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("VSTest process dispose error.", ex);
			}
		}

		Task startTask;
		TaskCompletionSource<bool> startedSource;

		protected Task Start ()
		{
			if (startTask == null)
				startTask = PrivateStart ();
			return startTask;
		}

		async Task PrivateStart ()
		{
			startedSource = new TaskCompletionSource<bool> ();
			communicationManager = new SocketCommunicationManager ();
			int port = communicationManager.HostServer ();
			communicationManager.AcceptClientAsync ().Ignore ();
			vsTestConsoleExeProcess = StartVsTestConsoleExe (port);
			var sw = Stopwatch.StartNew ();
			if (!await Task.Run (() => {
				while (true) {
					if (communicationManager.WaitForClientConnection (100))
						return true;
					if (clientConnectionTimeOut < sw.ElapsedMilliseconds)
						return false;
				}
			})) {
				sw.Stop ();
				throw new TimeoutException ("vstest.console failed to connect.");
			}
			sw.Stop ();

			messageProcessingThread =
				new Thread (ReceiveMessages) {
					IsBackground = true
				};
			messageProcessingThread.Start (restartTokenSource.Token);
			var timeoutDelay = Task.Delay (clientConnectionTimeOut);
			if (await Task.WhenAny (startedSource.Task, timeoutDelay) == timeoutDelay)
				throw new TimeoutException ("vstest.console failed to respond.");
		}

		TextWriter outW = new StringWriter ();
		TextWriter errW = new StringWriter ();

		ProcessWrapper StartVsTestConsoleExe (int port)
		{
			string VsTestConsoleExeFolder = Path.Combine (Path.GetDirectoryName (typeof (VsTestAdapter).Assembly.Location), "VsTestConsole");
			var startPar = new ProcessStartInfo {
				FileName = Path.Combine (VsTestConsoleExeFolder, "vstest.console.exe"),
				Arguments = GetVSTestArguments (port),
				WorkingDirectory = VsTestConsoleExeFolder,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			//Workaround macOs "bug" where terminal path has dotnet added to path but gui apps path doesn't(IDE)
			if (Platform.IsMac)
				startPar.EnvironmentVariables ["PATH"] = Environment.GetEnvironmentVariable ("PATH") + Path.PathSeparator + "/usr/local/share/dotnet/";
			return Runtime.ProcessService.StartProcess (
				startPar,
				outW,
				errW,
				VsTestProcessExited);

		}

		string GetVSTestArguments (int port)
		{
			return $"/parentprocessid:{Process.GetCurrentProcess ().Id} /port:{port}";
		}

		void VsTestProcessExited (object sender, EventArgs e)
		{
			var process = (Process)sender;
			LoggingService.LogError ("vstest.console.exe exited. Exit code: {0}", process.ExitCode);
			Restart ();
		}

		void ReceiveMessages (object obj)
		{
			var token = (CancellationToken)obj;
			while (!token.IsCancellationRequested) {
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

		void OnSessionConnected ()
		{
			SendExtensionList ();
			startedSource.SetResult (true);
		}

		private void SendExtensionList ()
		{
			var addinPath = Path.GetDirectoryName (typeof (VsTestAdapter).Assembly.Location);
			communicationManager.SendMessage (MessageType.ExtensionsInitialize, new string []
			{
				Path.Combine(addinPath, "NUnit3Adapter", "NUnit3.TestAdapter.dll"),
				Path.Combine(addinPath, "NUnit3CoreAdapter", "NUnit3.TestAdapter.dll"),
				//Path.Combine(addinPath, "NUnit2Adapter", "NUnit.VisualStudio.TestAdapter.dll"),
				Path.Combine(addinPath, "XUnitAdapter", "xunit.runner.visualstudio.testadapter.dll"),
				Path.Combine(addinPath, "MSTestAdapter", "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.dll")
			});
		}

		protected virtual void ProcessMessage (Message message)
		{
			switch (message.MessageType) {
			case MessageType.SessionConnected:
				OnSessionConnected ();
				break;
			default:
				LoggingService.LogWarning ($"Unprocessed vstest message {message}");
				break;
			}
		}

		public static string GetAssemblyFileName (Project project)
		{
			return project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
		}
	}
}
