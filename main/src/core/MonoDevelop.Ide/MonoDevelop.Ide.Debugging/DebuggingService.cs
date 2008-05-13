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

using Mono.Debugging.Client;

/*
 * Some places we should be doing some error handling we used to toss
 * exceptions, now we error out silently, this needs a real solution.
 */

namespace MonoDevelop.Ide.Debugging
{

	public class DebuggingService
	{
		BreakpointStore breakpoints = new BreakpointStore ();
		
		IConsole console;
		DebugExecutionHandlerFactory executionHandlerFactory;
		
		DebuggerSession session;
		Backtrace current_backtrace;

		internal DebuggingService()
		{
			executionHandlerFactory = new DebugExecutionHandlerFactory ();
		}

		public IExecutionHandlerFactory GetExecutionHandlerFactory ()
		{
			return executionHandlerFactory;
		}
		
		public DebuggerSession DebuggerSession {
			get { return session; }
		}
		
		public BreakpointStore Breakpoints {
			get { return breakpoints; }
		}
		
		void Cleanup ()
		{
			if (!IsDebugging)
				return;

			if (StoppedEvent != null)
				StoppedEvent (null, new EventArgs ());

			session = null;
			
			if (console != null) {
				console.Dispose ();
				console = null;
			}
			
			NotifyLocationChanged ();
		}

		public bool IsDebugging {
			get {
				return session != null;
			}
		}

		public bool IsRunning {
			get {
				return IsDebugging && session.IsRunning;
			}
		}

		void target_output (bool is_stderr, string line)
		{
			Console.WriteLine (line);
			console.Out.Write (line);
		}

		void NotifyPaused ()
		{
			if (PausedEvent != null)
				PausedEvent (null, EventArgs.Empty);
			NotifyLocationChanged ();
			
			Gtk.Application.Invoke (delegate {
				if (!string.IsNullOrEmpty (CurrentFilename))
					IdeApp.Workbench.OpenDocument (CurrentFilename, CurrentLineNumber, 1, true);
			});
		}
		
		void NotifyLocationChanged ()
		{
			if (ExecutionLocationChanged != null)
				ExecutionLocationChanged (null, EventArgs.Empty);
		}

		public event EventHandler PausedEvent;
		public event EventHandler ResumedEvent;
		public event EventHandler StartedEvent;
		public event EventHandler StoppedEvent;
		public event EventHandler ExecutionLocationChanged;

		void KillApplication (object obj)
		{
			Cleanup ();
		}

		public void Pause ()
		{
			session.Stop ();
		}

		public void Resume ()
		{
			session.Continue ();
			NotifyLocationChanged ();
		}

		public IProcessAsyncOperation Run (string file, IConsole console)
		{
			DebugExecutionHandler h = new DebugExecutionHandler (null);
			return h.Execute (file, null, null, null, console);
		}
		
		internal void InternalRun (string platform, DebuggerStartInfo startInfo, IConsole console)
		{
			this.console = console;
			
			if (platform != null)
				session = DebuggerEngine.CreateDebugSessionForPlatform (platform);
			else
				session = DebuggerEngine.CreateDebugSessionForFile (startInfo.Command);
			
			session.Breakpoints = breakpoints;
			session.Run (startInfo);
			session.TargetEvent += OnTargetEvent;

			console.CancelRequested += new EventHandler (OnCancelRequested);
			NotifyLocationChanged ();
		}
		
		void OnTargetEvent (object sender, TargetEventArgs args)
		{
			try {
				Console.WriteLine ("OnTargetEvent, type - {0}", args.Type);
				if (args.Type != TargetEventType.TargetExited) {
					current_backtrace = args.Backtrace;
					for (int i = 0; i < args.Backtrace.FrameCount; i ++) {
						StackFrame frame = args.Backtrace.GetFrame (i);
						Console.WriteLine ("addr - 0x{0:X}, file: {1}, method : {2}, line : {3}", frame.Address, frame.SourceLocation.Filename, frame.SourceLocation.Method, frame.SourceLocation.Line);
					}
				}
				
				switch (args.Type) {
					case TargetEventType.TargetExited:
					case TargetEventType.TargetSignaled:
						KillApplication (null);
						break;
					case TargetEventType.TargetStopped:
					case TargetEventType.TargetRunning:
					case TargetEventType.TargetHitBreakpoint:
					case TargetEventType.TargetInterrupted:
					case TargetEventType.UnhandledException:
						NotifyPaused ();
						break;
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
			if (!IsDebugging)
				return;

			session.Stop ();
			Cleanup ();
		}

		public void StepInto ()
		{
			if (!IsDebugging)
				//throw new Exception ("Can't step without running debugger.");
				return;

			if (IsRunning)
				throw new Exception ("Can't step unless paused.");

			session.StepLine ();
			NotifyLocationChanged ();
		}

		public void StepOver ()
		{
			if (!IsDebugging)
				//throw new Exception ("Can't step without running debugger.");
				return;

			if (IsRunning)
				//throw new Exception ("Can't step unless paused.");
				return;

			session.NextLine ();
			NotifyLocationChanged ();
		}

		public void StepOut ()
		{
			if (!IsDebugging)
				return;

			if (IsRunning)
				return;

			session.Finish ();
			NotifyLocationChanged ();
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
		
		public Backtrace CurrentBacktrace {
			get { return current_backtrace; }
		}

		public string CurrentFilename {
			get {
				StackFrame sf = FindCurrentFrame ();
				if (sf != null)
					return sf.SourceLocation.Filename;
				else
					return null;
			}
		}

		public int CurrentLineNumber {
			get {
				StackFrame sf = FindCurrentFrame ();
				if (sf != null)
					return sf.SourceLocation.Line;
				else
					return -1;
			}
		}
		
		StackFrame FindCurrentFrame ()
		{
			if (current_backtrace != null) {
				for (int n=0; n<current_backtrace.FrameCount; n++) {
					StackFrame sf = current_backtrace.GetFrame (n);
					if (!string.IsNullOrEmpty (sf.SourceLocation.Filename))
						return sf;
				}
			}
			return null;
		}
	}
}
