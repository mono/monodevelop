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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using System.IO;
using System.Threading;
using System.Text;
using System.Diagnostics;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui;


namespace MonoDevelop.IPhone
{


	public class IPhoneExecutionHandler : IExecutionHandler
	{

		public bool CanExecute (ExecutionCommand command)
		{
			IPhoneExecutionCommand cmd = command as IPhoneExecutionCommand;
			return cmd != null && cmd.Simulator;
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			IPhoneExecutionCommand cmd = (IPhoneExecutionCommand) command;
			
			string mtouchPath = cmd.Runtime.GetToolPath (cmd.Framework, "mtouch");
			if (string.IsNullOrEmpty (mtouchPath))
				throw new InvalidOperationException ("Cannot execute iPhone application. mtouch tool is missing.");
			
			string outLog = cmd.LogDirectory.Combine ("out.log");
			string errLog = cmd.LogDirectory.Combine ("err.log");
			
			string mtouchArgs = String.Format ("-launchsim='{0}' -stderr='{1}' -stdout='{2}'", cmd.AppPath, errLog, outLog);
			
			var psi = new ProcessStartInfo (mtouchPath, mtouchArgs) {
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
				WorkingDirectory = cmd.LogDirectory,
				UseShellExecute = false
			};
			
			try {
				if (File.Exists (errLog))
				    File.Delete (errLog);
				if (File.Exists (outLog))
				    File.Delete (outLog);
			} catch (IOException) {}
			
			var outTail = new Tail (outLog, console.Out);
			var errTail = new Tail (errLog, console.Error);
			outTail.Start ();
			errTail.Start ();
			
			//FIXME: do something with these
			var outWriter = new StringWriter ();
			var errWriter = new StringWriter ();
			var mtouchProcess = Runtime.ProcessService.StartProcess (psi, outWriter, errWriter, null);
			mtouchProcess.Exited += delegate {
				string err;
				if (mtouchProcess.ExitCode != 0 && (err = errWriter.ToString ()).Length > 0) {
					Gtk.Application.Invoke (delegate {
						using (var errorDialog = new ErrorDialog (IdeApp.Workbench.RootWindow)) {
							errorDialog.Message = "mtouch encountered an error running the application";
							errorDialog.AddDetails (err, false);
							errorDialog.Run ();	
						}
					});
				}
			};
			
			return new IPhoneProcess (mtouchProcess, outTail, errTail);
		}
	}
	
	class Tail : IDisposable
	{
		volatile bool finish;
		string file;
		TextWriter writer;
		Thread thread;
		ManualResetEvent endHandle = new ManualResetEvent (false);
		
		public Tail (string file, TextWriter writer)
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
						writer.Write (encoding.GetString (buffer, 0, nr));
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
