//
// ProcessHostController.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Timer = System.Timers.Timer;

namespace MonoDevelop.Core.Execution
{
	internal class ProcessHostController: MarshalByRefObject, IProcessHostController
	{
		int references;
		uint stopDelay;
		DateTime lastReleaseTime;
		bool starting;
		bool stopping;
		IProcessAsyncOperation process;
		Timer timer;
		string id;
		IExecutionHandler executionHandlerFactory;
		int shutdownTimeout = 2000;

		IProcessHost processHost;
		ManualResetEvent runningEvent = new ManualResetEvent (false);
		ManualResetEvent exitRequestEvent = new ManualResetEvent (false);
		ManualResetEvent exitedEvent = new ManualResetEvent (false);
		
		public ProcessHostController (string id, uint stopDelay, IExecutionHandler executionHandlerFactory)
		{
			if (string.IsNullOrEmpty (id))
				id = "?";
			this.id = id;
			this.stopDelay = stopDelay;
			this.executionHandlerFactory = executionHandlerFactory;
			timer = new Timer ();
			timer.AutoReset = false;
			timer.Elapsed += new System.Timers.ElapsedEventHandler (WaitTimeout);
		}
		
		public void Start ()
		{
			lock (this)
			{
				if (starting) return;
				starting = true;
				exitRequestEvent.Reset ();
				
				string chId = Runtime.ProcessService.RegisterRemotingChannel ();
				
				BinaryFormatter bf = new BinaryFormatter ();
				ObjRef oref = RemotingServices.Marshal (this);
				MemoryStream ms = new MemoryStream ();
				bf.Serialize (ms, oref);
				string sref = Convert.ToBase64String (ms.ToArray ());
				string tmpFile = null;

				try {
					string location = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
					location = Path.Combine (location, "mdhost.exe");
					
					if (executionHandlerFactory != null) {
						ProcessHostConsole cons = new ProcessHostConsole ();
						tmpFile = Path.GetTempFileName ();
						File.WriteAllText (tmpFile, chId + "\n" + sref + "\n");
						DotNetExecutionCommand cmd = new DotNetExecutionCommand (location, id + " " + tmpFile, AppDomain.CurrentDomain.BaseDirectory);
						process = executionHandlerFactory.Execute (cmd, cons);
					}
					else {
						string args = string.Empty;
						if (isDebugMode) args += " --debug";
						args += " \"" + location + "\" " + id;
						
						InernalProcessHost proc = new InernalProcessHost ();
						proc.StartInfo = new ProcessStartInfo ("mono", args);
						proc.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
						proc.StartInfo.UseShellExecute = false;
						proc.StartInfo.RedirectStandardInput = true;
						proc.EnableRaisingEvents = true;
						proc.Start ();
						proc.StandardInput.WriteLine (chId);
						proc.StandardInput.WriteLine (sref);
						proc.StandardInput.Flush ();
						process = proc;
					}
					process.Completed += ProcessExited;
					
				} catch (Exception ex) {
					if (tmpFile != null) {
						try {
							File.Delete (tmpFile);
						} catch {
						}
					}
					LoggingService.LogError (ex.ToString ());
					throw;
				}
			}
		}
		
		bool isDebugMode
		{
			get {
				return System.Environment.StackTrace.IndexOf ("ProcessHostController.cs:") > -1;
			}
		}

		void ProcessExited (IAsyncOperation oper)
		{
			lock (this) {
				Runtime.ProcessService.UnregisterHostInstance (this, null);
				exitedEvent.Set ();
				if (oper != process) return;

				// The process suddently died
				runningEvent.Reset ();
				processHost = null;
				process = null;
				references = 0;
			}
		}
		
		public RemoteProcessObject CreateInstance (Type type, string[] addins)
		{
			lock (this) {
				references++;
				if (processHost == null)
					Start ();
			}
			
			if (!runningEvent.WaitOne (15000, false)) {
				references--;
				throw new ApplicationException ("Couldn't create a remote process.");
			}
			
			try {
				// Before creating the instance, load the add-ins on which it depends
				if (addins != null && addins.Length > 0)
					processHost.LoadAddins (addins);
				RemoteProcessObject obj = processHost.CreateInstance (type);
				Runtime.ProcessService.RegisterHostInstance (this, obj);
				return obj;
			} catch {
				ReleaseInstance (null);
				throw;
			}
		}
		
		public RemoteProcessObject CreateInstance (string assemblyPath, string typeName, string[] addins)
		{
			lock (this) {
				references++;
				if (processHost == null)
					Start ();
			}
			
			if (!runningEvent.WaitOne (15000, false)) {
				references--;
				throw new ApplicationException ("Couldn't create a remote process.");
			}
			
			try {
				// Before creating the instance, load the add-ins on which it depends
				if (addins != null && addins.Length > 0)
					processHost.LoadAddins (addins);
				RemoteProcessObject obj = processHost.CreateInstance (assemblyPath, typeName);
				Runtime.ProcessService.RegisterHostInstance (this, obj);
				return obj;
			} catch {
				ReleaseInstance (null);
				throw;
			}
		}
		
		public void ReleaseInstance (RemoteProcessObject proc)
		{
			ReleaseInstance (proc, 2000);
		}
		
		public void ReleaseInstance (RemoteProcessObject proc, int shutdownTimeout)
		{
			if (processHost == null) return;

			if (proc != null)
				Runtime.ProcessService.UnregisterHostInstance (this, proc);
			
			lock (this) {
				references--;
				if (references == 0) {
					lastReleaseTime = DateTime.Now;
					if (!stopping) {
						stopping = true;
						this.shutdownTimeout = shutdownTimeout;
						if (stopDelay == 0) {
							// Always stop asyncrhonously, so the remote object
							// has time to end the dispose call.
							timer.Interval = 1000;
							timer.Enabled = true;
						} else {
							timer.Interval = stopDelay;
							timer.Enabled = true;
						}
					}
				}
			}
		}
		
		void WaitTimeout (object sender, System.Timers.ElapsedEventArgs args)
		{
			try {
				IProcessAsyncOperation oldProcess;
				
				lock (this) {
					if (references > 0) {
						stopping = false;
						return;
					}
	
					uint waited = (uint) (DateTime.Now - lastReleaseTime).TotalMilliseconds;
					if (waited < stopDelay) {
						timer.Interval = stopDelay - waited;
						timer.Enabled = true;
						return;
					}
				
					runningEvent.Reset ();
					exitedEvent.Reset ();
					exitRequestEvent.Set ();
					oldProcess = process;
					processHost = null;
					process = null;
					stopping = false;
				}
	
				if (!exitedEvent.WaitOne (shutdownTimeout, false)) {
					try {
						oldProcess.Cancel ();
					} catch {
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}
		
		public void RegisterHost (IProcessHost processHost)
		{
			lock (this)
			{
				this.processHost = processHost;
				runningEvent.Set ();
				starting = false;
			}
		}
		
		public void WaitForExit ()
		{
			exitRequestEvent.WaitOne ();
		}
	}
	
	class ProcessHostConsole: IConsole
	{
		public event EventHandler CancelRequested;
		
		public TextReader In {
			get { return Console.In; }
		}
		
		public TextWriter Out {
			get { return Console.Out; }
		}
		
		public TextWriter Error {
			get { return Console.Error; }
		}
		
		public TextWriter Log {
			get { return Out; }
		}
		
		public bool CloseOnDispose {
			get {
				return false;
			}
		}
		
		public void Dispose ()
		{
		}
	}
	
	class InernalProcessHost: Process, IProcessAsyncOperation
	{
		object doneLock = new object ();
		bool finished;
		OperationHandler completed;
		
		public InernalProcessHost ()
		{
			Exited += delegate {
				lock (doneLock) {
					finished = true;
					Monitor.PulseAll (doneLock);
					if (completed != null)
						completed (this);
				}
			};
		}
		
		public int ProcessId {
			get { return Id; }
		}
		
		public event OperationHandler Completed {
			add {
				lock (doneLock) {
					completed += value; 
					if (finished)
						value (this);
				}
			}
			remove {
				lock (doneLock) {
					completed -= value;
				}
			}
		}
		
		public void Cancel ()
		{
			Kill ();
		}
		
		public void WaitForCompleted ()
		{
			lock (doneLock) {
				while (!finished)
					Monitor.Wait (doneLock);
			}
		}
		
		public bool IsCompleted {
			get {
				lock (doneLock) {
					return finished;
				}
			}
		}
		
		public bool Success {
			get {
				return true;
			}
		}
		
		public bool SuccessWithWarnings {
			get {
				return true;
			}
		}
	}
}
