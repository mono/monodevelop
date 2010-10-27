// 
// SoftDebuggerSession.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

//#define DEBUG_EVENT_QUEUEING

using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Mono.Debugging.Client;
using Mono.Debugger.Soft;
using Mono.Debugging.Evaluation;
using MDB = Mono.Debugger.Soft;
using System.Net.Sockets;
using System.IO;
using System.Reflection;
using System.Text;
using System.Net;

namespace Mono.Debugging.Soft
{
	public class SoftDebuggerSession : DebuggerSession
	{
		VirtualMachine vm;
		Thread eventHandler;
		Dictionary<string, List<TypeMirror>> source_to_type = new Dictionary<string, List<TypeMirror>> ();
		Dictionary<string,TypeMirror> types = new Dictionary<string, TypeMirror> ();
		Dictionary<EventRequest,BreakInfo> breakpoints = new Dictionary<EventRequest,BreakInfo> ();
		List<BreakEvent> pending_bes = new List<BreakEvent> ();
		ThreadMirror current_thread, recent_thread;
		ProcessInfo[] procs;
		ThreadInfo[] current_threads;
		bool exited;
		bool started;
		internal int StackVersion;
		StepEventRequest currentStepRequest;
		ExceptionEventRequest unhandledExceptionRequest;
		string remoteProcessName;
		
		Dictionary<long,ObjectMirror> activeExceptionsByThread = new Dictionary<long, ObjectMirror> ();
		
		Thread outputReader;
		Thread errorReader;
		
		IAsyncResult connectionHandle;
		
		LinkedList<Event> queuedEvents = new LinkedList<Event> ();
		
		List<string> userAssemblyNames;
		List<AssemblyMirror> assemblyFilters;
		
		bool loggedSymlinkedRuntimesBug = false;
		
		public readonly SoftDebuggerAdaptor Adaptor = new SoftDebuggerAdaptor ();
		
		public SoftDebuggerSession ()
		{
			Adaptor.BusyStateChanged += delegate(object sender, BusyStateEventArgs e) {
				SetBusyState (e);
			};
		}
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			if (exited)
				throw new InvalidOperationException ("Already exited");
			
			var dsi = (SoftDebuggerStartInfo) startInfo;
			var runtime = Path.Combine (Path.Combine (dsi.MonoRuntimePrefix, "bin"), "mono");
			RegisterUserAssemblies (dsi.UserAssemblyNames);
			
			var psi = new System.Diagnostics.ProcessStartInfo (runtime) {
				Arguments = string.Format ("\"{0}\" {1}", dsi.Command, dsi.Arguments),
				WorkingDirectory = dsi.WorkingDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			
			LaunchOptions options = null;
			
			if (dsi.UseExternalConsole && dsi.ExternalConsoleLauncher != null) {
				options = new LaunchOptions ();
				options.CustomTargetProcessLauncher = dsi.ExternalConsoleLauncher;
				psi.RedirectStandardOutput = false;
				psi.RedirectStandardError = false;
			}

			var sdbLog = Environment.GetEnvironmentVariable ("MONODEVELOP_SDB_LOG");
			if (!String.IsNullOrEmpty (sdbLog)) {
				options = options ?? new LaunchOptions ();
				options.AgentArgs = string.Format ("loglevel=1,logfile='{0}'", sdbLog);
			}
			
			foreach (var env in dsi.MonoRuntimeEnvironmentVariables)
				psi.EnvironmentVariables[env.Key] = env.Value;
			
			foreach (var env in startInfo.EnvironmentVariables)
				psi.EnvironmentVariables[env.Key] = env.Value;
			
			if (!String.IsNullOrEmpty (dsi.LogMessage))
				OnDebuggerOutput (false, dsi.LogMessage + "\n");
			
			var callback = HandleConnectionCallbackErrors ((IAsyncResult ar) => {
				ConnectionStarted (VirtualMachineManager.EndLaunch (ar));
			});
			ConnectionStarting (VirtualMachineManager.BeginLaunch (psi, callback, options), dsi);
		}
		
		/// <summary>Starts the debugger listening for a connection over TCP/IP</summary>
		protected void StartListening (RemoteSoftDebuggerStartInfo dsi)
		{
			IPEndPoint dbgEP, conEP;
			InitForRemoteSession (dsi, out dbgEP, out conEP);
			
			var callback = HandleConnectionCallbackErrors (delegate (IAsyncResult ar) {
				ConnectionStarted (VirtualMachineManager.EndListen (ar));
			});
			ConnectionStarting (VirtualMachineManager.BeginListen (dbgEP, conEP, callback), dsi);
		}

		protected virtual bool ShouldRetryConnection (Exception ex, int attemptNumber)
		{
			var sx = ex as SocketException;
			if (sx != null) {
				if (sx.ErrorCode == 10061) //connection refused
					return true;
			}
			return false;
		}
		
		/// <summary>Starts the debugger connecting to a remote IP</summary>
		protected void StartConnecting (RemoteSoftDebuggerStartInfo dsi, int maxAttempts, int timeBetweenAttempts)
		{
			if (timeBetweenAttempts < 0 || timeBetweenAttempts > 10000)
				throw new ArgumentException ("timeBetweenAttempts");
			
			IPEndPoint dbgEP, conEP;
			InitForRemoteSession (dsi, out dbgEP, out conEP);
			
			AsyncCallback callback = null;
			int attemptNumber = 0;
			callback = delegate (IAsyncResult ar) {
				try {
					ConnectionStarted (VirtualMachineManager.EndConnect (ar));
					return;
				} catch (Exception ex) {
					attemptNumber++;
					if (!ShouldRetryConnection(ex, attemptNumber) || attemptNumber == maxAttempts || Exited) {
						OnConnectionError (ex);
						return;
					}
				}
				try {
					if (timeBetweenAttempts > 0)
						System.Threading.Thread.Sleep (timeBetweenAttempts);
					
					ConnectionStarting (VirtualMachineManager.BeginConnect (dbgEP, conEP, callback), dsi);
					
				} catch (Exception ex2) {
					OnConnectionError (ex2);
				}
			};
			
			ConnectionStarting (VirtualMachineManager.BeginConnect (dbgEP, conEP, callback), dsi);
		}
		
		void InitForRemoteSession (RemoteSoftDebuggerStartInfo dsi, out IPEndPoint dbgEP, out IPEndPoint conEP)
		{
			if (remoteProcessName != null)
				throw new InvalidOperationException ("Cannot initialize connection more than once");
			
			remoteProcessName = dsi.AppName;
			if (string.IsNullOrEmpty (remoteProcessName))
				remoteProcessName = "mono";
			
			RegisterUserAssemblies (dsi.UserAssemblyNames);
			
			dbgEP = new IPEndPoint (dsi.Address, dsi.DebugPort);
			conEP = dsi.RedirectOutput? new IPEndPoint (dsi.Address, dsi.OutputPort) : null;
			
			if (!String.IsNullOrEmpty (dsi.LogMessage))
				LogWriter (false, dsi.LogMessage + "\n");
		}
		
		///<summary>Catches errors in async callbacks and hands off to OnConnectionError</summary>
		AsyncCallback HandleConnectionCallbackErrors (AsyncCallback callback)
		{
			return delegate (IAsyncResult ar) {
				connectionHandle = null;
				try {
					callback (ar);
				} catch (Exception ex) {
					OnConnectionError (ex);
				}
			};
		}
		
		/// <summary>
		/// Called when the debugger starts connecting.
		/// </summary>
		protected virtual void OnConnectionStarting (DebuggerStartInfo dsi, bool retrying)
		{
		}
		
		/// <summary>
		/// Called when the debugger succeeds connecting.
		/// </summary>
		protected virtual void OnConnectionStarted ()
		{
		}
		
		/// <summary>
		/// Called if an error happens while making the connection. Default terminates the session.
		/// </summary>
		protected virtual void OnConnectionError (Exception ex)
		{
			//if the exception was caused by cancelling the session
			if (Exited && ex is SocketException)
				return;
			
			if (!HandleException (ex)) {
				LoggingService.LogAndShowException ("Unhandled error launching soft debugger", ex);
				EndSession ();
			}
		}
		
		void ConnectionStarting (IAsyncResult connectionHandle, DebuggerStartInfo dsi) 
		{
			if (this.connectionHandle != null && !this.connectionHandle.IsCompleted)
				throw new InvalidOperationException ("Already connecting");
			bool retrying = this.connectionHandle != null;
			this.connectionHandle = connectionHandle;
			OnConnectionStarting (dsi, retrying);
		}
		
		void EndLaunch ()
		{
			if (connectionHandle != null) {
				VirtualMachineManager.CancelConnection (connectionHandle);
				connectionHandle = null;
			}
		}
		
		protected virtual void EndSession ()
		{
			if (!exited) {
				exited = true;
				EndLaunch ();
				OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
			}
		}
		
		protected bool Exited {
			get { return exited; }
		}
		
		/// <summary>
		/// If subclasses do an async connect in OnRun, they should pass the resulting VM to this method.
		/// If the vm is null, the session will be closed.
		/// </summary>
		void ConnectionStarted (VirtualMachine vm)
		{
			if (this.vm != null)
				throw new InvalidOperationException ("The VM has already connected");
			
			if (vm == null) {
				EndSession ();
				return;
			}
			
			connectionHandle = null;
			
			this.vm = vm;
			
			ConnectOutput (vm.StandardOutput, false);
			ConnectOutput (vm.StandardError, true);
			
			OnConnectionStarted ();
			
			vm.EnableEvents (EventType.AssemblyLoad, EventType.TypeLoad, EventType.ThreadStart, EventType.ThreadDeath, EventType.AssemblyUnload);
			try {
				unhandledExceptionRequest = vm.CreateExceptionRequest (null, false, true);
				unhandledExceptionRequest.Enable ();
			} catch (NotSupportedException) {
				//Mono < 2.6.3 doesn't support catching unhandled exceptions
			}
			
			OnStarted ();
			started = true;
			
			/* Wait for the VMStart event */
			HandleEvent (vm.GetNextEvent ());
			
			eventHandler = new Thread (EventHandler);
			eventHandler.Name = "SDB event handler";
			eventHandler.Start ();
		}
		
		protected void RegisterUserAssemblies (List<AssemblyName> userAssemblyNames)
		{
			if (Options.ProjectAssembliesOnly && userAssemblyNames != null) {
				assemblyFilters = new List<AssemblyMirror> ();
				this.userAssemblyNames = userAssemblyNames.Select (x => x.ToString ()).ToList ();
			}
		}
		
		protected void ConnectOutput (System.IO.StreamReader reader, bool error)
		{
			Thread t = (error ? errorReader : outputReader);
			if (t != null || reader == null)
				return;
			t = new Thread (delegate () {
				ReadOutput (reader, error);
			});
			t.Name = error? "SDB error reader" : "SDB output reader";
			t.IsBackground = true;
			t.Start ();

			if (error)
				errorReader = t;	
			else
				outputReader = t;
		}

		void ReadOutput (System.IO.StreamReader reader, bool isError)
		{
			try {
				char[] buffer = new char [256];
				while (!exited) {
					int c = reader.Read (buffer, 0, buffer.Length);
					OnTargetOutput (isError, new string (buffer, 0, c));
				}
			} catch {
				// Ignore
			}
		}

		protected virtual void OnResumed ()
		{
			current_threads = null;
			current_thread = null;
			procs = null;
			activeExceptionsByThread.Clear ();
		}
		
		public VirtualMachine VirtualMachine {
			get { return vm; }
		}
		
		public TypeMirror GetType (string fullName)
		{
			TypeMirror tm;
			types.TryGetValue (fullName, out tm);
			return tm;
		}
		
		public IEnumerable<TypeMirror> GetAllTypes ()
		{
			return types.Values;
		}

		protected override bool AllowBreakEventChanges {
			get { return true; }
		}

		public override void Dispose ()
		{
			base.Dispose ();
			if (!exited) {
				exited = true;
				EndLaunch ();
				if (vm != null) {
					ThreadPool.QueueUserWorkItem (delegate {
						try {
							vm.Exit (0);
						} catch (VMDisconnectedException) {
						} catch (Exception ex) {
							LoggingService.LogError ("Error exiting SDB VM:", ex);
						}
						try {
							vm.Dispose ();
						} catch (VMDisconnectedException) {
						} catch (Exception ex) {
							LoggingService.LogError ("Error disposing SDB VM:", ex);
						}
					});
				}
			}
			Adaptor.Dispose ();
		}

		protected override void OnAttachToProcess (long processId)
		{
			throw new System.NotImplementedException ();
		}

		protected override void OnContinue ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				Adaptor.CancelAsyncOperations (); // This call can block, so it has to run in background thread to avoid keeping the main session lock
				OnResumed ();
				vm.Resume ();
				DequeueEventsForFirstThread ();
			});
		}

		protected override void OnDetach ()
		{
			throw new System.NotImplementedException ();
		}

		protected override void OnExit ()
		{
			exited = true;
			EndLaunch ();
			if (vm != null)
				try {
					vm.Exit (0);
				} catch (SocketException se) {
					// This will often happen during normal operation
					LoggingService.LogError ("Error closing debugger session", se);
				}
			QueueEnsureExited ();
		}
		
		void QueueEnsureExited ()
		{
			if (vm != null) {
				//FIXME: this might never get reached if the IDE is exited first
				var t = new System.Timers.Timer ();
				t.Interval = 10000;
				t.Elapsed += delegate {
					try {
						EnsureExited ();
						t.Enabled = false;
						t.Dispose ();
					} catch (Exception ex) {
						LoggingService.LogError ("Failed to force-terminate process", ex);
					}
				};
				t.Enabled = true;
			}	
		}
		
		/// <summary>This is a fallback in case the debugger agent doesn't respond to an exit call</summary>
		protected virtual void EnsureExited ()
		{
			try {
				if (vm != null && vm.TargetProcess != null && !vm.TargetProcess.HasExited)
					vm.TargetProcess.Kill ();
			} catch (Exception ex) {
				LoggingService.LogError ("Error force-terminating soft debugger process", ex);
			}
		}

		protected override void OnFinish ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				Adaptor.CancelAsyncOperations (); // This call can block, so it has to run in background thread to avoid keeping the main session lock
				var req = vm.CreateStepRequest (current_thread);
				req.Depth = StepDepth.Out;
				req.Size = StepSize.Line;
				if (assemblyFilters != null && assemblyFilters.Count > 0)
					req.AssemblyFilter = assemblyFilters;
				req.Enabled = true;
				currentStepRequest = req;
				OnResumed ();
				vm.Resume ();
				DequeueEventsForFirstThread ();
			});
		}

		protected override ProcessInfo[] OnGetProcesses ()
		{
			if (procs == null) {
				if (remoteProcessName != null) {
					procs = new ProcessInfo[] { new ProcessInfo (0, remoteProcessName) };
				} else {
					try {
						procs = new ProcessInfo[] { new ProcessInfo (vm.TargetProcess.Id, vm.TargetProcess.ProcessName) };
					} catch (Exception ex) {
						if (!loggedSymlinkedRuntimesBug) {
							loggedSymlinkedRuntimesBug = true;
							LoggingService.LogError ("Error getting debugger process info. Known Mono bug with symlinked runtimes.", ex);
						}
						procs = new ProcessInfo[] { new ProcessInfo (0, "mono") };
					}
				}
			}
			return new ProcessInfo[] { new ProcessInfo (procs[0].Id, procs[0].Name) };
		}

		protected override Backtrace OnGetThreadBacktrace (long processId, long threadId)
		{
			return GetThreadBacktrace (GetThread (processId, threadId));
		}
		
		Backtrace GetThreadBacktrace (ThreadMirror thread)
		{
			return new Backtrace (new SoftDebuggerBacktrace (this, thread));
		}

		protected override ThreadInfo[] OnGetThreads (long processId)
		{
			if (current_threads == null) {
				List<ThreadInfo> threads = new List<ThreadInfo> ();
				foreach (ThreadMirror t in vm.GetThreads ())
					threads.Add (new ThreadInfo (processId, t.Id, t.Name, null));
				current_threads = threads.ToArray ();
			}
			return current_threads;
		}
		
		ThreadMirror GetThread (long processId, long threadId)
		{
			foreach (ThreadMirror t in vm.GetThreads ())
				if (t.Id == threadId)
					return t;
			return null;
		}
		
		ThreadInfo GetThread (ProcessInfo process, ThreadMirror thread)
		{
			foreach (var t in OnGetThreads (process.Id))
				if (t.Id == thread.Id)
					return t;
			return null;
		}

		protected override object OnInsertBreakEvent (BreakEvent be, bool activate)
		{
			if (exited)
				return null;
			BreakInfo bi = new BreakInfo ();
			bi.Enabled = activate;
			bi.BreakEvent = be;
			
			if (be is Breakpoint) {
				Breakpoint bp = (Breakpoint) be;
				bi.Location = FindLocation (bp.FileName, bp.Line);
				if (bi.Location != null)
					InsertBreakpoint (bp, bi);
				else {
					pending_bes.Add (bp);
					SetBreakEventStatus (be, false);
				}
			} else if (be is Catchpoint) {
				var cp = (Catchpoint) be;
				TypeMirror type;
				if (types.TryGetValue (cp.ExceptionName, out type)) {
					InsertCatchpoint (cp, bi, type);
				} else {
					pending_bes.Add (be);
					SetBreakEventStatus (be, false);
				}
			}
			return bi;
		}

		protected override void OnRemoveBreakEvent (object handle)
		{
			if (exited)
				return;
			BreakInfo bi = (BreakInfo) handle;
			if (bi.Req != null) {
				bi.Req.Enabled = false;
				RemoveQueuedEvents (bi.Req);
			}
			pending_bes.Remove (bi.BreakEvent);
		}

		protected override void OnEnableBreakEvent (object handle, bool enable)
		{
			if (exited)
				return;
			BreakInfo bi = (BreakInfo) handle;
			bi.Enabled = enable;
			if (bi.Req != null) {
				bi.Req.Enabled = enable;
				if (!enable)
					RemoveQueuedEvents (bi.Req);
			}
		}

		protected override object OnUpdateBreakEvent (object handle, BreakEvent be)
		{
			return handle;
		}

		void InsertBreakpoint (Breakpoint bp, BreakInfo bi)
		{
			bi.Req = vm.SetBreakpoint (bi.Location.Method, bi.Location.ILOffset);
			bi.Req.Enabled = bi.Enabled;
			breakpoints [bi.Req] = bi;
			OnBreakpointBound (bp, (BreakpointEventRequest) bi.Req);
		}
		
		void InsertCatchpoint (Catchpoint cp, BreakInfo bi, TypeMirror excType)
		{
			var request = bi.Req = vm.CreateExceptionRequest (excType, true, true);
			request.Count = cp.HitCount;
			bi.Req.Enabled = bi.Enabled;
		}

		protected virtual void OnBreakpointBound (Breakpoint bp, BreakpointEventRequest request)
		{
		}
		
		Location FindLocation (string file, int line)
		{
			if (!started)
				return null;
			string filename = System.IO.Path.GetFileName (file);
	
			// Try the current class first
			Location target_loc = null;// = GetLocFromType (current_thread.GetFrames()[0].Method.DeclaringType, filename, line);
	
			// Try already loaded types in the current source file
			if (target_loc == null) {
				List<TypeMirror> types;
	
				if (source_to_type.TryGetValue (filename, out types)) {
					foreach (TypeMirror t in types) {
						target_loc = GetLocFromType (t, filename, line);
						if (target_loc != null)
							break;
					}
				}
			}
	
			// FIXME: Add a pending breakpoint
	
			return target_loc;
		}
		
		protected override void OnNextInstruction ()
		{
			throw new System.NotImplementedException ();
		}

		protected override void OnNextLine ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					Adaptor.CancelAsyncOperations (); // This call can block, so it has to run in background thread to avoid keeping the main session lock
					var req = vm.CreateStepRequest (current_thread);
					req.Depth = StepDepth.Over;
					req.Size = StepSize.Line;
					if (assemblyFilters != null && assemblyFilters.Count > 0)
						req.AssemblyFilter = assemblyFilters;
					req.Enabled = true;
					currentStepRequest = req;
					OnResumed ();
					vm.Resume ();
					DequeueEventsForFirstThread ();
				} catch (Exception ex) {
					LoggingService.LogError ("Next Line command failed", ex);
				}
			});
		}

		void EventHandler ()
		{
			while (true) {
				try {
					Event e = vm.GetNextEvent ();
					if (e is VMDeathEvent || e is VMDisconnectEvent) {
						OnVMDeathEvent ();
						break;
					}
					HandleEvent (e);
				} catch (VMDisconnectedException ex) {
					OnVMDeathEvent ();
					OnDebuggerOutput (true, ex.ToString ());
					break;
				} catch (Exception ex) {
					OnDebuggerOutput (true, ex.ToString ());
				}
			}
			
			exited = true;
			OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
		}

		void HandleEvent (Event e)
		{
			bool isBreakEvent = e is BreakpointEvent || e is ExceptionEvent || e is StepEvent;
			if (isBreakEvent && current_thread != null && e.Thread.Id != current_thread.Id) {
				QueueEvent (e);
			} else {
				HandleEvent (e, false);
			}
		}
		
		void HandleEvent (Event e, bool dequeuing)
		{
			if (dequeuing && exited)
				return;

			bool resume = true;
			ObjectMirror exception = null;
			
			TargetEventType etype = TargetEventType.TargetStopped;

#if DEBUG_EVENT_QUEUEING
			if (!(e is TypeLoadEvent))
				Console.WriteLine ("pp event: " + e);
#endif

			OnHandleEvent (e);
			
			if (e is AssemblyLoadEvent) {
				AssemblyLoadEvent ae = (AssemblyLoadEvent)e;
				bool isExternal = !UpdateAssemblyFilters (ae.Assembly) && userAssemblyNames != null;
				string flagExt = isExternal? " [External]" : "";
				OnDebuggerOutput (false, string.Format ("Loaded assembly: {0}{1}\n", ae.Assembly.Location, flagExt));
			}
			
			if (e is AssemblyUnloadEvent) {
				AssemblyUnloadEvent aue = (AssemblyUnloadEvent)e;
				
				// Mark affected breakpoints as pending again
				List<KeyValuePair<EventRequest,BreakInfo>> affectedBreakpoints = new List<KeyValuePair<EventRequest, BreakInfo>> (
					breakpoints.Where (x=> (x.Value.Location.Method.DeclaringType.Assembly.Location.Equals (aue.Assembly.Location, StringComparison.OrdinalIgnoreCase)))
				);
				foreach (KeyValuePair<EventRequest,BreakInfo> breakpoint in affectedBreakpoints) {
					OnDebuggerOutput (false, string.Format ("Re-pending breakpoint at {0}:{1}\n",
					                                        Path.GetFileName (breakpoint.Value.Location.SourceFile),
					                                        breakpoint.Value.Location.LineNumber));
					breakpoints.Remove (breakpoint.Key);
					pending_bes.Add (breakpoint.Value.BreakEvent);
				}
				
				// Remove affected types from the loaded types list
				List<string> affectedTypes = new List<string> (
					from pair in types
					where pair.Value.Assembly.Location.Equals (aue.Assembly.Location, StringComparison.OrdinalIgnoreCase)
					select pair.Key
				);
				foreach (string typename in affectedTypes) {
					types.Remove (typename);
				}
				
				foreach (var pair in source_to_type) {
					pair.Value.RemoveAll (delegate (TypeMirror mirror){
						return mirror.Assembly.Location.Equals (aue.Assembly.Location, StringComparison.OrdinalIgnoreCase);
					});
				}
				OnDebuggerOutput (false, string.Format ("Unloaded assembly: {0}\n", aue.Assembly.Location));
			}
			
			if (e is VMStartEvent) {
				//HACK: 2.6.1 VM doesn't emit type load event, so work around it
				var t = vm.RootDomain.Corlib.GetType ("System.Exception", false, false);
				if (t != null)
					ResolveBreakpoints (t);
				OnVMStartEvent ((VMStartEvent) e);
			}
			
			if (e is TypeLoadEvent) {
				var t = ((TypeLoadEvent)e).Type;
				
				string typeName = t.FullName;
				
				if (types.ContainsKey (typeName)) {
					if (typeName != "System.Exception")
						LoggingService.LogError ("Type '" + typeName + "' loaded more than once", null);
				} else {
					ResolveBreakpoints (t);
				}
			}
			
			if (e is BreakpointEvent) {
				BreakpointEvent be = (BreakpointEvent)e;
				if (!HandleBreakpoint (e.Thread, be.Request)) {
					etype = TargetEventType.TargetHitBreakpoint;
					resume = false;
				}
			}
			
			if (e is ExceptionEvent) {
				var ev = (ExceptionEvent)e;
				if (ev.Request == unhandledExceptionRequest)
					etype = TargetEventType.UnhandledException;
				else
					etype = TargetEventType.ExceptionThrown;
				exception = ev.Exception;
				if (ev.Request != unhandledExceptionRequest || exception.Type.FullName != "System.Threading.ThreadAbortException")
					resume = false;
			}
			
			if (e is StepEvent) {
				etype = TargetEventType.TargetStopped;
				resume = false;
			}
			
			if (e is ThreadStartEvent) {
				ThreadStartEvent ts = (ThreadStartEvent)e;
				OnDebuggerOutput (false, string.Format ("Thread started: {0}\n", ts.Thread.Name));
			}
			
			if (resume)
				vm.Resume ();
			else {
				if (currentStepRequest != null) {
					currentStepRequest.Enabled = false;
					currentStepRequest = null;
				}
				current_thread = recent_thread = e.Thread;
				TargetEventArgs args = new TargetEventArgs (etype);
				args.Process = OnGetProcesses () [0];
				args.Thread = GetThread (args.Process, current_thread);
				args.Backtrace = GetThreadBacktrace (current_thread);
				
				if (exception != null)
					activeExceptionsByThread [current_thread.Id] = exception;
				
				OnTargetEvent (args);
			}
		}

		protected virtual void OnHandleEvent (Event e)
		{
		}

		protected virtual void OnVMStartEvent (VMStartEvent e)
		{
		}

		protected virtual void OnVMDeathEvent ()
		{
		}

		public ObjectMirror GetExceptionObject (ThreadMirror thread)
		{
			ObjectMirror obj;
			if (activeExceptionsByThread.TryGetValue (thread.Id, out obj))
				return obj;
			else
				return null;
		}
		
		void QueueEvent (Event ev)
		{
#if DEBUG_EVENT_QUEUEING
			Console.WriteLine ("qq event: " + ev);
#endif
			lock (queuedEvents) {
				queuedEvents.AddLast (ev);
			}
		}
		
		void RemoveQueuedEvents (EventRequest request)
		{
			int resume = 0;
			lock (queuedEvents) {
				var node = queuedEvents.First;
				while (node != null) {
					if (node.Value.Request == request) {
						var d = node;
						node = node.Next;
						queuedEvents.Remove (d);
						resume++;
					} else {
						node = node.Next;
					}
				}
			}
			for (int i = 0; i < resume; i++)
				vm.Resume ();
		}
		
		void DequeueEventsForFirstThread ()
		{
			List<Event> dequeuing;
			lock (queuedEvents) {
				if (queuedEvents.Count < 1)
					return;
				
				dequeuing = new List<Event> ();
				var node = queuedEvents.First;
				
				//making this the current thread means that all events from other threads will get queued
				current_thread = node.Value.Thread;
				while (node != null) {
					if (node.Value.Thread.Id == current_thread.Id) {
						var d = node;
						node = node.Next;
						dequeuing.Add (d.Value);
						queuedEvents.Remove (d);
					} else {
						node = node.Next;
					}
				}
			}

#if DEBUG_EVENT_QUEUEING
			foreach (var e in dequeuing)
				Console.WriteLine ("dq event: " + e);
#endif

			//firing this off in a thread prevents possible infinite recursion
			ThreadPool.QueueUserWorkItem (delegate {
				if (!exited) {
					foreach (var ev in dequeuing) {
						try {
							 HandleEvent (ev, true);
						} catch (VMDisconnectedException ex) {
							OnDebuggerOutput (true, ex.ToString ());
							break;
						} catch (Exception ex) {
							OnDebuggerOutput (true, ex.ToString ());
						}
					}
				}
			});
		}
		
		bool HandleBreakpoint (ThreadMirror thread, EventRequest er)
		{
			BreakInfo binfo;
			if (!breakpoints.TryGetValue (er, out binfo))
				return false;
			
			Breakpoint bp = binfo.BreakEvent as Breakpoint;
			if (bp == null)
				return false;
			
			if (bp.HitCount > 1) {
				// Just update the count and continue
				UpdateHitCount (binfo, bp.HitCount - 1);
				return true;
			}
			
			if (!string.IsNullOrEmpty (bp.ConditionExpression)) {
				string res = EvaluateExpression (thread, bp.ConditionExpression);
				if (bp.BreakIfConditionChanges) {
					if (res == binfo.LastConditionValue)
						return true;
					binfo.LastConditionValue = res;
				} else {
					if (res != null && res.ToLower () == "false")
						return true;
				}
			}
			switch (bp.HitAction) {
				case HitAction.CustomAction:
					// If custom action returns true, execution must continue
					return OnCustomBreakpointAction (bp.CustomActionId, binfo);
				case HitAction.PrintExpression: {
					string exp = EvaluateTrace (thread, bp.TraceExpression);
					UpdateLastTraceValue (binfo, exp);
					return true;
				}
				case HitAction.Break:
					return false;
			}
			return false;
		}
		
		string EvaluateTrace (ThreadMirror thread, string exp)
		{
			StringBuilder sb = new StringBuilder ();
			int last = 0;
			int i = exp.IndexOf ('{');
			while (i != -1) {
				if (i < exp.Length - 1 && exp [i+1] == '{') {
					sb.Append (exp.Substring (last, i - last + 1));
					last = i + 2;
					i = exp.IndexOf ('{', i + 2);
					continue;
				}
				int j = exp.IndexOf ('}', i + 1);
				if (j == -1)
					break;
				string se = exp.Substring (i + 1, j - i - 1);
				se = EvaluateExpression (thread, se);
				sb.Append (exp.Substring (last, i - last));
				sb.Append (se);
				last = j + 1;
				i = exp.IndexOf ('{', last);
			}
			sb.Append (exp.Substring (last, exp.Length - last));
			return sb.ToString ();
		}
		
		string EvaluateExpression (ThreadMirror thread, string exp)
		{
			try {
				MDB.StackFrame[] frames = thread.GetFrames ();
				if (frames.Length == 0)
					return string.Empty;
				EvaluationOptions ops = Options.EvaluationOptions;
				ops.AllowTargetInvoke = true;
				SoftEvaluationContext ctx = new SoftEvaluationContext (this, frames[0], ops);
				ValueReference val = ctx.Evaluator.Evaluate (ctx, exp);
				return val.CreateObjectValue (false).Value;
			} catch (Exception ex) {
				OnDebuggerOutput (true, ex.ToString ());
				return string.Empty;
			}
		}
		
		void ResolveBreakpoints (TypeMirror t)
		{
			string typeName = t.FullName;
			types [typeName] = t;
			
			/* Handle pending breakpoints */
			
			var resolved = new List<BreakEvent> ();
			
			foreach (string s in t.GetSourceFiles ()) {
				List<TypeMirror> typesList;
				
				if (source_to_type.TryGetValue (s, out typesList)) {
					typesList.Add (t);
				} else {
					typesList = new List<TypeMirror> ();
					typesList.Add (t);
					source_to_type[s] = typesList;
				}
				
				
				foreach (var bp in pending_bes.OfType<Breakpoint> ()) {
					if (System.IO.Path.GetFileName (bp.FileName) == s) {
						Location l = GetLocFromType (t, s, bp.Line);
						if (l != null) {
							OnDebuggerOutput (false, string.Format ("Resolved pending breakpoint at '{0}:{1}' to {2}:{3}.\n",
							                                        s, bp.Line, l.Method.FullName, l.ILOffset));
							ResolvePendingBreakpoint (bp, l);
							resolved.Add (bp);
						} else {
							OnDebuggerOutput (true, string.Format ("Could not insert pending breakpoint at '{0}:{1}'. " +
								"Perhaps the source line does not contain any statements, or the source does not correspond " +
								"to the current binary.\n", s, bp.Line));
						}
					}
				}
				
				foreach (var be in resolved)
					pending_bes.Remove (be);
				resolved.Clear ();
			}
			
			//handle pending catchpoints
			
			foreach (var cp in pending_bes.OfType<Catchpoint> ()) {
				if (cp.ExceptionName == typeName) {
					ResolvePendingCatchpoint (cp, t);
					resolved.Add (cp);
				}
			}
			foreach (var be in resolved)
				pending_bes.Remove (be);
		}
		
		Location GetLocFromType (TypeMirror type, string file, int line)
		{
			Location target_loc = null;
			foreach (MethodMirror m in type.GetMethods ()) {
				foreach (Location l in m.Locations) {
					if (System.IO.Path.GetFileName (l.SourceFile) == file && l.LineNumber == line) {
						target_loc = l;
						break;
						}
				}
				if (target_loc != null)
					break;
			}
	
			return target_loc;
		}

		void ResolvePendingBreakpoint (Breakpoint bp, Location l)
		{
			BreakInfo bi = GetBreakInfo (bp);
			if (bi != null) {
				bi.Location = l;
				InsertBreakpoint (bp, bi);
				SetBreakEventStatus (bp, true);
			}
		}
				
		void ResolvePendingCatchpoint (Catchpoint cp, TypeMirror type)
		{
			BreakInfo bi = GetBreakInfo (cp);
			InsertCatchpoint (cp, bi, type);
			SetBreakEventStatus (cp, true);
		}
		
		bool UpdateAssemblyFilters (AssemblyMirror asm)
		{
			var name = asm.GetName ().FullName;
			if (userAssemblyNames != null) {
				//HACK: not sure how else to handle xsp-compiled pages
				if (name.StartsWith ("App_")) {
					assemblyFilters.Add (asm);
					return true;
				}
			
				foreach (var n in userAssemblyNames) {
					if (n == name) {
						assemblyFilters.Add (asm);
						return true;
					}
				}
			}
			return false;
		}
		
		internal void WriteDebuggerOutput (bool isError, string msg)
		{
			OnDebuggerOutput (isError, msg);
		}
		
		protected override void OnSetActiveThread (long processId, long threadId)
		{
		}

		protected override void OnStepInstruction ()
		{
			throw new System.NotImplementedException ();
		}

		protected override void OnStepLine ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				Adaptor.CancelAsyncOperations (); // This call can block, so it has to run in background thread to avoid keeping the main session lock
				var req = vm.CreateStepRequest (current_thread);
				req.Depth = StepDepth.Into;
				req.Size = StepSize.Line;
				if (assemblyFilters != null && assemblyFilters.Count > 0)
					req.AssemblyFilter = assemblyFilters;
				req.Enabled = true;
				currentStepRequest = req;
				OnResumed ();
				vm.Resume ();
				DequeueEventsForFirstThread ();
			});
		}

		protected override void OnStop ()
		{
			vm.Suspend ();
			
			//emit a stop event at the current position of the most recent thread
			//we use "getprocesses" instead of "ongetprocesses" because it attaches the process to the session
			//using private Mono.Debugging API, so our thread/backtrace calls will cache stuff that will get used later
			var process = GetProcesses () [0];				
			EnsureRecentThreadIsValid (process);
			OnTargetEvent (new TargetEventArgs (TargetEventType.TargetStopped) {
				Process = process,
				Thread = GetThread (process, recent_thread),
				Backtrace = GetThreadBacktrace (recent_thread)});
		}
		
		void EnsureRecentThreadIsValid (ProcessInfo process)
		{
			var infos = process.GetThreads ();
			
			if (ThreadIsAlive (recent_thread) && HasUserFrame (recent_thread.Id, infos))
				return;

			var threads = vm.GetThreads ();
			foreach (var thread in threads) {
				if (ThreadIsAlive (thread) && HasUserFrame (thread.Id, infos)) {
					recent_thread = thread;
					return;
				}
			}
			recent_thread = threads[0];	
		}
		
		static bool ThreadIsAlive (ThreadMirror thread)
		{
			if (thread == null)
				return false;
			var state = thread.ThreadState;
			return state != ThreadState.Stopped && state != ThreadState.Aborted;
		}
		
		//we use the Mono.Debugging classes because they are cached
		bool HasUserFrame (long tid, ThreadInfo[] infos)
		{
			foreach (var t in infos) {
				if (t.Id != tid)
					continue;
				var bt = t.Backtrace;
				for (int i = 0; i < bt.FrameCount; i++) {
					var frame = bt.GetFrame (i);
					if (frame != null && !frame.IsExternalCode)
						return true;
				}
				return false;
			}
			return false;
		}
		
		BreakInfo GetBreakInfo (BreakEvent be)
		{
			object bi;
			if (GetBreakpointHandle (be, out bi))
				return (BreakInfo) bi;
			else
				return null;
		}
		
		public bool IsExternalCode (Mono.Debugger.Soft.StackFrame frame)
		{
			return frame.Method == null || string.IsNullOrEmpty (frame.FileName)
				|| (assemblyFilters != null && !assemblyFilters.Contains (frame.Method.DeclaringType.Assembly));
		}
		
		public bool IsExternalCode (TypeMirror type)
		{
			return assemblyFilters != null && !assemblyFilters.Contains (type.Assembly);
		}
	}
	
	class BreakInfo
	{
		public bool Enabled;
		public Location Location;
		public EventRequest Req;
		public BreakEvent BreakEvent;
		public string LastConditionValue;
	}
}
