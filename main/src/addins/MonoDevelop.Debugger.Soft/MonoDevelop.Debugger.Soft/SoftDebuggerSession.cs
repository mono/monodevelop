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

using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Mono.Debugging.Client;
using Mono.Debugger;
using Mono.Debugging.Evaluation;
using MDB = Mono.Debugger;
using System.Net.Sockets;
using MonoDevelop.Core;
using System.IO;
using System.Reflection;

namespace MonoDevelop.Debugger.Soft
{
	public class SoftDebuggerSession : DebuggerSession
	{
		VirtualMachine vm;
		Thread eventHandler;
		Dictionary<string, List<TypeMirror>> source_to_type = new Dictionary<string, List<TypeMirror>> ();
		Dictionary<string,TypeMirror> types = new Dictionary<string, TypeMirror> ();
		List<BreakEvent> pending_bes = new List<BreakEvent> ();
		ThreadMirror first_thread;
		ThreadMirror current_thread;
		ProcessInfo[] procs;
		ThreadInfo[] current_threads;
		bool exited;
		bool started;
		internal int StackVersion;
		
		Thread outputReader;
		Thread errorReader;
		
		IAsyncResult connectionHandle;
		
		LinkedList<Event> queuedEvents = new LinkedList<Event> ();
		
		List<string> userAssemblyNames;
		List<AssemblyMirror> assemblyFilters;
		
		bool loggedSymlinkedRuntimesBug = false;
		
		public readonly NRefactoryEvaluator Evaluator = new NRefactoryEvaluator ();
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
			var runtime = Path.Combine (Path.Combine (dsi.Runtime.Prefix, "bin"), "mono");
			RegisterUserAssemblies (dsi.UserAssemblyNames);
			
			var psi = new System.Diagnostics.ProcessStartInfo (runtime) {
				Arguments = string.Format ("\"{0}\" {1}", dsi.Command, dsi.Arguments),
				WorkingDirectory = dsi.WorkingDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};
			
			foreach (var env in dsi.Runtime.EnvironmentVariables)
				psi.EnvironmentVariables[env.Key] = env.Value;
			
			foreach (var env in startInfo.EnvironmentVariables)
				psi.EnvironmentVariables[env.Key] = env.Value;
			
			OnConnecting (VirtualMachineManager.BeginLaunch (psi, HandleCallbackErrors (delegate (IAsyncResult ar) {
				OnConnected (VirtualMachineManager.EndLaunch (ar));
			}), null));
		}
		
		internal AsyncCallback HandleCallbackErrors (AsyncCallback callback)
		{
			return delegate (IAsyncResult ar) {
				connectionHandle = null;
				try {
					callback (ar);
				} catch (Exception ex) {
					MonoDevelop.Core.Gui.MessageService.ShowException (ex, "Soft debugger error: " + ex.Message);
					LoggingService.LogError ("Unhandled error launching soft debugger", ex);
					EndSession ();
				}
			};
		}
		
		/// <summary>
		/// Subclasses should pass any handles they get from the VirtualMachineManager to this
		/// so that they will be closed if the connection attempt is aborted before OnConnected is called.
		/// </summary>
		internal void OnConnecting (IAsyncResult connectionHandle)
		{
			if (this.connectionHandle != null)
				throw new InvalidOperationException ("Already connecting");
			this.connectionHandle = connectionHandle;
		}
		
		void EndLaunch ()
		{
			if (connectionHandle != null) {
				((Socket)connectionHandle.AsyncState).Close ();
				connectionHandle = null;
			}
		}
		
		protected virtual void EndSession ()
		{
			if (!exited) {
				EndLaunch ();
				exited = true;
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
		internal void OnConnected (VirtualMachine vm)
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
			
			vm.EnableEvents (EventType.AssemblyLoad, EventType.TypeLoad, EventType.ThreadStart, EventType.ThreadDeath);
			
			OnStarted ();
			started = true;
			
			/* Wait for the VMStart event */
			HandleEvent (vm.GetNextEvent ());
			
			eventHandler = new Thread (EventHandler);
			eventHandler.Start ();
		}
		
		internal void RegisterUserAssemblies (List<AssemblyName> userAssemblyNames)
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
					OnTargetOutput (true, new string (buffer, 0, c));
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
				EndLaunch ();
				ThreadPool.QueueUserWorkItem (delegate {
					try {
						vm.Exit (0);
					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
					try {
						vm.Dispose ();
					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
				});
				exited = true;
			}
			Adaptor.Dispose ();
		}

		protected override void OnAttachToProcess (long processId)
		{
			throw new System.NotImplementedException ();
		}

		protected override void OnContinue ()
		{
			Adaptor.CancelAsyncOperations ();
			OnResumed ();
			vm.Resume ();
			DequeueEventsForFirstThread ();
		}

		protected override void OnDetach ()
		{
			throw new System.NotImplementedException ();
		}

		protected override void OnExit ()
		{
			EndLaunch ();
			if (vm != null)
				vm.Exit (0);
			exited = true;
		}

		protected override void OnFinish ()
		{
			Adaptor.CancelAsyncOperations ();
			var req = vm.CreateStepRequest (current_thread);
			req.Depth = StepDepth.Out;
			req.Size = StepSize.Line;
			if (assemblyFilters != null && assemblyFilters.Count > 0)
				req.AssemblyFilter = assemblyFilters;
			req.Enabled = true;
			OnResumed ();
			vm.Resume ();
			DequeueEventsForFirstThread ();
		}

		protected override ProcessInfo[] OnGetProcesses ()
		{
			if (procs == null) {
				try {
					procs = new ProcessInfo[] { new ProcessInfo (vm.Process.Id, vm.Process.ProcessName) };
				} catch (Exception ex) {
					if (!loggedSymlinkedRuntimesBug) {
						loggedSymlinkedRuntimesBug = true;
						LoggingService.LogError ("Error getting debugger process info. Known Mono bug with symlinked runtimes.", ex);
					}
					procs = new ProcessInfo[] { new ProcessInfo (0, "mono") };
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
			
			if (be is Breakpoint) {
				Breakpoint bp = (Breakpoint) be;
				bi.Location = FindLocation (bp.FileName, bp.Line);
				if (bi.Location != null)
					InsertBreakpoint (bp, bi);
				else
					pending_bes.Add (bp);
			} else if (be is Catchpoint) {
				var cp = (Catchpoint) be;
				TypeMirror type;
				if (types.TryGetValue (cp.ExceptionName, out type)) {
					InsertCatchpoint (cp, bi, type);
				} else {
					pending_bes.Add (be);
				}
			}
			return bi;
		}

		protected override void OnRemoveBreakEvent (object handle)
		{
			if (exited)
				return;
			BreakInfo bi = (BreakInfo) handle;
			if (bi.Req != null)
				bi.Req.Enabled = false;
		}

		protected override void OnEnableBreakEvent (object handle, bool enable)
		{
			if (exited)
				return;
			BreakInfo bi = (BreakInfo) handle;
			bi.Enabled = enable;
			if (bi.Req != null) {
				bi.Req.Enabled = enable;
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
		}
		
		void InsertCatchpoint (Catchpoint cp, BreakInfo bi, TypeMirror excType)
		{
			var request = bi.Req = vm.CreateExceptionRequest (excType);
			request.Count = cp.HitCount;
			bi.Req.Enabled = bi.Enabled;
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
			Adaptor.CancelAsyncOperations ();
			var req = vm.CreateStepRequest (current_thread);
			req.Depth = StepDepth.Over;
			req.Size = StepSize.Line;
			if (assemblyFilters != null && assemblyFilters.Count > 0)
				req.AssemblyFilter = assemblyFilters;
			req.Enabled = true;
			OnResumed ();
			vm.Resume ();
			DequeueEventsForFirstThread ();
		}

		void EventHandler ()
		{
			while (true) {
				try {
					Event e = vm.GetNextEvent ();
					if (e is VMDeathEvent || e is VMDisconnectEvent)
						break;
					HandleEvent (e);
				} catch (VMDisconnectedException ex) {
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
		
		void HandleEvent  (Event e, bool dequeuing)
		{
			if (dequeuing && exited)
				return;

			bool resume = true;
			
			TargetEventType etype = TargetEventType.TargetStopped;

#if DEBUG_EVENT_QUEUEING
			if (!(e is TypeLoadEvent))
				Console.WriteLine ("pp event: " + e);
#endif
			
			if (e is AssemblyLoadEvent) {
				AssemblyLoadEvent ae = (AssemblyLoadEvent)e;
				UpdateAssemblyFilters (ae.Assembly);
				OnDebuggerOutput (false, string.Format ("Loaded assembly: {0}\n", ae.Assembly.Location));
			}
			
			if (e is TypeLoadEvent) {
				ResolveBreakpoints ((TypeLoadEvent)e);
			}
			
			if (e is BreakpointEvent) {
				etype = TargetEventType.TargetHitBreakpoint;
				resume = false;
			}
			
			if (e is ExceptionEvent) {
				etype = TargetEventType.ExceptionThrown;
				resume = false;
			}
			
			if (e is StepEvent) {
				StepEventRequest req = (StepEventRequest)e.Request;
				req.Enabled = false;
				etype = TargetEventType.TargetStopped;
				resume = false;
			}
			
			if (e is ThreadStartEvent) {
				ThreadStartEvent ts = (ThreadStartEvent)e;
				OnDebuggerOutput (false, string.Format ("Thread started: {0}\n", ts.Thread.Name));
			}
			
			if (e is VMStartEvent) {
				first_thread = e.Thread;
			}
			
			if (resume)
				vm.Resume ();
			else {
				current_thread = e.Thread;
				TargetEventArgs args = new TargetEventArgs (etype);
				args.Process = OnGetProcesses () [0];
				args.Thread = GetThread (args.Process, current_thread);
				args.Backtrace = GetThreadBacktrace (current_thread);
				OnTargetEvent (args);
			}
		}
		
		void QueueEvent (Event ev)
		{
#if DEBUG_EVENT_QUEUEING
			Console.WriteLine ("qq event: " + e);
#endif
			lock (queuedEvents) {
				queuedEvents.AddLast (ev);
			}
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
						dequeuing.Add (node.Value);
						queuedEvents.Remove (node);
					}
					node = node.Next;
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
		
		void ResolveBreakpoints (TypeLoadEvent te)
		{
			string typeName = te.Type.FullName;
			types [typeName] = te.Type;
			
			/* Handle pending breakpoints */
			
			var resolved = new List<BreakEvent> ();
			
			foreach (string s in te.Type.GetSourceFiles ()) {
				List<TypeMirror> typesList;
				
				if (source_to_type.TryGetValue (s, out typesList)) {
					typesList.Add (te.Type);
				} else {
					typesList = new List<TypeMirror> ();
					typesList.Add (te.Type);
					source_to_type[s] = typesList;
				}
				
				
				foreach (var bp in pending_bes.OfType<Breakpoint> ()) {
					if (System.IO.Path.GetFileName (bp.FileName) == s) {
						Location l = GetLocFromType (te.Type, s, bp.Line);
						if (l != null) {
							OnDebuggerOutput (false, string.Format ("Resolved pending breakpoint at '{0}:{1}' to {2}:{3}.\n", s, bp.Line, l.Method.FullName, l.ILOffset));
							ResolvePendingBreakpoint (bp, l);
							resolved.Add (bp);
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
					ResolvePendingCatchpoint (cp, te.Type);
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
			bi.Location = l;
			InsertBreakpoint (bp, bi);
		}
				
		void ResolvePendingCatchpoint (Catchpoint cp, TypeMirror type)
		{
			BreakInfo bi = GetBreakInfo (cp);
			InsertCatchpoint (cp, bi, type);
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
			Adaptor.CancelAsyncOperations ();
			var req = vm.CreateStepRequest (current_thread);
			req.Depth = StepDepth.Into;
			req.Size = StepSize.Line;
			if (assemblyFilters != null && assemblyFilters.Count > 0)
				req.AssemblyFilter = assemblyFilters;
			req.Enabled = true;
			OnResumed ();
			vm.Resume ();
			DequeueEventsForFirstThread ();
		}

		protected override void OnStop ()
		{
			vm.Suspend ();
			
			//emit a stop event at the current position of the most recent thread
			EnsureCurrentThreadIsValid ();
			var process = OnGetProcesses () [0];
			OnTargetEvent (new TargetEventArgs (TargetEventType.TargetStopped) {
				Process = process,
				Thread = GetThread (process, current_thread),
				Backtrace = GetThreadBacktrace (current_thread)});
		}
		
		void EnsureCurrentThreadIsValid ()
		{
			if (!ThreadIsAlive (current_thread)) {
				current_thread = first_thread;
				if (!ThreadIsAlive (current_thread))
					current_thread = vm.GetThreads ()[0];
			}
		}
		
		static bool ThreadIsAlive (ThreadMirror thread)
		{
			if (thread == null)
				return false;
			var state = thread.ThreadState;
			return state != ThreadState.Stopped && state != ThreadState.Aborted;
		}
		
		BreakInfo GetBreakInfo (BreakEvent be)
		{
			object bi;
			if (GetBreakpointHandle (be, out bi))
				return (BreakInfo) bi;
			else
				return null;
		}
	}
	
	class BreakInfo
	{
		public bool Enabled { get; set; }
		public Location Location { get; set; }
		public EventRequest Req { get; set; }
	}
}
