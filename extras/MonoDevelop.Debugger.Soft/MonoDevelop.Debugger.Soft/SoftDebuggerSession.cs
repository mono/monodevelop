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
		List<Breakpoint> pending_bps = new List<Breakpoint> ();
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
			string[] vmargs = new string[startInfo.Arguments.Length + 1];
			vmargs[0] = startInfo.Command;
			Array.Copy (startInfo.Arguments.Split (' '), 0, vmargs, 1, startInfo.Arguments.Length);
			
			LaunchOptions options = new LaunchOptions ();
			options.RedirectStandardOutput = true;
			
			vm = VirtualMachineManager.Launch (vmargs, options);
			vm.EnableEvents (EventType.AssemblyLoad, EventType.TypeLoad, EventType.ThreadStart, EventType.ThreadDeath);

			outputReader = new Thread (delegate () {
				ReadOutput (vm.Process.StandardOutput, false);
			});
			outputReader.IsBackground = true;
			outputReader.Start ();
			
			errorReader = new Thread (delegate () {
				ReadOutput (vm.Process.StandardOutput, true);
			});
			errorReader.IsBackground = true;
			errorReader.Start ();
			
			OnStarted ();
			started = true;
			
			/* Wait for the VMStart event */
			vm.GetNextEvent ();
			
			InitEventHandler ();
			
			OnResumed ();
			vm.Resume ();
		}

		protected void InitEventHandler ()
		{
			eventHandler = new Thread (EventHandler);
			eventHandler.Start ();
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
			protected set { vm = value; }
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
			vm.Exit (0);
			exited = true;
		}

		protected override void OnFinish ()
		{
			var req = vm.CreateStepRequest (current_thread);
			req.Depth = StepDepth.Out;
			req.Size = StepSize.Line;
			req.Enable ();
			OnResumed ();
			vm.Resume ();
		}

		protected override ProcessInfo[] OnGetPocesses ()
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
			Breakpoint bp = (Breakpoint) be;
			bi.Location = FindLocation (bp.FileName, bp.Line);
			bi.Enabled = activate;
			if (bi.Location != null)
				InsertBreakpoint (bp, bi);
			else
				pending_bps.Add (bp);
			return bi;
		}

		protected override void OnRemoveBreakEvent (object handle)
		{
			BreakInfo bi = (BreakInfo) handle;
			if (bi.Req != null)
				bi.Req.Disable ();
		}

		protected override void OnEnableBreakEvent (object handle, bool enable)
		{
			BreakInfo bi = (BreakInfo) handle;
			bi.Enabled = enable;
			if (bi.Req != null) {
				if (enable)
					bi.Req.Enable ();
				else
					bi.Req.Disable ();
			}
		}

		protected override object OnUpdateBreakEvent (object handle, BreakEvent be)
		{
			return handle;
		}

		void InsertBreakpoint (Breakpoint bp, BreakInfo bi)
		{
			bi.Req = vm.SetBreakpoint (bi.Location.Method, bi.Location.ILOffset);
			if (!bi.Enabled)
				bi.Req.Disable ();
			else
				bi.Req.Enable ();
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
			req.Enable ();
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
				OnDebuggerOutput (false, "Loaded assembly: " + ae.Assembly.Location);
			}
			
			if (e is TypeLoadEvent) {
				TypeLoadEvent te = (TypeLoadEvent)e;
				types [te.Type.FullName] = te.Type;
				
				/* Handle pending breakpoints */
				foreach (string s in te.Type.GetSourceFiles ()) {
					List<TypeMirror> typesList;
					
					if (source_to_type.TryGetValue (s, out typesList)) {
						typesList.Add (te.Type);
					} else {
						typesList = new List<TypeMirror> ();
						typesList.Add (te.Type);
						source_to_type[s] = typesList;
					}
					
					List<Breakpoint> resolved = new List<Breakpoint> ();
					foreach (Breakpoint bp in pending_bps) {
						if (System.IO.Path.GetFileName (bp.FileName) == s) {
							Location l = GetLocFromType (te.Type, s, bp.Line);
							if (l != null) {
								OnDebuggerOutput (false, "Resolved pending breakpoint at '" + s + ":" + bp.Line + "' to " + l.Method.FullName + ":" + l.ILOffset + ".");
								ResolvePendingBreakpoint (bp, l);
								resolved.Add (bp);
							}
						}
					}
					
					foreach (Breakpoint bp in resolved)
						pending_bps.Remove (bp);
				}
			}
			
			if (e is BreakpointEvent) {
				etype = TargetEventType.TargetHitBreakpoint;
				resume = false;
			}
			
			if (e is StepEvent) {
				StepEventRequest req = (StepEventRequest)e.Request;
				req.Disable ();
				etype = TargetEventType.TargetStopped;
				resume = false;
			}
			
			if (e is ThreadStartEvent) {
				ThreadStartEvent ts = (ThreadStartEvent)e;
				OnDebuggerOutput (false, "Thread started: " + ts.Thread.Name);
			}
			
			if (resume)
				vm.Resume ();
			else {
				current_thread = e.Thread;
				TargetEventArgs args = new TargetEventArgs (etype);
				args.Process = OnGetPocesses () [0];
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
			req.Enable ();
			OnResumed ();
			vm.Resume ();
		}

		protected override void OnStop ()
		{
			vm.Suspend ();
		}
		
		BreakInfo GetBreakInfo (BreakEvent bp)
		{
			object bi;
			if (GetBreakpointHandle (bp, out bi))
				return (BreakInfo) bi;
			else
				return null;
		}
	}
	
	class BreakInfo
	{
		public bool Enabled { get; set; }
		public Location Location { get; set; }
		public BreakpointEventRequest Req { get; set; }
	}
}
