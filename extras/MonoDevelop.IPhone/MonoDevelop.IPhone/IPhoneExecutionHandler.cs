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
			
			string errLog = cmd.LogDirectory.Combine ("out.log");
			string outLog = cmd.LogDirectory.Combine ("err.log");
			
			string mtouchArgs = String.Format ("-launchsim='{0}' -stderr='{1}' -stdout='{2}'", cmd.AppPath, errLog, outLog);
			
			var psi = new ProcessStartInfo (mtouchPath, mtouchArgs) {
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
				WorkingDirectory = cmd.LogDirectory,
				UseShellExecute = false
			};
			
			var outWriter = new StringWriter ();
			var errWriter = new StringWriter ();
			var mtouchProcess = Runtime.ProcessService.StartProcess (psi, outWriter, errWriter, null);
			
			try {
				if (File.Exists (errLog))
				    File.Delete (errLog);
				if (File.Exists (outLog))
				    File.Delete (outLog);
			} catch (IOException) {}
			
			var t = new Tail (cmd.LogDirectory.Combine ("out.log"), console.Out);
			var t2 = new Tail (cmd.LogDirectory.Combine ("err.log"), console.Error);
			t.StartThread ();
			t2.StartThread ();
			mtouchProcess.Exited += delegate {
				t.Stop ();
				t2.Stop ();
			};
			
			return new IPhoneProcess (mtouchProcess, outWriter, errWriter);
		}
	}
	
	class Tail
	{
		bool stop;
		string file;
		TextWriter writer;
		
		public Tail (string file, TextWriter writer)
		{
			this.file = file;
			this.writer = writer;
		}
		
		public void StartThread ()
		{
			if (stop)
				throw new InvalidOperationException ("Cannot start after stopping");
			var t = new Thread (Run);
			t.Start ();
		}
		
		void Run ()
		{
			FileStream fs = null;
			try {
				fs = File.Open (file, FileMode.OpenOrCreate);
				byte[] buffer = new byte [1024];
				var encoding = System.Text.Encoding.UTF8;
				while (true && !stop) {
					int nr;
					if (fs.Position < fs.Length)
						while ((nr = fs.Read (buffer, 0, buffer.Length)) > 0)
							writer.Write (encoding.GetString (buffer, 0, nr));
					Thread.Sleep (500);
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			} finally {
				if (fs != null)
					fs.Dispose ();
			}
		}
		
		public void Stop ()
		{
			stop = true;
		}
	}
	
	class IPhoneProcess : IProcessAsyncOperation, IDisposable
	{
		ProcessWrapper mtouchProcess;
		StringWriter mtouchOut;
		StringWriter mtouchErr;
		
		public IPhoneProcess (ProcessWrapper mtouchProcess, StringWriter mtouchOut, StringWriter mtouchErr)
		{
			this.mtouchProcess = mtouchProcess;
			this.mtouchErr = mtouchErr;
			this.mtouchOut = mtouchOut;
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
		
		public event OperationHandler Completed {
			add { ((IProcessAsyncOperation)mtouchProcess).Completed += value; }
			remove { ((IProcessAsyncOperation)mtouchProcess).Completed -= value; }
		}
		
		public void Cancel ()
		{
			mtouchProcess.StandardInput.Write ('\n');
			mtouchProcess.WaitForExit (1000);
			if (!((IProcessAsyncOperation)mtouchProcess).IsCompleted)
				((IProcessAsyncOperation)mtouchProcess).Cancel ();
		}
		
		public void WaitForCompleted ()
		{
			((IProcessAsyncOperation)mtouchProcess).WaitForCompleted ();
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
			mtouchProcess.Dispose ();
		}
	}
}
