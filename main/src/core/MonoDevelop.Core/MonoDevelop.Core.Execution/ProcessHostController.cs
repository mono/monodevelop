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
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Reflection;
using Timer = System.Timers.Timer;
using MonoDevelop.Core.Logging;
using Mono.Addins;

namespace MonoDevelop.Core.Execution
{
	[System.ComponentModel.DesignerCategory ("Code")]
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
		
		List<object> remoteObjects = new List<object> ();

		static ProcessHostController ( )
		{
			// In some cases MS.NET can't properly resolve assemblies even if they
			// are already loaded. For example, when deserializing objects.
			AppDomain.CurrentDomain.AssemblyResolve += delegate (object s, ResolveEventArgs args) {
				foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ()) {
					if (asm.GetName ().FullName == args.Name)
						return asm;
				}
				return null;
			};
		}
		
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
				
				RemotingService.RegisterRemotingChannel ();
				
				BinaryFormatter bf = new BinaryFormatter ();
				ObjRef oref = RemotingServices.Marshal (this);
				MemoryStream ms = new MemoryStream ();
				bf.Serialize (ms, oref);
				string sref = Convert.ToBase64String (ms.ToArray ());
				string tmpFile = null;

				if (executionHandlerFactory == null)
					executionHandlerFactory = Runtime.SystemAssemblyService.CurrentRuntime.GetExecutionHandler ();

				try {
					string location = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
					location = Path.Combine (location, "mdhost.exe");
					
					tmpFile = Path.GetTempFileName ();
					StreamWriter sw = new StreamWriter (tmpFile);
					sw.WriteLine (sref);
					sw.WriteLine (Process.GetCurrentProcess ().Id);
					sw.WriteLine (Runtime.SystemAssemblyService.CurrentRuntime.RuntimeId);
					
					// Explicitly load Mono.Addins since the target runtime may not have it installed
					sw.WriteLine (2);
					sw.WriteLine (typeof(AddinManager).Assembly.Location);
					sw.WriteLine (typeof(Mono.Addins.Setup.SetupService).Assembly.Location);
					sw.Close ();
					
					string arguments = string.Format("{0} \"{1}\"", id, tmpFile);
					DotNetExecutionCommand cmd = new DotNetExecutionCommand(location, arguments, AppDomain.CurrentDomain.BaseDirectory);
					cmd.DebugMode = isDebugMode;
					ProcessHostConsole cons = new ProcessHostConsole ();
					process = executionHandlerFactory.Execute (cmd, cons);
					Counters.ExternalHostProcesses++;
					
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

				Counters.ExternalHostProcesses--;
				
				// Remove all callbacks from existing objects
				foreach (object ob in remoteObjects)
					RemotingService.UnregisterMethodCallback (ob, "Dispose");
				
				remoteObjects.Clear ();
				
				exitedEvent.Set ();
				
				// If the remote process crashes, a thread may be left hung in WaitForExit. This will awaken it.
				exitRequestEvent.Set ();
				
				if (oper != process) return;

				// The process suddently died
				runningEvent.Reset ();
				processHost = null;
				process = null;
				references = 0;
			}
		}
		
		public object CreateInstance (Type type, string[] addins)
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
				RemotingService.RegisterAssemblyForSimpleResolve (type.Assembly.GetName ().Name);
				object obj = processHost.CreateInstance (type);
				RemotingService.RegisterMethodCallback (obj, "Dispose", RemoteProcessObjectDisposing, null);
				RemotingService.RegisterMethodCallback (obj, "Shutdown", RemoteProcessObjectShuttingDown, null);
				remoteObjects.Add (obj);
				Counters.ExternalObjects++;
				return obj;
			} catch {
				ReleaseInstance (null);
				throw;
			}
		}
		
		public object CreateInstance (string assemblyPath, string typeName, string[] addins)
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
				RemotingService.RegisterAssemblyForSimpleResolve (Path.GetFileNameWithoutExtension (assemblyPath));
				object obj = processHost.CreateInstance (assemblyPath, typeName);
				RemotingService.RegisterMethodCallback (obj, "Dispose", RemoteProcessObjectDisposing, null);
				RemotingService.RegisterMethodCallback (obj, "Shutdown", RemoteProcessObjectShuttingDown, null);
				remoteObjects.Add (obj);
				Counters.ExternalObjects++;
				return obj;
			} catch {
				ReleaseInstance (null);
				throw;
			}
		}
		
		IMethodReturnMessage RemoteProcessObjectDisposing (object obj, IMethodCallMessage msg)
		{
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					processHost.DisposeObject ((IDisposable)obj);
				} catch {
				}
				ReleaseInstance (obj);
			});
			return new ReturnMessage (null, null, 0, msg.LogicalCallContext, msg);
		}
		
		IMethodReturnMessage RemoteProcessObjectShuttingDown (object obj, IMethodCallMessage msg)
		{
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					process.Cancel ();
				} catch {
				}
			});
			return new ReturnMessage (null, null, 0, msg.LogicalCallContext, msg);
		}
		
		public void ReleaseInstance (object obj)
		{
			ReleaseInstance (obj, 2000);
		}
		
		public void ReleaseInstance (object proc, int shutdownTimeout)
		{
			Counters.ExternalObjects--;
			if (processHost == null)
				return;
			
			lock (this) {
				for (int n=0; n<remoteObjects.Count; n++) {
					if (remoteObjects [n] == proc) {
						remoteObjects.RemoveAt (n);
						break;
					}
				}
				
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
		
		public ILogger GetLogger ()
		{
			return LoggingService.RemoteLogger;
		}

	}
	
	class ProcessHostConsole: IConsole
	{
		//nothing fires this event so make a dummy implementation to keep csc happy
		event EventHandler IConsole.CancelRequested { add { } remove {} }
		
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
