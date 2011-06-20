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
		Dictionary<string, List<TypeMirror>> source_to_type = new Dictionary<string, List<TypeMirror>> (PathComparer);
		bool useFullPaths = true;
		Dictionary<string,TypeMirror> types = new Dictionary<string, TypeMirror> ();
		Dictionary<EventRequest,BreakInfo> breakpoints = new Dictionary<EventRequest,BreakInfo> ();
		List<BreakInfo> pending_bes = new List<BreakInfo> ();
		ThreadMirror current_thread, recent_thread;
		ProcessInfo[] procs;
		ThreadInfo[] current_threads;
		bool exited;
		bool started;
		internal int StackVersion;
		StepEventRequest currentStepRequest;
		ExceptionEventRequest unhandledExceptionRequest;
		string remoteProcessName;
		Dictionary<long,long> localThreadIds = new Dictionary<long, long> ();
		IConnectionDialog connectionDialog;
		
		Dictionary<long,ObjectMirror> activeExceptionsByThread = new Dictionary<long, ObjectMirror> ();
		
		Thread outputReader;
		Thread errorReader;
		
		IAsyncResult connectionHandle;
		
		LinkedList<List<Event>> queuedEventSets = new LinkedList<List<Event>> ();
		
		List<string> userAssemblyNames;
		List<AssemblyMirror> assemblyFilters;
		
		bool loggedSymlinkedRuntimesBug = false;
		
		public SoftDebuggerAdaptor Adaptor {
			get { return adaptor; }
		}
		
		readonly SoftDebuggerAdaptor adaptor = new SoftDebuggerAdaptor ();
		
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
			if (dsi.StartArgs is SoftDebuggerLaunchArgs) {
				StartLaunching (dsi);
			} else if (dsi.StartArgs is SoftDebuggerConnectArgs) {
				StartConnecting (dsi);
			} else if (dsi.StartArgs is SoftDebuggerListenArgs) {
				StartListening (dsi);
			} else {
				throw new Exception (string.Format ("Unknown args: {0}", dsi.StartArgs));
			}
		}
		
		void StartLaunching (SoftDebuggerStartInfo dsi)
		{
			var args = (SoftDebuggerLaunchArgs) dsi.StartArgs;
			var runtime = Path.Combine (Path.Combine (args.MonoRuntimePrefix, "bin"), "mono");
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
			
			if (dsi.UseExternalConsole && args.ExternalConsoleLauncher != null) {
				options = new LaunchOptions ();
				options.CustomTargetProcessLauncher = args.ExternalConsoleLauncher;
				psi.RedirectStandardOutput = false;
				psi.RedirectStandardError = false;
			}

			var sdbLog = Environment.GetEnvironmentVariable ("MONODEVELOP_SDB_LOG");
			if (!String.IsNullOrEmpty (sdbLog)) {
				options = options ?? new LaunchOptions ();
				options.AgentArgs = string.Format ("loglevel=1,logfile='{0}'", sdbLog);
			}
			
			foreach (var env in args.MonoRuntimeEnvironmentVariables)
				psi.EnvironmentVariables[env.Key] = env.Value;
			
			foreach (var env in dsi.EnvironmentVariables)
				psi.EnvironmentVariables[env.Key] = env.Value;
			
			if (!String.IsNullOrEmpty (dsi.LogMessage))
				OnDebuggerOutput (false, dsi.LogMessage + "\n");
			
			var callback = HandleConnectionCallbackErrors ((IAsyncResult ar) => {
				ConnectionStarted (VirtualMachineManager.EndLaunch (ar));
			});
			ConnectionStarting (VirtualMachineManager.BeginLaunch (psi, callback, options), dsi, true, 0);
		}
		
		/// <summary>Starts the debugger listening for a connection over TCP/IP</summary>
		protected void StartListening (SoftDebuggerStartInfo dsi)
		{
			int dp, cp;
			StartListening (dsi, out dp, out cp);
		}
		
		/// <summary>Starts the debugger listening for a connection over TCP/IP</summary>
		protected void StartListening (SoftDebuggerStartInfo dsi, out int assignedDebugPort)
		{
			int cp;
			StartListening (dsi, out assignedDebugPort, out cp);
		}
		
		/// <summary>Starts the debugger listening for a connection over TCP/IP</summary>
		protected void StartListening (SoftDebuggerStartInfo dsi,
			out int assignedDebugPort, out int assignedConsolePort)
		{
		
			IPEndPoint dbgEP, conEP;
			InitForRemoteSession (dsi, out dbgEP, out conEP);
			
			var callback = HandleConnectionCallbackErrors (delegate (IAsyncResult ar) {
				ConnectionStarted (VirtualMachineManager.EndListen (ar));
			});
			var a = VirtualMachineManager.BeginListen (dbgEP, conEP, callback, out assignedDebugPort, out assignedConsolePort);
			ConnectionStarting (a, dsi, true, 0);
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
		
		protected void StartConnecting (SoftDebuggerStartInfo dsi)
		{
			var args = (SoftDebuggerConnectArgs) dsi.StartArgs;
			StartConnecting (dsi, args.MaxConnectionAttempts, args.TimeBetweenConnectionAttempts);
		}
		
		/// <summary>Starts the debugger connecting to a remote IP</summary>
		protected void StartConnecting (SoftDebuggerStartInfo dsi, int maxAttempts, int timeBetweenAttempts)
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
					if (!ShouldRetryConnection (ex, attemptNumber) || attemptNumber == maxAttempts || Exited) {
						OnConnectionError (ex);
						return;
					}
				}
				try {
					if (timeBetweenAttempts > 0)
						System.Threading.Thread.Sleep (timeBetweenAttempts);
					
					ConnectionStarting (VirtualMachineManager.BeginConnect (dbgEP, conEP, callback), dsi, false, attemptNumber);
					
				} catch (Exception ex2) {
					OnConnectionError (ex2);
				}
			};
			
			ConnectionStarting (VirtualMachineManager.BeginConnect (dbgEP, conEP, callback), dsi, false, 0);
		}
		
		void InitForRemoteSession (SoftDebuggerStartInfo dsi, out IPEndPoint dbgEP, out IPEndPoint conEP)
		{
			if (remoteProcessName != null)
				throw new InvalidOperationException ("Cannot initialize connection more than once");
			
			var args = (SoftDebuggerRemoteArgs) dsi.StartArgs;
			
			remoteProcessName = args.AppName;
			if (string.IsNullOrEmpty (remoteProcessName))
				remoteProcessName = "mono";
			
			RegisterUserAssemblies (dsi.UserAssemblyNames);
			
			dbgEP = new IPEndPoint (args.Address, args.DebugPort);
			conEP = args.RedirectOutput? new IPEndPoint (args.Address, args.OutputPort) : null;
			
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
		
		void ConnectionStarting (IAsyncResult connectionHandle, DebuggerStartInfo dsi, bool listening, int attemptNumber) 
		{
			if (this.connectionHandle != null && (attemptNumber == 0 || !this.connectionHandle.IsCompleted))
				throw new InvalidOperationException ("Already connecting");
			
			this.connectionHandle = connectionHandle;
			
			if (ConnectionDialogCreator != null && attemptNumber == 0) {
				connectionDialog = ConnectionDialogCreator ();
				connectionDialog.UserCancelled += delegate {
					EndSession ();
				};
			}
			if (connectionDialog != null)
				connectionDialog.SetMessage (dsi, GetConnectingMessage (dsi), listening, attemptNumber);
		}
		
		protected virtual string GetConnectingMessage (DebuggerStartInfo dsi)
		{
			return null;
		}
		
		void EndLaunch ()
		{
			HideConnectionDialog ();
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
		
		void HideConnectionDialog ()
		{
			if (connectionDialog != null) {
				connectionDialog.Dispose ();
				connectionDialog = null;
			}
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
			
			//full paths, from GetSourceFiles (true), are only supported by sdb protocol 2.2 and later
			useFullPaths = vm.Version.AtLeast (2, 2);
			
			ConnectOutput (vm.StandardOutput, false);
			ConnectOutput (vm.StandardError, true);
			
			HideConnectionDialog ();
			
			vm.EnableEvents (EventType.AssemblyLoad, EventType.TypeLoad, EventType.ThreadStart, EventType.ThreadDeath,
				EventType.AssemblyUnload, EventType.UserBreak, EventType.UserLog);
			try {
				unhandledExceptionRequest = vm.CreateExceptionRequest (null, false, true);
				unhandledExceptionRequest.Enable ();
			} catch (NotSupportedException) {
				//Mono < 2.6.3 doesn't support catching unhandled exceptions
			}
			
			started = true;
			
			/* Wait for the VMStart event */
			HandleEventSet (vm.GetNextEventSet ());
			
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
		
		protected bool SetSocketTimeouts (int send_timeout, int receive_timeout, int keepalive_interval)
		{
			try {
				if (vm.Version.AtLeast (2, 4)) {
					vm.EnableEvents (EventType.KeepAlive);
					vm.SetSocketTimeouts (send_timeout, receive_timeout, keepalive_interval);
					return true;
				} else {
					return false;
				}
			} catch {
				return false;
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
				var buffer = new char [1024];
				while (!exited) {
					int c = reader.Read (buffer, 0, buffer.Length);
					if (c > 0) {
						OnTargetOutput (isError, new string (buffer, 0, c));
					} else {
						//FIXME: workaround for buggy console stream that never blocks
						Thread.Sleep (250);
					}
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
			throw new System.NotSupportedException ();
		}

		protected override void OnContinue ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					Adaptor.CancelAsyncOperations (); // This call can block, so it has to run in background thread to avoid keeping the main session lock
					OnResumed ();
					vm.Resume ();
					DequeueEventsForFirstThread ();
				} catch (Exception ex) {
					if (!HandleException (ex))
						OnDebuggerOutput (true, ex.ToString ());
				}
			});
		}

		protected override void OnDetach ()
		{
			throw new System.NotSupportedException ();
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
				try {
					if (vm.Process != null) {
						ThreadPool.QueueUserWorkItem (delegate {
							// This is a workaround for a mono bug
							// Without this call, the process may become zombie in mono < 2.10.2
							vm.Process.WaitForExit ();
						});
					}
				} catch {
					// Ignore
				}
				var t = new System.Timers.Timer ();
				t.Interval = 1000;
				t.Elapsed += delegate {
					try {
						t.Enabled = false;
						t.Dispose ();
						EnsureExited ();
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
			Step (StepDepth.Out, StepSize.Line);
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
				IList<ThreadMirror> mirrors = vm.GetThreads ();
				var threads = new ThreadInfo[mirrors.Count];
				for (int i = 0; i < mirrors.Count; i++) {
					ThreadMirror t = mirrors [i];
					string name = t.Name;
					if (string.IsNullOrEmpty (name) && t.IsThreadPoolThread)
						name = "<Thread Pool>";
					threads[i] = new ThreadInfo (processId, GetId (t), name, null);
				}
				current_threads = threads;
			}
			return current_threads;
		}
		
		ThreadMirror GetThread (long processId, long threadId)
		{
			foreach (ThreadMirror t in vm.GetThreads ())
				if (GetId (t) == threadId)
					return t;
			return null;
		}
		
		ThreadInfo GetThread (ProcessInfo process, ThreadMirror thread)
		{
			long tid = GetId (thread);
			foreach (var t in OnGetThreads (process.Id))
				if (t.Id == tid)
					return t;
			return null;
		}
		
		protected override BreakEventInfo OnInsertBreakEvent (BreakEvent ev)
		{
			if (exited)
				return null;
			
			var bi = new BreakInfo ();
			
			if (ev is Breakpoint) {
				var bp = (Breakpoint) ev;
				bool inisideLoadedRange;
				bi.Location = FindLocation (bp.FileName, bp.Line, out inisideLoadedRange);
				if (bi.Location != null) {
					InsertBreakpoint (bp, bi);
					bi.SetStatus (BreakEventStatus.Bound, null);
				}
				else {
					pending_bes.Add (bi);
					if (inisideLoadedRange)
						bi.SetStatus (BreakEventStatus.Invalid, null);
					else
						bi.SetStatus (BreakEventStatus.NotBound, null);
				}
			} else if (ev is Catchpoint) {
				var cp = (Catchpoint) ev;
				TypeMirror type;
				if (types.TryGetValue (cp.ExceptionName, out type)) {
					InsertCatchpoint (cp, bi, type);
					bi.SetStatus (BreakEventStatus.Bound, null);
				} else {
					pending_bes.Add (bi);
					bi.SetStatus (BreakEventStatus.NotBound, null);
				}
			}
			return bi;
		}

		protected override void OnRemoveBreakEvent (BreakEventInfo binfo)
		{
			if (exited)
				return;
			var bi = (BreakInfo) binfo;
			if (bi.Req != null) {
				bi.Req.Enabled = false;
				RemoveQueuedBreakEvents (bi.Req);
			}
			pending_bes.Remove (bi);
		}

		protected override void OnEnableBreakEvent (BreakEventInfo binfo, bool enable)
		{
			if (exited)
				return;
			var bi = (BreakInfo) binfo;
			if (bi.Req != null) {
				bi.Req.Enabled = enable;
				if (!enable)
					RemoveQueuedBreakEvents (bi.Req);
			}
		}

		protected override void OnUpdateBreakEvent (BreakEventInfo binfo)
		{
		}

		void InsertBreakpoint (Breakpoint bp, BreakInfo bi)
		{
			bi.Req = vm.SetBreakpoint (bi.Location.Method, bi.Location.ILOffset);
			bi.Req.Enabled = bp.Enabled;
			breakpoints [bi.Req] = bi;
			
			if (bi.Location.LineNumber != bp.Line)
				bi.AdjustBreakpointLocation (bi.Location.LineNumber);
		}
		
		void InsertCatchpoint (Catchpoint cp, BreakInfo bi, TypeMirror excType)
		{
			var request = bi.Req = vm.CreateExceptionRequest (excType, true, true);
			request.Count = cp.HitCount;
			bi.Req.Enabled = cp.Enabled;
		}
		
		Location FindLocation (string file, int line, out bool inisideLoadedRange)
		{
			inisideLoadedRange = false;
			if (!started)
				return null;

			string filename = PathToFileName (file);
	
			Location target_loc = null;
	
			// Try already loaded types in the current source file
			List<TypeMirror> types;

			if (source_to_type.TryGetValue (filename, out types)) {
				foreach (TypeMirror t in types) {
					bool insideRange;
					target_loc = GetLocFromType (t, filename, line, out insideRange);
					if (insideRange)
						inisideLoadedRange = true;
					if (target_loc != null)
						break;
				}
			}
	
			// FIXME: Add a pending breakpoint
	
			return target_loc;
		}
		
		public override bool CanCancelAsyncEvaluations {
			get {
				return Adaptor.IsEvaluating;
			}
		}
		
		protected override void OnCancelAsyncEvaluations ()
		{
			Adaptor.CancelAsyncOperations ();
		}
		
		protected override void OnNextInstruction ()
		{
			Step (StepDepth.Over, StepSize.Min);
		}

		protected override void OnNextLine ()
		{
			Step (StepDepth.Over, StepSize.Line);
		}
		
		void Step (StepDepth depth, StepSize size)
		{
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					Adaptor.CancelAsyncOperations (); // This call can block, so it has to run in background thread to avoid keeping the main session lock
					var req = vm.CreateStepRequest (current_thread);
					req.Depth = depth;
					req.Size = size;
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
					EventSet e = vm.GetNextEventSet ();
					var type = e[0].EventType;
					if (type == EventType.VMDeath || type == EventType.VMDisconnect) {
						break;
					}
					HandleEventSet (e);
				} catch (VMDisconnectedException ex) {
					if (!HandleException (ex))
						OnDebuggerOutput (true, ex.ToString ());
					break;
				} catch (Exception ex) {
					if (!HandleException (ex))
						OnDebuggerOutput (true, ex.ToString ());
				}
			}
			
			try {
				// This is a workaround for a mono bug
				// Without this call, the process may become zombie in mono < 2.10.2
				if (vm.Process != null)
					vm.Process.WaitForExit (1);
			} catch {
				// Ignore
			}
			
			exited = true;
			OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
		}
		
		protected override bool HandleException (Exception ex)
		{
			HideConnectionDialog ();
			
			if (ex is VMDisconnectedException)
				ex = new DisconnectedException ();
			else if (ex is SocketException)
				ex = new DebugSocketException (ex);
			
			return base.HandleException (ex);
		}
		
		// This method dispatches an event set.
		//
		// Based on the subset of events for which we register, and the contract for EventSet contents (equivalent to 
		// Java - http://download.oracle.com/javase/1.5.0/docs/guide/jpda/jdi/com/sun/jdi/event/EventSet.html)
		// we know that event sets we receive are either:
		// 1) Set of step and break events for a location in a single thread.
		// 2) Set of catchpoints for a single exception.
		// 3) A single event of any other kind.
		// We verify these assumptions where possible, because things will break in horrible ways if they are wrong.
		//
		// If we are currently stopped on a thread, and the break events are on a different thread, we must queue
		// that event set and dequeue it next time we resume. This eliminates race conditions when multiple threads
		// hit breakpoints or catchpoints simultaneously.
		//
		void HandleEventSet (EventSet es)
		{
#if DEBUG_EVENT_QUEUEING
			if (!(es[0] is TypeLoadEvent))
				Console.WriteLine ("pp eventset({0}): {1}", es.Events.Length, es[0]);
#endif
			var type = es[0].EventType;
			bool isBreakEvent = type == EventType.Step || type == EventType.Breakpoint || type == EventType.Exception || type == EventType.UserBreak;
			
			if (isBreakEvent) {
				if (current_thread != null && es[0].Thread.Id != current_thread.Id) {
					QueueBreakEventSet (es.Events);
				} else {
					HandleBreakEventSet (es.Events, false);
				}
			} else {
				if (es.Events.Length != 1)
					throw new InvalidOperationException ("EventSet has unexpected combination of events");
				HandleEvent (es[0]);
				vm.Resume ();
			}
		}
		
		void HandleBreakEventSet (Event[] es, bool dequeuing)
		{
			if (dequeuing && exited)
				return;
			
			bool resume = true;
			ObjectMirror exception = null;
			TargetEventType etype = TargetEventType.TargetStopped;
			BreakEvent breakEvent = null;
			
			if (es[0] is ExceptionEvent) {
				var bad = es.FirstOrDefault (ee => ee.EventType != EventType.Exception);
				if (bad != null)
					throw new Exception ("Catchpoint eventset had unexpected event type " + bad.GetType ());
				var ev = (ExceptionEvent)es[0];
				if (ev.Request == unhandledExceptionRequest)
					etype = TargetEventType.UnhandledException;
				else
					etype = TargetEventType.ExceptionThrown;
				exception = ev.Exception;
				if (ev.Request != unhandledExceptionRequest || exception.Type.FullName != "System.Threading.ThreadAbortException")
					resume = false;
			}
			else {
				//always need to evaluate all breakpoints, some might be tracepoints or conditional bps with counters
				foreach (Event e in es) {
					var be = e as BreakpointEvent;
					if (be != null) {
						if (!HandleBreakpoint (e.Thread, be.Request)) {
							etype = TargetEventType.TargetHitBreakpoint;
							BreakInfo binfo;
							if (breakpoints.TryGetValue (be.Request, out binfo))
								breakEvent = binfo.BreakEvent;
							resume = false;
						}
					} else if (e.EventType == EventType.Step) {
						etype = TargetEventType.TargetStopped;
						resume = false;
					} else if (e.EventType == EventType.UserBreak) {
						etype = TargetEventType.TargetStopped;
						resume = false;
					} else {
						throw new Exception ("Break eventset had unexpected event type " + e.GetType ());
					}
				}
			}
			
			if (resume) {
				//all breakpoints were conditional and evaluated as false
				vm.Resume ();
				DequeueEventsForFirstThread ();
			} else {
				if (currentStepRequest != null) {
					currentStepRequest.Enabled = false;
					currentStepRequest = null;
				}
				current_thread = recent_thread = es[0].Thread;
				var args = new TargetEventArgs (etype);
				args.Process = OnGetProcesses () [0];
				args.Thread = GetThread (args.Process, current_thread);
				args.Backtrace = GetThreadBacktrace (current_thread);
				args.BreakEvent = breakEvent;
				
				if (exception != null)
					activeExceptionsByThread [current_thread.ThreadId] = exception;
				
				OnTargetEvent (args);
			}
		}
		
		void HandleEvent (Event e)
		{
			switch (e.EventType) {
			case EventType.AssemblyLoad: {
				var ae = (AssemblyLoadEvent) e;
				bool isExternal = !UpdateAssemblyFilters (ae.Assembly) && userAssemblyNames != null;
				string flagExt = isExternal? " [External]" : "";
				OnDebuggerOutput (false, string.Format ("Loaded assembly: {0}{1}\n", ae.Assembly.Location, flagExt));
				break;
			}
			case EventType.AssemblyUnload: {
				var aue = (AssemblyUnloadEvent) e;
				
				// Mark affected breakpoints as pending again
				var affectedBreakpoints = new List<KeyValuePair<EventRequest, BreakInfo>> (
					breakpoints.Where (x=> (x.Value.Location.Method.DeclaringType.Assembly.Location.Equals (aue.Assembly.Location, StringComparison.OrdinalIgnoreCase)))
				);
				foreach (KeyValuePair<EventRequest,BreakInfo> breakpoint in affectedBreakpoints) {
					string file = PathToFileName (breakpoint.Value.Location.SourceFile);
					int line = breakpoint.Value.Location.LineNumber;
					OnDebuggerOutput (false, string.Format ("Re-pending breakpoint at {0}:{1}\n", file, line));
					breakpoints.Remove (breakpoint.Key);
					pending_bes.Add (breakpoint.Value);
				}
				
				// Remove affected types from the loaded types list
				var affectedTypes = new List<string> (
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
				break;
			}
			case EventType.VMStart: {
				OnStarted (new ThreadInfo (0, GetId (e.Thread), e.Thread.Name, null));
				//HACK: 2.6.1 VM doesn't emit type load event, so work around it
				var t = vm.RootDomain.Corlib.GetType ("System.Exception", false, false);
				if (t != null)
					ResolveBreakpoints (t);
				break;
			}
			case EventType.TypeLoad: {
				var t = ((TypeLoadEvent)e).Type;
				
				string typeName = t.FullName;
				
				if (types.ContainsKey (typeName)) {
					if (typeName != "System.Exception" && typeName != "<Module>")
						LoggingService.LogError ("Type '" + typeName + "' loaded more than once", null);
				} else {
					ResolveBreakpoints (t);
				}
				break;
			}
			case EventType.ThreadStart: {
				var ts = (ThreadStartEvent) e;
				OnDebuggerOutput (false, string.Format ("Thread started: {0}\n", ts.Thread.Name));
				OnTargetEvent (new TargetEventArgs (TargetEventType.ThreadStarted) {
					Thread = new ThreadInfo (0, GetId (ts.Thread), ts.Thread.Name, null),
				});
				break;
			}
			case EventType.ThreadDeath: {
				var ts = (ThreadDeathEvent) e;
				OnDebuggerOutput (false, string.Format ("Thread finished: {0}\n", ts.Thread.Name));
				OnTargetEvent (new TargetEventArgs (TargetEventType.ThreadStopped) {
					Thread = new ThreadInfo (0, GetId (ts.Thread), ts.Thread.Name, null),
				});
				break;
			}
			case EventType.UserLog: {
				var ul = (UserLogEvent) e;
				OnDebuggerOutput (false, string.Format ("[{0}:{1}] {2}\n", ul.Level, ul.Category, ul.Message));
				break;
			}
			default:
				Console.WriteLine ("Unknown debugger event type {0}", e.GetType ());
				break;
			}
		}

		public ObjectMirror GetExceptionObject (ThreadMirror thread)
		{
			ObjectMirror obj;
			if (activeExceptionsByThread.TryGetValue (thread.ThreadId, out obj))
				return obj;
			else
				return null;
		}
		
		void QueueBreakEventSet (Event[] eventSet)
		{
#if DEBUG_EVENT_QUEUEING
			Console.WriteLine ("qq eventset({0}): {1}", eventSet.Length, eventSet[0]);
#endif
			var events = new List<Event> (eventSet);
			lock (queuedEventSets) {
				queuedEventSets.AddLast (events);
			}
		}
		
		void RemoveQueuedBreakEvents (EventRequest request)
		{
			int resume = 0;
			lock (queuedEventSets) {
				var node = queuedEventSets.First;
				while (node != null) {
					List<Event> q = node.Value;
					for (int i = 0; i < q.Count; i++)
						if (q[i].Request == request)
							q.RemoveAt (i--);
					if (q.Count == 0) {
						var d = node;
						node = node.Next;
						queuedEventSets.Remove (d);
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
			List<List<Event>> dequeuing;
			lock (queuedEventSets) {
				if (queuedEventSets.Count < 1)
					return;
				
				dequeuing = new List<List<Event>> ();
				var node = queuedEventSets.First;
				
				//making this the current thread means that all events from other threads will get queued
				current_thread = node.Value[0].Thread;
				while (node != null) {
					if (node.Value[0].Thread.Id == current_thread.Id) {
						var d = node;
						node = node.Next;
						dequeuing.Add (d.Value);
						queuedEventSets.Remove (d);
					} else {
						node = node.Next;
					}
				}
			}

#if DEBUG_EVENT_QUEUEING
			foreach (var e in dequeuing)
				Console.WriteLine ("dq eventset({0}): {1}", e.Count, e[0]);
#endif

			//firing this off in a thread prevents possible infinite recursion
			ThreadPool.QueueUserWorkItem (delegate {
				if (!exited) {
					foreach (var es in dequeuing) {
						try {
							 HandleBreakEventSet (es.ToArray (), true);
						} catch (VMDisconnectedException ex) {
							if (!HandleException (ex))
								OnDebuggerOutput (true, ex.ToString ());
							break;
						} catch (Exception ex) {
							if (!HandleException (ex))
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
			
			var bp = binfo.BreakEvent as Breakpoint;
			if (bp == null)
				return false;
			
			if (bp.HitCount > 0) {
				// Just update the count and continue
				binfo.UpdateHitCount (bp.HitCount - 1);
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
					return binfo.RunCustomBreakpointAction (bp.CustomActionId);
				case HitAction.PrintExpression: {
					string exp = EvaluateTrace (thread, bp.TraceExpression);
					binfo.UpdateLastTraceValue (exp);
					return true;
				}
				case HitAction.Break:
					return false;
			}
			return false;
		}
		
		string EvaluateTrace (ThreadMirror thread, string exp)
		{
			var sb = new StringBuilder ();
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
				var ctx = new SoftEvaluationContext (this, frames[0], ops);
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
			
			var resolved = new List<BreakInfo> ();
			
			//get the source file paths
			//full paths, from GetSourceFiles (true), are only supported by sdb protocol 2.2 and later
			string[] sourceFiles;
			if (useFullPaths) {
				sourceFiles = t.GetSourceFiles (true);
			} else {
				sourceFiles = t.GetSourceFiles ();
				
				//HACK: if mdb paths are windows paths but the sdb agent is on unix, it won't map paths to filenames correctly
				if (IsWindows) {
					for (int i = 0; i < sourceFiles.Length; i++) {
						string s = sourceFiles[i];
						if (s != null && !s.StartsWith ("/"))
							sourceFiles[i] = System.IO.Path.GetFileName (s);
					}
				}
			}
			
			for (int n=0; n<sourceFiles.Length; n++)
				sourceFiles[n] = NormalizePath (sourceFiles[n]);
			
			foreach (string s in sourceFiles) {
				List<TypeMirror> typesList;
				
				if (source_to_type.TryGetValue (s, out typesList)) {
					typesList.Add (t);
				} else {
					typesList = new List<TypeMirror> ();
					typesList.Add (t);
					source_to_type[s] = typesList;
				}
				
				foreach (var bi in pending_bes.Where (b => b.BreakEvent is Breakpoint)) {
					var bp = (Breakpoint) bi.BreakEvent;
					if (PathComparer.Compare (PathToFileName (bp.FileName), s) == 0) {
						bool inisideLoadedRange;
						Location l = GetLocFromType (t, s, bp.Line, out inisideLoadedRange);
						if (l != null) {
							OnDebuggerOutput (false, string.Format ("Resolved pending breakpoint at '{0}:{1}' to {2} [0x{3:x5}].\n",
							                                        s, l.LineNumber, l.Method.FullName, l.ILOffset));
							ResolvePendingBreakpoint (bi, l);
							resolved.Add (bi);
						} else {
							if (inisideLoadedRange) {
								bi.SetStatus (BreakEventStatus.Invalid, null);
							}
						}
					}
				}
				
				foreach (var be in resolved)
					pending_bes.Remove (be);
				resolved.Clear ();
			}
			
			//handle pending catchpoints
			
			foreach (var bi in pending_bes.Where (b => b.BreakEvent is Catchpoint)) {
				var cp = (Catchpoint) bi.BreakEvent;
				if (cp.ExceptionName == typeName) {
					ResolvePendingCatchpoint (bi, t);
					resolved.Add (bi);
				}
			}
			foreach (var be in resolved)
				pending_bes.Remove (be);
		}
		
		internal static string NormalizePath (string path)
		{
			if (!IsWindows && path.StartsWith ("\\"))
				return path.Replace ('\\','/');
			else
				return path;
		}
		
		string PathToFileName (string path)
		{
			if (useFullPaths)
				return path;
			return System.IO.Path.GetFileName (path);
		}
		
		bool PathsAreEqual (string p1, string p2)
		{
			return PathComparer.Compare (p1, p2) == 0;
		}
		
		Location GetLocFromType (TypeMirror type, string file, int line, out bool insideTypeRange)
		{
			Location target_loc = null;
			insideTypeRange = false;
			
			foreach (MethodMirror m in type.GetMethods ())
			{
				int rangeFirstLine = -1;
				int rangeLastLine = -1;
				
				foreach (Location l in m.Locations) {
					if (PathComparer.Compare (PathToFileName (NormalizePath (l.SourceFile)), file) == 0) {
						// If we are inserting a breakpoint in line L, but L+1 has the same IL offset as L,
						// pick the L+1 location, since that's where the debugger is going to stop.
						if (l.LineNumber == line) {
							if (target_loc == null)
								target_loc = l;
						}
						else if (target_loc != null) {
							if (target_loc.ILOffset == l.ILOffset)
								target_loc = l;
							else
								break;
						}
						rangeLastLine = l.LineNumber;
						if (rangeFirstLine == -1)
							rangeFirstLine = l.LineNumber;
					} else {
						if (rangeFirstLine != -1 && line >= rangeFirstLine && line <= rangeLastLine)
							insideTypeRange = true;
						rangeFirstLine = -1;
					}
				}
				if (target_loc != null)
					break;
				if (rangeFirstLine != -1 && line >= rangeFirstLine && line <= rangeLastLine)
					insideTypeRange = true;
			}
	
			return target_loc;
		}

		void ResolvePendingBreakpoint (BreakInfo bi, Location l)
		{
			bi.Location = l;
			InsertBreakpoint ((Breakpoint) bi.BreakEvent, bi);
			bi.SetStatus (BreakEventStatus.Bound, null);
		}
				
		void ResolvePendingCatchpoint (BreakInfo bi, TypeMirror type)
		{
			InsertCatchpoint ((Catchpoint) bi.BreakEvent, bi, type);
			bi.SetStatus (BreakEventStatus.Bound, null);
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
			Step (StepDepth.Into, StepSize.Min);
		}

		protected override void OnStepLine ()
		{
			Step (StepDepth.Into, StepSize.Line);
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
			
			if (ThreadIsAlive (recent_thread) && HasUserFrame (GetId (recent_thread), infos))
				return;

			var threads = vm.GetThreads ();
			foreach (var thread in threads) {
				if (ThreadIsAlive (thread) && HasUserFrame (GetId (thread), infos)) {
					recent_thread = thread;
					return;
				}
			}
			recent_thread = threads[0];	
		}
		
		long GetId (ThreadMirror thread)
		{
			long id;
			if (!localThreadIds.TryGetValue (thread.ThreadId, out id)) {
				id = localThreadIds.Count + 1;
				localThreadIds [thread.ThreadId] = id;
			}
			return id;
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
		
		public bool IsExternalCode (Mono.Debugger.Soft.StackFrame frame)
		{
			return frame.Method == null || string.IsNullOrEmpty (frame.FileName)
				|| (assemblyFilters != null && !assemblyFilters.Contains (frame.Method.DeclaringType.Assembly));
		}
		
		public bool IsExternalCode (TypeMirror type)
		{
			return assemblyFilters != null && !assemblyFilters.Contains (type.Assembly);
		}
		
		protected override AssemblyLine[] OnDisassembleFile (string file)
		{
			List<TypeMirror> types;
			if (!source_to_type.TryGetValue (file, out types))
				return new AssemblyLine [0];
			
			var lines = new List<AssemblyLine> ();
			foreach (TypeMirror type in types) {
				foreach (MethodMirror met in type.GetMethods ()) {
					if (!PathsAreEqual (NormalizePath (met.SourceFile), file))
						continue;
					var body = met.GetMethodBody ();
					int lastLine = -1;
					int firstPos = lines.Count;
					string addrSpace = met.FullName;
					foreach (var ins in body.Instructions) {
						Location loc = met.LocationAtILOffset (ins.Offset);
						if (loc != null && lastLine == -1) {
							lastLine = loc.LineNumber;
							for (int n=firstPos; n<lines.Count; n++) {
								AssemblyLine old = lines [n];
								lines [n] = new AssemblyLine (old.Address, old.AddressSpace, old.Code, loc.LineNumber);
							}
						}
						lines.Add (new AssemblyLine (ins.Offset, addrSpace, Disassemble (ins), loc != null ? loc.LineNumber : lastLine));
					}
				}
			}
			lines.Sort (delegate (AssemblyLine a1, AssemblyLine a2) {
				int res = a1.SourceLine.CompareTo (a2.SourceLine);
				if (res != 0)
					return res;
				else
					return a1.Address.CompareTo (a2.Address);
			});
			return lines.ToArray ();
		}
		
		public AssemblyLine[] Disassemble (Mono.Debugger.Soft.StackFrame frame, int firstLine, int count)
		{
			MethodBodyMirror body = frame.Method.GetMethodBody ();
			var instructions = body.Instructions;
			ILInstruction current = null;
			foreach (var ins in instructions) {
				if (ins.Offset >= frame.ILOffset) {
					current = ins;
					break;
				}
			}
			if (current == null)
				return new AssemblyLine [0];
			
			var result = new List<AssemblyLine> ();
			
			int pos = firstLine;
			
			while (firstLine < 0 && count > 0) {
				if (current.Previous == null) {
//					result.Add (new AssemblyLine (99999, "<" + (pos++) + ">"));
					result.Add (AssemblyLine.OutOfRange);
					count--;
					firstLine++;
				} else {
					current = current.Previous;
					firstLine++;
				}
			}
			
			while (current != null && firstLine > 0) {
				current = current.Next;
				firstLine--;
			}
			
			while (count > 0) {
				if (current != null) {
					Location loc = frame.Method.LocationAtILOffset (current.Offset);
					result.Add (new AssemblyLine (current.Offset, frame.Method.FullName, Disassemble (current), loc != null ? loc.LineNumber : -1));
					current = current.Next;
					pos++;
				} else
					result.Add (AssemblyLine.OutOfRange);
//					result.Add (new AssemblyLine (99999, "<" + (pos++) + ">"));
				count--;
			}
			return result.ToArray ();
		}
		
		string Disassemble (ILInstruction ins)
		{
			string oper;
			if (ins.Operand is MethodMirror)
				oper = ((MethodMirror)ins.Operand).FullName;
			else if (ins.Operand is TypeMirror)
				oper = ((TypeMirror)ins.Operand).FullName;
			else if (ins.Operand is ILInstruction)
				oper = ((ILInstruction)ins.Operand).Offset.ToString ("x8");
			else if (ins.Operand == null)
				oper = string.Empty;
			else
				oper = ins.Operand.ToString ();
			
			return ins.OpCode + " " + oper;
		}
		
		readonly static bool IsWindows;
		readonly static bool IsMac;
		readonly static StringComparer PathComparer;
		
		static SoftDebuggerSession ()
		{
			IsWindows = Path.DirectorySeparatorChar == '\\';
			IsMac = !IsWindows && IsRunningOnMac();
			PathComparer = (IsWindows || IsMac)? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
		}
		
		//From Managed.Windows.Forms/XplatUI
		static bool IsRunningOnMac ()
		{
			IntPtr buf = IntPtr.Zero;
			try {
				buf = System.Runtime.InteropServices.Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = System.Runtime.InteropServices.Marshal.PtrToStringAnsi (buf);
					if (os == "Darwin")
						return true;
				}
			} catch {
			} finally {
				if (buf != IntPtr.Zero)
					System.Runtime.InteropServices.Marshal.FreeHGlobal (buf);
			}
			return false;
		}
		
		[System.Runtime.InteropServices.DllImport ("libc")]
		static extern int uname (IntPtr buf);
	}
	
	class BreakInfo: BreakEventInfo
	{
		public Location Location;
		public EventRequest Req;
		public string LastConditionValue;
	}
	
	class DisconnectedException: DebuggerException
	{
		public DisconnectedException ():
			base ("The connection with the debugger has been lost. The target application may have exited.")
		{
		}
	}
	
	class DebugSocketException: DebuggerException
	{
		public DebugSocketException (Exception ex):
			base ("Could not open port for debugger. Another process may be using the port.", ex)
		{
		}
	}
}
