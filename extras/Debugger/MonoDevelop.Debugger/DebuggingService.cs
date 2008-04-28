// DebuggingService.cs - Debugging service frontend for MonoDebugger
//
//  Author: Mike Kestner <mkesner@ximian.com>
//
// Copyright (c) 2004 Novell, Inc.

using System;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;

using MonoDevelop.Ide.Gui;
using BreakpointEventHandler = MonoDevelop.Core.Execution.BreakpointEventHandler;

using DebuggerClient;
using DebuggerLibrary;

/*
 * Some places we should be doing some error handling we used to toss
 * exceptions, now we error out silently, this needs a real solution.
 */

namespace MonoDevelop.Debugger
{

	public class DebuggingService : AbstractService, IDebuggingService
	{
		Dictionary<string, IBreakpoint> breakpoints = new Dictionary<string, IBreakpoint> ();
		
		bool firstStop = true;
		IConsole console;
		IProgressMonitor current_monitor;
		DebugExecutionHandlerFactory executionHandlerFactory;
		//DebugAttributeHandler attr_handler;
		
		DebuggerClient.Debugger debugger;
		DebuggerSession session;
		
		Backtrace current_backtrace;
		
		public DebuggingService()
		{
			executionHandlerFactory = new DebugExecutionHandlerFactory (this);
			//attr_handler = new DebugAttributeHandler();
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

//			backend.Dispose ();
//			backend = null;
			if (console != null) {
				console.Dispose ();
				console = null;
			}
			//attr_handler = null;
			
			if (ExecutionLocationChanged != null)
				ExecutionLocationChanged (this, EventArgs.Empty);
		}

		public override void UnloadService ()
		{
			Cleanup ();
			base.UnloadService ();
		}


		/*public DebugAttributeHandler AttributeHandler {
			get {
				return attr_handler;
			}
		}
*/
		public bool IsDebugging {
			get {
				//return backend != null && proc != null && proc.HasTarget;
				return true;
			}
		}

		public bool IsRunning {
			get {
				//return IsDebugging && !proc.IsStopped;
				return true;
			}
		}
/*
		public void LoadLibrary (Process thread, string assembly)
		{
			backend.LoadLibrary (thread, assembly);
		}*/

		/*
		private Breakpoint CreateBreakpoint (string name)
		{
			SimpleBreakpoint point = new SimpleBreakpoint (name);
			point.BreakpointHitEvent += new Mono.Debugger.BreakpointEventHandler (OnBreakpointHit);
			return point;
		}
*/
		public bool AddBreakpoint (string filename, int linenum)
		{
				Console.WriteLine("AddBreakpoint");
			return false;
//			bool paused = false;
//			try {
//				if (IsRunning) {
//					paused = true;
//					Pause ();
//				}
//				
//				string key = filename + ":" + linenum;
//				if (breakpoints.Contains (key)) return true;
//				
//				BreakpointHandle brkptnum = null;
//				if (IsDebugging) {
//					Breakpoint point = CreateBreakpoint (key);
//					SourceLocation loc = backend.FindLocation(filename, linenum);
//					if (loc == null)
//						return false;
//					brkptnum = loc.InsertBreakpoint (proc, point);
//				}
//				
//				BreakpointEntry entry = new BreakpointEntry (this, filename, linenum);
//				entry.Handle = brkptnum;
//	
//				breakpoints.Add (key, entry);
//				
//				if (BreakpointAdded != null)
//					BreakpointAdded (this, new BreakpointEventArgs (entry));
//	
//				return true;
//			} finally {
//				if (paused)
//					Resume ();
//			}
		}

		public void RemoveBreakpoint (string filename, int linenum)
		{
//			string key = filename + ":" + linenum;
//			BreakpointEntry entry = (BreakpointEntry) breakpoints [key];
//			
//			if (entry != null)
//				RemoveBreakpoint (entry);
			Console.WriteLine("RemoveBreakpoint");
		}
/*
		void RemoveBreakpoint (BreakpointEntry entry)
		{
			if (IsDebugging && entry.Handle != null)
				entry.Handle.Remove (proc);

			breakpoints.Remove (entry.FileName + ":" + entry.Line);
		
			if (BreakpointRemoved != null)
				BreakpointRemoved (this, new BreakpointEventArgs (entry));
		}
*/
		public bool ToggleBreakpoint (string filename, int linenum)
		{
//			if (!breakpoints.ContainsKey (filename + ":" + linenum))
//				return AddBreakpoint (filename, linenum);
//			else
//				RemoveBreakpoint (filename, linenum);
//			return true;
			Console.WriteLine("ToggleBreakpoint");
			return false;
		}
		/*
		internal void EnableBreakpoint (BreakpointEntry entry, bool enable)
		{
			if (enable)
				entry.Handle.Enable (proc);
			else
				entry.Handle.Disable (proc);
			
			if (BreakpointChanged != null)
				BreakpointChanged (this, new BreakpointEventArgs (entry));
		}
*/
		public IBreakpoint[] Breakpoints {
			get {
				IBreakpoint[] list = new IBreakpoint[breakpoints.Count];
				breakpoints.Values.CopyTo (list, 0);
				return list;
			}
		}
		
		
		public IBreakpoint[] GetBreakpointsAtFile (string sourceFile)
		{
		/*	ArrayList list = new ArrayList ();
			foreach (IBreakpoint b in breakpoints.Values)
				if (b.FileName == sourceFile)
					list.Add (b);
			return (IBreakpoint[]) list.ToArray (typeof(IBreakpoint));*/
			Console.WriteLine ("GetBreakpointsAtFile");
			return null;
		}
		
		public void ClearAllBreakpoints ()
		{
			/*object[] list = new object [breakpoints.Count];
			breakpoints.Values.CopyTo (list, 0);
			foreach (BreakpointEntry b in list)
				RemoveBreakpoint (b);*/
			Console.WriteLine ("ClearAllBreakpoints");
		}
/*
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
			
			Console.WriteLine ("initialized_event");
			try {

			proc.TargetOutput += new TargetOutputHandler (target_output);
			proc.DebuggerOutput += new DebuggerOutputHandler (debugger_output);
			proc.DebuggerError += new DebuggerErrorHandler (debugger_error);
			proc.TargetEvent += new TargetEventHandler (target_event);

			Console.WriteLine ("p0");
			//insert_breakpoints ();

			Console.WriteLine ("p1");
			
			if (StartedEvent != null)
				StartedEvent (this, EventArgs.Empty);

			// This should not be needed, but it hangs if not dispatched in this way
			// It's prolly a synchronization issue that does not show when the call
			// is delayed by the dispatcher
			Console.WriteLine ("p2");
			DispatchService.GuiDispatch (new StatefulMessageHandler (ChangeState), null);
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
				throw;
			}
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
				Console.WriteLine ("b4 " + key);
				Breakpoint point = CreateBreakpoint (key);
				string[] toks = point.Name.Split (':');
				string filename = toks [0];
				int linenumber = Int32.Parse (toks [1]);
				SourceLocation loc = backend.FindLocation(filename, linenumber);
				if (loc == null) {
					Console.WriteLine ("Couldn't find breakpoint location " + key + " " + backend.Modules.Length);
					return;
				}
				
				try {
					ArrayList list = null;
					object o = list.Count;
				} catch {}
				
				BreakpointHandle handle = loc.InsertBreakpoint (proc, point);
				((BreakpointEntry)breakpoints [key]).Handle = handle;
			}
		}

		void EmitThreadStateEvent (object obj)
		{
			if (ThreadStateEvent != null)
				ThreadStateEvent (this, EventArgs.Empty);
		}
*/
		void ChangeState (object obj)
		{
			Console.WriteLine ("ChangeState");
			if (ThreadStateEvent != null)
				ThreadStateEvent (this, EventArgs.Empty);

			/*if (IsRunning) {
				if (ResumedEvent != null) {
					ResumedEvent (this, EventArgs.Empty);
				}
			} else {
				if (firstStop) {
					insert_breakpoints ();
					firstStop = false;
					Resume ();
				} else*/
					if (PausedEvent != null)
				PausedEvent (this, EventArgs.Empty);
			/*}
			if (ExecutionLocationChanged != null)
				ExecutionLocationChanged (this, EventArgs.Empty);*/
		}

		public event EventHandler PausedEvent;
		public event EventHandler ResumedEvent;
		public event EventHandler StartedEvent;
		public event EventHandler StoppedEvent;
		public event EventHandler ThreadStateEvent;
		
		public event BreakpointEventHandler BreakpointAdded;
		public event BreakpointEventHandler BreakpointRemoved;
		public event BreakpointEventHandler BreakpointChanged;
		public event EventHandler ExecutionLocationChanged;

		void KillApplication (object obj)
		{
			Cleanup ();
		}

		public void Pause ()
		{
//			if (!IsDebugging)
//				//throw new Exception ("Debugger not running.");
//				return;
//
//			if (proc.IsStopped)
//				return;
//
//			proc.Stop ();
			Console.WriteLine ("Pause");
		}

		public void Resume ()
		{
//			if (!IsDebugging)
//				//throw new Exception ("Debugger not running.");
//				return;
//
//			if (!proc.IsStopped)
//				return;
//
//			proc.Continue (false);
			Console.WriteLine("Resume");
		}

		public void Run (IConsole console, string[] argv)
		{
			//if (IsDebugging)
			//	return;
				
			firstStop = true;

#if NET_2_0
			AttributeHandler.Rescan();
#endif
			this.console = console;
			
			if (debugger == null)
				debugger = new DebuggerClient.Debugger ();

			Console.WriteLine ("Starting session");
			session = debugger.StartSession (argv);
			session.TargetEvent += OnTargetEvent;

//			backend = new DebuggerBackend ();
//			backend.ThreadManager.MainThreadCreatedEvent += new ThreadEventHandler (initialized_event);
//			backend.ThreadManager.ThreadCreatedEvent += new ThreadEventHandler (thread_created);
//			backend.ThreadManager.ThreadExitedEvent += new ThreadEventHandler (thread_exited);
//			backend.Run (new ProcessStart (null, argv));
			
			console.CancelRequested += new EventHandler (OnCancelRequested);
		}
		
		void OnTargetEvent (TargetEventArgs args)
		{
			try {
				Console.WriteLine ("OnTargetEvent, type - {0}", args.Type);
				if (args.Type == TargetEventType.TargetExited)
					return;

				current_backtrace = args.Backtrace;
				for (int i = 0; i < args.Backtrace.FrameCount; i ++) {
					StackFrame frame = args.Backtrace.GetFrame (i);
					Console.WriteLine ("addr - 0x{0:X}, file: {1}, method : {2}, line : {3}", frame.Address, frame.SourceLocation.Filename, frame.SourceLocation.Method, frame.SourceLocation.Line);
				}
				
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
			} catch (Exception e) {
				Console.WriteLine ("OnTargetEvent, {0}", e.ToString ());
			}

		}
		
		void OnCancelRequested (object sender, EventArgs args)
		{
			Stop ();
		}

		public void Stop ()
		{
			session.Stop ();
			Cleanup ();
		}

		public void StepInto ()
		{
			/*if (!IsDebugging)
				//throw new Exception ("Can't step without running debugger.");
				return;

			if (IsRunning)
				//throw new Exception ("Can't step unless paused.");
				return;

			proc.StepLine (false);*/
			Console.WriteLine ("StepInto");
			session.StepLine ();
		}

		public void StepOver ()
		{
		/*	if (!IsDebugging)
				//throw new Exception ("Can't step without running debugger.");
				return;

			if (IsRunning)
				//throw new Exception ("Can't step unless paused.");
				return;

			proc.NextLine (false);*/
			Console.WriteLine ("StepOver");
			session.NextLine ();
		}

		public void StepOut ()
		{
		/*	if (!IsDebugging)
				return;

			if (IsRunning)
				return;

			proc.Finish (false);*/
			Console.WriteLine ("StepOut");
			session.Finish ();
		}

		public string[] Backtrace {
			get {
				//FIXME: == null
				string [] result = new string [current_backtrace.FrameCount];
				for (int i = 0; i < current_backtrace.FrameCount; i ++)
					result [i] = current_backtrace.GetFrame (i).ToString ();
				
				return result;
			}
		}

/*		public StackFrame[] GetStack ()
		{
			return proc.GetBacktrace ().Frames;
		}
				
		

		public Process MainThread {
			get {
				return proc;
			}
		}

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
		}*/
		
		public Backtrace CurrentBacktrace {
			get { return current_backtrace; }
		}

		public string CurrentFilename {
			get {
				/*if (IsRunning || proc == null)
					return String.Empty;

				StackFrame frame = GetCurrentSourceFrame ();
				
				if (frame == null)
					return String.Empty;

				return frame.SourceAddress.MethodSource.SourceFile.FileName;*/
				return "Dummy filename";
			}
		}

		public int CurrentLineNumber {
			get {
				/*if (IsRunning || proc == null)
					return -1;

				StackFrame frame = GetCurrentSourceFrame ();
				if (frame == null)
					return -1;

				return frame.SourceAddress.Row;*/
				return -1;
			}
		}
/*		
		StackFrame GetCurrentSourceFrame ()
		{
			if (proc.CurrentFrame.SourceAddress != null /* there's no source for this frame *
				  && !proc.CurrentFrame.SourceAddress.MethodSource.IsDynamic)
				return proc.CurrentFrame;
			
			foreach (StackFrame frame in GetStack ()) {
				if (frame.SourceAddress != null && !frame.SourceAddress.MethodSource.IsDynamic)
					return frame;
			}
			return null;
		}
*/
		public string LookupValue (string expr)
		{
			return "";
		}

/*	
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
*/
	/*	public class BreakpointHitArgs : EventArgs {

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
		}*/
	}

	/*class BreakpointEntry: IBreakpoint
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
	}*/
}
