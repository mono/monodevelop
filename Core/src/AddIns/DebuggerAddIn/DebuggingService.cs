// DebuggingService.cs - Debugging service frontend for MonoDebugger
//
//  Author: Mike Kestner <mkesner@ximian.com>
//
// Copyright (c) 2004 Novell, Inc.

using System;
using System.Collections;

using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns;

using MonoDevelop.Services;
using MonoDevelop.Gui;

using Mono.Debugger;
using Mono.Debugger.Languages;

/*
 * Some places we should be doing some error handling we used to toss
 * exceptions, now we error out silently, this needs a real solution.
 */

namespace MonoDevelop.Debugger
{

	public class DebuggingService : AbstractService, IDebuggingService
	{
		Process proc;
		Hashtable procs = new Hashtable ();
		Hashtable breakpoints = new Hashtable ();
		DebuggerBackend backend;
		IConsole console;
		IProgressMonitor current_monitor;
		DebugExecutionHandlerFactory executionHandlerFactory;

#if NET_2_0
		DebugAttributeHandler attr_handler;
#endif
		public DebuggingService()
		{
			executionHandlerFactory = new DebugExecutionHandlerFactory (this);
#if NET_2_0
			attr_handler = new DebugAttributeHandler();
#endif
		}

		public IExecutionHandlerFactory GetExecutionHandlerFactory ()
		{
			return executionHandlerFactory;
		}
		
		void Cleanup ()
		{
			if (!IsDebugging)
				return;

			if (StoppedEvent != null)
				StoppedEvent (this, new EventArgs ());

			backend.Dispose ();
			backend = null;
			console.Dispose ();
			console = null;
#if NET_2_0
			attr_handler = null;
#endif
			proc = null;
			
			if (ExecutionLocationChanged != null)
				ExecutionLocationChanged (this, EventArgs.Empty);
		}

		public override void UnloadService ()
		{
			Cleanup ();
			base.UnloadService ();
		}

#if NET_2_0
		public DebugAttributeHandler AttributeHandler {
			get {
				return attr_handler;
			}
		}
#endif

		public bool IsDebugging {
			get {
				return backend != null && proc != null && proc.HasTarget;
			}
		}

		public bool IsRunning {
			get {
				return IsDebugging && !proc.IsStopped;
			}
		}

		public void LoadLibrary (Process thread, string assembly)
		{
			backend.LoadLibrary (thread, assembly);
		}

		private Breakpoint CreateBreakpoint (string name)
		{
			SimpleBreakpoint point = new SimpleBreakpoint (name);
			point.BreakpointHitEvent += new Mono.Debugger.BreakpointEventHandler (OnBreakpointHit);
			return point;
		}

		public bool AddBreakpoint (string filename, int linenum)
		{
			string key = filename + ":" + linenum;
			if (breakpoints.Contains (key)) return true;
			
			BreakpointHandle brkptnum = null;
			if (IsDebugging) {
				Breakpoint point = CreateBreakpoint (key);
				SourceLocation loc = backend.FindLocation(filename, linenum);
				if (loc == null)
					return false;
				brkptnum = loc.InsertBreakpoint (proc, point);
			}
			
			BreakpointEntry entry = new BreakpointEntry (this, filename, linenum);
			entry.Handle = brkptnum;

			breakpoints.Add (key, entry);
			
			if (BreakpointAdded != null)
				BreakpointAdded (this, new BreakpointEventArgs (entry));

			return true;
		}

		public void RemoveBreakpoint (string filename, int linenum)
		{
			string key = filename + ":" + linenum;
			BreakpointEntry entry = (BreakpointEntry) breakpoints [key];
			
			if (entry != null)
				RemoveBreakpoint (entry);
		}

		void RemoveBreakpoint (BreakpointEntry entry)
		{
			if (IsDebugging)
				entry.Handle.Remove (proc);

			breakpoints.Remove (entry.FileName + ":" + entry.Line);
		
			if (BreakpointRemoved != null)
				BreakpointRemoved (this, new BreakpointEventArgs (entry));
		}

		public bool ToggleBreakpoint (string filename, int linenum)
		{
			if (!breakpoints.ContainsKey (filename + ":" + linenum))
				return AddBreakpoint (filename, linenum);
			else
				RemoveBreakpoint (filename, linenum);
			return true;
		}
		
		internal void EnableBreakpoint (BreakpointEntry entry, bool enable)
		{
			if (enable)
				entry.Handle.Enable (proc);
			else
				entry.Handle.Disable (proc);
			
			if (BreakpointChanged != null)
				BreakpointChanged (this, new BreakpointEventArgs (entry));
		}

		public IBreakpoint[] Breakpoints {
			get {
				IBreakpoint[] list = new IBreakpoint[breakpoints.Count];
				breakpoints.Values.CopyTo (list, 0);
				return list;
			}
		}
		
		public IBreakpoint[] GetBreakpointsAtFile (string sourceFile)
		{
			ArrayList list = new ArrayList ();
			foreach (IBreakpoint b in breakpoints.Values)
				if (b.FileName == sourceFile)
					list.Add (b);
			return (IBreakpoint[]) list.ToArray (typeof(IBreakpoint));
		}
		
		public void ClearAllBreakpoints ()
		{
			object[] list = new object [breakpoints.Count];
			breakpoints.Values.CopyTo (list, 0);
			foreach (BreakpointEntry b in list)
				RemoveBreakpoint (b);
		}

		private void thread_created (ThreadManager manager, Process process)
		{
			lock (procs) {
				procs.Add (process.ID, process);

				process.TargetOutput += new TargetOutputHandler (target_output);
				process.DebuggerOutput += new DebuggerOutputHandler (debugger_output);
				process.DebuggerError += new DebuggerErrorHandler (debugger_error);
			}

			EmitThreadStateEvent (null);
		}

		private void thread_exited (ThreadManager manager, Process process)
		{
			lock (procs) {
				procs.Remove (process.ID);
			}

			EmitThreadStateEvent (null);
		}

		private void initialized_event (ThreadManager manager, Process process)
		{
			this.proc = process;

			proc.TargetOutput += new TargetOutputHandler (target_output);
			proc.DebuggerOutput += new DebuggerOutputHandler (debugger_output);
			proc.DebuggerError += new DebuggerErrorHandler (debugger_error);
			proc.TargetEvent += new TargetEventHandler (target_event);

			insert_breakpoints ();

			if (StartedEvent != null)
				StartedEvent (this, EventArgs.Empty);

			// This should not be needed, but it hangs if not dispatched in this way
			// It's prolly a synchronization issue that does not show when the call
			// is delayed by the dispatcher
			Runtime.DispatchService.GuiDispatch (new StatefulMessageHandler (ChangeState), null);
		}

		void target_output (bool is_stderr, string line)
		{
			Console.WriteLine (line);
			console.Out.Write (line);
		}

		void debugger_output (string line)
		{
			Console.WriteLine (line);
			console.Out.Write (line);
		}

		void debugger_error (object sender, string message, Exception e)
		{
			Console.WriteLine (message);
			console.Error.Write (message);
			console.Error.Write (e.ToString ());
		}

		private void target_event (object sender, TargetEventArgs args)
		{
			switch (args.Type) {
			case TargetEventType.TargetExited:
			case TargetEventType.TargetSignaled:
				KillApplication (null);
				break;
			case TargetEventType.TargetStopped:
			case TargetEventType.TargetRunning:
				ChangeState (null);
				break;
			case TargetEventType.TargetHitBreakpoint:
			default:
				break;
			}
		}

		void insert_breakpoints ()
		{
			string[] keys = new string [breakpoints.Keys.Count];
			breakpoints.Keys.CopyTo (keys, 0);
			foreach (string key in keys) {
				Breakpoint point = CreateBreakpoint (key);
				string[] toks = point.Name.Split (':');
				string filename = toks [0];
				int linenumber = Int32.Parse (toks [1]);
				SourceLocation loc = backend.FindLocation(filename, linenumber);
				if (loc == null) {
					Console.WriteLine ("Couldn't find breakpoint location " + key + " " + backend.Modules.Length);
					return;
				}
				
				BreakpointHandle handle = loc.InsertBreakpoint (proc, point);
				((BreakpointEntry)breakpoints [key]).Handle = handle;
			}
		}

		void EmitThreadStateEvent (object obj)
		{
			if (ThreadStateEvent != null)
				ThreadStateEvent (this, EventArgs.Empty);
		}

		void ChangeState (object obj)
		{
			if (ThreadStateEvent != null)
				ThreadStateEvent (this, EventArgs.Empty);

			if (IsRunning) {
				if (ResumedEvent != null) {
					ResumedEvent (this, EventArgs.Empty);
				}
			} else {
				if (PausedEvent != null)
					PausedEvent (this, EventArgs.Empty);
			}
			if (ExecutionLocationChanged != null)
				ExecutionLocationChanged (this, EventArgs.Empty);
		}

		public event EventHandler PausedEvent;
		public event EventHandler ResumedEvent;
		public event EventHandler StartedEvent;
		public event EventHandler StoppedEvent;
		public event EventHandler ThreadStateEvent;
		
		public event MonoDevelop.Services.BreakpointEventHandler BreakpointAdded;
		public event MonoDevelop.Services.BreakpointEventHandler BreakpointRemoved;
		public event MonoDevelop.Services.BreakpointEventHandler BreakpointChanged;
		public event EventHandler ExecutionLocationChanged;

		void KillApplication (object obj)
		{
			Cleanup ();
		}

		public void Pause ()
		{
			if (!IsDebugging)
				//throw new Exception ("Debugger not running.");
				return;

			if (proc.IsStopped)
				return;

			proc.Stop ();
		}

		public void Resume ()
		{
			if (!IsDebugging)
				//throw new Exception ("Debugger not running.");
				return;

			if (!proc.IsStopped)
				return;

			proc.Continue (false);
		}

		public void Run (IConsole console, string[] argv)
		{
			if (IsDebugging)
				return;

#if NET_2_0
			AttributeHandler.Rescan();
#endif
			this.console = console;

			backend = new DebuggerBackend ();
			backend.ThreadManager.InitializedEvent += new ThreadEventHandler (initialized_event);
			backend.ThreadManager.ThreadCreatedEvent += new ThreadEventHandler (thread_created);
			backend.ThreadManager.ThreadExitedEvent += new ThreadEventHandler (thread_exited);
			backend.Run (new ProcessStart (null, argv));
			
			console.CancelRequested += new EventHandler (OnCancelRequested);
		}
		
		void OnCancelRequested (object sender, EventArgs args)
		{
			Stop ();
		}

		public void Stop ()
		{
			Cleanup ();
		}

		public void StepInto ()
		{
			if (!IsDebugging)
				//throw new Exception ("Can't step without running debugger.");
				return;

			if (IsRunning)
				//throw new Exception ("Can't step unless paused.");
				return;

			proc.StepLine (false);
		}

		public void StepOver ()
		{
			if (!IsDebugging)
				//throw new Exception ("Can't step without running debugger.");
				return;

			if (IsRunning)
				//throw new Exception ("Can't step unless paused.");
				return;

			proc.NextLine (false);
		}

		public void StepOut ()
		{
			if (!IsDebugging)
				return;

			if (IsRunning)
				return;

			proc.Finish (false);
		}

		public string[] Backtrace {
			get {
				Backtrace trace = proc.GetBacktrace ();
				string[] result = new string [trace.Frames.Length];
				int i = 0;
				foreach (StackFrame frame in trace.Frames)
					result [i++] = frame.SourceAddress.Name;

				return result;
			}
		}
		
		public StackFrame[] GetStack ()
		{
			return proc.GetBacktrace ().Frames;
		}

#if NET_2_0
		public Process MainThread {
			get {
				return proc;
			}
		}
#endif

		public Process[] Threads {
			get {
				Process[] retval = new Process [procs.Count];
				procs.Values.CopyTo (retval, 0);
				return retval;
			}
		}

		public StackFrame CurrentFrame {
			get {
				if (IsRunning)
					return null;
				return proc.CurrentFrame;
			}
		}

		public string CurrentFilename {
			get {
				if (IsRunning || proc == null)
					return String.Empty;

				StackFrame frame = GetCurrentSourceFrame ();
				
				if (frame == null)
					return String.Empty;

				return frame.SourceAddress.MethodSource.SourceFile.FileName;
			}
		}

		public int CurrentLineNumber {
			get {
				if (IsRunning || proc == null)
					return -1;

				StackFrame frame = GetCurrentSourceFrame ();
				if (frame == null)
					return -1;

				return frame.SourceAddress.Row;
			}
		}
		
		StackFrame GetCurrentSourceFrame ()
		{
			if (proc.CurrentFrame.SourceAddress != null /* there's no source for this frame */
				  && !proc.CurrentFrame.SourceAddress.MethodSource.IsDynamic)
				return proc.CurrentFrame;
			
			foreach (StackFrame frame in GetStack ()) {
				if (frame.SourceAddress != null && !frame.SourceAddress.MethodSource.IsDynamic)
					return frame;
			}
			return null;
		}

		public string LookupValue (string expr)
		{
			return "";
		}

		private void OnBreakpointHit (Breakpoint pointFromDbg, StackFrame frame)
		{
			string[] toks = pointFromDbg.Name.Split (':');
			string filename = toks [0];
			int linenumber = Int32.Parse (toks [1]);

			if (this.BreakpointHit == null)
				return;
			
			BreakpointHitArgs args = new BreakpointHitArgs (filename, linenumber);
			if (BreakpointHit != null)
				BreakpointHit (this, args);
		}

		public event DebuggingService.BreakpointHitHandler BreakpointHit;

		public delegate void BreakpointHitHandler (object o, BreakpointHitArgs args);

		public class BreakpointHitArgs : EventArgs {

			string filename;
			int linenumber;

			public BreakpointHitArgs (string filename, int linenumber)
			{
				this.filename = filename;
				this.linenumber = linenumber;
			}

			public string Filename {
				get {
					return filename;
				}
			}

			public int LineNumber {
				get {
					return linenumber;
				}
			}
		}
	}

	class BreakpointEntry: IBreakpoint
	{
		DebuggingService service;
		BreakpointHandle handle;

		string file;
		int line;
		
		public BreakpointEntry (DebuggingService service, string file, int line)
		{
			this.service = service;
			this.file = file;
			this.line = line;
		}
		
		public string FileName {
			get { return file; }
		}
		
		public int Line {
			get { return line; }
		}
		
		public BreakpointHandle Handle {
			get { return handle; }
			set { handle = value; }
		}
		
		public bool Enabled {
			get {
				return handle != null && handle.IsEnabled;
			}
			set {
				if (handle == null) return;
				if (value == handle.IsEnabled) return;
				service.EnableBreakpoint (this, value);
			}
		}
	}
}
