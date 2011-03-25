// 
// MonoMacExecutionHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009-2010 Novell, Inc. (http://www.novell.com)
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
using System.IO;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MonoMac
{
	public class MonoMacExecutionHandler : IExecutionHandler
	{
		public bool CanExecute (ExecutionCommand command)
		{
			return command is MonoMacExecutionCommand;
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			return OpenApplication ((MonoMacExecutionCommand)command, null, console.Out.Write, console.Error.Write);
		}
		
		public static MonoMacProcess OpenApplication (MonoMacExecutionCommand command, ApplicationStartInfo asi,
			Action<string> stdout, Action<string> stderr)
		{
			var logDir = command.AppPath.ParentDirectory;
			var outLog = logDir.Combine ("stdout.log");
			var errLog = logDir.Combine ("stderr.log");
			try {
				if (File.Exists (errLog))
				    File.Delete (errLog);
				if (File.Exists (outLog))
					File.Delete (outLog);
			} catch (IOException) {}
			
			var outTail = new Tail (outLog, stdout);
			var errTail = new Tail (errLog, stderr);
			outTail.Start ();
			errTail.Start ();
			
			if (asi == null)
				asi = new ApplicationStartInfo (command.AppPath);
			asi.NewInstance = true;
			
			//need async or we won't be able to debug things that happen while the app is launching
			asi.Async = true;
			
			var monoRuntime = (MonoDevelop.Core.Assemblies.MonoTargetRuntime)command.Runtime;
			
			//assume MD is running on /L/F/M.f/V/Current
			//if target runtime is different then override for bundle
			if (!monoRuntime.IsRunning) {
				asi.Environment ["MONOMAC_DEBUGLAUNCHER_RUNTIME"] = monoRuntime.Prefix;
			}
			
			asi.Environment ["MONOMAC_DEBUGLAUNCHER_LOGDIR"] = logDir;
			
			var psn = LaunchServices.OpenApplication (asi);
			return new MonoMacProcess (psn, outTail, errTail);
		}
	}
	
	//copied from IPhoneExecutionHandler
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
	
	public class MonoMacProcess : IProcessAsyncOperation, IDisposable
	{
		ProcessSerialNumber psn;
		int pid;
		Tail outTail, errTail;
		
		public MonoMacProcess (ProcessSerialNumber psn, Tail outTail, Tail errTail)
		{
			this.psn = psn;
			
			this.pid = ProcessManager.GetProcessPid (psn);
			if (pid < 0)
				throw new Exception (string.Format ("Could not get PID for PSN {0} {1}", psn.High, psn.Low));
			
			this.outTail = outTail;
			this.errTail = errTail;
		}
		
		public int ExitCode {
			get {
				//FIXME: implement. is it possible?
				return 0;
			}
		}
		
		public int ProcessId {
			get {
				return pid;
			}
		}
		
		void CompletionWatcher ()
		{
			try {
				WaitForCompleted ();
				if (completedEvent != null)
					completedEvent (this);
			} catch (Exception ex) {
				LoggingService.LogError ("Error in process watcher thread", ex);
			}
		}
		
		object lockObj = new object ();
		Thread completionNotifierThread;
		OperationHandler completedEvent;
		
		public event OperationHandler Completed {
			add {
				bool fire = false;
				lock (lockObj) {
					if (IsCompleted) {
						fire = true;
					} else if (completionNotifierThread == null) {
						//FIXME: is it worth worrying about stopping this thread when the events are unsubscribed?
						completionNotifierThread = new System.Threading.Thread (CompletionWatcher);
						completionNotifierThread.Start ();
					}
					completedEvent += value;
				}
				if (fire) {
					value (this);
				}
			}
			remove {
				lock (lockObj) {
					completedEvent -= value;
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
			if (!IsCompleted) {
				ProcessManager.KillProcess (psn);
				WaitForExit (1000);
			}
			FinishCollectingOutput (1000);
		}
		
		void WaitForExit (int timeoutMilliseconds)
		{
			int times = (int)Math.Ceiling (timeoutMilliseconds / 500.0);
			int sleep = timeoutMilliseconds / times;
			while (times-- > 0 && !IsCompleted) {
				Thread.Sleep (sleep);
			}
		}
		
		public void WaitForCompleted ()
		{
			while (!IsCompleted)
				Thread.Sleep (500);
			FinishCollectingOutput (1000);
		}
		
		bool isCompleted;
		
		public bool IsCompleted {
			get { return isCompleted || (isCompleted = ProcessManager.GetProcessPid (psn) < 0); }
		}
		
		public bool Success {
			get { return IsCompleted && ExitCode <= 0; }
		}
		
		public bool SuccessWithWarnings {
			get { return Success; }
		}
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		void Dispose (bool disposing)
		{
			if (disposing) {
				if (outTail != null) {
					outTail.Dispose ();
					outTail = null;
				}
				if (errTail != null) {
					errTail.Dispose ();
					errTail = null;
				}
			}
		}
		
		~MonoMacProcess ()
		{
			Dispose (false);
		}
	}
}
