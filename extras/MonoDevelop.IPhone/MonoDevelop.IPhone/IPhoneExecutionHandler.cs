// 
// IPhoneExecutionHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.AspNet;
using System.IO;
using System.Threading;
using System.Text;
using System.Diagnostics;
using MonoDevelop.Ide;

namespace MonoDevelop.IPhone
{
	public class IPhoneExecutionHandler : IExecutionHandler
	{
		public IPhoneExecutionHandler ()
		{
		}
		
		public IPhoneExecutionHandler (IPhoneSimulatorTarget target)
		{
			this.SimulatorTarget = target;
		}		
		
		public IPhoneSimulatorTarget SimulatorTarget { get; private set; }

		public bool CanExecute (ExecutionCommand command)
		{
			var cmd = command as IPhoneExecutionCommand;
			if (cmd == null)
				return false;
			if (SimulatorTarget != null && (!cmd.Simulator || !SimulatorTarget.Supports (cmd.MinimumOSVersion, cmd.SupportedDevices)))
				return false;
			return true;
		}
		
		public static ProcessStartInfo CreateMtouchSimStartInfo (IPhoneExecutionCommand cmd, bool logSimOutput)
		{
			return CreateMtouchSimStartInfo (cmd, logSimOutput, cmd.SimulatorTarget);
		}
		
		public static ProcessStartInfo CreateMtouchSimStartInfo (IPhoneExecutionCommand cmd, bool logSimOutput, 
		                                                         IPhoneSimulatorTarget forceTarget)
		{
			string mtouchPath = cmd.Runtime.GetToolPath (cmd.Framework, "mtouch");
			if (string.IsNullOrEmpty (mtouchPath))
				throw new InvalidOperationException ("Cannot execute iPhone application. mtouch tool is missing.");
			
			var outLog = cmd.OutputLogPath;
			var errLog = cmd.ErrorLogPath;
			try {
				if (File.Exists (errLog))
				    File.Delete (errLog);
				if (File.Exists (outLog))
				    File.Delete (outLog);
			} catch (IOException) {}
			
			var sb = new StringBuilder ();
			sb.AppendFormat ("-launchsim='{0}'", cmd.AppPath);
			if (logSimOutput) 
				sb.AppendFormat (" -stderr='{0}' -stdout='{1}'", errLog, outLog);
			
			if (forceTarget != null) {
				sb.AppendFormat (" -sdk='{0}'", forceTarget.Version.ToString ());
				if (forceTarget.Device == TargetDevice.IPad)
					sb.AppendFormat (" -device=2");
			}
			
			var psi = new ProcessStartInfo (mtouchPath, sb.ToString ()) {
				WorkingDirectory = cmd.LogDirectory,
				UseShellExecute = false
			};
			
			return psi;
		}
		
		const string DoNotShowAgainKey = "DoNotShowAgain";

		void TellUserToStartApplication ()
		{
			var message = new GenericMessage () {
				Text = GettextCatalog.GetString ("Please Start Application"),
				SecondaryText = GettextCatalog.GetString (
					"The application has been built and uploaded, or is already up to date.\n" +
					"Please start it by tapping the application icon on the device."),
				Icon = "phone-apple-iphone",
				DefaultButton = 0,
			};
			
			message.AddOption (DoNotShowAgainKey, GettextCatalog.GetString ("Do not show this message again"),
			                   !IPhoneSettings.ShowStartOnDeviceMessage);
			message.Buttons.Add (AlertButton.Ok);
			MessageService.GenericAlert (message);
			IPhoneSettings.ShowStartOnDeviceMessage = !message.GetOptionValue (DoNotShowAgainKey);
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			IPhoneExecutionCommand cmd = (IPhoneExecutionCommand) command;
			if (!cmd.Simulator) {
				if (IPhoneSettings.ShowStartOnDeviceMessage) {
					Gtk.Application.Invoke (delegate {
						TellUserToStartApplication ();
					});
				}
				return NullProcessAsyncOperation.Success;
			}
			
			var psi = CreateMtouchSimStartInfo (cmd, true, SimulatorTarget ?? cmd.SimulatorTarget);
			
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;
			psi.RedirectStandardInput = true;
			
			LineInterceptingTextWriter intercepter = null;
			intercepter = new LineInterceptingTextWriter (null, delegate {
				if (intercepter.GetLine ().StartsWith ("Application launched")) {
					IPhoneUtility.MakeSimulatorGrabFocus ();
					intercepter.FinishedIntercepting = true;
				} else if (intercepter.LineCount > 20) {
					intercepter.FinishedIntercepting = true;
				}
			});
			
			var outTail = new Tail (cmd.OutputLogPath, console.Out.Write);
			var errTail = new Tail (cmd.ErrorLogPath, console.Error.Write);
			outTail.Start ();
			errTail.Start ();
			
			var mtouchProcess = Runtime.ProcessService.StartProcess (psi, intercepter, console.Log, null);
			return new IPhoneProcess (mtouchProcess, outTail, errTail);
		}
	}
	
	public class Tail : IDisposable
	{
		volatile bool finish;
		string file;
		Action<string> writer;
		Thread thread;
		ManualResetEvent endHandle = new ManualResetEvent (false);
		
		public Tail (string file, Action<string> writer)
		{
			this.file = file;
			this.writer = writer;
		}
		
		public WaitHandle EndHandle {
			get { return endHandle; }
		}
		
		public void Start ()
		{
			if (thread != null)
				throw new InvalidOperationException ("Already started");
			thread = new Thread (Run) {
				IsBackground = true,
				Name = "File tail",
			};
			thread.Start ();
		}
		
		void Run ()
		{
			FileStream fs = null;
			try {
				fs = File.Open (file, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
				byte[] buffer = new byte [1024];
				var encoding = System.Text.Encoding.UTF8;
				while (!finish) {
					Thread.Sleep (500);
					int nr;
					while ((nr = fs.Read (buffer, 0, buffer.Length)) > 0)
						writer (encoding.GetString (buffer, 0, nr));
				}
			} catch (ThreadAbortException) {
				Thread.ResetAbort ();
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			} finally {
				if (fs != null)
					fs.Dispose ();
				endHandle.Set ();
			}
		}
		
		public void Finish ()
		{
			finish = true;
		}
		
		public void Cancel ()
		{
			thread.Abort ();
		}
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		void Dispose (bool disposing)
		{
			if (thread != null && thread.IsAlive) {
				LoggingService.LogWarning ("Aborted tail thread on file '" + file + "'. Should wait to collect remaining output.");
				thread.Abort ();
				thread = null;
			}
		}
		
		~Tail ()
		{
			Dispose (false);
		}
	}
	
	class IPhoneProcess : IProcessAsyncOperation, IDisposable
	{
		ProcessWrapper mtouchProcess;
		Tail outTail, errTail;
		
		public IPhoneProcess (ProcessWrapper mtouchProcess, Tail outTail, Tail errTail)
		{
			this.mtouchProcess = mtouchProcess;
			this.outTail = outTail;
			this.errTail = errTail;
		}
		
		public int ExitCode {
			get {
				//FIXME: implement
				return 0;
			}
		}
		
		public int ProcessId {
			get {
				//FIXME: implement
				return mtouchProcess.Id;
			}
		}
		
		void CompletionWrapper (IAsyncOperation op)
		{
			FinishCollectingOutput (1000);
			completed (op);
		}
		
		OperationHandler completed;
		
		public event OperationHandler Completed {
			add {
				lock (completed) {
					if (completed == null)
						((IProcessAsyncOperation)mtouchProcess).Completed += CompletionWrapper;
					completed += value;
				}
			}
			remove {
				lock (completed) {
					completed -= value;
					if (completed == null)
						((IProcessAsyncOperation)mtouchProcess).Completed -= CompletionWrapper;
				}
			}
		}
		
		void FinishCollectingOutput (int timeoutMilliseconds)
		{
			outTail.Finish ();
			errTail.Finish ();
			WaitHandle.WaitAll (new WaitHandle[] { outTail.EndHandle, errTail.EndHandle }, timeoutMilliseconds);
		}
		
		public void Cancel ()
		{
			mtouchProcess.StandardInput.Write ('\n');
			mtouchProcess.WaitForExit (1000);
			if (!((IProcessAsyncOperation)mtouchProcess).IsCompleted)
				((IProcessAsyncOperation)mtouchProcess).Cancel ();
			FinishCollectingOutput (1000);
		}
		
		public void WaitForOutput ()
		{
			((IProcessAsyncOperation)mtouchProcess).WaitForCompleted ();
			FinishCollectingOutput (1000);
		}
		
		void IAsyncOperation.WaitForCompleted ()
		{
			WaitForOutput ();
		}
		
		public bool IsCompleted {
			get { return ((IProcessAsyncOperation)mtouchProcess).IsCompleted; }
		}
		
		public bool Success {
			get { return ((IProcessAsyncOperation)mtouchProcess).Success; }
		}
		
		public bool SuccessWithWarnings {
			get { return ((IProcessAsyncOperation)mtouchProcess).SuccessWithWarnings; }
		}
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		void Dispose (bool disposing)
		{
			if (mtouchProcess == null)
				return;
			mtouchProcess.Dispose ();
			outTail.Dispose ();
			errTail.Dispose ();
			mtouchProcess = null;
		}
		
		~IPhoneProcess ()
		{
			Dispose (false);
		}
	}
}
