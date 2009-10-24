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
		
		Thread outputReader;
		Thread errorReader;
		
		public NRefactoryEvaluator Evaluator = new NRefactoryEvaluator ();
		public SoftDebuggerAdaptor Adaptor = new SoftDebuggerAdaptor ();
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			bool startsSuspended;
			vm = LaunchVirtualMachine (startInfo, out startsSuspended);
			
			if (vm == null) {
				MarkAsExited ();
				return;
			}
			
			vm.EnableEvents (EventType.AssemblyLoad, EventType.TypeLoad, EventType.ThreadStart, EventType.ThreadDeath);
			
			OnStarted ();
			started = true;
			
			/* Wait for the VMStart event */
			HandleEvent (vm.GetNextEvent ());
			
			eventHandler = new Thread (EventHandler);
			eventHandler.Start ();
			
			//FIXME: why is this necessary when we do Launch but explodes with Listen? 
			if (startsSuspended) {
				OnResumed ();
				vm.Resume ();
			}
		}
		
		protected void MarkAsExited ()
		{
			if (!exited) {
				exited = true;
				OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
			}
		}
		
		protected virtual VirtualMachine LaunchVirtualMachine (DebuggerStartInfo startInfo, out bool startsSuspended)
		{
			startsSuspended = true;
			
			string[] vmargs = new string[startInfo.Arguments.Length + 1];
			vmargs[0] = startInfo.Command;
			Array.Copy (startInfo.Arguments.Split (' '), 0, vmargs, 1, startInfo.Arguments.Length);
			
			LaunchOptions options = new LaunchOptions ();
			options.RedirectStandardOutput = true;
			
			var vm = VirtualMachineManager.Launch (vmargs, options);
			
			ConnectOutput (vm.StandardOutput, false);
			ConnectOutput (vm.StandardError, true);
			
			return vm;
		}

		protected void ConnectOutput (System.IO.StreamReader reader, bool error)
		{
			Thread t = (error ? outputReader : errorReader);
			if (t != null)
				return;
			t = new Thread (delegate () {
				ReadOutput (reader, true);
			});
			t.IsBackground = true;
			t.Start ();
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

		protected void OnResumed ()
		{
			current_threads = null;
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
			if (!exited) {
				vm.Exit (0);
				exited = true;
			}
		}

		protected override void OnAttachToProcess (long processId)
		{
			throw new System.NotImplementedException ();
		}

		protected override void OnContinue ()
		{
			OnResumed ();
			vm.Resume ();
		}

		protected override void OnDetach ()
		{
			throw new System.NotImplementedException ();
		}

		protected override void OnExit ()
		{
			if (vm != null)
				vm.Exit (0);
			exited = true;
		}

		protected override void OnFinish ()
		{
			var req = vm.CreateStepRequest (current_thread);
			req.Depth = StepDepth.Out;
			req.Size = StepSize.Line;
			req.Enabled = true;
			OnResumed ();
			vm.Resume ();
		}

		protected override ProcessInfo[] OnGetProcesses ()
		{
			if (procs == null)
				procs = new ProcessInfo[] { new ProcessInfo (vm.Process.Id, vm.Process.ProcessName) };
			return procs;
		}

		protected override Backtrace OnGetThreadBacktrace (long processId, long threadId)
		{
			return GetThreadBacktrace (GetThread (processId, threadId));
		}
		
		Backtrace GetThreadBacktrace (ThreadMirror thread)
		{
			MDB.StackFrame[] frames = thread.GetFrames ();
			return new Backtrace (new SoftDebuggerBacktrace (this, frames));
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
		
		ThreadInfo GetThread (ThreadMirror thread)
		{
			if (current_threads == null)
				return null;
			foreach (ThreadInfo t in current_threads)
				if (t.Id == thread.Id)
					return t;
			return null;
		}

		protected override object OnInsertBreakEvent (BreakEvent be, bool activate)
		{
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
			BreakInfo bi = (BreakInfo) handle;
			if (bi.Req != null)
				bi.Req.Enabled = false;
		}

		protected override void OnEnableBreakEvent (object handle, bool enable)
		{
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
			bi.Req.Enabled = bi.Enabled;
			request.Count = cp.HitCount;
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
			var req = vm.CreateStepRequest (current_thread);
			req.Depth = StepDepth.Over;
			req.Size = StepSize.Line;
			req.Enabled = true;
			OnResumed ();
			vm.Resume ();
		}

		void EventHandler ()
		{
			while (true) {
				Event e = vm.GetNextEvent ();
				
				bool disconnected = false;
				
				try {
					HandleEvent (e);
				} catch (VMDisconnectedException) {
					disconnected = true;
				}
				
				if (e is VMDeathEvent || e is VMDisconnectEvent || disconnected)
					break;
			}
			
			exited = true;
			OnTargetEvent (new TargetEventArgs (TargetEventType.TargetExited));
		}

		void HandleEvent (Event e)
		{
			bool resume = true;
			TargetEventType etype = TargetEventType.TargetStopped;
			
			if (!(e is TypeLoadEvent))
				Console.WriteLine ("pp event: " + e);
			
			if (e is AssemblyLoadEvent) {
				AssemblyLoadEvent ae = (AssemblyLoadEvent)e;
				OnDebuggerOutput (false, string.Format ("Loaded assembly: {0}\n", ae.Assembly.Location));
			}
			
			if (e is TypeLoadEvent) {
				TypeLoadEvent te = (TypeLoadEvent)e;
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
				args.Thread = GetThread (current_thread);
				args.Backtrace = GetThreadBacktrace (current_thread);
				OnTargetEvent (args);
			}
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
			var req = vm.CreateStepRequest (current_thread);
			req.Depth = StepDepth.Into;
			req.Size = StepSize.Line;
			req.Enabled = true;
			OnResumed ();
			vm.Resume ();
		}

		protected override void OnStop ()
		{
			vm.Suspend ();
			
			//emit a stop event at the current position of the most recent thread
			EnsureCurrentThreadIsValid ();
			OnTargetEvent (new TargetEventArgs (TargetEventType.TargetStopped) {
				Process = OnGetProcesses () [0],
				Thread = GetThread (current_thread),
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
